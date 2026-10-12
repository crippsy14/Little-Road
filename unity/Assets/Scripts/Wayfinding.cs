using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Finding your way (docs/60, the outsider review: "knowing what to do next" and "growing distances").
    // A notice board in each town square lists what the town offers and pins any of it on the map, and every junction of
    // the mainland road network gets a white fingerpost with the nearest places down each road, in miles.
    // Both are placed by search (clear, level ground off the road), so they follow the towns as they change.
    public static class Wayfinding
    {
        public sealed class Board {public string town;public Vector3 at,front;}
        public static readonly List<Board> Boards=new();
        public static readonly List<(Vector3 at,string text)> Posts=new();
        static Material white,black,frame,cork,roof;static Material[] papers;
        static void Materials()
        {
            if(white!=null)return;
            white=TrialWorld.Material("Fingerpost white",new Color(.95f,.95f,.92f),0,.35f);black=TrialWorld.Material("Fingerpost black",new Color(.08f,.08f,.09f),0,.3f);
            frame=TrialWorld.Material("Notice board frame",new Color(.16f,.33f,.25f),0,.3f);cork=TrialWorld.Material("Notice board cork",new Color(.70f,.53f,.35f),0,.1f);
            roof=TrialWorld.Material("Notice board roof",new Color(.30f,.31f,.35f),0,.25f);
            papers=new[]{TrialWorld.Material("Notice paper",new Color(.97f,.96f,.92f),0,.1f),TrialWorld.Material("Notice paper yellow",new Color(.98f,.86f,.42f),0,.1f),
                TrialWorld.Material("Notice paper pink",new Color(.96f,.62f,.72f),0,.1f),TrialWorld.Material("Notice paper sky",new Color(.58f,.78f,.95f),0,.1f)};
        }
        public static void Clear(){Boards.Clear();Posts.Clear();}

        // ---------------------------------------------------------------- ground tests
        // The ground at (x,z): the highest upward-facing hit within `window` of y (not a roof, not a tree). Road tells you it's a road.
        internal static bool Ground(float x,float z,float y,float window,out RaycastHit ground,out bool road)
        {
            ground=default;road=false;bool found=false;
            foreach(var hit in Physics.RaycastAll(new Vector3(x,y+window+2,z),Vector3.down,window*2+4,~0,QueryTriggerInteraction.Ignore)) {
                if(hit.normal.y<.8f||hit.point.y<y-window||hit.point.y>y+window)continue;
                if(!found||hit.point.y>ground.point.y){ground=hit;found=true;}
            }
            if(found)road=ground.collider.GetComponent<RoadSurface>()!=null;
            return found;
        }
        internal static bool Clear(Vector3 centre,Vector3 half,Quaternion rotation,Collider ground)=>Clear(centre,half,rotation,ground,out _);
        static bool Clear(Vector3 centre,Vector3 half,Quaternion rotation,Collider ground,out string what)
        {
            what=null;
            // (A tree still to stream in counts as there: WorldTiles.)
            if(WorldTiles.Blocked(centre,Mathf.Max(half.x,half.z))){what="a streamed tree";return false;}
            foreach(var c in Physics.OverlapBox(centre,half,rotation,~0,QueryTriggerInteraction.Ignore))if(c!=ground&&!c.name.Contains("mainland collider")&&!c.name.Contains("Terrain")){what=c.name;return false;}
            return true;
        }

        // ---------------------------------------------------------------- notice boards
        // The board stands on clear, level ground beside a road near the hub (the square), facing the road.
        // `road` (optional) measures the road where it isn't tagged RoadSurface (Bayview's loop): distance past its edge, and
        // the way to it.
        public static Board PlaceBoard(Transform parent,string town,Vector3 hub,Func<Vector3,(float edge,Vector3 toRoad)> road=null)
        {
            Materials();Physics.SyncTransforms();int noGround=0,onRoad=0,noRoad=0,uneven=0,blocked=0;string blocker="";
            for(float r=7;r<=46;r+=1.5f)for(int k=0;k<30;k++) {
                float a=(k*12+r*7)*Mathf.Deg2Rad;float x=hub.x+Mathf.Cos(a)*r,z=hub.z+Mathf.Sin(a)*r;
                if(!Ground(x,z,hub.y,3,out var g,out bool tagged)){noGround++;continue;}
                var measured=road?.Invoke(new Vector3(x,g.point.y,z));
                if(tagged||measured?.edge<.5f){onRoad++;continue;}
                // A road 3-7 m away (to stop beside) and none closer: the board faces it.
                Vector3 toRoad=Vector3.zero;float nearest=99;bool tooClose=false;
                if(measured is (float edge,Vector3 way)){nearest=edge;toRoad=way;tooClose=edge<3;}
                else for(int d=0;d<12;d++)for(float s=1.5f;s<=7;s+=1.5f) {
                    var dir=Quaternion.Euler(0,d*30,0)*Vector3.forward;
                    if(Ground(x+dir.x*s,z+dir.z*s,g.point.y,1.5f,out _,out bool r2)&&r2){if(s<3)tooClose=true;if(s<nearest){nearest=s;toRoad=dir;}break;}
                }
                if(tooClose||nearest>7){noRoad++;continue;}
                var side=Vector3.Cross(Vector3.up,toRoad);
                if(!Ground(x+side.x*1.2f,z+side.z*1.2f,g.point.y,1,out var l,out bool rl)||rl||!Ground(x-side.x*1.2f,z-side.z*1.2f,g.point.y,1,out var rr,out bool rr2)||rr2)continue;
                if(Mathf.Abs(l.point.y-rr.point.y)>.3f||Mathf.Abs(l.point.y-g.point.y)>.3f){uneven++;continue;}
                var rotation=Quaternion.LookRotation(toRoad);
                if(!Clear(g.point+Vector3.up*1.55f,new Vector3(1.45f,1.3f,.6f),rotation,g.collider,out string what)){blocked++;blocker=what;continue;}
                var board=new Board{town=town,at=new Vector3(x,Mathf.Min(l.point.y,rr.point.y,g.point.y),z),front=toRoad};
                BuildBoard(parent,board);Boards.Add(board);return board;
            }
            Debug.LogWarning($"LITTLE_ROAD_WAYFINDING no notice board spot near {town} hub=({hub.x:0},{hub.y:0.0},{hub.z:0}) noGround={noGround} onRoad={onRoad} noRoadNear={noRoad} uneven={uneven} blocked={blocked} ({blocker})");return null;
        }
        static void BuildBoard(Transform parent,Board b)
        {
            var root=new GameObject("Notice board / "+b.town).transform;root.SetParent(parent);root.SetPositionAndRotation(b.at,Quaternion.LookRotation(b.front));
            foreach(int s in new[]{-1,1})Part(root,"Board post",new Vector3(s*1.05f,1.35f,0),new Vector3(.13f,2.9f,.13f),frame,true);
            Part(root,"Board frame",new Vector3(0,1.62f,0),new Vector3(2.3f,1.5f,.12f),frame,false);
            Part(root,"Board cork",new Vector3(0,1.6f,.07f),new Vector3(2.08f,1.28f,.03f),cork,false);
            foreach(int s in new[]{-1,1}){var slope=Part(root,"Board roof",new Vector3(s*.62f,2.82f,0),new Vector3(1.4f,.06f,.55f),roof,false);slope.localRotation=Quaternion.Euler(0,0,-s*16);}
            Part(root,"Board header",new Vector3(0,2.52f,.02f),new Vector3(2.1f,.32f,.1f),frame,false);
            Text(root,"NOTICES",new Vector3(0,2.52f,.08f),Quaternion.Euler(0,180,0),.03f,new Color(.98f,.94f,.8f));
            var random=new System.Random(b.town.GetHashCode());
            for(int i=0;i<7;i++){float w=.32f+(float)random.NextDouble()*.18f,h=.3f+(float)random.NextDouble()*.22f;
                var paper=Part(root,"Notice",new Vector3(-.78f+(i%4)*.52f+(float)random.NextDouble()*.06f,1.92f-(i/4)*.6f+(float)random.NextDouble()*.06f,.095f),new Vector3(w,h,.01f),papers[i%papers.Length],false);
                paper.localRotation=Quaternion.Euler(0,0,((float)random.NextDouble()-.5f)*12);
                Part(root,"Pin",paper.localPosition+new Vector3(0,h*.42f,.01f),new Vector3(.03f,.03f,.02f),papers[(i+2)%papers.Length],false);}
        }

        // ---------------------------------------------------------------- fingerposts
        // At every junction (three or more roads): a post in the widest gap between the roads, one finger per road with
        // the two nearest places you reach that way (shortest route over the network), distances in miles.
        // (`only`: the junctions it accepts; null for all. A background district's junctions get theirs once it's built.)
        public static void Fingerposts(Transform parent,RoadNetwork net,IList<(string name,Vector3 at)> places,Func<Vector3,bool> only=null)
        {
            if(net==null)return;Materials();Physics.SyncTransforms();
            var length=new Dictionary<RoadNetwork.Road,float>();foreach(var r in net.roads)length[r]=Length(r.line);
            // Where each place meets the network: (road, distance along it from line[0]).
            var meets=new List<(string name,RoadNetwork.Road road,float s)>();
            foreach(var (name,at) in places) {
                RoadNetwork.Road best=null;float bestD=250,bestS=0;
                foreach(var r in net.roads){float s=Nearest(r.line,at,out float d);if(d<bestD){bestD=d;best=r;bestS=s;}}
                if(best!=null)meets.Add((name,best,bestS));
            }
            var root=new GameObject("Fingerposts").transform;root.SetParent(parent);
            foreach(var n in net.nodes) {
                if(n.arms.Count<3||n.HasExternal||only!=null&&!only(n.at))continue;
                var from=Dijkstra(net,n,length);
                var fingers=new Dictionary<RoadNetwork.Arm,List<(string name,float d)>>();
                foreach(var (name,road,s) in meets) {
                    float bestD=float.MaxValue;RoadNetwork.Arm bestArm=null;
                    foreach(var a in n.arms) {
                        float d=Via(a,road,s,from,length,net);
                        if(d<bestD){bestD=d;bestArm=a;}
                    }
                    if(bestArm==null||bestD<120||bestD>12000)continue;   // (you're there already)
                    if(!fingers.TryGetValue(bestArm,out var list))fingers[bestArm]=list=new();list.Add((name,bestD));
                }
                if(fingers.Count==0)continue;
                if(!PostSpot(n,out var foot)){Debug.LogWarning($"LITTLE_ROAD_WAYFINDING no fingerpost spot at ({n.at.x:0},{n.at.z:0})");continue;}
                var post=new GameObject($"Fingerpost ({n.at.x:0},{n.at.z:0})").transform;post.SetParent(root);post.position=foot;
                Part(post,"Post",new Vector3(0,1.6f,0),new Vector3(.13f,3.2f,.13f),white,true);
                Part(post,"Finial",new Vector3(0,3.27f,0),new Vector3(.2f,.14f,.2f),black,false);
                Part(post,"Post foot",new Vector3(0,.12f,0),new Vector3(.2f,.24f,.2f),black,false);
                int row=0;var words=new List<string>();
                foreach(var pair in fingers) {
                    pair.Value.Sort((p,q)=>Town(p.name)!=Town(q.name)?Town(q.name).CompareTo(Town(p.name)):p.d.CompareTo(q.d));
                    var lines=new List<string>();for(int i=0;i<Mathf.Min(2,pair.Value.Count);i++)lines.Add(pair.Value[i].name+"  "+Miles(pair.Value[i].d));
                    Finger(post,pair.Key.dir,3.0f-row*.5f,lines);row++;words.Add(string.Join(" / ",lines));
                }
                Posts.Add((foot,string.Join(" | ",words)));
            }
            Debug.Log($"LITTLE_ROAD_WAYFINDING fingerposts={Posts.Count} "+string.Join(" ; ",Posts.ConvertAll(p=>$"({p.at.x:0},{p.at.z:0}) {p.text}")));
        }
        static readonly HashSet<string> Towns=new(){"Bayview","Hillcrest","Quarry Hills"};
        static bool Town(string place)=>Towns.Contains(place);
        static void Finger(Transform post,Vector3 dir,float height,List<string> lines)
        {
            dir.y=0;dir.Normalize();var rotation=Quaternion.LookRotation(dir);float h=lines.Count>1?.4f:.24f,length=1.75f;
            var finger=new GameObject("Finger").transform;finger.SetParent(post,false);finger.localPosition=new Vector3(0,height,0);finger.rotation=rotation;
            var arm=Part(finger,"Finger board",new Vector3(0,0,.12f+length*.5f),new Vector3(.05f,h,length),white,false);
            var tip=Part(finger,"Finger tip",new Vector3(0,0,.12f+length),new Vector3(.05f,h*.7071f,h*.7071f),white,false);tip.localRotation=Quaternion.Euler(45,0,0);
            Part(finger,"Finger edge",new Vector3(0,0,.12f+length*.5f),new Vector3(.045f,h+.03f,length-.02f),black,false);
            string text=string.Join("\n",lines);
            // Both faces read normally (the text runs toward the tip on one side and from it on the other, as on real posts).
            Text(finger,text,new Vector3(.032f,0,.12f+length*.5f),Quaternion.LookRotation(-Vector3.right),.016f,new Color(.08f,.08f,.09f));
            Text(finger,text,new Vector3(-.032f,0,.12f+length*.5f),Quaternion.LookRotation(Vector3.right),.016f,new Color(.08f,.08f,.09f));
        }
        static bool PostSpot(RoadNetwork.Node n,out Vector3 foot)
        {
            // The widest gaps between the roads first; then further out.
            var angles=n.arms.ConvertAll(a=>Mathf.Atan2(a.dir.x,a.dir.z)*Mathf.Rad2Deg);angles.Sort();
            var gaps=new List<(float mid,float width)>();
            for(int i=0;i<angles.Count;i++){float a=angles[i],b=i+1<angles.Count?angles[i+1]:angles[0]+360;gaps.Add(((a+b)*.5f,b-a));}
            gaps.Sort((p,q)=>q.width.CompareTo(p.width));
            foreach(var (mid,width) in gaps)for(float r=n.Radius+2.5f;r<=n.Radius+9;r+=1)for(int k=0;k<5;k++) {
                float ang=mid+(k==0?0:(k%2==0?1:-1)*((k+1)/2)*width*.12f);
                var dir=Quaternion.Euler(0,ang,0)*Vector3.forward;var p=n.at+dir*r;
                if(!Ground(p.x,p.z,n.at.y,4,out var g,out bool road)||road)continue;
                // Not right on the kerb: no road within 1.2 m.
                bool kerb=false;for(int d=0;d<8&&!kerb;d++){var o=Quaternion.Euler(0,d*45,0)*Vector3.forward*1.2f;if(Ground(p.x+o.x,p.z+o.z,g.point.y,1,out _,out bool r2)&&r2)kerb=true;}
                if(kerb)continue;
                if(!Clear(g.point+Vector3.up*1.7f,new Vector3(.3f,1.4f,.3f),Quaternion.identity,g.collider))continue;
                foot=g.point;return true;
            }
            foot=default;return false;
        }
        // Shortest network distance from n to every node.
        static Dictionary<RoadNetwork.Node,float> Dijkstra(RoadNetwork net,RoadNetwork.Node start,Dictionary<RoadNetwork.Road,float> length)
        {
            var dist=new Dictionary<RoadNetwork.Node,float>{[start]=0};var open=new List<RoadNetwork.Node>{start};var done=new HashSet<RoadNetwork.Node>();
            while(open.Count>0) {
                open.Sort((a,b)=>dist[a].CompareTo(dist[b]));var u=open[0];open.RemoveAt(0);if(!done.Add(u))continue;
                foreach(var a in u.arms) {
                    if(a.external||a.road==null)continue;var other=a.atStart?a.road.end?.node:a.road.start?.node;if(other==null)continue;
                    float d=dist[u]+length[a.road];if(!dist.TryGetValue(other,out float old)||d<old){dist[other]=d;open.Add(other);}
                }
            }
            return dist;
        }
        // Distance from the arm's node to a place at s along `road`, leaving by this arm.
        static float Via(RoadNetwork.Arm a,RoadNetwork.Road road,float s,Dictionary<RoadNetwork.Node,float> from,Dictionary<RoadNetwork.Road,float> length,RoadNetwork net)
        {
            if(a.external||a.road==null)return float.MaxValue;float L=length[a.road];
            if(a.road==road)return a.atStart?s:L-s;
            var far=a.atStart?a.road.end?.node:a.road.start?.node;if(far==null)return float.MaxValue;
            // From the far node, the shortest way to the place's road (it must not come straight back through here).
            var back=Dijkstra(net,far,length);float best=float.MaxValue;var here=a.node;
            if(road.start?.node!=null&&back.TryGetValue(road.start.node,out float ds)&&road.start.node!=here)best=Mathf.Min(best,ds+s);
            if(road.end?.node!=null&&back.TryGetValue(road.end.node,out float de)&&road.end.node!=here)best=Mathf.Min(best,de+length[road]-s);
            return best==float.MaxValue?best:L+best;
        }
        static float Length(Vector3[] line){float s=0;for(int i=1;i<line.Length;i++)s+=Vector2.Distance(new(line[i].x,line[i].z),new(line[i-1].x,line[i-1].z));return s;}
        static float Nearest(Vector3[] line,Vector3 p,out float distance)
        {
            distance=float.MaxValue;float best=0,run=0;var q=new Vector2(p.x,p.z);
            for(int i=1;i<line.Length;i++) {
                Vector2 a=new(line[i-1].x,line[i-1].z),b=new(line[i].x,line[i].z);var ab=b-a;float len=ab.magnitude;
                float t=len<1e-4f?0:Mathf.Clamp01(Vector2.Dot(q-a,ab)/(len*len));float d=Vector2.Distance(q,a+ab*t);
                if(d<distance){distance=d;best=run+t*len;}run+=len;
            }
            return best;
        }
        // Miles to the nearest quarter, as on a fingerpost: "¾", "1½", "2".
        public static string Miles(float metres)
        {
            float m=Mathf.Max(.25f,Mathf.Round(metres/1609.34f*4)/4);int whole=Mathf.FloorToInt(m);int q=Mathf.RoundToInt((m-whole)*4);
            string frac=q==1?"¼":q==2?"½":q==3?"¾":"";return (whole>0?whole.ToString():"")+frac;
        }

        // ---------------------------------------------------------------- pieces
        static Transform Part(Transform parent,string name,Vector3 local,Vector3 size,Material material,bool solid)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=local;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material;if(!solid)UnityEngine.Object.Destroy(go.GetComponent<Collider>());else go.isStatic=true;
            return go.transform;
        }
        static void Text(Transform parent,string words,Vector3 local,Quaternion rotation,float size,Color colour)
        {
            var go=new GameObject("Sign / "+words.Replace('\n',' '));go.transform.SetParent(parent,false);go.transform.localPosition=local;go.transform.localRotation=rotation;
            var text=go.AddComponent<TextMesh>();text.text=words;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=80;text.characterSize=size;text.color=colour;
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontStyle=FontStyle.Bold;go.GetComponent<Renderer>().sharedMaterial=text.font.material;
            go.AddComponent<WorldText>();
        }
    }
}
