using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Road Trip Adventure-style countryside inside the loop: a farm with striped fields,
    // a campsite, a lookout by the windmill, and roadside dressing (bend chevrons,
    // telegraph poles with sagging wires). Everything stays outside the race corridor.
    public sealed class BayviewCountryside
    {
        public static readonly Vector3 Farm=new(40,0,2),Camp=new(-58,0,-22),Lookout=new(-24,0,12);
        // Tree planting keeps these clear.
        public static bool Reserved(Vector3 p){if(Flat(p,Farm)<30||Flat(p,Camp)<14||Flat(p,Lookout)<12)return true;foreach(var plot in Plots)if(Flat(p,plot)<PlotHalf+4)return true;return false;}
        // Mediterranean crop plots on the island's gentle slopes (30 September): lavender and vines, like
        // Hillcrest's patchwork, so Bayview's hills are not one flat green. Chosen before tree planting.
        public static readonly List<Vector3> Plots=new();const float PlotHalf=16;
        public static void ChoosePlots(CoastalLandscape land)
        {
            Plots.Clear();var candidates=new List<(Vector3 p,float slope)>();
            for(float x=-140;x<=140;x+=8)for(float z=-70;z<=105;z+=8) {
                var p=new Vector3(x,0,z);if(Reserved(p)||!land.PlotGround(p,PlotHalf))continue;
                float gx=land.Height(x+4,z)-land.Height(x-4,z),gz=land.Height(x,z+4)-land.Height(x,z-4);candidates.Add((p,gx*gx+gz*gz));
            }
            candidates.Sort((a,b)=>a.slope.CompareTo(b.slope));
            foreach(var c in candidates){bool apart=true;foreach(var q in Plots)if(Flat(c.p,q)<PlotHalf*2+22)apart=false;if(apart)Plots.Add(c.p);if(Plots.Count==4)break;}
        }
        static float Flat(Vector3 a,Vector3 b)=>new Vector2(a.x-b.x,a.z-b.z).magnitude;
        readonly CoastalLandscape land;readonly Circuit track;readonly Transform root;
        public BayviewCountryside(CoastalLandscape landscape,Circuit circuit,Transform parent){land=landscape;track=circuit;root=parent;}
        Vector3 Ground(Vector3 p){p.y=land.SurfacePoint(p,out var hit,false)?hit.point.y:land.Height(p.x,p.z);return p;}
        GameObject Put(string id,Vector3 p,float yaw,BayviewKit.Tint tint=null,float scale=1,bool solid=true)=>BayviewKit.Spawn(id,Ground(p)+Vector3.down*.05f,yaw,root,tint,solid:solid,scale:scale);
        static BayviewKit.Tint T(int wall,int shutter,int awning,int door)=>new(Toy.Hex(wall),Toy.Hex(shutter),Toy.Hex(awning),Toy.Hex(door));
        public void Build()
        {
            if(!BayviewKit.Has("barn"))return;
            FarmYard();Campsite();LookoutPoint();Tracks();Roadside();CropPlots();
        }
        // A little farm on a levelled pad: house and barn facing across an earth yard, the
        // tractor and a bale stack, a drive-through wheat field and a lavender plot, all fenced.
        void FarmYard()
        {
            var f=Farm;
            var yard=GameObject.CreatePrimitive(PrimitiveType.Cube);yard.name="Farmyard earth";yard.transform.SetParent(root);Object.Destroy(yard.GetComponent<Collider>());
            yard.transform.position=Ground(f+new Vector3(0,0,6))+Vector3.up*.03f;yard.transform.localScale=new Vector3(34,.05f,12);yard.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material("Farmyard earth",new Color(.62f,.52f,.38f),0,.08f);
            // On stone plinths down to the lowest ground under them (street review, 4 Oct: both hung over the road cutting).
            Plinth(f+new Vector3(-11,0,16),new Vector2(8.5f,7.5f));Put("house_a",f+new Vector3(-11,0,16),180,T(0xfff3dc,0x2e9e5b,0xff4d5e,0x2e9e5b));
            Plinth(f+new Vector3(10,0,17),new Vector2(10.5f,9.5f));Put("barn",f+new Vector3(10,0,17),180,T(0xd94040,0xffffff,0xffffff,0xd94040),1.15f);
            Put("tractor",f+new Vector3(-2,0,7),70);
            foreach(var (x,y) in new[]{(19f,0f),(20.3f,0f),(21.6f,0f),(19.65f,1.25f),(20.95f,1.25f),(20.3f,2.45f)}){var b=Put("hay_bale",f+new Vector3(x,0,8),0);b.transform.position+=Vector3.up*y;}
            // Wheat: 30 x 18 m of car-high crop to plough through.
            var wheat=f+new Vector3(-7,0,-11);WheatField.Create("Bayview farm wheat",root,wheat,new Vector2(30,18),0,(x,z)=>Ground(new Vector3(x,0,z)).y,77);
            for(float x=-21;x<=9;x+=6)if(x<-10||x>-4)Put("fence",f+new Vector3(x,0,-1.2f),0);
            for(float x=-21;x<=9;x+=6)Put("fence",f+new Vector3(x,0,-20.8f),0);
            foreach(float z in new[]{-17f,-11f,-5f})Put("fence",f+new Vector3(-23.2f,0,z),90);
            // Lavender beside it.
            if(BayviewKit.Has("lavender_row"))for(int r=0;r<7;r++)Put("lavender_row",f+new Vector3(24f,0,-18+r*2.3f),0,null,1,false);
            foreach(float z in new[]{-17f,-11f,-5f})Put("fence",f+new Vector3(9.2f,0,z),90);
        }
        void Plinth(Vector3 at,Vector2 size)
        {
            float floor=Ground(at).y,low=floor;
            for(int i=0;i<5;i++)for(int j=0;j<5;j++)low=Mathf.Min(low,Ground(at+new Vector3((i/2f-1)*(size.x*.5f+.3f),0,(j/2f-1)*(size.y*.5f+.3f))).y);
            if(floor-low<.25f)return;
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="Stone plinth";g.transform.SetParent(root);g.tag="Respawn";
            g.transform.position=new Vector3(at.x,(low-.5f+floor)*.5f,at.z);g.transform.localScale=new Vector3(size.x+.5f,floor-low+.5f,size.y+.5f);
            g.GetComponent<Renderer>().sharedMaterial=WorldLook.Kit("Bayview stone plinth",new Color(.78f,.72f,.6f),WorldLook.Pattern.Stone,0,.12f,true);
        }
        // Dirt tracks off the inside of the loop: to the farmyard, the campsite and the lookout.
        void Tracks()
        {
            var dirt=TrialWorld.Material("Farm track dirt",new Color(.72f,.62f,.45f),0,.08f);var verge=TrialWorld.Material("Farm track verge",new Color(.58f,.60f,.34f),0,.06f);
            foreach(var to in new[]{Farm+new Vector3(-3,0,8),Camp+new Vector3(-13,0,2),Lookout+new Vector3(6,0,4)}) {
                float d=track.Nearest(to,out _);var edge=track.At(d);var dir=to-edge;dir.y=0;dir.Normalize();
                var start=edge+dir*8.4f;var control=new[]{start,start+dir*6,Vector3.Lerp(start+dir*6,to,.5f)+Vector3.Cross(Vector3.up,dir)*3,to};
                var line=HillcrestRoute.Dense(control,1.2f);
                // Start where it can be driven (street review, 4 Oct: the tracks were draped straight down the road cuttings'
                // faces, and the two strips fought on the steep part): start after the last 1.2 m step steeper than 30%.
                int first=0;for(int i=0;i+1<line.Length-4;i++)if(Mathf.Abs(Ground(line[i+1]).y-Ground(line[i]).y)>.36f)first=i+1;
                if(first>0)line=line[first..];
                Strip("Farm track verge",line,4.6f,.035f,verge);Strip("Farm track",line,3.0f,.06f,dirt);
            }
        }
        void Strip(string name,Vector3[] line,float width,float lift,Material material)
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<line.Length;i++) {
                var f=line[Mathf.Min(line.Length-1,i+1)]-line[Mathf.Max(0,i-1)];f.y=0;f.Normalize();var r=Vector3.Cross(Vector3.up,f);
                foreach(int s in new[]{-1,1})v.Add(Ground(line[i]+r*s*width*.5f)+Vector3.up*lift);
                if(i<line.Length-1){int a=i*2;t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        // Rows along the contour (across the slope), alternating lavender and vines, with a flowering edge.
        void CropPlots()
        {
            for(int k=0;k<Plots.Count;k++) {
                var c=Plots[k];float gx=land.Height(c.x+4,c.z)-land.Height(c.x-4,c.z),gz=land.Height(c.x,c.z+4)-land.Height(c.x,c.z-4);
                float yaw=Mathf.Atan2(gx,gz)*Mathf.Rad2Deg+90;var rot=Quaternion.Euler(0,yaw,0);string id=k%2==0?"lavender_row":"vine_row";
                for(int r=0;r<11;r++)for(int col=-1;col<=1;col++)Put(id,c+rot*new Vector3(col*11.5f,0,-PlotHalf+2+r*2.8f),yaw,null,1,false);
                for(int e=-2;e<=2;e++)Put("flower_bush",c+rot*new Vector3(e*6.5f,0,-PlotHalf-.5f),e*40,null,.8f,false);
            }
            Debug.Log("LITTLE_ROAD_BAYVIEW_PLOTS count="+Plots.Count);
        }
        void Campsite()
        {
            var c=Camp;
            foreach(var (x,z,yaw,t) in new[]{(-5f,3f,160f,0xff8a3d),(0f,6f,190f,0x4a7bff),(5f,3.5f,215f,0x3ee6a4)})
                Put("tent",c+new Vector3(x,0,z),yaw,T(t,0xffffff,0xffd23f,0xffffff));
            Put("campfire",c+new Vector3(0,0,-1),0);
            Put("caravan",c+new Vector3(-7,0,-6),110,T(0x5ec8ff,0xffffff,0xff4d5e,0x5ec8ff));
            Put("picnic_table",c+new Vector3(6,0,-5),20);
            var car=TrialWorld.Car("doodle",Toy.Hex(0xffd23f));car.transform.SetPositionAndRotation(Ground(c+new Vector3(-10.5f,0,-1))+Vector3.up*.02f,Quaternion.Euler(0,20,0));
            car.transform.SetParent(root);
            var box=car.AddComponent<BoxCollider>();box.center=CarBodies.Envelope.center;box.size=CarBodies.Envelope.size;car.tag="Respawn";
            var chatter=car.AddComponent<NpcChatter>();chatter.speaker="Camper Jo";chatter.title="Bayview campsite";car.AddComponent<ResidentIdle>();chatter.colour=Toy.Sun;
            chatter.lines=new[]{"Shh! Someone's napping in the tent!","Marshmallows are nearly ready!","Oi! This is a campsite, not a car park!"};
        }
        void LookoutPoint()
        {
            var l=Lookout+new Vector3(0,0,-7);
            Put("telescope",l,180);
            Put("bench",l+new Vector3(-2.4f,0,.4f),180);Put("bench",l+new Vector3(2.4f,0,.4f),180);
            Put("picnic_table",l+new Vector3(0,0,4.5f),90);
            Put("flower_bed",l+new Vector3(6,0,3),0,null,.6f);
        }
        void Roadside()
        {
            // Chevrons on the outside of the tighter bends, beyond the walls (13 m), facing traffic.
            for(float d=0;d<track.length;d+=14) {
                float bend=Vector3.Cross(track.Forward(d-6),track.Forward(d+6)).y;
                if(Mathf.Abs(bend)<.2f)continue;
                float outside=-Mathf.Sign(bend);Vector3 p=track.At(d)+track.Right(d)*outside*15.2f;
                if(!land.SolidClearance(p)||!land.SurfacePoint(p,out var hit,false)||hit.point.y<-.5f||hit.point.y>3f)continue;
                var sign=Put("chevron_sign",p,Quaternion.LookRotation(-track.Forward(d)).eulerAngles.y,solid:false);
                // The board's arrows point along its local +X (the driver's left); mirror for right-handers.
                if(bend<0)sign.transform.localScale=new Vector3(-1,1,1);
            }
            // Telegraph poles along the inside of the northern stretch, wires sagging between them.
            Vector3? last=null;
            for(float d=track.length*.30f;d<track.length*.62f;d+=26) {
                Vector3 p=track.At(d)-track.Right(d)*21f;
                if(!land.SolidClearance(p)||Reserved(p)){last=null;continue;}
                var pole=Put("telegraph_pole",p,Quaternion.LookRotation(track.Forward(d)).eulerAngles.y+90);
                var top=pole.transform.position+Vector3.up*6.8f;
                if(last is Vector3 a)foreach(float o in new[]{-.8f,.8f})Wire(a+pole.transform.right*o,top+pole.transform.right*o);
                last=top;
            }
        }
        void Wire(Vector3 a,Vector3 b)
        {
            var go=new GameObject("Telegraph wire");go.transform.SetParent(root);var line=go.AddComponent<LineRenderer>();
            line.positionCount=9;line.widthMultiplier=.04f;line.sharedMaterial=TrialWorld.Material("Telegraph wire",new Color(.12f,.12f,.14f),0,.3f);
            for(int i=0;i<9;i++){float t=i/8f;line.SetPosition(i,Vector3.Lerp(a,b,t)+Vector3.down*Mathf.Sin(t*Mathf.PI)*.9f);}
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
