using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LittleRoad
{
    // The snap tool (docs/73 §4; Richard, 13 Oct: "a screenshot tool so I can drive around and screenshot obvious issues").
    // F9, or hold the right stick in for 0.6 s: a screenshot of what you see, and beside it where you are (position, heading,
    // area, the camera's exact pose, the time of day), the build and the save, so the spot can be found and reproduced
    // exactly. On the keyboard a one-line note can follow (Enter to keep it; F9 or Enter on an empty line to skip).
    // Snaps go to snaps/<date>/ in the game's data folder. On everywhere except batch runs; --no-snaps turns it off.
    public sealed class SnapTool:MonoBehaviour
    {
        static SnapTool instance;
        public static bool Enabled=>!Application.isBatchMode&&!RaceTrial.Has("--no-snaps");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Enabled&&instance==null){var go=new GameObject("Snap tool");DontDestroyOnLoad(go);instance=go.AddComponent<SnapTool>();}}

        // (In the game's own data folder, which the dev build and the play copy share and a rebuild never touches.)
        public static string Folder=>Path.Combine(Application.persistentDataPath,RaceTrial.Has("--snap-test")?"snaps-test":"snaps",DateTime.Now.ToString("yyyy-MM-dd"));   // (the self-test keeps out of the real inbox)
        float held;bool writing;string note="",pending;float savedScale=1,toastUntil;string toast;GUIStyle box,label;

        float openedAt;
        void Update()
        {
            // (Safety exits from the note box: any pad button, or a minute untouched; the note so far is kept.)
            if(writing){var p=Gamepad.current;if(p!=null&&(p.buttonSouth.wasPressedThisFrame||p.buttonEast.wasPressedThisFrame||p.startButton.wasPressedThisFrame)||Time.unscaledTime-openedAt>60)Finish(note.Trim().Length>0);return;}
            var k=Keyboard.current;var pad=Gamepad.current;
            bool key=k?.f9Key.wasPressedThisFrame==true;
            if(pad!=null&&pad.rightStickButton.isPressed){held+=Time.unscaledDeltaTime;}else held=0;
            bool padSnap=held>.6f&&held-Time.unscaledDeltaTime<=.6f;
            if(key||padSnap)Snap(true);   // (a note box after either: type on the keyboard, or skip with F9 or a pad button)
        }

        void Snap(bool canWrite)
        {
            try {
                toast=null;   // (the last snap's "Snapped" message would be in this one's picture)
                Directory.CreateDirectory(Folder);
                string stamp=DateTime.Now.ToString("HHmmss");int n=1;while(File.Exists(Path.Combine(Folder,$"snap-{stamp}-{n}.png")))n++;
                string name=$"snap-{stamp}-{n}";
                File.WriteAllText(Path.Combine(Folder,name+".json"),JsonUtility.ToJson(Describe(name),true));
                Debug.Log($"LITTLE_ROAD_SNAP {name} at {Path.Combine(Folder,name)}");
                // The frame is grabbed first; only then does the note box (or the toast) show, so neither is in the picture.
                StartCoroutine(Capture(Path.Combine(Folder,name+".png"),()=>{
                    // (The game holds still while you type, and its key handlers don't see the letters: the keyboard device is off
                    // for the game until the note is done; the note box still gets the typing.)
                    if(canWrite){pending=name;note="";writing=true;openedAt=Time.unscaledTime;savedScale=Time.timeScale;Time.timeScale=0;muted=Keyboard.current;if(muted!=null)InputSystem.DisableDevice(muted);}
                    else Toast($"Snapped ({name}): it's in the snaps folder");}));
            } catch(Exception e){Debug.LogWarning("LITTLE_ROAD_SNAP failed: "+e.Message);Toast("Snap failed: "+e.Message);}
        }

        // The frame as drawn (as the race captures do: RaceTrial), at the end of this frame.
        System.Collections.IEnumerator Capture(string path,Action then)
        {
            yield return new WaitForEndOfFrame();
            var grab=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);grab.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);grab.Apply(false);
            try{File.WriteAllBytes(path,grab.EncodeToPNG());}catch(Exception e){Debug.LogWarning("LITTLE_ROAD_SNAP image not saved: "+e.Message);}
            Destroy(grab);then?.Invoke();
        }
        [Serializable] public sealed class Record
        {
            public string name,time,build,version,scene,area,save,note;
            public Vector3 carPosition,cameraPosition;public float carHeading,cameraYaw,cameraPitch,cameraFov,hourOfDay,carSpeed,fps;
            public bool racing;public string track;
            // What was running: a challenge or the zip line (free roam); the event, its seed and every entrant (races).
            public string activity,raceEvent;public int raceSeed;public string[] field;
        }
        Record Describe(string name)
        {
            var r=new Record{name=name,time=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),build=Application.buildGUID,version=Application.version,
                scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,hourOfDay=TimeOfDay.Hour,fps=1/Mathf.Max(1e-4f,Time.smoothDeltaTime),
                save=AdventureSession.Store?.path??"",note=""};
            RoadCar car=null;foreach(var c in FindObjectsByType<RoadCar>(FindObjectsSortMode.None))if(c.driverId==0){car=c;break;}
            if(car!=null){r.carPosition=car.transform.position;r.carHeading=car.transform.eulerAngles.y;r.carSpeed=car.speed;}
            var cam=Camera.main;
            if(cam!=null){r.cameraPosition=cam.transform.position;var e=cam.transform.eulerAngles;r.cameraYaw=e.y;r.cameraPitch=e.x;r.cameraFov=cam.fieldOfView;}
            var at=car!=null?car.transform.position:cam!=null?cam.transform.position:Vector3.zero;
            try{r.area=AreaBanner.Area(at);}catch(Exception){r.area="";}
            var race=FindAnyObjectByType<RaceTrial>();r.racing=race!=null&&!race.freeRoam;r.track=r.racing?race.TrackId:"";
            if(r.racing&&race.Roster!=null) {
                var roster=race.Roster;r.raceEvent=string.IsNullOrEmpty(roster.Event)?roster.Title:roster.Event+" ("+roster.Title+")";r.raceSeed=roster.Seed;
                r.field=new string[roster.Entries.Count];
                for(int i=0;i<r.field.Length;i++){var e=roster.Entries[i];r.field[i]=$"slot {e.Slot}: {e.Body} · {e.EquipmentLabel}";}
            }
            r.activity=FindAnyObjectByType<AdventureGame>()?.ActivityTitle??"";
            foreach(var line in ZipLine.All)if(line.Busy)r.activity=line.title;
            return r;
        }

        void Toast(string text){toast=text;toastUntil=Time.unscaledTime+3;}
        // Test (--snap-test): once the town is ready, a snap as if F9 were pressed and the note skipped; pass when its picture
        // and record are both there and the record knows where the car was.
        System.Collections.IEnumerator Start()
        {
            if(!RaceTrial.Has("--snap-test"))yield break;
            while(FindAnyObjectByType<RoadCar>()==null||Time.timeSinceLevelLoad<8)yield return null;
            yield return new WaitForSeconds(2);
            int before=Directory.Exists(Folder)?Directory.GetFiles(Folder,"*.png").Length:0;
            Snap(false);yield return null;yield return null;yield return new WaitForSecondsRealtime(.5f);
            var pngs=Directory.GetFiles(Folder,"*.png");System.Array.Sort(pngs);var png=pngs.Length>0?pngs[^1]:null;
            var json=png!=null?Path.ChangeExtension(png,".json"):null;var r=json!=null&&File.Exists(json)?JsonUtility.FromJson<Record>(File.ReadAllText(json)):null;
            long bytes=png!=null?new FileInfo(png).Length:0;
            bool pass=pngs.Length>before&&bytes>10000&&r!=null&&r.carPosition!=Vector3.zero&&r.cameraFov>0&&!string.IsNullOrEmpty(r.area);
            // (And a note: the files take its slug, the record keeps it, the day's index lists it.)
            string named="";
            try {   // (whatever happens, the test still reports and quits)
                named=pass?Index(Path.GetFileNameWithoutExtension(png),"Bush floating, by the garage!"):"";
                pass&=File.Exists(Path.Combine(Folder,named+".png"))&&named.EndsWith("-bush-floating-by-the-garage")&&JsonUtility.FromJson<Record>(File.ReadAllText(Path.Combine(Folder,named+".json"))).note=="Bush floating, by the garage!"
                    &&File.ReadAllText(Path.Combine(Folder,"index.txt")).Contains(named+".png");
            } catch(Exception e){pass=false;Debug.LogWarning("LITTLE_ROAD_SNAP_TEST note step failed: "+e.Message);}
            Debug.Log($"LITTLE_ROAD_SNAP_TEST pass={pass} named={named} bytes={bytes} area={r?.area} car={r?.carPosition} hour={r?.hourOfDay:0.0}");
            Application.Quit(pass?0:1);
        }

        void OnGUI()
        {
            box??=new GUIStyle(GUI.skin.box){fontSize=22,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(16,16,10,10)};
            label??=new GUIStyle(GUI.skin.label){fontSize=20};
            if(writing) {
                float w=Mathf.Min(900,Screen.width-60);var rect=new Rect((Screen.width-w)/2,Screen.height*.72f,w,110);
                GUI.Box(rect,"",box);
                GUI.Label(new Rect(rect.x+16,rect.y+8,w-32,30),$"Snap {pending}: what's wrong? (Enter to save · F9 or a pad button to skip)",label);
                GUI.SetNextControlName("snap-note");note=GUI.TextField(new Rect(rect.x+16,rect.y+46,w-32,44),note,200,box);GUI.FocusControl("snap-note");
                var e=Event.current;
                if(e.type==EventType.KeyDown&&(e.keyCode==KeyCode.Return||e.keyCode==KeyCode.KeypadEnter||e.keyCode==KeyCode.F9)) {
                    Finish(e.keyCode!=KeyCode.F9&&note.Trim().Length>0);e.Use();
                }
                return;
            }
            if(toast!=null&&Time.unscaledTime<toastUntil)GUI.Box(new Rect(24,Screen.height-84,Mathf.Min(760,Screen.width-48),56),toast,box);
        }
        void Finish(bool keep)
        {
            string name=Index(pending,keep?note.Trim():"");
            writing=false;Time.timeScale=savedScale;Unmute();Toast(keep?$"Snapped with a note ({name})":$"Snapped ({name})");
        }
        // The note goes in the record and, as a short slug, in the files' names (the folder reads as a list of issues:
        // snap-133253-1-bush-floating-by-garage.png); every snap gets a line in the day's index.txt. Returns the final name.
        static string Index(string name,string text)
        {
            string final=name;
            try {
                string json=Path.Combine(Folder,name+".json");var r=JsonUtility.FromJson<Record>(File.ReadAllText(json));
                if(text.Length>0) {
                    r.note=text;string slug=Slug(text);
                    if(slug.Length>0){final=name+"-"+slug;r.name=final;
                        foreach(var ext in new[]{".png",".json"}){var from=Path.Combine(Folder,name+ext);if(File.Exists(from))File.Move(from,Path.Combine(Folder,final+ext));}
                        json=Path.Combine(Folder,final+".json");}
                    File.WriteAllText(json,JsonUtility.ToJson(r,true));Debug.Log($"LITTLE_ROAD_SNAP_NOTE {final}: {text}");
                }
                File.AppendAllText(Path.Combine(Folder,"index.txt"),$"{final}.png | {r.area} | {(r.racing?"race "+r.track:"free roam")} | {r.time} | {(text.Length>0?text:"(no note)")}{Environment.NewLine}");
            } catch(Exception e){Debug.LogWarning("LITTLE_ROAD_SNAP note not saved: "+e.Message);}
            return final;
        }
        // Lower-case words joined by hyphens, letters and digits only, at most 48 characters.
        public static string Slug(string text)
        {
            var sb=new System.Text.StringBuilder();bool gap=false;
            foreach(char c in text.ToLowerInvariant()){if(char.IsLetterOrDigit(c)&&c<128){if(gap&&sb.Length>0)sb.Append('-');sb.Append(c);gap=false;}else gap=true;if(sb.Length>=48)break;}
            return sb.ToString().TrimEnd('-');
        }
        Keyboard muted;void Unmute(){if(muted!=null){InputSystem.EnableDevice(muted);muted=null;}}
        void OnDestroy(){if(writing)Time.timeScale=savedScale;Unmute();if(instance==this)instance=null;}
    }
}
