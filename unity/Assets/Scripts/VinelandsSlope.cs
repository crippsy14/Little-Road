using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Vinelands zone 4 (docs/89), with zone 1's machinery (VinelandsVila.cs): the working slope, the terraced amphitheatre
    // between the town and the river. Each parcel (Vinelands.Parcels, from the town plan's ownership map) grows what its owner
    // grows: the quintas' old vines, bush by bush along the benches (Velha); Vinha Nova's trellised rows straight down the
    // slope; the townsfolk's smallholdings, plot by plot (Courela); the abandoned terraces gone to broom (Mortorio); the
    // Pombal's orange grove; the olive terraces above the Cais. Schist walls on every riser (broken where abandoned). The
    // three quintas' houses, lagares, cottages, dovecote, gates and cypress avenue, and Casal do Meio round its threshing
    // floor at the crossing of the Calcada and the Estrada do Meio. The quinta kit is car-forge forge/world/kits/quinta.py.
    public sealed partial class VinelandsVila
    {
        void Zone4(){Quintas();Casal();Slope();}
        static Vector3 FV(Vector2 f)=>F(f.x,f.y);
        // clear of the town, the roads, the buildings and the old town's ways
        bool Clear(Vector3 p,float pad,bool inTown=false)
        {
            if(ValleyField.RoadDistance(p.x,p.z)<pad+4f||OnWay(p))return false;
            var f=new Vector2(p.x,p.z)-Vinelands.Centre;if(!inTown&&Vinelands.InTown(f)>.3f)return false;
            var q=new Vector2(p.x,p.z);foreach(var o in footprints){var l=Quaternion.Euler(0,-o.yaw,0)*new Vector3(q.x-o.c.x,0,q.y-o.c.y);if(Mathf.Abs(l.x)<o.half.x+pad&&Mathf.Abs(l.z)<o.half.y+pad)return false;}
            return true;
        }
        static float Frac(float x)=>x-Mathf.Floor(x);
        static float Hash(float a,float b,int salt){float h=Mathf.Sin(a*12.9898f+b*78.233f+salt*37.719f)*43758.5453f;return h-Mathf.Floor(h);}

        // ---- merged meshes, in chunks of about 60k vertices (one object per vine would be tens of thousands)
        sealed class Batch
        {
            readonly Transform parent;readonly string name;readonly Material mat;readonly bool shadows;readonly List<Vector3> v=new();readonly List<int> t=new();int part;public int Tris;
            public Batch(Transform parent,string name,Material mat,bool shadows=true){this.parent=parent;this.name=name;this.mat=mat;this.shadows=shadows;}
            public int Start{get{if(v.Count>60000)Finish();return v.Count;}}
            public void V(Vector3 p)=>v.Add(p);
            public void Tri(int a,int b,int c){t.Add(a);t.Add(b);t.Add(c);Tris++;}
            public void Quad(int a,int b,int c,int d){Tri(a,b,c);Tri(a,c,d);}
            public void Check(){if(v.Count>60000)Finish();}
            public void Finish()
            {
                if(v.Count==0)return;
                var mesh=new Mesh{name=name+" "+part,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                var go=new GameObject(name+" "+part++,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=shadows?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.Off;go.isStatic=true;v.Clear();t.Clear();
            }
            // a bush: three cross-sections along `along` (narrow, full, narrow), each a low arch, capped at both ends
            public void Bush(Vector3 c,Vector3 along,float length,float width,float height,float lift=0,float seed=0)
            {
                along=Quaternion.Euler(0,(Frac(seed*3.1f)-.5f)*26,0)*along;var side=new Vector3(-along.z,0,along.x);int s0=Start;
                foreach(var (u,k,i) in new[]{(-.5f,.3f,0),(0f,1f,1),(.5f,.35f,2)}) {
                    var m=c+along*(u*length)+side*((Frac(seed*7.3f+i)-.5f)*.25f);float w=width*.5f*k*(.8f+.4f*Frac(seed*5.7f+i)),h=height*(.55f+.45f*k)*(.8f+.4f*Frac(seed*9.1f+i));
                    V(m-side*w+Vector3.up*lift);V(m-side*w*.7f+Vector3.up*(lift+h*.85f));V(m+side*w*.7f+Vector3.up*(lift+h*.85f));V(m+side*w+Vector3.up*lift);
                }
                for(int i=0;i<2;i++)for(int j=0;j<3;j++){int a=s0+i*4+j,b=a+1,cc=a+5,d=a+4;Quad(a,b,cc,d);}   // (wound outward)
                Quad(s0+3,s0+2,s0+1,s0);Quad(s0+8,s0+8+1,s0+8+2,s0+8+3);
            }
            // a rounded lump (broom, rock-rose): five sections of a seven-point dome, tapering to both ends
            public void Lump(Vector3 c,float length,float width,float height,float seed)
            {
                var along=Quaternion.Euler(0,Frac(seed*3.7f)*180,0)*Vector3.forward;var side=new Vector3(-along.z,0,along.x);int s0=Start;
                float[] us={-.5f,-.28f,0,.28f,.5f},ks={.15f,.75f,1,.8f,.2f};
                for(int i=0;i<5;i++) {
                    var m=c+along*(us[i]*length)+side*((Frac(seed*5.3f+i)-.5f)*.2f*width);float w=width*.5f*ks[i]*(.85f+.3f*Frac(seed*7.1f+i)),h=height*(.35f+.65f*ks[i])*(.85f+.3f*Frac(seed*2.9f+i));
                    for(int j=0;j<7;j++){float a=Mathf.PI*j/6;V(m+side*(Mathf.Cos(a)*w)+Vector3.up*(Mathf.Sin(a)*h*(1+.15f*(Frac(seed*11.3f+i*7+j)-.5f))-.05f));}
                }
                for(int i=0;i<4;i++)for(int j=0;j<6;j++){int a=s0+i*7+j;Quad(a,a+7,a+8,a+1);}   // (wound outward)
                for(int j=1;j<6;j++){Tri(s0,s0+j,s0+j+1);Tri(s0+28,s0+28+j+1,s0+28+j);}
            }
            // an upright slab (a post, a little wall): a box from foot to top
            public void Box(Vector3 foot,Vector3 along,float length,float width,float height)
            {
                var side=new Vector3(-along.z,0,along.x)*(width*.5f);var a=along*(length*.5f);int s=Start;
                foreach(var p in new[]{foot-a-side,foot+a-side,foot+a+side,foot-a+side})V(p);foreach(var p in new[]{foot-a-side,foot+a-side,foot+a+side,foot-a+side})V(p+Vector3.up*height);
                Quad(s,s+4,s+5,s+1);Quad(s+1,s+5,s+6,s+2);Quad(s+2,s+6,s+7,s+3);Quad(s+3,s+7,s+4,s);Quad(s+4,s+7,s+6,s+5);   // (wound outward)
            }
        }

        // ---- the slope, bench by bench: from the town's foot to the Seis Voltas (n -1000 to 700), every 1.5 m along the contour;
        // each bench's edges found by bisecting the built ground between the crest and the river
        public static int Plants,Olives,Scrub,WallMetres;
        void Slope()
        {
            Plants=Olives=Scrub=WallMetres=0;
            var leafOld=new Batch(root,"Old vines",WorldLook.Kit("Old vines",new Color(.30f,.42f,.17f),WorldLook.Pattern.Leaf,0,.25f,true));
            var leafOld2=new Batch(root,"Old vines (yellowing)",WorldLook.Kit("Old vines yellowing",new Color(.42f,.46f,.18f),WorldLook.Pattern.Leaf,0,.25f,true));
            var leafSmall=new Batch(root,"Smallholders' vines",WorldLook.Kit("Smallholders' vines",new Color(.36f,.50f,.19f),WorldLook.Pattern.Leaf,0,.25f,true));
            var leafNova=new Batch(root,"Vinha Nova",WorldLook.Kit("Vinha Nova trellis",new Color(.33f,.52f,.20f),WorldLook.Pattern.Leaf,0,.25f,true));
            var posts=new Batch(root,"Vine posts",WorldLook.Kit("Vine posts",new Color(.42f,.33f,.24f),WorldLook.Pattern.Plain,0,.1f,true),false);
            var broom=new Batch(root,"Broom",WorldLook.Kit("Broom scrub",new Color(.38f,.44f,.29f),WorldLook.Pattern.Leaf,0,.2f,true));
            var broomGold=new Batch(root,"Broom in flower",WorldLook.Kit("Broom in flower",new Color(.78f,.70f,.22f),WorldLook.Pattern.Leaf,0,.2f,true));
            var schist=new Batch(root,"Terrace walls",WorldLook.Kit("Vinelands schist",new Color(.36f,.33f,.30f),WorldLook.Pattern.DryStone,0,.1f,true),false);
            var plotWall=new Batch(root,"Plot walls",WorldLook.Kit("Plot wall schist",new Color(.56f,.52f,.46f),WorldLook.Pattern.DryStone,0,.1f,true));
            var rubble=new Batch(root,"Fallen walls",WorldLook.Kit("Fallen schist",new Color(.43f,.40f,.36f),WorldLook.Pattern.DryStone,0,.1f,true));
            float A(float a,float n){var w=Vinelands.Centre+new Vector2(a,n);return H(w.x,w.y);}
            float Contour(float n,float t,float a0,float a1){if(A(a0,n)<t||A(a1,n)>t)return float.NaN;for(int i=0;i<22;i++){float m=(a0+a1)*.5f;if(A(m,n)>t)a0=m;else a1=m;}return (a0+a1)*.5f;}
            const int K0=2,K1=46;var prevFoot=new Vector3?[K1+1];var prevTop=new Vector3?[K1+1];var plotOf=new int[K1+1];for(int k=0;k<=K1;k++)plotOf[k]=int.MinValue;
            int plantCount=0,scrubCount=0;const float step=1.5f;int idx=0;
            for(float n=-1250;n<=700;n+=step,idx++) {   // (to the Seis Voltas' bowl: zone 5)
                var (ca,_)=Vinelands.CrestAtN(n);float ra=Vinelands.RiverAtN(n);float a0=ca+20,a1=ra-95;if(a1<=a0)continue;
                for(int k=K0;k<=K1;k++) {
                    float step5=Vinelands.TerraceStep;float eE=Contour(n,k*step5+.05f,a0,a1),eW=Contour(n,k*step5+1.05f,a0,a1);
                    float wallFoot=eW,wallTop=Contour(n,(k+1)*step5-.05f,a0,a1);
                    bool bench=!float.IsNaN(eE)&&!float.IsNaN(eW)&&eE-eW>6&&eE-eW<70;
                    var mid=new Vector2(bench?(eE+eW)*.5f:float.IsNaN(eE)?0:eE,n);var parcel=bench?Vinelands.ParcelAt(mid):null;var kind=parcel?.kind;
                    // the riser's wall: a schist band from its foot to its top (none in Vinha Nova, which has no terraces)
                    bool riser=!float.IsNaN(wallFoot)&&!float.IsNaN(wallTop)&&wallFoot-wallTop>.8f&&wallFoot-wallTop<20&&kind!=Vinelands.Kind.Nova;
                    if(riser) {
                        var foot=FV(new(wallFoot,n));var top=FV(new(wallTop,n));foot.y+=.06f;top.y+=.06f;
                        bool broken=kind==Vinelands.Kind.Mortorio&&Mathf.PerlinNoise(n*.05f+k,k*.3f)>.55f;
                        if(!Clear(foot,0)||!Clear(top,0)||broken) {
                            prevFoot[k]=prevTop[k]=null;
                            if(broken&&Hash(n,k,3)<.35f){var r=foot+(top-foot)*.2f;rubble.Lump(r,1.6f,1.4f,.5f,Hash(n,k,44)*97);}
                        } else {
                            if(prevFoot[k] is Vector3 pf&&prevTop[k] is Vector3 pt){int s=schist.Start;schist.V(pf);schist.V(pt);schist.V(top);schist.V(foot);schist.Quad(s,s+1,s+2,s+3);WallMetres+=Mathf.RoundToInt(step);}
                            prevFoot[k]=foot;prevTop[k]=top;
                        }
                    } else prevFoot[k]=prevTop[k]=null;
                    if(!bench||kind==null||kind==Vinelands.Kind.Nova)continue;
                    var dirN=Vector3.forward;   // (the contour runs about north-south)
                    switch(kind.Value) {
                        case Vinelands.Kind.Velha: {   // three rows of old bush vines near the bench's outer edge, an olive now and then on the wall's top
                            for(int j=0;j<3;j++){float a=eE-(1.8f+j*2.4f);if(a<eW+1.5f)break;if(Hash(n,k*7+j,1)<.11f)continue;var p=FV(new(a,n));if(!Clear(p,.5f))continue;
                                float s=.85f+.3f*Hash(n,k+j,2);(Hash(n,k*3+j,36)<.3f?leafOld2:leafOld).Bush(p,dirN,1.1f*s,.8f*s,.85f*s,.12f,Hash(n,k+j,37)*97);if(++plantCount%8==0)AreaAudit.ExtraNature.Add(p);Plants++;}
                            if(Hash(n,k,4)<1f/55){var o=FV(new(eE-.9f,n));if(Clear(o,1.5f)&&Put("olive_tree",new Vector3(o.x,o.y-.05f,o.z),Hash(n,k,5)*360,null,null,true,.8f+.3f*Hash(n,k,6))!=null)Olives++;}
                            break;
                        }
                        case Vinelands.Kind.Courela: {   // the townsfolk's plots: 15-30 m each along the bench, each its own crop
                            float len=15+15*Hash(Mathf.Floor(n/40),k,7);int plot=Mathf.FloorToInt((n+k*37)/len);float u=Hash(plot,k,8);
                            if(plot!=plotOf[k]) {   // (a new plot: its boundary wall across the bench; a hut, a tank)
                                plotOf[k]=plot;var b0=FV(new(eE-.5f,n));var b1=FV(new(eW+.5f,n));
                                // (in 2 m pieces, each on its own ground: one box across the bench stood 1.9 m out of its low end, round 4)
                                if(Clear(b0,0)&&Clear(b1,0)){var along=b1-b0;along.y=0;float L=along.magnitude;if(L>2&&L<28){var d=along/L;int pc=Mathf.CeilToInt(L/2f);for(int q=0;q<pc;q++){var w0=b0+d*(L*q/pc);var w1=b0+d*(L*(q+1)/pc);float g0=H(w0.x,w0.z),g1=H(w1.x,w1.z);var m=(w0+w1)*.5f;m.y=Mathf.Min(g0,g1)-.15f;plotWall.Box(m,d,L/pc+.03f,.4f,.8f+Mathf.Abs(g0-g1));}}}
                                if(Hash(plot,k,9)<.3f){var hp=FV(new(eW+3f,n+5));if(Clear(hp,1)){var hc=hp;Building("quinta_casebre",hc,Yaw(hc,hc+Vector3.right),null,null,2f,4.4f,2f);}}
                                else if(Hash(plot,k,10)<.2f){var tp=FV(new(eW+2.5f,n+6));if(Clear(tp,1))Put("vila_tanque",tp,90);}
                            }
                            if(u<.42f){for(int j=0;j<3;j++){float a=eE-(1.6f+j*2.1f);if(a<eW+1f||Hash(n,k*5+j,11)<.08f)continue;var p=FV(new(a,n));if(!Clear(p,.4f))continue;
                                    leafSmall.Bush(p,dirN,1.1f,.8f,.85f,.1f,Hash(n,k+j,38)*97);if(++plantCount%8==0)AreaAudit.ExtraNature.Add(p);Plants++;}}
                            else if(u<.6f){if(idx%4==0){var p=FV(new((eE+eW)*.5f,n));if(Clear(p,1.5f))Put("vila_horta",p,0,null,null,false);}}
                            else if(u<.74f){if(idx%6==0){var p=FV(new(eE-3f,n));if(Clear(p,1.5f)&&Put("olive_tree",new Vector3(p.x,p.y-.05f,p.z),Hash(n,k,12)*360,null,null,true,.75f+.3f*Hash(n,k,13))!=null)Olives++;}}
                            else if(u<.88f){if(Hash(n,k,14)<.35f){var p=FV(new(eW+(eE-eW)*Hash(n,k,15),n));if(Clear(p,.4f)){(Hash(n,k,16)<.25f?broomGold:broom).Lump(p,1.3f,1.1f,.8f,Hash(n,k,17)*97);Scrub++;}}}
                            else{if(idx%7==0){var p=FV(new(eE-4f,n));if(Clear(p,2f))Put("vila_figueira",new Vector3(p.x,p.y-.05f,p.z),Hash(n,k,18)*360);}}
                            break;
                        }
                        case Vinelands.Kind.Mortorio: {   // gone to broom and rock-rose, a wild olive here and there
                            int c=1+(int)(Hash(n,k,19)*2.2f);
                            for(int i=0;i<c;i++){float a=eW+(eE-eW)*Hash(n,k*3+i,20);var p=FV(new(a,n+Hash(n,k+i,21)*1.4f));if(!Clear(p,.3f))continue;
                                (Hash(n,k+i,22)<.2f?broomGold:broom).Lump(p,.8f+.7f*Hash(n,i,24),.7f+.5f*Hash(n,i,25),.45f+.4f*Hash(n,i,26),Hash(n,k+i,23)*97);Scrub++;if(++scrubCount%6==0)AreaAudit.ExtraNature.Add(p);}
                            if(Hash(n,k,27)<1f/110){var o=FV(new(eE-2f-6f*Hash(n,k,28),n));if(Clear(o,1.5f)&&Put("olive_tree",new Vector3(o.x,o.y-.05f,o.z),Hash(n,k,29)*360,null,null,true,.6f+.25f*Hash(n,k,30))!=null)Olives++;}
                            break;
                        }
                        case Vinelands.Kind.Laranjal: {   // the Pombal's oranges in rows along the benches, 5 m apart
                            if(idx%3==0)for(int j=0;j<4;j++){float a=eE-(2.5f+j*5f);if(a<eW+2f)break;var p=FV(new(a,n));if(Clear(p,1.5f))Put("vila_laranjeira",new Vector3(p.x,p.y-.05f,p.z),Hash(n,k+j,31)*360,null,null,true,.9f+.2f*Hash(n,j,32));}
                            break;
                        }
                        case Vinelands.Kind.Olival:break;   // (the grove's own pass below: the slope above the Cais isn't all benched)
                    }
                }
            }
            // ---- the olive grove above the Cais: trees on a jittered 10 m grid, broom under some
            foreach(var (pname,pkind,poly) in Vinelands.Parcels) {
                if(pkind!=Vinelands.Kind.Olival)continue;
                for(float a=720;a<=995;a+=10)for(float n=-125;n<=265;n+=10) {
                    var f=new Vector2(a+(Hash(a,n,45)-.5f)*6,n+(Hash(a,n,46)-.5f)*6);if(Vinelands.ParcelAt(f)?.kind!=Vinelands.Kind.Olival)continue;var p=FV(f);if(p.y<Vinelands.Water+1.5f||!Clear(p,2f,true))continue;
                    if(Hash(a,n,47)<.12f){(Hash(a,n,48)<.3f?broomGold:broom).Lump(p,1.4f,1.2f,.7f,Hash(a,n,49)*97);continue;}
                    if(Put("olive_tree",new Vector3(p.x,p.y-.05f,p.z),Hash(a,n,50)*360,null,null,true,.8f+.35f*Hash(a,n,51))!=null)Olives++;
                }
            }
            // ---- Vinha Nova: rows straight down the slope 2.6 m apart, trellised on posts every 6 m (the 1998 planting, no terraces)
            var nova=Vinelands.Parcels[4].poly;float nMin=float.MaxValue,nMax=float.MinValue,aMin=float.MaxValue,aMax=float.MinValue;foreach(var q in nova){nMin=Mathf.Min(nMin,q.y);nMax=Mathf.Max(nMax,q.y);aMin=Mathf.Min(aMin,q.x);aMax=Mathf.Max(aMax,q.x);}
            for(float n=nMin+3;n<nMax-3;n+=2.6f) {
                var run=new List<Vector3>();
                void Flush()
                {
                    if(run.Count>=4) {
                        int s0=leafNova.Start;
                        for(int i=0;i<run.Count;i++){var p=run[i];var d=run[Mathf.Min(i+1,run.Count-1)]-run[Mathf.Max(i-1,0)];d.y=0;d.Normalize();var side=new Vector3(-d.z,0,d.x)*.3f;
                            leafNova.V(p-side+Vector3.up*.6f);leafNova.V(p-side*.9f+Vector3.up*1.55f);leafNova.V(p+side*.9f+Vector3.up*1.55f);leafNova.V(p+side+Vector3.up*.6f);
                            if(i>0){int a=s0+(i-1)*4;for(int j=0;j<3;j++)leafNova.Quad(a+j,a+4+j,a+5+j,a+j+1);}
                            if(i%4==0)posts.Box(p+Vector3.down*.2f,d,.12f,.12f,2.0f);
                            if(i%6==3)AreaAudit.ExtraNature.Add(p);Plants+=5;
                        }
                        leafNova.Check();posts.Check();
                    }
                    run.Clear();
                }
                for(float a=aMin;a<=aMax;a+=1.4f) {
                    var f=new Vector2(a,n+(Hash(n,0,35)-.5f)*.4f);bool inside=Vinelands.ParcelAt(f)?.kind==Vinelands.Kind.Nova;var p=FV(f);
                    if(inside&&Clear(p,.5f))run.Add(p);else Flush();
                }
                Flush();
            }
            foreach(var b in new[]{plotWall,leafOld,leafOld2,leafSmall,leafNova,posts,broom,broomGold,schist,rubble})b.Finish();
            Debug.Log($"LITTLE_ROAD_VINELANDS_SLOPE plants={Plants} olives={Olives} scrub={Scrub} walls={WallMetres} m tris={leafOld.Tris+leafOld2.Tris+leafSmall.Tris+leafNova.Tris+posts.Tris+broom.Tris+broomGold.Tris+schist.Tris+rubble.Tris}");
        }

        // ---- the three quintas: each house on its yard by its track, the lagar, the workers' cottages, the gate with its name
        void Quintas()
        {
            var granite=T(0xf6f1e6,0x2e5e3e,0x2e5e3e,0x5a3a24,0xb5532f);
            void Gate(string track,string name){var tr=HillcrestRoute.Find(track);if(tr==null)return;var g=tr.At(14);var d=tr.At(15)-tr.At(13);d.y=0;g.y=H(g.x,g.z);Put("quinta_portao",g,Quaternion.LookRotation(d).eulerAngles.y,T(0xffffff,0xffffff,0xffffff,0xffffff),name,false);}
            void Yard(string name,Vector2 min,Vector2 max){Plaza(name,min,max,Setts(),true);}
            // Quinta do Pombal (north): the best kept: the house over its yard, the dovecote, the chapel, the cottages, the lagar,
            // the cypress avenue up the track from the Estrada do Meio
            {var h=F(470,413);Building("quinta_casa",h,Yaw(h,F(470,380)),granite,null,4f,3f,4f);
                var l=F(522,397);Building("quinta_lagar",l,Yaw(l,F(470,388)),null,null,4f,3f,4f);
                var p=F(507,428);Building("quinta_pombal",p,0,T(0xf6f1e6,0xffffff,0xffffff,0xffffff),null,3f,3f,3f);
                var c=F(446,446);Building("cais_capela",c,Yaw(c,F(470,400)),T(0xf8f5ee,0x2a5490,0x2a5490,0x5a3a24),null,3f,3f,3f);
                var w=F(482,362);Building("quinta_caseiros",w,Yaw(w,F(482,390)),null,null,6f,3f,4f);
                Yard("Pombal yard",new Vector2(447,373),new Vector2(500,399));Gate(Vinelands.PombalTrack,"QUINTA DO POMBAL");
                var tr=HillcrestRoute.Find(Vinelands.PombalTrack);if(tr!=null)for(float s=22;s<150;s+=9)foreach(int side in new[]{1,-1}){var q=tr.At(s)+tr.Right(s).normalized*side*4.6f;q.y=H(q.x,q.z);if(Clear(q,.5f))Put("vila_cipreste",new Vector3(q.x,q.y-.05f,q.z),0,null,null,true,.9f+.2f*Hash(s,side,40));}
                foreach(var (a,n,y) in new[]{(456f,385f,30f),(488f,380f,300f)})if(BayviewKit.Has("tractor")){var t=F(a,n);t.y=Top(t)+.05f;Put("tractor",t,y);}
                {var q=F(510,384);q.y=Top(q)+.05f;Put("quinta_caixas",q,15);}
                Spots["pombal"]=(new Vector3(F(478,388).x,Top(F(478,388))+.1f,F(478,388).z),180);}
            // Quinta das Lajes (middle): old vines, slate in its walls; the house, the big lagar, the labourers' house
            {var h=F(560,-268);Building("quinta_casa",h,Yaw(h,F(560,-300)),T(0xf3eee2,0x8b1d2c,0x8b1d2c,0x5a3a24,0xa9472b),null,4f,3f,4f);
                var l=F(589,-266);Building("quinta_lagar",l,Yaw(l,F(589,-300)),null,null,7f,3f,4f);
                var w=F(548,-330);Building("quinta_caseiros",w,Yaw(w,F(548,-300)),null,null,4f,3f,4f);
                var a=F(531,-262);Building("quinta_alpendre",a,Yaw(a,F(540,-290)),null,null,4f,3f,4f);
                Yard("Lajes yard",new Vector2(535,-299),new Vector2(590,-282));Gate(Vinelands.LajesTrack,"QUINTA DAS LAJES");
                {var q=F(578,-290);q.y=Top(q)+.05f;Put("quinta_caixas",q,80);}
                if(BayviewKit.Has("tractor")){var t=F(545,-290);t.y=Top(t)+.05f;Put("tractor",t,90);}
                Spots["lajes"]=(new Vector3(F(566,-292).x,Top(F(566,-292))+.1f,F(566,-292).z),180);}
            // Quinta do Fojo (south): half abandoned: the manor a ruin with a fig inside it, the family in the old stable
            {var r=F(430,-690);Building("quinta_ruina",r,Yaw(r,F(430,-720)),T(0xd8cfbd,0xffffff,0xffffff,0xffffff),null,4f,3f,4f);
                var s=F(468,-696);Building("casa_douro_c",s,Yaw(s,F(468,-720)),House("granito"),null,4f,3f,4f);
                var l=F(392,-694);Building("quinta_lagar",l,Yaw(l,F(392,-720)),null,null,4f,3f,7f);   // (cut into the slope behind)
                Yard("Fojo yard",new Vector2(412,-728),new Vector2(475,-711));Gate(Vinelands.FojoTrack,"QUINTA DO FOJO");
                Spots["fojo"]=(new Vector3(F(444,-720).x,Top(F(444,-720))+.1f,F(444,-720).z),180);}
        }

        // ---- Casal do Meio: the mid-slope hamlet at the crossing, its farmhouses round the threshing floor, the chapel, the cross
        void Casal()
        {
            var eira=F(575,58);Building("quinta_eira",eira,0,null,null,1.5f,3f,1.5f);
            {var c=F(540,92);Building("cais_capela",c,Yaw(c,eira),T(0xf8f5ee,0x3f7f6a,0x3f7f6a,0x5a3a24),null,3f,4.4f,3f);}
            {var x=F(596,40);if(ValleyField.RoadDistance(x.x,x.z)>5f)Put("vila_cruzeiro",x,0);}
            string[] kinds={"casa_douro_c","casa_douro_d","casa_douro_c","casa_douro_a"};int placed=0;
            // (along the arms of the crossing, both sides, fronts to the road: round 1's rings round the eira mostly fell on the roads)
            foreach(var (rn,idx0) in new[]{(Vinelands.CaisRoadName,Vinelands.CaisMeioT),(Vinelands.MeioName,Vinelands.MeioCasalT)}) {
                var r=HillcrestRoute.Find(rn);if(r==null)continue;float s0=0;for(int i=1;i<=idx0;i++)s0+=Vector3.Distance(r.line[i],r.line[i-1]);
                foreach(int dir in new[]{1,-1})for(float ds=20;ds<=95;ds+=12.5f)foreach(int side in new[]{1,-1}) {
                    float s=s0+dir*ds;var at=r.At(s);var nn=r.Right(s);nn.y=0;nn.Normalize();nn*=side;string id=kinds[(int)(Hash(s,side,42)*4)];var sz=Footprint(id);
                    var c=at+nn*(r.width*.5f+3f+sz.y*.5f);c.y=H(c.x,c.z);
                    if(Building(id,c,Yaw(c,at),House(Hash(s,side,43)<.3f?"azulejo":"granito"),null,3.5f,3.6f,3.5f)){placed++;yards.Add((c+nn*(sz.y*.5f),nn,Quaternion.Euler(0,90,0)*nn,sz.x,"horta"));}
                }
            }
            foreach(var (r,count) in new[]{(19f,9),(31f,13),(44f,16)})for(int i=0;i<count&&placed<24;i++) {
                float ang=i*360f/count+r*3;var p=eira+Quaternion.Euler(0,ang,0)*Vector3.forward*r;p.y=H(p.x,p.z);
                string id=kinds[(i+(int)r)%kinds.Length];if(Building(id,p,Yaw(p,eira),House(Hash(i,r,41)<.3f?"azulejo":"granito"),null,3.5f,4.4f,3.5f)){placed++;var back=(p-eira);back.y=0;back.Normalize();var sz=Footprint(id);yards.Add((p+back*(sz.y*.5f),back,Quaternion.Euler(0,90,0)*back,sz.x,"horta"));}
            }
            {var q=F(588,62);q.y=Top(q)+.05f;Put("quinta_caixas",q,40);}
            if(BayviewKit.Has("tractor")){var t=F(560,48);t.y=Top(t)+.05f;Put("tractor",t,120);}
            Spots["casal"]=(new Vector3(F(566,64).x,Top(F(566,64))+.1f,F(566,64).z),90);
            Debug.Log($"LITTLE_ROAD_VINELANDS_CASAL houses={placed}");
        }
    }
}
