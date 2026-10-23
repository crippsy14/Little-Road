using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Vinelands zone 6 (docs/91), with zone 1's machinery (VinelandsVila.cs): the Alto da Portela, the town's newest edge on
    // the saddle where the entry road crosses the ridge (the fire station on the junction, the school, the football ground
    // and its courts, the health centre on Rua Direita, the Portela's flats and semi-detached houses along Rua das Amendoeiras,
    // the olive press, the lay-by with the double view); and the Entroncamento, the co-op wine quarter where the Estrada do Meio
    // meets the entry road (the Adega and its tanks and weighbridge, the bottling plant, the cooperage, the warehouses, the farm
    // supply shop, a row of co-op houses, tractors). The portela kit is car-forge forge/world/kits/portela.py.
    public sealed partial class VinelandsVila
    {
        // the arc length along a road nearest a frame point
        static float SNear(HillcrestRoute.Road road,Vector2 frame){var w=Vinelands.Centre+frame;float best=0,bd=float.MaxValue;for(float s=0;s<road.Length;s+=1){var q=road.At(s);float d=(new Vector2(q.x,q.z)-w).sqrMagnitude;if(d<bd){bd=d;best=s;}}return best;}
        // a building fronting a road at arc length s, on the side away from (side +1) or toward (-1) the road's right, set back
        bool Front(HillcrestRoute.Road road,float s,int side,string id,float setback,BayviewKit.Tint tint=null,string sign=null,float fall=4f,float rise=4f)
        {
            var at=road.At(s);var n=road.Right(s);n.y=0;n.Normalize();n*=side;var size=Footprint(id);var c=at+n*(road.width*.5f+setback+size.y*.5f);c.y=H(c.x,c.z);
            return Building(id,c,Yaw(c,at),tint,sign,fall,4.4f,rise);
        }
        int SideOf(HillcrestRoute.Road road,float s,Vector2 frame){var at=road.At(s);var n=road.Right(s);var w=Vinelands.Centre+frame;return Vector2.Dot(new Vector2(n.x,n.z),w-new Vector2(at.x,at.z))>0?1:-1;}

        void Portela()
        {
            var en=HillcrestRoute.Find(Vinelands.EntryName);var rd=HillcrestRoute.Find(Vinelands.RuaDireitaName);if(en==null||rd==null)return;
            var red=T(0xf2efe6,0xc0392b,0xc0392b,0xc0392b);var civic=T(0xf3eee2,0x2a5490,0x2a5490,0x2a5490);
            // the fire station on the junction's south-east corner, its engines' doors to the road; the school east along it
            bool AtSite(string name,string id,BayviewKit.Tint tint,string sign){if(!Vinelands.Sites.TryGetValue(name,out var st))return false;var c=st.c;c.y=H(c.x,c.z);return Building(id,c,st.yaw,tint,sign,2.5f,4.4f,2.5f);}
            AtSite("bombeiros","portela_bombeiros",red,"BOMBEIROS VOLUNTARIOS");
            {AtSite("escola","portela_escola",civic,"ESCOLA BASICA DA CUMEADA");float s=SNear(en,new(110,735));
                var at=en.At(s);var n=en.Right(s);n.y=0;n.Normalize();n*=SideOf(en,s,new(110,690));var along=Quaternion.Euler(0,90,0)*n;
                foreach(int k in new[]{-2,-1,1,2}){var q=at+n*(en.width*.5f+3.5f)+along*(k*5.5f+(k>0?14:-14));q.y=Top(at+n*(en.width*.5f))+.1f;Parking.Add((q,Yaw(q,at)));}}
            // the football ground north of the road, on the saddle's one flat field; the stand on its road side, the courts beside it
            {var c=F(0,830);Building("portela_campo",c,90,T(0xf3eee2,0xffffff,0xffffff,0xffffff),null,2f,1f,2f);
                var st=F(0,790);Building("portela_bancada",st,Yaw(st,F(0,770)),T(0xd8cfbd,0xffffff,0x2e7d32,0xffffff),null,2f,1f,2f);
                var q=F(0,882);Building("portela_quadra",q,90,null,null,2f,1f,2f);
                for(int k=0;k<6;k++){var p=F(-40+k*6,782);p.y=Top(p)+.12f;Parking.Add((p,0));}}
            // the health centre on Rua Direita's east side at n 600
            AtSite("saude","portela_saude",civic,"CENTRO DE SAUDE");
            // the lay-by at the saddle (Portela miradouro): benches both ways
            foreach(var (a,n,toward) in new[]{(-178f,690f,new Vector2(-600,700)),(-172f,712f,new Vector2(400,780))}){var b=F(a,n);if(ValleyField.RoadDistance(b.x,b.z)>5f)Put("bench",b,Yaw(b,F(toward.x,toward.y)),null,null,false);}
            // Rua das Amendoeiras: the flats and semis along it, gardens behind, cars on the drives
            var am=HillcrestRoute.Find(Vinelands.AmendoeirasName);
            if(am!=null){var arng=new System.Random(9101);int placed=0;
                // (each on its own platform at the street's height: Vinelands.AmendoeirasPlots)
                foreach(var (pc,yaw,side) in Vinelands.AmendoeirasPlots) {
                    string id=arng.NextDouble()<.35?"portela_predio":"portela_geminada";var size=Footprint(id);
                    var tint=id=="portela_predio"?T(RuaWashes[arng.Next(RuaWashes.Length)],0xffffff,0xffffff,0x5a3a24):T(Whites[arng.Next(Whites.Length)],0x6f6a5f,0x6f6a5f,0x8b6f4a,Roofs[arng.Next(Roofs.Length)]);
                    var c=pc;c.y=H(c.x,c.z);var n=Quaternion.Euler(0,yaw,0)*Vector3.back;
                    if(Building(id,c,yaw,tint,null,2.5f,4.4f,2.5f)){placed++;yards.Add((c+n*(size.y*.5f),n,Quaternion.Euler(0,90,0)*n,size.x,"horta"));
                        if(arng.NextDouble()<.6){var q=c-n*(size.y*.5f+2.2f);q.y=H(q.x,q.z)+.1f;Parking.Add((q,yaw+90));}}
                }
                Debug.Log($"LITTLE_ROAD_VINELANDS_PORTELA houses={placed}");
                // the olive press at the street's far end
                float e=am.Length-6;Front(am,e,SideOf(am,e,new(-380,650)),"portela_lagar_azeite",6f,null,null,5f,5f);
            }
            {var q=F(-60,742);q.y=Top(q)+.1f;Spots["bombeiros"]=(q,180);}
            {float s=SNear(en,new(110,735));var q=en.At(s)+Vector3.up*.1f;Spots["escola"]=(q,0);}
        }

        // ---- the Entroncamento: the Adega's yard and buildings south of the entry road east of the Estrada do Meio's junction,
        // the bottling plant, the cooperage and the warehouses across the road, the farm shop, the co-op houses west of the junction
        void Entroncamento()
        {
            var en=HillcrestRoute.Find(Vinelands.EntryName);if(en==null)return;
            float sj=SNear(en,new(900,985));
            var white=T(0xf6f2ea,0x8b1d2c,0x8b1d2c,0x5a3a24);
            // the Adega (south side, past the junction), its tanks behind, the weighbridge at the yard's gate
            int south=SideOf(en,sj+60,new(960,930));
            Front(en,sj+62,south,"adega_coop",14f,white,"ADEGA COOPERATIVA DA CUMEADA",5f,5f);
            {var at=en.At(sj+62);var n=en.Right(sj+62);n.y=0;n.Normalize();n*=south;var along=Quaternion.Euler(0,90,0)*n;
                var tanks=at+n*(en.width*.5f+14+16+8)+along*2;tanks.y=H(tanks.x,tanks.z);Building("portela_cubas",tanks,Yaw(tanks,at),null,null,5f,1f,5f);
                var wb=at+n*(en.width*.5f+5)+along*24;wb.y=Top(wb);Put("portela_bascula",wb,Yaw(wb,wb+along),T(0xf3eee2,0xffffff,0xffffff,0xffffff),null,true);
                for(int k=0;k<5;k++){var q=at+n*(en.width*.5f+5.5f)-along*(10+k*4.2f);q.y=Top(q)+.1f;   // (on the yard's own ground: at the road edge's height they floated, round 4)
                    if(BayviewKit.Has("tractor")&&k%2==0)Put("tractor",q,Yaw(q,q+along));else Parking.Add((q,Yaw(q,q+along)));}
                var sp=at+n*(en.width*.5f+4)+along*8;sp.y=Top(sp)+.1f;Spots["adega"]=(sp,Yaw(sp,at));}
            // across the road: the bottling plant, the cooperage, two warehouses
            int north=-south;
            Front(en,sj+40,north,"portela_engarrafamento",10f,T(0xdfe3e6,0x8b1d2c,0x8b1d2c,0x2a5490),"ENGARRAFAMENTO",5f,5f);
            Front(en,sj+88,north,"portela_tanoaria",8f,null,"TANOARIA",5f,5f);
            Front(en,sj+122,north,"portela_armazem",8f,T(0xdfe3e6,0xffffff,0xffffff,0x2e5e3e),null,5f,5f);
            Front(en,sj+122,south,"portela_armazem",8f,T(0xdfe3e6,0xffffff,0xffffff,0x8b1d2c),null,5f,5f);
            // west of the junction: the farm shop on the south side, the co-op houses on the north
            Front(en,sj-30,south,"portela_agricola",6f,T(0xf3eee2,0xffffff,0xffffff,0x2e7d32),"AGRO CUMEADA",4f,4f);
            Front(en,sj-60,north,"quinta_caseiros",8f,null,null,5f,5f);
            for(int k=0;k<3;k++){var q=en.At(sj-22-k*5);var n=en.Right(sj-22-k*5);n.y=0;n.Normalize();n*=south;q+=n*(en.width*.5f+2.5f);q.y=Top(en.At(sj-22-k*5)+n*(en.width*.5f))+.1f;Parking.Add((q,Yaw(en.At(sj-23-k*5),en.At(sj-21-k*5))));}
        }
    }
}
