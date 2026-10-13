using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// The UI rules in docs/ui-design.md that code can hold: map regions classify the world, every keepsake
// has a home region, footers name the back action, and the Castle Climb route and prizes are sane.
public static class InterfaceChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        Check(MapRegions.At(new Vector3(-40,0,-100)).id=="bayview"&&MapRegions.At(HillcrestRoute.Square).id=="hillcrest"&&MapRegions.At(HillcrestRoute.Paddock).id=="valley"&&MapRegions.At(HillcrestRoute.CastleCourt).id=="hillcrest",
            "Map regions: Bayview, Hillcrest town (square, castle) and the valley (raceway) classify correctly");
        Check(MapRegions.All.All(r=>r.outline.Count>=4),"Every map region has an outline for the pause map");
        Check(Enumerable.Range(0,Collectibles.Stamps).All(i=>MapRegions.StampPlace(i)!="Hillcrest Valley")&&Enumerable.Range(0,Collectibles.Postcards).All(i=>MapRegions.PostcardPlace(i)!="Bayview"),
            "Every postcard and stamp has its own place name in the album");
        Check(InputGlyphs.Footer(true).Contains(": back")&&InputGlyphs.Footer(true).Contains(": tabs")&&InputGlyphs.Menu.Contains(": back"),"Menu footers always name Back (and Tabs in the pause hub)");
        Check(Collectibles.Known(Collectibles.CastleTrialHard)&&Collectibles.Known(Collectibles.CastleTrial),"Both Castle Climb prizes are known collectibles (saves validate)");
        var line=CastleClimb.Line;float length=0;for(int i=1;i<line.Count;i++)length+=Vector3.Distance(line[i-1],line[i]);
        Check(line.Count>50&&length>600&&Vector3.Distance(line[^1],HillcrestRoute.CastleCourt)<40,$"The Castle Climb route runs from the town gate to the courtyard ({length:0} m)");
        Check(CastleClimb.Edge(line[line.Count/2],out _,out _,out _)<0&&CastleClimb.Edge(line[line.Count/2]+Vector3.right*40,out _,out _,out _)>0,"The climb corridor test puts the road inside and 40 m off outside");
        // The snap tool (docs/73 §4): a snap's record round-trips with everything triage needs to find and reproduce the spot.
        {
            var r=new SnapTool.Record{name="snap-check",carPosition=new Vector3(1,2,3),cameraPosition=new Vector3(4,5,6),cameraYaw=30,cameraPitch=10,cameraFov=55,hourOfDay=18.5f,area="Bayview",note="a bush floats"};
            var back=JsonUtility.FromJson<SnapTool.Record>(JsonUtility.ToJson(r));
            Check(back.carPosition==r.carPosition&&back.cameraPosition==r.cameraPosition&&back.cameraYaw==30&&back.hourOfDay==18.5f&&back.area=="Bayview"&&back.note==r.note,
                "Snap records keep the car and camera pose, time of day, area and note (docs/73)");
            Check(SnapTool.Slug("Bush floating, by the garage!")=="bush-floating-by-the-garage"&&SnapTool.Slug("!!!")=="","A snap's note becomes a short file-name slug");
            Check(SnapTool.Folder.Replace(System.IO.Path.DirectorySeparatorChar,'/').Contains("/snaps/"),"Snaps are saved in snaps/<date>/ in the game's data folder");
        }
        return checks;
    }
}
