using UnityEngine;

public class SleepState : ElementalState
{
    private float sleepTimer;
    private float minSleepTime = 5;
    public override void Enter(ElementalManager elemental, StateMachine stateMachine)
    {
        base.Enter(elemental, stateMachine);

        sleepTimer = CalculateSleepTime(elemental.MissingHP, elemental.HPRecoveryRate, minSleepTime);
        elemental.Agent.isStopped = true;
        Debug.Log($"{elemental.elementalData.elementalName} is sleeping for {sleepTimer}");
    }

    public override void Update()
    {
        sleepTimer -= Time.deltaTime;
        elemental.RestoreHP(1);
        elemental.RestoreStamina(2);
    }
    public override void OnDamageTaken(ElementalManager attacker)
    {
        Debug.Log($"{elemental.elementalData.elementalName} woke up suddenly due to taking damage!");
        //elemental.stats.DecreaseHappiness(15); // Being woken up by damage makes the elemental unhappy
        //stateMachine.ChangeState(new FleeState(attacker.transform));
    }
    public override void CheckTransitions()
    {
        // don't check for threats since we are sleeping
        //if (CheckForThreats()) return;

        if (sleepTimer <= 0)
        {
            stateMachine.ChangeState(new IdleState());
        }
    }
    public static float CalculateSleepTime(float missingHP, float recoveryRate, float randomnessFactor)
    {
        if (recoveryRate <= 0)
        {
            Debug.LogError("Recovery rate must be greater than zero.");
            return 0;
        }

        float baseSleepTime = missingHP / recoveryRate;
        float randomOffset = Random.Range(-randomnessFactor, randomnessFactor);

        return Mathf.Max(0, baseSleepTime + randomOffset); // Ensure no negative sleep time
    }
}
