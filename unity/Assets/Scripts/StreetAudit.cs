using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LittleRoad
{
    // Street audit (part of --area-audit, docs/80): the street reviews' commonest findings, measured instead of looked for.
    //   - Hidden signs: each shop name is seen from the street, 7 m out at eye height, straight on and 30 degrees either
    //     side. A sight line to the top of the lettering that meets anything on the way (a balcony, a lamp, a car, a canopy)
    //     is blocked; a sign with two of its three lines blocked is hidden.
    //   - Tipped furniture: a bench, bin, lamp or the like whose model leans more than 25 degrees off upright.
    // Each finding comes with a --view-shots camera (Shots), so only the flagged places need photographing.
    public static class StreetAudit
    {
        public static readonly List<Vector3> HiddenSigns=new(),Tipped=new();
        public static readonly List<string> Lines=new(),Shots=new(),Samples=new();
        public static int SignsChecked;
        static readonly string[] Upright={"bench","litter_bin","lamp_post","picnic_table","map_board","flag_pole","telescope","phone_box","pillar_box","bollard","sandwich_board","planter","bike_rack","parasol","market_stall","kiosk","fountain"};

        public static void Run()
        {
            HiddenSigns.Clear();Tipped.Clear();Lines.Clear();Shots.Clear();Samples.Clear();SignsChecked=0;
            var signs=Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Sign / ")&&t.transform.parent!=null&&t.transform.parent.name.StartsWith("SignAnchor_shop")).ToList();
            // Mesh colliders for the moment on everything near a sign (balconies, lamps and canopies have none of their own).
            // Combined meshes (over 40 m: walls, paving) are left out; they're too big to cook and sit below the lettering.
            var cells=new HashSet<(int,int)>();foreach(var t in signs){var p=t.transform.position;for(int i=-1;i<=1;i++)for(int j=-1;j<=1;j++)cells.Add(((int)Mathf.Floor(p.x/12)+i,(int)Mathf.Floor(p.z/12)+j));}
            var temp=new List<MeshCollider>();
            foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)) {
                var r=mf.GetComponent<MeshRenderer>();if(r==null||!r.enabled||mf.sharedMesh==null||mf.GetComponent<Collider>()!=null||mf.name=="LOD1")continue;
                var b=r.bounds;if(b.size.x>40||b.size.z>40)continue;var c=b.center;if(!cells.Contains(((int)Mathf.Floor(c.x/12),(int)Mathf.Floor(c.z/12))))continue;
                var mc=mf.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=mf.sharedMesh;temp.Add(mc);
            }
            Physics.SyncTransforms();
            foreach(var t in signs) {
                var r=t.GetComponent<Renderer>();if(r==null)continue;SignsChecked++;
                var b=r.bounds;var top=b.center+Vector3.up*b.extents.y*.7f;
                var face=-t.transform.forward;face.y=0;if(face.sqrMagnitude<1e-4f)continue;face.Normalize();   // (the lettering reads from in front of it)
                int blocked=0;Vector3 eye0=default;
                foreach(float a in new[]{0f,-30f,30f}) {
                    var d=Quaternion.Euler(0,a,0)*face;var eye=top+d*7;eye.y=Stand(eye)+1.4f;if(a==0)eye0=eye;
                    var ray=top-eye;float len=ray.magnitude-.3f;
                    if(Physics.RaycastAll(eye,ray/ray.magnitude,len).Any(h=>!h.collider.isTrigger&&!h.collider.transform.IsChildOf(t.transform)))blocked++;
                }
                if(blocked<2&&SignsChecked%40==1)Samples.Add(Shot($"sample-{SignsChecked:000}-{Safe(t.text)}",eye0,b.center));   // (a few that pass, to check the check)
                if(blocked>=2){HiddenSigns.Add(b.center);string words=t.text.Replace(';',' ').Replace(':',' ');
                    Lines.Add($"  sign {words} at {b.center.x:0},{b.center.y:0},{b.center.z:0}: {blocked} of 3 sight lines blocked");
                    if(Shots.Count<24)Shots.Add(Shot($"sign-{HiddenSigns.Count:00}-{Safe(words)}",eye0,b.center));}
            }
            foreach(var mc in temp)Object.Destroy(mc);
            // Tipped furniture: every rendered part of the piece, not just its root (a Forge stand-in turns a child).
            foreach(var go in BayviewKit.Placed) {
                if(go==null||!Upright.Contains(go.name))continue;
                float worst=0;foreach(var r in go.GetComponentsInChildren<MeshRenderer>())worst=Mathf.Max(worst,Vector3.Angle(r.transform.up,Vector3.up));
                if(worst>25){var p=go.transform.position;Tipped.Add(p);Lines.Add($"  {go.name} at {p.x:0.0},{p.y:0.0},{p.z:0.0} leans {worst:0} degrees");
                    if(Shots.Count<36)Shots.Add(Shot($"tipped-{Tipped.Count:00}-{go.name}",p+new Vector3(3.5f,1.6f,3.5f),p+Vector3.up*.4f));}
            }
            Debug.Log($"LITTLE_ROAD_STREET_AUDIT signs={SignsChecked} hidden={HiddenSigns.Count} tipped={Tipped.Count}");
            if(Shots.Count>0)Debug.Log("LITTLE_ROAD_STREET_AUDIT_SHOTS "+string.Join(";",Shots));
            if(Samples.Count>0)Debug.Log("LITTLE_ROAD_STREET_AUDIT_SAMPLES "+string.Join(";",Samples));
        }
        // (where someone would stand: the highest solid surface below the sign's height, a quay deck or pavement, never the
        //  sea floor; the first audit put eyes under the Slate Quay's deck)
        static float Stand(Vector3 p)
        {
            float best=Mathf.Max(Ground.Surface(p.x,p.z),0);
            foreach(var h in Physics.RaycastAll(new Vector3(p.x,p.y+.5f,p.z),Vector3.down,60))if(!h.collider.isTrigger&&h.collider.attachedRigidbody==null)best=Mathf.Max(best,h.point.y);
            return best;
        }
        static string Safe(string s)=>new string(s.ToLowerInvariant().Select(ch=>char.IsLetterOrDigit(ch)?ch:'-').ToArray());
        static string Shot(string name,Vector3 from,Vector3 at)=>System.FormattableString.Invariant($"{name}:{from.x:0.00},{from.y:0.00},{from.z:0.00}>{at.x:0.00},{at.y:0.00},{at.z:0.00}");
    }
}
