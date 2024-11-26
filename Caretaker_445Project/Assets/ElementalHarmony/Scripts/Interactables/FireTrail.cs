using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireTrail : ElementalObject
{
    [Header("Fire Trail Settings")]
    public float moveSpeed = 2f;
    public float spreadChance = 0.3f;

    private Vector3 moveDirection;

    protected override void Start()
    {
        base.Start();
        moveDirection = Random.insideUnitSphere;
        moveDirection.y = 0;
        moveDirection.Normalize();
    }

    protected override void Update()
    {
        base.Update();
        if (!isActive) return;

        // Move in direction
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // Check for plants to ignite
        Collider[] nearbyPlants = Physics.OverlapSphere(transform.position, interactionRadius);
        foreach (var collider in nearbyPlants)
        {
            Plant plant = collider.GetComponent<Plant>();
            if (plant != null && Random.value < spreadChance * Time.deltaTime)
            {
                StartCoroutine(plant.HandleFireInteraction());
            }
        }
    }

    public override bool CanInteract(GameObject spirit) => false;

    protected override IEnumerator InteractInternal(GameObject spirit)
    {
        yield break;
    }
}
