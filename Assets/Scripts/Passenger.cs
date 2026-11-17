using UnityEngine;

public class Passenger : Ressource
{   
    public Passenger(TrainStation start, TrainStation dest) : base(start, dest){}
    public override void onDelivered(){Debug.Log("dropped 1 passenger");}
    public override void onTaken(){Debug.Log("took 1 passenger");}
}