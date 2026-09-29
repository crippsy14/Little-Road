using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Pit Stop: the services base every town has, identical everywhere so it is recognised on
    // sight (source-art pit_stop_2 / pit_stop_3): an orange wing roof, chequered band, colour-coded
    // bays with icon badges and a roadside pylon carrying a giant spinning tyre.
    //   Garage (teal): the town's local race, fitting owned parts, swapping owned bodies.
    //   Parts (yellow): buying performance parts from the town's stock.
    //   Body shop (pink): new models (looks only) and paint. Not in Bayview, the tutorial town.
    // Hosts stand at each bay's HostAnchor; AdventureGame turns them into residents.
    public static class PitStop
    {
        public static readonly Color Brand=Toy.Hex(0xff6a10),Garage=Toy.Hex(0x14b8a8),Parts=Toy.Hex(0xffbd10),Body=Toy.Hex(0xff57a6);
        public const string Signs="brand=PIT\nSTOP|garage=GARAGE|parts=PARTS|body=BODY SHOP";
        // "bayview/garage" -> where that bay's host stands and faces.
        public static readonly Dictionary<string,(Vector3 at,float yaw)> Hosts=new();
        public static readonly Dictionary<string,Vector3> Sites=new();
        public static void Reset(){Hosts.Clear();Sites.Clear();}
        public static GameObject Build(string town,bool bodyShop,Vector3 at,float yaw,Transform parent)
        {
            var go=BayviewKit.Spawn(bodyShop?"pit_stop_3":"pit_stop_2",at,yaw,parent,null,Signs);go.name="Pit Stop "+town;
            // The tyre on the pylon rolls steadily (KitSpin), a little quicker than the windmills.
            foreach(var spin in go.GetComponentsInChildren<KitSpin>())spin.degreesPerSecond=40;
            foreach(var t in go.GetComponentsInChildren<Transform>()) {
                if(!t.name.StartsWith("HostAnchor_"))continue;
                // Hosts park beside their bay, nose out to the forecourt.
                Hosts[town+"/"+t.name.Substring("HostAnchor_".Length)]=(t.position,yaw);
                Debug.Log($"LITTLE_ROAD_PIT_HOST {town}/{t.name} at={t.position} local={go.transform.InverseTransformPoint(t.position)}");
            }
            Sites[town]=at;
            Debug.Log($"LITTLE_ROAD_PIT_STOP town={town} bays={(bodyShop?3:2)} at={at} hosts={Hosts.Count}");
            return go;
        }
    }
}
