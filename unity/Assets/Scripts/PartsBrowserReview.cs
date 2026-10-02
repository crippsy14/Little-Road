using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LittleRoad
{
    // Isolated end-to-end garage review. Synthetic input validates bindings and
    // focus, not the physical DualSense or player acceptance of the interface.
    public sealed class PartsBrowserReview:MonoBehaviour
    {
        PartsBrowser browser;AdventureStore store;RoadCar car;AdventureUI ui;Action<PartsBrowser.Mode> open;
        readonly List<string> checks=new();string folder;Gamepad pad;Keyboard keyboard;
        [Serializable] sealed class Evidence {public string[] checks;public int cachedIdleRenders;public float stockSprint,workshopSprint;public string limitation="Synthetic input; physical DualSense and visible frame-time acceptance remain open.";}
        public void Setup(PartsBrowser b,AdventureStore s,RoadCar c,AdventureUI u,Action<PartsBrowser.Mode> reopen)
        {
            browser=b;store=s;car=c;ui=u;open=reopen;
            Application.logMessageReceived+=FailOnException;StartCoroutine(Run());
        }
        void FailOnException(string message,string stack,LogType type){if(type==LogType.Exception){Debug.LogError("PARTS_BROWSER_REVIEW_FAILED "+message);Application.Quit(2);}}
        void Check(bool value,string label){if(!value)throw new Exception(label);checks.Add(label);Debug.Log("PARTS_BROWSER_CHECK "+label);}
        IEnumerator Pad(GamepadButton button)
        {InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return new WaitForSeconds(.1f);InputSystem.QueueStateEvent(pad,new GamepadState());yield return new WaitForSeconds(.2f);}
        IEnumerator PressKey(Key key)
        {InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return new WaitForSeconds(.1f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSeconds(.2f);}
        void Capture(string name)=>ui.Capture(Path.Combine(folder,name+".png"));
        // Shop: browse, can't afford, can't fit, buy with confirm. Categories switch in place along the
        // tabs. Fitting bay: only owned parts, the fitted one first, confirm fits, swapping back is free.
        IEnumerator Run()
        {
            folder=RaceTrial.Arg("--trial-captures");if(string.IsNullOrEmpty(folder)||string.IsNullOrEmpty(RaceTrial.Arg("--adventure-save")))throw new Exception("Parts review needs isolated output and save paths.");
            Directory.CreateDirectory(folder);Check(store.Data.credits==0&&store.Data.Loadout()["engine"]=="engine_stock","Fresh stock profile required");
            yield return new WaitForSeconds(.6f);pad=InputSystem.AddDevice<Gamepad>("PartsReviewGamepad");keyboard=InputSystem.AddDevice<Keyboard>("PartsReviewKeyboard");
            string original=JsonUtility.ToJson(store.Data);bool hadFile=File.Exists(store.path);string originalDisk=hadFile?File.ReadAllText(store.path):null;
            Check(browser.CurrentMode==PartsBrowser.Mode.Shop&&browser.Slot=="engine"&&browser.SelectedId=="engine_workshop","Shop opens on the first engine you don't own (stock parts aren't for sale)");
            Capture("01-shop");
            yield return Pad(GamepadButton.South);Check(JsonUtility.ToJson(store.Data)==original,"Confirm can't buy what you can't afford");
            Check(!browser.InvokeAction("buy")&&!browser.InvokeAction("fit"),"Unaffordable: no purchase; the shop never fits");
            yield return Pad(GamepadButton.DpadRight);Check(browser.SelectedId=="engine_club","D-pad browses along the carousel");
            yield return Pad(GamepadButton.DpadLeft);
            float angle=browser.PreviewAngle;
            InputSystem.QueueStateEvent(pad,new GamepadState{rightStick=new Vector2(.8f,0),rightTrigger=1});var start=car.body.position;yield return new WaitForSeconds(.3f);
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return new WaitForSeconds(.2f);
            Check(Mathf.Abs(Mathf.DeltaAngle(angle,browser.PreviewAngle))>8,"Right stick spins the part");
            Check(browser.SelectedId=="engine_workshop","Right stick does not also browse");
            Check(Vector3.Distance(start,car.body.position)<.08f,"Throttle stays blocked in the garage");
            int renderCount=browser.PreviewRenderCount;yield return new WaitForSeconds(.5f);
            Check(browser.PreviewRenderCount==renderCount,"Idle screen reuses cached renders");
            // Categories on D-pad down / up (RB / LB step the class tabs now: one class's parts at a time).
            yield return Pad(GamepadButton.DpadDown);Check(browser.Slot=="turbo","D-pad down moves to the next category");Capture("02-shop-turbo");
            yield return Pad(GamepadButton.DpadUp);Check(browser.Slot=="engine","D-pad up moves back");
            string tier=browser.Tier;yield return Pad(GamepadButton.RightShoulder);
            Check(browser.Tiers.Count<2?browser.Tier==tier:browser.Tier!=tier,$"RB steps the class tabs ({string.Join(" ",browser.Tiers)}; one class here leaves it)");
            browser.SetDiscipline("rally");Check(browser.Slot=="engine"&&browser.PartCount>0,"Rally (no parts yet) says coming soon instead of opening an empty shop");
            yield return PressKey(Key.RightBracket);Check(browser.Slot=="turbo","] moves right along the category tabs");yield return PressKey(Key.LeftBracket);Check(browser.Slot=="engine","[ moves left");
            yield return Pad(GamepadButton.East);Check(!browser.IsOpen&&ui.IsOpen,"B returns to Sid without road input leaking");
            Check(JsonUtility.ToJson(store.Data)==original&&File.Exists(store.path)==hadFile&&(!hadFile||File.ReadAllText(store.path)==originalDisk),"Browsing and backing out preserve inventory and save bytes");
            // Earn the real early-game budget through the store's reward transactions.
            store.AcceptDelivery();store.Deliver();store.ClaimDelivery();store.Collect("postcard_0");store.Collect("postcard_1");
            open(PartsBrowser.Mode.Shop);yield return new WaitForSeconds(.4f);Check(browser.SelectedId=="engine_workshop","Selection survives back and reopen");
            Capture("03-affordable");float stockPower=car.tuning.power;
            yield return Pad(GamepadButton.South);
            Check(store.Data.credits==0&&store.Data.owned.Contains("engine_workshop")&&store.Data.Loadout()["engine"]=="engine_stock"&&car.tuning.power==stockPower,"Confirm buys (450 cr) without fitting");
            Check(!browser.InvokeAction("buy")&&!store.Buy("engine_workshop")&&store.Data.credits==0,"Repeat purchase cannot charge twice");Capture("04-bought");
            yield return PressKey(Key.Escape);Check(!browser.IsOpen,"Escape leaves the shop");
            open(PartsBrowser.Mode.Fit);yield return new WaitForSeconds(.4f);
            Check(browser.CurrentMode==PartsBrowser.Mode.Fit&&browser.PartCount==2&&browser.SelectedId=="engine_stock","Fitting bay shows only owned engines, the fitted one first");Capture("05-fit-stock");
            yield return Pad(GamepadButton.DpadRight);Check(browser.SelectedId=="engine_workshop","Browse to the owned upgrade");
            Check(!browser.InvokeAction("buy"),"The fitting bay never sells");
            yield return Pad(GamepadButton.South);Check(store.Data.Loadout()["engine"]=="engine_workshop"&&car.tuning.power>stockPower,"Confirm fits the part and updates driving power");
            Check(new AdventureStore(store.path).Data.Loadout()["engine"]=="engine_workshop","Reload preserves the fitted engine");Capture("06-fitted");
            yield return PressKey(Key.RightArrow);Check(browser.SelectedId=="engine_stock","Arrow keys browse (and wrap)");
            angle=browser.PreviewAngle;yield return PressKey(Key.X);Check(Mathf.Abs(Mathf.DeltaAngle(angle,browser.PreviewAngle))>2,"X spins the part");
            yield return PressKey(Key.Enter);Check(store.Data.Loadout()["engine"]=="engine_stock"&&store.Data.credits==0&&Mathf.Abs(car.tuning.power-stockPower)<.001f,"Enter refits the stock engine for free");
            foreach(string s in new[]{"turbo","tyres","transmission","brakes","steering","chassis"}){yield return Pad(GamepadButton.DpadDown);Check(browser.Slot==s&&browser.PartCount>=1,"Fitting bay reaches "+s);Capture("07-fit-"+s);}
            yield return Pad(GamepadButton.East);Check(!browser.IsOpen&&ui.IsOpen,"B returns to Mara");
            var workshop=new Dictionary<string,string>(store.Data.Loadout()){["engine"]="engine_workshop"};
            var evidence=new Evidence{checks=checks.ToArray(),cachedIdleRenders=renderCount,stockSprint=PartsBrowser.Sprint(CarTuning.Stock),workshopSprint=PartsBrowser.Sprint(EquipmentCatalog.Stats(workshop))};
            File.WriteAllText(Path.Combine(folder,"parts-browser-review.json"),JsonUtility.ToJson(evidence,true));
            Debug.Log("LITTLE_ROAD_PARTS_BROWSER_REVIEW_PASSED checks="+checks.Count);Application.Quit();
        }
        void OnDestroy(){Application.logMessageReceived-=FailOnException;if(pad!=null)InputSystem.RemoveDevice(pad);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);}
    }
}
