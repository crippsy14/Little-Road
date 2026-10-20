using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Bramblewick (docs/82): the West Country farm village 1.7 km east of Hillcrest's Golden Acre Farm, the first place on
    // docs/81's list. Behind --bramblewick (off by default) until it's judged.
    //   - The land: a farmland vale at about 34 m, gently rolling, framed by low hills, in place of the valley's east ring of
    //     hills there; a pass through that ring from the farm, where Valley Road South climbs out of the valley.
    //   - Valley Road South: off the farm lane at a T before Golden Acre's gate, over the pass and down into the village, on
    //     to a gate at its far side (closed until Vinelands, as the Kettle Falls road's).
    // The valley's height grid reaches east to x 3500 and south to z -2500 with it (ValleyField), so the vale has its mesh,
    // collider and roads as the valley does.
    public static class Bramblewick
    {
        public static bool Enabled=>enabled??=RaceTrial.Has("--bramblewick");static bool? enabled;
        public static readonly Vector2 Centre=new(2400,-1780);
        public const float GreenY=34,East=3500,South=-2500,FarmFloor=12;   // (FarmFloor: Golden Acre's level, about 11.7 m; not HillcrestRoute.FarmY, which asks this)
        public const string RoadName="Valley Road South";
        // The pass: from the farm lane, east through the ring of hills, down to the green.
        static readonly Vector2[] Pass={new(620,-1190),new(820,-1260),new(1050,-1350),new(1300,-1450),new(1560,-1560),new(1820,-1660),new(2100,-1740),new(2320,-1775)};
        public static Vector3 Tee,Gate;
        public static bool Contains(float x,float z)=>Vector2.Distance(new(x,z),Centre)<950;
        static float Blend(float a,float b,float x)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,x));
        static float PassDistance(float x,float z,out float t)
        {
            float best=float.MaxValue,run=0,bestRun=0,total=0;for(int i=1;i<Pass.Length;i++)total+=Vector2.Distance(Pass[i-1],Pass[i]);
            var p=new Vector2(x,z);
            for(int i=1;i<Pass.Length;i++){var a=Pass[i-1];var ab=Pass[i]-a;float L=ab.magnitude;float u=Mathf.Clamp01(Vector2.Dot(p-a,ab)/(L*L));float d=Vector2.Distance(p,a+ab*u);if(d<best){best=d;bestRun=run+u*L;}run+=L;}
            t=bestRun/total;return best;
        }
        // The vale over the valley's land `valley` (ValleyNatural, or Quarry Hills' gap through it).
        public static float Natural(float x,float z,float valley)
        {
            float d=Vector2.Distance(new(x,z),Centre);
            float n1=Mathf.PerlinNoise((x+5100)*.004f,(z+700)*.004f),n2=Mathf.PerlinNoise(x*.017f+31,z*.017f+11),n3=Mathf.PerlinNoise(x*.0025f+9,z*.0025f+3);
            // Farmland: the green at 34 m, the fields rolling ±6 m, rising gently to the vale's rim; then hills round it.
            float farm=GreenY+(n1-.5f)*12+(n2-.5f)*3+10*Blend(350,850,d);
            float hills=farm+70*Blend(850,1300,d)*(.6f+.8f*n3);
            float vale=Mathf.Lerp(valley,hills,1-Blend(1250,1500,d));
            // The pass: a broad saddle from the valley floor up to the vale, the hills either side left standing.
            float dp=PassDistance(x,z,out float t);if(dp>260)return vale;
            float floor=Mathf.Lerp(FarmFloor,GreenY,Mathf.SmoothStep(0,1,t))+(n2-.5f)*2;
            return vale<=floor?vale:Mathf.Lerp(floor,vale,Blend(60,250,dp));
        }
        // Valley Road South: a T on the farm lane, the pass, the green, and on to the gate.
        public static List<HillcrestRoute.Road> Roads(Vector3[] farmLane)
        {
            int tee=0;float best=float.MaxValue;for(int i=0;i<farmLane.Length;i++){float d=Vector2.Distance(new(farmLane[i].x,farmLane[i].z),new(560,-1158));if(d<best){best=d;tee=i;}}
            Tee=farmLane[tee];Gate=new Vector3(2780,float.NaN,-2040);
            var via=new List<Vector2>{new(Tee.x+12,Tee.z-30)};via.AddRange(Pass[1..]);via.Add(new(2520,-1830));via.Add(new(2660,-1930));
            var line=QuarryHills.Profile(Tee,via.ToArray(),Gate,.08f,30,QuarryHills.BackStreetLanding);   // (level off the T for 20 m: a 1 m drop in its first 12 m tilted the junction)
            return new List<HillcrestRoute.Road>{HillcrestRoute.Make(RoadName,line,7.5f,6,40)};
        }
    }
}
