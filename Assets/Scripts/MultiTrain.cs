using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MultiTrain : Train
{
    public int numberOfCars = 3;
    public float carSpacing = 10f;

    public override List<int> GetDefaultHistoryFor(int currentSplineIndex, bool reversed = false)
    {
        List<int> result = base.GetDefaultHistoryFor(currentSplineIndex, reversed);

        float lastT = currentT;
        int newSplineIndex = currentSplineIndex;
        bool currentReversed = reversed;

        for (int i = 1; i < numberOfCars; i++)
        {
            StepResult stepResult = splineManager.StepDistance(newSplineIndex, lastT, -carSpacing * (currentReversed ? -1f : 1f), useStraightLineDistance: true);
            lastT = stepResult.newT;
            if (stepResult.reachedEnd){
                newSplineIndex = stepResult.newSplineIndex;
                currentReversed = stepResult.reverseDirection;
                result.Add(currentReversed ? -newSplineIndex : newSplineIndex);
            }
        }
        return result;
    }

    public override float GetEffectiveMass()
    {
        return mass * numberOfCars;
    }

    public override void ApplyForces()
    {
        base.ApplyForces();

        float lastT = currentT;
        int historyDepth = 0;

        for (int i = 1; i < numberOfCars; i++)
        {
            int preferSplineIndex = historyDepth + 1 < history.splineIndices.Count ? getSplineIndex(historyDepth+1) : -1;
            int preferDirection = historyDepth + 1 < history.splineIndices.Count ? (getIsReversed(historyDepth+1) ? 1 : 0) : -1; // Iterating backwards through the train, so reversed is flipped
            StepResult stepResult = splineManager.StepDistance(getSplineIndex(historyDepth), lastT, -carSpacing * (getIsReversed(historyDepth) ? -1f : 1f), useStraightLineDistance: true, preferSplineIndex: preferSplineIndex, preferDirection: preferDirection);
            lastT = stepResult.newT;
            if (getSplineIndex(historyDepth) != stepResult.newSplineIndex){
                historyDepth++;

                if (historyDepth >= history.splineIndices.Count)
                {
                    break;
                }
            }

            ApplyGravity(getSplineIndex(historyDepth), lastT);
            ApplyBrake(getSplineIndex(historyDepth), lastT);
        }

        maxUsedHistoryDepth = historyDepth;
    }

    protected override void OnDrawGizmos()
    {
        if (splineManager == null || !attached) return;

        base.OnDrawGizmos();

        float lastT = currentT;
        int historyDepth = 0;

        for (int i = 1; i < numberOfCars; i++)
        {
            int preferSplineIndex = historyDepth + 1 < history.splineIndices.Count ? getSplineIndex(historyDepth+1) : -1;
            int preferDirection = historyDepth + 1 < history.splineIndices.Count ? (getIsReversed(historyDepth+1) ? 1 : 0) : -1; // See comment above about iterating backwards through the train
            StepResult stepResult = splineManager.StepDistance(getSplineIndex(historyDepth), lastT, -carSpacing * (getIsReversed(historyDepth) ? -1f : 1f), useStraightLineDistance: true, preferSplineIndex: preferSplineIndex, preferDirection: preferDirection);
            lastT = stepResult.newT;
            if (getSplineIndex(historyDepth) != stepResult.newSplineIndex){
                historyDepth++;

                if (historyDepth >= history.splineIndices.Count)
                {
                    break;
                }
            }

            Gizmos.color = i % 2 == 0 ? Color.red : Color.blue;
            DrawBogey(getSplineIndex(historyDepth), lastT);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MultiTrain))]
public class MultiTrainEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MultiTrain multiTrain = (MultiTrain)target;
        if (GUILayout.Button("Pathfind to Goal"))
        {
            multiTrain.PathfindToGoal();
        }
    }
}
#endif