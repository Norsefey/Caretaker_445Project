using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class objectSpawner : MonoBehaviour
{
    public GameObject[] objects;
    [SerializeField] float spawnCooldown = 1f;
    private float spawnTime;
    // Creating spawnCooldown and spawnTime variables for spawn system
    void Start()
    {
        spawnTime = spawnCooldown;
    }
    void Update()
    {
        if (spawnTime > 0) spawnTime -= Time.deltaTime; //Object spawns when cooldown timer hits zero
        if (spawnTime <= 0)
        {
            ObjectSpawn(); //Function called in update method
            spawnTime = spawnCooldown;
        }

    }

    void ObjectSpawn() //Method to spawn objects randomly in random locations
    {
        int randomIndex = Random.Range(0, objects.Length);

        Vector3 randomSpawnPoint = new Vector3(Random.Range(-10, 11), 5, Random.Range(-10, 11));

        Instantiate(objects[randomIndex], randomSpawnPoint, Quaternion.identity);
    }

}
