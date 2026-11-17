using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevelPhysics;

public class PassengersTests : MonoBehaviour
{
    public LogicalTrain train;
    public List<TrainStation> stations = new List<TrainStation>();
    // Start is called once berailsGeneratorfore the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        stations.Add(new TrainStation());
        stations.Add(new TrainStation());
        stations.Add(new TrainStation());

        stations[0].passengers.Add(new Passenger(stations[0], stations[1]));
        stations[0].passengers.Add(new Passenger(stations[0], stations[2]));
        stations[1].passengers.Add(new Passenger(stations[1], stations[2]));
        stations[1].passengers.Add(new Passenger(stations[1], stations[1]));
        stations[2].passengers.Add(new Passenger(stations[2], stations[1]));
        stations[2].passengers.Add(new Passenger(stations[2], stations[0]));

        train = new LogicalTrain(stations);
    }

    // Update is called once per frame
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

        if (GUILayout.Button("Next Station"))
        {
            passengersTests.train.goToNextStation();
        }
    }
}
#endif
