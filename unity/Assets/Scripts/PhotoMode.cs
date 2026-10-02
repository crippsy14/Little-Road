using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace LittleRoad
{
    // Photo mode (Claude's own addition, 30 Sep; docs/51). P / D-pad Up in free roam: the world freezes, the HUD goes,
    // and a camera orbits your car. Five looks (a post-processing volume of its own, above the world's grade, only
    // while the mode is open), depth of field on the car, and a "Greetings from ..." postcard frame to fit the
    // game's postcards. Photos save to Pictures\Little Road.
    public sealed class PhotoMode:MonoBehaviour
    {
        public bool Active {get;private set;}
        // After each photo: which of Pip's requests it shows (AdventureGame pays them).
        public Action<Camera> Photographed;
        public int LastCloseFrame {get;private set;}=-1;int openedFrame=-1;
        Camera view;Transform car;AdventureUI ui;Func<string> area;Action<string> notify;Action<bool> hudShown;
        float yaw,pitch=10,distance=7,lift=.9f,fov=45;int look;bool frame=true,focus=true,shooting;
        Volume volume;ColorAdjustments colour;WhiteBalance white;UnityEngine.Rendering.Universal.Vignette vignette;FilmGrain grain;DepthOfField depth;
        Canvas canvas;Text help,lookName;RectTransform frameRoot;Text caption;
        static readonly string[] Looks={"Natural","Golden hour","Postcard","Faded film","Black & white"};

        public void Setup(Camera cameraView,Transform playerCar,AdventureUI adventureUi,Func<string> areaName,Action<string> toast,Action<bool> showHud)
        {
            view=cameraView;car=playerCar;ui=adventureUi;area=areaName;notify=toast;hudShown=showHud;
            var go=new GameObject("Photo mode look");go.transform.SetParent(transform,false);volume=go.AddComponent<Volume>();volume.isGlobal=true;volume.priority=100;volume.weight=0;
            var profile=volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            colour=profile.Add<ColorAdjustments>();white=profile.Add<WhiteBalance>();vignette=profile.Add<UnityEngine.Rendering.Universal.Vignette>();grain=profile.Add<FilmGrain>();depth=profile.Add<DepthOfField>();
            depth.mode.Override(DepthOfFieldMode.Bokeh);depth.focalLength.Override(70);depth.aperture.Override(4);
            BuildUi();
        }
        void BuildUi()
        {
            canvas=CoastUI.Canvas(transform,"Photo mode",45);canvas.gameObject.layer=Toy.MenuLayer;var root=(RectTransform)canvas.transform;
            // The postcard frame: a cream border and a caption (captured with the photo).
            frameRoot=Toy.Rect(root,"Postcard frame",new Vector2(1920,1080),Vector2.zero);
            foreach(var (size,pos) in new[]{(new Vector2(1920,46),new Vector2(0,517)),(new Vector2(1920,150),new Vector2(0,-465)),(new Vector2(46,1080),new Vector2(-937,0)),(new Vector2(46,1080),new Vector2(937,0))}){var bar=Toy.Rect(frameRoot,"Border",size,pos).gameObject.AddComponent<Image>();bar.color=Toy.Cream;bar.raycastTarget=false;}
            caption=Toy.Title(frameRoot,"Caption","",64,new Vector2(1600,90),new Vector2(-40,-462),Toy.Orange,TextAnchor.MiddleLeft,-2);
            var stamp=Toy.Card(frameRoot,"Stamp",new Vector2(120,140),new Vector2(830,-455),Toy.Sky,8,5,4);stamp.rectTransform.localRotation=Quaternion.Euler(0,0,4);
            Toy.Title(stamp.transform,"Stamp mark","LR",40,new Vector2(110,60),new Vector2(0,10),Toy.White,TextAnchor.MiddleCenter);Toy.Label(stamp.transform,"Stamp value","2026",18,new Vector2(110,30),new Vector2(0,-42),Toy.White,TextAnchor.MiddleCenter,true);
            // Controls and the look's name (hidden while the photo is taken).
            var pill=Toy.Card(root,"Photo help",new Vector2(1500,54),new Vector2(0,-300),Toy.Navy,27,4,4);pill.borderColor=Toy.White;
            help=Toy.Label(pill.transform,"Photo help text","",21,new Vector2(1460,54),Vector2.zero,Toy.White,TextAnchor.MiddleCenter,true);
            var name=Toy.Card(root,"Photo look",new Vector2(420,58),new Vector2(0,380),Toy.Sun,29,5,5);lookName=Toy.Title(name.transform,"Look name","",32,new Vector2(400,58),Vector2.zero,Toy.White,TextAnchor.MiddleCenter);
            canvas.gameObject.SetActive(false);
        }

        public void Open()
        {
            if(Active)return;Active=true;openedFrame=Time.frameCount;Time.timeScale=0;hudShown?.Invoke(false);
            var d=view.transform.position-car.position;yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;distance=Mathf.Clamp(new Vector2(d.x,d.z).magnitude,3,14);pitch=Mathf.Clamp(Mathf.Asin(Mathf.Clamp(d.y/Mathf.Max(.1f,d.magnitude),-1,1))*Mathf.Rad2Deg,-5,70);
            fov=view.fieldOfView;volume.weight=1;canvas.gameObject.SetActive(true);Apply();
        }
        public void Close()
        {
            if(!Active)return;Active=false;LastCloseFrame=Time.frameCount;Time.timeScale=1;volume.weight=0;canvas.gameObject.SetActive(false);hudShown?.Invoke(true);
        }
        void Apply()
        {
            // Relative to the world's grade (WorldLook): each look nudges it, it never replaces it.
            float sat=0,con=0,exp=0,temp=0,tint=0,vig=.18f,grainAmount=0;Color filterColour=Color.white;
            switch(look){
                case 1:temp=32;tint=6;sat=8;exp=.12f;vig=.3f;break;
                case 2:sat=28;con=14;exp=.05f;vig=.42f;temp=8;break;
                case 3:sat=-38;con=-14;exp=.1f;vig=.36f;filterColour=new Color(1,.95f,.86f);grainAmount=.45f;break;
                case 4:sat=-100;con=26;vig=.4f;grainAmount=.35f;break;
            }
            colour.saturation.Override(sat);colour.contrast.Override(con);colour.postExposure.Override(exp);colour.colorFilter.Override(filterColour);
            white.temperature.Override(temp);white.tint.Override(tint);vignette.intensity.Override(vig);vignette.smoothness.Override(.45f);
            grain.intensity.Override(grainAmount);grain.active=grainAmount>0;depth.active=focus;
            lookName.text=Looks[look].ToUpperInvariant();frameRoot.gameObject.SetActive(frame);caption.text="Greetings from "+Title(area());
            bool pad=InputGlyphs.Current!=InputGlyphs.Family.Keyboard;
            help.text=pad?"Right stick: orbit   Left stick: closer / height   LB / RB: zoom   Y: look   X: frame   D-pad Left: focus   A: take photo   B: back"
                         :"Arrows / mouse: orbit   W / S: closer   Q / E: height   Z / X: zoom   F: look   G: frame   V: focus   Enter: take photo   Esc: back";
        }
        static string Title(string s){s=(s??"").ToLowerInvariant();var w=s.Split(' ');for(int i=0;i<w.Length;i++)if(w[i].Length>0)w[i]=char.ToUpperInvariant(w[i][0])+w[i][1..];return string.Join(" ",w);}

        void Update()
        {
            // (Not on the frame it opened: the same D-pad Up / P press then read as "close" and it shut at once. Richard, 1 Oct.)
            if(!Active||shooting||Time.frameCount==openedFrame)return;
            var k=Keyboard.current;var pad=Gamepad.current;var mouse=Mouse.current;float dt=Time.unscaledDeltaTime;
            if(k?.escapeKey.wasPressedThisFrame==true||k?.pKey.wasPressedThisFrame==true||pad?.buttonEast.wasPressedThisFrame==true||pad?.dpad.up.wasPressedThisFrame==true){Close();return;}
            Vector2 orbit=Vector2.zero,move=Vector2.zero;float zoom=0;
            if(pad!=null){orbit+=pad.rightStick.ReadValue();move+=pad.leftStick.ReadValue();zoom+=(pad.rightShoulder.isPressed?1:0)-(pad.leftShoulder.isPressed?1:0);}
            if(k!=null){orbit+=new Vector2((k.rightArrowKey.isPressed?1:0)-(k.leftArrowKey.isPressed?1:0),(k.upArrowKey.isPressed?1:0)-(k.downArrowKey.isPressed?1:0));
                move+=new Vector2((k.eKey.isPressed?1:0)-(k.qKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));zoom+=(k.xKey.isPressed?1:0)-(k.zKey.isPressed?1:0);}
            if(mouse!=null&&(mouse.leftButton.isPressed||mouse.rightButton.isPressed))orbit+=mouse.delta.ReadValue()*.08f;
            if(mouse!=null)distance-=mouse.scroll.ReadValue().y*.004f;
            yaw+=orbit.x*90*dt;pitch=Mathf.Clamp(pitch-orbit.y*60*dt,-8,75);
            distance=Mathf.Clamp(distance-move.y*6*dt,2.2f,18);lift=Mathf.Clamp(lift+move.x*2*dt,.2f,4);fov=Mathf.Clamp(fov-zoom*25*dt,18,80);
            if(k?.fKey.wasPressedThisFrame==true||pad?.buttonNorth.wasPressedThisFrame==true){look=(look+1)%Looks.Length;Apply();}
            if(k?.gKey.wasPressedThisFrame==true||pad?.buttonWest.wasPressedThisFrame==true){frame=!frame;Apply();}
            if(k?.vKey.wasPressedThisFrame==true||pad?.dpad.left.wasPressedThisFrame==true){focus=!focus;Apply();}
            if(k?.enterKey.wasPressedThisFrame==true||k?.spaceKey.wasPressedThisFrame==true||pad?.buttonSouth.wasPressedThisFrame==true)StartCoroutine(Shoot());
            Place();
        }
        // Runs after AdventureGame's camera (which stands aside while the mode is open).
        void LateUpdate(){if(Active)Place();}
        void Place()
        {
            var target=car.position+Vector3.up*lift;var dir=Quaternion.Euler(pitch,yaw,0)*Vector3.back;
            var at=target-dir*distance;
            foreach(var hit in Physics.RaycastAll(target,-dir,distance))if(!hit.collider.transform.IsChildOf(car)&&!hit.collider.isTrigger)at=target-dir*Mathf.Min(Vector3.Distance(target,at),hit.distance-.3f);
            view.transform.position=at;view.transform.LookAt(target);view.fieldOfView=fov;
            depth.focusDistance.Override(Vector3.Distance(at,target));depth.focalLength.Override(Mathf.Lerp(40,110,Mathf.InverseLerp(80,18,fov)));
        }
        public static string Folder=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),"Little Road");
        IEnumerator Shoot()
        {
            shooting=true;var pill=help.transform.parent.gameObject;var name=lookName.transform.parent.gameObject;pill.SetActive(false);name.SetActive(false);
            ui.SetVisible(false);yield return new WaitForEndOfFrame();
            string path=null;
            try{Directory.CreateDirectory(Folder);path=Path.Combine(Folder,$"Little Road {DateTime.Now:yyyy-MM-dd HH-mm-ss}.png");Toy.Capture(path,Screen.width,Screen.height);}
            catch(Exception e){Debug.LogException(e);path=null;}
            pill.SetActive(true);name.SetActive(true);ui.SetVisible(true);
            AudioDirector.Beep(2637,.04f);
            notify?.Invoke(path!=null?"Photo saved to Pictures\\Little Road":"Couldn't save the photo");Debug.Log("LITTLE_ROAD_PHOTO "+path);if(path!=null)Photographed?.Invoke(view);
            shooting=false;
        }
        // Reviews: a photo with a given look, framed or not, to a file.
        public IEnumerator Review(string file,int withLook,bool withFrame)
        {
            look=withLook;frame=withFrame;Apply();Place();yield return null;yield return new WaitForEndOfFrame();
            var pill=help.transform.parent.gameObject;var name=lookName.transform.parent.gameObject;pill.SetActive(false);name.SetActive(false);ui.SetVisible(false);Toy.Capture(file);ui.SetVisible(true);pill.SetActive(true);name.SetActive(true);
        }
    }
}
