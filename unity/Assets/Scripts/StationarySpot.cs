using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LittleRoad
{
    // Keeps standing cars (residents, townsfolk, parked cars) out of buildings, props and walls. After a car
    // is placed, its body box (lifted clear of the ground it stands on) is checked against every solid
    // obstacle; if it overlaps, the car slides to the nearest clear spot (up to 14 m away, keeping its
    // heading and standing on the ground there). The car-placement audit (--car-audit) lists anything left.
    public static class StationarySpot
    {
        static readonly Collider[] hits=new Collider[32];
        static bool Obstacle(Collider c,GameObject self)
        {
            if(c==null||c.isTrigger||c.transform.IsChildOf(self.transform))return false;
            if(c.GetComponentInParent<RoadSurface>()!=null||c.GetComponentInParent<RoadSupport>()!=null)return false;
            if(c is MeshCollider)return false;       // ground, roads and terrain chunks
            if(c.GetComponentInParent<RoadCar>()!=null)return false;       // the player
            return true;
        }
        static Bounds Body(GameObject car)
        {
            var rs=car.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;
        }
        // Colliders overlapping the car's body (its local box, raised 0.3 m off the ground, 10 cm inset).
        public static List<Collider> Overlaps(GameObject car,Vector3 at)
        {
            var b=Body(car);var local=car.transform.InverseTransformVector(b.size);
            var half=new Vector3(Mathf.Abs(local.x),Mathf.Abs(local.y),Mathf.Abs(local.z))*.5f-new Vector3(.1f,.15f,.1f);
            var centre=at+Vector3.up*(half.y+.3f);var result=new List<Collider>();
            int n=Physics.OverlapBoxNonAlloc(centre,half,hits,car.transform.rotation,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<n;i++)if(Obstacle(hits[i],car))result.Add(hits[i]);
            return result;
        }
        static float Ground(Vector3 p,GameObject self)
        {
            float best=float.MinValue;
            foreach(var h in Physics.RaycastAll(p+Vector3.up*6,Vector3.down,20))if(!h.collider.isTrigger&&!h.collider.transform.IsChildOf(self.transform)&&!Obstacle(h.collider,self))best=Mathf.Max(best,h.point.y);
            return best==float.MinValue?p.y:best;
        }
        // Moves the car to the nearest clear spot; returns how far it moved (0 when it was already clear).
        public static float Settle(GameObject car)=>Settle(car,null);
        // `allowed` limits where it may go (the Castle Climb keeps its crowd off the route).
        public static float Settle(GameObject car,System.Func<Vector3,bool> allowed)
        {
            Physics.SyncTransforms();var at=car.transform.position;if(Overlaps(car,at).Count==0&&!WorldTiles.Blocked(at,2.2f)&&(allowed==null||allowed(at)))return 0;
            var right=car.transform.right;var fwd=car.transform.forward;
            for(float r=1.5f;r<=14;r+=1.5f)for(int k=0;k<12;k++) {
                float a=k*Mathf.PI*2/12;var p=at+(right*Mathf.Cos(a)+fwd*Mathf.Sin(a))*r;p.y=Ground(p,car)+.03f;
                if(Mathf.Abs(p.y-at.y)>2.5f||allowed!=null&&!allowed(p))continue;
                if(Overlaps(car,p).Count==0&&!WorldTiles.Blocked(p,2.2f)){car.transform.position=p;Physics.SyncTransforms();return r;}
            }
            return -1;
        }
        // Every standing car and what it overlaps.
        public static string Audit()
        {
            Physics.SyncTransforms();var sb=new StringBuilder();int count=0,bad=0;
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) {
                if(!(t.name.StartsWith("Resident ")||t.name=="Parked car"))continue;count++;
                var o=Overlaps(t.gameObject,t.position);if(o.Count==0)continue;bad++;
                sb.AppendLine($"  {t.name} at ({t.position.x:0},{t.position.y:0.0},{t.position.z:0}) overlaps {string.Join(", ",o.Select(c=>(c.transform.parent!=null?c.transform.parent.name+"/":"")+c.name).Distinct().Take(4))}");
            }
            return $"standing cars={count} overlapping={bad}\n"+sb;
        }
    }
}
