using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class IdleState : PetState
{
    private float idleTimer;
    private float minIdleTime = 3f;
    private float maxIdleTime = 8f;
    private float targetIdleTime;

    public IdleState(PetBehavior pet) : base(pet, PetStateType.Idle) 
    {
    }
    public override void EnterState()
    {
        agent.isStopped = true;
        targetIdleTime = Random.Range(minIdleTime, maxIdleTime);
        idleTimer = 0f;

        if(pet.anime != null)
            pet.anime.SetTrigger("Idle");

        // Slightly recover energy while idle
        pet.needs.ModifyNeed(PetNeedType.Energy, 2f);
    }
    public override void UpdateState()
    {
        idleTimer += Time.deltaTime;

        // Occasionally look around
        if (Random.Range(0f, 100f) < 5f)
        {
            Vector3 randomLook = pet.transform.position + Random.insideUnitSphere * 5f;
            randomLook.y = pet.transform.position.y;
            pet.transform.LookAt(randomLook);
        }
    }
    public override void ExitState()
    {
        agent.isStopped = false;
    }
}
