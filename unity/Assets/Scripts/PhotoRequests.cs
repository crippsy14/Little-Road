using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Pip's photo requests (Claude's own addition, 30 Sep; docs/51). Pip makes Bayview's postcards and wants new
    // pictures: six sights across the island and the valley. A photo taken in photo mode counts for every sight that
    // is well framed (the middle 70% of the picture), near enough and not hidden behind something. Each pays once.
    public static class PhotoRequests
    {
        public sealed class Request{public string id,name;public Func<Vector3?> where;public float reach;}
        public static readonly string[] Ids={"photo_lighthouse","photo_halt","photo_castle","photo_falls","photo_train","photo_balloons","photo_galleries","photo_slatequay","photo_blondin","photo_bw_church","photo_bw_plough","photo_bw_pond","photo_bw_dairy","photo_bw_hairpin","photo_bw_beacon","photo_bw_rookery","photo_bw_cider","photo_bw_meadow","photo_bw_hedgegap"};
        public static bool Known(string id)=>Array.IndexOf(Ids,id)>=0;
        public const int Prize=100;
        public static readonly List<Request> All=new();
        public static void Setup(Vector3 lighthouse)
        {
            All.Clear();
            All.Add(new Request{id=Ids[0],name="The lighthouse",where=()=>lighthouse+Vector3.up*8,reach=90});
            All.Add(new Request{id=Ids[1],name="Bayview Halt",where=()=>new Vector3(93,4,-142),reach=90});
            All.Add(new Request{id=Ids[2],name="Hillcrest Castle",where=()=>HillcrestRoute.CastleCourt+Vector3.up*10,reach=520});
            All.Add(new Request{id=Ids[3],name="Silver Falls",where=()=>HillcrestRoute.FallsHead+Vector3.up*1.5f,reach=170   /* the lip, where the water tips over */});
            All.Add(new Request{id=Ids[4],name="Bert's train",where=()=>{var t=ValleyTrain.Instance;return t!=null&&t.Visible&&t.Loco!=null?t.Loco.position+Vector3.up*2:null;},reach=80});
            // (The balloon you're pointing at: nearest the middle of the picture, not nearest the camera.)
            All.Add(new Request{id=Ids[5],name="The balloons",where=()=>{Vector3? best=null;float ba=float.MaxValue;var cam=Camera.main;if(cam==null)return null;
                foreach(var b in UnityEngine.Object.FindObjectsByType<BalloonDrift>(FindObjectsSortMode.None)){var p=b.transform.position+Vector3.up*8;float a=Vector3.Angle(cam.transform.forward,p-cam.transform.position);if(a<ba){ba=a;best=p;}}return best;},reach=170});
            // Quarry Hills (--quarry-hills): the galleries across Glas, Slate Quay's light, the Blondin's floor tower.
            if(QuarryHills.Enabled) {
                All.Add(new Request{id=Ids[6],name="The galleries",where=()=>QuarryHills.PitPoint(QuarryHills.GalleryAxis,QuarryHills.Benches*QuarryHills.BenchWidth*.5f)+Vector3.up*(QuarryHills.FloorY+QuarryHills.Benches*QuarryHills.BenchHeight*.5f),reach=700});
                All.Add(new Request{id=Ids[7],name="Slate Quay",where=()=>QuarryHills.QuayPoint(QuarryHills.QuayHalf-6,-QuarryHills.QuaySea+5,QuarryHills.QuayY+6),reach=160});
                All.Add(new Request{id=Ids[8],name="The Blondin",where=()=>{var b=QuarryFloor.BlondinFloor;return new Vector3(b.x,ValleyField.Height(b.x,b.y)+18,b.y);},reach=220});
            }
            AddBramblewick();
        }
        // Bramblewick (--bramblewick, docs/82): St Michael's tower, The Plough, the duck pond, the dairy. A village built in the
        // background adds them when it arrives (Districts); each sight once.
        public static void AddBramblewick()
        {
            if(BramblewickVillage.Tower!=Vector3.zero&&!All.Exists(r=>r.id==Ids[9])) {
                All.Add(new Request{id=Ids[9],name="St Michael's tower",where=()=>BramblewickVillage.Tower,reach=320});
                All.Add(new Request{id=Ids[10],name="The Plough",where=()=>BramblewickVillage.PubFront,reach=120});
                All.Add(new Request{id=Ids[11],name="The duck pond",where=()=>BramblewickVillage.PondAt,reach=110});
                All.Add(new Request{id=Ids[12],name="Bramblewick Dairy",where=()=>BramblewickVillage.DairyAt,reach=160});
                // (the lanes circuit: the Green Hairpin from its banks, the Beacon crest from the rim)
                if(Bramblewick.Marks.TryGetValue("Green Apex",out var hp))All.Add(new Request{id=Ids[13],name="The Green Hairpin",where=()=>new Vector3(hp.x,ValleyField.Height(hp.x,hp.y)+1.5f,hp.y),reach=120});
                if(Bramblewick.Marks.TryGetValue("Beacon Apex",out var bc))All.Add(new Request{id=Ids[14],name="Beacon crest",where=()=>new Vector3(bc.x,ValleyField.Height(bc.x,bc.y)+1.5f,bc.y),reach=160});
                if(Bramblewick.Marks.TryGetValue("Rookery",out var rk))All.Add(new Request{id=Ids[15],name="The Rookery",where=()=>new Vector3(rk.x,ValleyField.Height(rk.x,rk.y)+1.5f,rk.y),reach=140});
                if(Bramblewick.Marks.TryGetValue("Cider Press",out var cp))All.Add(new Request{id=Ids[16],name="Cider Press corner",where=()=>new Vector3(cp.x,ValleyField.Height(cp.x,cp.y)+1.5f,cp.y),reach=140});
                if(Bramblewick.Marks.TryGetValue("Hedge Brow",out var hg))All.Add(new Request{id=Ids[18],name="Hedge Gap",where=()=>new Vector3(hg.x,ValleyField.Height(hg.x,hg.y)+1.5f,hg.y),reach=140});
                if(Bramblewick.Marks.TryGetValue("Long Meadow Roll",out var lm))All.Add(new Request{id=Ids[17],name="Long Meadow",where=()=>new Vector3(lm.x,ValleyField.Height(lm.x,lm.y)+1.5f,lm.y),reach=160});
            }
        }
        // Nothing solid between: railings, posts and fences (under 0.6 m in their thinnest direction) don't hide a sight.
        static bool Clear(Vector3 from,Vector3 to)
        {
            float d=Vector3.Distance(from,to);var dir=(to-from)/d;
            foreach(var h in Physics.RaycastAll(from,dir,d)){if(h.collider.isTrigger||h.collider.GetComponentInParent<RoadCar>()!=null)continue;var s=h.collider.bounds.size;if(Mathf.Min(s.x,Mathf.Min(s.y,s.z))<.6f)continue;if(d-h.distance>Mathf.Max(8,d*.22f))return false;}
            return true;
        }
        // The requests this camera's picture shows.
        public static List<Request> Seen(Camera cam)
        {
            var seen=new List<Request>();
            foreach(var r in All) {
                var at=r.where();if(at==null)continue;var p=at.Value;float d=Vector3.Distance(cam.transform.position,p);if(d<6||d>r.reach)continue;
                var vp=cam.WorldToViewportPoint(p);if(vp.z<=0||vp.x<.15f||vp.x>.85f||vp.y<.15f||vp.y>.85f)continue;
                // In clear view: some part of it (the point itself, above it or either side) with nothing solid in the way until
                // close to the sight (its own walls count as it). A single point skimmed banks and kerbs the eye sees over.
                var side=Vector3.Cross(Vector3.up,(p-cam.transform.position).normalized)*4;
                foreach(var q in new[]{p,p+Vector3.up*3,p+Vector3.up*6,p+side,p-side})if(Clear(cam.transform.position,q)){seen.Add(r);break;}
            }
            return seen;
        }
    }
}
