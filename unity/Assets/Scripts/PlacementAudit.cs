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
        static readonly HashSet<string> Airborne=new(){"boat","rabelo","cais_pontao","cais_ponte","vila_estendal","vila_lanterna","vila_oratorio","Fireworks barge","balloon","zip_trolley","wind_turbine","lighthouse","pier","harbour_steps","sea_wall","promenade_rail","headland_gate","rail_gate","tunnel_portal"};
        // The last Run(): floating pieces above the sea floor (sunken sea rocks are out of sight) and loose bunting ends.
        public static readonly List<Vector3> FloatingAt=new(),LooseBunting=new();
        // Before static batching (CoastalLandscape): planting (bushes, trees, rocks, flowers) left hanging more than
        // 0.3 m over the ground under its whole footprint (a slope edge, a road ribbon's edge) is lowered onto it.
        // Any area gets this for free; the audit then only finds pieces placed afloat on purpose or by structure kits.
        static readonly string[] Settleable={"bush","flower_bush","olive_tree","tree_","rock_","reeds","grass_tuft","hay_bale"};
        public static int Settle()
        {
            int moved=0;
            foreach(var go in BayviewKit.Placed)if(SettleOne(go))moved++;
            if(moved>0)Physics.SyncTransforms();
            Debug.Log("LITTLE_ROAD_SETTLED_PLANTING moved="+moved);return moved;
        }
        // One piece onto the ground beneath it (also each streamed tree as it's built: WorldTiles).
        public static bool SettleOne(GameObject go)
        {
            {
                if(go==null||!(Settleable.Any(id=>id.EndsWith("_")?go.name.StartsWith(id):go.name==id)||PropForge.Enabled&&(go.name=="vine_row"||go.name=="lavender_row")))return false;   // (--prop-forge: crop rows too, the Hillcrest vine row hung over a cutting's lip, docs/80)
                var renderers=go.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return false;
                var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
                if(b.min.y<CoastalLandscape.SeaLevel-1)return false; // sea rocks stand on the seabed by their own rule
                // Quick test first: ground within 0.3 m under the centre (nearly every piece) needs nothing more.
                bool near=false;foreach(var h in Physics.RaycastAll(new Vector3(b.center.x,b.min.y+.2f,b.center.z),Vector3.down,.5f))if(!h.collider.isTrigger&&!h.collider.transform.IsChildOf(go.transform)){near=true;break;}
                if(near)return false;
                float ground=float.MinValue;var ex=b.extents*.7f;
                foreach(var o in new[]{Vector3.zero,new Vector3(ex.x,0,ex.z),new Vector3(-ex.x,0,ex.z),new Vector3(ex.x,0,-ex.z),new Vector3(-ex.x,0,-ex.z)})
                    foreach(var hit in Physics.RaycastAll(new Vector3(b.center.x+o.x,b.max.y+.5f,b.center.z+o.z),Vector3.down,b.size.y+60)) {
                        // (not the deep sea floor, 50 m down under a piece set into a hillside: the town at load settled before the
                        //  sea was made, a district built later after it, and its crop rows fell to it, --near-start)
                        if(hit.collider.isTrigger||hit.collider.transform.IsChildOf(go.transform)||hit.collider.GetComponentInParent<RoadCar>()!=null||hit.collider.name=="Deep sea floor")continue;
                        if(hit.point.y<=b.max.y)ground=Mathf.Max(ground,Mathf.Min(hit.point.y,b.min.y+5));
                    }
                if(ground==float.MinValue||b.min.y-ground<=.3f)return false;
                go.transform.position+=Vector3.down*(b.min.y-ground+.05f);return true;
            }
        }
        public static string Run()
        {
            Physics.SyncTransforms();var floating=new List<string>();var sunk=new List<string>();int checkedCount=0;FloatingAt.Clear();LooseBunting.Clear();
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
                if(bottom-highestGround>.3f){floating.Add($"{where} gap {bottom-highestGround:0.00} m");if(bottom>CoastalLandscape.SeaLevel-1)FloatingAt.Add(b.center);}
                else if(centreGround!=float.MinValue&&centreGround-bottom>1.2f&&centreGround<b.max.y)sunk.Add($"{where} buried {centreGround-bottom:0.00} m");
            }
            // Structures that aren't kit pieces (guardrail posts, corner walls' pieces): the same test on each post.
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) {
                if(t.name!="Guardrail post"||!t.gameObject.activeInHierarchy)continue;var r=t.GetComponent<Renderer>();if(r==null)continue;var b=r.bounds;checkedCount++;
                float ground=float.MinValue;foreach(var hit in Physics.RaycastAll(new Vector3(b.center.x,b.max.y+.5f,b.center.z),Vector3.down,b.size.y+60))
                    if(!hit.collider.isTrigger&&hit.collider.transform!=t&&hit.collider.GetComponentInParent<RoadCar>()==null&&hit.point.y<=b.max.y)ground=Mathf.Max(ground,hit.point.y);
                if(ground!=float.MinValue&&b.min.y-ground>.3f){floating.Add($"Guardrail post at ({b.center.x:0},{b.min.y:0.0},{b.center.z:0}) gap {b.min.y-ground:0.00} m");FloatingAt.Add(b.center);}
            }
            // Bunting ends: each must touch a placed piece (lamp, flag pole, building, gate) within 0.8 m.
            var loose=new List<string>();int ends=0;
            foreach(var (a,b) in HillcrestTown.BuntingEnds)foreach(var e in new[]{a,b}) {
                ends++;bool held=false;
                foreach(var go in BayviewKit.Placed){if(go==null)continue;foreach(var r in go.GetComponentsInChildren<Renderer>()){var rb=r.bounds;rb.Expand(1.6f);if(rb.Contains(e)){held=true;break;}}if(held)break;}
                if(!held){loose.Add($"bunting end at ({e.x:0},{e.y:0.0},{e.z:0})");LooseBunting.Add(e);}
            }
            var sb=new StringBuilder();
            sb.AppendLine($"checked={checkedCount} floating={floating.Count} sunk={sunk.Count} buntingEnds={ends} looseBunting={loose.Count}");
            foreach(var l in loose)sb.AppendLine("  "+l);
            sb.AppendLine("floating (gap under the whole footprint):");foreach(var f in floating.OrderBy(s=>s))sb.AppendLine("  "+f);
            sb.AppendLine("sunk (centre below ground):");foreach(var s in sunk.OrderBy(s=>s))sb.AppendLine("  "+s);
            return sb.ToString();
        }
        // Road seams (Richard, 3 Oct: "joins in roads being difficult to traverse"): driving lines along every road (the centre
        // and both wheel tracks) and through every junction (each arm's mouth to every other's, via the node), the top
        // surface sampled every 0.5 m. A step is where the surface jumps by 6 cm or more against the slope on either side
        // (a smooth grade or a crest doesn't count; a ledge, a lip between a ribbon and a junction, or a hump does).
        public static readonly List<Vector3> SeamAt=new();
        public static string Seams(Circuit bayview=null)
        {
            Physics.SyncTransforms();SeamAt.Clear();var sb=new StringBuilder();int lines=0;var found=new List<string>();
            string topName="";
            float Top(Vector3 p){float best=float.MinValue;topName="";foreach(var h in Physics.RaycastAll(p+Vector3.up*3,Vector3.down,7))if(!h.collider.isTrigger&&h.collider.GetComponentInParent<RoadCar>()==null&&!h.collider.CompareTag("Respawn")&&h.point.y>best){best=h.point.y;topName=h.collider.name;}return best;}
            void Drive(string name,IList<Vector3> path,bool closed)
            {
                // Resample at 0.5 m.
                var pts=new List<Vector3>();int n=path.Count;
                for(int i=0;i<(closed?n:n-1);i++){var a=path[i];var b=path[(i+1)%n];float len=Vector2.Distance(new(a.x,a.z),new(b.x,b.z));for(float d=0;d<len;d+=.5f)pts.Add(Vector3.Lerp(a,b,d/len));}
                if(pts.Count<5)return;lines++;
                var h=new float[pts.Count];var names=new string[pts.Count];for(int i=0;i<pts.Count;i++){h[i]=Top(pts[i]);names[i]=topName;}
                Vector3 last=new(1e6f,0,0);
                for(int i=2;i<pts.Count-2;i++) {
                    if(h[i]==float.MinValue||h[i-1]==float.MinValue||h[i-2]==float.MinValue||h[i+1]==float.MinValue)continue;
                    float before=h[i-1]-h[i-2],step=h[i]-h[i-1],after=h[i+1]-h[i];
                    // Against both neighbouring slopes: a jump that the road on either side doesn't share.
                    if(Mathf.Abs(step-before)>.06f&&Mathf.Abs(step-after)>.06f&&Mathf.Abs(step)>.06f) {
                        var p=pts[i];if(Vector3.Distance(p,last)<3)continue;last=p;SeamAt.Add(p);
                        // (With the drawn line's own heights either side, to tell a ribbon fault from a line fault.)
                        int near=0;float nd=float.MaxValue;for(int q=0;q<path.Count;q++){float dd=Vector2.Distance(new(path[q].x,path[q].z),new(p.x,p.z));if(dd<nd){nd=dd;near=q;}}
                        var around=new List<string>();for(int q=Mathf.Max(0,near-2);q<=Mathf.Min(path.Count-1,near+2);q++)around.Add($"{path[q].y:0.00}@({path[q].x:0.0},{path[q].z:0.0})");
                        // (Every surface under the two samples either side of the step: name, kind, height, parent.)
                        string Hits(Vector3 q){const string none="none";var l=new List<string>();foreach(var hh in Physics.RaycastAll(q+Vector3.up*3,Vector3.down,7))if(!hh.collider.isTrigger)l.Add($"{hh.collider.name}/{hh.collider.GetType().Name}/{hh.point.y:0.00}/{(hh.collider.transform.parent!=null?hh.collider.transform.parent.name:none)}/{hh.collider.GetEntityId()}");return string.Join(" ; ",l);}
                        found.Add($"  {name} step {step:+0.00;-0.00} m at ({p.x:0},{h[i]:0.0},{p.z:0}) [{names[i-1]} -> {names[i]}] surface {h[i-2]:0.00},{h[i-1]:0.00},{h[i]:0.00},{h[i+1]:0.00} line {string.Join(" ",around)}\n      before: {Hits(pts[i-1])}\n      after: {Hits(pts[i])}");
                    }
                }
            }
            if(bayview!=null)Drive("Bayview road",bayview.points,true);
            foreach(var road in HillcrestRoute.All()) {
                var line=road.line;Drive(road.name,line,false);
                foreach(int side in new[]{-1,1}){var track=new List<Vector3>();for(int i=0;i<line.Length;i++){var f=line[Mathf.Min(line.Length-1,i+1)]-line[Mathf.Max(0,i-1)];f.y=0;f.Normalize();track.Add(line[i]+Vector3.Cross(Vector3.up,f)*side*(road.width*.5f-1.1f));}Drive(road.name+(side<0?" (left track)":" (right track)"),track,false);}
            }
            Drive("Hillcrest Raceway",HillcrestRoute.Circuit,true);
            if(QuarryHills.Enabled){Drive("Quarry Sprint",QuarryFloor.Loop,true);if(GalleryClimb.Path.Count>4)Drive("Gallery Climb",GalleryClimb.Path,false);}
            var net=CoastalLandscape.Network;
            if(net!=null)foreach(var node in net.nodes) {
                if(node.arms.Count<2)continue;
                var mouths=node.arms.ConvertAll(a=>(a.MouthLeft+a.MouthRight)*.5f);
                for(int i=0;i<mouths.Count;i++)for(int j=0;j<mouths.Count;j++)if(i!=j){var a=mouths[i];var b=mouths[j];
                    // From 8 m out along arm i, through the node, to 8 m out along arm j.
                    var ai=a-node.at;ai.y=0;var bj=b-node.at;bj.y=0;Drive($"junction ({node.at.x:0},{node.at.z:0})",new List<Vector3>{a+ai.normalized*8,a,node.at,b,b+bj.normalized*8},false);}
            }
            sb.AppendLine($"seam lines={lines} steps={SeamAt.Count}");foreach(var f in found)sb.AppendLine(f);
            return sb.ToString();
        }
        // Road edges you can't drive back up (--edge-audit): along every valley road, steps out from the
        // asphalt edge to 4 m beyond it, sampling the highest solid surface; a rise of more than 12 cm between
        // neighbouring samples (10 cm apart) is a lip a car's tyre catches on. Road ends are checked too.
        // Every road the car can drive (Bayview's loop, the valley roads, the raceway); each side's edge is found where
        // the tarmac collider (RoadSurface) ends, so no road's width has to be known.
        // Where the last Edges() run found each lip (true) or too-steep bank (false), for the area audit.
        public static readonly List<(Vector3 at,bool lip)> EdgeFindings=new();
        // (each unguarded bank: the road point, out across the road towards the drop, along the road, where the drop starts
        //  measured from the centreline, the road's name, the tarmac's half-width: BankBarriers walls them, docs/80 round 7)
        public static readonly List<(Vector3 at,Vector3 outward,Vector3 along,float dropAt,string road,float half)> BankSites=new();
        // (`only`: the part of the world to audit, by road point; all of it when null. A background district's roads are
        // audited once it has built, so its walls count as guards: Districts, BankBarriers.)
        public static string Edges(Circuit bayview=null,System.Func<Vector3,bool> only=null)
        {
            Physics.SyncTransforms();var sb=new StringBuilder();int lips=0,checks=0;EdgeFindings.Clear();BankSites.Clear();
            float Top(Vector3 p){float best=float.MinValue;foreach(var h in Physics.RaycastAll(p+Vector3.up*4,Vector3.down,10))if(!h.collider.isTrigger&&h.collider.GetComponentInParent<RoadCar>()==null&&!h.collider.CompareTag("Respawn"))best=Mathf.Max(best,h.point.y);return best==float.MinValue?float.NaN:best;}   // NaN: nothing solid (open water)
            bool Tarmac(Vector3 p){float best=float.MinValue;bool road=false;foreach(var h in Physics.RaycastAll(p+Vector3.up*4,Vector3.down,10))if(!h.collider.isTrigger&&h.collider.GetComponentInParent<RoadCar>()==null&&h.point.y>best){best=h.point.y;road=h.collider.GetComponentInParent<RoadSurface>()!=null;}return road;}
            var found=new List<string>();var bankList=new List<string>();int banks=0,guardedEdges=0;
            // A barrier at p: something solid standing 35 cm or more over the ground there (a guardrail, parapet,
            // railing, wall, house or plinth). The ground is the terrain, a road or a gravel shoulder or ramp.
            static bool Ground(Collider c)=>c.GetComponent<RoadSurface>()!=null||c.name.Contains("mainland collider")||c.name.Contains("island collider")||c.name.Contains("shoulder")||c.name.Contains("ramp");
            bool Barrier(Vector3 p)
            {
                float ground=float.MinValue,top=float.MinValue;
                foreach(var h in Physics.RaycastAll(p+Vector3.up*40,Vector3.down,60)) {
                    if(h.collider.isTrigger||h.collider.GetComponentInParent<RoadCar>()!=null)continue;
                    if(Ground(h.collider))ground=Mathf.Max(ground,h.point.y);else top=Mathf.Max(top,h.point.y);
                }
                return ground>float.MinValue&&top-ground>.35f;
            }
            // A barrier between the edge and `upTo` metres out from the centreline.
            // A gap between barriers narrower than a car (1.1 m either side along the road) is closed too: the gap between
            // two terraced houses on a hillside.
            // (For the bank list: the surface height and what it is, every metre out from the edge.)
            IEnumerable<string> ProfileOut(Vector3 at,Vector3 r,float half)
            {
                for(int k=0;k<=12;k+=1){var p=at+r*(half+k);float best=float.MinValue;string what="none";
                    foreach(var h in Physics.RaycastAll(p+Vector3.up*4,Vector3.down,10))if(!h.collider.isTrigger&&h.collider.GetComponentInParent<RoadCar>()==null&&!h.collider.CompareTag("Respawn")&&h.point.y>best){best=h.point.y;what=h.collider.name;}
                    yield return $"{best-at.y:+0.0;-0.0}:{what}";}
            }
            bool Guarded(Vector3 at,Vector3 r,float half,float upTo,Vector3 along)
            {
                for(float o=half+.1f;o<=upTo;o+=.04f){var p=at+r*o;if(Barrier(p)||Barrier(p+along*1.1f)&&Barrier(p-along*1.1f))return true;}
                return false;
            }
            var lines=new List<(string name,Vector3[] line,float half)>();
            if(bayview!=null)lines.Add(("Bayview road",bayview.points,Circuit.Width*.5f));
            foreach(var road in HillcrestRoute.All())lines.Add((road.name,road.line,road.width*.5f));
            lines.Add(("Hillcrest Raceway",HillcrestRoute.Circuit,8));
            foreach(var (name,line,guess) in lines) {
                float run=0;
                for(int i=1;i<line.Length-1;i+=2) {
                    run+=Vector3.Distance(line[i],line[i-1])+(i>1?Vector3.Distance(line[i-1],line[i-2]):0);
                    if(only!=null&&!only(line[i]))continue;
                    var f=line[i+1]-line[i-1];f.y=0;f.Normalize();var r=Vector3.Cross(Vector3.up,f);
                    foreach(int side in new[]{-1,1}) {
                        // The edge: where the tarmac stops (the road's own half-width if it isn't tarmac here, a bridge deck).
                        float half=guess;if(Tarmac(line[i])){for(float o=1;o<guess+6;o+=.1f)if(!Tarmac(line[i]+r*side*o)){half=o;break;}}
                        checks++;float prev=Top(line[i]+r*side*(half-.3f)),worst=0;float at=0;
                        for(float o=half-.2f;o<=half+4;o+=.1f){float h=Top(line[i]+r*side*o);if(float.IsNaN(h))break;float rise=prev-h;if(rise>worst){worst=rise;at=o;}prev=h;}
                        // A barrier between the edge and the step keeps a car on the road: a step behind one isn't a trap.
                        bool guarded=worst>.12f&&Guarded(line[i],r*side,half,at+1f,f);
                        if(worst>.12f&&worst<2&&!guarded){lips++;EdgeFindings.Add((line[i],true));found.Add($"  {name} run {run:0} side {(side>0?"R":"L")} lip {worst:0.00} m at {at-half:0.0} m past the edge ({line[i].x:0},{line[i].y:0.0},{line[i].z:0}) edge {half:0.0} m, out every metre: {string.Join(" ",ProfileOut(line[i],r*side,half-1))}");}
                        // Banks (Richard, 30 Sep: roads by the railway you can't get back onto): out to 12 m, the
                        // steepest climb back towards the road over any metre, where the climb is at least 40 cm.
                        // (Drops of 2 m and more are bridges and sea walls, which have parapets.)
                        if(worst<2) {
                            var list=new List<float>();for(int k=0;k<49;k++){float h=Top(line[i]+r*side*(half+k*.25f));if(float.IsNaN(h))break;list.Add(h);}var hs=list.ToArray();
                            float steep=0,rise=0;for(int k=0;k+4<hs.Length;k++){float s=hs[k]-hs[k+4];if(s>steep)steep=s;}
                            for(int k=1;k<hs.Length;k++)if(hs[k-1]-hs[k]>.05f)rise+=hs[k-1]-hs[k];
                            int kSteep=0;for(int k=0;k+4<hs.Length;k++)if(hs[k]-hs[k+4]>=steep-1e-4f){kSteep=k;break;}
                            // A barrier between the edge and the top of the drop (a sea-wall railing, a parapet) guards it.
                            if(steep>.55f&&rise>.4f&&Guarded(line[i],r*side,half,half+kSteep*.25f+1f,f)){guardedEdges++;continue;}
                            if(steep>.55f&&rise>.4f){banks++;EdgeFindings.Add((line[i],false));BankSites.Add((line[i],r*side,f,half+kSteep*.25f,name,half));bankList.Add($"  {name} run {run:0} side {(side>0?"R":"L")} bank {Mathf.Atan(steep)*Mathf.Rad2Deg:0}° climbing {rise:0.0} m ({line[i].x:0},{line[i].y:0.0},{line[i].z:0}) edge {half:0.0} m, out every metre: {string.Join(" ",ProfileOut(line[i],r*side,half))}");}
                        }
                    }
                }
                // Road ends: straight on past the last point (loops have none).
                if(Vector3.Distance(line[0],line[^1])>12)foreach(bool start in new[]{true,false}) {
                    var e=start?line[0]:line[^1];if(only!=null&&!only(e))continue;var d=start?line[0]-line[1]:line[^1]-line[^2];d.y=0;d.Normalize();float prev=Top(e-d*.3f),worst=0;
                    for(float o=0;o<=4;o+=.1f){float h=Top(e+d*o);if(float.IsNaN(h))break;worst=Mathf.Max(worst,prev-h);prev=h;}
                    checks++;if(worst>.12f){lips++;EdgeFindings.Add((e,true));found.Add($"  {name} {(start?"start":"end")} lip {worst:0.00} m ({e.x:0},{e.z:0})");}
                }
            }
            sb.AppendLine($"edge checks={checks} lips={lips} banks={banks} guarded={guardedEdges}");foreach(var l in found)sb.AppendLine(l);
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
