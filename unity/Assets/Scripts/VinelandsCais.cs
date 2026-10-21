using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Vinelands zone 3 (docs/88), with zone 1's machinery (VinelandsVila.cs): the Ribeira do Cais at the foot of the
    // amphitheatre. The stone quay between the river road and the water (its wall, bollards, lamps, the slipway, the pontoon
    // and the wine boats, cars nosed up to the river); the Praça do Cais where the Calçada arrives, its tascas; the three wine
    // lodges across the road with their names painted on, barrels at their doors; the quay's chapel; the boatmen's houses on
    // their raised ground floors up and down the river road; the Passeio Ribeirinho north to the river beach; Ponte Nova;
    // and the Calçada Velha, the old mule steps up the amphitheatre to the town. The cais kit is car-forge
    // forge/world/kits/cais.py.
    public sealed partial class VinelandsVila
    {
        // the river road, a metre at a time: where it is and which way is east (the river side), by frame n
        List<(Vector3 at,Vector3 east,float n)> river;
        (Vector3 at,Vector3 east) RiverAt(float n){var best=river[0];float bd=float.MaxValue;foreach(var r in river){float d=Mathf.Abs(r.n-n);if(d<bd){bd=d;best=r;}}return (best.at,best.east);}

        void Zone3()
        {
            var rr=HillcrestRoute.Find(Vinelands.RiverRoadName);if(rr==null)return;Physics.SyncTransforms();
            river=new();for(float s=0;s<rr.Length;s+=1){var a=rr.At(s);var r=rr.Right(s);r.y=0;r.Normalize();if(r.x<0)r=-r;river.Add((a,r,a.z-Vinelands.Centre.y));}
            Quay();Praca();Lodges();Ribeira();PasseioRibeirinho();Ponte();CalcadaVelha();
        }

        // ---- the quay: paved from the road to the water, its wall down into the river, bollards and lamps along the edge; the
        // slipway (n -35), the pontoon (n 8) with two rabelos alongside; barrels waiting to load; cars nosed up to the river
        void Quay()
        {
            Plaza("Cais quay",new Vector2(976.3f,-62),new Vector2(997,62),Setts(),true);   // (from the river road's very edge: beside the Cais junction its shoulder stops, and the quay met the road with a lip)Physics.SyncTransforms();
            var wall=new List<Matrix4x4>();float bed=Vinelands.Water-5;
            for(float n=-62;n<62;n+=2) {
                if(n>-38&&n<-31)continue;   // (the slipway's gap)
                var a=F(997.6f,n+1);float top=Top(F(996.5f,n+1))+.12f;wall.Add(Matrix4x4.TRS(new Vector3(a.x,(top+bed)*.5f,a.z),Quaternion.identity,new Vector3(1.4f,top-bed,2.02f)));
            }
            QuarryFloor.Combine("Cais quay wall",wall,plinth,root,true);
            for(float n=-56;n<=58;n+=9.5f){if(n>-40&&n<-29)continue;var b=F(996.2f,n);b.y=Top(b)+.1f;Put("cais_cabeco",b,0);}
            for(float n=-50;n<=50;n+=25){var l=F(995,n);l.y=Top(l)+.1f;Put("lamp_post",l,0,T(0xffffff,0x1d2b3a,0xffffff,0xffffff),null,false);}
            {var s=F(997.6f,-34.5f);s.y=Top(F(996.5f,-34.5f))+.1f;Put("cais_rampa",s,270,null,null,false);}   // (it falls east, into the river)
            {var p=F(1004.2f,8);p.y=Vinelands.Water-.1f;Put("cais_pontao",p,270,null,null,false);}   // (its gangway west, up to the quay)
            if(BayviewKit.Has("rabelo"))foreach(float n in new[]{0f,18f}){var b=F(1010.5f,n);Put("rabelo",new Vector3(b.x,Vinelands.Water,b.z),0,null,null,false);}
            foreach(float n in new[]{-12f,26f}){var q=F(991.5f,n);q.y=Top(q)+.1f;Put("cais_pipas",q,0);}
            Spots["barqueiro"]=(new Vector3(F(990,-3).x,Top(F(990,-3))+.1f,F(990,-3).z),90);
            for(float n=30;n<=58;n+=5.5f){if(dress.NextDouble()<.45)continue;var q=F(987,n);q.y=Top(q)+.15f;Parking.Add((q,90));}
            for(float n=-54;n<=-42;n+=5.5f){if(dress.NextDouble()<.45)continue;var q=F(987,n);q.y=Top(q)+.15f;Parking.Add((q,90));}
        }

        // ---- the Praça do Cais: paved in waves like the Largo, tascas round its north and south sides, their terraces out,
        // the cross where the Calçada comes in
        void Praca()
        {
            Plaza("Praca do Cais",new Vector2(930,-18),new Vector2(966,22),Waves(),true);
            var signs=new Queue<string>(new[]{"TASCA DO RIO","O BARQUEIRO","CASA DO PIPO","O RABELO","MERCEARIA DO CAIS","FADO E VINHO"});
            Frontage(new Vector2(930,24),new Vector2(966,24),new Vector2(0,1),signs,true);
            Frontage(new Vector2(966,-20),new Vector2(930,-20),new Vector2(0,-1),signs,true);
            foreach(var (a,n) in new[]{(940f,16f),(956f,15f),(944f,-12f)}){var e=F(a,n);if(ValleyField.RoadDistance(e.x,e.z)>5.5f)Put("vila_esplanada",e,(float)dress.NextDouble()*360,T(0xffffff,0xffffff,Awnings[dress.Next(Awnings.Length)],0xffffff),null,false);}
            {var x=F(938,-8);if(ValleyField.RoadDistance(x.x,x.z)>5f)Put("vila_cruzeiro",x,0);}
            Spots["tasca"]=(F(948,18),180);
            foreach(var (a,n,y) in new[]{(934f,-14f,0f),(961f,18f,180f),(961f,-14f,0f)}){var q=F(a,n);if(ValleyField.RoadDistance(q.x,q.z)>5f){q.y=Top(q)+.15f;Parking.Add((q,y));}}
        }

        // ---- the three lodges across the road from the quay, their names along their fronts, barrels at their doors; the chapel
        void Lodges()
        {
            foreach(var (n,name,tint) in new[]{(-62f,"BORGES VELHO",0xf3eee2),(72f,"SAMPAIO",0xe8c27a),(118f,"CRUZ & FILHOS",0xedc9b4)}) {   // (clear of the praca's houses)
                var (at,east)=RiverAt(n);var c=at-east*(3.75f+3f+9.3f);c.y=H(c.x,c.z);
                if(!Building("cais_armazem",c,Yaw(c,at),T(tint,0x8b1d2c,0x8b1d2c,0x5a3a24),name,4.5f,4.4f,8f))continue;   // (cut into the slope behind: lodges are cellars)
                var along=Quaternion.Euler(0,90,0)*east;
                foreach(float u in new[]{-13f,13f}){var q=c+east*(9.3f+1.5f)+along*u;q.y=H(q.x,q.z);Put("cais_pipas",q,Yaw(q,q+along));}
                if(name=="SAMPAIO"){var v=c+east*(9.3f+2.6f)+along*6f;v.y=H(v.x,v.z);Spots["lodge"]=(v,Yaw(v,v+along));}
            }
            {var (at,east)=RiverAt(-108);var c=at-east*(3.75f+3f+4.7f);c.y=H(c.x,c.z);Building("cais_capela",c,Yaw(c,at),T(0xf8f5ee,0x2a5490,0x2a5490,0x5a3a24),null,3f,4.4f,3f);}
        }

        // ---- the boatmen's houses up and down the river road's town side, each on its stone store, a garden plot behind
        void Ribeira()
        {
            foreach(var (n0,n1) in new[]{(140f,305f),(-112f,-220f)}) {
                float step=n1>n0?1:-1;float n=n0;
                while((n1-n)*step>0) {
                    var (at,east)=RiverAt(n);var c=at-east*(3.75f+2.6f+5.5f);c.y=H(c.x,c.z);
                    if(Building("cais_casa",c,Yaw(c,at),House(dress.NextDouble()<.3?"azulejo":"granito"),null,3.5f,4.4f,3.5f)){var back=-east;yards.Add((c+back*5.5f,back,Quaternion.Euler(0,90,0)*back,7.4f,"horta"));}
                    n+=step*(9f+(float)dress.NextDouble()*5f);
                }
            }
        }

        // ---- the Passeio Ribeirinho: a promenade along the river side of the road from the quay north (n 64-330), planes,
        // benches to the river, lamps, the railing; the river beach (Praia da Senhorinha) at its end
        void PasseioRibeirinho()
        {
            var line=new List<Vector3>();foreach(var r in river)if(r.n>=64&&r.n<=330)line.Add(r.at);if(line.Count<10)return;
            var pts=line.ToArray();var d0=pts[pts.Length/2+1]-pts[pts.Length/2-1];int side=(-d0.z)>0?1:-1;   // (the side whose normal points east)
            Pavement(pts,3.75f,5.5f,side);
            float run=0;
            for(int i=1;i<pts.Length-1;i++) {
                run+=Vector3.Distance(pts[i],pts[i-1]);var d=pts[i+1]-pts[i-1];d.y=0;d.Normalize();var e=new Vector3(-d.z,0,d.x)*side;
                int k=Mathf.RoundToInt(run);
                if(k%4==0){var g=pts[i]+e*(3.75f+5.45f);g.y=H(g.x,g.z);Put("cais_guarda",g,Yaw(g,g+d)-90,null,null,false);}
                if(k%14==7){var t=pts[i]+e*(3.75f+7.2f);t.y=H(t.x,t.z);if(t.y>Vinelands.Water+.4f)Put("tree_round",new Vector3(t.x,t.y-.05f,t.z),(float)dress.NextDouble()*360,null,null,true,1.2f+(float)dress.NextDouble()*.2f);}
                if(k%14==0){var b=pts[i]+e*(3.75f+4.4f);b.y=Top(pts[i]+e*3.75f)+.08f;Put("bench",b,Yaw(b,b+e),null,null,false);}
                if(k%28==21){var l=pts[i]+e*(3.75f+4.9f);l.y=Top(pts[i]+e*3.75f)+.08f;Put("lamp_post",l,0,T(0xffffff,0x1d2b3a,0xffffff,0xffffff),null,false);}
            }
            Plaza("Praia da Senhorinha",new Vector2(988,256),new Vector2(1024,286),Sand(),false);
            if(BayviewKit.Has("parasol"))foreach(var (a,n) in new[]{(998f,264f),(1006f,276f),(994f,279f),(1010f,265f)}){var p=F(a,n);if(p.y>Vinelands.Water+.4f)Put("parasol",p,(float)dress.NextDouble()*360,null,null,false);}
            if(BayviewKit.Has("kiosk")){var q=F(990,270);if(q.y>Vinelands.Water+.4f&&ValleyField.RoadDistance(q.x,q.z)>6f)Put("kiosk",q,270);}
        }

        // ---- Ponte Nova: the kit's arches and parapets under and beside the road's own deck, the parapets solid (a car can't
        // drive off it into the river)
        void Ponte()
        {
            var c=Vinelands.Centre+new Vector2(1053,-70);Put("cais_ponte",new Vector3(c.x,Vinelands.PonteDeck,c.y),0,null,null,false);
            var rails=new GameObject("Ponte Nova parapets");rails.transform.SetParent(root,false);
            for(int k=0;k<12;k++)foreach(float s in new[]{-1f,1f}) {
                float a=998+k*10;float u=(a-1053)/55f;float y=Vinelands.PonteDeck+.8f*(1-u*u);var w=Vinelands.Centre+new Vector2(a,-70+s*4.75f);
                var box=rails.AddComponent<BoxCollider>();box.center=new Vector3(w.x,y+.55f,w.y);box.size=new Vector3(10.2f,1.1f,.3f);
            }
        }

        // ---- the Calçada Velha: the old mule path's granite steps from the town's east edge down to the Praça do Cais, with low
        // walls each side, broken where it crosses the road's bends (the road is the path there)
        void CalcadaVelha()
        {
            Vector2[] pts={new(222,5),new(260,10),new(380,30),new(500,20),new(680,15),new(800,10),new(900,5),new(928,2)};
            var line=Dense(pts,1.2f);var steps=new List<Matrix4x4>();var walls=new List<Matrix4x4>();
            for(int i=1;i<line.Length;i++) {
                var a=line[i-1];var b=line[i];var mid=(a+b)*.5f;if(ValleyField.RoadDistance(mid.x,mid.z)<5.5f)continue;
                bool inside=false;var m2=new Vector2(mid.x,mid.z);foreach(var o in footprints){var l=Quaternion.Euler(0,-o.yaw,0)*new Vector3(m2.x-o.c.x,0,m2.y-o.c.y);if(Mathf.Abs(l.x)<o.half.x+1.5f&&Mathf.Abs(l.z)<o.half.y+1.5f){inside=true;break;}}if(inside)continue;
                var d=b-a;d.y=0;float y=Mathf.Max(a.y,b.y);var rot=Quaternion.LookRotation(d);
                steps.Add(Matrix4x4.TRS(new Vector3(mid.x,y-.15f,mid.z),rot,new Vector3(2.2f,.3f+Mathf.Abs(a.y-b.y),d.magnitude+.04f)));
                foreach(float s in new[]{-1f,1f}){var w=mid+Vector3.Cross(Vector3.up,d.normalized)*s*1.3f;float g=H(w.x,w.z);walls.Add(Matrix4x4.TRS(new Vector3(w.x,g+.15f,w.z),rot,new Vector3(.35f,.9f+Mathf.Abs(a.y-b.y),d.magnitude+.04f)));}
            }
            var stone=WorldLook.Kit("Vila step granite",new Color(.66f,.64f,.6f),WorldLook.Pattern.Stone,0,.1f,true);
            QuarryFloor.Combine("Calcada Velha steps",steps,stone,root,false);QuarryFloor.Combine("Calcada Velha walls",walls,plinth,root,false);
        }

        static Material sand;
        // the river beach's sand: warm, pale, a little shell and grit
        static Material Sand()
        {
            if(sand!=null)return sand;const int N=256;var tex=new Texture2D(N,N,TextureFormat.RGBA32,true){wrapMode=TextureWrapMode.Repeat,name="River sand"};var px=new Color32[N*N];var r=new System.Random(13);
            for(int i=0;i<N*N;i++){int k=r.Next(100);byte c=(byte)(k<5?170:k<12?246:214+r.Next(16));px[i]=new Color32(c,(byte)(c-14),(byte)(c-48),255);}
            tex.SetPixels32(px);tex.Apply(true);sand=TrialWorld.Material("River sand",Color.white,0,.05f);sand.SetTexture("_BaseMap",tex);sand.mainTexture=tex;return sand;
        }
    }
}
