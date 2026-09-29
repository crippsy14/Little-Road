using UnityEngine;

namespace LittleRoad
{
    // At night a moving car's tail lamps leave a faint, short red streak (about 0.06 s), a light cue for
    // speed that doesn't blur the car itself. Two trails at the rear lamp corners of the collision box;
    // only after dark and above ~22 mph. (Headlamp trails and a longer streak read as too much: Richard.)
    public sealed class LightTrails:MonoBehaviour
    {
        const float Time_=.06f,MinSpeed=10;
        static Material tail;
        TrailRenderer[] trails;RoadCar car;
        public void Setup(RoadCar owner,Vector3 centre,Vector3 half)
        {
            car=owner;var shader=Shader.Find("LittleRoad/LightTrail");if(shader==null)return;
            tail??=new Material(shader){name="Tail light trail"};tail.SetColor("_Tint",new Color(1.1f,.07f,.03f,1));
            trails=new TrailRenderer[2];int k=0;
            const int end=-1;
            foreach(int side in new[]{-1,1}) {
                var go=new GameObject("Tail lamp trail");go.transform.SetParent(transform,false);
                go.transform.localPosition=centre+new Vector3(side*half.x*.62f,.02f,end*(half.z-.05f));
                var t=go.AddComponent<TrailRenderer>();t.time=Time_;t.minVertexDistance=.25f;t.emitting=false;t.sharedMaterial=tail;
                t.widthCurve=new AnimationCurve(new Keyframe(0,.07f),new Keyframe(1,0));t.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;t.receiveShadows=false;
                var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.4f,0),new GradientAlphaKey(0,1)});t.colorGradient=g;
                trails[k++]=t;
            }
        }
        void LateUpdate()
        {
            if(trails==null||car==null)return;
            bool on=NightLights.Night>.5f&&Mathf.Abs(car.speed)>MinSpeed&&!car.controlsBlocked;
            foreach(var t in trails)if(t!=null&&t.emitting!=on)t.emitting=on;
        }
    }
}
