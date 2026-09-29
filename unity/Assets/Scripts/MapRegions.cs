using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // The regions the pause map and collection album are organised by (docs/ui-design.md). Each has a
    // containment test and an outline (world x/z) for the map; keepsakes belong to the region they lie in.
    public static class MapRegions
    {
        public sealed class Region{public string id,name,blurb;public Color colour;public System.Func<float,float,bool> contains;public List<Vector2> outline=new();}
        static List<Region> all;
        public static IReadOnlyList<Region> All=>all??=Build();
        public static bool InTown(float x,float z)=>Vector2.Distance(new(x,z),HillcrestRoute.HillCentre)<232||(z<-872&&z>-962&&x>-104&&x<472);
        static List<Region> Build()
        {
            var list=new List<Region>();
            // Bayview island, its halt and the causeway (everything north of the headland cliffs).
            var bay=new Region{id="bayview",name="Bayview",blurb="Home island · harbour town",colour=Toy.Sky,contains=(x,z)=>z>-452};
            foreach(var p in new[]{new Vector2(-170,130),new Vector2(190,130),new Vector2(190,-452),new Vector2(-170,-452)})bay.outline.Add(p);
            // Hillcrest town: the hill, its spiral streets and the lower town along the quay (star-shaped from
            // the hill centre, so the outline is traced by rays).
            var town=new Region{id="hillcrest",name="Hillcrest",blurb="Market town on the hill",colour=Toy.Orange,contains=InTown};
            var c=HillcrestRoute.HillCentre;
            for(int k=0;k<96;k++){float a=k*Mathf.PI*2/96;var d=new Vector2(Mathf.Sin(a),Mathf.Cos(a));float r=0;for(float s=0;s<520;s+=4)if(InTown(c.x+d.x*s,c.y+d.y*s))r=s;town.outline.Add(c+d*r);}
            // The valley: the rest of the mainland.
            var valley=new Region{id="valley",name="Hillcrest Valley",blurb="Farms, falls, the raceway and the lake",colour=Toy.Mint,contains=(x,z)=>z<=-452&&!InTown(x,z)};
            float x0=WorldMap.X0+40,x1=WorldMap.X0+WorldMap.Size-40,z1=WorldMap.Z0+40;
            foreach(var p in new[]{new Vector2(x0,-452),new Vector2(x1,-452),new Vector2(x1,z1),new Vector2(x0,z1)})valley.outline.Add(p);
            list.Add(bay);list.Add(town);list.Add(valley);return list;
        }
        public static Region At(Vector3 p){foreach(var r in All)if(r.contains(p.x,p.z))return r;return All[^1];}
        // Where each keepsake is and what it's called (AdventureGame places them; the album names them).
        public static readonly Dictionary<string,(Vector3 at,string place)> Keepsakes=new();
        public static string StampPlace(int i)=>i switch{0=>"Valley viewpoint",1=>"Silver Falls lookout",2=>"Castle courtyard",3=>"Raceway paddock",4=>"Hillcrest Station",5=>"Balloon field",6=>"Vineyard hills",7=>"Lake meadows",_=>"Hillcrest Valley"};
        public static string PostcardPlace(int i)=>i switch{0=>"Coast road",1=>"Harbour bend",2=>"Lighthouse run",3=>"Windmill hill",4=>"Campsite straight",_=>"Bayview"};
    }
}
