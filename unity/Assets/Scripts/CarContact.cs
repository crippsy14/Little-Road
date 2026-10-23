using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LittleRoad
{
    // Cars on the ground (Richard, 2 Oct: "some models seem to float still"). Every car model has four wheel pivots
    // (Wheel_FL/FR/RL/RR); a wheel's contact patch is its pivot less the rolling radius along the car's up. Standing
    // cars (residents, parked cars, the Halt's taxis, the farmer) were placed upright, so on a slope one or two wheels
    // hung in the air. Seat() lowers and tilts each standing car onto its four wheels' ground (up to 18 degrees);
    // Audit() measures every car with wheels, standing or moving, for the area audit (docs/57).
    public static class CarContact
    {
        public const float FloatGap=.06f,WheelOffGap=.15f,SunkGap=.12f,MaxTilt=18;
        // Ground under p: the highest solid surface below it that isn't this car or another car.
        static float Ground(Vector3 p,GameObject car)
        {
            float best=float.MinValue;
            foreach(var h in Physics.RaycastAll(p+Vector3.up*1.5f,Vector3.down,6)) {
                if(h.collider.isTrigger||h.collider.transform.IsChildOf(car.transform)||h.collider.GetComponentInParent<RoadCar>()!=null)continue;
                if(h.collider.CompareTag("Respawn")&&h.collider.GetComponentInParent<NpcChatter>()!=null)continue; // another standing car
                best=Mathf.Max(best,h.point.y);
            }
            return best;
        }
        static Transform[] Wheels(GameObject car)=>car.GetComponentsInChildren<Transform>(true).Where(t=>WheelVisuals.IsPivot(t.name)).ToArray();
        // Each wheel's contact point and the gap to the ground under it (positive: in the air; negative: sunk).
        public static List<(Vector3 contact,float gap)> Gaps(GameObject car)
        {
            var list=new List<(Vector3,float)>();var wheels=Wheels(car);if(wheels.Length<3)return list;
            float radius=new WheelVisuals(car).Radius;
            foreach(var w in wheels){var c=w.position-car.transform.up*radius;float g=Ground(c,car);if(g>float.MinValue)list.Add((c,c.y-g));}
            return list;
        }
        public static bool IsStanding(GameObject car)
        {
            var body=car.GetComponent<Rigidbody>();if(body!=null&&!body.isKinematic)return false;
            return car.GetComponent<RoadCar>()==null;
        }
        // Lower and tilt a standing car so its four wheels sit on the ground under them.
        public static bool Seat(GameObject car)
        {
            var wheels=Wheels(car);if(wheels.Length<4)return false;float radius=new WheelVisuals(car).Radius;
            Physics.SyncTransforms();
            var t=car.transform;var fwd=t.forward;fwd.y=0;if(fwd.sqrMagnitude<1e-4f)return false;fwd.Normalize();
            // Ground under each wheel's footprint with the car level: fit a plane (front/back and left/right slopes).
            var level=Quaternion.LookRotation(fwd,Vector3.up);var pts=new List<Vector3>();
            foreach(var w in wheels){var local=t.InverseTransformPoint(w.position);local.y=0;var p=t.position+level*local;float g=Ground(p,car);if(g==float.MinValue)return false;p.y=g;pts.Add(p);}
            Vector3 centre=pts.Aggregate(Vector3.zero,(a,b)=>a+b)/pts.Count;
            // Front pair vs rear pair (pitch), left pair vs right pair (roll), from the wheels' own positions.
            var right=Vector3.Cross(Vector3.up,fwd);
            var front=pts.Where(p=>Vector3.Dot(p-centre,fwd)>0).ToList();var rear=pts.Where(p=>Vector3.Dot(p-centre,fwd)<=0).ToList();
            var leftSide=pts.Where(p=>Vector3.Dot(p-centre,right)<0).ToList();var rightSide=pts.Where(p=>Vector3.Dot(p-centre,right)>=0).ToList();
            if(front.Count==0||rear.Count==0||leftSide.Count==0||rightSide.Count==0)return false;
            Vector3 F=Avg(front),R=Avg(rear),L=Avg(leftSide),Rt=Avg(rightSide);
            var along=(F-R).normalized;var across=(Rt-L).normalized;var up=Vector3.Cross(along,across).normalized;if(up.y<0)up=-up;
            if(Vector3.Angle(up,Vector3.up)>MaxTilt)up=Vector3.RotateTowards(Vector3.up,up,MaxTilt*Mathf.Deg2Rad,0);
            t.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(fwd,up).normalized,up);
            // Then drop or raise it so the lowest-sitting wheel touches (no wheel sinks; the rest sit within a few cm).
            Physics.SyncTransforms();float lift=float.MaxValue;
            foreach(var w in wheels){var c=w.position-t.up*radius;float g=Ground(c,car);if(g>float.MinValue)lift=Mathf.Min(lift,c.y-g);}
            if(lift==float.MaxValue)return false;
            t.position-=Vector3.up*(lift-.01f);Physics.SyncTransforms();
            // On ground twisted under it (a plane through four wheels can't touch all four), one wheel can stay well up: sit
            // the car a little deeper, as on soft grass, sharing the gap out between the high wheel and the others, within
            // the sink the audit allows. (Docs/80: a resident at a Quarry Hills home kept a wheel 0.25 m up.)
            var gaps=Gaps(car);
            if(gaps.Count>0){float hi=gaps.Max(g=>g.gap),lo=gaps.Min(g=>g.gap);if(hi>WheelOffGap){float down=Mathf.Min(hi-(WheelOffGap-.02f),lo+SunkGap-.02f);if(down>0){t.position-=Vector3.up*down;Physics.SyncTransforms();}}}
            return true;
        }
        static Vector3 Avg(List<Vector3> v)=>v.Aggregate(Vector3.zero,(a,b)=>a+b)/v.Count;
        // Every standing car in the scene (residents, parked cars, taxis): seat them all. Returns how many moved.
        // A standing car on ground too steep to sit on (a wheel still well off after seating): try nearby spots, up to
        // 6 m away, and seat it on the flattest that's clear.
        static void Relocate(GameObject car)
        {
            var t=car.transform;var start=t.position;var startRot=t.rotation;float best=MaxGap(car);var bestPos=start;var bestRot=startRot;
            for(float r=1.5f;r<=6;r+=1.5f)for(int k=0;k<8;k++) {
                float a=k*Mathf.PI/4;var p=start+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*r;
                t.SetPositionAndRotation(p,Quaternion.Euler(0,startRot.eulerAngles.y,0));Physics.SyncTransforms();
                if(!Seat(car)||StationarySpot.Overlaps(car,t.position).Count>0)continue;
                float gap=MaxGap(car);if(gap<best){best=gap;bestPos=t.position;bestRot=t.rotation;}
            }
            t.SetPositionAndRotation(bestPos,bestRot);Physics.SyncTransforms();
        }
        static float MaxGap(GameObject car){var g=Gaps(car);return g.Count==0?float.MaxValue:g.Max(x=>x.gap);}
        public static int SeatAll()
        {
            int seated=0;
            // Cars built into the landscape are static-batched: their visuals can't move any more, so they're seated before
            // batching (CoastalLandscape) and skipped here.
            foreach(var car in Cars()) {
                if(!IsStanding(car)||car.GetComponentsInChildren<Renderer>().Any(r=>r.isPartOfStaticBatch))continue;
                if(Seat(car)){seated++;if(MaxGap(car)>WheelOffGap)Relocate(car);}
            }
            Debug.Log("LITTLE_ROAD_CARS_SEATED "+seated);return seated;
        }
        // A background district's own standing cars (Districts): the rest of the world's were seated when they were made,
        // and seating them again moves each a little (the lowest wheel's centimetre, the sink on twisted ground), 23 Oct.
        public static int SeatUnder(Transform holder)
        {
            int seated=0;
            foreach(var car in Cars()) {
                if(!car.transform.IsChildOf(holder)||!IsStanding(car)||car.GetComponentsInChildren<Renderer>().Any(r=>r.isPartOfStaticBatch))continue;
                if(Seat(car)){seated++;if(MaxGap(car)>WheelOffGap)Relocate(car);}
            }
            Debug.Log($"LITTLE_ROAD_CARS_SEATED {seated} under {holder.name}");return seated;
        }
        public static IEnumerable<GameObject> Cars()
        {
            var seen=new HashSet<GameObject>();
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) {
                if(t.name!="Wheel_FL")continue;
                // The car's root: car models (TrialWorld.Car) carry their LODGroup there.
                var lod=t.GetComponentInParent<LODGroup>(true);if(lod==null)continue;var car=lod.transform;
                if(seen.Add(car.gameObject)&&car.gameObject.activeInHierarchy)yield return car.gameObject;
            }
        }
        // The audit: every car with wheels, and those not on the ground (all wheels up, one wheel well off, or sunk).
        public static readonly List<Vector3> Floating=new();
        public static string Audit(out int checkedCars)
        {
            Physics.SyncTransforms();Floating.Clear();checkedCars=0;var sb=new StringBuilder();
            foreach(var car in Cars()) {
                var gaps=Gaps(car);if(gaps.Count<3)continue;checkedCars++;
                float min=gaps.Min(g=>g.gap),max=gaps.Max(g=>g.gap);
                string issue=min>FloatGap?"floating":max>WheelOffGap?"a wheel off the ground":min< -SunkGap?"sunk":null;
                if(issue==null)continue;
                Floating.Add(car.transform.position);
                sb.AppendLine($"  {car.name} at ({car.transform.position.x:0},{car.transform.position.y:0.0},{car.transform.position.z:0}) {issue}: wheel gaps {string.Join(" ",gaps.Select(g=>g.gap.ToString("0.00")))} m{(IsStanding(car)?"":" (moving)")}");
            }
            return $"cars checked={checkedCars} not on the ground={Floating.Count}\n"+sb;
        }
    }
}
