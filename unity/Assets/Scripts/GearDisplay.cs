using UnityEngine;
using UnityEngine.UI;

namespace LittleRoad
{
    // Gear, AUTO/MANUAL and a segmented rev strip for the driving dashes (docs/47 stage 3). The strip runs from
    // idle to the redline: green, amber over peak power, red at the top; it flashes as a shift light near the
    // redline. Cars without a drivetrain (--legacy-drivetrain) keep the old D/R selector and no strip.
    public sealed class GearDisplay
    {
        const int Segments=16;
        static readonly Color Off=new(1,1,1,.16f),Green=new(.55f,.92f,.55f),Amber=new(1,.78f,.3f),Red=new(1,.32f,.27f),Flash=new(.62f,.86f,1);
        readonly Text gear,mode;readonly Graphic[] lamps=new Graphic[Segments];readonly RectTransform strip;
        public GearDisplay(Text gear,Text mode,Transform dash,float width,float y)
        {
            this.gear=gear;this.mode=mode;
            strip=CoastUI.Panel(dash,"Rev strip",new Vector2(width,7),new Vector2(0,y),new Color(0,0,0,0),0);
            float step=width/Segments;
            for(int i=0;i<Segments;i++)lamps[i]=CoastUI.Panel(strip,"Rev lamp "+i,new Vector2(step-3,7),new Vector2(-width*.5f+step*(i+.5f),0),Off,1).GetComponent<CoastPanel>();
        }
        public void Show(RoadCar car)
        {
            var box=car.gears;
            if(box==null){gear.text=RaceHud.DriveIndicator(car.speed,car.throttle);mode.text="AUTO";strip.gameObject.SetActive(false);return;}
            strip.gameObject.SetActive(true);
            gear.text=Label(box);mode.text=box.Manual?"MANUAL":"AUTO";
            var train=box.train;float span=train.redline-train.idle;
            float lit=Mathf.Clamp01((box.Rpm-train.idle)/span)*Segments,power=(train.powerRpm-train.idle)/span*Segments;
            // Shift light: flash near the redline (and on the limiter) so a manual driver knows to change up.
            bool flash=box.Rpm>=train.redline*.96f&&box.Gear<train.Gears-1&&box.Gear>=0&&Mathf.Repeat(Time.unscaledTime*8,1)<.5f;
            for(int i=0;i<Segments;i++) {
                var on=i>=Segments-2?Red:i>=power?Amber:Green;
                lamps[i].color=flash?Flash:i<lit?on:Off;
            }
        }
        public static string Label(GearState box)=>box.Gear<0?"R":box.Shifting?"–":(box.Gear+1).ToString();
    }
}
