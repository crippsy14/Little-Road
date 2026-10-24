using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LittleRoad
{
    // Race entry and return rebuild the whole world in one blocking step (~8 s). Instead of a frozen
    // last frame, a toy loading card is shown and presented first, survives the scene load, and
    // fades once the new world reports ready (AdventureGame / RaceTrial call Ready).
    public sealed class LoadingScreen:MonoBehaviour
    {
        static LoadingScreen instance;
        CanvasGroup group;
        static readonly string[] Tips={
            "Every Pit Stop looks the same, so you can always find the garage.",
            "Bodies are looks only: your fitted parts decide the pace.",
            "Drive through the wheat at Golden Acre: it springs back.",
            "Rival kit stays fixed, so every upgrade you fit shows on the clock.",
            "Stuck? R or D-pad Down puts you back on the road.",
            "The Hillcrest Line runs from Bayview Halt, over the sea and through the headland."};
        public static void Load(int scene,string title)
        {
            // Scene switches (docs/72): race entry and return to town are timed from here to LITTLE_ROAD_SCENE_SWITCH ready.
            Debug.Log($"LITTLE_ROAD_SCENE_SWITCH begin t={Time.realtimeSinceStartup:0.00} to={title}");
            if(instance==null) {
                var go=new GameObject("Loading screen",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));DontDestroyOnLoad(go);
                instance=go.AddComponent<LoadingScreen>();instance.Build();
            }
            instance.Show(title);instance.StartCoroutine(instance.LoadAfterPresent(scene));
        }
        public static void Ready(){if(instance!=null&&instance.group.alpha>0){instance.Progress01(1,"Ready");instance.StartCoroutine(instance.Fade());}}
        // First launch has no card yet: show one while the world builds.
        public static void Begin(string title)
        {
            if(Application.isBatchMode)return;
            if(instance==null){var go=new GameObject("Loading screen",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));DontDestroyOnLoad(go);instance=go.AddComponent<LoadingScreen>();instance.Build();}
            if(instance.group.alpha<=0)instance.Show(title);
        }
        public static void Progress(float fraction,string label){if(instance!=null&&instance.group.alpha>0)instance.Progress01(fraction,label);}
        Text heading,tip,step;RectTransform fill;
        void Progress01(float f,string label){fill.sizeDelta=new Vector2(Mathf.Lerp(0,760,Mathf.Clamp01(f)),26);step.text=label;}
        void Build()
        {
            var canvas=GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
            var scaler=GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            group=GetComponent<CanvasGroup>();
            var bg=Toy.Rect(transform,"Loading backdrop",new Vector2(1920,1080),Vector2.zero);bg.gameObject.AddComponent<RectMask2D>();
            Toy.Sunburst(bg,new Vector2(2600,2600),Vector2.zero,PitStop.Brand,Toy.Hex(0xff9a4a),30,3);
            Toy.Checker(bg,1920,525);Toy.Checker(bg,1920,-525);
            var card=Toy.Card(bg,"Loading card",new Vector2(1100,420),new Vector2(0,20),Toy.Cream,50,8,12);
            heading=Toy.Title(card.transform,"Loading title","",76,new Vector2(1000,110),new Vector2(0,90),PitStop.Brand,TextAnchor.MiddleCenter,-2); // orange sticker text: navy fill merged with its navy outline
            // Progress bar with the current step ("Carving the cliffs"...).
            var bar=Toy.Card(card.transform,"Loading bar",new Vector2(780,40),new Vector2(0,-4),Toy.Navy,20,0,4);
            fill=Toy.Card(bar.transform,"Loading fill",new Vector2(0,26),Vector2.zero,PitStop.Brand,13,0,0).rectTransform;
            fill.anchorMin=fill.anchorMax=new Vector2(0,.5f);fill.pivot=new Vector2(0,.5f);fill.anchoredPosition=new Vector2(10,0);
            step=Toy.Label(card.transform,"Loading step","",24,new Vector2(780,34),new Vector2(0,-46),Toy.Navy,TextAnchor.MiddleCenter,true);
            tip=Toy.Label(card.transform,"Loading tip","",28,new Vector2(980,110),new Vector2(0,-120),Toy.Navy,TextAnchor.MiddleCenter);
        }
        void Show(string title){heading.text=title.ToUpperInvariant();tip.text="Tip: "+Tips[Random.Range(0,Tips.Length)];group.alpha=1;group.blocksRaycasts=true;Progress01(0,"");}
        IEnumerator LoadAfterPresent(int scene){yield return null;yield return new WaitForEndOfFrame();SceneManager.LoadScene(scene);}
        IEnumerator Fade()
        {
            yield return null;
            for(float t=0;t<.45f;t+=Time.unscaledDeltaTime){group.alpha=1-t/.45f;yield return null;}
            group.alpha=0;group.blocksRaycasts=false;
        }
    }
}
