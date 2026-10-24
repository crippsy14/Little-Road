using UnityEngine;

namespace LittleRoad
{
    // A field gate that swings open when a car drives into it, away from the car, and drifts shut a few seconds after
    // (Richard's snap, 24 Oct: "gate should open when hit"). The kit's gate (posts and leaf in one 3.2 m piece) hangs from
    // a hinge at one end; it has no collider of its own, so a car pushes it open rather than stopping dead against it.
    // Not a trigger: the game's ray queries hit triggers, so each gate measures the player's car itself (one distance test
    // while it rests). Hung under the kit's animated root, which the districts' static batching leaves alone.
    public sealed class SwingGate:MonoBehaviour
    {
        public const float Width=3.2f;
        Transform car;Rigidbody body;
        Vector3 centre,along,normal;float angle,speed,shut;
        public float Angle=>angle;
        // Hang a spawned field_gate (yaw as placed: its length along local x) on a hinge at its -x end.
        public static SwingGate Hang(GameObject gate)
        {
            if(gate==null)return null;
            foreach(var c in gate.GetComponentsInChildren<Collider>())Object.Destroy(c);
            var t=gate.transform;var hinge=new GameObject("Field gate hinge").transform;hinge.SetParent(BayviewKit.AnimatedRoot);
            hinge.SetPositionAndRotation(t.position-t.right*(Width*.5f-.1f),t.rotation);t.SetParent(hinge,true);
            var swing=hinge.gameObject.AddComponent<SwingGate>();swing.centre=t.position;swing.along=t.right;swing.normal=t.forward;return swing;
        }
        void Update()
        {
            float dt=Time.deltaTime;
            if(car==null){car=Grazer.Car;body=car!=null?car.GetComponent<Rigidbody>():null;}
            if(car!=null) {
                var d=car.position-centre;
                if(d.sqrMagnitude<36) {
                    // In the gateway (within half a gate's width along it, and 2.6 m either side): pushed open away from the car.
                    float side=Vector3.Dot(d,normal),across=Vector3.Dot(d,along);
                    var v=body!=null?body.linearVelocity:Vector3.zero;
                    if(Mathf.Abs(across)<Width*.5f+.6f&&Mathf.Abs(side)<2.6f&&v.sqrMagnitude>.25f) {
                        float away=-Mathf.Sign(side==0?1:side)*95,push=Mathf.Max(90,v.magnitude*45);
                        if(Mathf.Abs(angle)<Mathf.Abs(away)-1||Mathf.Sign(angle)!=Mathf.Sign(away))speed=Mathf.MoveTowards(speed,Mathf.Sign(away)*push,push*8*dt);
                        shut=4;
                    }
                }
            }
            if(shut>0)shut-=dt;
            // Swinging: damped, stopped hard at the hinge's 100 degrees, then a slow closing pull once the car has gone.
            if(shut<=0)speed+=(-angle*1.2f-speed*1.6f)*dt;else speed*=Mathf.Exp(-1.2f*dt);
            angle+=speed*dt;
            if(Mathf.Abs(angle)>100){angle=Mathf.Sign(angle)*100;speed=-speed*.25f;}
            if(shut<=0&&Mathf.Abs(angle)<.3f&&Mathf.Abs(speed)<.5f){angle=0;speed=0;}
            transform.rotation=Quaternion.AngleAxis(angle,Vector3.up)*Quaternion.LookRotation(normal,Vector3.up);
        }
    }
}
