using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LittleRoad
{
    // The run test range (docs/77, step 1; the architecture review 7.3): a fixture strip with every hard case a run
    // meets, its own analytic ground and named roads, so a run's checks and captures are the same every time and
    // independent of any town. --run-range opens it to look round; --run-range-review captures it into --trial-captures
    // and quits. The range is the Ground provider while it runs: roads first (flat across, following the land along),
    // then the land, which blends into each road over 4 m.
    //   straight     a level road along the south, with a T-junction (spur) and a drive opening (drive)
    //   crest-hollow a road over a crest (+3 m) and through a hollow (-2.5 m)
    //   bank-road    a road cut along a 30% cross-slope
    //   bends        a road through 40 m, 15 m and 8 m bends
    public sealed class RunRange:MonoBehaviour,Ground.IProvider
    {
        public static bool Requested=>RaceTrial.Has("--run-range")||RaceTrial.Has("--run-range-review");
        public sealed class RangeRoad{public string name;public Vector3[] line;public float width;}
        public static readonly List<RangeRoad> Roads=new();
        public const float Half=130,Cell=1;   // (1 m: at 2 m the land showed through the road where the bends climb the bank)
        Camera view;

        // The land: flat, with a crest across z=60, a hollow across z=0 (both west of x=-40 only) and a 30% bank
        // rising east between x=40 and x=70.
        public static float Land(float x,float z)
        {
            float w=Mathf.Clamp01((-30-x)/20);
            float crest=3f*Mathf.Exp(-Sq((z-60)/14))*w,hollow=-2.5f*Mathf.Exp(-Sq(z/12))*w;
            // (the bank's edges rounded over 3 m either side, as real ground is: a hard kink there put a wall's top 3 deg off
            //  the ground's slope for half a metre, a fixture artefact, 18 Oct)
            float bank=.3f*(Ramp(x-40)-Ramp(x-70));
            return crest+hollow+bank;
        }
        static float Ramp(float t){const float b=3;return t<=-b?0:t>=b?t:(t+b)*(t+b)/(4*b);}
        static float Sq(float v)=>v*v;

        public static void BuildRoads()
        {
            Roads.Clear();
            Add("straight",Line(new Vector2(-120,-100),new Vector2(120,-100)),7f);
            Add("spur",Line(new Vector2(0,-100),new Vector2(0,-55)),6f);
            // (the drive on level ground: on the bank its flat surface stood proud of the sloping road it joins, a hump
            //  no real drive has, 18 Oct)
            Add("drive",Line(new Vector2(25,-100),new Vector2(25,-86)),3f);
            Add("crest-hollow",Line(new Vector2(-90,-60),new Vector2(-90,120)),6.5f);
            Add("bank-road",Line(new Vector2(55,-70),new Vector2(55,40)),6.5f);
            // (a turtle: east, a 40 m bend left, a 15 m bend right, an 8 m bend left, with straights between)
            var pts=new List<Vector2>();var p=new Vector2(-20,20);float heading=0;pts.Add(p);
            void Straight(float d){for(float s=2;s<=d;s+=2){pts.Add(p+new Vector2(Mathf.Cos(heading),Mathf.Sin(heading))*s);}p=pts[^1];}
            void Arc(float r,float turn){int n=Mathf.CeilToInt(Mathf.Abs(turn)*r/1.5f);float sign=Mathf.Sign(turn);var c=p+new Vector2(-Mathf.Sin(heading),Mathf.Cos(heading))*r*sign;
                float a0=heading-sign*Mathf.PI/2;for(int i=1;i<=n;i++){float a=a0+turn*i/n;pts.Add(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r);}p=pts[^1];heading+=turn;}
            Straight(14);Arc(40,Mathf.PI/2);Straight(10);Arc(15,-Mathf.PI/2);Straight(10);Arc(8,Mathf.PI/2);Straight(16);
            Add("bends",pts.ToArray(),6.5f);
        }
        static Vector2[] Line(Vector2 a,Vector2 b){int n=Mathf.CeilToInt((b-a).magnitude/2);var o=new Vector2[n+1];for(int i=0;i<=n;i++)o[i]=Vector2.Lerp(a,b,i/(float)n);return o;}
        static void Add(string name,Vector2[] xz,float width)
        {
            // (the road's height along it: the land at its centre, smoothed over 5 points so the crest and hollow are
            //  driveable curves)
            var raw=new float[xz.Length];for(int i=0;i<xz.Length;i++)raw[i]=Land(xz[i].x,xz[i].y);
            var line=new Vector3[xz.Length];
            for(int i=0;i<xz.Length;i++){float s=0;int n=0;for(int k=-2;k<=2;k++){int j=Mathf.Clamp(i+k,0,xz.Length-1);s+=raw[j];n++;}line[i]=new Vector3(xz[i].x,s/n,xz[i].y);}
            Roads.Add(new RangeRoad{name=name,line=line,width=width});
        }
        public static RangeRoad Road(string name){foreach(var r in Roads)if(r.name==name)return r;throw new KeyNotFoundException("run range: no road "+name);}

        // The range's runs (docs/77, step 2): a fence both sides of every road, 1.25 m beyond its edge, cut wherever another
        // road crosses (the spur's junction and the drive make openings), and one run too short to lay.
        public static List<RunSpec> Specs(string kit="post_rail")
        {
            var o=new List<RunSpec>();
            bool Cut(Vector3 p,RangeRoad own){foreach(var r in Roads){if(r==own)continue;if(Nearest(r,p.x,p.z,out _)<r.width*.5f+.8f)return true;}return false;}
            foreach(var r in Roads) {
                if(r.name=="drive"||r.name=="spur")continue;
                foreach(int side in new[]{-1,1}) {
                    var own=r;o.Add(new RunSpec{name=$"{r.name} {(side<0?"left":"right")}",kit=kit,seed=$"{r.name}-{side}",along=r.line,offset=side*(r.width*.5f+1.25f),cut=p=>Cut(p,own)});
                }
            }
            var st=Road("straight");o.Add(new RunSpec{name="too short",kit=kit,seed="short",along=st.line,offset=-(st.width*.5f+1.25f),from=150,to=151.2f});
            // (a deforming kit also gets a field wall with corners of every kind: 76, 72, 36 and 101 deg, on open land)
            if(PropKit.Load(kit).file.kind=="deform")o.Add(new RunSpec{name="field corners",kit=kit,seed="field",along=FieldCorners,offset=0});
            return o;
        }
        // The range's bales (docs/77, step 4): a field over the crest's gentle slopes, a field on the 30% bank (where the
        // round bales turn downhill), and a row of round bales along the yard north of the straight. Spots are jittered
        // and turned by their own hash, so the layout is the same every time.
        public sealed class BaleSpec{public string name,kit="hay_bale";public List<(float x,float z,float yaw)> spots=new();public bool row;public string[] only;}
        public static List<BaleSpec> Bales()
        {
            float H(string s){var h=Hash128.Compute(s);return (h.GetHashCode()&0x7fffffff)/(float)int.MaxValue;}
            var crest=new BaleSpec{name="crest bales"};
            for(int i=0;i<5;i++)for(int j=0;j<10;j++){string k=$"crest/{i}/{j}";crest.spots.Add((-78+i*7+(H(k+"x")-.5f)*3,28+j*7+(H(k+"z")-.5f)*3,H(k+"y")*360));}
            var bank=new BaleSpec{name="bank bales"};
            for(int i=0;i<2;i++)for(int j=0;j<17;j++){string k=$"bank/{i}/{j}";bank.spots.Add((63+i*4+(H(k+"x")-.5f)*1.5f,-64+j*6+(H(k+"z")-.5f)*2,H(k+"y")*360));}
            var yard=new BaleSpec{name="yard row",row=true,only=new[]{"round","wrap"}};
            for(int i=0;i<29;i++)yard.spots.Add((-100+i*3,-88+(H($"yard/{i}z")-.5f)*.4f,(H($"yard/{i}y")-.5f)*8));
            // (step 5: the jerry can's world user, a few cans stood at the yard's end, the same library object the Cob carries)
            var cans=new BaleSpec{name="quay cans",kit="jerry_can"};
            for(int i=0;i<6;i++)cans.spots.Add((-10+(i%3)*.5f+(H($"can/{i}x")-.5f)*.12f,-88+(i/3)*.32f,(H($"can/{i}y")-.5f)*30));
            return new List<BaleSpec>{crest,bank,yard,cans};
        }
        public static readonly Vector3[] FieldCorners={new(-35,0,-50),new(-15,0,-50),new(-12,0,-38),new(-25,0,-30),new(-30,0,-18),new(-15,0,-15)};
        // The runs drawn on the range: fences along the straight (its junction and drive), walls along the crest and
        // hollow, the bank and the bends, and the field wall; the checks solve every road with both kits.
        public static List<RunSpec> Drawn()
        {
            var walls=new[]{"crest-hollow","bank-road","bends"};
            var o=Specs("post_rail").Where(s=>!walls.Any(w=>s.name.StartsWith(w))).ToList();
            o.AddRange(Specs("dry_stone").Where(s=>walls.Any(w=>s.name.StartsWith(w))||s.name=="field corners"));
            return o;
        }

        // The nearest point of a road's centre line: its height, and the horizontal distance to it.
        public static float Nearest(RangeRoad r,float x,float z,out float y)
        {
            float best=float.MaxValue;y=0;var q=new Vector2(x,z);
            for(int i=0;i+1<r.line.Length;i++) {
                Vector2 a=new(r.line[i].x,r.line[i].z),b=new(r.line[i+1].x,r.line[i+1].z),ab=b-a;
                float t=Mathf.Clamp01(Vector2.Dot(q-a,ab)/Mathf.Max(ab.sqrMagnitude,1e-6f));float d=(a+ab*t-q).magnitude;
                if(d<best){best=d;y=Mathf.Lerp(r.line[i].y,r.line[i+1].y,t);}
            }
            return best;
        }
        public float Height(float x,float z)=>Surface(x,z);
        public static float Surface(float x,float z)
        {
            float land=Land(x,z),h=land,blend=0;
            foreach(var r in Roads) {
                float d=Nearest(r,x,z,out float y),half=r.width*.5f;
                if(d<=half)return y;
                float w=1-Mathf.Clamp01((d-half)/4);if(w>blend){blend=w;h=Mathf.Lerp(land,y,w*w*(3-2*w));}
            }
            return h;
        }

        IEnumerator Start()
        {
            var race=GetComponent<RaceTrial>();race.freeRoam=true;race.track=new Circuit();race.elapsed=1;
            LoadingScreen.Begin("Little Road · run range");yield return null;
            // (a review run that throws quits at once, so a test window never sits open on a half-built range: the jerry can's
            //  red paint had no look and the window stayed up until stopped, 18 Oct)
            if(RaceTrial.Has("--run-range-review"))Application.logMessageReceived+=(m,st,t)=>{if(t==LogType.Exception){UnityEngine.Debug.Log("LITTLE_ROAD_RUN_RANGE_FAILED "+m);Application.Quit(1);}};
            BuildRoads();Ground.Use(this);
            var root=new GameObject("Run range").transform;
            Lighting();LandMesh(root);RoadMeshes(root);
            Runs.Laid.Clear();
            foreach(var spec in Drawn()){var L=Runs.Solve(spec);Runs.Draw(L,root);Runs.Laid.Add(L);}
            Objects.Laid.Clear();
            foreach(var b in Bales()){var L=Objects.Place(b.name,b.kit,b.spots,b.name,b.row,b.only);Objects.Draw(L,root);Objects.Laid.Add(L);}
            if(RaceTrial.Has("--run-debug"))Debug(root);
            view=new GameObject("Run range camera").AddComponent<Camera>();view.tag="MainCamera";view.fieldOfView=50;view.nearClipPlane=.2f;view.farClipPlane=2000;
            view.gameObject.AddComponent<AudioListener>();WorldLook.Camera(view);Look(new Vector3(-150,70,-170),new Vector3(0,0,0));
            UnityEngine.Debug.Log($"LITTLE_ROAD_RUN_RANGE_READY roads={Roads.Count} runs={Runs.Laid.Count} t={Time.realtimeSinceStartup:0.00}");LoadingScreen.Ready();
            string report=RaceTrial.Arg("--run-report");if(report!=null)Runs.WriteReport(report);
            if(RaceTrial.Has("--run-range-review"))yield return Review(RaceTrial.Arg("--trial-captures",Path.Combine(Application.persistentDataPath,"RunRange")));
        }
        void Look(Vector3 from,Vector3 at)=>view.transform.SetPositionAndRotation(from,Quaternion.LookRotation(at-from));
        void Lighting()
        {
            var dir=CoastalSky.Apply();
            var sun=new GameObject("Run range sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2f;sun.color=new Color(1,.95f,.86f);
            sun.transform.rotation=Quaternion.LookRotation(-dir);sun.shadows=LightShadows.Soft;RenderSettings.sun=sun;QualitySettings.shadowDistance=300;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.62f,.74f,.88f);
            RenderSettings.ambientEquatorColor=new Color(.5f,.55f,.55f);RenderSettings.ambientGroundColor=new Color(.3f,.32f,.28f);
        }
        void LandMesh(Transform root)
        {
            int n=Mathf.RoundToInt(2*Half/Cell);var v=new Vector3[(n+1)*(n+1)];
            // (a land vertex within a cell and a half of a road's edge goes no higher than the road there: a triangle across the
            //  edge otherwise rose through the road where the bends climb the bank, 18 Oct)
            float Vertex(float x,float z){float y=Surface(x,z);foreach(var r in Roads){float d=Nearest(r,x,z,out float ry);if(d<r.width*.5f+Cell*1.5f)y=Mathf.Min(y,ry-.02f);}return y;}
            for(int j=0;j<=n;j++)for(int i=0;i<=n;i++){float x=-Half+i*Cell,z=-Half+j*Cell;v[j*(n+1)+i]=new Vector3(x,Vertex(x,z),z);}
            var t=new int[n*n*6];int k=0;
            for(int j=0;j<n;j++)for(int i=0;i<n;i++){int a=j*(n+1)+i;t[k++]=a;t[k++]=a+n+1;t[k++]=a+1;t[k++]=a+1;t[k++]=a+n+1;t[k++]=a+n+2;}
            var m=new Mesh{name="Run range land",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};m.vertices=v;m.triangles=t;m.RecalculateNormals();
            var go=new GameObject("Run range land",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(root);
            go.GetComponent<MeshFilter>().sharedMesh=m;go.GetComponent<MeshCollider>().sharedMesh=m;
            go.GetComponent<MeshRenderer>().sharedMaterial=WorldLook.Kit("Run range grass",new Color(.42f,.58f,.3f),WorldLook.Pattern.Leaf,0,.05f,true);
        }
        void RoadMeshes(Transform root)
        {
            var mat=WorldLook.Kit("Run range asphalt",new Color(.28f,.29f,.3f),WorldLook.Pattern.Asphalt,0,.2f,true);
            foreach(var r in Roads) {
                var v=new List<Vector3>();var t=new List<int>();
                for(int i=0;i<r.line.Length;i++) {
                    var f=(r.line[Mathf.Min(i+1,r.line.Length-1)]-r.line[Mathf.Max(i-1,0)]);f.y=0;f.Normalize();var right=new Vector3(f.z,0,-f.x)*r.width*.5f;
                    v.Add(r.line[i]-right+Vector3.up*.03f);v.Add(r.line[i]+right+Vector3.up*.03f);
                    if(i>0){int a=v.Count-4;t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
                }
                var m=new Mesh{name="Run range road "+r.name};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();
                var go=new GameObject("Run range road "+r.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);
                go.GetComponent<MeshFilter>().sharedMesh=m;go.GetComponent<MeshRenderer>().sharedMaterial=mat;
            }
        }
        IEnumerator Review(string folder)
        {
            Directory.CreateDirectory(folder);
            IEnumerator Shot(string name,Vector3 from,Vector3 at)
            {
                Look(from,at);for(int f=0;f<4;f++)yield return null;yield return new WaitForEndOfFrame();
                var grab=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);grab.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);
                File.WriteAllBytes(Path.Combine(folder,name+".png"),grab.EncodeToPNG());Destroy(grab);
            }
            yield return new WaitForSecondsRealtime(2.5f);   // (the loading screen fades out first: the first capture caught it)
            yield return Shot("01-overview",new Vector3(-150,90,-180),new Vector3(0,0,0));
            yield return Shot("02-crest-hollow",new Vector3(-60,12,-10),new Vector3(-90,0,40));
            yield return Shot("03-bank",new Vector3(20,14,-60),new Vector3(60,8,0));
            yield return Shot("04-bends",new Vector3(-30,40,-20),new Vector3(20,0,60));
            yield return Shot("05-junction",new Vector3(-20,10,-120),new Vector3(20,0,-95));
            yield return Shot("06-fence-close",new Vector3(-6,2.2f,-89),new Vector3(6,.6f,-96));
            yield return Shot("07-crest-fence",new Vector3(-80,4,30),new Vector3(-95,3,60));
            yield return Shot("08-bank-fence",new Vector3(66,14,-40),new Vector3(55,9,-10));
            static Vector3 G(float x,float h,float z)=>new(x,Surface(x,z)+h,z);   // (h above the ground: the bends climb the bank)
            yield return Shot("09-tight-bend",G(46,5,78),G(63,.5f,91));
            yield return Shot("10-field-corners",G(-4,9,-62),G(-22,0,-33));
            yield return Shot("11-corner-pier",G(-8,2.2f,-44),G(-12,.6f,-38));
            yield return Shot("12-wall-close",G(-88,1.5f,-4),G(-85.5f,.7f,2));
            yield return Shot("13-wall-crest",G(-80,2,40),G(-94.5f,1,60));
            yield return Shot("14-wall-bank",G(47,2.5f,-32),G(59.5f,.8f,-6));
            yield return Shot("15-bale-field",G(-40,7,30),G(-64,0,60));
            yield return Shot("16-bale-bank",G(55,3.5f,-50),G(65,0,-28));
            yield return Shot("17-bale-row",G(-58,2.5f,-80),G(-80,.5f,-88));
            var near=Objects.Laid[0].placed[12].at;yield return Shot("18-bale-close",G(near.x+3.2f,1.5f,near.z-2.4f),near+Vector3.up*.6f);
            var steep=Objects.Laid[1].placed[7].at;yield return Shot("19-bale-steep",G(steep.x-3.5f,1.2f,steep.z-1.5f),steep+Vector3.up*.6f);
            yield return Shot("20-quay-cans",G(-9,1.1f,-90.2f),G(-9.5f,.25f,-87.8f));
            Runs.WriteReport(Path.Combine(folder,"run-report.txt"));
            UnityEngine.Debug.Log("LITTLE_ROAD_RUN_RANGE_REVIEW "+folder);Application.Quit(0);
        }
        // --run-debug: each run's offset line in yellow, its post feet as small cubes (green when the run passed)
        void Debug(Transform root)
        {
            var line=TrialWorld.Material("Run debug line",new Color(1,.85f,.1f),0,.2f,false);var ok=TrialWorld.Material("Run debug ok",new Color(.2f,.85f,.3f),0,.2f,false);var bad=TrialWorld.Material("Run debug fail",new Color(.9f,.15f,.1f),0,.2f,false);
            foreach(var L in Runs.Laid) {
                var lr=new GameObject("Run debug "+L.spec.name).AddComponent<LineRenderer>();lr.transform.SetParent(root);lr.sharedMaterial=line;lr.widthMultiplier=.15f;
                lr.positionCount=L.line.Length;lr.SetPositions(L.line.Select(p=>new Vector3(p.x,Ground.Surface(p.x,p.z)+.25f,p.z)).ToArray());
                foreach(var f in L.feet){var c=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(c.GetComponent<Collider>());c.transform.SetParent(root);c.transform.position=f+Vector3.up*1.6f;c.transform.localScale=Vector3.one*.25f;c.GetComponent<Renderer>().sharedMaterial=L.failures.Count==0?ok:bad;}
            }
        }
        void OnDestroy(){Ground.Reset();}
    }
}
