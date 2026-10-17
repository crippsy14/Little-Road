using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Each car owns its converted materials; all of its LODs share those same
    // instances. Rear lenses are separate from headlights and passive reflectors.
    public sealed class VehicleLights:MonoBehaviour
    {
        readonly List<Material> brake=new(),reverse=new();
        RoadCar driver;
        bool initialized,collected;
        public bool Braking {get;private set;}
        public bool Reversing {get;private set;}
        public int BrakeMaterialCount=>brake.Count;
        public int ReverseMaterialCount=>reverse.Count;
        void Awake()=>Initialize();
        public void Initialize()
        {
            if(collected)return;collected=true;
            var seen=new HashSet<Material>();
            foreach(var renderer in GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials) {
                if(material==null||!seen.Add(material))continue;
                if(material.name.Contains("TailLight"))brake.Add(material);
                if(material.name.Contains("ReverseLight"))reverse.Add(material);
            }
            SetState(false,false);
        }
        // After a garage body swap: collect the new model's lamp materials.
        public void Recollect(){collected=false;initialized=false;brake.Clear();reverse.Clear();Initialize();}
        void LateUpdate()
        {
            if(driver==null)driver=GetComponent<RoadCar>();
            if(driver==null)return; // Parked residents and explicit gallery poses.
            if(driver.race==null||!driver.race.running||driver.controlsBlocked||driver.race.elapsed<0){SetState(false,false);return;}
            ApplyDrive(driver.speed,driver.throttle);
        }
        public void ApplyDrive(float signedSpeed,float command)
        {
            // Match the existing shared brake/reverse pedal: negative command
            // brakes forward motion, then engages reverse below 0.1 m/s.
            // Forward input while backing also decelerates the car.
            bool braking=command<-.05f&&signedSpeed>.1f||command>.05f&&signedSpeed<-.1f;
            bool reversing=signedSpeed<-.1f||command<-.05f&&signedSpeed<=.1f;
            SetState(braking,reversing);
        }
        public void SetState(bool braking,bool reversing)
        {
            if(initialized&&Braking==braking&&Reversing==reversing)return;
            initialized=true;
            Braking=braking;Reversing=reversing;
            foreach(var material in brake) {
                // (a Car Forge tail lens is tinted glass: it keeps its see-through tint, brightening under braking)
                var off=material.name.Contains("LampGlass")?TrialWorld.LampGlassColour(material.name):new Color(.42f,.015f,.008f);
                material.SetColor("_BaseColor",braking?new Color(.95f,.018f,.008f,off.a):off);
                material.SetColor("_EmissionColor",braking?new Color(2.5f,.006f,.002f):Color.black);
            }
            foreach(var material in reverse) {
                material.SetColor("_BaseColor",reversing?Color.white:new Color(.65f,.70f,.72f));
                material.SetColor("_EmissionColor",reversing?new Color(2.4f,2.4f,2.4f):Color.black);
            }
        }
        void OnDisable()=>SetState(false,false);
    }
}
