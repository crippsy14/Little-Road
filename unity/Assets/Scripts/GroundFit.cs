using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Ground fit (docs/78, Richard 18 Oct: "all props placed at the angle of the land they've been put on, for this and
    // moving forward"). Every kit piece BayviewKit spawns is fitted to the ground under its footprint by its kind, so the
    // rule holds for every placement in every town, today's and any added later, with no layout code changed:
    //   tilt   loose things settle onto the slope: the footprint's corners sampled, the piece turned to their plane (up to
    //          its kind's cap) and lowered until no corner is in the air (rocks, bales, tents, crates, benches, planting);
    //   rake   long runs follow the slope along their length and stay upright across it (fences);
    //   plumb  things that grow or are planted stand upright, sunk until no edge of their foot hangs in the air (trees,
    //          posts, lamps, bins, signs);
    //   leave  buildings and big structures (their plinths and pads level them), animated props and anything over 8 m.
    // Only on the land: a piece whose base stands more than 0.35 m off the land (a quay, a pavement on a plinth, a deck) is
    // on a structure, not the land, and is left as placed. Behind --prop-forge (with Prop Forge's world). A new kit id
    // takes its kind from the table below; an id not in it is plumb if it's small (under 3 m), else left.
    public static class GroundFit
    {
        public static bool On=>PropForge.Enabled;
        enum Mode{Leave,Plumb,Rake,Tilt}
        static readonly Dictionary<string,(Mode mode,float cap)> kinds=new(){
            {"rock_a",(Mode.Tilt,25)},{"rock_b",(Mode.Tilt,20)},{"rock_cluster",(Mode.Tilt,25)},{"rock_stand_a",(Mode.Tilt,8)},{"rock_stand_b",(Mode.Tilt,8)},
            {"rock_stand_c",(Mode.Tilt,15)},{"rock_stand_d",(Mode.Tilt,20)},{"hay_bale",(Mode.Tilt,20)},{"tent",(Mode.Tilt,15)},{"campfire",(Mode.Tilt,20)},
            {"picnic_table",(Mode.Tilt,10)},{"bench",(Mode.Tilt,8)},{"caravan",(Mode.Tilt,6)},{"flower_bed",(Mode.Tilt,15)},{"flower_bush",(Mode.Tilt,25)},
            {"bush",(Mode.Tilt,25)},{"grass_tuft",(Mode.Tilt,30)},{"reeds",(Mode.Tilt,25)},{"deck_chair",(Mode.Tilt,10)},{"windbreak",(Mode.Tilt,10)},
            {"fish_crates",(Mode.Tilt,10)},{"barrel",(Mode.Tilt,8)},{"lobster_pot",(Mode.Tilt,15)},{"rope_coil",(Mode.Tilt,20)},{"buoy",(Mode.Tilt,20)},
            {"sandwich_board",(Mode.Tilt,6)},{"planter",(Mode.Tilt,8)},{"slate_stack",(Mode.Tilt,8)},{"tyre_wall",(Mode.Tilt,10)},{"water_trough",(Mode.Tilt,6)},
            {"milk_churn",(Mode.Tilt,8)},{"tractor",(Mode.Tilt,10)},{"rowing_boat",(Mode.Tilt,10)},
            {"wheelbarrow",(Mode.Tilt,12)},{"beehive",(Mode.Tilt,5)},{"kennel",(Mode.Tilt,6)},{"garden_shed",(Mode.Tilt,4)},{"washing_line",(Mode.Rake,10)},{"bus_shelter",(Mode.Tilt,3)},
            {"fence",(Mode.Rake,25)},
            {"lamp_post",(Mode.Plumb,0)},{"flag_pole",(Mode.Plumb,0)},{"flag_wales",(Mode.Plumb,0)},{"telegraph_pole",(Mode.Plumb,0)},{"chevron_sign",(Mode.Plumb,0)},
            {"map_board",(Mode.Plumb,0)},{"telescope",(Mode.Plumb,0)},{"litter_bin",(Mode.Plumb,0)},{"bollard",(Mode.Plumb,0)},{"pillar_box",(Mode.Plumb,0)},
            {"phone_box",(Mode.Plumb,0)},{"bike_rack",(Mode.Plumb,0)},{"parasol",(Mode.Plumb,0)},{"field_gate",(Mode.Plumb,0)},{"ad_board",(Mode.Plumb,0)},
            {"vine_row",(Mode.Leave,0)},{"lavender_row",(Mode.Leave,0)},{"sunflower_row",(Mode.Leave,0)}};   // (crop rows: their own fitting, BayviewCountryside)
        public static int Tilted,Raked,Sunk,Skipped,AirBefore,AirAfter;
        static readonly Dictionary<string,Bounds> footprints=new();

        static (Mode mode,float cap) KindOf(string id,Bounds local)
        {
            if(kinds.TryGetValue(id,out var k))return k;
            if(id.StartsWith("tree_")||id=="palm"||id=="olive_tree")return (Mode.Plumb,0);
            float size=Mathf.Max(local.size.x,local.size.y,local.size.z);
            return size<3?(Mode.Plumb,0):(Mode.Leave,0);
        }
        // The piece's own bounds in its local axes (its Collision parts aside), once per kit id.
        static Bounds Local(string key,GameObject go)
        {
            if(footprints.TryGetValue(key,out var b))return b;bool any=false;var inv=go.transform.worldToLocalMatrix;
            foreach(var f in go.GetComponentsInChildren<MeshFilter>(true)) {
                if(f.sharedMesh==null||f.name.StartsWith("Collision"))continue;var mb=f.sharedMesh.bounds;var m=inv*f.transform.localToWorldMatrix;
                for(int i=0;i<8;i++){var c=mb.center+Vector3.Scale(mb.extents,new Vector3((i&1)*2-1,(i>>1&1)*2-1,(i>>2&1)*2-1));var q=m.MultiplyPoint3x4(c);if(!any){b=new Bounds(q,Vector3.zero);any=true;}else b.Encapsulate(q);}
            }
            return footprints[key]=b;
        }
        static float G(Vector3 p)=>Ground.Surface(p.x,p.z);

        // Fit a freshly spawned piece (its root at its base, yawed, scaled) to the land.
        public static void Fit(string key,string id,GameObject go)
        {
            var t=go.transform;var local=Local(key,go);var (mode,cap)=KindOf(id,local);
            if(mode==Mode.Leave||Mathf.Max(local.size.x,local.size.z)*t.localScale.x>8){Skipped++;return;}
            var at=t.position;float g0=G(at);if(Mathf.Abs(at.y-g0)>.35f){Skipped++;return;}   // (on a structure, not the land)
            float s=t.localScale.x;Vector3 lo=local.min*s,hi=local.max*s;
            if(mode==Mode.Plumb){lo.x=Mathf.Max(lo.x,-.45f);lo.z=Mathf.Max(lo.z,-.45f);hi.x=Mathf.Min(hi.x,.45f);hi.z=Mathf.Min(hi.z,.45f);}   // (a trunk's or a post's foot, not its canopy)
            var corners=new[]{new Vector3(lo.x,0,lo.z),new Vector3(hi.x,0,lo.z),new Vector3(hi.x,0,hi.z),new Vector3(lo.x,0,hi.z)};
            var yaw=Quaternion.Euler(0,t.eulerAngles.y,0);
            float Air(Quaternion rot,float y){float worst=float.MinValue;foreach(var c in corners){var w=at+rot*c;w.y=y+(rot*c).y;worst=Mathf.Max(worst,w.y-G(w));}return worst;}
            bool before=Air(yaw,at.y)>.1f;if(before)AirBefore++;
            var rot=yaw;
            if(mode==Mode.Tilt||mode==Mode.Rake) {
                var w=new Vector3[4];for(int i=0;i<4;i++){w[i]=at+yaw*corners[i];w[i].y=G(w[i]);}
                // (the plane's slopes along the piece's own axes, from opposite edges' mid-heights)
                float alongX=((w[1].y+w[2].y)-(w[0].y+w[3].y))*.5f/Mathf.Max(hi.x-lo.x,.1f),alongZ=((w[2].y+w[3].y)-(w[0].y+w[1].y))*.5f/Mathf.Max(hi.z-lo.z,.1f);
                float rz=-Mathf.Atan(alongX)*Mathf.Rad2Deg,rx=Mathf.Atan(alongZ)*Mathf.Rad2Deg;
                if(mode==Mode.Rake){var axis=Mathf.Abs(hi.x-lo.x)>=Mathf.Abs(hi.z-lo.z);if(axis)rx=0;else rz=0;}   // (only along its length)
                rz=Mathf.Clamp(rz,-cap,cap);rx=Mathf.Clamp(rx,-cap,cap);rot=yaw*Quaternion.Euler(-rx,0,rz);
                if(mode==Mode.Tilt)Tilted++;else Raked++;
            }
            // lowered until no corner is in the air (and at most 0.4 m: a deeper hollow under one corner keeps that corner up)
            float air=Air(rot,at.y);float drop=Mathf.Clamp(air+.02f,0,.4f);
            if(mode==Mode.Plumb){if(air<=.02f){Skipped++;return;}Sunk++;}
            t.SetPositionAndRotation(at+Vector3.down*drop,rot);
            if(Air(rot,at.y-drop)>.1f)AirAfter++;
        }
        public static string Summary=>$"tilted={Tilted} raked={Raked} sunk={Sunk} left={Skipped} cornerOver10cmInAir: before={AirBefore} after={AirAfter}";
        public static void Reset(){Tilted=Raked=Sunk=Skipped=AirBefore=AirAfter=0;}
    }
}
