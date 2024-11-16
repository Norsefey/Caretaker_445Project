using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

// Custom editor for better rule management
[CustomEditor(typeof(PetBehavior))]
public class PetBehaviorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PetBehavior pet = (PetBehavior)target;

        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Add Common Rules"))
        {
            AddCommonRules(pet);
        }
    }

    private void AddCommonRules(PetBehavior pet)
    {
        // Example of adding a compound rule
        SerializedObject serializedObject = new SerializedObject(pet);
        var globalRulesProperty = serializedObject.FindProperty("globalPriorityRules");

        // Create tired + unhappy → sleep rule
        var sleepRule = CreateCompoundRule(
            "Critical Sleep",
            PetStateType.Sleep,
            new (PetNeedType type, float threshold)[]
            {
                (PetNeedType.Energy, 20f),
                (PetNeedType.Happiness, 30f)
            },
            100
        );

        // Add the rule to the list
        int index = globalRulesProperty.arraySize;
        globalRulesProperty.InsertArrayElementAtIndex(index);
        var element = globalRulesProperty.GetArrayElementAtIndex(index);
        // Copy rule properties...

        serializedObject.ApplyModifiedProperties();
    }

    private StateTransitionRule CreateCompoundRule(
        string name,
        PetStateType toState,
        (PetNeedType type, float threshold)[] conditions,
        int priority)
    {
        var rule = new StateTransitionRule
        {
            transitionName = name,
            canTransitionFromAnyState = true,
            toState = toState,
            priority = priority,
            requireAllNeedConditions = true,
            needConditions = new List<NeedCondition>()
        };

        foreach (var (type, threshold) in conditions)
        {
            rule.needConditions.Add(new NeedCondition
            {
                needType = type,
                threshold = threshold,
                invertCheck = false
            });
        }

        return rule;
    }
}
#endif
