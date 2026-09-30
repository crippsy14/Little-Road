using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LittleRoad
{
    // Finds kit pieces that don't sit on the world (--world-review --placement-audit): for each placed
    // piece, the ground under its footprint (anything solid that isn't the piece itself) is compared with
    // the bottom of its meshes. Floating: the ground is more than 0.3 m below it everywhere under the
    // footprint. Sunk: more than 1.2 m of the piece is below the ground at its centre. Pieces meant to be
    // off the ground (boats, balloons, bunting, the zip trolley) are skipped. Writes placement-audit.txt.
    public static class PlacementAudit
    {
        static readonly HashSet<string> Airborne=new(){"boat","balloon","zip_trolley","wind_turbine","lighthouse","pier","harbour_steps","sea_wall","promenade_rail","headland_gate","rail_gate","tunnel_portal"};
        public static string Run()
        {
            Physics.SyncTransforms();var floating=new List<string>();var sunk=new List<string>();int checkedCount=0;
            foreach(var go in BayviewKit.Placed) {
                if(go==null||!go.activeInHierarchy||Airborne.Contains(go.name))continue;
                var renderers=go.GetComponentsInChildren<Renderer>();if(renderers.Length==0)continue;
                var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);if(b.size.y<.05f)continue;
                checkedCount++;float bottom=b.min.y;float highestGround=float.MinValue;var ex=b.extents*.7f;
                // Ground: the highest solid surface at or just below the piece's base, at its centre and corners.
                foreach(var o in new[]{Vector3.zero,new Vector3(ex.x,0,ex.z),new Vector3(-ex.x,0,ex.z),new Vector3(ex.x,0,-ex.z),new Vector3(-ex.x,0,-ex.z)}) {
                    // Cast from above the piece (a start inside a hillside finds nothing), keeping surfaces no
                    // higher than the piece's top: the ground it stands on, not a roof it sits under.
                    var from=new Vector3(b.center.x+o.x,b.max.y+.5f,b.center.z+o.z);
                    foreach(var hit in Physics.RaycastAll(from,Vector3.down,b.size.y+60)) {
                        if(hit.collider.isTrigger||hit.collider.transform.IsChildOf(go.transform)||hit.collider.GetComponentInParent<RoadCar>()!=null)continue;
                        if(hit.point.y<=b.max.y)highestGround=Mathf.Max(highestGround,Mathf.Min(hit.point.y,bottom+5));
                    }
                }
                if(highestGround==float.MinValue)continue; // over open water
                float centreGround=b.center.z<-380?ValleyField.Height(b.center.x,b.center.z):float.MinValue;
                string where=$"{go.name} at ({b.center.x:0},{bottom:0.0},{b.center.z:0})";
                if(bottom-highestGround>.3f)floating.Add($"{where} gap {bottom-highestGround:0.00} m");
                else if(centreGround!=float.MinValue&&centreGround-bottom>1.2f&&centreGround<b.max.y)sunk.Add($"{where} buried {centreGround-bottom:0.00} m");
            }
            var sb=new StringBuilder();
            sb.AppendLine($"checked={checkedCount} floating={floating.Count} sunk={sunk.Count}");
            sb.AppendLine("floating (gap under the whole footprint):");foreach(var f in floating.OrderBy(s=>s))sb.AppendLine("  "+f);
            sb.AppendLine("sunk (centre below ground):");foreach(var s in sunk.OrderBy(s=>s))sb.AppendLine("  "+s);
            return sb.ToString();
        }
        // Road edges you can't drive back up (--edge-audit): along every valley road, steps out from the
        // asphalt edge to 4 m beyond it, sampling the highest solid surface; a rise of more than 12 cm between
        // neighbouring samples (10 cm apart) is a lip a car's tyre catches on. Road ends are checked too.
        // Every road the car can drive (Bayview's loop, the valley roads, the raceway); each side's edge is found where
        // the tarmac collider (RoadSurface) ends, so no road's width has to be known.
        public static string Edges(Circuit bayview=null)
        {
            Physics.SyncTransforms();var sb=new StringBuilder();int lips=0,checks=0;
            float Top(Vector3 p){float best=float.MinValue;foreach(var h in Physics.RaycastAll(p+Vector3.up*4,Vector3.down,10))if(!h.collider.isTrigger&&h.collider.GetComponentInParent<RoadCar>()==null&&!h.collider.CompareTag("Respawn"))best=Mathf.Max(best,h.point.y);return best==float.MinValue?float.NaN:best;}   // NaN: nothing solid (open water)
            bool Tarmac(Vector3 p){float best=float.MinValue;bool road=false;foreach(var h in Physics.RaycastAll(p+Vector3.up*4,Vector3.down,10))if(!h.collider.isTrigger&&h.collider.GetComponentInParent<RoadCar>()==null&&h.point.y>best){best=h.point.y;road=h.collider.GetComponentInParent<RoadSurface>()!=null;}return road;}
            var found=new List<string>();var bankList=new List<string>();int banks=0;
            var lines=new List<(string name,Vector3[] line,float half)>();
            if(bayview!=null)lines.Add(("Bayview road",bayview.points,Circuit.Width*.5f));
            foreach(var road in HillcrestRoute.All())lines.Add((road.name,road.line,road.width*.5f));
            lines.Add(("Hillcrest Raceway",HillcrestRoute.Circuit,8));
            foreach(var (name,line,guess) in lines) {
                float run=0;
                for(int i=1;i<line.Length-1;i+=2) {
                    run+=Vector3.Distance(line[i],line[i-1])+(i>1?Vector3.Distance(line[i-1],line[i-2]):0);
                    var f=line[i+1]-line[i-1];f.y=0;f.Normalize();var r=Vector3.Cross(Vector3.up,f);
                    foreach(int side in new[]{-1,1}) {
                        // The edge: where the tarmac stops (the road's own half-width if it isn't tarmac here, a bridge deck).
                        float half=guess;if(Tarmac(line[i])){for(float o=1;o<guess+6;o+=.1f)if(!Tarmac(line[i]+r*side*o)){half=o;break;}}
                        checks++;float prev=Top(line[i]+r*side*(half-.3f)),worst=0;float at=0;
                        for(float o=half-.2f;o<=half+4;o+=.1f){float h=Top(line[i]+r*side*o);if(float.IsNaN(h))break;float rise=prev-h;if(rise>worst){worst=rise;at=o;}prev=h;}
                        if(worst>.12f&&worst<2){lips++;found.Add($"  {name} run {run:0} side {(side>0?"R":"L")} lip {worst:0.00} m at {at-half:0.0} m past the edge ({line[i].x:0},{line[i].y:0.0},{line[i].z:0})");}
                        // Banks (Richard, 30 Sep: roads by the railway you can't get back onto): out to 12 m, the
                        // steepest climb back towards the road over any metre, where the climb is at least 40 cm.
                        // (Drops of 2 m and more are bridges and sea walls, which have parapets.)
                        if(worst<2) {
                            var list=new List<float>();for(int k=0;k<49;k++){float h=Top(line[i]+r*side*(half+k*.25f));if(float.IsNaN(h))break;list.Add(h);}var hs=list.ToArray();
                            float steep=0,rise=0;for(int k=0;k+4<hs.Length;k++){float s=hs[k]-hs[k+4];if(s>steep)steep=s;}
                            for(int k=1;k<hs.Length;k++)if(hs[k-1]-hs[k]>.05f)rise+=hs[k-1]-hs[k];
                            if(steep>.55f&&rise>.4f){banks++;bankList.Add($"  {name} run {run:0} side {(side>0?"R":"L")} bank {Mathf.Atan(steep)*Mathf.Rad2Deg:0}° climbing {rise:0.0} m ({line[i].x:0},{line[i].y:0.0},{line[i].z:0})");}
                        }
                    }
                }
                // Road ends: straight on past the last point (loops have none).
                if(Vector3.Distance(line[0],line[^1])>12)foreach(bool start in new[]{true,false}) {
                    var e=start?line[0]:line[^1];var d=start?line[0]-line[1]:line[^1]-line[^2];d.y=0;d.Normalize();float prev=Top(e-d*.3f),worst=0;
                    for(float o=0;o<=4;o+=.1f){float h=Top(e+d*o);if(float.IsNaN(h))break;worst=Mathf.Max(worst,prev-h);prev=h;}
                    checks++;if(worst>.12f){lips++;found.Add($"  {name} {(start?"start":"end")} lip {worst:0.00} m ({e.x:0},{e.z:0})");}
                }
            }
            sb.AppendLine($"edge checks={checks} lips={lips} banks={banks}");foreach(var l in found)sb.AppendLine(l);
            sb.AppendLine("banks too steep to drive back up:");foreach(var l in bankList)sb.AppendLine(l);
            // Roads beside the railways: every 8 m of road within 25 m of a line, with both heights.
            sb.AppendLine("roads within 25 m of a railway:");
            foreach(var (name,line,_) in lines)for(int i=0;i<line.Length;i+=4) {
                var p=line[i];float best=25;Vector3 near=default;
                foreach(var rail in new[]{HillcrestRoute.Rail.line,HillcrestRoute.SeaRail.line})foreach(var q in rail){float d=Vector2.Distance(new(p.x,p.z),new(q.x,q.z));if(d<best){best=d;near=q;}}
                if(best<25)sb.AppendLine($"  {name} at ({p.x:0},{p.y:0.0},{p.z:0}) rail {best:0} m away at ({near.x:0},{near.y:0.0},{near.z:0})");
            }
            return sb.ToString();
        }
    }
}
