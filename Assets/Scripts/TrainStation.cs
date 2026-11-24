using System.Collections.Generic;

public class TrainStation
{
    public string name;
    public List<Passenger> passengers = new List<Passenger>();
    public List<Container> containers = new List<Container>();

    public TrainStation(string name = "Station")
    {
        this.name = name;
    }
}
