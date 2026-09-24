using UnityEngine;

namespace LittleRoad
{
    // Samples real collision geometry and requests alignment torque. It never
    // sets the car's position, velocity or orientation to a route/ground pose.
    public sealed class RoadGrounding
    {
        readonly RoadCar car;readonly BoxCollider shape;
        readonly RaycastHit[] hits=new RaycastHit[16];
        public Vector3 normal=Vector3.up,point,forward,right;
        public float clearance,alignmentDegrees;
        public bool grounded,hasSupport;
        public int supportHits,querySaturations;
        public RoadGrounding(RoadCar owner){car=owner;shape=owner.GetComponent<BoxCollider>();}
        public void Sample()
        {
            Vector3 normals=Vector3.zero,points=Vector3.zero;supportHits=0;
            // Four footprint samples avoid reacting to one triangle seam.
            for(int axle=-1;axle<=1;axle+=2)for(int side=-1;side<=1;side+=2) {
                Vector3 origin=car.body.position+car.transform.forward*(axle*1.05f)+car.transform.right*(side*.75f)+Vector3.up*1.2f;
                int count=Physics.RaycastNonAlloc(origin,Vector3.down,hits,4,~0,QueryTriggerInteraction.Ignore);
                if(count==hits.Length)querySaturations++;
                float nearest=float.MaxValue;int chosen=-1;
                for(int i=0;i<count;i++)if(hits[i].collider.GetComponent<RoadSupport>()!=null&&hits[i].normal.y>.5f&&hits[i].distance<nearest){nearest=hits[i].distance;chosen=i;}
                if(chosen>=0){normals+=hits[chosen].normal;points+=hits[chosen].point;supportHits++;}
            }
            hasSupport=supportHits>=2;
            if(hasSupport){normal=normals.normalized;point=points/supportHits;}
            forward=Vector3.ProjectOnPlane(car.transform.forward,normal).normalized;
            right=Vector3.Cross(normal,forward).normalized;
            Vector3 e=shape.size*.5f;
            float half=Mathf.Abs(Vector3.Dot(car.transform.right,normal))*e.x+Mathf.Abs(Vector3.Dot(car.transform.up,normal))*e.y+Mathf.Abs(Vector3.Dot(car.transform.forward,normal))*e.z;
            clearance=hasSupport?Vector3.Dot(shape.transform.TransformPoint(shape.center)-point,normal)-half:float.PositiveInfinity;
            grounded=hasSupport&&clearance<.12f&&clearance>-.20f;
            alignmentDegrees=Vector3.Angle(car.transform.up,normal);
        }
        public void Align()
        {
            if(!grounded)return;
            Vector3 tiltVelocity=Vector3.ProjectOnPlane(car.body.angularVelocity,normal);
            car.body.AddTorque(Vector3.ClampMagnitude(Vector3.Cross(car.transform.up,normal)*80-tiltVelocity*14,40),ForceMode.Acceleration);
        }
    }
}
