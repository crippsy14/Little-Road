using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // --valley-tour: a watchable run-through. The player's car drives itself from the Bayview road
    // junction over the causeway, through the Headland Tunnel and up Via Crest to the castle (chase
    // camera, area title cards on), then the camera glides past the valley's sights and quits.
    public sealed class ValleyTour:MonoBehaviour
    {
        RoadCar car;Camera view;AdventureUI ui;
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        public void Setup(RoadCar player,Camera camera,AdventureUI hud){car=player;view=camera;ui=hud;StartCoroutine(Run());}
        IEnumerator Run()
        {
            ui.SetHudVisible(false);view.fieldOfView=58;
            yield return Drive();
            car.commandSource=_=>new Vector2(-.3f,0);
            foreach(var shot in Shots())yield return Glide(shot.name,shot.from,shot.to,shot.lookFrom,shot.lookTo,shot.seconds);
            ui.Toast("That's Hillcrest Valley! · Tour complete");yield return new WaitForSeconds(3);Application.Quit();
        }
        IEnumerator Drive()
        {
            var route=HillcrestRoute.DriveToCastle();
            var start=route[0]+Vector3.up*(.05f-car.ContactBottom);var heading=Flat(route[3]-route[0]);
            car.body.position=start;car.body.rotation=Quaternion.LookRotation(heading);car.body.linearVelocity=car.body.angularVelocity=Vector3.zero;
            car.transform.SetPositionAndRotation(start,car.body.rotation);Physics.SyncTransforms();
            int target=0;
            car.commandSource=c=>{
                while(target<route.Count-1&&Vector3.Distance(Flat(c.body.position),Flat(route[target]))<10)target++;
                var ahead=Flat(route[Mathf.Min(route.Count-1,target+1)]-route[target]).normalized;var aim=route[target]+Vector3.Cross(Vector3.up,ahead)*1.3f-c.body.position;aim.y=0;
                float limit=22;
                for(int k=target;k<Mathf.Min(route.Count-6,target+15);k++){float turn=Mathf.Abs(Vector3.SignedAngle(Flat(route[k+1]-route[k]),Flat(route[k+6]-route[k+5]),Vector3.up))*Mathf.Deg2Rad;limit=Mathf.Min(limit,Mathf.Sqrt(5.5f*10/Mathf.Max(turn,.01f))+(k-target)*.35f);}
                float pedal=target>=route.Count-3?.3f:c.speed>limit+1?-.8f:c.speed>limit?0:1;
                return new Vector2(pedal,Mathf.Clamp(Vector3.SignedAngle(Flat(c.transform.forward),aim,Vector3.up)/25,-1,1));};
            float t=0,stuck=0;Vector3 camPos=car.transform.position-car.transform.forward*7+Vector3.up*3.4f;
            while(t<260&&Vector3.Distance(Flat(car.body.position),Flat(HillcrestRoute.CastleCourt))>16) {
                yield return null;t+=Time.deltaTime;
                // Never stall the tour: after 5 s stuck, hop 15 m along the route.
                stuck=Mathf.Abs(car.speed)<1?stuck+Time.deltaTime:0;
                if(stuck>5){stuck=0;int next=Mathf.Min(route.Count-2,target+8);var p=route[next]+Vector3.up*(.3f-car.ContactBottom);car.body.position=p;car.body.rotation=Quaternion.LookRotation(Flat(route[next+1]-route[next]));car.body.linearVelocity=Vector3.zero;target=next;}
                var f=Flat(car.transform.forward).normalized;var goal=car.transform.position-f*7.5f+Vector3.up*3.2f;
                camPos=Vector3.Lerp(camPos,goal,1-Mathf.Exp(-Time.deltaTime*4));view.transform.position=camPos;view.transform.LookAt(car.transform.position+Vector3.up*1.1f+f*3);
            }
        }
        struct Shot{public string name;public Vector3 from,to,lookFrom,lookTo;public float seconds;}
        static Shot S(string name,Vector3 from,Vector3 to,Vector3 lookFrom,Vector3 lookTo,float seconds=7)=>new Shot{name=name,from=from,to=to,lookFrom=lookFrom,lookTo=lookTo,seconds=seconds};
        IEnumerable<Shot> Shots()
        {
            var sq=HillcrestRoute.Square;var castle=HillcrestRoute.CastleCourt;var main=HillcrestRoute.Mainline;float south=HillcrestRoute.PortalSouthRun;
            var reveal=main.At(south+6)+Vector3.up*3.2f;var revealLook=main.At(south+6)+main.Forward(south+6)*200+Vector3.down*6;
            yield return S("HILLCREST CASTLE",castle+new Vector3(-60,35,40),castle+new Vector3(60,30,40),castle,castle+Vector3.up*6,8);
            yield return S("MARKET SQUARE",sq+new Vector3(-40,22,-10),sq+new Vector3(-10,9,-30),sq,sq+new Vector3(12,2,4),8);
            yield return S("VIA CREST",HillcrestRoute.Crest.At(120)+Vector3.up*3,HillcrestRoute.Crest.At(190)+Vector3.up*3,HillcrestRoute.Crest.At(150)+Vector3.up*2,HillcrestRoute.Crest.At(230)+Vector3.up*2,8);
            yield return S("THE QUAY",new Vector3(-40,9,-905),new Vector3(200,9,-915),new Vector3(60,5,-910),new Vector3(320,5,-925),9);
            yield return S("HILLCREST STATION",new Vector3(400,40,-960),new Vector3(430,25,-1080),new Vector3(480,8,-1030),new Vector3(510,8,-1040),8);
            yield return S("SILVER FALLS",HillcrestRoute.Lookout+new Vector3(-40,30,-40),HillcrestRoute.Lookout+new Vector3(-5,8,-10),HillcrestRoute.FallsHead,HillcrestRoute.FallsHead+new Vector3(10,-6,20),8);
            yield return S("HILLCREST RACEWAY",new Vector3(-240,50,-1000),new Vector3(-560,40,-1030),new Vector3(-400,8,-1180),new Vector3(-450,8,-1250),9);
            yield return S("VALLEY FIELDS",new Vector3(560,60,-1080),new Vector3(820,40,-1250),new Vector3(700,8,-1250),new Vector3(800,8,-1350),8);
            yield return S("VALLEY VIEWPOINT",reveal,HillcrestRoute.Vista+new Vector3(-4,4,4),revealLook,new Vector3(200,30,-1100),9);
            yield return S("THE SEA VIADUCT",new Vector3(60,14,-300),new Vector3(170,20,-360),new Vector3(130,12,-360),new Vector3(125,22,-430),8);
            yield return S("BAYVIEW HALT",new Vector3(150,8,-200),new Vector3(66,9,-100),new Vector3(100,2,-150),new Vector3(92,1,-138),8);
            yield return S("HILLCREST VALLEY",new Vector3(40,380,-380),new Vector3(160,520,-560),new Vector3(150,0,-1000),new Vector3(160,0,-1150),10);
        }
        IEnumerator Glide(string name,Vector3 from,Vector3 to,Vector3 lookFrom,Vector3 lookTo,float seconds)
        {
            ui.Toast(name);
            for(float t=0;t<seconds;t+=Time.deltaTime) {
                float u=Mathf.SmoothStep(0,1,t/seconds);
                view.transform.position=Vector3.Lerp(from,to,u);view.transform.LookAt(Vector3.Lerp(lookFrom,lookTo,u));yield return null;
            }
        }
    }
}
