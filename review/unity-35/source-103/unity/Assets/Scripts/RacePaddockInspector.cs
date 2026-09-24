using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LittleRoad
{
    public sealed class PaddockHover:MonoBehaviour,IPointerEnterHandler
    {
        public Action entered;public void OnPointerEnter(PointerEventData e)=>entered?.Invoke();
    }
    public sealed class PaddockPartPicker:MonoBehaviour,IPointerClickHandler
    {
        public Action<Vector2> picked;
        public void OnPointerClick(PointerEventData e){if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out var local))picked?.Invoke(local);}
    }
    public sealed class DriverRadar:MaskableGraphic
    {
        Vector3 values;
        public void Set(float risk,float braking,float consistency){values=new Vector3(risk,braking,consistency);raycastTarget=false;SetVerticesDirty();}
        static Vector2 Point(int axis,float radius){float a=(90-axis*120)*Mathf.Deg2Rad;return new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();float radius=rectTransform.rect.height*.43f;
            for(int ring=1;ring<=4;ring++)for(int i=0;i<3;i++)Line(vh,Point(i,radius*ring/4),Point((i+1)%3,radius*ring/4),1,new Color(.1f,.3f,.3f,.3f));
            for(int i=0;i<3;i++)Line(vh,Vector2.zero,Point(i,radius),1,new Color(.1f,.3f,.3f,.4f));
            int start=vh.currentVertCount;for(int i=0;i<3;i++)vh.AddVert(Point(i,radius*Mathf.Clamp01(values[i])),new Color(.06f,.5f,.46f,.35f),Vector2.zero);vh.AddTriangle(start,start+1,start+2);
            for(int i=0;i<3;i++)Line(vh,Point(i,radius*Mathf.Clamp01(values[i])),Point((i+1)%3,radius*Mathf.Clamp01(values[(i+1)%3])),3,CoastUI.Sea);
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {var delta=b-a;if(delta.sqrMagnitude<.00001f)return;var side=new Vector2(-delta.y,delta.x).normalized*width*.5f;int v=vh.currentVertCount;vh.AddVert(a-side,c,Vector2.zero);vh.AddVert(a+side,c,Vector2.zero);vh.AddVert(b+side,c,Vector2.zero);vh.AddVert(b-side,c,Vector2.zero);vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);}
    }
    public sealed class RacePaddockInspector:MonoBehaviour
    {
        RectTransform root,orbitRect;PartPreviewStage stage;RaceEntrant entrant,player;RawImage car,orbit,part;Text heading,partText,driverText,motionText;DriverRadar radar;
        Button close,motion;Button[] hotspots=new Button[7];int selected;float nextFrame,orbitAngle;bool animate=true;Action closed;
        public bool IsOpen=>root!=null&&root.gameObject.activeSelf;
        public int LastCloseFrame {get;private set;}=-1;
        public string SelectedPartId=>entrant?.Parts[EquipmentCatalog.All.slots[selected]];
        public bool Animated=>animate;
        public void Open(Transform parent,RaceEntrant entry,RaceEntrant own,PartPreviewStage model,Action onClose)
        {
            entrant=entry;player=own;stage=model;closed=onClose;if(root==null)Build(parent);root.SetAsLastSibling();root.gameObject.SetActive(true);
            heading.text=(entry.Id==0?"YOUR PADDOCK":entry.Driver?.Name.ToUpperInvariant()+" / PADDOCK");
            car.texture=stage.Car;orbit.texture=stage.Orbit;part.texture=stage.Selected;orbitAngle=0;animate=true;
            radar.gameObject.SetActive(entry.Id!=0&&entry.Driver!=null);
            if(entry.Id==0)driverText.text="YOU SET THE PACE\nYour driving writes the story.\nNo AI personality is applied to you.";
            else if(entry.Driver==null)driverText.text="Original driver\nNo personality profile assigned.";
            else {var d=entry.Driver;radar.Set(d.Planning.Risk/10f,d.BaseBraking,d.Consistency);driverText.text=$"{d.Style.ToUpperInvariant()}\nRisk  {d.Planning.Risk}/10\nBrake commitment  {d.BaseBraking*100:0.#}%\nConsistency  {d.Consistency*100:0}%\n\nTraits shape decisions, not car power.";}
            SelectPart(0,false);RenderScene();EventSystem.current.SetSelectedGameObject(close.gameObject);
        }
        Text Label(Transform parent,string value,int size,Vector2 box,Vector2 position,bool bold=false)=>CoastUI.Label(parent,"Paddock text",value,size,box,position,CoastMenu.Navy,bold);
        RawImage View(Transform parent,string name,Vector2 size,Vector2 position)
        {var r=CoastUI.Rect(parent,name,size,position);var image=r.gameObject.AddComponent<RawImage>();image.raycastTarget=false;return image;}
        void Build(Transform parent)
        {
            root=CoastUI.Rect(parent,"Interactive paddock",new Vector2(1920,1080),Vector2.zero);
            CoastUI.Panel(root,"Paddock shade",new Vector2(2600,1800),Vector2.zero,new Color(.035f,.12f,.13f,.94f),0);
            var paper=CoastMenu.Paper(root,"Paddock notebook",new Vector2(1750,944),Vector2.zero);
            heading=Label(paper,"",34,new Vector2(1570,56),new Vector2(0,408),true);
            Label(paper,"01 / THE REVEAL",18,new Vector2(740,30),new Vector2(-431,340),true);
            car=View(paper,"Rotating car in 3D showroom",new Vector2(750,410),new Vector2(-431,108));
            var stamp=CoastUI.Panel(paper,"Showroom admission sticker",new Vector2(148,34),new Vector2(-155,289),CoastUI.Sun,5);stamp.localRotation=Quaternion.Euler(0,0,-5);
            var stampText=Label(stamp,"ALL ACCESS",14,new Vector2(140,30),Vector2.zero,true);stampText.alignment=TextAnchor.MiddleCenter;
            Label(paper,"02 / UNDER THE SKIN",18,new Vector2(740,30),new Vector2(413,340),true);
            var orbitBackground=CoastUI.Panel(paper,"Workshop inspection mat",new Vector2(760,410),new Vector2(413,108),new Color(.055f,.18f,.20f),20);
            var blueprint=CoastUI.Rect(orbitBackground,"Workshop markings",new Vector2(740,390),Vector2.zero).gameObject.AddComponent<WorkshopMat>();blueprint.raycastTarget=false;
            var benchLabel=CoastUI.Label(orbitBackground,"Bench caption","FITTED & READY",15,new Vector2(235,28),new Vector2(-230,175),CoastMenu.Cream,true);
            CoastUI.Label(orbitBackground,"Bench hint","7 PARTS / PICK ONE",13,new Vector2(220,28),new Vector2(243,-176),new Color(.55f,.79f,.74f));
            orbit=View(orbitBackground,"Orbiting fitted parts",new Vector2(744,392),Vector2.zero);orbitRect=orbit.rectTransform;
            orbit.raycastTarget=true;orbit.gameObject.AddComponent<PaddockHover>().entered=()=>SetAnimation(false);
            orbit.gameObject.AddComponent<PaddockPartPicker>().picked=position=>{
                int nearest=-1;float distance=90*90;
                for(int i=0;i<7;i++){var p=stage.OrbitPosition(i);var centre=new Vector2((p.x-.5f)*orbitRect.sizeDelta.x,(p.y-.5f)*orbitRect.sizeDelta.y);float d=(centre-position).sqrMagnitude;if(d<distance){distance=d;nearest=i;}}
                if(nearest>=0)SelectPart(nearest);
            };
            for(int i=0;i<7;i++){
                int index=i;hotspots[i]=CoastMenu.Button(orbitRect,"Inspect "+EquipmentCatalog.All.slots[i],(i+1).ToString(),new Vector2(46,33),Vector2.zero,()=>SelectPart(index));
                hotspots[i].GetComponentInChildren<Text>().rectTransform.sizeDelta=new Vector2(30,30);hotspots[i].GetComponentInChildren<Text>().rectTransform.anchoredPosition=Vector2.zero;
                ((RectTransform)hotspots[i].transform.Find("Focus arrow")).anchoredPosition=new Vector2(-30,0);
                hotspots[i].gameObject.AddComponent<PaddockHover>().entered=()=>SetAnimation(false);
            }
            for(int i=0;i<7;i++)hotspots[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnRight=hotspots[(i+1)%7],selectOnLeft=hotspots[(i+6)%7]};
            var driverCard=CoastUI.Panel(paper,"Driver dossier",new Vector2(760,233),new Vector2(-431,-241),new Color(.91f,.91f,.80f),16);
            var partCard=CoastUI.Panel(paper,"Part notes",new Vector2(760,233),new Vector2(413,-241),new Color(1,.97f,.89f),16);
            Label(driverCard,"03 / WHO'S AT THE WHEEL?",13,new Vector2(620,22),new Vector2(-42,97),true);
            Label(partCard,"04 / ON THE BENCH",13,new Vector2(620,22),new Vector2(-42,97),true);
            var graphRoot=CoastUI.Rect(paper,"Driver trait graph",new Vector2(250,190),new Vector2(-667,-257));radar=graphRoot.gameObject.AddComponent<DriverRadar>();
            Label(graphRoot,"RISK",13,new Vector2(100,22),new Vector2(-14,103));Label(graphRoot,"BRAKING",13,new Vector2(94,22),new Vector2(109,-67));Label(graphRoot,"CONSISTENCY",13,new Vector2(123,22),new Vector2(-109,-67));
            driverText=Label(paper,"",20,new Vector2(470,235),new Vector2(-297,-244));
            part=View(paper,"Selected part",new Vector2(250,145),new Vector2(172,-217));partText=Label(paper,"",20,new Vector2(455,226),new Vector2(557,-225));
            Label(paper,"L1 / R1: parts  ·  Right stick: turn car  ·  Click a part number to inspect",17,new Vector2(1510,32),new Vector2(-37,-371));
            close=CoastMenu.Button(paper,"Back to grid","Back to grid",new Vector2(340,56),new Vector2(-606,-423),Hide);
            motion=CoastMenu.Button(paper,"Paddock animation","",new Vector2(370,56),new Vector2(585,-423),()=>SetAnimation(!animate));motionText=motion.GetComponentInChildren<Text>();
            close.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnRight=motion,selectOnUp=hotspots[0]};motion.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=close,selectOnUp=hotspots[6]};
        }
        public void SelectPart(int index,bool pause=true)
        {
            selected=(index+7)%7;if(pause)SetAnimation(false);var id=SelectedPartId;var data=EquipmentCatalog.Get(id);stage.Select(id);
            var own=EquipmentCatalog.Get(player.Parts[data.slot]);partText.text=$"{selected+1}. {data.name}\n{data.hint}\n\nYour car: {own.name}";
        }
        public void ClickPart(int index)=>hotspots[index].onClick.Invoke();
        public void SetAnimation(bool enabled){animate=enabled;if(motionText!=null)motionText.text=animate?"Pause display motion":"Resume display motion";}
        void RenderScene()
        {
            stage.RenderOrbit(orbitAngle);for(int i=0;i<7;i++){var p=stage.OrbitPosition(i);((RectTransform)hotspots[i].transform).anchoredPosition=new Vector2((p.x-.5f)*orbitRect.sizeDelta.x,(p.y-.5f)*orbitRect.sizeDelta.y-37);}
            motionText.text=animate?"Pause display motion":"Resume display motion";
        }
        void Update()
        {
            if(!IsOpen)return;var k=Keyboard.current;var pad=Gamepad.current;
            if(k?.escapeKey.wasPressedThisFrame==true||pad?.buttonEast.wasPressedThisFrame==true){Hide();return;}
            if(k?.leftArrowKey.wasPressedThisFrame==true||pad?.leftShoulder.wasPressedThisFrame==true)SelectPart(selected-1);
            if(k?.rightArrowKey.wasPressedThisFrame==true||pad?.rightShoulder.wasPressedThisFrame==true)SelectPart(selected+1);
            float turn=pad?.rightStick.x.ReadValue()??0;
            if(Time.unscaledTime<nextFrame)return;nextFrame=Time.unscaledTime+.05f;
            if(animate||Mathf.Abs(turn)>.1f){stage.RotateCar(Mathf.Abs(turn)>.1f?turn*3:.65f);if(animate){orbitAngle=Mathf.Repeat(orbitAngle+.38f,360);RenderScene();stage.Rotate(.6f);}}
        }
        public void Hide(){if(root==null)return;root.gameObject.SetActive(false);LastCloseFrame=Time.frameCount;closed?.Invoke();}
    }
}
