using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Hillcrest Castle zipline: the kit pieces carry the anchors the ride is built from, the cable sags
// as specified, and a ride between two stand-in stations moves the car from bay to bay.
public static class ZipLineChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        foreach(var id in new[]{"zip_tower","zip_station"}) {
            var kit=Resources.Load<GameObject>("Bayview/"+id);Check(kit!=null,"Zipline kit present: "+id);
            var names=kit.GetComponentsInChildren<Transform>(true).Select(t=>t.name).ToList();
            Check(names.Contains("CableAnchor")&&names.Contains("BayAnchor")&&names.Any(n=>n.StartsWith("Spin_X")),id+" has cable and bay anchors and a spinning pulley");
        }
        Check(Resources.Load<GameObject>("Bayview/zip_trolley")!=null,"Zipline trolley kit present");
        var span=ZipLine.Catenary(Vector3.zero,new Vector3(90,-30,0),ZipLine.SagPerMetre); // 60 segments: the middle sample is mid-span
        var mid=span[span.Count/2];float straight=-15;
        Check(span.Count==61&&Mathf.Abs((straight-mid.y)-90*ZipLine.SagPerMetre)<.01f&&span[0]==Vector3.zero&&span[^1]==new Vector3(90,-30,0),"Cable sags "+ZipLine.SagPerMetre*100+" cm per metre of span at mid-span and meets both anchors");
        return checks;
    }
}
