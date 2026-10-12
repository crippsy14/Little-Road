using System;
using UnityEngine;

namespace LittleRoad
{
    // The Tip Leap (docs/59's "things to do": Forza Horizon's danger-sign jumps; Claude's addition, 5 Oct, doc 63). The
    // Blondin drops you on the big slate tip, 51 m above the quarry floor; run 270 m down its ridge, hit the slate kicker
    // at the end, and fly. The distance is measured from the kicker's lip to where you touch down; stars at 45, 75 and 105 m
    // pay once each, and your best is kept (the speed traps' record). A toy-physics landing: near the ground the fall is
    // eased to 9 m/s, so you land and roll out instead of folding the car.
    public sealed class TipLeap : MonoBehaviour
    {
        public const string Id="tip_leap";
        public static bool Known(string id)=>id==Id;
        public static readonly float[] Marks={45,75,105};public static readonly int[] Prizes={75,150,250};
        public static Vector3 Lip,Dir,RunUp;public static bool Ready;
        // Where the jump is: the big tip's floor-side end, and its ridge behind it.
        public static void Plan()
        {
            Ready=false;if(!QuarryHills.Enabled)return;
            var t=QuarryHills.Tips[0];var d=(t.a-t.b).normalized;
            // Walk out along the ridge until the top falls away: the lip stands 7 m short of that edge.
            // (The ground itself: the quarry floor's levelling cuts the tip's end back.) From well up the ridge, outwards.
            float s=-220;while(s<120&&ValleyField.Height(t.a.x+d.x*s,t.a.y+d.y*s)>t.top-12)s+=1;s-=7;
            var lip=t.a+d*s;Lip=new Vector3(lip.x,ValleyField.Height(lip.x,lip.y),lip.y);Dir=new Vector3(d.x,0,d.y);
            var run=lip-d*150;RunUp=new Vector3(run.x,ValleyField.Height(run.x,run.y),run.y);Ready=true;
            Debug.Log($"LITTLE_ROAD_TIP_LEAP lip=({Lip.x:0},{Lip.y:0},{Lip.z:0}) drop={Lip.y-QuarryHills.FloorY:0}m");
        }
        // The kicker (a wedge 10 m long rising 1.9 m, about 11 degrees, with tarmac grip) and the danger signs.
        public static void Build(Transform parent)
        {
            if(!Ready)return;var root=new GameObject("Tip Leap").transform;root.SetParent(parent);
            const float L=10,W=7,H=1.9f;var f=Dir;var r=Vector3.Cross(Vector3.up,f);var back=Lip-f*L;
            float gb=ValleyField.Height(back.x,back.z),gl=Lip.y;var bb=new Vector3(back.x,gb-.05f,back.z);var lb=new Vector3(Lip.x,gl-.05f,Lip.z);var top=new Vector3(Lip.x,gl+H,Lip.z);
            Vector3[] v={bb-r*W/2,bb+r*W/2,top-r*W/2,top+r*W/2,lb-r*W/2,lb+r*W/2,lb-r*W/2-Vector3.up*3,lb+r*W/2-Vector3.up*3};
            int[] tri={0,2,1,1,2,3, 2,6,3,3,6,7, 0,4,2,2,4,6, 1,3,5,3,7,5};
            var mesh=new Mesh{name="Tip Leap kicker",vertices=v,triangles=tri};mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject("Tip Leap kicker",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(root);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=WorldLook.Kit("Tip Leap slate",new Color(.42f,.44f,.48f),WorldLook.Pattern.Stone,0,.12f,true);
            go.GetComponent<MeshCollider>().sharedMesh=mesh;go.AddComponent<RoadSurface>();
            // Danger boards either side of the run-in, facing the run-up, and chevrons at the edge.
            if(BayviewKit.Has("ad_board"))foreach(int side in new[]{-1,1}){var p=Lip-f*16+r*side*7;p.y=ValleyField.Height(p.x,p.z)-.02f;
                BayviewKit.Spawn("ad_board",p,Quaternion.LookRotation(-f).eulerAngles.y,root,new BayviewKit.Tint(Toy.Hex(0xf1c40f),Toy.Hex(0x111111),Color.white,Color.white),"DANGER · TIP LEAP");}
            if(BayviewKit.Has("chevron_sign"))foreach(float x in new[]{-6f,6f}){var p=Lip+r*x+f*2;p.y=ValleyField.Height(p.x,p.z);BayviewKit.Spawn("chevron_sign",p,Quaternion.LookRotation(-f).eulerAngles.y,root);}
        }

        RoadCar player;Func<AdventureStore> store;Action<string> notify;Action<string,string> card;
        float armedAt=-1,airborneAt=-1;Vector3 takeoff;public float Last;
        public void Setup(RoadCar car,Func<AdventureStore> s,Action<string> n,Action<string,string> c){player=car;store=s;notify=n;card=c;}
        float Clearance(Vector3 p){float d=60;foreach(var h in Physics.RaycastAll(p+Vector3.up*.5f,Vector3.down,60))if(!h.collider.isTrigger&&h.collider.GetComponentInParent<RoadCar>()==null)d=Mathf.Min(d,h.distance-.5f);return d;}   // (the nearest: RaycastAll's order is arbitrary)
        void FixedUpdate()
        {
            if(!Ready||player==null)return;var p=player.body.position;
            // Armed when you pass over the kicker's lip going out at a good lick.
            if(armedAt<0){var rel=p-Lip;rel.y=0;if(rel.magnitude<5&&Vector3.Dot(player.body.linearVelocity,Dir)>8){armedAt=Time.time;takeoff=Lip;airborneAt=-1;}return;}
            float clear=Clearance(p);
            if(airborneAt<0){if(clear>1.2f)airborneAt=Time.time;else if(Time.time-armedAt>1.5f)armedAt=-1;return;}
            // The landing assist: near the ground, the fall eases to 9 m/s.
            var v=player.body.linearVelocity;if(clear<4&&v.y<-9){v.y=-9;player.body.linearVelocity=v;}
            if(clear<.6f&&Time.time-airborneAt>.4f) {
                armedAt=-1;var d=p-takeoff;d.y=0;Last=d.magnitude;int stars=0;for(int i=0;i<Marks.Length;i++)if(Last>=Marks[i])stars=i+1;
                int paid=store().RecordTrap(Id,Mathf.Round(Last*10)/10,stars,Prizes);var best=SpeedTraps.Record(store().Data,Id);
                string starText=new string('★',stars)+new string('☆',3-stars);
                card?.Invoke($"{Last:0} m",$"{starText}   best {best?.best??Last:0} m"+(paid>0?$"   +{paid} credits":"")+(stars<3?$"   next star at {Marks[stars]:0} m":""));
                notify?.Invoke(stars==3?"Tip Leap · three stars! The quarrymen are talking about it already.":$"Tip Leap · {Last:0} m");
                Debug.Log($"LITTLE_ROAD_TIP_LEAP landed distance={Last:0.0} stars={stars} paid={paid}");
            }
            else if(Time.time-airborneAt>12)armedAt=-1;
        }
    }
}
