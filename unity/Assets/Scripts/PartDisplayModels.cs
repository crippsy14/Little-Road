using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Original reusable display meshes for the six non-engine categories.
    // Shape/tier colour identifies equipment; presentation never supplies physics values.
    public static class PartDisplayModels
    {
        public static GameObject Create(EquipmentCatalog.Part part,Transform parent,List<Material> materials,List<Mesh> meshes)
        {
            var root=new GameObject(part.id+" display model");root.transform.SetParent(parent,false);
            Material Make(string name,Color color,float metal=.6f){var m=TrialWorld.Material(name,color,metal,.38f);materials.Add(m);return m;}
            var alloy=Make("Display alloy",new Color(.56f,.64f,.65f));var dark=Make("Display rubber",new Color(.045f,.065f,.07f),0);
            var accent=Make("Display equipment finish",part.shop=="hillcrest"?new Color(.52f,.26f,.78f):part.id.Contains("club")||part.id.Contains("race")?new Color(.86f,.31f,.15f):new Color(.10f,.46f,.43f));
            GameObject Primitive(PrimitiveType type,string name,Vector3 position,Vector3 scale,Material material,Vector3 rotation=default)
            {var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root.transform,false);go.transform.localPosition=position;go.transform.localScale=scale;go.transform.localEulerAngles=rotation;go.GetComponent<Renderer>().sharedMaterial=material;return go;}
            void Box(string name,Vector3 p,Vector3 s,Material m,Vector3 rotation=default)=>Primitive(PrimitiveType.Cube,name,p,s,m,rotation);
            void Cylinder(string name,Vector3 p,float radius,float height,Material m,Vector3 rotation=default)=>Primitive(PrimitiveType.Cylinder,name,p,new Vector3(radius*2,height*.5f,radius*2),m,rotation);
            GameObject Ring(string name,Vector3 p,float radius,float tube,Material m,Vector3 rotation=default)
            {
                const int n=40,k=12;var vertices=new Vector3[n*k];var triangles=new int[n*k*6];
                for(int i=0;i<n;i++)for(int j=0;j<k;j++){
                    float u=i*Mathf.PI*2/n,v=j*Mathf.PI*2/k;vertices[i*k+j]=new Vector3((radius+tube*Mathf.Cos(v))*Mathf.Cos(u),(radius+tube*Mathf.Cos(v))*Mathf.Sin(u),tube*Mathf.Sin(v));
                    int a=i*k+j,b=((i+1)%n)*k+j,c=((i+1)%n)*k+(j+1)%k,d=i*k+(j+1)%k,t=(i*k+j)*6;
                    triangles[t]=a;triangles[t+1]=b;triangles[t+2]=c;triangles[t+3]=a;triangles[t+4]=c;triangles[t+5]=d;
                }
                var mesh=new Mesh{name="Paddock "+name,vertices=vertices,triangles=triangles};mesh.RecalculateNormals();meshes.Add(mesh);
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);go.transform.localPosition=p;go.transform.localEulerAngles=rotation;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=m;return go;
            }
            var axle=new Vector3(90,0,0);
            switch(part.slot){
                case "turbo":
                    Ring("Compressor housing",new Vector3(0,.62f,0),.40f,.16f,alloy);Cylinder("Compressor inlet",new Vector3(0,.62f,.12f),.26f,.26f,dark,axle);
                    Ring("Inlet lip",new Vector3(0,.62f,.26f),.24f,.035f,alloy);Cylinder("Outlet pipe",new Vector3(.48f,.8f,0),.15f,.54f,alloy,new Vector3(0,0,-35));
                    Cylinder("Turbine housing",new Vector3(0,.62f,-.37f),.34f,.36f,accent,axle);Box("Mount",new Vector3(0,.17f,-.13f),new Vector3(.45f,.13f,.4f),alloy);break;
                case "tyres":
                    Ring("Tyre",new Vector3(0,.72f,0),.54f,.18f,dark);Ring("Rim lip",new Vector3(0,.72f,.14f),.37f,.04f,alloy);Cylinder("Wheel hub",new Vector3(0,.72f,0),.12f,.32f,accent,axle);
                    for(int i=0;i<6;i++){float a=i*60*Mathf.Deg2Rad;Box("Alloy spoke",new Vector3(Mathf.Cos(a)*.22f,.72f+Mathf.Sin(a)*.22f,.04f),new Vector3(.43f,.065f,.12f),alloy,new Vector3(0,0,i*60));}
                    for(int i=0;i<30;i++){float a=i*Mathf.PI/15;Box("Tread block",new Vector3(Mathf.Cos(a)*.706f,.72f+Mathf.Sin(a)*.706f,0),new Vector3(.065f,.024f,.18f),dark,new Vector3(0,0,i*12-90));}break;
                case "transmission":
                    Box("Gearbox housing",new Vector3(0,.46f,0),new Vector3(.8f,.66f,1.15f),alloy);Cylinder("Bell housing",new Vector3(0,.46f,.69f),.53f,.30f,alloy,axle);Cylinder("Output shaft",new Vector3(0,.46f,-.85f),.13f,.58f,dark,axle);
                    for(int i=0;i<7;i++)Box("Cooling rib",new Vector3(0,.48f,-.45f+i*.14f),new Vector3(.87f,.72f,.038f),accent);break;
                case "brakes":
                    Cylinder("Brake rotor",new Vector3(0,.67f,0),.61f,.08f,alloy,axle);Cylinder("Rotor hub",new Vector3(0,.67f,.07f),.23f,.16f,dark,axle);Box("Brake caliper",new Vector3(.50f,.73f,0),new Vector3(.29f,.59f,.35f),accent);
                    for(int i=0;i<16;i++){float a=i*Mathf.PI/8;Cylinder("Rotor vent",new Vector3(Mathf.Cos(a)*.44f,.67f+Mathf.Sin(a)*.44f,.046f),.028f,.005f,dark,axle);}break;
                case "steering":
                    Cylinder("Steering rack",new Vector3(0,.36f,0),.13f,2.45f,alloy,new Vector3(0,0,90));Box("Rack housing",new Vector3(-.1f,.36f,0),new Vector3(.72f,.34f,.34f),accent);Cylinder("Pinion shaft",new Vector3(-.22f,.75f,0),.085f,.70f,alloy,new Vector3(0,0,-15));
                    for(int side=-1;side<=1;side+=2)for(int i=0;i<6;i++)Ring("Rack boot",new Vector3(side*(.52f+i*.065f),.36f,0),.16f,.025f,dark,new Vector3(0,90,0));break;
                case "chassis":
                    for(int side=-1;side<=1;side+=2)Box("Frame rail",new Vector3(side*.58f,.15f,0),new Vector3(.13f,.20f,2.3f),accent);
                    for(int i=-1;i<=1;i++)Box("Crossmember",new Vector3(0,.15f,i*.86f),new Vector3(1.25f,.16f,.14f),alloy);
                    Box("Diagonal brace",new Vector3(0,.15f,0),new Vector3(.09f,.13f,2),alloy,new Vector3(0,35,0));Box("Diagonal brace",new Vector3(0,.15f,0),new Vector3(.09f,.13f,2),alloy,new Vector3(0,-35,0));break;
                default:throw new InvalidOperationException("No authored part display for "+part.slot);
            }
            return root;
        }
    }
}
