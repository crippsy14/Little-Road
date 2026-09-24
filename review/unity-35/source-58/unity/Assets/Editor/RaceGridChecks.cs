using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class RaceGridChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();var track=new Circuit();
        foreach(int count in new[]{1,2,3,24,32})foreach(bool line in new[]{false,true}) {
            var slots=new HashSet<int>();var positions=new List<Vector3>();float playerProgress=0;
            for(int id=0;id<count;id++) {
                int slot=RaceGrid.Slot(id,count);if(!slots.Add(slot))throw new Exception("Duplicate grid slot");
                var position=RaceGrid.Position(track,slot,line);float progress=track.Nearest(position,out float offset)-track.length;
                if(progress>=0||Mathf.Abs(offset)>7.5f)throw new Exception("Grid outside valid start approach");
                if(id==0)playerProgress=progress;else if(progress<=playerProgress)throw new Exception("Player is not last on actual route");
                foreach(var other in positions)if(Vector3.Distance(position,other)<3.4f)throw new Exception("Insufficient grid spacing");
                positions.Add(position);
            }
            if(RaceGrid.Slot(0,count)!=count-1||slots.Count!=count)throw new Exception("Wrong player grid slot");
            checks.Add("Rear player, unique valid grid and spacing: "+count+" cars, "+(line?"line":"pack"));
        }
        return checks;
    }
}
