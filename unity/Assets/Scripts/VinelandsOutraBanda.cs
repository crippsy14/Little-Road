using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Vinelands zone 8 (docs/93), with zone 1's machinery (VinelandsVila.cs): the far bank, the Outra Banda, the view the whole
    // town looks at. The strip by the water at the bridge's foot (orange and lemon groves, kitchen plots, a handful of houses
    // and the café where the old ferry landed); the slope above, partly replanted in young vines by small growers, then the
    // abandoned terraces gone to broom, then pine; the Aldeia da Outra Banda on its spur, eight people all year and its
    // chapel's August festa; the ruined Quinta do Corvo with its one huge fig at the end of the lane south.
    public sealed partial class VinelandsVila
    {

        // ---- the strip by the water: the café and houses at the bridge's foot, then groves and plots north along the lane
        void Margem()
        {
            var ou=HillcrestRoute.Find(Vinelands.OutraName);if(ou==null)return;var mrng=new System.Random(9301);int houses=0;
            string[] ids={"casa_douro_d","casa_douro_c","casa_douro_a"};
            foreach(int side in new[]{1,-1})for(float s=22;s<200;) {
                string id=ids[mrng.Next(ids.Length)];var size=Footprint(id);bool cafe=houses==0&&side==1;
                if(Front(ou,s+size.x*.5f,side,cafe?"vila_loja_6":id,3f,cafe?House("loja"):House(mrng.NextDouble()<.3?"azulejo":"granito"),cafe?"CAFE DO BARQUEIRO":null,3.5f,3.5f)) {
                    houses++;var at=ou.At(s+size.x*.5f);var n=ou.Right(s+size.x*.5f);n.y=0;n.Normalize();n*=side;var c=at+n*(ou.width*.5f+3+size.y*.5f);yards.Add((c+n*(size.y*.5f),n,Quaternion.Euler(0,90,0)*n,size.x,"horta"));
                    if(cafe){var e=at+n*(ou.width*.5f+1.2f);e.y=H(e.x,e.z);Put("vila_esplanada",e,0,T(0xffffff,0xffffff,0x2a5490,0xffffff),null,false);Spots["barqueiro_cafe"]=(e,Yaw(e,at));}
                }
                s+=size.x+5+(float)mrng.NextDouble()*10;
            }
            // the groves on the flat by the river: oranges and lemons in rows, plots between
            int grove=Grove(new Vector2[]{new(1105,-20),new(1160,-20),new(1185,120),new(1150,150),new(1110,60)},7,"vila_laranjeira",.85f,1.1f,140,.8f);
            for(float a=1180;a<=1240;a+=6)for(float n=-60;n<=40;n+=8){var p=FV(new(a,n));if(Hash(a,n,145)<.35f||!Clear(p,2f,true))continue;Put("vila_horta",p,0,null,null,false);}
            Debug.Log($"LITTLE_ROAD_VINELANDS_MARGEM houses={houses} grove={grove}");
        }

        // ---- the slope: young vines in patches on the lower terraces' line, broom above, pine on the top
        void Encosta()
        {
            var young=new Batch(root,"Far bank vines",WorldLook.Kit("Young vines",new Color(.40f,.56f,.22f),WorldLook.Pattern.Leaf,0,.25f,true));
            var broom=new Batch(root,"Far bank broom",WorldLook.Kit("Broom scrub far",new Color(.38f,.44f,.29f),WorldLook.Pattern.Leaf,0,.2f,true));
            int vines=0,scrub=0;
            for(float n=-500;n<=500;n+=1.5f) {
                float ra=Vinelands.RiverAtN(n);
                for(float off=160;off<=560;off+=2.4f) {
                    var f=new Vector2(ra+off+(Hash(n,off,150)-.5f)*1.2f,n);var p=FV(f);if(!Clear(p,1f,true))continue;
                    float patch=Mathf.PerlinNoise(f.x*.012f+3,f.y*.012f+7);
                    if(off<400&&patch>.55f&&Mathf.Repeat(off,4.8f)<2.4f){young.Bush(p,Vector3.forward,1.0f,.7f,.75f,.08f,Hash(n,off,151)*97);vines++;if(vines%10==0)AreaAudit.ExtraNature.Add(p);}
                    else if(off>300&&Hash(n,off,152)<.06f){broom.Lump(p,1.1f,.9f,.6f,Hash(n,off,153)*97);scrub++;if(scrub%6==0)AreaAudit.ExtraNature.Add(p);}
                }
            }
            young.Finish();broom.Finish();
            int pines=Grove(new Vector2[]{new(1560,-500),new(1760,-500),new(1760,500),new(1600,500)},14,"tree_pine",.9f,1.3f,160,.75f);
            Debug.Log($"LITTLE_ROAD_VINELANDS_ENCOSTA vines={vines} scrub={scrub} pines={pines}");
        }

        // ---- the Aldeia: houses round its little largo on the spur, the chapel of São Gonçalo, the festa's bunting across the largo
        void Aldeia()
        {
            var c=FV(Vinelands.AldeiaAt);var arng=new System.Random(9302);int placed=0;string[] ids={"casa_douro_c","casa_douro_d","casa_douro_a","casa_douro_c"};
            {var ch=c+new Vector3(0,0,18);ch.y=H(ch.x,ch.z);Building("cais_capela",ch,Yaw(ch,c),T(0xf8f5ee,0x2a5490,0x2a5490,0x5a3a24),null,3f,4.4f,3f);}
            {var x=c+new Vector3(-6,0,-4);x.y=H(x.x,x.z);Put("vila_cruzeiro",x,0);}
            foreach(var (r,count) in new[]{(20f,8),(32f,12)})for(int i=0;i<count;i++) {
                float ang=i*360f/count+r*2;var p=c+Quaternion.Euler(0,ang,0)*Vector3.forward*r;p.y=H(p.x,p.z);string id=ids[arng.Next(ids.Length)];
                if(Building(id,p,Yaw(p,c),House(arng.NextDouble()<.4?"fechada":"granito"),null,3.5f,4.4f,3.5f)){placed++;var back=p-c;back.y=0;back.Normalize();var sz=Footprint(id);yards.Add((p+back*(sz.y*.5f),back,Quaternion.Euler(0,90,0)*back,sz.x,"horta"));}
            }
            // the festa: bunting zigzag across the largo, from pole to pole
            for(int k=0;k<6;k++){float a0=k*60,a1=a0+150;var pa=c+Quaternion.Euler(0,a0,0)*Vector3.forward*13;var pb=c+Quaternion.Euler(0,a1,0)*Vector3.forward*13;
                foreach(var q in new[]{pa,pb}){var g=q;g.y=H(g.x,g.z);Put("flag_pole",g,0,T(0xffffff,0xffd23f,0x2a5490,0),null,true);}
                pa.y=H(pa.x,pa.z)+5.2f;pb.y=H(pb.x,pb.z)+5.2f;HillcrestTown.BuntingLine(root,pa,pb,1.1f);}
            {var q=c+new Vector3(6,0,-6);q.y=Top(q)+.1f;Spots["aldeia"]=(q,0);}
            Debug.Log($"LITTLE_ROAD_VINELANDS_ALDEIA houses={placed}");
        }

        // ---- the Quinta do Corvo: the manor in ruins at the lane's end, the huge fig, the young vines replanted beside it
        void Corvo()
        {
            var co=HillcrestRoute.Find(Vinelands.CorvoName);if(co==null)return;var end=co.At(co.Length-2);var dir=co.At(co.Length-1)-co.At(co.Length-8);dir.y=0;dir.Normalize();
            var r=end+dir*20;r.y=H(r.x,r.z);Building("quinta_ruina",r,Yaw(r,end),T(0xd8cfbd,0xffffff,0xffffff,0xffffff),null,4f,3f,4f);
            var f=end+Vector3.Cross(Vector3.up,dir)*16+dir*6;f.y=H(f.x,f.z);Put("vila_figueira",new Vector3(f.x,f.y-.05f,f.z),30,null,null,true,2.2f);
            {var q=end+Vector3.Cross(Vector3.up,dir)*-8;q.y=Top(q)+.1f;Spots["corvo"]=(q,Yaw(q,r));}
        }
    }
}
