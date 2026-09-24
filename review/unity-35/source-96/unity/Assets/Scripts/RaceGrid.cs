using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LittleRoad
{
    public static class RaceGrid
    {
        [Serializable] public class Entry {public int driver,slot,laps,nextGate;public float progress,nextGateProgress;public string equipment;public Vector3 position;public CarTuning tuning;}
        [Serializable] public class Evidence {public bool playerLast;public int entrants,playerStartingPlace;public Entry[] entries;}
        public static void Capture(RaceTrial race,string folder)
        {
            if(string.IsNullOrEmpty(folder))return;
            var player=race.cars[0];int place=1+race.cars.Count(c=>c!=player&&c.RankedProgress>player.RankedProgress);
            var data=new Evidence{entrants=race.cars.Count,playerStartingPlace=place,playerLast=place==race.cars.Count,
                entries=race.cars.Select(c=>new Entry{driver=c.driverId,slot=TrialSlot(c.driverId,race.cars.Count,RaceTrial.Arg("--traffic-case")),laps=c.laps,nextGate=c.nextGate,progress=c.progress,nextGateProgress=c.nextGateProgress,equipment=c.equipment,position=c.body.position,tuning=c.tuning}).ToArray()};
            Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"starting-grid.json"),JsonUtility.ToJson(data,true));
        }
        // Keep driver 0 as the player everywhere; only move their starting slot.
        public static int Slot(int driver,int count)
        {
            if(count<1||driver<0||driver>=count)throw new ArgumentOutOfRangeException(nameof(driver));
            return driver==0?count-1:driver-1;
        }
        // Counterfactual pace measurement: remove traffic, retain the24-car
        // player's exact rear-grid pose. Ordinary races still use Slot.
        public static int TrialSlot(int driver,int count,string scenario)
        {
            if(scenario!="standing-solo")return Slot(driver,count);
            if(count!=1||driver!=0)throw new ArgumentException("Standing solo requires exactly one driver.");
            return Slot(0,24);
        }
        public static float Distance(int slot,bool line)=>line?-8-slot*7:-8-(slot/3)*5.5f-(slot%3)*.6f;
        public static float Side(int slot,bool line)=>line?0:(slot%3-1)*3.5f;
        public static Vector3 Position(Circuit track,int slot,bool line)=>track.At(Distance(slot,line))+track.Right(Distance(slot,line))*Side(slot,line)+Vector3.up*.12f;
    }
}
