using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace LittleRoad
{
    // --field-lineup (with --trial-captures <folder>): every car body actually entered into the class C races and into the
    // class B races, each class as one labelled studio line-up image (lineup-class-c.png, lineup-class-b.png), plus
    // lineup.txt listing them. The fields are built exactly as the game builds them (RaceRoster, several race serials so
    // every body that can be dealt shows up); the player's own car is left out. Richard, 14 Oct: to see which models race
    // in which class.
    public sealed class FieldLineup:MonoBehaviour
    {
        sealed class Entry{public string body;public Color paint;public int count;public readonly SortedSet<string> races=new();}
        IEnumerator Start()
        {
            string folder=RaceTrial.Arg("--trial-captures",Path.Combine(Application.persistentDataPath,"Lineup"));Directory.CreateDirectory(folder);
            var c=new Dictionary<string,Entry>();var b=new Dictionary<string,Entry>();
            void Add(Dictionary<string,Entry> into,RaceRoster roster,string race)
            {
                foreach(var e in roster.Entries){if(e.Id==0)continue;
                    if(!into.TryGetValue(e.Body,out var x))into[e.Body]=x=new Entry{body=e.Body,paint=e.Paint};x.count++;x.races.Add(race);}
            }
            for(int serial=0;serial<8;serial++) {
                var save=AdventureProgress.New();save.raceSerial=serial;
                Add(c,RaceRoster.Adventure(save),"Bayview");Add(c,RaceRoster.AdventureHillcrest(save),"Hillcrest");Add(c,RaceRoster.AdventureQuarry(save),"Quarry");
                foreach(var ev in RaceCalendar.Every.Where(e=>e.raceClass=="B"))Add(b,RaceRoster.AdventureTuner(save,ev),ev.venue);
            }
            Studio();
            var text=new System.Text.StringBuilder();
            // --forge-lineup: the Car Forge bodies instead (docs/75), each in a paint of its own.
            if(RaceTrial.Has("--forge-lineup")) {
                var forge=new Dictionary<string,Entry>();var paints=new[]{new Color(.55f,.72f,.70f),new Color(.80f,.22f,.12f),new Color(.92f,.72f,.10f),new Color(.2f,.4f,.8f)};int k=0;
                // (--forge-only a,b: just those Forge bodies, for a close comparison)
                var only=RaceTrial.Arg("--forge-only","").Split(',',System.StringSplitOptions.RemoveEmptyEntries);
                foreach(var id in CarBodies.Forge)if(only.Length==0||System.Array.IndexOf(only,id)>=0)forge[id]=new Entry{body=id,paint=paints[k++%paints.Length],count=1};
                // --forge-compare bramble,dover,...: legacy bodies beside them, the toy benchmark (docs/75 §10 stage A2).
                foreach(var id in RaceTrial.Arg("--forge-compare","").Split(',',System.StringSplitOptions.RemoveEmptyEntries))forge[id]=new Entry{body=id,paint=paints[k++%paints.Length],count=1};
                c=forge;b=new Dictionary<string,Entry>();
            }
            foreach(var (name,set) in new[]{("class-c",c),("class-b",b)}) {
                if(set.Count==0)continue;
                var list=set.Values.OrderBy(e=>Order(e.body)).ThenBy(e=>e.body).ToList();
                text.AppendLine($"{name}: {list.Count} bodies");foreach(var e in list)text.AppendLine($"  {e.body,-12} {CarBodies.Name(e.body),-24} entries {e.count,4}  races: {string.Join(", ",e.races)}");
                yield return Shoot(list,Path.Combine(folder,$"lineup-{name}.png"),name=="class-c"?"CLASS C FIELDS":"CLASS B FIELDS");
            }
            File.WriteAllText(Path.Combine(folder,"lineup.txt"),text.ToString());
            Debug.Log("LITTLE_ROAD_FIELD_LINEUP done "+folder+"\n"+text);Application.Quit(0);
        }
        // Family cars first, then sporty, flagship, off-road; Tuners, then their off-roaders.
        static int Order(string id)=>System.Array.IndexOf(CarBodies.Family,id)>=0?0:System.Array.IndexOf(CarBodies.Sporty,id)>=0?1:id==CarBodies.Flagship?2:
            System.Array.IndexOf(CarBodies.OffRoad,id)>=0?3:System.Array.IndexOf(CarBodies.TunerOffRoad,id)>=0?5:4;

        Camera view;Transform stage;Font font;
        void Studio()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.63f,.70f,.78f);
            RenderSettings.ambientEquatorColor=new Color(.54f,.55f,.51f);RenderSettings.ambientGroundColor=new Color(.28f,.27f,.23f);
            var sun=new GameObject("Lineup sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.7f;sun.color=new Color(1,.94f,.84f);
            sun.transform.rotation=Quaternion.Euler(50,-30,0);sun.shadows=LightShadows.Soft;
            var fill=new GameObject("Lineup fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.45f;fill.color=new Color(.72f,.84f,1);fill.transform.rotation=Quaternion.Euler(25,150,0);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.05f,0);floor.transform.localScale=new Vector3(400,.1f,400);
            floor.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material("Lineup floor",new Color(.80f,.78f,.72f));
            var volume=new GameObject("Lineup tone").AddComponent<Volume>();volume.isGlobal=true;volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            view=new GameObject("Lineup camera").AddComponent<Camera>();view.tag="MainCamera";view.orthographic=true;
            view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.86f,.84f,.79f);view.nearClipPlane=.1f;view.farClipPlane=600;
            var data=view.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        IEnumerator Shoot(List<Entry> list,string path,string title)
        {
            if(stage!=null)Destroy(stage.gameObject);stage=new GameObject("Lineup stage").transform;
            // A straight grid, first row at the top, the camera square on to it from the front; each car turned three-quarters.
            int rows=list.Count<=6?1:4;int columns=Mathf.CeilToInt(list.Count/(float)rows);const float dx=6.2f,dz=7.4f,pitch=32;
            var canvas=new GameObject("Labels",typeof(Canvas)).GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.transform.SetParent(stage);
            var camRot=Quaternion.Euler(pitch,0,0);
            for(int i=0;i<list.Count;i++) {
                var e=list[i];int col=i%columns,row=i/columns;var at=new Vector3((col-(columns-1)*.5f)*dx,0,((rows-1)*.5f-row)*dz);
                var car=TrialWorld.Car(e.body,e.paint);car.transform.SetParent(stage);car.transform.SetPositionAndRotation(at,Quaternion.Euler(0,215,0));
                foreach(var lod in car.GetComponentsInChildren<LODGroup>())lod.ForceLOD(0);
                Label(canvas.transform,CarBodies.Name(e.body),at+new Vector3(0,.55f,-2.75f),camRot,1,40,true);
                Label(canvas.transform,$"{e.body} · {e.count} entries · {Where(e)}",at+new Vector3(0,.12f,-3.35f),camRot,1,27,false);
            }
            Label(canvas.transform,$"{title}  ·  {list.Count} car models actually entered (from the game's own race fields)",new Vector3(0,1.2f,(rows-1)*.5f*dz+4.6f),camRot,1.5f,40,true);
            float wide=columns*dx+1,deep=rows*dz+6;
            view.transform.rotation=camRot;view.transform.position=new Vector3(0,0,-.6f)-(camRot*Vector3.forward)*200;
            view.orthographicSize=Mathf.Max(deep*Mathf.Sin(pitch*Mathf.Deg2Rad)*.5f+1.4f,wide/(2*16f/9f));
            yield return null;yield return null;yield return new WaitForSeconds(.3f);
            Toy.Capture(path,2880,1620);
        }
        // Which races, briefly: the C venues by name; the B events as tarmac and/or rallycross.
        static string Where(Entry e)
        {
            if(e.races.All(r=>r=="Bayview"||r=="Hillcrest"||r=="Quarry"))return string.Join(", ",e.races);
            bool gravel=e.races.Any(r=>r.Contains("Rallycross")),tarmac=e.races.Any(r=>!r.Contains("Rallycross"));
            return tarmac&&gravel?"tarmac + rallycross":gravel?"rallycross only":"tarmac only";
        }
        void Label(Transform parent,string text,Vector3 at,Quaternion facing,float scale,int size,bool bold)
        {
            var go=new GameObject("Label",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var t=go.GetComponent<Text>();t.font=font;t.fontSize=size;t.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;t.text=text;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(.12f,.13f,.16f);
            t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;
            var rt=(RectTransform)go.transform;rt.sizeDelta=new Vector2(600,80);rt.position=at;rt.rotation=facing;rt.localScale=Vector3.one*.0105f*scale;
        }
    }
}
