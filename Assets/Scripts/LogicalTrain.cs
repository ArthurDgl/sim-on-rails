using System.Collections.Generic;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class LogicalTrain
{
    public List<TrainStation> schedule = new List<TrainStation>();
    public TrainStation currentStation;
    private int currentStationIndex = 0;
    public List<Passenger> passengers = new List<Passenger>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public LogicalTrain(List<TrainStation> stations)
{
    schedule = stations;

    if (schedule == null || schedule.Count == 0)
    {
        Debug.LogError("No stations!");
        return;
    }

    currentStationIndex = 0;
    currentStation = schedule[currentStationIndex];

    takePassengers();
}
    public void goToNextStation()
    {
        next();
        dropOffPassengers();
        takePassengers();
    }

    void dropOffPassengers()
    {
        for(int i = passengers.Count - 1; i >= 0; i--)
        {
            Passenger p = passengers[i];

            if(p.route[1] == currentStation) 
            {
                currentStation.passengers.Add(p);
                p.changeStation(currentStation);
                passengers.RemoveAt(i);
                Debug.Log("dropped 1");
            }
        }
    }

    void takePassengers()
    {
        for(int i = currentStation.passengers.Count - 1; i >= 0; i--)
        {
            Passenger p = currentStation.passengers[i];

            if(p.route.Count < 2) continue;
            if(p.route[1] == currentStation) continue;
            
            if(schedule.Contains(p.route[1]))
            {
                passengers.Add(p);
                currentStation.passengers.RemoveAt(i);
                Debug.Log("took 1");
            }
        } 
    }

    void next()
    {
        currentStationIndex = (currentStationIndex + 1) % schedule.Count;
        currentStation = schedule[currentStationIndex];
    }

}
