using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LittleRoad
{
    // Gwen's rally licence, Gran Turismo style (Richard, 4 Oct). The licence screen lists the six tests down the left;
    // the focused one's card shows what to do, the medal times and prizes, and your best. Pick a test to start it, or
    // exit the licences (the car goes back to Gwen). After every attempt a result card offers Try again, Next test and
    // Back to licences. Built on the adventure canvas like the race results; AdventureUI owns the one open screen.
    public sealed class LicenceScreen:MonoBehaviour
    {
        public sealed class Test{public string name,how;public float gold,silver,bronze,best;public int medal;}
        public sealed class Attempt{public int index;public bool failed;public string reason,extra;public float time,penalty;public int medal,paid;public bool newBest;}
        RectTransform root;Action back;readonly List<Button> tiles=new();int shown=-1;
        Text cardNumber,cardName,cardHow,cardBest;readonly Text[] targetTimes=new Text[3],targetPrizes=new Text[3];readonly ToyShape[] targetRows=new ToyShape[3];ToyShape bestPill;
        List<Test> tests;int[] prizes;
        public readonly List<Button> Choices=new();
        public bool IsResult{get;private set;}
        public int Focused=>shown;
        static readonly Color[] MedalColours={Toy.Gold,Toy.Silver,Toy.Bronze};static readonly string[] MedalNames={"GOLD","SILVER","BRONZE"};
        static Color MedalColour(int m)=>m==3?Toy.Gold:m==2?Toy.Silver:m==1?Toy.Bronze:Toy.White;

        RectTransform Backdrop(Transform canvas,string name,Color a,Color b)
        {
            root=Toy.Rect(canvas,name,new Vector2(1920,1080),Vector2.zero);root.gameObject.AddComponent<RectMask2D>().softness=Vector2Int.zero;
            Toy.Sunburst(root,new Vector2(2600,2600),new Vector2(0,-40),a,b,28,2);root.gameObject.AddComponent<Image>().color=new Color(0,0,0,0);
            Toy.Checker(root,1920,525);Toy.Checker(root,1920,-525);return root;
        }

        // ---- the licence screen
        public void BuildLicences(Transform canvas,string school,IReadOnlyList<Test> list,int[] prizeTable,int focus,Action<int> start,Action exit)
        {
            tests=new List<Test>(list);prizes=prizeTable;back=exit;IsResult=false;
            Backdrop(canvas,"Rally licence",Toy.Mint,Toy.Hex(0x7af0c0));
            Toy.Title(root,"Heading","RALLY LICENCE",92,new Vector2(1200,120),new Vector2(0,440),Color.white,TextAnchor.MiddleCenter);
            Toy.Label(root,"School",school.ToUpperInvariant(),28,new Vector2(900,40),new Vector2(0,372),Toy.Navy,TextAnchor.MiddleCenter,true);
            int golds=0;foreach(var t in tests)if(t.medal==3)golds++;
            Toy.Pill(root,"Gold count",$"GOLD IN {golds} OF {tests.Count}",24,new Vector2(300,50),new Vector2(700,372),golds==tests.Count?Toy.Gold:Toy.White,Toy.Navy,3);

            // The test list: a tile per test, its medal on the right.
            var column=Toy.Rect(root,"Tests",new Vector2(620,760),new Vector2(-560,-20));
            for(int i=0;i<tests.Count;i++){
                int k=i;var t=tests[i];
                var tile=Toy.Candy(column,"Choice Test "+(i+1)+" "+t.name,"",null,new Vector2(600,88),new Vector2(0,300-i*104),i%2==0?Toy.Blue:Toy.Sky,()=>start(k));
                var face=tile.transform.Find("Face");
                Toy.Title(face,"Number",(i+1).ToString(),52,new Vector2(70,88),new Vector2(-250,2),Color.white,TextAnchor.MiddleCenter);
                Toy.Title(face,"Name",t.name.ToUpperInvariant(),34,new Vector2(360,88),new Vector2(-20,2),Color.white,TextAnchor.MiddleLeft);
                var chip=Toy.Card(face,"Medal",new Vector2(64,64),new Vector2(250,0),t.medal>0?MedalColour(t.medal):new Color(1,1,1,.35f),32,4,0);
                if(t.medal>0)Toy.Label(chip.transform,"Medal letter",MedalNames[3-t.medal][..1],30,new Vector2(64,64),Vector2.zero,Toy.Navy,TextAnchor.MiddleCenter,true);
                var hover=tile.gameObject.AddComponent<EventTrigger>();var enter=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};
                enter.callback.AddListener(_=>EventSystem.current?.SetSelectedGameObject(tile.gameObject));hover.triggers.Add(enter);
                tiles.Add(tile);Choices.Add(tile);
            }
            var exitButton=Toy.Candy(column,"Choice Back Exit licences","EXIT LICENCES","O",new Vector2(520,84),new Vector2(0,300-tests.Count*104-14),Toy.Red,exit);Choices.Add(exitButton);
            for(int i=0;i<Choices.Count;i++)Choices[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=Choices[(i+Choices.Count-1)%Choices.Count],selectOnDown=Choices[(i+1)%Choices.Count]};

            // The test card.
            var card=Toy.Card(root,"Test card",new Vector2(1060,760),new Vector2(330,-20),Toy.Cream,40,7,12);
            cardNumber=Toy.Title(card.transform,"Test number","",34,new Vector2(400,50),new Vector2(-300,320),Toy.Orange);
            cardName=Toy.Title(card.transform,"Test name","",64,new Vector2(900,90),new Vector2(-40,262),Toy.Sky);
            cardHow=Toy.Label(card.transform,"How","",30,new Vector2(940,170),new Vector2(0,120),Toy.Navy,TextAnchor.UpperLeft);cardHow.lineSpacing=1.1f;
            Toy.Label(card.transform,"Targets heading","TARGET TIMES",26,new Vector2(400,40),new Vector2(-270,-10),Toy.Soft,TextAnchor.MiddleLeft,true);
            for(int m=0;m<3;m++){
                var row=Toy.Card(card.transform,"Target "+MedalNames[m],new Vector2(900,66),new Vector2(0,-62-m*78),MedalColours[m],33,5,5);targetRows[m]=row;
                Toy.Title(row.transform,"Medal",MedalNames[m],34,new Vector2(260,66),new Vector2(-300,2),Color.white);
                targetTimes[m]=Toy.Label(row.transform,"Time","",34,new Vector2(260,66),new Vector2(20,0),Toy.Navy,TextAnchor.MiddleCenter,true);
                targetPrizes[m]=Toy.Label(row.transform,"Prize","",26,new Vector2(260,66),new Vector2(300,0),Toy.Navy,TextAnchor.MiddleRight,true);
            }
            bestPill=Toy.Card(card.transform,"Your best",new Vector2(620,62),new Vector2(-140,-318),Toy.White,31,5,5);
            cardBest=Toy.Label(bestPill.transform,"Best","",28,new Vector2(580,62),Vector2.zero,Toy.Navy,TextAnchor.MiddleCenter,true);
            Toy.Label(card.transform,"Start hint","X  START TEST",26,new Vector2(300,62),new Vector2(330,-318),Toy.Soft,TextAnchor.MiddleRight,true);
            Focus(Mathf.Clamp(focus,0,tests.Count-1));
            StartCoroutine(SelectNextFrame(tiles[Mathf.Clamp(focus,0,tests.Count-1)]));
        }
        void Focus(int i)
        {
            if(i==shown||tests==null||i<0||i>=tests.Count)return;shown=i;var t=tests[i];
            cardNumber.text=$"TEST {i+1} OF {tests.Count}";cardName.text=t.name.ToUpperInvariant();cardHow.text=t.how+"\nKnocked cones cost 2 s each.";
            float[] times={t.gold,t.silver,t.bronze};
            for(int m=0;m<3;m++){targetTimes[m].text=Clock(times[m]);int prize=prizes[2-m];bool earned=t.medal>=3-m;
                targetPrizes[m].text=earned?"EARNED":$"+{prize} credits";targetRows[m].color=earned?MedalColours[m]:Color.Lerp(MedalColours[m],Color.white,.55f);}
            cardBest.text=t.medal>0||t.best>0?$"YOUR BEST  {Clock(t.best)}  ·  {RallySchool.MedalName(t.medal).ToUpperInvariant()}":"NOT YET ATTEMPTED";
            bestPill.color=t.medal>0?MedalColour(t.medal):Toy.White;
            for(int k=0;k<tiles.Count;k++)tiles[k].transform.localScale=Vector3.one*(k==i?1.04f:1);
        }
        public static string Clock(float s)=>s>=60?$"{(int)s/60}:{s%60:00.0}":$"{s:0.0} s";

        // ---- the result card after an attempt
        public void BuildResult(Transform canvas,Test test,int count,Attempt a,Action again,Action next,Action licences)
        {
            back=licences;IsResult=true;bool medal=!a.failed&&a.medal>0;
            Backdrop(canvas,"Rally licence result",medal?MedalColour(a.medal):Toy.Sky,medal?Color.Lerp(MedalColour(a.medal),Color.white,.4f):Toy.SkyLight);
            if(medal){var confetti=Toy.Rect(root,"Confetti",new Vector2(1920,1080),Vector2.zero).gameObject.AddComponent<ToyConfetti>();confetti.Setup(new Vector2(1920,1080),40);}
            Toy.Title(root,"Heading",a.failed?"TEST FAILED":medal?"TEST CLEARED!":"NO MEDAL",92,new Vector2(1200,120),new Vector2(0,440),Color.white,TextAnchor.MiddleCenter);
            Toy.Label(root,"Test",$"RALLY LICENCE · TEST {a.index+1} OF {count} · {test.name.ToUpperInvariant()}",28,new Vector2(1200,40),new Vector2(0,372),Toy.Navy,TextAnchor.MiddleCenter,true);
            var card=Toy.Card(root,"Result card",new Vector2(1200,560),new Vector2(0,30),Toy.Cream,40,7,12);
            var badge=Toy.Card(card.transform,"Medal badge",new Vector2(300,300),new Vector2(-380,40),medal?MedalColour(a.medal):Toy.Faster,150,8,10);
            Toy.Title(badge.transform,"Medal",a.failed?"X":medal?MedalNames[3-a.medal]:"—",a.failed?150:medal&&a.medal==2?58:70,new Vector2(300,160),new Vector2(0,6),Color.white,TextAnchor.MiddleCenter);
            badge.gameObject.AddComponent<ToyStamp>().tilt=-6;
            Toy.Title(card.transform,"Time",a.failed?"—":Clock(a.time),96,new Vector2(600,130),new Vector2(200,170),Toy.Sun,TextAnchor.MiddleCenter);
            string detail=a.failed?a.reason:(a.penalty>0?$"Includes +{a.penalty:0} s for cones. ":"")+(medal?(a.newBest?"A new best! ":$"Your best is {Clock(test.best)}. "):$"Bronze needs {Clock(test.bronze)}. ");
            if(!a.failed&&a.medal<3)detail+=$"Gold is {Clock(test.gold)}.";
            Toy.Label(card.transform,"Detail",detail,30,new Vector2(620,150),new Vector2(200,30),Toy.Navy,TextAnchor.MiddleCenter);
            if(a.paid>0)Toy.Pill(card.transform,"Paid",$"+{a.paid} CREDITS",30,new Vector2(320,62),new Vector2(200,-80),Toy.Sun,Toy.Navy,-2);
            if(!string.IsNullOrEmpty(a.extra))Toy.Label(card.transform,"Extra",a.extra,26,new Vector2(1060,80),new Vector2(0,-190),Toy.Soft,TextAnchor.MiddleCenter,true);

            var list=Toy.Candy(root,"Choice Back to licences","BACK TO LICENCES","O",new Vector2(520,96),new Vector2(-600,-445),Toy.Blue,licences);
            Button nextButton=null;if(next!=null)nextButton=Toy.Candy(root,"Choice Next test","NEXT TEST","[]",new Vector2(400,96),new Vector2(-70,-445),Toy.Pink,next);
            var retry=Toy.Candy(root,"Choice Try again","TRY AGAIN","X",new Vector2(460,106),new Vector2(540,-445),Toy.Gold,again,true);
            Choices.Add(list);if(nextButton!=null)Choices.Add(nextButton);Choices.Add(retry);
            for(int i=0;i<Choices.Count;i++)Choices[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=i>0?Choices[i-1]:null,selectOnRight=i+1<Choices.Count?Choices[i+1]:null};
            StartCoroutine(SelectNextFrame(retry));
        }

        System.Collections.IEnumerator SelectNextFrame(Button b){yield return null;if(b!=null)EventSystem.current?.SetSelectedGameObject(b.gameObject);}
        void Update()
        {
            if(root==null)return;
            if(!IsResult){var sel=EventSystem.current?.currentSelectedGameObject;if(sel!=null){int i=tiles.FindIndex(t=>t.gameObject==sel);if(i>=0)Focus(i);}}
            var k=Keyboard.current;var p=Gamepad.current;
            // Square is the shortcut for Next test on the result card.
            if(IsResult&&(k?.tabKey.wasPressedThisFrame==true||p?.buttonWest.wasPressedThisFrame==true))Choices.Find(c=>c.name=="Choice Next test")?.onClick.Invoke();
        }
        // B / Esc (through AdventureUI.Back): the result card goes back to the licences; the licences exit to Gwen.
        public void Back()=>back?.Invoke();
        public void Close(){if(root!=null)Destroy(root.gameObject);Destroy(this);}
    }
}
