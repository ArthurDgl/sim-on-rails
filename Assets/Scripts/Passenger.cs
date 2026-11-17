using System.Collections.Generic;
using UnityEngine;

public class Passenger
{
    public List<TrainStation> route;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Passenger(TrainStation start, TrainStation dest)
    {
        route = new List<TrainStation>
        {
            start,
            dest
        };
    }

    public void changeStation(TrainStation nextStation)
    {
        route.RemoveAt(0);
    }

}
