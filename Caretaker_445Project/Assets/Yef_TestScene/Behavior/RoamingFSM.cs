using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class roamingFSM : MonoBehaviour
{
    NavMeshAgent agent;

    //Codes for roaming
    Vector3 destination;
    bool destpointSet;
    [SerializeField] float range;
    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        Debug.Log("Roaming...");
    }

    // Update is called once per frame
    void Update()
    {
        Roam();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Pet")
        {
            Debug.Log("Petting");
        }
    }
    void Roam()
    {
        if (!destpointSet) SearchForDest(); //If no destination point is set, a function is triggered to look for one
        if (destpointSet) agent.SetDestination(destination); //checks if destination point is set and makes it destination
        if (Vector3.Distance(transform.position, destination) < 10) destpointSet = false; //checks if vector3 distance between transform.position and destination is less than ten units. destpointSet is false if so.
    }

    void SearchForDest()
    {
        float x = Random.Range(-range, range);
        float z = Random.Range(-range, range);

        destination = new Vector3(transform.position.x + x, transform.position.y, transform.position.z + z);
        
        if(Physics.Raycast(destination, Vector3.down))
        {
            destpointSet = true;
        }
    }
}
