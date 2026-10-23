using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Vinelands zone 5 (docs/90), with zone 1's machinery (VinelandsVila.cs): the Mirante crest and the south bowl. The crest
    // road's villas, built by those who made their money in Brazil and France and came home to the view, each in its walled
    // garden behind its gate; the cemetery on the stony ground beside the road, walled and white, its cypresses; the three
    // windmills of the Moinhos do Alto (two ruined, one restored); the Belvedere's knoll with its tower, chapel and terrace; and
    // the Subida das Seis Voltas up the south bowl, its olives along the edges and its hay bales at the voltas, the paddock on
    // the Veiga do Fojo. The mirante kit is car-forge forge/world/kits/mirante.py.
    public sealed partial class VinelandsVila
    {
        // (Zone 5's parts, in VinelandsVila.BuildRoutine's order: the cemetery, mills and knoll first: the villas fit round them.)

        // ---- the villas: both sides of the Mirante road from past the bus terminus to the windmills, set back behind their
        // garden walls, a gate on the road, a palm or a camellia in the front garden
        void Villas()
        {
            var road=HillcrestRoute.Find(Vinelands.MiranteName);if(road==null)return;var vrng=new System.Random(9005);int placed=0;
            string[] ids={"mirante_brasileiro","mirante_chalet","mirante_emigrante","mirante_brasileiro","mirante_emigrante"};
            var walls=new List<Matrix4x4>();var wallMat=TrialWorld.Material("Villa garden wall",new Color(.93f,.9f,.83f),0,.08f);
            foreach(int side in new[]{1,-1}) {
                float s=175+(side>0?0:11);
                while(s<470) {   // (villas near the town, then the open crest to the mills and the knoll)
                    string id=ids[vrng.Next(ids.Length)];var size=Footprint(id);float plot=size.x+9+(float)vrng.NextDouble()*8;
                    float mid=s+plot*.5f;var at=road.At(mid);var n=road.Right(mid);n.y=0;n.Normalize();n*=side;var along=Quaternion.Euler(0,90,0)*n;
                    float set=road.width*.5f+7f;var c=at+n*(set+size.y*.5f);c.y=H(c.x,c.z);
                    var tint=id=="mirante_brasileiro"?T(Tiles[vrng.Next(Tiles.Length)],0x2e5e3e,0x2e5e3e,0x5a3a24,0xc8623c):id=="mirante_chalet"?T(0xf3eee2,0x5a3a24,0x5a3a24,0x5a3a24,0xb5532f):T(RuaWashes[vrng.Next(RuaWashes.Length)],0x6f6a5f,0x6f6a5f,0x8b6f4a,0xc8623c);
                    if(Building(id,c,Yaw(c,at),tint,null,4f,4.4f,4f)) {
                        placed++;
                        // the front wall along the road, its gate at the middle; the garden: a palm or camellias
                        var w0=at+n*(road.width*.5f+3f)-along*(plot*.5f-.5f);var w1=at+n*(road.width*.5f+3f)+along*(plot*.5f-.5f);
                        foreach(var (a,b) in new[]{(w0,at+n*(road.width*.5f+3f)-along*2.6f),(at+n*(road.width*.5f+3f)+along*2.6f,w1)}){var d=b-a;d.y=0;float L=d.magnitude;if(L<.5f)continue;int k=Mathf.CeilToInt(L/2f);
                            for(int i=0;i<k;i++){var m=a+d*((i+.5f)/k);if(OnWay(m)&&ValleyField.RoadDistance(m.x,m.z)<4.4f)continue;float g=H(m.x,m.z);walls.Add(Matrix4x4.TRS(new Vector3(m.x,g+.45f,m.z),Quaternion.LookRotation(d),new Vector3(.4f,1.3f,L/k+.02f)));}}
                        var gp=at+n*(road.width*.5f+3f);gp.y=H(gp.x,gp.z);Put("mirante_portao",gp,Yaw(gp,gp+n),T(0xf6f1e6,0xffffff,0xffffff,0xffffff),null,false);
                        var gd=at+n*(road.width*.5f+5.2f)+along*(plot*.3f*(vrng.NextDouble()<.5?1:-1));gd.y=H(gd.x,gd.z);
                        if(vrng.NextDouble()<.55&&BayviewKit.Has("palm"))Put("palm",new Vector3(gd.x,gd.y-.05f,gd.z),(float)vrng.NextDouble()*360);else Put("rua_camelia",new Vector3(gd.x,gd.y-.05f,gd.z),(float)vrng.NextDouble()*360);
                        var back=n;yards.Add((c+back*(size.y*.5f),back,along,size.x+2,"horta"));
                        if(vrng.NextDouble()<.4){var q=at+n*(road.width*.5f+1.2f)+along*(plot*.35f);q.y=Top(at+n*(road.width*.5f))+.08f;Parking.Add((q,Yaw(road.At(mid-1),road.At(mid+1))+(side>0?0:180)));}
                    }
                    s+=plot;
                }
            }
            QuarryFloor.Combine("Mirante garden walls",walls,wallMat,root,true);
            {var p=Vinelands.Centre+new Vector2(-235,-275);var q=new Vector3(p.x,H(p.x,p.y),p.y);Spots["mirante"]=(q,0);}
            Debug.Log($"LITTLE_ROAD_VINELANDS_MIRANTE villas={placed}");
        }

        // ---- the cemetery: an 84 x 54 m walled enclosure on the stony ground east of the road, its gate to the road, the
        // mortuary chapel at the far end, the family tombs along the walls, graves in rows, cypresses
        void Cemiterio()
        {
            var road=HillcrestRoute.Find(Vinelands.MiranteName);if(road==null)return;
            float best=0,bd=float.MaxValue;var target=Vinelands.Centre+new Vector2(-130,-310);for(float s=0;s<road.Length;s+=2){var q=road.At(s);float d=(new Vector2(q.x,q.z)-target).sqrMagnitude;if(d<bd){bd=d;best=s;}}
            var at=road.At(best);var n=road.Right(best);n.y=0;n.Normalize();if(Vector2.Dot(new Vector2(n.x,n.z),target-new Vector2(at.x,at.z))<0)n=-n;var along=Quaternion.Euler(0,90,0)*n;
            const float L=84,Wd=54;var c=at+n*(road.width*.5f+10+Wd*.5f);c.y=H(c.x,c.z);
            Vector3 P(float u,float v){var q=c+along*u+n*v;q.y=H(q.x,q.z);return q;}
            var wall=new List<Matrix4x4>();var cal=TrialWorld.Material("Cemetery whitewash",new Color(.95f,.93f,.88f),0,.08f);
            void Run(Vector3 a,Vector3 b){var d=b-a;d.y=0;float len=d.magnitude;int k=Mathf.CeilToInt(len/2f);var rot=Quaternion.LookRotation(d);
                for(int i=0;i<k;i++){var m=a+d*((i+.5f)/k);var e0=a+d*(i/(float)k);var e1=a+d*((i+1f)/k);float g=H(m.x,m.z),lo=Mathf.Min(g,Mathf.Min(H(e0.x,e0.z),H(e1.x,e1.z)))-.3f,top=g+2.6f;wall.Add(Matrix4x4.TRS(new Vector3(m.x,(lo+top)*.5f,m.z),rot,new Vector3(.6f,top-lo,len/k+.02f)));}}
            Run(P(-L/2,-Wd/2),P(-2.4f,-Wd/2));Run(P(2.4f,-Wd/2),P(L/2,-Wd/2));Run(P(L/2,-Wd/2),P(L/2,Wd/2));Run(P(L/2,Wd/2),P(-L/2,Wd/2));Run(P(-L/2,Wd/2),P(-L/2,-Wd/2));
            QuarryFloor.Combine("Cemetery walls",wall,cal,root,true);
            {var g=P(0,-Wd/2);Put("quinta_portao",g,Yaw(g,g-n),T(0xffffff,0xffffff,0xffffff,0xffffff),"CEMITERIO",false);}
            {var ch=P(0,Wd/2-6);Building("cais_capela",ch,Yaw(ch,c),T(0xf8f5ee,0x2a5490,0x2a5490,0x5a3a24),null,4f,1f,4f);}
            var crng=new System.Random(9006);
            for(float u=-L/2+4;u<=L/2-4;u+=4.2f)foreach(float v in new[]{-Wd/2+3.2f,Wd/2-3.2f}){if(Mathf.Abs(u)<8&&v>0)continue;if(Mathf.Abs(u)<4&&v<0)continue;var p=P(u,v);Building("mirante_jazigo",p,Yaw(p,p-n*Mathf.Sign(v)),null,null,1.5f,1f,1.5f);}
            for(float u=-L/2+8;u<=L/2-8;u+=2.2f)for(float v=-Wd/2+9;v<=Wd/2-12;v+=3.4f){if(Mathf.Abs(u)<2.5f)continue;var p=P(u,v);if(crng.NextDouble()<.12)continue;Put("mirante_campa",p,Yaw(p,p+n),null,null,false);}
            foreach(var (u,v) in new[]{(-6f,-Wd/2+6),(6f,-Wd/2+6),(-6f,0f),(6f,0f),(-6f,Wd/2-14),(6f,Wd/2-14),(-L/2+3f,-Wd/2+3f),(L/2-3f,-Wd/2+3f)}){var p=P(u,v);Put("vila_cipreste",new Vector3(p.x,p.y-.05f,p.z),0,null,null,true,.95f+.25f*(float)crng.NextDouble());}
        }

        // ---- the Moinhos do Alto: three mills on the crest by the road, two ruined, the middle one restored
        void Moinhos()
        {
            foreach(var (a,n,id) in new[]{(-412f,-496f,"mirante_moinho"),(-478f,-448f,"mirante_moinho_vivo"),(-470f,-512f,"mirante_moinho")}) {
                var p=F(a,n);float yaw=id=="mirante_moinho_vivo"?Yaw(p,F(-300,-330)):(a*7)%360;Building(id,p,yaw,T(0xf3eee2,0xffffff,0xffffff,0xffffff),null,2.5f,4.4f,2.5f);
            }
            for(int i=0;i<16;i++){var p=F(-445+(Hash(i,1,60)-.5f)*90,-480+(Hash(i,2,61)-.5f)*80);if(Clear(p,1f,true))Put("rock_cluster",p,Hash(i,3,62)*360,null,null,true,.5f+.4f*Hash(i,4,63));}
        }

        // ---- the Belvedere: the knoll's paved terrace and car park round the roads' end, the bell tower and its chapel, benches
        // to the view both ways, the telescope
        void Belvedere()
        {
            var b=Vinelands.Belvedere;var f=new Vector2(b.x,b.z)-Vinelands.Centre;
            Plaza("Largo do Belvedere",f+new Vector2(-18,-18),f+new Vector2(18,18),Setts(),true);
            var tw=FV(f+new Vector2(-14,6));Building("torre_belvedere",tw,Yaw(tw,b),T(0xf7f3ea,0x2a5490,0x2a5490,0x2e5e3e),null,3f,1f,3f);
            var er=FV(f+new Vector2(-12,-10));Building("cais_capela",er,Yaw(er,b),T(0xf8f5ee,0x2a5490,0x2a5490,0x5a3a24),null,3f,1f,3f);
            foreach(var (a,n,toward) in new[]{(14f,-14f,new Vector2(400,-600)),(16f,0f,new Vector2(900,-700)),(12f,14f,new Vector2(400,0)),(-16f,16f,new Vector2(-900,-400))}){var p=FV(f+new Vector2(a,n));p.y=Top(p)+.08f;Put("bench",p,Yaw(p,FV(toward)),null,null,false);}
            if(BayviewKit.Has("telescope")){var t=FV(f+new Vector2(17,-6));t.y=Top(t)+.08f;Put("telescope",t,Yaw(t,FV(new(900,-700))),null,null,false);}
            foreach(var (a,n) in new[]{(-17f,-17f),(17f,17f),(-17f,17f),(17f,-17f)}){var l=FV(f+new Vector2(a,n));l.y=Top(l)+.08f;Put("lamp_post",l,0,T(0xffffff,0x1d2b3a,0xffffff,0xffffff),null,false);}
            foreach(var (a,n,y) in new[]{(4f,-12f,90f),(8.5f,-12f,90f),(4f,12f,270f)}){var q=FV(f+new Vector2(a,n));q.y=Top(q)+.15f;Parking.Add((q,y));}
            {var q=FV(f+new Vector2(-4,-4));q.y=Top(q)+.1f;Spots["belvedere"]=(q,Yaw(q,b));}
        }

        // ---- the Seis Voltas: olives along the road's edges in lines, hay bales round the outside of each volta, a marker stone
        // with the volta's name at each; the paddock on the Veiga do Fojo, the start
        void Voltas()
        {
            var road=HillcrestRoute.Find(Vinelands.ClimbName);if(road==null)return;var half=road.width*.5f;
            for(float s=30;s<road.Length-40;s+=14)foreach(int side in new[]{1,-1}) {
                if(Hash(s,side,64)<.35f)continue;var n=road.Right(s);n.y=0;n.Normalize();var p=road.At(s)+n*side*(half+5.5f);p.y=H(p.x,p.z);
                if(Clear(p,1.5f,true)&&Put("olive_tree",new Vector3(p.x,p.y-.05f,p.z),Hash(s,side,65)*360,null,null,true,.8f+.3f*Hash(s,side,66))!=null)Olives++;
            }
            foreach(var (name,at) in Vinelands.ClimbMarks) {
                if(!name.StartsWith("Volta")&&!name.StartsWith("Gancho"))continue;
                // (the volta's apex: the road's point nearest the marker's turn, a little past it)
                var w=Vinelands.Centre+at;float best=0,bd=float.MaxValue;for(float s=0;s<road.Length;s+=1){var q=road.At(s);float d=(new Vector2(q.x,q.z)-w).sqrMagnitude;if(d<bd){bd=d;best=s;}}
                float apex=best+18;var c=road.At(apex);var dir=road.At(apex+6)-road.At(apex-6);dir.y=0;dir.Normalize();var outN=road.Right(apex);outN.y=0;outN.Normalize();
                var turnDir=road.At(apex+10)-road.At(apex);turnDir.y=0;if(Vector3.Dot(outN,turnDir.normalized)>0)outN=-outN;   // (the outside: away from where the road turns)
                if(BayviewKit.Has("hay_bale"))for(int k=-3;k<=3;k++){var p=road.At(apex+k*3.2f)+outN*(half+3.2f);p.y=H(p.x,p.z);if(Clear(p,0,true))Put("hay_bale",p,Yaw(road.At(apex+k*3.2f-1),road.At(apex+k*3.2f+1)),null,null,true);}
                var mk=c+outN*(half+6.5f);mk.y=H(mk.x,mk.z);if(Clear(mk,0,true))Put("quinta_portao",mk,Yaw(mk,c),T(0xffffff,0xffffff,0xffffff,0xffffff),name.ToUpperInvariant(),false);
            }
            // the paddock: the Veiga do Fojo's field, gravel, a few cars and trailers waiting
            // (north of the climb's start, beside the river road: round 1 laid it on the slope across the climb)
            Plaza("Veiga do Fojo paddock",new Vector2(1015,-962),new Vector2(1088,-912),Gravel(),true);
            foreach(var (a,n,y) in new[]{(1025f,-920f,0f),(1033f,-920f,0f),(1041f,-920f,0f),(1060f,-955f,180f),(1068f,-955f,180f)}){var q=F(a,n);q.y=Top(q)+.15f;Parking.Add((q,y));}
            {var q=F(1045,-940);q.y=Top(q)+.1f;Spots["veiga"]=(q,90);}
        }
    }
}
