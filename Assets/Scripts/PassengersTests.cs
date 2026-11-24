using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PassengersTests : MonoBehaviour
{
    public LogicalTrain train;
    public List<TrainStation> stations = new List<TrainStation>();
    public bool IsAdvancingStation { get; private set; }

    void Start()
    {
        stations.Add(new TrainStation("Gare de l'Est - 1"));
        stations.Add(new TrainStation("Gare de Lyon - 2"));
        stations.Add(new TrainStation("Gare du Nord - 3"));

        stations[0].passengers.Add(new Passenger(stations[0], stations[2]));
        stations[0].passengers.Add(new Passenger(stations[0], stations[2]));
        stations[1].passengers.Add(new Passenger(stations[1], stations[2]));
        stations[1].passengers.Add(new Passenger(stations[1], stations[2]));
        stations[2].passengers.Add(new Passenger(stations[2], stations[1]));
        stations[2].passengers.Add(new Passenger(stations[2], stations[0]));

        stations[0].containers.Add(new Container(stations[0], stations[2]));
        stations[1].containers.Add(new Container(stations[1], stations[2]));
        stations[2].containers.Add(new Container(stations[2], stations[0]));
        stations[2].containers.Add(new Container(stations[2], stations[0]));
        stations[2].containers.Add(new Container(stations[2], stations[0]));

        train = new LogicalTrain(stations);
        StartCoroutine(InitializeTrain());
    }

    IEnumerator InitializeTrain()
    {
        IsAdvancingStation = true;
        try { yield return train.initialize(); }
        finally { IsAdvancingStation = false; }
    }

    public void AdvanceToNextStation()
    {
        if (IsAdvancingStation || train == null) return;

        StartCoroutine(AdvanceToNextStationCoroutine());
    }

    IEnumerator AdvanceToNextStationCoroutine()
    {
        IsAdvancingStation = true;
        try { yield return train.goToNextStation(); }
        finally { IsAdvancingStation = false; }
    }

    void Update()
    {
        
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PassengersTests))]
public class PassengersTestsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PassengersTests passengersTests = (PassengersTests)target;

        using (new EditorGUI.DisabledScope(!Application.isPlaying || passengersTests.train == null || passengersTests.IsAdvancingStation))
        {
            if (GUILayout.Button("Next Station"))
            {
                passengersTests.AdvanceToNextStation();
            }
        }
    }
}
#endif
