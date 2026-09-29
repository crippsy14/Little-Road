using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Weight transfer you can see: the body rolls out of corners, dives under braking, squats under power
    // and bobs over bumps, on springs, while the wheels stay planted. Visual only: the collider, the
    // Rigidbody and every physics result are untouched (the body meshes move inside the car's transform).
    // Wheel pivots are lifted out of the body visuals into their own holder so they don't lean with it.
    public sealed class BodyMotion:MonoBehaviour
    {
        // Degrees per m/s² of cornering and fore/aft acceleration, and the limits (a toy's generous lean).
        public const float RollPerAccel=.55f,PitchPerAccel=.42f,MaxRoll=5f,MaxPitch=4f,Stiffness=90f,Damping=11f;
        static readonly Vector3 Pivot=new(0,.35f,0);   // roll/pitch centre, in the car's space
        Rigidbody body;Transform wheelSet;
        readonly List<(Transform shell,Vector3 position,Quaternion rotation)> shells=new();
        Vector3 lastVelocity,accel;bool haveVelocity;
        float roll,rollVelocity,pitch,pitchVelocity,heave,heaveVelocity;
        public float Roll=>roll;public float Pitch=>pitch;
        public static bool Enabled=>!RaceTrial.Has("--no-body-motion");

        // (Re)collects the body visuals (call again after a body swap, before WheelVisuals is built).
        public void Collect(Rigidbody rigidbody)
        {
            body=rigidbody;shells.Clear();
            if(wheelSet==null){wheelSet=new GameObject("Wheel set").transform;wheelSet.SetParent(transform,false);}
            // Wheels from a previous body left with it.
            for(int i=wheelSet.childCount-1;i>=0;i--){var w=wheelSet.GetChild(i);w.SetParent(null);Destroy(w.gameObject);}
            foreach(Transform child in transform) {
                if(child==wheelSet||child.GetComponentInChildren<Renderer>()==null)continue;
                var wheels=new List<Transform>();
                foreach(var t in child.GetComponentsInChildren<Transform>(true))if(t.name.StartsWith("Wheel_"))wheels.Add(t);
                foreach(var w in wheels)w.SetParent(wheelSet,true);
                shells.Add((child,child.localPosition,child.localRotation));
            }
            roll=pitch=heave=rollVelocity=pitchVelocity=heaveVelocity=0;haveVelocity=false;
        }
        void FixedUpdate()
        {
            if(body==null)return;
            var v=body.isKinematic?Vector3.zero:body.linearVelocity;
            if(haveVelocity){var a=transform.InverseTransformDirection((v-lastVelocity)/Time.fixedDeltaTime);accel=Vector3.Lerp(accel,a,.35f);}
            lastVelocity=v;haveVelocity=!body.isKinematic;
        }
        void LateUpdate()
        {
            if(body==null||shells.Count==0||!Enabled)return;
            float dt=Mathf.Min(Time.deltaTime,.05f);bool carried=body.isKinematic;
            float targetRoll=carried?0:Mathf.Clamp(accel.x*RollPerAccel,-MaxRoll,MaxRoll);
            float targetPitch=carried?0:Mathf.Clamp(-accel.z*PitchPerAccel,-MaxPitch,MaxPitch);
            float targetHeave=carried?0:Mathf.Clamp(-(accel.y)*.004f,-.06f,.06f);
            Spring(ref roll,ref rollVelocity,targetRoll,dt);Spring(ref pitch,ref pitchVelocity,targetPitch,dt);Spring(ref heave,ref heaveVelocity,targetHeave,dt);
            var lean=Quaternion.AngleAxis(roll,Vector3.forward)*Quaternion.AngleAxis(pitch,Vector3.right);
            foreach(var (shell,position,rotation) in shells) {
                if(shell==null)continue;
                shell.localRotation=lean*rotation;shell.localPosition=Pivot+lean*(position-Pivot)+Vector3.up*heave;
            }
        }
        static void Spring(ref float x,ref float velocity,float target,float dt)
        {
            velocity+=((target-x)*Stiffness-velocity*Damping)*dt;x+=velocity*dt;
        }
    }
}
