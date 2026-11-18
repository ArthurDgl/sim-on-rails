using UnityEngine;

public class MultiTrain : Train
{
    public int numberOfCars = 3;
    public float carSpacing = 10f;

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
            StepResult stepResult = splineManager.StepDistance(getSplineIndex(historyDepth), lastT, -carSpacing * (getIsReversed(historyDepth) ? -1f : 1f), useStraightLineDistance: true, preferSplineIndex: preferSplineIndex);
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

    void OnDrawGizmos()
    {
        if (splineManager == null || !attached) return;

        float lastT = currentT;
        int historyDepth = 0;

        for (int i = 0; i < numberOfCars; i++)
        {
            Gizmos.color = i % 2 == 0 ? Color.red : Color.blue;
            DrawBogey(getSplineIndex(historyDepth), lastT);

            if (i < numberOfCars - 1)
            {
                int preferSplineIndex = historyDepth + 1 < history.splineIndices.Count ? getSplineIndex(historyDepth+1) : -1;
                StepResult stepResult = splineManager.StepDistance(getSplineIndex(historyDepth), lastT, -carSpacing * (getIsReversed(historyDepth) ? -1f : 1f), useStraightLineDistance: true, preferSplineIndex: preferSplineIndex);
                lastT = stepResult.newT;
                if (getSplineIndex(historyDepth) != stepResult.newSplineIndex){
                    historyDepth++;

                    if (historyDepth >= history.splineIndices.Count)
                    {
                        break;
                    }
                }
            }
        }
    }
}
