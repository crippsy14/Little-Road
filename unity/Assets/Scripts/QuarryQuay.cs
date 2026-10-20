using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Slate Quay (docs/59): the little harbour where the slate trains end. A walled apron of made ground on the coast:
    // sea walls down into deep water with a coping kerb, a retaining wall with coping where the land behind stands
    // higher; the railway terminus (track along the quay, the station, the engine shed with its engine and a rake of
    // slate wagons); slate stacked for shipping, a quayside crane, bollards, boats, the pier and its light; a harbour row
    // of houses and the pub looking out to sea.
    public static class QuarryQuay
    {
        public static void Build(Transform parent)
        {
            if(!QuarryHills.Enabled)return;
            var root=new GameObject("Slate Quay").transform;root.SetParent(parent);
            var stone=WorldLook.Kit("Quay stone",new Color(.50f,.52f,.55f),WorldLook.Pattern.Stone,0,.12f,true);
            var dark=TrialWorld.Material("Quay iron",new Color(.18f,.19f,.21f),.4f,.4f);var rust=TrialWorld.Material("Crane iron",Toy.Hex(0x8c3b2e),.2f,.35f);
            float Y=QuarryHills.QuayY;Vector3 P(float u,float v,float y)=>QuarryHills.QuayPoint(u,v,y);
            var along=new Vector3(QuarryHills.QuayAlong.x,0,QuarryHills.QuayAlong.y);var inland=new Vector3(QuarryHills.QuayInland.x,0,QuarryHills.QuayInland.y);
            float seaYaw=Quaternion.LookRotation(-inland).eulerAngles.y,alongYaw=Quaternion.LookRotation(along).eulerAngles.y;
            Walls(root,stone);
            var boxes=new List<Matrix4x4>();var irons=new List<Matrix4x4>();var rusts=new List<Matrix4x4>();
            void Box(List<Matrix4x4> list,Vector3 c,Vector3 size,float yaw)=>list.Add(Matrix4x4.TRS(c,Quaternion.Euler(0,yaw,0),size));
            // Bollards along the sea wall.
            for(float u=-QuarryHills.QuayHalf+6;u<QuarryHills.QuayHalf-5;u+=12)Box(irons,P(u,-QuarryHills.QuaySea+1.6f,Y+.45f),new Vector3(.55f,.9f,.55f),0);
            // The railway terminus: two tracks along the quay (rails and sleepers), buffer stops, the station facing them.
            var slate=WorldLook.Kit("Stacked slate",new Color(.36f,.38f,.43f),WorldLook.Pattern.Rock,0,.15f,true);var slates=new List<Matrix4x4>();
            var sleeper=new List<Matrix4x4>();var rails=new List<Matrix4x4>();
            foreach(float v in new[]{4f,9f}) {
                for(float u=-80;u<=70;u+=.9f)Box(sleeper,P(u,v,Y+.005f),new Vector3(.25f,.02f,2.5f),alongYaw+90);
                // Rails let into the quay's setts (flush, so a car crosses them anywhere: Richard, 3 Oct, couldn't get onto the quay).
                foreach(float r in new[]{-.72f,.72f})Box(rails,P(-5,v+r,Y+.025f),new Vector3(.1f,.06f,151),alongYaw);
                Box(rusts,P(-81.5f,v,Y+.7f),new Vector3(2.6f,1.2f,.6f),alongYaw); // buffer stop
            }
            var wood=TrialWorld.Material("Sleepers",Toy.Hex(0x5a4632),0,.2f);
            QuarryFloor.Combine("Quay sleepers",sleeper,wood,root,false);QuarryFloor.Combine("Quay rails",rails,dark,root,false);
            var stationAt=P(18,16.5f,Y-.04f);   // (east of where Quay Lane comes in at u -30)Put("station",stationAt,seaYaw,Tint(0x8c9095,0xf4f1ea,0xc0392b,0x229954,0x4b5158),"SLATE QUAY",root);
            QuarryHillsTown.Spots["slatequay"]=(P(18,12.2f,Y),alongYaw);
            // The engine shed at the east end over the inner track, its engine outside, and a rake of loaded wagons.
            Put("motors",P(55,13,Y-.04f),seaYaw,Tint(0x7f868d,0x3b4047,0xc0392b,0x3b4047,0x40464d),"ENGINE SHED",root);
            if(BayviewKit.Has("train_loco"))BayviewKit.Spawn("train_loco",P(36,9,Y+.3f),alongYaw,root,null,null,true,1);
            var wagonRed=TrialWorld.Material("Slate wagon",Toy.Hex(0x8c3b2e),0,.3f);var wagons=new List<Matrix4x4>();
            // (--prop-forge: the quarry kit's slate wagons, loaded or empty, on their wheels: docs/80 round 6)
            bool forgeWorks=PropForge.Enabled&&BayviewKit.Has("slate_wagon")&&BayviewKit.Has("quay_crane");
            for(int k=0;k<8;k++){float u=-70+k*4.2f;if(forgeWorks){Put("slate_wagon",P(u,4,Y+.02f),alongYaw,new BayviewKit.Tint(Toy.Hex(0x8c3b2e),Color.white,Color.white,Color.white),null,root);continue;}Box(wagons,P(u,4,Y+.95f),new Vector3(1.5f,1.1f,3.4f),alongYaw);Box(slates,P(u,4,Y+1.7f),new Vector3(1.3f,.6f,3.0f),alongYaw);}
            QuarryFloor.Combine("Slate wagons",wagons,wagonRed,root,true);
            // Slate stacked for shipping on the seaward half: rows of stacks by grade.
            var rng=new System.Random(3301);
            // (--prop-forge: the quarry kit's racked slate stacks, not boxes: docs/80 round 6)
            for(float u=-78;u<60;u+=7.5f)for(float v=-38;v<-14;v+=6.5f){if(rng.NextDouble()<.22)continue;float h=.8f+(float)rng.NextDouble()*1.4f;if(forgeWorks&&BayviewKit.Has("slate_stack")){Put("slate_stack",P(u+(float)rng.NextDouble(),v,Y+.02f),alongYaw+(float)rng.NextDouble()*6-3,null,null,root);continue;}Box(slates,P(u+(float)rng.NextDouble(),v,Y+h*.5f),new Vector3(2.4f,h,1.4f),alongYaw+(float)rng.NextDouble()*6-3);}
            QuarryFloor.Combine("Slate stacks",slates,slate,root,true);
            // A quayside crane: a mast with a jib out over the water.
            var cr=P(20,-QuarryHills.QuaySea+6,Y);
            if(forgeWorks)Debug.Log($"LITTLE_ROAD_SPOT crane {cr} wagons {P(-55,4,Y)} sea_yaw {seaYaw:0}");if(forgeWorks)Put("quay_crane",cr,seaYaw,new BayviewKit.Tint(Toy.Hex(0x8c3b2e),Toy.Hex(0x8c3b2e),Toy.Hex(0x8c3b2e),Toy.Hex(0x8c3b2e)),null,root);else{   // (the quarry kit's hand crane, its jib out over the water)
            Box(rusts,cr+Vector3.up*6,new Vector3(1.2f,12,1.2f),seaYaw);Box(rusts,cr+Vector3.up*.6f,new Vector3(4,1.2f,4),seaYaw);
            var jibFoot=cr+Vector3.up*10;var jibTip=cr+Vector3.up*15-inland*13;rusts.Add(Matrix4x4.TRS((jibFoot+jibTip)*.5f,Quaternion.LookRotation(jibTip-jibFoot),new Vector3(.7f,.7f,Vector3.Distance(jibFoot,jibTip))));
            irons.Add(Matrix4x4.TRS(jibTip+Vector3.down*4,Quaternion.identity,new Vector3(.06f,8,.06f)));}
            QuarryFloor.Combine("Quay ironwork",irons,dark,root,true);QuarryFloor.Combine("Crane",rusts,rust,root,true);
            // Boats moored off the wall, the pier, and its light.
            for(int k=0;k<3;k++){var b=P(-60+k*38,-QuarryHills.QuaySea-6-k%2*5,CoastalLandscape.SeaLevel);Put("boat",b,alongYaw+(k%2==0?0:180),null,null,root,false);}
            Put("pier",P(-6,-QuarryHills.QuaySea+.5f,Y+.02f),seaYaw,null,null,root);
            if(BayviewKit.Has("lighthouse"))BayviewKit.Spawn("lighthouse",P(QuarryHills.QuayHalf-6,-QuarryHills.QuaySea+5,Y),0,root,null,null,true,.32f);
            // Lamps along the seafront with bunting between them; benches looking out.
            var lamps=new List<Vector3>();
            for(float u=-80;u<=80;u+=20){var l=P(u,-QuarryHills.QuaySea+3.5f,Y);if(Put("lamp_post",l,0,Tint(0xffffff,0x1f3a93,0xffffff,0xffffff,0xffffff),null,root,false)!=null)lamps.Add(l);}
            for(int i=0;i+1<lamps.Count;i++)HillcrestTown.BuntingLine(root,lamps[i]+Vector3.up*4.4f,lamps[i+1]+Vector3.up*4.4f,.8f);
            foreach(float u in new[]{-50f,-10f,30f})Put("bench",P(u,-QuarryHills.QuaySea+5.5f,Y),seaYaw+180,null,null,root);
            // The harbour row behind the retaining wall, fronts to the sea: the harbour master, Y Llong (the pub), the chandler.
            string[] names={"HARBOUR MASTER","Y LLONG","CHANDLER","FISH","CAFFI"};int name=0;
            for(float u=-86;u<86;) {
                string id=new[]{"terrace_a","terrace_b","terrace_d","terrace_f","terrace_g"}[rng.Next(5)];float w=5.4f;
                var c=P(u+w*.5f,QuarryHills.QuayLand+3+4+3.5f,0);float g=ValleyField.Height(c.x,c.z);c.y=g+.02f;
                if(ValleyField.RoadDistance(c.x,c.z)>9&&Mathf.Abs(ValleyField.Height(c.x+inland.x*4,c.z+inland.z*4)-g)<4) {
                    string sign=id=="terrace_b"?names[name++%names.Length]:null;
                    Put(id,c,seaYaw,RandomStone(rng),sign,root);
                }
                u+=w+.05f;
            }
            Debug.Log($"LITTLE_ROAD_SLATE_QUAY centre=({QuarryHills.QuayCentre.x:0},{QuarryHills.QuayCentre.y:0}) inland=({QuarryHills.QuayInland.x:0.00},{QuarryHills.QuayInland.y:0.00})");
        }
        static BayviewKit.Tint RandomStone(System.Random rng)
        {
            int[] walls={0x7f868d,0x8c9095,0x969089,0xe9e3d3,0xd6dfe4,0xf0dc9a,0xa9c9d9};int[] doors={0xc0392b,0x2e86c1,0x229954,0xf1c40f,0x1f3a93};
            int d=doors[rng.Next(doors.Length)];return Tint(walls[rng.Next(walls.Length)],0xf4f1ea,doors[rng.Next(doors.Length)],d,0x4b5158);
        }
        static BayviewKit.Tint Tint(int wall,int shutter,int awning,int door,int roof){var t=new BayviewKit.Tint(Toy.Hex(wall),Toy.Hex(shutter),Toy.Hex(awning),Toy.Hex(door));t.roof=Toy.Hex(roof);return t;}
        static GameObject Put(string id,Vector3 p,float yaw,BayviewKit.Tint tint,string sign,Transform root,bool solid=true)=>BayviewKit.Has(id)?BayviewKit.Spawn(id,p,yaw,root,tint,sign,solid,1):null;
        // The apron's walls, walked round its edge every 2 m: on the sea side a wall from 16 m below the water to a kerb
        // 0.35 m above the apron with a 3 m coping inward (over the grid's step); on the land side, where the ground behind
        // stands higher, a retaining wall to just above it with a 6 m coping back over the step. Open where a road comes in.
        static void Walls(Transform root,Material stone)
        {
            var v=new List<Vector3>();var t=new List<int>();float Y=QuarryHills.QuayY,H=QuarryHills.QuayHalf,S=QuarryHills.QuaySea,L=QuarryHills.QuayLand;
            // Two-sided quads get their own vertices per side (shared ones averaged the normals to nothing: black walls).
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool both){int s=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);t.AddRange(new[]{s,s+1,s+2,s,s+2,s+3});
                if(both){s=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);t.AddRange(new[]{s,s+2,s+1,s,s+3,s+2});}}
            // Perimeter: (u,v) corners anticlockwise from the sea-west corner, outward normal per side.
            var sides=new[]{(new Vector2(-H,-S),new Vector2(H,-S),new Vector2(0,-1)),(new Vector2(H,-S),new Vector2(H,L),new Vector2(1,0)),
                            (new Vector2(H,L),new Vector2(-H,L),new Vector2(0,1)),(new Vector2(-H,L),new Vector2(-H,-S),new Vector2(-1,0))};
            foreach(var (a,b,n) in sides) {
                float len=Vector2.Distance(a,b);int steps=Mathf.CeilToInt(len/2);
                for(int i=0;i<steps;i++) {
                    Vector2 p0=Vector2.Lerp(a,b,i/(float)steps),p1=Vector2.Lerp(a,b,(i+1f)/steps),mid=(p0+p1)*.5f;
                    bool sea=mid.y<0;
                    // Land side walls stand 3 m out (the apron runs on under them); the sea walls stand on the edge.
                    float out0=sea?.6f:3.6f;
                    Vector3 W(Vector2 q,float off,float y){var w=QuarryHills.QuayPoint(q.x+n.x*off,q.y+n.y*off,y);return w;}
                    var probe=W(mid,out0+5,0);float g=ValleyField.Height(probe.x,probe.z);
                    var at=W(mid,out0,0);if(ValleyField.RoadDistance(at.x,at.z)<6)continue;
                    if(sea||g<Y+.4f) {
                        float bottom=sea?CoastalLandscape.SeaLevel-16:g-1,top=Y+.35f;
                        Quad(W(p0,out0,bottom),W(p0,out0,top),W(p1,out0,top),W(p1,out0,bottom),true);
                        Quad(W(p0,out0,top),W(p0,out0-3.4f,top),W(p1,out0-3.4f,top),W(p1,out0,top),true);
                    } else {
                        float g0=ValleyField.Height(W(p0,out0+6,0).x,W(p0,out0+6,0).z),g1=ValleyField.Height(W(p1,out0+6,0).x,W(p1,out0+6,0).z);
                        float t0=Mathf.Max(Y+.5f,g0+.3f),t1=Mathf.Max(Y+.5f,g1+.3f);
                        Quad(W(p0,out0,Y-.4f),W(p0,out0,t0),W(p1,out0,t1),W(p1,out0,Y-.4f),true);
                        Quad(W(p0,out0,t0),W(p0,out0+6,t0),W(p1,out0+6,t1),W(p1,out0,t1),true);
                    }
                }
            }
            var mesh=new Mesh{name="Quay walls",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject("Quay walls",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(root);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=stone;go.GetComponent<MeshCollider>().sharedMesh=mesh;go.tag="Respawn";
        }
    }
}
