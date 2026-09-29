using UnityEngine;

namespace LittleRoad
{
    // Brings a parked resident car to life: a slow idle sway on its springs, a turn of the nose
    // towards the player when they pull up close, and a little hop with a flash of the brake
    // lights when it speaks. Only the body moves (an "Idle pivot" holding the visual); the
    // collider stays put.
    public sealed class ResidentIdle:MonoBehaviour
    {
        public static Transform Player;
        public bool cheering;   // Castle Climb crowd: bounces and flashes its lights
        Transform pivot;NpcChatter chatter;VehicleLights lights;float phase,turn,lightsOff;
        void Start()
        {
            pivot=new GameObject("Idle pivot").transform;pivot.SetParent(transform,false);
            for(int i=transform.childCount-1;i>=0;i--){var c=transform.GetChild(i);if(c!=pivot)c.SetParent(pivot,true);}
            chatter=GetComponent<NpcChatter>();lights=GetComponent<VehicleLights>();phase=Random.value*10;
        }
        void Update()
        {
            if(pivot==null)return;float t=Time.time;
            // Parked cars stay parked: they used to swivel up to 40° to face you as you passed, which read as
            // the whole street staring. Only the gentle idle breathing and the hop after speaking remain.
            turn=0;
            // Hop after speaking.
            float since=chatter!=null?t-chatter.LastSaid:99,hop=since<.55f?Mathf.Sin(since/.55f*Mathf.PI):0;
            if(cheering){float c=Mathf.Repeat(t*.8f+phase,1);if(c<.4f)hop=Mathf.Max(hop,Mathf.Abs(Mathf.Sin(c/.4f*Mathf.PI*2))*.8f);if(c<.05f)since=0;}
            pivot.localPosition=new Vector3(0,Mathf.Sin(t*1.3f+phase)*.012f+hop*.16f,0);
            pivot.localRotation=Quaternion.Euler(-hop*4+Mathf.Sin(t*.9f+phase)*.35f,turn,Mathf.Sin(t*1.1f+phase*1.7f)*.5f);
            if(lights!=null) {
                if(since<.6f){lights.SetState(true,false);lightsOff=.6f;}
                else if(lightsOff>0){lightsOff-=Time.deltaTime;if(lightsOff<=0)lights.SetState(false,false);}
            }
        }
    }
}
