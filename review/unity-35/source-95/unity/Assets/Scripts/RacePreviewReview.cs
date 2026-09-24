using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LittleRoad
{
    public sealed class RacePreviewReview:MonoBehaviour
    {
        RacePreviewUI ui;RaceTrial race;Gamepad pad;Keyboard keyboard;readonly List<string> checks=new();
        [Serializable] sealed class Evidence {public string[] checks;public string limitation="Synthetic input and rendered images; physical DualSense and player usability review remain open.";}
        public void Setup(RacePreviewUI preview,RaceTrial owner)
        {ui=preview;race=owner;Application.logMessageReceived+=Error;StartCoroutine(Run());}
        void Error(string message,string stack,LogType type){if(type==LogType.Exception)Application.Quit(2);}
        void Check(bool ok,string why){if(!ok)throw new Exception("Race preview review: "+why);checks.Add(why);}
        IEnumerator Pulse(GamepadButton button)
        {InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return new WaitForSecondsRealtime(.12f);InputSystem.QueueStateEvent(pad,new GamepadState());yield return new WaitForSecondsRealtime(.12f);}
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
            ui.SelectEntry(0);yield return Pulse(GamepadButton.DpadUp);Check(ui.SelectedEntrant!=0,"D-pad navigation inspects a different driver");
            yield return Pulse(GamepadButton.RightShoulder);Check(ui.Page==0,"R1 wraps last roster page to first");
            ui.Capture(Path.Combine(folder,"02-rival.png"));
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
