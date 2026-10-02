using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// The Duke's drive (docs/51): out of the courtyard, down to the middle of the headland tunnel and back, on the roads,
// at sane speeds, with the Shadow record accepted by save validation.
public static class DukeDriveChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();void Check(bool pass,string label){if(!pass)throw new Exception("Duke drive check: "+label);checks.Add(label);}
        var c=HillcrestRoute.CastleCourt;float a=(172+20)*Mathf.Deg2Rad;var home=new Vector3(c.x+Mathf.Sin(a)*24,c.y,c.z+Mathf.Cos(a)*24);
        var path=new List<Vector3>();var run=new List<float>();var limit=new List<float>();
        float turn=DukeDrive.BuildRoute(home,Quaternion.Euler(0,262,0),path,run,limit);float total=run[^1];
        Check(total>1500&&total<5000&&Mathf.Abs(turn-total*.5f)<40,$"The route runs out and back: {total:0} m, turning at {turn:0} m");
        var tunnel=HillcrestRoute.Mainline.At((HillcrestRoute.PortalNorthRun+HillcrestRoute.PortalSouthRun)*.5f);
        int ti=run.FindIndex(r=>r>=turn);Check(Vector2.Distance(new(path[ti].x,path[ti].z),new(tunnel.x,tunnel.z))<8,"He turns in the middle of the headland tunnel, out of sight");
        Check(limit.Max()<=24.01f&&limit[0]==0&&limit[^1]==0,$"Speeds: at most 24 m/s, from and to a standstill ({limit.Max():0.0})");
        Check(Enumerable.Range(0,run.Count).Where(i=>run[i]<30||run[i]>total-30).All(i=>limit[i]<=5.51f),"Walking pace through the courtyard");
        var roads=new[]{HillcrestRoute.Mainline,HillcrestRoute.Quay,HillcrestRoute.Crest};float worst=0;
        for(int i=0;i<path.Count;i+=5){if(run[i]<45||run[i]>total-45)continue;float d=roads.Min(r=>HillcrestRoute.Distance(r.line,path[i].x,path[i].z,out _));worst=Mathf.Max(worst,d);}
        Check(worst<3.4f,$"On the road all the way (furthest {worst:0.0} m from a centreline, in his lane)");
        Check(DukeDrive.Known(DukeDrive.ShadowId)&&!DukeDrive.Known("nonsense"),"Shadow the Duke records pass save validation");
        return checks;
    }
}
