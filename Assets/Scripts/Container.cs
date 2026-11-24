using System.Collections;
using UnityEngine;

public class Container : Ressource
{
    public float waitTime = 2f;
    public Container(TrainStation start, TrainStation dest) : base(start, dest){}
    public override void onDelivered(){Debug.Log("dropped 1 container");}
    public override void onTaken(){Debug.Log("took 1 container");}

    public override IEnumerator wait()
    {
        yield return new WaitForSeconds(waitTime);
        Debug.Log("Container waited for " + waitTime + " seconds");
    }

}