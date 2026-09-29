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
    public sealed class RacePreviewReview:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void FailFast(){if(RaceTrial.Has("--race-preview-review"))Application.logMessageReceived+=(message,stack,type)=>{if(type==LogType.Exception)Application.Quit(2);};}
        RacePreviewUI ui;RaceTrial race;Gamepad pad;Keyboard keyboard;readonly List<string> checks=new();
        [Serializable] sealed class Evidence {public string[] checks;public string limitation="Synthetic input and rendered images; physical DualSense and player usability review remain open.";}
        public void Setup(RacePreviewUI preview,RaceTrial owner)
        {ui=preview;race=owner;Application.logMessageReceived+=Error;StartCoroutine(Run());}
        void Error(string message,string stack,LogType type){if(type==LogType.Exception)Application.Quit(2);}
        void Check(bool ok,string why){if(!ok)throw new Exception("Race preview review: "+why);checks.Add(why);}
        IEnumerator Pulse(GamepadButton button)
        // A connected DualSense streams motion data and keeps becoming Gamepad.current; the virtual pad must be current.
        {pad.MakeCurrent();InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return new WaitForSecondsRealtime(.12f);InputSystem.QueueStateEvent(pad,new GamepadState());yield return new WaitForSecondsRealtime(.12f);}
        IEnumerator PressKey(UnityEngine.InputSystem.Key key)
        {InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(.12f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.12f);}
        IEnumerator Run()
        {
            var folder=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(RaceTrial.Arg("--trial-report"))),"preview");Directory.CreateDirectory(folder);
            pad=InputSystem.AddDevice<Gamepad>();keyboard=InputSystem.AddDevice<Keyboard>();yield return null;yield return null;
            Check(ui.IsOpen&&Time.timeScale==0&&ui.Page==2&&ui.SelectedEntrant==0,"Preview opens on player's rear-grid row and stops the countdown");
            Check(ReferenceEquals(ui.Roster,race.Roster),"Display and spawned race share the same immutable roster");
            ui.Capture(Path.Combine(folder,"01-player.png"));float elapsed=race.elapsed;
            foreach(var entrant in race.Roster.Entries) {
                ui.SelectEntry(entrant.Id);yield return null;
                foreach(string slot in EquipmentCatalog.All.slots)Check(ui.DisplayedParts.Contains(EquipmentCatalog.Get(entrant.Parts[slot]).name),"Visible fitted part matches entrant "+entrant.Id+" / "+slot);
                Check(ui.DisplayedRating==entrant.Rating+" PP"&&race.cars[entrant.Id].tuning.Equals(entrant.Tuning)&&ReferenceEquals(race.cars[entrant.Id].Driver,entrant.Driver),"Rating, tuning and driver agree for entrant "+entrant.Id);
            }
            Check(race.elapsed==elapsed,"Inspecting all24 entries does not advance the race");
            ui.SelectEntry(0);float angle=ui.DisplayCarAngle;yield return new WaitForSecondsRealtime(.4f);Check(ui.DisplayCarAngle!=angle&&race.elapsed==elapsed,"Display car turns on its platform without advancing the countdown");
            yield return Pulse(GamepadButton.West);Check(ui.Inspector!=null&&ui.Inspector.IsOpen,"Square opens the interactive paddock");
            for(int i=0;i<EquipmentCatalog.All.slots.Length;i++){ui.Inspector.ClickPart(i);yield return null;Check(ui.Inspector.SelectedPartId==race.Roster.Entries[0].Parts[EquipmentCatalog.All.slots[i]]&&!ui.Inspector.Animated,"Clickable part "+i+" shows actual fitted equipment and pauses motion");}
            ui.Capture(Path.Combine(folder,"03-player-paddock.png"));
            yield return Pulse(GamepadButton.RightShoulder);Check(ui.Inspector.SelectedPartId==race.Roster.Entries[0].Parts[EquipmentCatalog.All.slots[0]],"R1 wraps the seven fitted parts");
            yield return Pulse(GamepadButton.East);Check(ui.IsOpen&&!ui.Inspector.IsOpen&&race.elapsed==elapsed,"Circle closes only the paddock and preserves the race entry");
            ui.SelectEntry(1);ui.OpenInspector();yield return new WaitForSecondsRealtime(.3f);ui.Capture(Path.Combine(folder,"04-rival-paddock.png"));ui.Inspector.Hide();
            ui.SetTour(true);int beforePage=ui.Page;yield return new WaitForSecondsRealtime(6.2f);Check(ui.Page==(beforePage+1)%3&&race.elapsed==elapsed,"Grid tour cycles a page without advancing or rerolling the race");ui.SetTour(false);
            // Restore the page/focus used by the original navigation fixture.
            while(ui.Page!=2)ui.TurnPage(1);ui.SelectEntry(0);ui.FocusStart();yield return null;
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            // Selecting the player's row restores the intended upward-navigation origin.
            var row=ui.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.name=="Entrant 0");
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(row.gameObject);
            ui.SelectEntry(0);yield return Pulse(GamepadButton.DpadUp);Check(ui.SelectedEntrant!=0,"D-pad navigation inspects a different driver");
            yield return Pulse(GamepadButton.RightShoulder);Check(ui.Page==0,"R1 wraps last roster page to first");
            ui.Capture(Path.Combine(folder,"02-rival.png"));
            // Driver rating and radar (docs/45 F2): DR beside PP; holding Triangle shows the radar.
            var rival=race.Roster.Entries[ui.SelectedEntrant];
            Check(rival.Driver==null||ui.DisplayedDriverRating==RacerRadar.DriverRating(RacerAbilities.From(rival.Driver)).ToString(),"Driver rating shown matches the rival's abilities");
            pad.MakeCurrent();InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.North));yield return null;pad.MakeCurrent();yield return new WaitForSecondsRealtime(.25f);
            Check(ui.RadarShown,"Holding Triangle shows the driver radar");ui.Capture(Path.Combine(folder,"05-driver-radar.png"));
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return new WaitForSecondsRealtime(.2f);
            Check(!ui.RadarShown,"Releasing Triangle hides the radar");
            yield return Pulse(GamepadButton.LeftShoulder);Check(ui.Page==2,"L1 returns to previous roster page");
            yield return PressKey(UnityEngine.InputSystem.Key.PageDown);Check(ui.Page==0,"Keyboard PageDown browses roster pages");
            ui.FocusStart();yield return Pulse(GamepadButton.South);
            Check(!ui.IsOpen&&Time.timeScale>0&&race.elapsed>elapsed,"Cross confirms entry and starts the original countdown");
            File.WriteAllText(Path.Combine(folder,"review.json"),JsonUtility.ToJson(new Evidence{checks=checks.ToArray()},true));
            Debug.Log("LITTLE_ROAD_RACE_PREVIEW_REVIEW_PASSED checks="+checks.Count);
            Destroy(this);
        }
        void OnDestroy(){Application.logMessageReceived-=Error;if(pad!=null)InputSystem.RemoveDevice(pad);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);}
    }
}
