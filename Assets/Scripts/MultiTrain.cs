using UnityEngine;
using System.Collections.Generic;
using System;

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

    public override float GetEffectiveMaxBrake()
    {
        return base.GetEffectiveMaxBrake() * numberOfCars;
    }

    private int IterateOverLatterCars(Action<float, int> action, bool reverseReversed = true)
    {
        float lastT = currentT;
        int historyDepth = 0;

        for (int i = 1; i < numberOfCars; i++)
        {
            int preferSplineIndex = historyDepth + 1 < history.splineIndices.Count ? getSplineIndex(historyDepth+1) : -1;
            int preferDirection = historyDepth + 1 < history.splineIndices.Count ? (getIsReversed(historyDepth+1) ^ reverseReversed ? 0 : 1) : -1;
            StepResult stepResult = splineManager.StepDistance(getSplineIndex(historyDepth), lastT, -carSpacing * (getIsReversed(historyDepth) ? -1f : 1f), useStraightLineDistance: true, preferSplineIndex: preferSplineIndex, preferDirection: preferDirection);
            lastT = stepResult.newT;
            if (getSplineIndex(historyDepth) != stepResult.newSplineIndex){
                historyDepth++;

                if (historyDepth >= history.splineIndices.Count)
                {
                    break;
                }
            }

            action(lastT, historyDepth);
        }

        return historyDepth;
    }

    public override void ApplyForces()
    {
        base.ApplyForces();

        maxUsedHistoryDepth = IterateOverLatterCars((t, historyDepth) => {
            ApplyGravity(getSplineIndex(historyDepth), t);
            ApplyBrake(getSplineIndex(historyDepth), t);
        });
    }

    public override TrainGraphics[] GetTrainGraphics()
    {
        TrainGraphics[] result = new TrainGraphics[numberOfCars];
        result[0] = ComputeTrainGraphics();

        int i = 1;
        IterateOverLatterCars((t, historyDepth) => {
            result[i] = ComputeTrainGraphics(getSplineIndex(historyDepth), t, getIsReversed(historyDepth));
            i++;
        });

        return result;
    }

    protected override void OnDrawGizmos()
    {
        if (splineManager == null || !attached) return;

        base.OnDrawGizmos();

        int i = 1;
        IterateOverLatterCars((t, historyDepth) => {
            Gizmos.color = i % 2 == 0 ? Color.red : Color.blue;
            DrawBogey(getSplineIndex(historyDepth), t);
            i++;
        });
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

        if (GUILayout.Button("Start Drive to Goal"))
        {
            multiTrain.StartDriveToGoal();
        }
    }
}
#endif