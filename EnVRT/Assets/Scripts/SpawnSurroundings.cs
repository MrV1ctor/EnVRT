using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSurroundings : MonoBehaviour
{


    //list of gameobjects to spawn
    public GameObject[] surroundings;

    public GameObject player;

    private int currentIndex = -1;
    private bool instantiateNext = false;
    private bool raycastNext = false;
    private GameObject surrounding;


    //on scene start spawn a random surrounding
    void Start()
    {
        SpawnSurrounding();
    }

    // Update is called once per frame
    void Update()
    {
        if (instantiateNext) {
            Instantiate(surrounding, new Vector3(0, 0, 0), Quaternion.identity);
            raycastNext = true;
            instantiateNext = false;
        } else if (raycastNext) {
            raycastAndSetPlayerPosition();
            raycastNext = false;
        }
    }

    //make function to spawn surroundings
    public void SpawnSurrounding()
    {
        
        int randomIndex;
        
        //no repeats
        do {
            randomIndex = Random.Range(0, surroundings.Length);
        } while (randomIndex == currentIndex);
        
        surrounding = surroundings[randomIndex];

        //set tag
        surrounding.tag = "surrounding";

        //remove old surroundings (repeated code because not instant)
        GameObject oldSurrounding = GameObject.FindGameObjectWithTag("surrounding");
        if (oldSurrounding != null)
        {
            Destroy(oldSurrounding);
            instantiateNext = true;
        } else {
            //spawn new surroundings
            Instantiate(surrounding, new Vector3(0, 0, 0), Quaternion.identity);

            raycastAndSetPlayerPosition();

        }
        
    }

    public void raycastAndSetPlayerPosition() {

        //set player position to 0, 0, 0 (so its not in the way of raycast accidentally)
        player.transform.position = new Vector3(0, 0, 0);
        //raycast down to find the ground
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(0, -1, 0), Vector3.down, out hit)) {//y=-1 to avoid player
            Debug.Log("Hit: " + hit.collider.gameObject.name);
            // if hit tag surrounding
            if (hit.collider.gameObject.tag == "surrounding") {
                player.transform.position = hit.point;
                Debug.Log("Player position set to: " + player.transform.position);
                return;
            }
        }//look up if nothing found down
        else if (Physics.Raycast(new Vector3(0, 2, 0), Vector3.up, out hit)) {//y=2 to avoid player
            Debug.Log("Hit: " + hit.collider.gameObject.name);
            // if hit tag surrounding
            if (hit.collider.gameObject.tag == "surrounding") {
                //raycast back down because hit roof not floor
                RaycastHit downHit;
                if (Physics.Raycast(hit.point, Vector3.down, out downHit)) {
                    Debug.Log("Down hit: " + downHit.collider.gameObject.name);
                    // if hit tag surrounding
                    if (downHit.collider.gameObject.tag == "surrounding") {
                        player.transform.position = downHit.point;
                        Debug.Log("Player position set to: " + player.transform.position);
                        return;
                    }

                }
                else {
                    player.transform.position = hit.point;
                }
            }

        }

    }


}
