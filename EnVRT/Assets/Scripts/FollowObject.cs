using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObject : MonoBehaviour
{

    public Transform target;
    public Vector3 offset;
    public Vector3 rotOffset;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Follow the target and rotation
        transform.position = target.position + offset;
        transform.rotation = target.rotation * Quaternion.Euler(rotOffset);
    }
}
