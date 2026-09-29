using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace LittleRoad
{
    // --contact-review (Richard's test): pairs of real car models placed exactly where their collision
    // boxes touch, nose to tail and side by side, photographed straight down with the boxes outlined.
    // Any daylight between the bodies at "contact" is collision padding a player would see.
    // contact-review.txt lists the visible gap per pair. --collider-width 2.32 shows the old boxes.
    public static class ContactReview
    {
        static readonly string[] Bodies={"puddle","pebble","marlow","sprint","rally","heron"};
        public static IEnumerator Run(string folder)
        {
            Directory.CreateDirectory(folder);TimeOfDay.Set(13.5f);
            var root=new GameObject("Contact review").transform;var origin=new Vector3(400,260,300);
            var outline=TrialWorld.Material("Collision outline",new Color(1,.85f,0),0,.2f,false);outline.EnableKeyword("_EMISSION");outline.SetColor("_EmissionColor",new Color(1,.8f,0)*1.5f);
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="Review deck";ground.transform.SetParent(root);ground.transform.position=origin+new Vector3(3,-.05f,-2);ground.transform.localScale=new Vector3(70,.1f,26);
            ground.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material("Review deck",new Color(.82f,.84f,.86f),0,.1f,false);
            var report=new StringBuilder($"collider width {RaceTrial.Arg("--collider-width","default "+CarBodies.CollisionWidth)}\n");
            for(int i=0;i<Bodies.Length;i++) {
                if(Resources.Load<GameObject>("Cars/"+Bodies[i])==null)continue;
                var at=origin+new Vector3(i*11-25,0,0);
                var a=Place(Bodies[i],at,root,outline);var (centre,size)=RoadCar.CollisionBox(a);
                var behind=Place(Bodies[i],at-new Vector3(0,0,size.z),root,outline);     // nose to tail
                var beside=Place(Bodies[i],at+new Vector3(size.x,0,6),root,outline);      // side by side (second row)
                var a2=Place(Bodies[i],at+new Vector3(0,0,6),root,outline);
                Physics.SyncTransforms();
                var ba=Visual(a);var bb=Visual(behind);var b2=Visual(a2);var bs=Visual(beside);
                float noseTail=ba.min.z-bb.max.z,side=bs.min.x-b2.max.x,shoulder=CarBodies.ShoulderWidth(Bodies[i])??ba.size.x,sideBody=size.x-(shoulder-.02f);
                report.AppendLine($"{Bodies[i],-8} box {size.x:0.00} x {size.z:0.00} m  body {ba.size.x:0.00} x {ba.size.z:0.00} m   nose-to-tail gap {noseTail*100:0} cm   side gap (incl. mirrors) {side*100:0} cm   side gap at the shoulders and tyres {sideBody*100:0} cm");
            }
            yield return null;yield return null;
            var cam=new GameObject("Contact camera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=13;cam.aspect=2.4f;
            cam.transform.SetPositionAndRotation(origin+new Vector3(3,60,-2),Quaternion.Euler(90,0,0));cam.nearClipPlane=1;cam.farClipPlane=120;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.2f,.24f,.3f);
            var rt=new RenderTexture(2880,1200,24);cam.targetTexture=rt;cam.Render();
            var active=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();RenderTexture.active=active;
            File.WriteAllBytes(Path.Combine(folder,"contact-top.png"),image.EncodeToPNG());cam.targetTexture=null;rt.Release();
            File.WriteAllText(Path.Combine(folder,"contact-review.txt"),report.ToString());
            Debug.Log("LITTLE_ROAD_CONTACT_REVIEW\n"+report);
        }
        static GameObject Place(string body,Vector3 at,Transform root,Material outline)
        {
            var car=TrialWorld.Car(body,new Color(.2f,.55f,.8f));car.transform.SetParent(root);car.transform.SetPositionAndRotation(at,Quaternion.identity);
            var (centre,size)=RoadCar.CollisionBox(car);
            // The box's footprint as a thin frame just above the roof line.
            float y=centre.y+size.y*.5f+.02f,hx=size.x*.5f,hz=size.z*.5f,t=.045f;
            foreach(var (p,s) in new[]{(new Vector3(0,y,hz),new Vector3(size.x,.02f,t)),(new Vector3(0,y,-hz),new Vector3(size.x,.02f,t)),(new Vector3(hx,y,0),new Vector3(t,.02f,size.z)),(new Vector3(-hx,y,0),new Vector3(t,.02f,size.z))}) {
                var bar=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.Destroy(bar.GetComponent<Collider>());bar.transform.SetParent(car.transform,false);
                bar.transform.localPosition=centre-new Vector3(0,centre.y,0)+p;bar.transform.localScale=s;bar.GetComponent<Renderer>().sharedMaterial=outline;bar.name="Collision outline";
            }
            return car;
        }
        static Bounds Visual(GameObject car)
        {
            bool first=true;var b=new Bounds();
            foreach(var r in car.GetComponentsInChildren<Renderer>()){if(r.name=="Collision outline"||r is TrailRenderer||!r.enabled)continue;if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);}
            return b;
        }
    }
}
