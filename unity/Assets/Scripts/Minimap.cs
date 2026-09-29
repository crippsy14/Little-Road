using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LittleRoad
{
    // Free-roam map. Bottom right: a round map of the baked world (WorldMap.Bake) centred on the car and
    // turned so the way you're heading is up, zooming out as you speed up. Marks show residents, Pit Stops,
    // the zipline and the current objective (a pulsing star; at the rim when it's off the map). M or the
    // pad's View/Select button opens the whole map, north up, with every place name (the atlas labels).
    public sealed class Minimap:MonoBehaviour
    {
        public enum Shape{Dot,Service,Zip,Objective}
        public struct Mark{public Vector3 at;public Color colour;public Shape shape;public Mark(Vector3 a,Color c,Shape s){at=a;colour=c;shape=s;}}
        public const float Diameter=330,NearRadius=150,FarRadius=330;
        Transform car;RoadCar driver;RenderTexture map;Func<List<Mark>> marks;Func<List<WorldMap.Label>> labels;
        Canvas canvas;RectTransform corner,full,northBadge;RawImage cornerMap;Material cornerMaterial,flatMaterial;MarkLayer cornerMarks,fullMarks;Text location;float locationCheck;
        float radius=NearRadius;bool shown=true,heldBefore;
        public bool FullOpen{get;private set;}
        public int LastCloseFrame{get;private set;}=-1;
        public float Radius=>radius;

        public void Setup(RoadCar player,RenderTexture baked,Func<List<Mark>> markSource,Func<List<WorldMap.Label>> labelSource)
        {
            driver=player;car=player.transform;map=baked;marks=markSource;labels=labelSource;
            canvas=CoastUI.Canvas(transform,"Minimap",8);canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=1;var root=(RectTransform)canvas.transform;
            // Corner map: a navy ring card, the map disc, marks, and a north badge riding the rim.
            corner=Toy.Rect(root,"Minimap corner",new Vector2(Diameter,Diameter),Vector2.zero);corner.anchorMin=corner.anchorMax=new Vector2(1,0);corner.anchoredPosition=new Vector2(-Diameter*.5f-36,Diameter*.5f+58);
            Toy.Card(corner,"Minimap ring",new Vector2(Diameter+14,Diameter+14),Vector2.zero,Toy.Navy,(Diameter+14)*.5f,0,7);
            cornerMap=Toy.Rect(corner,"Minimap disc",new Vector2(Diameter,Diameter),Vector2.zero).gameObject.AddComponent<RawImage>();cornerMap.raycastTarget=false;
            var shader=Shader.Find("LittleRoad/Minimap");
            if(shader!=null){cornerMaterial=new Material(shader){name="Minimap"};cornerMap.material=cornerMaterial;}
            cornerMap.texture=map;
            cornerMarks=Toy.Rect(corner,"Minimap marks",new Vector2(Diameter,Diameter),Vector2.zero).gameObject.AddComponent<MarkLayer>();cornerMarks.raycastTarget=false;
            northBadge=(RectTransform)Toy.Card(corner,"North badge",new Vector2(40,40),Vector2.zero,Toy.White,20,4,0).transform;
            Toy.Label(northBadge,"North","N",24,new Vector2(40,40),Vector2.zero,Toy.Navy,TextAnchor.MiddleCenter,true);
            // Where you are, under the map (the first visit gets a "discovered" card instead: AreaBanner).
            location=Toy.Label(corner,"Location",string.Empty,20,new Vector2(360,28),new Vector2(0,-Diameter*.5f-26),Toy.White,TextAnchor.MiddleCenter,true);
            location.gameObject.AddComponent<Outline>().effectColor=Toy.Navy;
            // Whole map: hidden until opened.
            // Its own canvas above the HUD (the adventure HUD sorts at 20).
            var top=CoastUI.Canvas(transform,"Full map canvas",45);top.GetComponent<CanvasScaler>().matchWidthOrHeight=1;
            full=Toy.Rect((RectTransform)top.transform,"Full map",new Vector2(1920,1080),Vector2.zero);full.gameObject.SetActive(false);
            var shade=full.gameObject.AddComponent<Image>();shade.color=new Color(.05f,.08f,.2f,.72f);shade.raycastTarget=false;
            Toy.Card(full,"Full map card",new Vector2(1010,1010),Vector2.zero,Toy.Navy,34,0,10);
            var whole=Toy.Rect(full,"Full map image",new Vector2(980,980),Vector2.zero).gameObject.AddComponent<RawImage>();whole.texture=map;whole.raycastTarget=false;
            flatMaterial=FlatMaterial();if(flatMaterial!=null)whole.material=flatMaterial;
            fullMarks=Toy.Rect(full,"Full map marks",new Vector2(980,980),Vector2.zero).gameObject.AddComponent<MarkLayer>();fullMarks.raycastTarget=false;
            Toy.Title(full,"Full map title","HILLCREST VALLEY & BAYVIEW",40,new Vector2(900,60),new Vector2(0,520),Toy.Sun,TextAnchor.MiddleCenter);
            var close=Toy.Label(full,"Full map close","M / Esc · close",22,new Vector2(400,30),new Vector2(0,-518),Toy.White,TextAnchor.MiddleCenter,true);close.gameObject.AddComponent<Outline>().effectColor=Toy.Navy;
        }
        // The square map with fog of war (full map, pause map).
        public static Material FlatMaterial(){var shader=Shader.Find("LittleRoad/Minimap");if(shader==null)return null;var m=new Material(shader){name="Flat map"};m.SetFloat("_Circle",0);return m;}
        public void SetShown(bool value){if(shown==value||corner==null)return;shown=value;corner.gameObject.SetActive(value&&!FullOpen);}
        void BuildLabels()
        {
            var holder=full.Find("Full map marks");for(int i=holder.childCount-1;i>=0;i--)Destroy(holder.GetChild(i).gameObject);
            // Most important first; a name that would overlap one already placed is left off.
            int Rank(string kind)=>kind=="area"?0:kind=="service"?1:kind=="place"?2:kind=="person"?3:9;
            var placed=new List<Rect>();var all=labels();all.Sort((a,b)=>Rank(a.kind).CompareTo(Rank(b.kind)));
            foreach(var l in all) {
                if(Rank(l.kind)>3||!ExploredMap.Seen(new Vector3(l.x,0,l.z)))continue;
                var uv=WorldMap.Uv(new Vector3(l.x,0,l.z));var p=(uv-new Vector2(.5f,.5f))*980;
                bool area=l.kind=="area";int size=area?22:l.kind=="person"?15:16;
                string text=area?l.name.ToUpperInvariant():l.kind=="service"?"Pit Stop":l.name;
                var colour=area?Toy.White:l.kind=="service"?Toy.Hex(0xffd6b8):l.kind=="person"?Toy.Hex(0xffe3e6):Toy.Cream;
                var at=p+new Vector2(0,area?0:15);
                var t=Toy.Label(holder,"Place "+l.name,text,size,new Vector2(300,30),at,colour,TextAnchor.MiddleCenter,area);
                var box=new Rect(at.x-t.preferredWidth*.5f-4,at.y-size*.6f,t.preferredWidth+8,size*1.2f);
                if(placed.Exists(r=>r.Overlaps(box))){Destroy(t.gameObject);continue;}
                placed.Add(box);t.gameObject.AddComponent<Outline>().effectColor=Toy.Navy;
            }
        }
        public void OpenFull(bool open)
        {
            if(FullOpen==open||full==null)return;FullOpen=open;
            // The car waits while the whole map is up.
            if(open){heldBefore=driver.controlsBlocked;driver.controlsBlocked=true;BuildLabels();}else{driver.controlsBlocked=heldBefore;LastCloseFrame=Time.frameCount;}
            full.gameObject.SetActive(open);corner.gameObject.SetActive(!open&&shown);
        }
        void Update()
        {
            if(car==null)return;
            var k=Keyboard.current;var pad=Gamepad.current;
            bool toggle=k?.mKey.wasPressedThisFrame==true||pad?.selectButton.wasPressedThisFrame==true;
            bool back=FullOpen&&(k?.escapeKey.wasPressedThisFrame==true||pad?.buttonEast.wasPressedThisFrame==true);
            if((toggle&&(FullOpen||shown))||back)OpenFull(!FullOpen);
        }
        void LateUpdate()
        {
            if(car==null||map==null)return;
            var list=marks();float pulse=.5f+.5f*Mathf.Sin(Time.unscaledTime*5);
            if(FullOpen){FullPose(list,pulse);return;}
            if(!shown)return;
            if(Time.unscaledTime>locationCheck){locationCheck=Time.unscaledTime+.5f;location.text=AreaBanner.Area(car.position);}
            // Zoom with speed, eased.
            float want=Mathf.Lerp(NearRadius,FarRadius,Mathf.InverseLerp(4,30,Mathf.Abs(driver.speed)));radius=Mathf.Lerp(radius,want,1-Mathf.Exp(-Time.unscaledDeltaTime*1.5f));
            var f=car.forward;f.y=0;float heading=f.sqrMagnitude>1e-4f?Mathf.Atan2(f.x,f.z):0;
            var uv=WorldMap.Uv(car.position);
            if(cornerMaterial!=null){cornerMaterial.SetVector("_Center",new Vector4(uv.x,uv.y,0,0));cornerMaterial.SetFloat("_Angle",heading);cornerMaterial.SetFloat("_Span",radius/WorldMap.Size);}
            float c=Mathf.Cos(heading),s=Mathf.Sin(heading),half=Diameter*.5f;
            Vector2 Local(Vector3 p){var o=p-car.position;return new Vector2(o.x*c-o.z*s,o.x*s+o.z*c)/radius*half;}
            cornerMarks.Begin();
            foreach(var m in list) {
                var at=Local(m.at);bool objective=m.shape==Shape.Objective;
                if(at.magnitude>half-10){if(!objective)continue;at=at.normalized*(half-14);} // the objective waits on the rim
                cornerMarks.Add(at,m.colour,m.shape,objective?13+3*pulse:m.shape==Shape.Dot?7:10);
            }
            cornerMarks.Player(Vector2.zero,0,15);cornerMarks.End();
            // North sits on the rim where north is.
            northBadge.anchoredPosition=new Vector2(-s,c)*(half+2);
        }
        void FullPose(List<Mark> list,float pulse)
        {
            Vector2 At(Vector3 p)=>(WorldMap.Uv(p)-new Vector2(.5f,.5f))*980;
            var f=car.forward;f.y=0;float heading=f.sqrMagnitude>1e-4f?Mathf.Atan2(f.x,f.z):0;
            fullMarks.Begin();
            foreach(var m in list)fullMarks.Add(At(m.at),m.colour,m.shape,m.shape==Shape.Objective?12+3*pulse:m.shape==Shape.Dot?6:9);
            fullMarks.Player(At(car.position),heading,13);fullMarks.End();
        }
        void OnDestroy(){if(cornerMaterial!=null)Destroy(cornerMaterial);if(flatMaterial!=null)Destroy(flatMaterial);}

        // All marks of one map in a single mesh, rebuilt each frame.
        public sealed class MarkLayer:MaskableGraphic
        {
            readonly List<(Vector2 at,Color colour,Shape shape,float size)> items=new();Vector2 player;float playerHeading,playerSize;bool hasPlayer;
            public void Begin(){items.Clear();hasPlayer=false;}
            public void Add(Vector2 at,Color colour,Shape shape,float size)=>items.Add((at,colour,shape,size));
            public void Player(Vector2 at,float heading,float size){player=at;playerHeading=heading;playerSize=size;hasPlayer=true;}
            public void End()=>SetVerticesDirty();
            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();
                foreach(var (at,colour,shape,size) in items) {
                    switch(shape) {
                        case Shape.Service:Square(vh,at,size+3,Toy.Navy);Square(vh,at,size,colour);break;
                        case Shape.Zip:Diamond(vh,at,size+3,Toy.Navy);Diamond(vh,at,size,colour);break;
                        case Shape.Objective:Star(vh,at,size+4,Toy.Navy);Star(vh,at,size,colour);break;
                        default:ToyMesh.Circle(vh,at,size+2.5f,Toy.Navy,16);ToyMesh.Circle(vh,at,size,colour,16);break;
                    }
                }
                if(hasPlayer)Arrow(vh,player,playerHeading,playerSize);
            }
            static void Square(VertexHelper vh,Vector2 c,float r,Color col)=>ToyMesh.Quad(vh,c+new Vector2(-r,-r),c+new Vector2(-r,r),c+new Vector2(r,r),c+new Vector2(r,-r),col);
            static void Diamond(VertexHelper vh,Vector2 c,float r,Color col)=>ToyMesh.Quad(vh,c+new Vector2(0,-r),c+new Vector2(-r,0),c+new Vector2(0,r),c+new Vector2(r,0),col);
            static void Star(VertexHelper vh,Vector2 c,float r,Color col)
            {
                int start=vh.currentVertCount;vh.AddVert(c,col,Vector2.zero);
                for(int i=0;i<10;i++){float a=Mathf.PI*.5f+i*Mathf.PI/5;float rr=i%2==0?r:r*.45f;vh.AddVert(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*rr,col,Vector2.zero);}
                for(int i=1;i<=10;i++)vh.AddTriangle(start,start+i,start+(i==10?1:i+1));
            }
            // The player: a white arrowhead with a navy outline, pointing along the heading (screen up = north on the full map).
            static void Arrow(VertexHelper vh,Vector2 c,float heading,float size)
            {
                Vector2 R(Vector2 v){float co=Mathf.Cos(-heading),si=Mathf.Sin(-heading);return c+new Vector2(v.x*co-v.y*si,v.x*si+v.y*co);}
                void Tri(float s,Color col){int b=vh.currentVertCount;vh.AddVert(R(new Vector2(0,s*1.1f)),col,Vector2.zero);vh.AddVert(R(new Vector2(s*.8f,-s*.8f)),col,Vector2.zero);vh.AddVert(R(new Vector2(0,-s*.35f)),col,Vector2.zero);vh.AddVert(R(new Vector2(-s*.8f,-s*.8f)),col,Vector2.zero);vh.AddTriangle(b,b+1,b+2);vh.AddTriangle(b,b+2,b+3);}
                Tri(size+4,Toy.Navy);Tri(size,Toy.White);
            }
        }
    }
}
