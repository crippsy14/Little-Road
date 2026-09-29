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
        public static string Edges()
        {
            Physics.SyncTransforms();var sb=new StringBuilder();int lips=0,checks=0;
            float Top(Vector3 p){float best=float.MinValue;foreach(var h in Physics.RaycastAll(p+Vector3.up*4,Vector3.down,10))if(!h.collider.isTrigger&&h.collider.GetComponentInParent<RoadCar>()==null&&!h.collider.CompareTag("Respawn"))best=Mathf.Max(best,h.point.y);return best==float.MinValue?ValleyField.Height(p.x,p.z):best;}
            var found=new List<string>();
            foreach(var road in HillcrestRoute.All()) {
                var line=road.line;float half=road.width*.5f;
                for(int i=1;i<line.Length-1;i+=2) {
                    var f=line[i+1]-line[i-1];f.y=0;f.Normalize();var r=Vector3.Cross(Vector3.up,f);
                    foreach(int side in new[]{-1,1}) {
                        checks++;float prev=Top(line[i]+r*side*(half-.3f)),worst=0;float at=0;
                        for(float o=half-.2f;o<=half+4;o+=.1f){float h=Top(line[i]+r*side*o);float rise=prev-h;if(rise>worst){worst=rise;at=o;}prev=h;}
                        if(worst>.12f){lips++;found.Add($"  {road.name} run {road.run[i]:0} side {(side>0?"R":"L")} lip {worst:0.00} m at {at-half:0.0} m past the edge ({line[i].x:0},{line[i].z:0})");}
                    }
                }
                // Road ends: straight on past the last point.
                foreach(bool start in new[]{true,false}) {
                    var e=start?line[0]:line[^1];var d=start?line[0]-line[1]:line[^1]-line[^2];d.y=0;d.Normalize();float prev=Top(e-d*.3f),worst=0;
                    for(float o=0;o<=4;o+=.1f){float h=Top(e+d*o);worst=Mathf.Max(worst,prev-h);prev=h;}
                    checks++;if(worst>.12f){lips++;found.Add($"  {road.name} {(start?"start":"end")} lip {worst:0.00} m ({e.x:0},{e.z:0})");}
                }
            }
            sb.AppendLine($"edge checks={checks} lips={lips}");foreach(var l in found)sb.AppendLine(l);return sb.ToString();
        }
    }
}
