using System.Collections;
using UnityEngine;

public class Passenger : Ressource
{   
    public float waitTime = 0.5f;
    public Passenger(TrainStation start, TrainStation dest) : base(start, dest){}
    public override void onDelivered(){Debug.Log("dropped 1 passenger");}
    public override void onTaken(){Debug.Log("took 1 passenger");}
    public override IEnumerator wait()
    {
        yield return new WaitForSeconds(waitTime);
        Debug.Log("Passenger waited for " + waitTime + " seconds");
    }
}