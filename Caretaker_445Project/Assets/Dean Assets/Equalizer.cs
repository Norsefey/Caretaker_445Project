using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class Equalizer : MonoBehaviour
{
    //referenced tutorial(s): https://www.youtube.com/watch?v=xtJgi8SblIk&t=235s, https://youtu.be/xtJgi8SblIk?si=_IKA_z1IcEXReYJj

    NavMeshAgent agent;

    GameObject elemental;

    GameObject equalizer;

    public int elementalsSpawned = 0;

    [SerializeField] LayerMask groundLayer, elementalLayer;


    //Codes for roaming
    Vector3 destination;
    bool destpointSet;
    [SerializeField] float range;

    //state change
    [SerializeField] float sightRange, attackRange;
    bool elementalInSight, elementalInAttackRange;
    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        elemental = GameObject.FindGameObjectWithTag("Elemental");
    }

    // Update is called once per frame
    void Update()
    {
        // Checks for objects within a sphere. Calls different functions depending on if the AI sees the elemental, is in range to attack, or neither
        elementalInSight = Physics.CheckSphere(transform.position, sightRange, elementalLayer);
        elementalInAttackRange = Physics.CheckSphere(transform.position, attackRange, elementalLayer);

        if (!elementalInSight && !elementalInAttackRange) Roam();
        if (elementalInSight && !elementalInAttackRange) Chase();
        if (elementalInSight && elementalInAttackRange) Attack();
        despawn();
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

        if (Physics.Raycast(destination, Vector3.down, groundLayer))
        {
            destpointSet = true;
        }
    }

    void Chase() //targeting function
    {
        agent.SetDestination(elemental.transform.position);
    }

    void Attack()
    {
   
    }


    public void despawn() // despawns equalizer after a set amount of time
    {
        Destroy(gameObject, 10f);
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Elemental"))
        {
            Destroy(other.gameObject);
        }
    }

    public void equalizerTrigger()
    {
        elemental = GameObject.FindGameObjectWithTag("Elemental");
        if (elementalsSpawned == 10)
        {
            Instantiate(equalizer);
        }
    }
}  