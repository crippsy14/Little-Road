using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LittleRoad
{
    public static class TechnicalCircuitWorld
    {
        static GameObject Box(string name,Vector3 position,Vector3 size,Material material,Quaternion rotation,bool solid=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;
            go.transform.SetPositionAndRotation(position,rotation);go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid){go.GetComponent<Collider>().enabled=false;Object.Destroy(go.GetComponent<Collider>());}else go.tag="Respawn";
            return go;
        }
        static void Ribbon(Circuit track,string name,float centre,float width,float height,Material material)
        {
            var go=new GameObject(name);go.AddComponent<MeshFilter>().sharedMesh=track.Ribbon(centre,width,height);go.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        static void Sign(string words,Vector3 point,Quaternion rotation,float size)
        {
            var go=new GameObject(words);go.transform.SetPositionAndRotation(point,rotation);
            var text=go.AddComponent<TextMesh>();text.text=words;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
            text.fontSize=80;text.characterSize=size;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontStyle=FontStyle.Bold;text.color=Color.white;
            go.GetComponent<Renderer>().sharedMaterial=text.font.material;go.AddComponent<WorldText>();
        }
        public static void Create(TechnicalCircuit course)
        {
            var track=course.track;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.62f,.76f,.9f);
            RenderSettings.ambientEquatorColor=new Color(.45f,.51f,.48f);RenderSettings.ambientGroundColor=new Color(.25f,.3f,.25f);
            var ambient=new SphericalHarmonicsL2();ambient.AddAmbientLight(new Color(.25f,.29f,.31f));RenderSettings.ambientProbe=ambient;
            var sun=new GameObject("Technical circuit sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2.0f;sun.color=new Color(1,.94f,.84f);
            sun.transform.rotation=Quaternion.LookRotation(-CoastalSky.Apply());sun.shadows=LightShadows.Soft;RenderSettings.sun=sun;RenderSettings.fog=false;
            var volume=new GameObject("Circuit colour").AddComponent<Volume>();volume.isGlobal=true;volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var grass=TrialWorld.Material("Mown circuit turf",new Color(.28f,.37f,.19f));
            var asphalt=TrialWorld.Material("Cedar tarmac",new Color(.20f,.22f,.24f));
            var runoff=TrialWorld.Material("Pale runoff",new Color(.58f,.55f,.42f));
            var white=TrialWorld.Material("Ivory markings",new Color(.95f,.94f,.85f));
            var red=TrialWorld.Material("Red kerb paint",new Color(.72f,.15f,.10f));
            var teal=TrialWorld.Material("Circuit signs",new Color(.06f,.27f,.26f));
            var amber=TrialWorld.Material("Ordered timing gates",new Color(.96f,.65f,.19f));
            // Cedar intentionally has no elevation. One level support avoids
            // speculative CCD treating coplanar road-mesh seams as side impacts.
            // Rendering keeps its small layering biases; race width, dirt grip,
            // car contacts and all steering/engine forces are unchanged.
            if(track.hasElevation)throw new System.InvalidOperationException("Cedar's planar support requires a level circuit.");
            bool tessellated=RaceTrial.Has("--tessellated-cedar-support");
            var ground=Box("Continuous test ground",new Vector3(-50,-.5f,0),new Vector3(1200,1,1000),grass,Quaternion.identity,tessellated);ground.tag="Untagged";
            Ribbon(track,"Runoff",0,24,.011f,runoff);Ribbon(track,"16 metre asphalt",0,Circuit.Width,.025f,asphalt);
            var support=new GameObject("Continuous drivable support",typeof(RoadSupport));
            if(tessellated)support.AddComponent<MeshCollider>().sharedMesh=track.DrivableSupport();
            else {
                support.transform.position=new Vector3(-50,-.475f,0);
                support.AddComponent<BoxCollider>().size=new Vector3(1200,1,1000);
            }
            foreach(int side in new[]{-1,1}) {
                Ribbon(track,"Road boundary",side*7.7f,.16f,.039f,white);
                for(float d=0;d<track.length;d+=4)if(track.Curvature(d)>.006f)
                    Box("Painted kerb",track.At(d)+track.Right(d)*side*8.35f+Vector3.up*.03f,new Vector3(.85f,.012f,3.8f),((int)(d/4)%2==0)?white:red,Quaternion.LookRotation(track.Forward(d)));
            }
            // These are the actual twelve RoadCar.TrackGates positions, not decorative checkpoints.
            for(int i=0;i<12;i++) {
                float d=track.length*i/12;var p=track.At(d);var rotation=Quaternion.LookRotation(track.Forward(d));
                Box("Gate "+i,p+Vector3.up*.045f,new Vector3(16,.01f,.22f),amber,rotation);
                foreach(int side in new[]{-1,1})Box("Timing post",p+track.Right(d)*side*9.4f+Vector3.up*.65f,new Vector3(.18f,1.3f,.18f),amber,rotation);
                Sign(i==0?"START / FINISH":"GATE "+i,p+track.Right(d)*10+Vector3.up*2,rotation,.065f);
            }
            for(int i=0;i<16;i++)for(int j=0;j<2;j++)Box("Timing checker",track.At(j*.55f)+track.Right(0)*(-7.5f+i)+Vector3.up*.052f,new Vector3(1,.01f,.55f),(i+j)%2==0?white:asphalt,Quaternion.LookRotation(track.Forward(0)));
            foreach(int corner in new[]{1,4,7,11,14}) {
                float entry=course.regionStarts[corner];
                foreach(int metres in new[]{100,50}) {
                    float d=entry-metres;var rotation=Quaternion.LookRotation(track.Forward(d));var p=track.At(d)+track.Right(d)*11+Vector3.up*1.25f;
                    Box("Braking distance board",p,new Vector3(2,1.5f,.12f),teal,rotation);Sign(metres.ToString(),p-track.Forward(d)*.08f,rotation,.13f);
                }
            }
            for(int i=1;i<course.regionStarts.Length;i++) {
                float d=course.regionStarts[i];var rotation=Quaternion.LookRotation(track.Forward(d));var p=track.At(d)+track.Right(d)*12+Vector3.up*2.5f;
                Box("Corner board",p,new Vector3(7,1.5f,.12f),teal,rotation);Sign(course.definition.knots[i].name,p-track.Forward(d)*.08f,rotation,.075f);
            }
            // Small trackside pits establish scale without hiding the driving test in scenery.
            for(int i=0;i<6;i++) {
                var p=track.At(30+i*14)+track.Right(0)*28;
                Box("Pit bay",p+Vector3.up*3,new Vector3(10,6,12),white,Quaternion.LookRotation(track.Forward(0)),true);
                Box("Pit bay shutter",p-track.Right(0)*5.03f+Vector3.up*2.3f,new Vector3(.08f,4,9),teal,Quaternion.LookRotation(track.Forward(0)));
            }
        }
    }
}
