using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LittleRoad
{
    public struct RaceStanding {public int place;public string name;public Color paint;public bool player,finished;public float time;public RaceEntrant entrant;}
    // Everything the results screen shows; built by RaceTrial when the player finishes.
    public sealed class RaceResultSummary
    {
        public string venue="Bayview Coast Cup";
        public int place,entrants,grid,reward,wallet,laps;
        public float finishTime,bestLap;
        public List<Vector2> trace=new(); // x = race distance in laps, y = place
        public Func<List<RaceStanding>> standings; // live: rivals still finishing
    }
    // Post-race celebration: stamped place, podium, live standings, credits,
    // and a "Race story" chart of the player's place lap by lap.
    public sealed class RaceResultScreen:MonoBehaviour
    {
        RaceResultSummary summary;RectTransform root,podium,story;readonly List<Text[]> rows=new();readonly List<ToyCarIcon> rowCars=new();readonly List<ToyShape> rowBacks=new();
        readonly List<RenderTexture> renders=new();string podiumKey="";float nextRefresh;Button storyClose,storyButton;
        public Text TimeLabel {get;private set;}
        public readonly List<Button> Choices=new();
        public bool StoryOpen=>story!=null&&story.gameObject.activeSelf;
        public void Build(Transform canvas,RaceResultSummary data,Action returnToTown,Action retry)
        {
            summary=data;root=Toy.Rect(canvas,"Race results",new Vector2(1920,1080),Vector2.zero);
            var mask=root.gameObject.AddComponent<RectMask2D>();mask.softness=Vector2Int.zero;
            Toy.Sunburst(root,new Vector2(2600,2600),new Vector2(0,-40),Toy.Orange,Toy.OrangeLight,32,3);
            root.gameObject.AddComponent<Image>().color=new Color(0,0,0,0);
            var confetti=Toy.Rect(root,"Confetti",new Vector2(1920,1080),Vector2.zero).gameObject.AddComponent<ToyConfetti>();confetti.Setup(new Vector2(1920,1080),46);
            Toy.Checker(root,1920,525);Toy.Checker(root,1920,-525);
            Toy.Title(root,"Heading","RACE RESULTS",92,new Vector2(1200,120),new Vector2(0,432),Color.white,TextAnchor.MiddleCenter);
            Toy.Label(root,"Venue",data.venue.ToUpperInvariant(),28,new Vector2(900,40),new Vector2(0,362),Toy.Navy,TextAnchor.MiddleCenter,true);

            // Stamped finishing place.
            var badge=Toy.Card(root,"Place badge",new Vector2(360,360),new Vector2(-705,140),Toy.Sky,180,9,12);
            var burst=Toy.Sunburst(badge.transform,new Vector2(330,330),Vector2.zero,Toy.Sky,Toy.SkyLight,16,-10);burst.circular=true;
            Toy.Title(badge.transform,"Finishing position",Toy.Ordinal(data.place),data.place>=10?100:116,new Vector2(360,150),new Vector2(0,26),Color.white,TextAnchor.MiddleCenter);
            Toy.Label(badge.transform,"Of field","of "+data.entrants,34,new Vector2(300,48),new Vector2(0,-86),Toy.Navy,TextAnchor.MiddleCenter,true);
            var stamp=badge.gameObject.AddComponent<ToyStamp>();stamp.tilt=-7;
            int gained=data.grid-data.place;
            TimeLabel=StatPill("Finish time",AdventureUI.FinishTimeLabel(data.finishTime),Toy.White,-110);
            StatPill("Best lap",data.bestLap>0&&data.bestLap<float.MaxValue?AdventureUI.FinishTimeLabel(data.bestLap):"-",Toy.White,-178);
            StatPill(gained>=0?"Places gained":"Places lost",(gained>0?"+":"")+gained+"  (from "+Toy.Ordinal(data.grid)+")",gained>0?Toy.Mint:gained<0?Toy.Faster:Toy.White,-246);

            podium=Toy.Rect(root,"Podium",new Vector2(620,560),new Vector2(-150,40));

            // Credits.
            var coin=Toy.Card(root,"Credit coin",new Vector2(96,96),new Vector2(-330,-315),Toy.Sun,48,6,5);
            Toy.Title(coin.transform,"Coin mark","C",58,new Vector2(96,96),Vector2.zero,Toy.Navy,TextAnchor.MiddleCenter);
            coin.gameObject.AddComponent<ToyCoinFlip>();
            Toy.Title(root,"Earned credits","+"+data.reward,78,new Vector2(260,100),new Vector2(-150,-312),Toy.Sun,TextAnchor.MiddleCenter);
            Toy.Label(root,"Wallet","credits\nwallet "+data.wallet+" · saved",24,new Vector2(240,70),new Vector2(80,-315),Toy.Navy,TextAnchor.MiddleLeft,true);

            // Standings board.
            var board=Toy.Card(root,"Final standings",new Vector2(800,690),new Vector2(530,20),Toy.Cream,34,7,10);
            Toy.Title(board.transform,"Standings heading","FINAL STANDINGS",40,new Vector2(700,60),new Vector2(-10,300),Toy.Orange);
            for(int i=0;i<24;i++){
                int column=i/12,row=i%12;var pos=new Vector2(-190+column*382,242-row*45);
                var back=Toy.Card(board.transform,"Standing "+(i+1),new Vector2(366,40),pos,row%2==0?new Color(1,1,1,.8f):new Color(1,1,1,0),12,0,0);rowBacks.Add(back);
                var n=Toy.Title(back.transform,"Place",(i+1).ToString(),24,new Vector2(44,40),new Vector2(-152,0),Toy.White,TextAnchor.MiddleCenter);
                var car=Toy.Car(back.transform,"Car",new Vector2(46,30),new Vector2(-104,0),Color.white);rowCars.Add(car);
                var name=Toy.Label(back.transform,"Name","",21,new Vector2(170,40),new Vector2(10,0),Toy.Navy);
                var gap=Toy.Label(back.transform,"Gap","",20,new Vector2(90,40),new Vector2(132,0),Toy.Navy,TextAnchor.MiddleRight,true);
                rows.Add(new[]{n,name,gap});
            }

            var home=Toy.Candy(root,"Choice Return to Bayview","BACK TO BAYVIEW","O",new Vector2(520,96),new Vector2(-640,-445),Toy.Blue,returnToTown);
            storyButton=Toy.Candy(root,"Choice Race story","RACE STORY","[]",new Vector2(400,96),new Vector2(-110,-445),Toy.Pink,OpenStory);
            var again=Toy.Candy(root,"Choice Race again","RACE AGAIN!","X",new Vector2(500,106),new Vector2(560,-445),Toy.Gold,retry,true);
            Choices.AddRange(new[]{home,storyButton,again});
            home.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnRight=storyButton};
            storyButton.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=home,selectOnRight=again};
            again.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=storyButton};
            BuildStory();RefreshStandings(true);
        }
        Text StatPill(string caption,string value,Color fill,float y)
        {
            var p=Toy.Card(root,"Stat "+caption,new Vector2(430,58),new Vector2(-705,y),fill,29,5,5);
            Toy.Label(p.transform,"Caption",caption,24,new Vector2(220,58),new Vector2(-90,0),Toy.Navy,TextAnchor.MiddleLeft,true);
            return Toy.Label(p.transform,"Value",value,24,new Vector2(240,58),new Vector2(78,0),Toy.Navy,TextAnchor.MiddleRight,true);
        }
        void Update()
        {
            if(root==null)return;
            if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.5f;RefreshStandings(false);}
            if(StoryOpen){var k=Keyboard.current;var p=Gamepad.current;if(k?.escapeKey.wasPressedThisFrame==true||p?.buttonEast.wasPressedThisFrame==true)CloseStory();}
            else{var k=Keyboard.current;var p=Gamepad.current;if(k?.tabKey.wasPressedThisFrame==true||p?.buttonWest.wasPressedThisFrame==true)OpenStory();}
        }
        public void RefreshNow()=>RefreshStandings(false);
        void RefreshStandings(bool force)
        {
            var list=summary.standings?.Invoke();if(list==null)return;
            float winner=list.Count>0&&list[0].finished?list[0].time:0;
            for(int i=0;i<rows.Count;i++){
                if(i>=list.Count){rows[i][1].text="";rows[i][2].text="";continue;}
                var s=list[i];rowCars[i].color=s.paint;
                rows[i][1].text=s.player?"YOU":s.name;
                rows[i][2].text=!s.finished?"racing":i==0?AdventureUI.FinishTimeLabel(s.time):"+"+(s.time-winner).ToString("0.0");
                rowBacks[i].SetFill(s.player?Toy.Sun:i%12%2==0?new Color(1,1,1,.8f):new Color(1,1,1,0));if(rowBacks[i].border!=(s.player?4:0)){rowBacks[i].border=s.player?4:0;rowBacks[i].SetVerticesDirty();}rowBacks[i].SetShadow(s.player?new Vector2(4,-4):Vector2.zero);
            }
            string key=string.Join(",",list.Take(3).Where(s=>s.finished).Select(s=>s.name));
            if(force||key!=podiumKey){podiumKey=key;BuildPodium(list.Take(3).Where(s=>s.finished).ToList());}
        }
        void BuildPodium(List<RaceStanding> top)
        {
            foreach(Transform child in podium)Destroy(child.gameObject);foreach(var r in renders)if(r!=null){r.Release();Destroy(r);}renders.Clear();pending.Clear();
            // Indexed by finishing rank: winner in the middle and tallest.
            float[] height={290,210,150},x={0,-205,205};Color[] block={Toy.Sun,Toy.Silver,Toy.Bronze};
            for(int rank=0;rank<top.Count&&rank<3;rank++){
                var s=top[rank];float h=height[rank];
                var step=Toy.Card(podium,"Podium "+(rank+1),new Vector2(190,h),new Vector2(x[rank],-280+h*.5f),block[rank],16,6,7);
                Toy.Title(step.transform,"Step",(rank+1).ToString(),84,new Vector2(190,110),new Vector2(0,h*.5f-62),Color.white,TextAnchor.MiddleCenter);
                var holder=Toy.Rect(podium,"Podium car "+(rank+1),new Vector2(230,128),new Vector2(step.rectTransform.anchoredPosition.x,-280+h+74));
                Toy.Car(holder,"Podium icon",new Vector2(150,96),new Vector2(0,-10),s.paint);
                if(s.entrant!=null)pending.Add((holder,s.entrant));
                var bob=holder.gameObject.AddComponent<ToyBob>();bob.phase=rank*.7f;bob.height=10;bob.speed=3.5f;
                Toy.Label(podium,"Podium name "+(rank+1),s.player?"YOU!":s.name,24,new Vector2(220,36),new Vector2(step.rectTransform.anchoredPosition.x,-280+h+14),Toy.Navy,TextAnchor.MiddleCenter,true);
            }
        }
        // Podium cars get 3D renders, one per frame: all preview stages share one hidden
        // spot and Destroy is deferred, so two stages in the same frame would overlap.
        readonly List<(RectTransform holder,RaceEntrant entrant)> pending=new();
        void LateUpdate()
        {
            if(pending.Count==0)return;var (holder,entrant)=pending[0];pending.RemoveAt(0);if(holder==null)return;
            try {
                using var stage=new PartPreviewStage(entrant.Body,entrant.Paint,EquipmentCatalog.All.slots.Select(slot=>EquipmentCatalog.Get(entrant.Parts[slot])),Color.clear,showroom:false);
                var copy=new RenderTexture(stage.Car.width,stage.Car.height,0,RenderTextureFormat.ARGB32);Graphics.Blit(stage.Car,copy);renders.Add(copy);
                foreach(Transform child in holder)Destroy(child.gameObject);
                var image=holder.gameObject.AddComponent<RawImage>();image.texture=copy;image.raycastTarget=false;
            }catch(Exception ex){Debug.LogWarning("Podium render skipped: "+ex.Message);}
        }
        public bool PodiumReady=>pending.Count==0;
        void BuildStory()
        {
            story=Toy.Rect(root,"Race story",new Vector2(1920,1080),Vector2.zero);
            var dim=story.gameObject.AddComponent<Image>();dim.color=new Color(.05f,.08f,.2f,.55f);
            var card=Toy.Card(story,"Race story card",new Vector2(1380,800),new Vector2(0,10),Toy.Cream,40,8,12);
            Toy.Title(card.transform,"Story heading","RACE STORY",62,new Vector2(900,90),new Vector2(-190,320),Toy.Pink);
            Toy.Label(card.transform,"Story subtitle","Your place at every checkpoint",26,new Vector2(600,40),new Vector2(-330,262),Toy.Soft,TextAnchor.MiddleLeft,true);
            var plot=Toy.Card(card.transform,"Chart paper",new Vector2(1120,470),new Vector2(40,-30),Toy.White,24,5,0);
            var chart=Toy.Rect(plot.transform,"Place chart",new Vector2(1040,400),new Vector2(10,6)).gameObject.AddComponent<ToyLineChart>();chart.raycastTarget=false;
            chart.maxX=Mathf.Max(1,summary.laps);chart.maxPlace=Mathf.Max(2,summary.entrants);chart.points.AddRange(summary.trace);chart.SetVerticesDirty();
            foreach(int p in new[]{1,6,12,18,24}.Where(p=>p<=summary.entrants))
                Toy.Label(card.transform,"Place "+p,Toy.Ordinal(p),22,new Vector2(80,30),new Vector2(-560,-30+chart.rectTransform.rect.height*.5f+6-(p-1)*400f/Mathf.Max(1,summary.entrants-1)),Toy.Soft,TextAnchor.MiddleRight,true);
            for(int lap=0;lap<=summary.laps;lap++)
                Toy.Label(card.transform,"Lap "+lap,lap==0?"START":lap==summary.laps?"FINISH":"LAP "+lap,22,new Vector2(140,30),new Vector2(50-520+1040f*lap/Mathf.Max(1,summary.laps),-290),Toy.Soft,TextAnchor.MiddleCenter,true);
            int best=summary.trace.Count>0?Mathf.RoundToInt(summary.trace.Min(p=>p.y)):summary.place,worst=summary.trace.Count>0?Mathf.RoundToInt(summary.trace.Max(p=>p.y)):summary.place;
            Toy.Pill(card.transform,"Best place","HIGHEST  "+Toy.Ordinal(best).ToUpperInvariant(),24,new Vector2(290,54),new Vector2(270,318),Toy.Mint,Toy.Navy,-3);
            Toy.Pill(card.transform,"Lowest place","LOWEST  "+Toy.Ordinal(worst).ToUpperInvariant(),24,new Vector2(290,54),new Vector2(560,318),Toy.Faster,Toy.Navy,3);
            storyClose=Toy.Candy(card.transform,"Close race story","BACK","O",new Vector2(300,84),new Vector2(0,-352),Toy.Red,CloseStory);
            story.gameObject.SetActive(false);
        }
        public void OpenStory(){if(story==null)return;story.gameObject.SetActive(true);EventSystem.current?.SetSelectedGameObject(storyClose.gameObject);}
        public void CloseStory(){if(story==null)return;story.gameObject.SetActive(false);EventSystem.current?.SetSelectedGameObject(storyButton.gameObject);}
        void OnDestroy(){foreach(var r in renders)if(r!=null){r.Release();Destroy(r);}}
        public void Close(){if(root!=null)Destroy(root.gameObject);Destroy(this);}
    }
    public sealed class ToyCoinFlip:MonoBehaviour{void Update()=>transform.localScale=new Vector3(Mathf.Abs(Mathf.Cos(Time.unscaledTime*2.2f))*.9f+.1f,1,1);}
}
