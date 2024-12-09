using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class Equalizer : MonoBehaviour
{
    //referenced tutorial: https://www.youtube.com/watch?v=xtJgi8SblIk&t=235s

    NavMeshAgent agent;

    GameObject spirit;

    [SerializeField] LayerMask groundLayer, elementalLayer;


    //Codes for roaming
    Vector3 destination;
    bool destpointSet;
    [SerializeField] float range;

    //state change
    [SerializeField] float sightRange, attackRange;
    bool spiritInSight, spiritInAttackRange;
    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        spirit = GameObject.FindGameObjectWithTag("Spirit");
    }

    // Update is called once per frame
    void Update()
    {
        // Checks for objects within a sphere. Calls different functions depending on if the AI sees the spirit, is in range to attack, or neither
        spiritInSight = Physics.CheckSphere(transform.position, sightRange, elementalLayer);
        spiritInAttackRange = Physics.CheckSphere(transform.position, attackRange, elementalLayer);

        if(!spiritInSight && !spiritInAttackRange) Roam();
        if (spiritInSight && !spiritInAttackRange) Chase();
        if (spiritInSight && spiritInAttackRange) Attack();
    }

    void Roam()
    {
        if (!destpointSet) SearchForDest(); //If no destination point is set, a function is triggered to look for one
        if (destpointSet) agent.SetDestination(destination); //checks if destination point is set and makes it destination
        if (Vector3.Distance(transform.position, destination) < 10) destpointSet = false; //checks if vector3 distance between transform.position and destination is less than ten units. destpointSet is false if so
    }

    void SearchForDest()
    {
        float x = Random.Range(-range, range);
        float z = Random.Range(-range, range);

        destination = new Vector3(transform.position.x + x, transform.position.y, transform.position.z + z);

        if (Physics.Raycast(destination, Vector3.down))
        {
            destpointSet = true;
        }
    }

    void Chase()
    {
        agent.SetDestination(spirit.transform.position);
    }

    void Attack()
    {

    }    
}
