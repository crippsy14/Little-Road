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
            if(scenario=="class-sort"){if(count!=ClassSortCars)throw new ArgumentException("Class sort needs exactly 15 cars.");return ClassSortSlot(driver);}
            if(scenario!="standing-solo")return Slot(driver,count);
            if(count!=1||driver!=0)throw new ArgumentException("Standing solo requires exactly one driver.");
            return Slot(0,24);
        }
        // Overtaking fixture (--traffic-case class-sort): 15 cars, three abreast, rows front to back
        // 3-3-3 / 2-2-2 / 3-3-3 / 1-1-1 / (empty) / 2-2-2, where class 1 is fastest (club kit),
        // 2 street and 3 stock. Clean racing should sort the field by class: 1s, then 2s, then 3s.
        public const int ClassSortCars=15;
        public static readonly int[] ClassSortRows={3,2,3,1,0,2};
        public static int ClassSortSlot(int driver){if(driver<0||driver>=ClassSortCars)throw new ArgumentOutOfRangeException(nameof(driver));int row=driver/3;if(row>=4)row++;return row*3+driver%3;}
        public static int ClassSortClass(int driver)=>ClassSortRows[ClassSortSlot(driver)/3];
        public static string ClassSortEquipment(int driver)=>ClassSortClass(driver) switch{1=>"club",2=>"street",_=>"stock"};
        // Pole's nose on the start line; rows of three 11 m apart (about 7 m clear behind a 3.8 m car) with a
        // 1.8 m stagger across the row, so the start spreads out instead of a bumper-to-bumper crush
        // (29 September; was 8 m back and 5.5 m rows). Line mode: single file, 11 m apart.
        public const float Pole=-2.6f,RowPitch=11f,RowStagger=1.8f;
        public static float Distance(int slot,bool line)=>line?Pole-slot*RowPitch:Pole-(slot/3)*RowPitch-(slot%3)*RowStagger;
        public static float Side(int slot,bool line)=>line?0:(slot%3-1)*3.5f;
        public static Vector3 Position(Circuit track,int slot,bool line)=>track.At(Distance(slot,line))+track.Right(Distance(slot,line))*Side(slot,line)+Vector3.up*.12f;
    }
}
