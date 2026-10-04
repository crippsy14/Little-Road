using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Gwen's rally school (docs/59; Richard, 3 Oct: the school comes before local races and parts). Six gravel lessons on the
    // quarry floor, each timed for bronze, silver and gold; each medal pays once, and gold in all six releases the rally
    // parts at Grit & Gravel (PartDiscovery "rallyschool"). A lesson lays out its cones, gates and boxes, holds the car for
    // a three-second countdown, then times it; a cone knocked down costs 2 s, a missed gate or an overshot box ends the go.
    public sealed class RallySchool:MonoBehaviour
    {
        public sealed class Lesson{public string id,name,how;public float gold,silver,bronze;}
        public static readonly Lesson[] Lessons={
            new(){id="lesson_0",name="Loose braking",how="Flat out down the yard, then stop dead inside the box. Gravel needs a straight line and a firm, early press.",gold=10.5f,silver=12.5f,bronze=25},
            new(){id="lesson_1",name="Slalom",how="Weave the six cones: the first on your left, then right, left... Smooth hands, small lifts.",gold=11.5f,silver=13.5f,bronze=25},
            new(){id="lesson_2",name="Handbrake hairpin",how="Out to the tyre stack, round it, and back through the start gate. Brake, turn in, then the handbrake to swing her round.",gold=14,silver=16.5f,bronze=30},
            new(){id="lesson_3",name="Scandinavian flick",how="Through the three gates of the S. Flick away from each bend before you turn in, and let the weight do the work.",gold=10.5f,silver=12.5f,bronze=25},
            new(){id="lesson_4",name="Crest and landing",how="Straight over the crest, keep it pointing true in the air, land and stop in the box beyond.",gold=11,silver=13,bronze=25},
            new(){id="lesson_5",name="Mini-stage",how="Everything together: one flying lap of the rallycross circuit.",gold=86,silver=95,bronze=115}};
        public static readonly int[] Prizes={60,100,150};
        public static bool Known(string id){foreach(var l in Lessons)if(l.id==id)return true;return false;}
        public static int Medal(Lesson l,float t)=>t<=l.gold?3:t<=l.silver?2:t<=l.bronze?1:0;
        public static string MedalName(int m)=>m switch{3=>"Gold",2=>"Silver",1=>"Bronze",_=>"No medal"};
        public static TrapRecord Record(AdventureProgress s,int i)=>s.traps?.Find(t=>t.id==Lessons[i].id);
        public static bool AllGold(AdventureProgress s){for(int i=0;i<Lessons.Length;i++){var r=Record(s,i);if(r==null||r.stars<3)return false;}return true;}
        public const string Unlock="rallyschool";

        // ---- the floor's layout (world x, z): the yard lane north of the loop, the hairpin and crest pads in the infield
        const float LaneZ=-2902,LaneStart=-2470,LaneFinish=-2330;
        static readonly Vector2 BrakeBoxMin=new(-2352,-2905.5f),BrakeBoxMax=new(-2340,-2898.5f);const float BrakeOvershoot=-2318;
        static readonly Vector2 PinStart=new(-2510,-3000),Pylon=new(-2440,-3000);
        const float CrestZ=-3030,CrestStart=-2560,RampFrom=-2496,RampTo=-2482;const float RampHeight=1.4f;
        static readonly Vector2 CrestBoxMin=new(-2440,-3033.5f),CrestBoxMax=new(-2428,-3026.5f);const float CrestOvershoot=-2410;
        static readonly float[] FlickX={-2420,-2394,-2368},FlickZ={-2902,-2892,-2902};

        public bool Active=>lesson>=0;public bool Holding=>Active&&Time.time<go;
        RoadCar player;Func<AdventureStore> store;Action<string> notify;Action<Vector3,float> place;Action<LicenceScreen.Attempt> finished;
        int lesson=-1,step;float go,penalty;Transform course;Vector3 last;
        readonly List<(Transform t,bool down)> cones=new();
        public void Setup(RoadCar car,Func<AdventureStore> s,Action<string> n,Action<Vector3,float> p,Action<LicenceScreen.Attempt> f=null){player=car;store=s;notify=n;place=p;finished=f;}
        public int Current=>lesson;
        static Vector3 G(float x,float z)=>new(x,ValleyField.Height(x,z),z);

        public void Begin(int i)
        {
            End();lesson=i;step=0;penalty=0;course=new GameObject("Rally lesson "+i).transform;var l=Lessons[i];
            switch(i) {
                case 0:Lane();Box(BrakeBoxMin,BrakeBoxMax);Setoff(G(LaneStart,LaneZ),90);break;
                case 1:Lane();for(int k=0;k<6;k++)Cone(G(-2440+k*16,LaneZ));Setoff(G(LaneStart,LaneZ),90);break;
                case 2:Paint(G(PinStart.x,PinStart.y-5),G(PinStart.x,PinStart.y+5),.4f);foreach(int s in new[]{-1,1})Cone(G(PinStart.x,PinStart.y+s*5.5f));
                    if(BayviewKit.Has("tyre_wall"))BayviewKit.Spawn("tyre_wall",G(Pylon.x,Pylon.y),0,course,null,null,true,1);else Cone(G(Pylon.x,Pylon.y));
                    Setoff(G(PinStart.x-14,PinStart.y),90);break;
                case 3:Lane();for(int k=0;k<3;k++)foreach(int s in new[]{-1,1})Cone(G(FlickX[k],FlickZ[k]+s*2.8f));Setoff(G(LaneStart,LaneZ),90);break;
                case 4:Paint(G(CrestStart+8,CrestZ-4),G(CrestStart+8,CrestZ+4),.4f);Box(CrestBoxMin,CrestBoxMax);Setoff(G(CrestStart,CrestZ),90);break;
                case 5:{var loop=QuarryFloor.Loop;int n=loop.Length,s0=StartIndex();var f=loop[(s0+2)%n]-loop[(s0+n-2)%n];f.y=0;
                    Setoff(loop[(s0-12+n)%n],Quaternion.LookRotation(f).eulerAngles.y);break;}
            }
            notify($"Rally school · {l.name} · Ready... GO in 3!");
        }
        void Lane(){Paint(G(LaneStart+10,LaneZ-4),G(LaneStart+10,LaneZ+4),.4f);Paint(G(LaneFinish,LaneZ-12),G(LaneFinish,LaneZ+12),.4f);}
        void Setoff(Vector3 at,float yaw){place(at,yaw);go=Time.time+3;last=at;}
        static int StartIndex()=>HillcrestRoute.Index(QuarryFloor.Loop,new Vector3(-2335,0,-2935));
        public void End(){lesson=-1;cones.Clear();if(course!=null)Destroy(course.gameObject);course=null;}

        // ---- course pieces: orange cones (knocked over on contact) and white paint
        static Mesh coneMesh;static Material orange,white;
        static void Materials()
        {
            if(coneMesh!=null)return;
            var v=new List<Vector3>{new(0,.75f,0)};var t=new List<int>();const int sides=14;
            for(int k=0;k<=sides;k++){float a=k*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Cos(a)*.24f,0,Mathf.Sin(a)*.24f));}
            for(int k=1;k<=sides;k++)t.AddRange(new[]{0,k+1,k});
            coneMesh=new Mesh{name="Cone"};coneMesh.SetVertices(v);coneMesh.SetTriangles(t,0);coneMesh.RecalculateNormals();
            orange=TrialWorld.Material("Cone orange",Toy.Hex(0xff7a1a),0,.3f);white=TrialWorld.Material("Lesson paint",new Color(.95f,.95f,.92f),0,.2f);
        }
        void Cone(Vector3 at)
        {
            Materials();
            var go=new GameObject("Cone",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(course);go.transform.position=at;
            go.GetComponent<MeshFilter>().sharedMesh=coneMesh;go.GetComponent<MeshRenderer>().sharedMaterial=orange;
            var foot=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(foot.GetComponent<Collider>());foot.transform.SetParent(go.transform,false);foot.transform.localScale=new Vector3(.5f,.04f,.5f);foot.GetComponent<Renderer>().sharedMaterial=orange;
            cones.Add((go.transform,false));
        }
        void Paint(Vector3 a,Vector3 b,float width)
        {
            Materials();
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(g.GetComponent<Collider>());g.transform.SetParent(course);var d=b-a;d.y=0;
            g.transform.SetPositionAndRotation((a+b)*.5f+Vector3.up*.05f,Quaternion.LookRotation(d));g.transform.localScale=new Vector3(width,.04f,d.magnitude);g.GetComponent<Renderer>().sharedMaterial=white;
        }
        void Box(Vector2 min,Vector2 max)
        {
            Paint(G(min.x,min.y),G(max.x,min.y),.3f);Paint(G(min.x,max.y),G(max.x,max.y),.3f);Paint(G(min.x,min.y),G(min.x,max.y),.3f);Paint(G(max.x,min.y),G(max.x,max.y),.3f);
        }

        // ---- the permanent crest on the infield (built with the floor): a gravel ramp up to a 1.4 m lip
        public static void BuildCrest(Transform parent,Material gravel)
        {
            var v=new List<Vector3>();var t=new List<int>();float half=3.6f;
            // Two-sided quads, each side with its own vertices (shared ones would average the normals away).
            void Q(Vector3 a,Vector3 b,Vector3 c,Vector3 d){foreach(bool back in new[]{false,true}){int s=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);t.AddRange(back?new[]{s,s+2,s+1,s,s+3,s+2}:new[]{s,s+1,s+2,s,s+2,s+3});}}
            float y0=ValleyField.Height(RampFrom,CrestZ),y1=ValleyField.Height(RampTo,CrestZ);
            Vector3 P(float x,float z,float y)=>new(x,y,z);
            var a0=P(RampFrom,CrestZ-half,y0);var a1=P(RampFrom,CrestZ+half,y0);var b0=P(RampTo,CrestZ-half,y1+RampHeight);var b1=P(RampTo,CrestZ+half,y1+RampHeight);
            Q(a0,a1,b1,b0);                                                                  // the slope
            Q(b0,b1,P(RampTo,CrestZ+half,y1-.2f),P(RampTo,CrestZ-half,y1-.2f));                // the lip's face
            Q(a0,b0,P(RampTo,CrestZ-half,y1-.2f),P(RampFrom,CrestZ-half,y0-.2f));Q(b1,a1,P(RampFrom,CrestZ+half,y0-.2f),P(RampTo,CrestZ+half,y1-.2f));
            var mesh=new Mesh{name="Crest"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject("Rally school crest",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=gravel;go.GetComponent<MeshCollider>().sharedMesh=mesh;
            // Chevrons either side of the run-up.
            foreach(int s in new[]{-1,1})if(BayviewKit.Has("chevron_sign"))BayviewKit.Spawn("chevron_sign",G(RampFrom-6,CrestZ+s*5.5f),90,parent,null,null,false,.8f);
        }

        // A clean line through lesson `i` (for --lesson-test): ground points the car is carried along.
        public static List<Vector3> TestPath(int i)
        {
            var w=new List<Vector3>();void A(float x,float z)=>w.Add(G(x,z));
            switch(i) {
                case 0:A(LaneStart,LaneZ);A((BrakeBoxMin.x+BrakeBoxMax.x)*.5f,LaneZ);break;
                case 1:A(LaneStart,LaneZ);for(int k=0;k<6;k++)A(-2440+k*16,LaneZ+(k%2==0?3:-3));A(LaneFinish+4,LaneZ);break;
                case 2:A(PinStart.x-14,PinStart.y);A(Pylon.x,Pylon.y+8);A(Pylon.x+9,Pylon.y);A(Pylon.x,Pylon.y-8);A(PinStart.x-6,PinStart.y);break;
                case 3:A(LaneStart,LaneZ);for(int k=0;k<3;k++)A(FlickX[k],FlickZ[k]);A(LaneFinish+4,LaneZ);break;
                case 4:A(CrestStart,CrestZ);A(RampTo+2,CrestZ);A((CrestBoxMin.x+CrestBoxMax.x)*.5f,CrestZ);break;
                case 5:{var loop=QuarryFloor.Loop;int n=loop.Length,s0=StartIndex();for(int k=-12;k<=n+2;k+=2)w.Add(loop[((s0+k)%n+n)%n]);break;}
            }
            return w;
        }
        // ---- the goal line for the HUD
        public string GoalText()
        {
            if(!Active)return null;var l=Lessons[lesson];
            if(Time.time<go)return $"Rally school · {l.name} · get ready!";
            float t=Time.time-go+penalty;return $"Rally school · {l.name} · {t:0.0} s · gold {l.gold:0.0} s"+(penalty>0?$" (+{penalty:0} s cones)":"");
        }

        void Update()
        {
            if(!Active||player==null)return;
            player.holdStill=Time.time<go;if(Time.time<go){last=player.body.position;return;}
            var p=player.body.position;float t=Time.time-go+penalty;var l=Lessons[lesson];
            if(t>(lesson==5?200:60)){Fail("Out of time");return;}
            for(int k=0;k<cones.Count;k++){var c=cones[k];if(c.down)continue;var d=c.t.position-p;d.y=0;
                if(d.magnitude<1.3f){c.t.rotation=Quaternion.LookRotation(-d.normalized)*Quaternion.Euler(80,0,0);cones[k]=(c.t,true);penalty+=2;notify("Cone! +2 s");}}
            bool stopped=player.body.linearVelocity.magnitude<.4f;
            switch(lesson) {
                case 0:if(p.x>BrakeOvershoot){Fail("Overshot the box");return;}if(stopped&&In(p,BrakeBoxMin,BrakeBoxMax))Done(t);break;
                case 1:for(int k=0;k<6;k++){float cx=-2440+k*16;if(last.x<cx&&p.x>=cx){bool left=p.z>LaneZ;if(left!=(k%2==0)){Fail($"Wrong side of cone {k+1}");return;}}}
                    if(p.x>=LaneFinish)Done(t);break;
                case 2:if(step==0&&p.x>Pylon.x+4&&Mathf.Abs(p.z-Pylon.y)<14)step=1;
                    if(Vector2.Distance(new(p.x,p.z),Pylon)>130){Fail("Lost the course");return;}
                    if(step==1&&last.x>PinStart.x&&p.x<=PinStart.x){if(Mathf.Abs(p.z-PinStart.y)<5.5f)Done(t);else{Fail("Missed the start gate");return;}}break;
                case 3:for(int k=0;k<3;k++)if(last.x<FlickX[k]&&p.x>=FlickX[k]&&Mathf.Abs(p.z-FlickZ[k])>2.6f){Fail($"Missed gate {k+1}");return;}
                    if(p.x>=LaneFinish)Done(t);break;
                case 4:if(step==0&&last.x<(RampFrom+RampTo)*.5f&&p.x>=(RampFrom+RampTo)*.5f){if(Mathf.Abs(p.z-CrestZ)<3.6f)step=1;else{Fail("Take the crest!");return;}}
                    if(p.x>CrestOvershoot){Fail("Overshot the box");return;}if(step==1&&stopped&&In(p,CrestBoxMin,CrestBoxMax))Done(t);break;
                case 5:{var loop=QuarryFloor.Loop;int n=loop.Length,s0=StartIndex();
                    if(step<3){var gate=loop[(s0+n*(step+1)/4)%n];if(Vector2.Distance(new(p.x,p.z),new(gate.x,gate.z))<14){step++;notify($"{l.name} · {t:0.0} s");}}
                    else if(Vector2.Distance(new(p.x,p.z),new(loop[s0].x,loop[s0].z))<12)Done(t);break;}
            }
            last=p;
        }
        static bool In(Vector3 p,Vector2 min,Vector2 max)=>p.x>=min.x&&p.x<=max.x&&p.z>=min.y&&p.z<=max.y;
        void Fail(string why)
        {
            int i=lesson;End();
            if(finished!=null)finished(new LicenceScreen.Attempt{index=i,failed=true,reason=why+". Gwen says: again!"});else notify($"{Lessons[i].name} · {why}. Gwen says: again!");
        }
        void Done(float t)
        {
            int i=lesson;var l=Lessons[i];int medal=Medal(l,t);float cones=penalty;End();Confetti.Burst(player.body.position+Vector3.up*2);
            var attempt=new LicenceScreen.Attempt{index=i,time=t,penalty=cones,medal=medal};
            if(medal==0){if(finished!=null)finished(attempt);else notify($"{l.name} {t:0.0} s · no medal (bronze is {l.bronze:0} s). Again!");return;}
            bool allBefore=AllGold(store().Data);float before=Record(store().Data,i)?.best??float.MaxValue;
            int paid=store().RecordLesson(l.id,t,medal,Prizes);attempt.paid=paid;attempt.newBest=t<before;
            if(!allBefore&&AllGold(store().Data))attempt.extra="Gold in every test! Bethan at Grit & Gravel has the rally kit for you now.";
            if(finished!=null){finished(attempt);return;}
            string line=$"{l.name} {t:0.0} s · {MedalName(medal)}!"+(paid>0?$" +{paid} credits":"");
            if(attempt.extra!=null)line+=" · Gold in every lesson! Bethan has the rally parts for you now.";
            notify(line);
        }
    }
}
