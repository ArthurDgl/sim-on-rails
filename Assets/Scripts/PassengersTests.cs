using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PassengersTests : MonoBehaviour
{
    public LogicalTrain train;
    public List<TrainStation> stations = new List<TrainStation>();

    void Start()
    {
        stations.Add(new TrainStation());
        stations.Add(new TrainStation());
        stations.Add(new TrainStation());

        stations[0].passengers.Add(new Passenger(stations[0], stations[2]));
        stations[0].passengers.Add(new Passenger(stations[0], stations[2]));
        stations[1].passengers.Add(new Passenger(stations[1], stations[2]));
        stations[1].passengers.Add(new Passenger(stations[1], stations[2]));
        stations[2].passengers.Add(new Passenger(stations[2], stations[1]));
        stations[2].passengers.Add(new Passenger(stations[2], stations[0]));

        stations[0].containers.Add(new Container(stations[1], stations[2]));
        stations[1].containers.Add(new Container(stations[2], stations[1]));
        stations[2].containers.Add(new Container(stations[2], stations[0]));

        train = new LogicalTrain(stations);
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

        if (GUILayout.Button("Next Station"))
        {
            passengersTests.train.goToNextStation();
        }
    }
}
#endif
