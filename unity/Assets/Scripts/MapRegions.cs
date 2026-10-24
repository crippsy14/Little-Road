using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // The regions the pause map and collection album are organised by (docs/ui-design.md). Each has a
    // containment test and an outline (world x/z) for the map; keepsakes belong to the region they lie in.
    public static class MapRegions
    {
        public sealed class Region{public string id,name,blurb,kind="country";public Color colour;public System.Func<float,float,bool> contains;public List<Vector2> outline=new();}
        static List<Region> all;
        public static IReadOnlyList<Region> All=>all??=Build();
        public static bool InTown(float x,float z)=>Vector2.Distance(new(x,z),HillcrestRoute.HillCentre)<232||(z<-872&&z>-962&&x>-104&&x<472);
        static List<Region> Build()
        {
            var list=new List<Region>();
            // Bayview island, its halt and the causeway (everything north of the headland cliffs).
            var bay=new Region{id="bayview",kind="town",name="Bayview",blurb="Home island · harbour town",colour=Toy.Sky,contains=(x,z)=>z>-452};
            foreach(var p in new[]{new Vector2(-170,130),new Vector2(190,130),new Vector2(190,-452),new Vector2(-170,-452)})bay.outline.Add(p);
            // Hillcrest town: the hill, its spiral streets and the lower town along the quay (star-shaped from
            // the hill centre, so the outline is traced by rays).
            var town=new Region{id="hillcrest",kind="town",name="Hillcrest",blurb="Market town on the hill",colour=Toy.Orange,contains=InTown};
            var c=HillcrestRoute.HillCentre;
            for(int k=0;k<96;k++){float a=k*Mathf.PI*2/96;var d=new Vector2(Mathf.Sin(a),Mathf.Cos(a));float r=0;for(float s=0;s<520;s+=4)if(InTown(c.x+d.x*s,c.y+d.y*s))r=s;town.outline.Add(c+d*r);}
            // The valley: the rest of the mainland.
            var valley=new Region{id="valley",name="Hillcrest Valley",blurb="Farms, falls, the raceway and the lake",colour=Toy.Mint,contains=(x,z)=>z<=-452&&!InTown(x,z)};
            // (With Quarry Hills the map reaches far west and south: the valley's outline keeps to the valley.)
            float x0=QuarryHills.Enabled?QuarryHills.SeamX:WorldMap.X0+40,x1=WorldMap.X0+WorldMap.Size-40,z1=QuarryHills.Enabled?-2060:WorldMap.Z0+40;
            foreach(var p in new[]{new Vector2(x0,-452),new Vector2(x1,-452),new Vector2(x1,z1),new Vector2(x0,z1)})valley.outline.Add(p);
            // Quarry Hills (docs/59): west of the valley's hills. Listed first so its test wins over the valley's.
            if(QuarryHills.Enabled) {
                var quarry=new Region{id="quarry",kind="town",name="Quarry Hills",blurb="Slate town · galleries · the pit lake",colour=Toy.Hex(0x7f8fa3),contains=QuarryHills.Contains};
                foreach(var p in new[]{new Vector2(QuarryHills.West,-1620),new Vector2(-2450,-1480),new Vector2(-1500,-880),new Vector2(QuarryHills.SeamX,-500),new Vector2(QuarryHills.SeamX,QuarryHills.South),new Vector2(QuarryHills.West,QuarryHills.South)})quarry.outline.Add(p);
                list.Add(quarry);
            }
            // Bramblewick (docs/82): its vale east of the valley's hills, listed before the valley so its test wins.
            if(Bramblewick.Enabled) {   // (the village, judged as a village; the vale and the pass round it as country)
                var bramble=new Region{id="bramblewick",kind="village",name="Bramblewick",blurb="Farm village · the green · The Plough",colour=Toy.Hex(0xc9a95a),contains=Bramblewick.Core};
                for(int k=0;k<32;k++){float a=k*Mathf.PI*2/32;bramble.outline.Add(Bramblewick.Centre+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*380);}
                var vale=new Region{id="bramble-vale",name="Bramblewick Vale",blurb="Hedged lanes · the dairy · the pass",colour=Toy.Hex(0x9cbf6a),contains=(x,z)=>Bramblewick.Contains(x,z)&&!Bramblewick.Core(x,z)};
                for(int k=0;k<32;k++){float a=k*Mathf.PI*2/32;vale.outline.Add(Bramblewick.Centre+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*950);}
                list.Add(bramble);list.Add(vale);
            }
            // Vinelands (docs/85): the vila, and the district round it (the amphitheatre, the river, the Vale).
            if(Vinelands.Enabled) {
                var vila=new Region{id="vinelands",kind="town",name="Vinelands",blurb="Wine town · the amphitheatre · the Cais",colour=Toy.Hex(0x9b2f45),contains=Vinelands.Core};
                for(int k=0;k<32;k++){float a=k*Mathf.PI*2/32;vila.outline.Add(Vinelands.Centre+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*460);}
                var vines=new Region{id="vinelands-valley",name="The Vinelands",blurb="Terraces · the river · the Vale",colour=Toy.Hex(0xb9a24f),contains=(x,z)=>Vinelands.Contains(x,z)&&!Vinelands.Core(x,z)&&!(Bramblewick.Enabled&&Bramblewick.Contains(x,z))};   // (Bramblewick's vale stays Bramblewick's: its audit counted its own lamps and residents as ours)
                for(int k=0;k<32;k++){float a=k*Mathf.PI*2/32;vines.outline.Add(Vinelands.Centre+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*Vinelands.Radius);}
                list.Insert(0,vines);list.Insert(0,vila);
            }
            list.Add(bay);list.Add(town);list.Add(valley);return list;
        }
        public static Region At(Vector3 p){foreach(var r in All)if(r.contains(p.x,p.z))return r;return All[^1];}
    }
}
