using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Resident name tags: only the nearest resident's tag shows (within 16 m of the camera), so hosts
    // standing together (Pit Stop bays) never stack their names, and the one you can talk to is named.
    public sealed class ResidentLabel:MonoBehaviour
    {
        const float Range=16;
        static readonly List<ResidentLabel> all=new();static int chosenFrame=-1;static ResidentLabel chosen;
        Renderer label;Vector3 fullScale;
        void Awake(){label=GetComponent<Renderer>();fullScale=transform.localScale;}
        void OnEnable()=>all.Add(this);
        void OnDisable()=>all.Remove(this);
        static ResidentLabel Nearest(Vector3 eye)
        {
            if(chosenFrame==Time.frameCount)return chosen;chosenFrame=Time.frameCount;chosen=null;float best=Range*Range;
            foreach(var l in all){float d=(l.transform.position-eye).sqrMagnitude;if(d<best){best=d;chosen=l;}}
            return chosen;
        }
        void LateUpdate()
        {
            var cameraView=Camera.main;if(cameraView==null){label.enabled=false;return;}
            Vector3 cameraPosition=cameraView.transform.position;
            float distance=Vector3.Distance(cameraPosition,transform.position);
            label.enabled=Nearest(cameraPosition)==this&&!Physics.Linecast(cameraPosition,transform.position);
            if(!label.enabled)return;
            transform.rotation=cameraView.transform.rotation;
            // World-size text filled the screen from the chase camera beside a resident (map sweep
            // m33). Shrink it up close so it keeps a steady, modest screen size; never larger than authored.
            transform.localScale=fullScale*Mathf.Clamp(distance/16,.3f,1);
        }
    }
}
