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
        public static bool Reserved(Vector3 p)=>Flat(p,Farm)<30||Flat(p,Camp)<14||Flat(p,Lookout)<12;
        static float Flat(Vector3 a,Vector3 b)=>new Vector2(a.x-b.x,a.z-b.z).magnitude;
        readonly CoastalLandscape land;readonly Circuit track;readonly Transform root;
        public BayviewCountryside(CoastalLandscape landscape,Circuit circuit,Transform parent){land=landscape;track=circuit;root=parent;}
        Vector3 Ground(Vector3 p){p.y=land.SurfacePoint(p,out var hit,false)?hit.point.y:land.Height(p.x,p.z);return p;}
        GameObject Put(string id,Vector3 p,float yaw,BayviewKit.Tint tint=null,float scale=1,bool solid=true)=>BayviewKit.Spawn(id,Ground(p)+Vector3.down*.05f,yaw,root,tint,solid:solid,scale:scale);
        static BayviewKit.Tint T(int wall,int shutter,int awning,int door)=>new(Toy.Hex(wall),Toy.Hex(shutter),Toy.Hex(awning),Toy.Hex(door));
        public void Build()
        {
            if(!BayviewKit.Has("barn"))return;
            FarmYard();Campsite();LookoutPoint();Roadside();
        }
        void FarmYard()
        {
            var f=Farm;
            Put("house_a",f+new Vector3(-9,0,8),200,T(0xfff3dc,0x2e9e5b,0xff4d5e,0x2e9e5b));
            Put("barn",f+new Vector3(8,0,10),160,T(0xd94040,0xffffff,0xffffff,0xd94040));
            Put("tractor",f+new Vector3(1,0,1),35);
            foreach(var (x,z) in new[]{(12f,2f),(13.6f,2.4f),(12.8f,.4f),(16f,5f)})Put("hay_bale",f+new Vector3(x,0,z),x*40);
            Field(f+new Vector3(-14,0,-10),new Vector2(18,12),12,Toy.Hex(0x9a7be0),Toy.Hex(0x5c9e45));
            Field(f+new Vector3(8,0,-12),new Vector2(16,11),-8,Toy.Hex(0xf2c94c),Toy.Hex(0xe0a93a));
            Field(f+new Vector3(-2,0,-24),new Vector2(20,9),4,Toy.Hex(0x4caf50),Toy.Hex(0x8d5a34));
            // Picket fence along the yard's road-facing side.
            for(int i=0;i<6;i++)Put("fence",f+new Vector3(-17+i*6,0,15),0);
        }
        // Crop rows draped on the terrain: alternating strips in two colours, no collider.
        void Field(Vector3 centre,Vector2 size,float yaw,Color crop,Color gap)
        {
            var rotation=Quaternion.Euler(0,yaw,0);int rows=Mathf.RoundToInt(size.y/1.1f),cells=Mathf.CeilToInt(size.x/1.5f);
            var meshes=new[]{new List<Vector3>(),new List<Vector3>()};var tris=new[]{new List<int>(),new List<int>()};
            for(int r=0;r<rows;r++) {
                int k=r%2;float z0=-size.y/2+r*size.y/rows,z1=z0+size.y/rows;
                int start=meshes[k].Count;
                for(int c=0;c<=cells;c++) {
                    float x=-size.x/2+c*size.x/cells;
                    foreach(float z in new[]{z0,z1}){var p=Ground(centre+rotation*new Vector3(x,0,z));meshes[k].Add(p+Vector3.up*(k==0?.12f:.06f));}
                }
                for(int c=0;c<cells;c++){int a=start+c*2;tris[k].AddRange(new[]{a,a+1,a+2,a+2,a+1,a+3});}
            }
            for(int k=0;k<2;k++) {
                var mesh=new Mesh{name="Field rows"};mesh.SetVertices(meshes[k]);mesh.SetTriangles(tris[k],0);mesh.RecalculateNormals();
                var go=new GameObject(k==0?"Field crop":"Field furrow",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=TrialWorld.Material(go.name,k==0?crop:gap,0,.1f);
            }
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
            var chatter=car.AddComponent<NpcChatter>();chatter.speaker="Camper Jo";chatter.colour=Toy.Sun;
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
