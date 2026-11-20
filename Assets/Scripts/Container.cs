using UnityEngine;

public class Container : Ressource
{
    public Container(TrainStation start, TrainStation dest) : base(start, dest){}
    public override void onDelivered(){Debug.Log("dropped 1 container");}
    public override void onTaken(){Debug.Log("took 1 container");}

}