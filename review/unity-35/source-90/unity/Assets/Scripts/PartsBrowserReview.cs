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
        PartsBrowser browser;AdventureStore store;RoadCar car;AdventureUI ui;Action reopen;
        readonly List<string> checks=new();string folder;Gamepad pad;Keyboard keyboard;
        [Serializable] sealed class Evidence {public string[] checks;public int cachedIdleRenders;public float stockSprint,workshopSprint;public string limitation="Synthetic input; physical DualSense and visible frame-time acceptance remain open.";}
        public void Setup(PartsBrowser b,AdventureStore s,RoadCar c,AdventureUI u,Action open)
        {
            browser=b;store=s;car=c;ui=u;reopen=open;
            Application.logMessageReceived+=FailOnException;StartCoroutine(Run());
        }
        void FailOnException(string message,string stack,LogType type){if(type==LogType.Exception){Debug.LogError("PARTS_BROWSER_REVIEW_FAILED "+message);Application.Quit(2);}}
        void Check(bool value,string label){if(!value)throw new Exception(label);checks.Add(label);Debug.Log("PARTS_BROWSER_CHECK "+label);}
        IEnumerator Pad(GamepadButton button)
        {InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return new WaitForSeconds(.1f);InputSystem.QueueStateEvent(pad,new GamepadState());yield return new WaitForSeconds(.15f);}
        IEnumerator PressKey(Key key)
        {InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return new WaitForSeconds(.1f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSeconds(.15f);}
        void Capture(string name)=>ui.Capture(Path.Combine(folder,name+".png"));
        IEnumerator Run()
        {
            folder=RaceTrial.Arg("--trial-captures");if(string.IsNullOrEmpty(folder)||string.IsNullOrEmpty(RaceTrial.Arg("--adventure-save")))throw new Exception("Parts review needs isolated output and save paths.");
            Directory.CreateDirectory(folder);Check(store.Data.credits==0&&store.Data.Loadout()["engine"]=="engine_stock","Fresh stock profile required");
            yield return new WaitForSeconds(.5f);pad=InputSystem.AddDevice<Gamepad>("PartsReviewGamepad");keyboard=InputSystem.AddDevice<Keyboard>("PartsReviewKeyboard");
            string original=JsonUtility.ToJson(store.Data);bool hadFile=File.Exists(store.path);string originalDisk=hadFile?File.ReadAllText(store.path):null;
            Capture("01-stock");
            yield return Pad(GamepadButton.DpadRight);Check(browser.SelectedId=="engine_workshop","D-pad previews workshop engine");
            yield return Pad(GamepadButton.South);Check(JsonUtility.ToJson(store.Data)==original,"Confirm on preview card does not buy or fit");
            Check(!browser.InvokeAction("buy"),"Insufficient credits disable purchase");
            Capture("02-unaffordable");float angle=browser.PreviewAngle;
            InputSystem.QueueStateEvent(pad,new GamepadState{rightStick=new Vector2(.8f,0),rightTrigger=1});var start=car.body.position;yield return new WaitForSeconds(.3f);
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return new WaitForSeconds(.2f);
            Check(Mathf.Abs(Mathf.DeltaAngle(angle,browser.PreviewAngle))>8,"Right stick rotates selected 3D model");
            Check(browser.SelectedId=="engine_workshop","Right stick does not also navigate the catalogue");
            Check(Vector3.Distance(start,car.body.position)<.08f,"Throttle remains blocked inside garage");
            int renderCount=browser.PreviewRenderCount;yield return new WaitForSeconds(.5f);
            Check(browser.PreviewRenderCount==renderCount,"Idle garage reuses cached model renders");
            yield return Pad(GamepadButton.East);Check(!browser.IsOpen&&ui.IsOpen,"Circle returns to garage menu without road input leaking");
            Check(JsonUtility.ToJson(store.Data)==original&&File.Exists(store.path)==hadFile&&(!hadFile||File.ReadAllText(store.path)==originalDisk),"Preview and cancel preserve inventory and save bytes");
            // Earn exactly the real early-game budget through the store's reward
            // transactions. No production save or free-credit shortcut is used.
            store.AcceptDelivery();store.Deliver();store.ClaimDelivery();store.Collect("postcard_0");store.Collect("postcard_1");
            reopen();yield return new WaitForSeconds(.3f);Check(browser.SelectedId=="engine_workshop","Selection survives back and reopen");
            Capture("03-affordable");float stockPower=car.tuning.power;
            yield return Pad(GamepadButton.DpadDown);Check(EventSystem.current.currentSelectedGameObject?.name=="BUY","D-pad reaches explicit buy action");
            yield return Pad(GamepadButton.South);
            Check(store.Data.credits==0&&store.Data.owned.Contains("engine_workshop")&&store.Data.Loadout()["engine"]=="engine_stock"&&car.tuning.power==stockPower,"Purchase costs 450 and owns engine without fitting");
            Check(!browser.InvokeAction("buy")&&!store.Buy("engine_workshop")&&store.Data.credits==0,"Repeat purchase cannot charge twice");
            Check(EventSystem.current.currentSelectedGameObject?.name=="FIT","Purchase moves focus to separate fit action");Capture("04-owned-unfitted");
            yield return Pad(GamepadButton.South);Check(store.Data.Loadout()["engine"]=="engine_workshop"&&car.tuning.power>stockPower,"Separate Cross press fits engine and updates driving power");
            Check(new AdventureStore(store.path).Data.Loadout()["engine"]=="engine_workshop","Reload preserves fitted engine");Capture("05-fitted");
            yield return PressKey(Key.RightArrow);Check(browser.SelectedId=="engine_club","Keyboard arrows preview next engine");
            angle=browser.PreviewAngle;yield return PressKey(Key.E);Check(Mathf.Abs(Mathf.DeltaAngle(angle,browser.PreviewAngle))>2,"Keyboard rotates engine");
            Capture("06-club");yield return PressKey(Key.LeftArrow);yield return PressKey(Key.LeftArrow);
            Check(browser.SelectedId=="engine_stock","Keyboard returns to stock part");yield return PressKey(Key.DownArrow);yield return PressKey(Key.Enter);
            Check(store.Data.Loadout()["engine"]=="engine_stock"&&store.Data.credits==0&&Mathf.Abs(car.tuning.power-stockPower)<.001f,"Keyboard freely refits owned stock engine");
            yield return Pad(GamepadButton.RightShoulder);Check(!browser.IsOpen&&ui.IsOpen,"Shoulder opens next equipment category");
            reopen();yield return new WaitForSeconds(.3f);browser.SelectPart("engine_workshop");Check(browser.InvokeAction("fit"),"Owned workshop engine can be fitted again");
            yield return PressKey(Key.Escape);Check(!browser.IsOpen&&ui.IsOpen,"Escape returns to garage");
            var evidence=new Evidence{checks=checks.ToArray(),cachedIdleRenders=renderCount,stockSprint=PartsBrowser.Sprint(CarTuning.Stock),workshopSprint=PartsBrowser.Sprint(car.tuning)};
            File.WriteAllText(Path.Combine(folder,"parts-browser-review.json"),JsonUtility.ToJson(evidence,true));
            Debug.Log("LITTLE_ROAD_PARTS_BROWSER_REVIEW_PASSED checks="+checks.Count);Application.Quit();
        }
        void OnDestroy(){Application.logMessageReceived-=FailOnException;if(pad!=null)InputSystem.RemoveDevice(pad);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);}
    }
}
