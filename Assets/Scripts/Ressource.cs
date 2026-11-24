using System.Collections;
using System.Collections.Generic;

public class Ressource
{
    public List<TrainStation> route;

    public Ressource(TrainStation start, TrainStation dest)
    {
        route = new List<TrainStation>
        {
            start,
            dest
        };
    }

    public void changeStation()
    {
        route.RemoveAt(0);
    }

    public virtual void onDelivered() {}
    public virtual void onTaken() {}
    public virtual IEnumerator wait() { yield break; }

}
