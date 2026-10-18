using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace LittleRoad
{
    // Prop Forge's proving ground in the live world (docs/77, step 6), behind --prop-forge: Bayview's own stamped walls,
    // fences and bales re-laid with the forge, where docs/76 means them to go first.
    //   coast wall    the outer coast road's stone wall (CoastalLandscape.Roadside stamps 3.6 m sections) as one dry-stone
    //                 run, bent round the bends, with the same gaps (RoadsideGap: the bridge, the junction, the turnouts)
    //   farm paddock  the wheat field's fence (BayviewCountryside stamps 6 m kit pieces) as one post-and-rail run from
    //                 one gatepost round its four corners to the other
    //   farm bales    the stack by the barn, and three round bales beside it
    //   quay cans     three jerry cans on the Halt quay: the library object the Cob carries (prop:jerry_can@1)
    // The ground is the island's drawn grid (CoastalLandscape.Drawn), and the quay's own surface for the cans. With the
    // flag off none of this runs and Roadside and the farm stamp their pieces as before (the world fingerprint holds it).
    public static class PropForgeBayview
    {
        sealed class Island:Ground.IProvider{public CoastalLandscape land;public float Height(float x,float z)=>land.Drawn(x,z);}
        // (whatever solid is highest under a point: the quay's paving stands above the island's ground)
        sealed class Surfaces:Ground.IProvider
        {
            public CoastalLandscape land;
            public float Height(float x,float z)
            {
                float best=float.MinValue;
                foreach(var hit in Physics.RaycastAll(new Vector3(x,60,z),Vector3.down,120))if(!hit.collider.isTrigger&&hit.collider.attachedRigidbody==null)best=Mathf.Max(best,hit.point.y);
                return best>float.MinValue?best:land.Drawn(x,z);
            }
        }
        public static double BuildMs;

        public static void Build(CoastalLandscape land,Transform root)
        {
            var sw=Stopwatch.StartNew();var parent=new GameObject("Prop Forge").transform;parent.SetParent(root,false);
            Runs.Laid.Clear();Objects.Laid.Clear();
            Ground.Use(new Island{land=land});
            try {
                var track=land.Track;var loop=new List<Vector3>(track.points){track.points[0]};
                Lay(new RunSpec{name="coast wall",kit="dry_stone",seed="bayview-coast",along=loop.ToArray(),offset=13.4f,cut=p=>land.RoadsideGap(track.Nearest(p,out _))},parent);
                var f=BayviewCountryside.Farm;Vector3 F(float x,float z)=>new(f.x+x,0,f.z+z);
                Lay(new RunSpec{name="farm paddock",kit="post_rail",seed="bayview-paddock",gateEnds=true,
                    along=new[]{F(-6,-1.2f),F(9.2f,-1.2f),F(9.2f,-20.8f),F(-23.2f,-20.8f),F(-23.2f,-1.2f),F(-12,-1.2f)}},parent);
                Place("farm stack",new[]{(f.x+20.3f,f.z+8,0f)},new[]{"stack"},parent);
                Place("farm bales",new[]{(f.x+16.5f,f.z+9.6f,25f),(f.x+24.2f,f.z+8.6f,-40f),(f.x+22.4f,f.z+11.8f,80f)},new[]{"round","wrap"},parent);
                Ground.Use(new Surfaces{land=land});
                var spot=QuaySpot(root);UnityEngine.Debug.Log($"LITTLE_ROAD_PROP_FORGE_QUAY_CANS {spot.x:0.0},{spot.z:0.0}");
                Place("quay cans",new[]{(spot.x,spot.z,8f),(spot.x+.46f,spot.z+.05f,-6f),(spot.x+.2f,spot.z-.4f,20f)},null,parent,"jerry_can");
            } finally { Ground.Reset(); }
            BuildMs=sw.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"LITTLE_ROAD_PROP_FORGE runs={Runs.Laid.Count} objects={Objects.Laid.Sum(l=>l.placed.Count)} failures={Runs.Laid.Sum(l=>l.failures.Count)+Objects.Laid.Sum(l=>l.failures.Count)} ms={BuildMs:0.0}");
            foreach(var L in Runs.Laid)UnityEngine.Debug.Log("LITTLE_ROAD_RUN "+Runs.Report(L));
            foreach(var L in Objects.Laid)UnityEngine.Debug.Log("LITTLE_ROAD_RUN "+Objects.Report(L));
            string report=RaceTrial.Arg("--run-report");if(report!=null)Runs.WriteReport(report);
        }
        static void Lay(RunSpec spec,Transform parent){var L=Runs.Solve(spec);Runs.Draw(L,parent);Runs.Laid.Add(L);}
        static void Place(string name,(float x,float z,float yaw)[] spots,string[] only,Transform parent,string kit="hay_bale")
        {
            var L=Objects.Place(name,kit,spots,"bayview-"+name,false,only);Objects.Draw(L,parent);Objects.Laid.Add(L);
        }
        // A clear spot on the Halt quay for the cans: inside the quay by 2 m, clear of the mainline, of every placed
        // footprint, and of anything drawn there, within 1.2 m (the first spot stood them against a palm's trunk: the
        // landscape's planting has neither footprint nor collider), nearest the forecourt's south-west corner by the
        // station (searched in a fixed order: the same spot every build).
        static Vector3 QuaySpot(Transform root)
        {
            var road=HillcrestRoute.Mainline;var want=new Vector3(62,0,-104);Vector3 best=want;float bd=float.MaxValue;
            var drawn=root.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Where(b=>b.size.x<20&&b.size.z<20&&HillcrestRoute.OnHaltQuay(b.center,4)).ToList();
            for(float x=HillcrestRoute.QuayWest+2;x<HillcrestRoute.QuayEast-2;x+=1.5f)for(float z=HillcrestRoute.QuaySouth+2;z<HillcrestRoute.QuayNorth-2;z+=1.5f) {
                var p=new Vector3(x,0,z);float d=(p-want).sqrMagnitude;if(d>=bd)continue;
                if(road.line.Any(q=>new Vector2(q.x-x,q.z-z).magnitude<road.width*.5f+2))continue;
                if(BayviewKit.Occupied(new Bounds(p+Vector3.up,new Vector3(2.4f,4,2.4f))))continue;
                float gy=Ground.Surface(x,z);var room=new Bounds(new Vector3(x,gy+1.1f,z),new Vector3(2.4f,1.8f,2.4f));
                if(Physics.OverlapBox(room.center,room.extents).Any(c=>!c.isTrigger)||drawn.Any(b=>b.Intersects(room)))continue;
                bd=d;best=p;
            }
            return best;
        }
    }
}
