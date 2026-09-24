using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LittleRoad
{
    public sealed class AdventureUI:MonoBehaviour
    {
        public sealed class Choice {public string text;public Action action;public bool enabled=true;public Choice(string text,Action action,bool enabled=true){this.text=text;this.action=action;this.enabled=enabled;}}
        Canvas canvas;RectTransform panel;Text heading,description,hud,prompt;Font font;
        RectTransform topBar,bottomBar;
        RectTransform resultPanel;Text resultPlace,resultTime,resultReward,resultWallet;float presentedFinish;
        readonly List<Button> buttons=new();
        public bool IsOpen=>(panel!=null&&panel.gameObject.activeSelf)||(resultPanel!=null&&resultPanel.gameObject.activeSelf);
        public string DisplayedFinishTime=>resultTime?.text;
        void Awake()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root=new GameObject("Adventure interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform);
            canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.planeDistance=1;canvas.sortingOrder=10;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            if(EventSystem.current==null){var events=new GameObject("Menu input",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(transform);events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            // Reserve the right stick for model inspection. Unity's defaults
            // bind both gamepad sticks to UI movement.
            var move=EventSystem.current.GetComponent<InputSystemUIInputModule>()?.move?.action;
            if(move!=null)for(int i=0;i<move.bindings.Count;i++)
                if(move.bindings[i].path?.StartsWith("<Gamepad>/rightStick")==true)move.ApplyBindingOverride(i,"");
            var top=Box("Journey HUD",root.transform,new Vector2(1880,110),new Vector2(0,475));
            topBar=top;
            hud=Text("Journey",top,"",25,new Vector2(1840,95),Vector2.zero);hud.alignment=TextAnchor.MiddleLeft;
            var bottom=Box("Journey controls",root.transform,new Vector2(1880,90),new Vector2(0,-475));
            bottomBar=bottom;
            prompt=Text("Controls",bottom,"",23,new Vector2(1840,80),Vector2.zero);prompt.alignment=TextAnchor.MiddleLeft;
            panel=Box("Conversation",root.transform,new Vector2(820,790),Vector2.zero);
            heading=Text("Heading",panel,"",33,new Vector2(744,60),new Vector2(0,332));heading.fontStyle=FontStyle.Bold;
            description=Text("Description",panel,"",23,new Vector2(744,145),new Vector2(0,232));description.alignment=TextAnchor.UpperLeft;
            panel.gameObject.SetActive(false);
        }
        RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
        }
        RectTransform Box(string name,Transform parent,Vector2 size,Vector2 position)
        {
            var rect=Rect(name,parent,size,position);rect.gameObject.AddComponent<Image>().color=new Color(.035f,.095f,.12f,.96f);return rect;
        }
        Text Text(string name,Transform parent,string value,int size,Vector2 dimensions,Vector2 position)
        {
            var rect=Rect(name,parent,dimensions,position);var text=rect.gameObject.AddComponent<Text>();text.font=font;text.text=value;text.fontSize=size;
            text.color=new Color(.96f,.96f,.88f);text.alignment=TextAnchor.MiddleLeft;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.raycastTarget=false;return text;
        }
        public void HUD(string value,string controls){hud.text=value;prompt.text=controls;}
        public void SetVisible(bool value){canvas.enabled=value;}
        public void SetHudVisible(bool value){topBar.gameObject.SetActive(value);bottomBar.gameObject.SetActive(value);}
        void LateUpdate()
        {
            if(canvas.worldCamera==null)canvas.worldCamera=Camera.main;
        }
        public void Capture(string path)
        {
            var camera=Camera.main;canvas.worldCamera=camera;CoastMenu.RefreshButtons(canvas.transform);Canvas.ForceUpdateCanvases();
            var rt=new RenderTexture(1920,1080,24);var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
            File.WriteAllBytes(path,texture.EncodeToPNG());camera.targetTexture=previousTarget;RenderTexture.active=previousActive;rt.Release();Destroy(rt);Destroy(texture);
        }
        public void Show(string title,string body,List<Choice> choices)
        {
            ClearChoices();if(resultPanel!=null)resultPanel.gameObject.SetActive(false);
            panel.gameObject.SetActive(true);heading.text=title;description.text=body;
            float height=Mathf.Min(57,490f/Mathf.Max(1,choices.Count));
            for(int i=0;i<choices.Count;i++) {
                var choice=choices[i];var button=CoastMenu.Button(panel,"Choice "+choice.text,choice.text,new Vector2(744,height-6),new Vector2(0,122-i*height),choice.action);
                button.interactable=choice.enabled;buttons.Add(button);
            }
            FocusChoices();
        }
        void ClearChoices()
        {
            StopAllCoroutines();foreach(var button in buttons){button.gameObject.SetActive(false);Destroy(button.gameObject);}buttons.Clear();EventSystem.current.SetSelectedGameObject(null);
        }
        void FocusChoices()
        {
            var active=buttons.FindAll(b=>b.interactable);
            for(int i=0;i<active.Count;i++)active[i].navigation=new Navigation {mode=Navigation.Mode.Explicit,selectOnUp=active[(i+active.Count-1)%active.Count],selectOnDown=active[(i+1)%active.Count]};
            if(active.Count>0)StartCoroutine(SelectNextFrame(active[0]));
        }
        public static string FinishTimeLabel(float seconds)=>$"{(int)seconds/60:0}:{seconds%60:00.000}";
        public void ShowRaceResult(int place,int entrants,float finishTime,int reward,int wallet,Action returnToTown,Action retry)
        {
            ClearChoices();panel.gameObject.SetActive(false);SetHudVisible(false);presentedFinish=finishTime;
            if(resultPanel==null){
                resultPanel=CoastMenu.Paper(canvas.transform,"Race day result",new Vector2(1230,710),new Vector2(0,10));
                var ribbon=CoastUI.Panel(resultPanel,"Finish ribbon",new Vector2(350,46),new Vector2(-380,293),CoastUI.Sun,12);
                CoastUI.Label(ribbon,"Result heading","RACE COMPLETE",22,new Vector2(300,40),Vector2.zero,CoastMenu.Navy,true);
                CoastUI.Label(resultPanel,"Venue","BAYVIEW COAST",35,new Vector2(1120,58),new Vector2(0,220),CoastMenu.Navy,true);
                var badge=CoastUI.Panel(resultPanel,"Place badge",new Vector2(250,250),new Vector2(-386,25),CoastUI.Sea,125);
                CoastUI.Label(badge,"Place caption","FINISH",18,new Vector2(180,30),new Vector2(0,68),CoastMenu.Cream,true,TextAnchor.MiddleCenter);
                resultPlace=CoastUI.Label(badge,"Finishing position","",59,new Vector2(230,95),new Vector2(0,1),Color.white,true,TextAnchor.MiddleCenter);
                CoastUI.Label(badge,"Event caption","COAST RUN",15,new Vector2(200,28),new Vector2(0,-67),CoastMenu.Cream,false,TextAnchor.MiddleCenter);
                CoastUI.Label(resultPanel,"Time caption","YOUR FINISH TIME",16,new Vector2(670,27),new Vector2(189,124),CoastUI.Muted);
                resultTime=CoastUI.Label(resultPanel,"Saved finishing time","",48,new Vector2(670,68),new Vector2(189,76),CoastMenu.Navy,true);
                resultReward=CoastUI.Label(resultPanel,"Earned credits","",32,new Vector2(670,49),new Vector2(189,-13),CoastMenu.Navy,true);
                resultWallet=CoastUI.Label(resultPanel,"Wallet and save","",20,new Vector2(670,40),new Vector2(189,-61),CoastUI.Muted);
                CoastUI.Panel(resultPanel,"Result rule",new Vector2(1120,2),new Vector2(0,-150),CoastMenu.Pale,0);
                CoastUI.Label(resultPanel,"Journey continues","A little further. A little faster.\nTake your earnings to the garage, or line up for another go.",22,new Vector2(1120,70),new Vector2(0,-207),CoastMenu.Navy);
            }
            resultPanel.gameObject.SetActive(true);resultPlace.text=$"{place}/{entrants}";resultTime.text=FinishTimeLabel(presentedFinish);
            resultReward.text=$"+{reward} CREDITS";resultWallet.text=$"Wallet {wallet}  ·  Result saved";
            buttons.Add(CoastMenu.Button(resultPanel,"Choice Return to Bayview","Return to Bayview",new Vector2(534,62),new Vector2(-291,-294),returnToTown));
            buttons.Add(CoastMenu.Button(resultPanel,"Choice Race again","Race again",new Vector2(534,62),new Vector2(291,-294),retry,true));
            buttons[0].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnRight=buttons[1],selectOnDown=buttons[1]};
            buttons[1].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=buttons[0],selectOnUp=buttons[0]};
            StartCoroutine(SelectNextFrame(buttons[0]));
        }
        IEnumerator SelectNextFrame(Button button){yield return null;if(button!=null&&IsOpen)EventSystem.current.SetSelectedGameObject(button.gameObject);}
        public void Close(){StopAllCoroutines();panel.gameObject.SetActive(false);if(resultPanel!=null)resultPanel.gameObject.SetActive(false);EventSystem.current.SetSelectedGameObject(null);}
        public bool InvokeChoice(string startsWith)
        {
            var button=buttons.Find(b=>b.name.StartsWith("Choice "+startsWith)&&b.interactable);if(button==null)return false;button.onClick.Invoke();return true;
        }
    }
}
