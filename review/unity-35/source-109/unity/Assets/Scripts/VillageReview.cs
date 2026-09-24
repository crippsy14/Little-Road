using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LittleRoad
{
    // Captures the actual adventure scene. Review poses are explicit; no scenery
    // is substituted and no generated illustration is used as game evidence.
    public sealed class VillageReview:MonoBehaviour
    {
        [Serializable] public sealed class Evidence {public string scope="Static car-sized clearance and road/shoulder support samples, not continuous collision proof";public int roadSamples,garageSamples,forecourtSamples,supportSamples;public List<string> obstructions=new();public bool passed;}
        [Serializable] public sealed class DrivingEvidence {public string scope="Physical garage entry and reverse exit using synthetic trigger input; not a manual route playtest";public bool entered,exited,passed;public int contacts,recoveries;public float entryZ,exitZ,maxLateralError;public Vector3 exitPosition,exitVelocity;public string[] exitContacts;}
        AdventureUI ui;Camera cameraView;RoadCar player;string folder;
        IEnumerator Start()
        {
            folder=RaceTrial.Arg("--trial-captures");if(string.IsNullOrEmpty(folder))throw new Exception("World review requires --trial-captures");Directory.CreateDirectory(folder);
            var world=GetComponent<RaceTrial>();player=world.cars[0];ui=GetComponent<AdventureUI>();cameraView=Camera.main;
            Place(player.body.position,player.body.rotation.eulerAngles.y);
            yield return new WaitForSeconds(.3f);ui.Capture(Path.Combine(folder,"01-default-chase.png"));
            ui.SetVisible(false);
            yield return Frame("02-village-approach",new Vector3(-39,.10f,-91),270,new Vector3(-28,4,-88),new Vector3(-66,2.2f,-104));
            yield return Frame("03-garage-forecourt",new Vector3(-66,.10f,-99),180,new Vector3(-69,3.4f,-88),new Vector3(-65,2.6f,-108));
            yield return Frame("04-open-workshop",new Vector3(-65,.10f,-108),180,new Vector3(-66.5f,2.9f,-100),new Vector3(-65,1.9f,-112));
            yield return Frame("05-village-wide",new Vector3(-65,.10f,-98),180,new Vector3(-14,15,-70),new Vector3(-42,2,-110));
            yield return Frame("06-coastal-road",world.track.At(570)+Vector3.up*.1f,0,new Vector3(-103,6,-44),new Vector3(-117,2,15));
            float bridge=world.track.Nearest(new Vector3(-117,0,9),out _);
            var forward=world.track.Forward(bridge);var right=world.track.Right(bridge);var centre=world.track.At(bridge);
            yield return Frame("07-bridge-approach",world.track.At(bridge-23),Quaternion.LookRotation(world.track.Forward(bridge-23)).eulerAngles.y,world.track.At(bridge-34)+Vector3.up*4,centre+Vector3.up*1.2f);
            yield return Frame("08-bridge-profile",centre,Quaternion.LookRotation(forward).eulerAngles.y,centre+right*43+forward*15+Vector3.up*5,centre+Vector3.down*4);
            var water=FindFirstObjectByType<CoastalWater>();
            if(water==null||!water.Surface.shader.isSupported)throw new Exception("Coastal water missing or unsupported");
            water.Surface.SetFloat("_ReviewTime",0);
            yield return Frame("09-water-time-0",centre,Quaternion.LookRotation(forward).eulerAngles.y,centre+right*43+forward*15+Vector3.down*3,centre+Vector3.down*10);
            water.Surface.SetFloat("_ReviewTime",1.6f);
            yield return Frame("10-water-time-1-6",centre,Quaternion.LookRotation(forward).eulerAngles.y,centre+right*43+forward*15+Vector3.down*3,centre+Vector3.down*10);
            var coast=world.track.At(554);var coastForward=world.track.Forward(554);
            water.Surface.SetFloat("_ReviewTime",0);
            yield return Frame("11-distant-water-time-0",coast,Quaternion.LookRotation(coastForward).eulerAngles.y,coast-coastForward*10+Vector3.up*4,coast+coastForward*10+Vector3.up);
            water.Surface.SetFloat("_ReviewTime",1.6f);
            yield return Frame("12-distant-water-time-1-6",coast,Quaternion.LookRotation(coastForward).eulerAngles.y,coast-coastForward*10+Vector3.up*4,coast+coastForward*10+Vector3.up);
            water.Surface.SetFloat("_ReviewTime",-1);
            if(RaceTrial.Has("--cliff-study"))yield return Frame("13-cliff-driving-distance",world.track.At(bridge),Quaternion.LookRotation(forward).eulerAngles.y,new Vector3(-115,3.5f,-16),new Vector3(-97,7,-7));
            var evidence=new Evidence();Physics.SyncTransforms();
            var lane=GameObject.Find("Lighthouse lane");
            if(lane==null)evidence.obstructions.Add("Missing lighthouse access lane");
            else foreach(Vector3 vertex in lane.GetComponent<MeshFilter>().sharedMesh.vertices) {
                Vector3 p=lane.transform.TransformPoint(vertex);float d=world.track.Nearest(p,out _);
                if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(world.track.At(d).x,world.track.At(d).z))<11.8f)
                    evidence.obstructions.Add("Lighthouse lane geometry intrudes inside outer road shoulder");
            }
            for(float d=0;d<world.track.length;d+=4) {
                evidence.roadSamples++;var hits=Physics.OverlapBox(world.track.At(d)+Vector3.up, new Vector3(7.8f,.8f,1.5f),Quaternion.LookRotation(world.track.Forward(d)));
                foreach(var hit in hits)if(hit.CompareTag("Respawn"))evidence.obstructions.Add("Road "+d.ToString("0.0")+": "+hit.name);
            }
            for(float z=-99;z>=-109;z-=1) {
                evidence.garageSamples++;var hits=Physics.OverlapBox(new Vector3(-65,.9f,z),new Vector3(1.25f,.75f,1.8f),Quaternion.identity);
                foreach(var hit in hits)if(hit.CompareTag("Respawn"))evidence.obstructions.Add("Garage "+z+": "+hit.name);
            }
            for(float x=-72;x<= -20;x+=2) {
                evidence.forecourtSamples++;var hits=Physics.OverlapBox(new Vector3(x,.9f,-95),new Vector3(1.35f,.75f,2),Quaternion.Euler(0,90,0));
                foreach(var hit in hits)if(hit.CompareTag("Respawn"))evidence.obstructions.Add("Forecourt "+x+": "+hit.name);
            }
            // Reject a visible road/shoulder over an unsupported void, and
            // terrain projecting through the road. Exclude the review car.
            for(float d=0;d<world.track.length;d+=2)foreach(float offset in new[]{-11f,-7f,0,7f,11f}) {
                evidence.supportSamples++;Vector3 point=world.track.At(d)+world.track.Right(d)*offset;
                var hits=Physics.RaycastAll(point+Vector3.up*2,Vector3.down,4).Where(h=>h.collider.GetComponentInParent<RoadCar>()==null).OrderBy(h=>h.distance).ToArray();
                if(hits.Length==0||Mathf.Abs(hits[0].point.y)>.08f)evidence.obstructions.Add("Road support "+d+" offset "+offset+": "+(hits.Length==0?"missing surface":hits[0].collider.name+" height "+hits[0].point.y));
            }
            evidence.obstructions=evidence.obstructions.Distinct().ToList();evidence.passed=evidence.obstructions.Count==0;
            File.WriteAllText(Path.Combine(folder,"clearance.json"),JsonUtility.ToJson(evidence,true));
            yield return DriveGarage();
            Debug.Log("LITTLE_ROAD_WORLD_REVIEW_COMPLETE clearance="+evidence.passed+" folder="+folder);Application.Quit(evidence.passed&&drivePassed?0:2);
        }
        bool drivePassed;
        void Place(Vector3 position,float heading)
        {
            var box=player.GetComponent<BoxCollider>();position.y=box.size.y*.5f-box.center.y+.015f;
            player.body.position=position;player.body.rotation=Quaternion.Euler(0,heading,0);player.body.linearVelocity=Vector3.zero;player.body.angularVelocity=Vector3.zero;
            // Review teleports must synchronise the render transform as well
            // as the physics pose before a camera capture requests culling.
            player.transform.SetPositionAndRotation(position,player.body.rotation);Physics.SyncTransforms();
        }
        IEnumerator Frame(string name,Vector3 carPosition,float heading,Vector3 cameraPosition,Vector3 target)
        {
            Place(carPosition,heading);
            cameraView.transform.position=cameraPosition;cameraView.transform.LookAt(target);yield return new WaitForFixedUpdate();yield return new WaitForSeconds(.5f);
            if(Vector3.Distance(player.body.position,player.transform.position)>.05f)throw new Exception("Review physics/render poses diverged: "+name);
            ui.Capture(Path.Combine(folder,name+".png"));
            Debug.Log("WORLD_FRAME "+name+" body="+player.body.position+" render="+player.transform.position+" visibleRenderers="+player.GetComponentsInChildren<Renderer>().Count(r=>r.isVisible));
        }
        IEnumerator DriveGarage()
        {
            var result=new DrivingEvidence();Place(new Vector3(-65,0,-98),180);
            var probe=player.gameObject.AddComponent<GarageContactReview>();
            yield return new WaitForSeconds(.1f);player.controlsBlocked=false;
            int initialContacts=player.contacts,initialRecoveries=player.recoveries;
            var pad=InputSystem.AddDevice<Gamepad>("GarageClearanceTest");
            try {
                float deadline=Time.time+14;
                while(player.body.position.z> -107&&Time.time<deadline) {
                    InputSystem.QueueStateEvent(pad,new GamepadState{rightTrigger=player.speed<2?.30f:0});
                    result.maxLateralError=Mathf.Max(result.maxLateralError,Mathf.Abs(player.body.position.x+65));yield return new WaitForFixedUpdate();
                }
                result.entryZ=player.body.position.z;result.entered=result.entryZ<= -107;
                InputSystem.QueueStateEvent(pad,new GamepadState{leftTrigger=1});
                deadline=Time.time+14;
                while(player.body.position.z< -98&&Time.time<deadline) {
                    InputSystem.QueueStateEvent(pad,new GamepadState{leftTrigger=player.speed> -2?.30f:0});
                    result.maxLateralError=Mathf.Max(result.maxLateralError,Mathf.Abs(player.body.position.x+65));yield return new WaitForFixedUpdate();
                }
                result.exitZ=player.body.position.z;result.exited=result.exitZ>= -98;
                result.exitPosition=player.body.position;result.exitVelocity=player.body.linearVelocity;result.exitContacts=probe.contacts.ToArray();
            }finally {InputSystem.RemoveDevice(pad);player.controlsBlocked=true;}
            result.contacts=player.contacts-initialContacts;result.recoveries=player.recoveries-initialRecoveries;
            result.passed=result.entered&&result.exited&&result.contacts==0&&result.recoveries==0&&result.maxLateralError<.3f;
            drivePassed=result.passed;File.WriteAllText(Path.Combine(folder,"garage-driving.json"),JsonUtility.ToJson(result,true));
            Debug.Log("LITTLE_ROAD_GARAGE_DRIVING passed="+result.passed);
        }
    }
    public sealed class GarageContactReview:MonoBehaviour
    {
        public readonly HashSet<string> contacts=new();
        void OnCollisionStay(Collision collision)
        {
            foreach(var point in collision.contacts)if(point.normal.y<.9f)
                contacts.Add(collision.collider.name+" normal="+point.normal.ToString("F3")+" point="+point.point.ToString("F3"));
        }
    }
}
