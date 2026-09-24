using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Physical set around the model; the reveal is rendered by the preview camera.
    // Owned by PartPreviewStage, including every material and primitive collider.
    public static class CarShowroom
    {
        public static GameObject Create(Transform parent,List<Material> materials,List<Mesh> meshes)
        {
            var room=new GameObject("Car reveal studio");room.transform.SetParent(parent,false);
            Material Surface(string name,Color color,float metal=0,float smooth=.3f){var m=TrialWorld.Material(name,color,metal,smooth);materials.Add(m);return m;}
            Material Strip(string name,Color color){var m=Surface(name,color*.2f);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color);return m;}
            var floor=Surface("Graphite studio floor",new Color(.026f,.038f,.045f),.35f,.65f);
            var wall=Surface("Midnight acoustic wall",new Color(.017f,.024f,.032f));
            var rib=Surface("Blue black wall ribs",new Color(.042f,.065f,.078f),.25f);
            var teal=Strip("Mint reveal strips",new Color(.34f,.91f,.83f));
            var white=Strip("Warm softbox panels",new Color(1,.91f,.72f));
            GameObject Shape(string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material){var o=GameObject.CreatePrimitive(type);o.name=name;o.layer=31;o.transform.SetParent(room.transform,false);o.transform.localPosition=position;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=material;var c=o.GetComponent<Collider>();c.enabled=false;Object.Destroy(c);return o;}
            Shape("Studio floor",PrimitiveType.Cube,new Vector3(0,-.21f,0),new Vector3(24,.12f,24),floor);
            Shape("Rear wall",PrimitiveType.Cube,new Vector3(0,3,-4),new Vector3(24,7,.2f),wall);
            for(int i=-12;i<=12;i++)Shape("Wall rib",PrimitiveType.Cube,new Vector3(i*.58f,2.8f,-3.84f),new Vector3(.055f,6,.10f),rib);
            Shape("Wall horizon light",PrimitiveType.Cube,new Vector3(0,.32f,-3.70f),new Vector3(24,.035f,.025f),teal);
            foreach(int side in new[]{-1,1}){
                var bar=Shape("Standing softbox",PrimitiveType.Cube,new Vector3(side*3.35f,1.65f,-2.8f),new Vector3(.09f,2.9f,.06f),white);bar.transform.localRotation=Quaternion.Euler(0,0,-side*18);
                Shape("Floor guide light",PrimitiveType.Cube,new Vector3(side*3.05f,-.14f,.4f),new Vector3(.035f,.015f,7.4f),teal);
            }
            // An illuminated ring follows the real turntable edge, with fixed index marks.
            for(int i=0;i<96;i++){
                float angle=i*Mathf.PI*2/96;
                var edge=Shape("Turntable rim",PrimitiveType.Cube,new Vector3(Mathf.Sin(angle)*1.895f,-.054f,Mathf.Cos(angle)*1.895f),new Vector3(.128f,.023f,.022f),teal);
                edge.transform.localRotation=Quaternion.Euler(0,angle*Mathf.Rad2Deg,0);
            }
            for(int i=0;i<12;i++){
                float angle=i*Mathf.PI/6;
                var marker=Shape("Deck index",PrimitiveType.Cube,new Vector3(Mathf.Sin(angle)*1.76f,-.037f,Mathf.Cos(angle)*1.76f),new Vector3(.025f,.006f,.13f),white);
                marker.transform.localRotation=Quaternion.Euler(0,angle*Mathf.Rad2Deg,0);
            }
            // Static set pieces become one mesh per finish, rather than hundreds
            // of separate renderer submissions during the rotating reveal.
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(var filter in room.GetComponentsInChildren<MeshFilter>()){
                var material=filter.GetComponent<Renderer>().sharedMaterial;
                if(!groups.ContainsKey(material))groups[material]=new List<CombineInstance>();
                groups[material].Add(new CombineInstance{mesh=filter.sharedMesh,transform=Matrix4x4.TRS(filter.transform.localPosition,filter.transform.localRotation,filter.transform.localScale)});
                filter.gameObject.SetActive(false);Object.Destroy(filter.gameObject);
            }
            foreach(var group in groups){
                var mesh=new Mesh{name="Combined showroom "+group.Key.name};mesh.CombineMeshes(group.Value.ToArray());meshes.Add(mesh);
                var batch=new GameObject(mesh.name);batch.layer=31;batch.transform.SetParent(room.transform,false);batch.AddComponent<MeshFilter>().sharedMesh=mesh;batch.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
            }
            room.SetActive(false);return room;
        }
    }
}
