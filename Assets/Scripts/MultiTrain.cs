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
        for (int i = 1; i < numberOfCars; i++)
        {
            StepResult stepResult = splineManager.StepDistance(attachedTo, lastT, -carSpacing, useStraightLineDistance: true);
            lastT = stepResult.newT;
            int splineIndex = stepResult.newSplineIndex;

            ApplyGravity(splineIndex, lastT);
            ApplyBrake(splineIndex, lastT);
        }
    }

    void OnDrawGizmos()
    {
        if (splineManager == null || !attached) return;

        Vector3 lastPosition = transform.position;
        float lastT = currentT;
        int splineIndex = attachedTo;

        for (int i = 0; i < numberOfCars; i++)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(lastPosition, 2f);

            if (i < numberOfCars - 1)
            {
                StepResult stepResult = splineManager.StepDistance(splineIndex, lastT, -carSpacing, useStraightLineDistance: true);
                lastT = stepResult.newT;
                splineIndex = stepResult.newSplineIndex;
                lastPosition = stepResult.position;
            }
        }
    }
}
