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
        readonly List<Button> buttons=new();
        public bool IsOpen=>panel!=null&&panel.gameObject.activeSelf;
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
            foreach(var button in buttons)if(button!=null)button.GetComponentInChildren<Text>().color=
                EventSystem.current.currentSelectedGameObject==button.gameObject?new Color(.035f,.08f,.10f):
                button.interactable?new Color(.96f,.96f,.88f):new Color(.62f,.68f,.69f);
        }
        public void Capture(string path)
        {
            var camera=Camera.main;canvas.worldCamera=camera;Canvas.ForceUpdateCanvases();
            var rt=new RenderTexture(1920,1080,24);var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
            File.WriteAllBytes(path,texture.EncodeToPNG());camera.targetTexture=previousTarget;RenderTexture.active=previousActive;rt.Release();Destroy(rt);Destroy(texture);
        }
        public void Show(string title,string body,List<Choice> choices)
        {
            StopAllCoroutines();foreach(var button in buttons)Destroy(button.gameObject);buttons.Clear();EventSystem.current.SetSelectedGameObject(null);
            panel.gameObject.SetActive(true);heading.text=title;description.text=body;
            float height=Mathf.Min(57,490f/Mathf.Max(1,choices.Count));
            for(int i=0;i<choices.Count;i++) {
                var choice=choices[i];var rect=Box("Choice "+choice.text,panel,new Vector2(744,height-6),new Vector2(0,122-i*height));
                var button=rect.gameObject.AddComponent<Button>();button.interactable=choice.enabled;button.targetGraphic=rect.GetComponent<Image>();
                var colors=button.colors;colors.normalColor=new Color(.24f,.48f,.49f);colors.highlightedColor=new Color(.60f,.78f,.68f);colors.selectedColor=new Color(.86f,.65f,.30f);colors.disabledColor=new Color(.20f,.25f,.27f);button.colors=colors;
                // White base lets the selectable tint remain visible.
                rect.GetComponent<Image>().color=Color.white;
                Text("Label",rect,choice.text,23,new Vector2(712,height-8),Vector2.zero);
                button.onClick.AddListener(()=>choice.action?.Invoke());buttons.Add(button);
            }
            var active=buttons.FindAll(b=>b.interactable);
            for(int i=0;i<active.Count;i++)active[i].navigation=new Navigation {mode=Navigation.Mode.Explicit,selectOnUp=active[(i+active.Count-1)%active.Count],selectOnDown=active[(i+1)%active.Count]};
            if(active.Count>0)StartCoroutine(SelectNextFrame(active[0]));
        }
        IEnumerator SelectNextFrame(Button button){yield return null;if(button!=null&&IsOpen)EventSystem.current.SetSelectedGameObject(button.gameObject);}
        public void Close(){StopAllCoroutines();panel.gameObject.SetActive(false);EventSystem.current.SetSelectedGameObject(null);}
        public bool InvokeChoice(string startsWith)
        {
            var button=buttons.Find(b=>b.name.StartsWith("Choice "+startsWith)&&b.interactable);if(button==null)return false;button.onClick.Invoke();return true;
        }
    }
}
