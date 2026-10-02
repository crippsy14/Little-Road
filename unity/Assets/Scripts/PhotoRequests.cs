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
        public static readonly string[] Ids={"photo_lighthouse","photo_halt","photo_castle","photo_falls","photo_train","photo_balloons"};
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
