using System.Collections.Generic;
using UnityEngine;

public class LogicalTrain
{
    public List<TrainStation> schedule;
    public TrainStation currentStation;
    private int currentStationIndex = 0;

    public List<Ressource> ressources = new List<Ressource>();
    public int maxCapacity = 3;

    public LogicalTrain(List<TrainStation> stations)
    {
        schedule = stations;

        if (schedule == null || schedule.Count == 0)
        {
            Debug.LogError("No stations!");
            return;
        }

        currentStation = schedule[0];
        takeOnRessources();
    }

    public void goToNextStation()
    {
        if (schedule == null || schedule.Count == 0) return;

        Debug.Log("Changed station");
        next();
        dropOffRessources();
        takeOnRessources();
    }

    void dropOffRessources()
    {
        dropOff(currentStation.passengers);
        dropOff(currentStation.containers);
    }

    void dropOff<T>(List<T> stationList) where T : Ressource
    {
        for (int i = ressources.Count - 1; i >= 0; i--)
        {
            if (ressources[i] is not T r)
                continue;

            if (r.route.Count < 2)
                continue;

            if (r.route[1] == currentStation)
            {
                r.changeStation();
                stationList.Add(r);
                ressources.RemoveAt(i);

                r.onDelivered();
            }
        }
    }

    void takeOnRessources()
    {
        takeOn(currentStation.passengers);
        takeOn(currentStation.containers);
    }

    void takeOn<T>(List<T> stationList) where T : Ressource
    {
        for (int i = stationList.Count - 1; i >= 0; i--)
        {
            if (ressources.Count >= maxCapacity)
                break;

            T r = stationList[i];

            if (r.route.Count < 2)
                continue;

            if (r.route[1] == currentStation)
                continue;

            if (schedule.Contains(r.route[1]))
            {
                ressources.Add(r);
                stationList.RemoveAt(i);

                r.onTaken();
            }
        }
    }

    void next()
    {
        currentStationIndex = (currentStationIndex + 1) % schedule.Count;
        currentStation = schedule[currentStationIndex];
    }
}