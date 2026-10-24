using UnityEngine;

namespace LittleRoad
{
    // A circuit's race-day dressing (grid boxes, gantry, kerbs, bale walls, spectators, corner boards), shown only while a race
    // is on: in free roam the lanes are the village's lanes again (Richard's snaps, 24 Oct). The world persists between free
    // roam and a race (PersistentWorld), so the holder follows the race state as it changes rather than being built per race.
    // --race-dressing shows it in free roam too.
    public sealed class RaceOnly:MonoBehaviour
    {
        public GameObject show;
        public static bool Showing=>RaceTrial.Racing||RaceTrial.Has("--race-dressing");
        void Update(){if(show==null)return;bool on=Showing;if(show.activeSelf!=on)show.SetActive(on);}
    }
}
