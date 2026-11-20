using System.Collections.Generic;
using UnityEngine;

public class LogicalTrain
{
    public List<TrainStation> schedule;
    public TrainStation currentStation;
    private int currentStationIndex = 0;

    public List<Ressource> ressources = new List<Ressource>();
    public int peopleCapacity = 3;
    public int containerCapacity = 2;

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

    void dropOff<T>(List<T> ressourceList) where T : Ressource
    {
        for (int i = ressources.Count - 1; i >= 0; i--)
        {
            if (ressources[i] is not T r) continue;
            if (r.route.Count < 2)
                continue;

            if (r.route[1] == currentStation)
            {
                r.changeStation();
                ressourceList.Add(r);
                ressources.RemoveAt(i);

                r.onDelivered();
            }
        }
    }

    void takeOnRessources()
    {
        takeOn(currentStation.passengers, peopleCapacity);
        takeOn(currentStation.containers, containerCapacity);
    }

    void takeOn<T>(List<T> ressourceList, int ressourceCapacity) where T : Ressource
    {
        int loadedCount = 0;
        foreach (Ressource r in ressources)
        {
            if (r is T) loadedCount++;
        }

        for (int i = ressourceList.Count - 1; i >= 0; i--)
        {
            if (loadedCount >= ressourceCapacity) break;

            T r = ressourceList[i];

            if (r.route.Count < 2) continue;

            if (r.route[1] == currentStation) continue;

            if (schedule.Contains(r.route[1]))
            {
                ressources.Add(r);
                ressourceList.RemoveAt(i);
                loadedCount++;

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