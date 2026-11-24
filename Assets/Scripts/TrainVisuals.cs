using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Train))]
public class TrainVisuals : MonoBehaviour
{
    [SerializeField] private GameObject locomotivePrefab;
    [SerializeField] private GameObject carPrefab;

    private Train train;
    private readonly List<Transform> cars = new List<Transform>();

    private void Awake()
    {
        train = GetComponent<Train>();
    }

    private void LateUpdate()
    {
        if (locomotivePrefab == null || !train.attached || train.splineManager == null ||
            train.history == null || train.history.splineIndices.Count == 0 ||
            train.history.isReversed.Count == 0 ||
            (train is MultiTrain multiTrain && multiTrain.numberOfCars <= 0))
        {
            HideCars();
            return;
        }

        TrainGraphics[] graphics = train.GetTrainGraphics();

        while (cars.Count < graphics.Length)
        {
            GameObject prefab = cars.Count == 0 ? locomotivePrefab : carPrefab;
            if (prefab == null) break;

            GameObject holder = new GameObject("Car Visual");
            holder.transform.SetParent(transform, false);
            Instantiate(prefab, holder.transform, false);
            cars.Add(holder.transform);
        }

        for (int index = 0; index < cars.Count; index++)
        {
            bool visible = index < graphics.Length &&
                graphics[index].tangent.sqrMagnitude > 0.0001f &&
                Vector3.Cross(graphics[index].tangent, graphics[index].localUp).sqrMagnitude > 0.0001f;

            cars[index].gameObject.SetActive(visible);
            if (!visible) continue;

            cars[index].position = transform.position + graphics[index].centerOffset;
            cars[index].rotation = Quaternion.LookRotation(
                graphics[index].tangent,
                graphics[index].localUp
            );
        }
    }

    private void HideCars()
    {
        foreach (Transform car in cars)
        {
            car.gameObject.SetActive(false);
        }
    }
}