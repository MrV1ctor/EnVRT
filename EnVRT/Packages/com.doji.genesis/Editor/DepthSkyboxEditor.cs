using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using SR = Genesis.Editor.StringResources;
using UnityEngine.Experimental.Rendering;


namespace Genesis.Editor {

    [CustomEditor(typeof(DepthSkybox))]
    public class DepthSkyboxEditor : UnityEditor.Editor {

        private DepthSkybox _depthSkybox;
        private GameObject gameObject;

        private void OnEnable() {
            _depthSkybox = (DepthSkybox)target;
            gameObject = _depthSkybox.gameObject;
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            if (GUILayout.Button(SR.ExtractMeshButton)) {
                ExtractMesh();
            }
        }

        private void ExtractMesh() {
            Mesh mesh = CreateMesh();
            GameObject g = new GameObject();
            g.name = $"{gameObject.name} (Mesh)";
            var mf = g.AddComponent<MeshFilter>();
            var mr = g.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            Material mat = new Material(Shader.Find("Unlit/Texture"));
            mat.mainTexture = (Texture2D)gameObject.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_MainTex");
            mr.sharedMaterial = mat;
            mf.sharedMesh = mesh;


            //save the mesh to the project
            string path = EditorUtility.SaveFilePanelInProject("Save Mesh", ""+g.name+" [mesh]", "asset", "Save mesh to project");
            if (path.Length != 0) {
                AssetDatabase.CreateAsset(mesh, path);
                AssetDatabase.SaveAssets();
            }

            //save the material to the project
            path = EditorUtility.SaveFilePanelInProject("Save Material", ""+g.name+" [material]", "mat", "Save material to project");
            if (path.Length != 0) {
                AssetDatabase.CreateAsset(mat, path);
                AssetDatabase.SaveAssets();
            }
        }

        private Mesh CreateMesh() {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();

            if (meshFilter == null || meshRenderer == null) {
                return null;
            }

            Texture2D depthTex = (Texture2D)meshRenderer.sharedMaterial.GetTexture("_Depth");
            if (!depthTex.isReadable) {
                throw new InvalidOperationException($"The texture must be readable. (Check if Read/Write is set in the texture's import settings)");
            }
            if (!DepthSampler.IsFormatSupported(depthTex.graphicsFormat)) {
                throw new NotSupportedException($"The texture format {depthTex.graphicsFormat} of this depth texture is not supported.");                
            }


            // /*
            //on the depth texture, interpolate between the first N pixels and last N pixels for smoother transition
            int width = depthTex.width;
            int height = depthTex.height;
            int totalPixelsToInterpolate = 100;

            //ensure totalPixelsToInterpolate is within valid range
            totalPixelsToInterpolate = Mathf.Max(Mathf.Min(totalPixelsToInterpolate, width / 2), 1);

            //Calculate the step size for interpolation
            float stepSize = 1.0f / (totalPixelsToInterpolate - 1);

            for (int i=0; i < height; i++) {
                Color startPixel = depthTex.GetPixel(width - (totalPixelsToInterpolate/2), i);
                Color endPixel = depthTex.GetPixel(totalPixelsToInterpolate/2, i);

                //interpolate between the first and last N pixels at start and end of image
                for (int j=0; j < totalPixelsToInterpolate/2; j++) {
                    float t = j * stepSize;
                    Color interpolatedPixel = Color.Lerp(startPixel, endPixel, t);
                    depthTex.SetPixel((width-(totalPixelsToInterpolate/2)) + j, i, interpolatedPixel);
                 
                    t = (j+(totalPixelsToInterpolate/2)) * stepSize;
                    interpolatedPixel = Color.Lerp(startPixel, endPixel, t);
                    depthTex.SetPixel(j, i, interpolatedPixel);
                }
                //specifically set the last col to the first col
                depthTex.SetPixel(0, i, depthTex.GetPixel(width-1, i, 0), 0);
            }



            depthTex.Apply();
            // */


            IDepthSampler sampler = DepthSampler.Get(depthTex);

            float scale = meshRenderer.sharedMaterial.GetFloat("_Scale");
            float max = meshRenderer.sharedMaterial.GetFloat("_Max");
            Mesh mesh = meshFilter.sharedMesh;
            Mesh extracted;
            NativeArray<Vector3> vertices = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<ushort> indices = new NativeArray<ushort>((int)mesh.GetIndexCount(0), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            NativeArray<Vector2> uvs = new NativeArray<Vector2>(mesh.vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            using (var data = Mesh.AcquireReadOnlyMeshData(mesh)) {
                data[0].GetVertices(vertices);
                data[0].GetIndices(indices, 0);
                data[0].GetUVs(0, uvs);
            }

            for (int i = 0; i < vertices.Length; i++) {
                Vector2 uv = uvs[i];
                uv.x = 1 - uv.x;
                uvs[i] = uv;

                float depth = sampler.SampleBilinear(uv);
                depth = scale / depth;
                depth = Mathf.Clamp(depth, 0, max * scale);
                vertices[i] = vertices[i] * depth;
            }

            extracted = new Mesh();
            extracted.SetVertices(vertices);
            extracted.SetIndices(indices, MeshTopology.Triangles, 0);
            extracted.SetUVs(0, uvs);
            extracted.RecalculateNormals();
            extracted.UploadMeshData(false);
            extracted.name = $"{gameObject.name}_Mesh";

            vertices.Dispose();
            indices.Dispose();
            uvs.Dispose();

            return extracted;
        }

        private float SampleBilinear(NativeArray<float> depthData, Vector2 uv, int width, int height) {
            float i = Mathf.Lerp(0f, width - 1, uv.x);
            int i0 = Mathf.FloorToInt(i);

            int i1 = i0 < width - 1 ? i0 + 1 : i0;
            float j = Mathf.Lerp(0f, height - 1, uv.y);
            int j0 = Mathf.FloorToInt(j);
            int j1 = j0 < height - 1 ? j0 + 1 : j0;

            float q11 = depthData[(i0 * width) + j0];
            float q21 = depthData[(i1 * width) + j0];
            float q12 = depthData[(i0 * width) + j1];
            float q22 = depthData[(i1 * width) + j1];

            float dx = i - i0;
            float dy = j - j0;

            float v1 = q11 + dx * (q21 - q11);
            float v2 = q12 + dx * (q22 - q12);

            return v1 + dy * (v2 - v1);
        }
    }
}