using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LittleRoad
{
    // Named toy paints. The first four keep the colours older saves were painted in.
    public static class PaintCatalog
    {
        public readonly struct Paint{public readonly string name;public readonly Color color;public readonly int price;public Paint(string n,Color c,int p){name=n;color=c;price=p;}}
        public static readonly Paint[] All={
            new("Sea teal",new Color(.08f,.56f,.49f),150),new("Harbour orange",new Color(.88f,.36f,.17f),150),
            new("Bay blue",new Color(.30f,.45f,.75f),150),new("Sunflower",new Color(.88f,.70f,.25f),150),
            new("Cherry red",Toy.Hex(0xd92b3a),150),new("Candy pink",Toy.Hex(0xff7ac8),150),new("Lilac",Toy.Hex(0xa98be8),150),new("Lime",Toy.Hex(0x9bd63a),150),
            new("Mint",Toy.Hex(0x5fd9b0),150),new("Sky",Toy.Hex(0x5ec8ff),150),new("Navy",Toy.Hex(0x22336e),150),new("Cream",Toy.Hex(0xf2e6c4),150),
            new("Snow white",Toy.Hex(0xf4f6f8),200),new("Graphite",Toy.Hex(0x3a3f46),200),new("Tangerine",Toy.Hex(0xff9a1f),200),new("Racing green",Toy.Hex(0x1f6b3a),200)};
        public static Color[] Colors(){var c=new Color[All.Length];for(int i=0;i<c.Length;i++)c[i]=All[i].color;return c;}
    }

    // Bayview Paints: a 4x4 swatch board. Moving focus repaints the showroom preview;
    // Cross (or a click) buys the focused colour; Circle leaves without changing anything.
    public sealed class PaintShop:MonoBehaviour
    {
        Canvas canvas;AdventureStore store;PartPreviewStage stage;Action<int> painted;Action back;
        RawImage carImage;Text wallet,swatchName,price,status;readonly List<Button> swatches=new();readonly List<ToyShape> rings=new();int focus=-1;
        public bool IsOpen=>canvas!=null&&canvas.gameObject.activeSelf;
        public int LastCloseFrame {get;private set;}=-1;
        public void Open(AdventureStore inventory,Action<int> onPainted,Action onBack)
        {
            store=inventory;painted=onPainted;back=onBack;if(canvas==null)Build();
            stage?.Dispose();stage=new PartPreviewStage(store.Data.bodyId,PaintCatalog.All[store.Data.paintIndex].color,Array.Empty<EquipmentCatalog.Part>(),Color.clear,lightScale:.5f); // true-to-swatch colour
            carImage.texture=stage.Car;status.text="";Toy.UseMenuCamera(canvas);canvas.gameObject.SetActive(true);
            Focus(store.Data.paintIndex);Toy.RefreshButtons(canvas.transform);StartCoroutine(FocusNextFrame());
        }
        IEnumerator FocusNextFrame(){yield return null;if(IsOpen)EventSystem.current.SetSelectedGameObject(swatches[store.Data.paintIndex].gameObject);}
        void Build()
        {
            var root=new GameObject("Paint shop",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);root.layer=Toy.MenuLayer;
            canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.planeDistance=.7f;canvas.sortingOrder=30;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var bg=Toy.Rect(root.transform,"Paint shop backdrop",new Vector2(1920,1080),Vector2.zero);bg.gameObject.AddComponent<RectMask2D>();
            Toy.Sunburst(bg,new Vector2(2600,2600),new Vector2(0,40),Toy.Pink,Toy.Hex(0xffa6da),30,2);
            Toy.Checker(bg,1920,525);Toy.Checker(bg,1920,-525);
            Toy.Title(bg,"Paint title","BAYVIEW PAINTS",86,new Vector2(900,100),new Vector2(-470,430),Toy.White,TextAnchor.MiddleLeft,-2);
            var purse=Toy.Card(bg,"Wallet",new Vector2(360,70),new Vector2(700,440),Toy.Sun,35,6,6);
            wallet=Toy.Label(purse.transform,"Wallet text","",30,new Vector2(300,70),new Vector2(0,0),Toy.Navy,TextAnchor.MiddleCenter,true);
            // Swatch board
            var board=Toy.Card(bg,"Swatch board",new Vector2(820,760),new Vector2(-450,-20),Toy.Cream,40,7,10);
            Toy.Title(board.transform,"Board heading","PICK A COLOUR",40,new Vector2(700,60),new Vector2(0,330),Toy.Pink,TextAnchor.MiddleCenter);
            for(int i=0;i<PaintCatalog.All.Length;i++) {
                int index=i;var p=PaintCatalog.All[i];var pos=new Vector2(-285+(i%4)*190,215-(i/4)*150);
                var ring=Toy.Card(board.transform,"Swatch ring "+i,new Vector2(128,128),pos,Toy.White,64,6,6);ring.raycastTarget=true;rings.Add(ring);
                var blob=Toy.Card(ring.transform,"Swatch paint",new Vector2(96,96),Vector2.zero,p.color,48,0,0);
                var button=ring.gameObject.AddComponent<Button>();button.targetGraphic=ring;button.transition=Selectable.Transition.None;button.name="SWATCH "+p.name;
                button.onClick.AddListener(()=>Buy(index));
                var trigger=ring.gameObject.AddComponent<EventTrigger>();
                var select=new EventTrigger.Entry{eventID=EventTriggerType.Select};select.callback.AddListener(_=>Focus(index));trigger.triggers.Add(select);
                var hover=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};hover.callback.AddListener(_=>Focus(index));trigger.triggers.Add(hover);
                swatches.Add(button);
            }
            for(int i=0;i<swatches.Count;i++) {
                int r=i/4,c=i%4;
                swatches[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,
                    selectOnLeft=c>0?swatches[i-1]:null,selectOnRight=c<3?swatches[i+1]:null,selectOnUp=r>0?swatches[i-4]:null,selectOnDown=r<3?swatches[i+4]:null};
            }
            // Preview and info
            var showcase=Toy.Card(bg,"Showroom",new Vector2(900,560),new Vector2(470,60),Toy.White,40,7,10);
            Toy.Sunburst(showcase.transform,new Vector2(820,480),Vector2.zero,Toy.Sun,Toy.Hex(0xffe27a),20,-6).circular=true;
            carImage=Toy.Rect(showcase.transform,"Car preview",new Vector2(860,478),Vector2.zero).gameObject.AddComponent<RawImage>();carImage.raycastTarget=false;
            var tag=Toy.Card(bg,"Swatch tag",new Vector2(620,110),new Vector2(470,-300),Toy.Navy,55,0,8);
            swatchName=Toy.Title(tag.transform,"Swatch name","",46,new Vector2(400,80),new Vector2(-80,0),Toy.White,TextAnchor.MiddleLeft);
            price=Toy.Label(tag.transform,"Swatch price","",30,new Vector2(220,80),new Vector2(180,0),Toy.Sun,TextAnchor.MiddleRight,true);
            status=Toy.Label(bg,"Paint status","",26,new Vector2(900,40),new Vector2(470,-380),Toy.Navy,TextAnchor.MiddleCenter,true);
            Toy.Candy(bg,"BACK TO THE ROAD","BACK","O",new Vector2(260,90),new Vector2(-740,-465),Toy.Red,Leave);
            Toy.Label(bg,"Paint hint","D-pad / stick: choose   ·   Cross: paint it!   ·   Circle: back",22,new Vector2(900,40),new Vector2(160,-465),Toy.Navy,TextAnchor.MiddleCenter,true);
            canvas.gameObject.SetActive(false);
        }
        void Focus(int index)
        {
            if(stage==null||index==focus)return;focus=index;var p=PaintCatalog.All[index];bool owned=index==store.Data.paintIndex;
            stage.Repaint(p.color);swatchName.text=p.name.ToUpperInvariant();
            price.text=owned?"ON YOUR CAR":p.price+" credits";wallet.text=store.Data.credits+" CREDITS";
            for(int i=0;i<rings.Count;i++)rings[i].SetBorder(i==index?Toy.Orange:i==store.Data.paintIndex?Toy.Mint:Toy.Navy);
        }
        void Buy(int index)
        {
            var p=PaintCatalog.All[index];
            if(index==store.Data.paintIndex){status.text="That's already your colour!";return;}
            if(store.Data.credits<p.price){status.text="Not enough credits for "+p.name+". Every race pays out!";return;}
            if(!store.Repaint(index,p.price)){status.text="Could not save that change.";return;}
            painted?.Invoke(index);focus=-1;Focus(index);status.text="Fresh paint! Your car is now "+p.name+".";
        }
        void Update()
        {
            if(!IsOpen)return;
            var k=Keyboard.current;var pad=Gamepad.current;
            if(k?.escapeKey.wasPressedThisFrame==true||pad?.buttonEast.wasPressedThisFrame==true)Leave();
            var selected=EventSystem.current?.currentSelectedGameObject;
            if(selected!=null){int i=swatches.FindIndex(b=>b.gameObject==selected);if(i>=0)Focus(i);}
        }
        public void Leave(){if(!IsOpen)return;canvas.gameObject.SetActive(false);stage?.Dispose();stage=null;focus=-1;LastCloseFrame=Time.frameCount;back?.Invoke();}
        void OnDestroy(){stage?.Dispose();}
    }
}
