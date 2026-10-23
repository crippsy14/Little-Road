using UnityEngine;

namespace LittleRoad
{
    // Quarry Hills' scenery (docs/59), built after Hillcrest's woods with the same kit and the same rules: nothing on a road,
    // nothing floating (everything sits on ValleyField), far stand-ins (KitLod) so the woods can be dense.
    public sealed partial class CoastalLandscape
    {
        // As a coroutine (docs/72): at load it runs straight through (TrialWorld.RunNow); as a background district (Districts)
        // it yields between pieces of work, a few milliseconds a frame. Either way the same work in the same order.
        void QuarryHillsScenery()=>TrialWorld.RunNow(QuarryHillsSceneryRoutine());
        // The district's holder (its settle pass batches it).
        Transform quarryHolder;
        // Build Quarry Hills in the background (Districts): a free-roam build with the car starting far from it.
        bool deferQuarry=>Districts.Enabled&&WorldTiles.Streams&&!RaceArea.Active&&QuarryHills.Enabled&&
            Vector2.Distance(new Vector2(WorldTiles.Focus.x,WorldTiles.Focus.z),QuarryHills.Square)>Districts.NearStart;
        // Rails and corner walls that a Quarry Hills building could trim (within 30 m of the district).
        public static bool NearQuarryHills(Vector3 p)=>p.x<QuarryHills.SeamX+30&&p.x>QuarryHills.West-30&&p.z>QuarryHills.South-30;
        // (Its settle pass is every district's: SettleDistrict, FarDistricts.cs.)
        System.Collections.IEnumerator QuarryHillsSceneryRoutine()
        {
            if(!QuarryHills.Enabled||!BayviewKit.Has("tree_round"))yield break;
            var holder=new GameObject("Quarry Hills scenery").transform;holder.SetParent(root);quarryHolder=holder;
            // (A race builds only its area: RaceArea. The town 690 m from the quarry floor stays; the farms and walls go.)
            if(RaceArea.Near(QuarryHills.Square,350))yield return new QuarryHillsTown(holder).BuildRoutine();else QuarryHillsTown.Last=null;
            Physics.SyncTransforms();Step("qh-town");yield return null;if(!RaceArea.Active)yield return QuarryHillsCountry.BuildRoutine(holder);Physics.SyncTransforms();Step("qh-country");yield return null;
            CoastalLandscape.Lap("qhs start");var rng=new System.Random(5903);float R()=>(float)rng.NextDouble();int trees=0,tufts=0;
            bool nature=BayviewKit.Has("tree_fir");
            // (Streamed in on its tile away from where you start: WorldTiles.)
            void Plant(string id,float x,float z,float scale,bool solid=true)
            {
                var at=new Vector3(x,ValleyField.Height(x,z)-.1f,z);float yaw=R()*360;if(!RaceArea.Near(x,z,60))return;
                WorldTiles.Place(at,solid?1.2f*scale:0,()=>{var g=BayviewKit.Spawn(id,at,yaw,holder,solid:solid,scale:scale);
                    if(id.StartsWith("tree_")||id=="bush"||id=="flower_bush")KitLod.Attach(g,id);if(WorldTiles.Late)PlacementAudit.SettleOne(g);});
            }
            // Where trees may stand: off the roads, the town core, the quarry floor, the pit and galleries, the tips, the
            // shore and steep rock, and above the moorland tree line.
            bool Open(float x,float z)
            {
                if(!QuarryHills.Contains(x,z)||x>QuarryHills.SeamX-40)return false;
                float h=ValleyField.Height(x,z);if(h<-6||h>185||ValleyField.Normal(x,z).y<.8f||ValleyField.RoadDistance(x,z)<12)return false;
                if(Vector2.Distance(new(x,z),QuarryHills.Square)<300||Vector2.Distance(new(x,z),QuarryHills.Source)<QuarryHills.SourceRadius+14)return false;
                // (Clear of the rugby ground, the farmyards and the campsite.)
                if(Vector2.Distance(new(x,z),QuarryHillsCountry.Pitch)<95)return false;
                foreach(var s in QuarryHillsTown.Spots)if((s.Key.StartsWith("farm ")||s.Key=="campsite"||s.Key=="workshops")&&Vector2.Distance(new(x,z),new(s.Value.at.x,s.Value.at.z))<55)return false;
                float qf=Mathf.Sqrt(Mathf.Pow((x-QuarryHills.FloorCentre.x)/QuarryHills.FloorRX,2)+Mathf.Pow((z-QuarryHills.FloorCentre.y)/QuarryHills.FloorRZ,2));if(qf<1.5f)return false;
                QuarryHills.PitMetric(x,z,out float q,out float e,out float mask);if(q<1.15f||mask>.2f&&e<QuarryHills.Benches*QuarryHills.BenchWidth+30)return false;
                if(QuarryHills.OnTip(x,z,18))return false;
                return true;
            }
            // The woods: the same noise the macro tint paints dark (so trees stand on woodland floor), plus the western forest.
            float Woods(float x,float z)
            {
                float h=ValleyField.Height(x,z),n=Mathf.PerlinNoise(x*.007f+31,z*.007f+57);
                float w=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.44f,.56f,n));
                w=Mathf.Max(w,Mathf.SmoothStep(0,1,Mathf.InverseLerp(-3600,-3900,x)));
                return w*Mathf.SmoothStep(0,1,Mathf.InverseLerp(25,45,h));
            }
            string Species(float x,float z,float roll)
            {
                if(!nature)return roll<.5f?"tree_pine":"tree_round";
                float h=ValleyField.Height(x,z);
                // Conifer plantations on the hills, birch and broadleaf lower down and by water.
                if(h>90)return roll<.62f?"tree_fir":roll<.9f?"tree_pine":"tree_birch";
                return roll<.3f?"tree_birch":roll<.55f?"tree_round":roll<.8f?"tree_fir":"tree_pine";
            }
            for(int i=0;i<60000&&trees<4200;i++) {
                if(i%8000==7999)yield return null;
                float x=QuarryHills.West+R()*(QuarryHills.SeamX-QuarryHills.West),z=QuarryHills.South+R()*2600;
                float w=Woods(x,z);if(w<.5f||R()>w||!Open(x,z))continue;
                int n=3+(int)(w*7+R()*3);float radius=8+w*14;string dominant=Species(x,z,R());
                for(int k=0;k<n&&trees<4200;k++){float a=R()*Mathf.PI*2,r=Mathf.Sqrt(R())*radius,px=x+Mathf.Cos(a)*r,pz=z+Mathf.Sin(a)*r;if(!Open(px,pz))continue;
                    Plant(R()<.75f?dominant:Species(px,pz,R()),px,pz,.8f+R()*.6f);trees++;}
            }
            CoastalLandscape.Lap("qhs woods");
            // Along the river's upper reach: alders and reeds.
            var river=HillcrestRoute.River;float head=river.Nearest(new Vector3(-1180,0,-1330),out _);
            for(float d=8;d<head;d+=7)foreach(int side in new[]{-1,1}) {
                var c=river.At(d);var p=c+river.Right(d)*side*(13+R()*4);if(ValleyField.RoadDistance(p.x,p.z)<9)continue;
                if(R()<.45f){Plant(nature?"tree_birch":"tree_round",p.x,p.z,.7f+R()*.4f);trees++;}else if(BayviewKit.Has("reeds"))Plant("reeds",c.x+river.Right(d).x*side*10.5f,c.z+river.Right(d).z*side*10.5f,.8f+R()*.4f,false);
            }
            // Verges: rough grass and the odd gorse bush along every Quarry Hills road.
            foreach(var road in HillcrestRoute.All()) {
                if(!QuarryHills.Owns(road.name))continue;
                for(float d=0;d<road.Length;d+=2.6f) {
                    int side=R()<.5f?-1:1;var q=road.At(d)+road.Right(d)*side*(road.width*.5f+2.6f+R()*6);
                    if(!QuarryHills.Contains(q.x,q.z)||QuarryHills.Bare(q.x,q.z)||ValleyField.RoadDistance(q.x,q.z)<road.width*.5f+2.3f||ValleyField.Height(q.x,q.z)<road.At(d).y-3)continue;
                    // Not on the square, the Pit Stop's forecourt or any plot (street review, 4 Oct: tufts in the garage bays).
                    if(QuarryHillsTown.Last!=null&&!QuarryHillsTown.Last.Clear(new Vector2(q.x,q.z),new Vector2(.6f,.6f),0))continue;
                    if(R()<.1f)Plant("bush",q.x,q.z,.7f+R()*.5f,false);else Plant("grass_tuft",q.x,q.z,.9f+R()*.8f,false);tufts++;
                }
            }
            CoastalLandscape.Lap("qhs river verges");yield return null;yield return SlateFaces(holder);int slate=slateCells;CoastalLandscape.Lap("qhs slate");yield return null;
            Galleries(holder);CoastalLandscape.Lap("qhs galleries");yield return null;Physics.SyncTransforms();GalleryClimb.Build(holder);CoastalLandscape.Lap("qhs climb");yield return null;
            TipLeap.Plan();TipLeap.Build(holder);CoastalLandscape.Lap("qhs tipleap");GalleryDressing(holder);CoastalLandscape.Lap("qhs dressing");yield return null;QuarryFloor.Build(holder);CoastalLandscape.Lap("qhs floor");yield return null;if(RaceArea.Near(QuarryHills.QuayCentre,250))QuarryQuay.Build(holder);
            Debug.Log($"LITTLE_ROAD_QUARRY_SCENERY trees={trees} tufts={tufts} slateCells={slate}");
        }
        // Worked slate: the galleries (benches and faces alike), the pit's walls above the water and the tips are all bare
        // slate, so they get a rock-only surface laid 5 cm over the ground (the terrain shader alone shows grass on every
        // flat shelf, and the galleries read as green terraces). Three detail levels to match the terrain chunks.
        public static bool IsSlate(float x,float z)
        {
            QuarryHills.PitMetric(x,z,out float q,out float e,out float mask);float ew=e+QuarryHills.Wobble(x,z);
            // (Not under the authored galleries mesh.)
            if(mask>.35f&&mask<QuarryHills.MeshMask+.05f&&ew>-3&&ew<QuarryHills.Benches*QuarryHills.BenchWidth+4)return true;
            if(QuarryHills.OnTip(x,z))return true;
            return false;
        }
        // The worked-slate surface (the quarry floor's sheet uses it too).
        public static Material SlateMaterial;
        int slateCells;
        System.Collections.IEnumerator SlateFaces(Transform parent)
        {
            // The tips and the floor: slate spoil, matte chips of every shade (the kit's slate pattern on flat-ish ground).
            var m=WorldLook.Kit("Quarry Hills worked slate",new Color(.31f,.32f,.36f),WorldLook.Pattern.Slate,0,.05f,true);SlateMaterial=m;
            var boxes=new System.Collections.Generic.List<Rect>{Rect.MinMaxRect(QuarryHills.Pit.x-QuarryHills.PitRX-340,QuarryHills.Pit.y-QuarryHills.PitRZ-340,QuarryHills.Pit.x+QuarryHills.PitRX+340,QuarryHills.Pit.y+QuarryHills.PitRZ+340)};
            for(int i=0;i<QuarryHills.Tips.Length;i++)boxes.Add(QuarryHills.TipBox(i));
            int cells=0;const int block=96;
            // Every block's three detail levels are worked out on all cores at once (docs/72: 279 ms on the main thread),
            // then made here in the original order: the same objects as before.
            var blocks=new System.Collections.Generic.List<(int bi,int bj,int ci1,int cj1)>();
            foreach(var box in boxes) {
                int i0=Mathf.Max(1,Mathf.FloorToInt((box.xMin-ValleyField.X0)/ValleyField.Cell)),i1=Mathf.Min(ValleyField.NX-2,Mathf.CeilToInt((box.xMax-ValleyField.X0)/ValleyField.Cell));
                int j0=Mathf.Max(1,Mathf.FloorToInt((box.yMin-ValleyField.Z0)/ValleyField.Cell)),j1=Mathf.Min(ValleyField.NZ-2,Mathf.CeilToInt((box.yMax-ValleyField.Z0)/ValleyField.Cell));
                for(int bj=j0;bj<j1;bj+=block)for(int bi=i0;bi<i1;bi+=block)blocks.Add((bi,bj,Mathf.Min(bi+block,i1),Mathf.Min(bj+block,j1)));
            }
            var data=new SlateData[blocks.Count*3];
            void Shape(int k){var b=blocks[k/3];int l=k%3;data[k]=SlateGeometry(b.bi,b.bj,b.ci1,b.cj1,l==0?1:l==1?2:4,l==0?.05f:l==1?.35f:.9f);}
            // (In the background the shapes are worked out off the main thread while the game runs.)
            if(Districts.Pending){var task=System.Threading.Tasks.Task.Run(()=>System.Threading.Tasks.Parallel.For(0,data.Length,Shape));while(!task.IsCompleted)yield return null;task.Wait();}
            else System.Threading.Tasks.Parallel.For(0,data.Length,Shape);
            for(int k=0;k<blocks.Count;k++) {
                {
                    var lods=new System.Collections.Generic.List<LOD>();var go=new GameObject("Worked slate");go.transform.SetParent(parent);int made=0;
                    for(int l=0;l<3;l++) {
                        var slab=data[k*3+l];var mesh=slab.quads==0?null:slab.ToMesh();if(l==0)made=slab.quads;if(mesh==null)continue;
                        var part=new GameObject("LOD"+l,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(go.transform);
                        part.GetComponent<MeshFilter>().sharedMesh=mesh;var r=part.GetComponent<MeshRenderer>();r.sharedMaterial=m;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                        lods.Add(new LOD(l==0?.26f:l==1?.085f:0,new Renderer[]{r}));
                    }
                    if(made==0){Object.Destroy(go);continue;}
                    cells+=made;go.AddComponent<LODGroup>().SetLODs(lods.ToArray());
                }
            }
            slateCells=cells;
        }
        // The galleries (docs/59): twelve benches as authored geometry round the pit's south-east side, each a 1 m battered
        // face of slate and a level shelf you can drive (with its own collider), plus the pit's wall all the way round
        // from below the lake to the floor. Built column by column round the pit (every 1.2 degrees); each strip of the
        // profile has its own vertices so the lips stay crisp.
        void Galleries(Transform parent)
        {
            // Split slate (Richard, 3 Oct: the quarry looked shiny and odd): the kit shader's matte slate pattern in world space, so
            // the faces show beds and cleavage instead of the terrain shader's stretched rock; Penrhyn's blue-grey with purple beds.
            var m=WorldLook.Kit("Quarry Hills gallery slate",new Color(.38f,.39f,.44f),WorldLook.Pattern.Slate,0,.05f,true);
            // Penrhyn slate: blue-grey with a purple cast, lighter than the tips (fresh faces catch the light).

            float axis=Mathf.Atan2(QuarryHills.GalleryAxis.y,QuarryHills.GalleryAxis.x)*Mathf.Rad2Deg;
            // The directions the mesh covers: where the cut's mask reaches MeshMask.
            float half=0;for(float a=0;a<=180;a+=.25f){var d=Dir(axis+a);if(QuarryHills.MaskOf(d)>=QuarryHills.MeshMask)half=a;}
            var v=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var t=new System.Collections.Generic.List<int>();var shelf=new System.Collections.Generic.List<int>();
            var rubble=WorldLook.Kit("Gallery slate rubble",new Color(.35f,.36f,.39f),WorldLook.Pattern.Slate,0,.05f,true);
            int B=QuarryHills.Benches;float W=QuarryHills.BenchWidth,H=QuarryHills.BenchHeight,F=QuarryHills.FloorY;
            // Profile (e beyond the rim, height): the pit wall from under the lake to the floor, then face and shelf by bench.
            System.Collections.Generic.List<Vector2> Profile(Vector2 dir,bool benches)
            {
                var p=new System.Collections.Generic.List<Vector2>{new(-1.2f,QuarryHills.LakeBed),new(0,F),new(QuarryHills.RimLedge+.6f,F)};
                if(!benches){p.Add(new(13.5f,F+.12f));foreach(float e in new[]{16f,19f,22f,26f}){var g=QuarryHills.PitPoint(dir,e);p.Add(new(e,Mathf.Max(F+.12f,ValleyField.Height(g.x,g.z)+.3f)));}return p;}
                for(int k=0;k<B;k++) {
                    var at=QuarryHills.PitPoint(dir,k*W);float lip=Mathf.Max(p[^1].x+(k==0?1.5f:.5f),k*W-QuarryHills.Wobble(at.x,at.z));
                    p.Add(new(lip,F+k*H));p.Add(new(lip+1.1f,F+(k+1)*H));
                }
                var top=QuarryHills.PitPoint(dir,B*W);p.Add(new(Mathf.Max(p[^1].x+3,B*W-QuarryHills.Wobble(top.x,top.z)+6),F+B*H));
                return p;
            }
            void Strips(float from,float to,bool benches)
            {
                int cols=Mathf.Max(2,Mathf.CeilToInt((to-from)/1.2f)+1);var profiles=new System.Collections.Generic.List<System.Collections.Generic.List<Vector2>>();var dirs=new System.Collections.Generic.List<Vector2>();
                for(int c=0;c<cols;c++){var d=Dir(Mathf.Lerp(from,to,c/(cols-1f)));dirs.Add(d);profiles.Add(Profile(d,benches));}
                int segs=profiles[0].Count-1;
                for(int s=0;s<segs;s++) {
                    int start=v.Count;float run=0;
                    for(int c=0;c<cols;c++) {
                        var a=QuarryHills.PitPoint(dirs[c],profiles[c][s].x);a.y=profiles[c][s].y;var b=QuarryHills.PitPoint(dirs[c],profiles[c][s+1].x);b.y=profiles[c][s+1].y;
                        if(c>0)run+=Vector3.Distance(new Vector3(a.x,0,a.z),new Vector3(v[^2].x,0,v[^2].z));
                        bool face=Mathf.Abs(b.y-a.y)>2;v.Add(a);v.Add(b);
                        uv.Add(face?new Vector2(run*.2f,a.y*.2f):new Vector2(a.x,a.z)*.2f);uv.Add(face?new Vector2(run*.2f,b.y*.2f):new Vector2(b.x,b.z)*.2f);
                        if(c>0){int k=start+(c-1)*2;(face?t:shelf).AddRange(new[]{k,k+2,k+1,k+2,k+3,k+1});} // up on shelves, towards the pit on faces
                    }
                }
            }
            Strips(axis-half,axis+half,true);
            Strips(axis+half,axis+360-half,false);
            var mesh=new Mesh{name="Galleries",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,subMeshCount=2};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.SetTriangles(shelf,1);
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject("Galleries",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=new[]{m,rubble};go.GetComponent<MeshCollider>().sharedMesh=mesh;
            // No cast shadows: the far shadow cascade blotched the whole bowl in coarse squares; the faces' own shading reads.
            go.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Debug.Log($"LITTLE_ROAD_QUARRY_GALLERIES half={half:0.0} vertices={v.Count}");
        }
        // The galleries' working life: scree at the foot of every face, birch seeded on the old upper benches, and two
        // inclines (rubble embankments with a rail track up the benches) with a drumhouse at the head of each.
        void GalleryDressing(Transform parent)
        {
            var rng=new System.Random(4409);float R()=>(float)rng.NextDouble();
            float axis=Mathf.Atan2(QuarryHills.GalleryAxis.y,QuarryHills.GalleryAxis.x)*Mathf.Rad2Deg;
            float half=0;for(float a=0;a<=180;a+=.25f)if(QuarryHills.MaskOf(Dir(axis+a))>=QuarryHills.MeshMask)half=a;
            int B=QuarryHills.Benches;float W=QuarryHills.BenchWidth,H=QuarryHills.BenchHeight,F=QuarryHills.FloorY;
            string[] rocks={"rock_a","rock_b","rock_cluster"};int scree=0,birch=0;
            for(int k=0;k<B-1;k++)for(float a=axis-half+4;a<axis+half-4;a+=5+R()*9) {
                if(GalleryClimb.Covers(a,k+1))continue;   // (the Gallery Climb's ramps and landings run here)
                var dir=Dir(a);var at=QuarryHills.PitPoint(dir,(k+1)*W);float toe=(k+1)*W-QuarryHills.Wobble(at.x,at.z);
                var p=QuarryHills.PitPoint(dir,toe-1.5f-R()*3);p.y=F+(k+1)*H-.2f;
                string id=rocks[rng.Next(rocks.Length)];if(!BayviewKit.Has(id))continue;
                BayviewKit.Spawn(id,p,R()*360,parent,null,null,false,1.4f+R()*1.8f);scree++;
                if(k>=5&&R()<.16f&&BayviewKit.Has("tree_birch"))for(int n=0;n<3;n++){var q=QuarryHills.PitPoint(Dir(a+(R()-.5f)*2),toe-8-R()*10);q.y=F+(k+1)*H-.1f;KitLod.Attach(BayviewKit.Spawn("tree_birch",q,R()*360,parent,solid:false,scale:.6f+R()*.4f),"tree_birch");birch++;}
            }
            var body=WorldLook.Kit("Incline rubble",new Color(.34f,.35f,.39f),WorldLook.Pattern.Slate,0,.05f,true);
            var rail=TrialWorld.Material("Incline rail",new Color(.22f,.2f,.2f),.3f,.4f);
            foreach(float off in new[]{-38f,34f}) {
                var dir=Dir(axis+off);float Y(float e)=>F+H+2.4f+(e-7.7f)*H/W;
                // (The west incline stops at bench 5: the Gallery Climb's Long Bench runs across above its head on bench 6.)
                float e0=11.7f,e1=(off<0?4:B-1)*W+9;Vector3 P(float e){var q=QuarryHills.PitPoint(dir,e);q.y=Y(e);return q;}
                Vector3 a=P(e0),b=P(e1),along=(b-a);along.y=0;along.Normalize();var side=Vector3.Cross(Vector3.up,along);
                // The embankment: a top along the incline, vertical sides 20 m deep (into the benches), vertical ends.
                var v=new System.Collections.Generic.List<Vector3>();var t=new System.Collections.Generic.List<int>();
                void Quad(Vector3 q0,Vector3 q1,Vector3 q2,Vector3 q3){int s=v.Count;v.Add(q0);v.Add(q1);v.Add(q2);v.Add(q3);t.AddRange(new[]{s,s+1,s+2,s,s+2,s+3});}
                Vector3 down=Vector3.down*20;float w=3.6f;
                Vector3 al=a-side*w,ar=a+side*w,bl=b-side*w,br=b+side*w;
                Quad(al,bl,br,ar);Quad(ar,br,br+down,ar+down);Quad(bl,al,al+down,bl+down);Quad(al,ar,ar+down,al+down);Quad(br,bl,bl+down,br+down);
                var mesh=new Mesh{name="Incline"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                var go=new GameObject("Incline",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=body;go.GetComponent<MeshCollider>().sharedMesh=mesh;go.tag="Respawn";
                // Two tracks (the loaded wagon down pulls the empty one up) and a wagon on each.
                var pitch=Quaternion.LookRotation((b-a).normalized,Vector3.up);float len=Vector3.Distance(a,b);
                foreach(float track in new[]{-1.6f,1.6f}) {
                    foreach(float r in new[]{-.55f,.55f}){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.Destroy(g.GetComponent<Collider>());g.name="Incline rail";g.transform.SetParent(parent);
                        g.transform.SetPositionAndRotation((a+b)*.5f+side*(track+r)+pitch*Vector3.up*.1f,pitch);g.transform.localScale=new Vector3(.12f,.16f,len);g.GetComponent<Renderer>().sharedMaterial=rail;}
                    var at=Vector3.Lerp(a,b,track<0?.3f:.62f)+side*track+pitch*Vector3.up*.75f;
                    // (--prop-forge: the quarry kit's wagon on the rails, pitched with the incline: docs/80 round 6)
                    if(PropForge.Enabled&&BayviewKit.Has("slate_wagon")){var fw=BayviewKit.Spawn("slate_wagon",at-pitch*Vector3.up*.65f,pitch.eulerAngles.y,parent,new BayviewKit.Tint(Toy.Hex(0x8c3b2e),Color.white,Color.white,Color.white),null,false);fw.transform.rotation=pitch;Debug.Log($"LITTLE_ROAD_SPOT incline_wagon {at}");continue;}
                    var wagon=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.Destroy(wagon.GetComponent<Collider>());wagon.name="Slate wagon";wagon.transform.SetParent(parent);wagon.transform.SetPositionAndRotation(at,pitch);wagon.transform.localScale=new Vector3(1.5f,1.1f,2.6f);
                    wagon.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material("Slate wagon",Toy.Hex(0x8c3b2e),0,.3f);
                }
                // The drumhouse at the head: a stone house over the winding drum, slate-roofed.
                var head=b+along*9;head.y=ValleyField.Height(head.x,head.z);
                if(BayviewKit.Has("house_c")){var tint=new BayviewKit.Tint(Toy.Hex(0x7f868d),Toy.Hex(0x3b4047),Toy.Hex(0x3b4047),Toy.Hex(0x3b4047));tint.roof=Toy.Hex(0x40464d);
                    BayviewKit.Spawn("house_c",head,Quaternion.LookRotation(-along).eulerAngles.y,parent,tint,"DRUMHOUSE",true,1.1f);}
            }
            Debug.Log($"LITTLE_ROAD_QUARRY_DRESSING scree={scree} birch={birch}");
        }
        static Vector2 Dir(float degrees){float r=degrees*Mathf.Deg2Rad;return new Vector2(Mathf.Cos(r),Mathf.Sin(r));}
        sealed class SlateData
        {
            public System.Collections.Generic.List<Vector3> v,n;public System.Collections.Generic.List<Vector2> uv;public System.Collections.Generic.List<int> t;public int quads;
            public Mesh ToMesh()
            {
                var mesh=new Mesh{name="Worked slate",indexFormat=v.Count>65000?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16};
                mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateBounds();return mesh;
            }
        }
        // (Pure: the height field and QuarryHills' shape functions only, so it runs on any thread.)
        static SlateData SlateGeometry(int i0,int j0,int i1,int j1,int step,float lift)
        {
            int quads;
            var v=new System.Collections.Generic.List<Vector3>();var n=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var t=new System.Collections.Generic.List<int>();
            int cols=(i1-i0)/step+1,rows=(j1-j0)/step+1;quads=0;
            for(int r=0;r<rows;r++)for(int c=0;c<cols;c++) {
                int i=Mathf.Min(i0+c*step,i1),j=Mathf.Min(j0+r*step,j1);var p=new Vector3(ValleyField.X0+i*ValleyField.Cell,ValleyField.Raw(i,j)+lift,ValleyField.Z0+j*ValleyField.Cell);
                v.Add(p);n.Add(new Vector3(ValleyField.Raw(i-1,j)-ValleyField.Raw(i+1,j),2*ValleyField.Cell,ValleyField.Raw(i,j-1)-ValleyField.Raw(i,j+1)).normalized);uv.Add(new Vector2(p.x,p.z)*.2f);
            }
            for(int r=0;r<rows-1;r++)for(int c=0;c<cols-1;c++) {
                var a=v[r*cols+c];var d=v[(r+1)*cols+c+1];if(!IsSlate((a.x+d.x)*.5f,(a.z+d.z)*.5f))continue;
                int k=r*cols+c;t.AddRange(new[]{k,k+cols,k+1,k+1,k+cols,k+cols+1});quads++;
            }
            return new SlateData{v=v,n=n,uv=uv,t=t,quads=quads};
        }
    }
}
