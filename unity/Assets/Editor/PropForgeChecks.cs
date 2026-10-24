using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Prop Forge, step 1 (docs/77): the kit contract read back (car-forge forge/world/kit.py -> Resources/Props), its axes and
// winding, one ground provider, and the run test range's roads. On by default since 20 Oct (--no-prop-forge, --old-bayview for the old world; --run-range).
public static class PropForgeChecks
{
    sealed class Slope:Ground.IProvider{public float Height(float x,float z)=>.3f*x+2;}
    sealed class Range:Ground.IProvider{public float Height(float x,float z)=>RunRange.Surface(x,z);}
    [Serializable] sealed class PaletteNames{public PaletteName[] entries;}[Serializable] sealed class PaletteName{public string name;}

    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        Check(!PropForge.Enabled&&!RunRange.Requested,"Prop Forge is off in the editor's checks without --prop-forge (on by default in the game since 20 Oct)");

        // the contract: the probe kit (one post, one rail) as the sync wrote it
        PropKit.Forget();var kit=PropKit.Load("probe");
        Check(kit.file.schema==PropKit.Schema&&kit.file.kit=="probe"&&kit.file.version>=1&&kit.file.pieces.Length==2,"Prop Forge: the probe kit reads (schema prop-forge-kit/0, two pieces)");
        var post=kit.Mesh("post");var rail=kit.Mesh("rail");
        Check(post.triangles.Length/3==kit.Piece("post").lods[0].triangles&&kit.Piece("post").lods.Length==3,"Prop Forge: a piece's mesh has the contract's triangles, and three LODs");
        Check(Mathf.Abs(post.bounds.size.y-1.1f)<.06f&&Mathf.Abs(post.bounds.min.y)<.03f&&Mathf.Abs(post.bounds.center.x)<.01f&&Mathf.Abs(post.bounds.center.z)<.01f,
            $"Prop Forge: axes: the post stands 1.1 m up y from its base at the origin ({post.bounds.min.y:0.00} to {post.bounds.max.y:0.00})");
        Check(Mathf.Abs(rail.bounds.size.z-2.4f)<.06f&&Mathf.Abs(rail.bounds.min.z)<.03f,$"Prop Forge: axes: a run piece runs 2.4 m along +z from 0 ({rail.bounds.min.z:0.00} to {rail.bounds.max.z:0.00})");
        var copy=UnityEngine.Object.Instantiate(post);var given=post.normals;copy.RecalculateNormals();var unity=copy.normals;
        int agree=0;for(int i=0;i<given.Length;i++)if(Vector3.Dot(given[i],unity[i])>0)agree++;UnityEngine.Object.DestroyImmediate(copy);
        Check(agree>=given.Length*.95f,$"Prop Forge: winding: the faces Unity draws face the way the kit's normals do ({agree}/{given.Length})");
        int outward=0;var pv=post.vertices;for(int i=0;i<pv.Length;i++){var radial=pv[i]-new Vector3(0,Mathf.Clamp(pv[i].y,.06f,1.04f),0);if(radial.sqrMagnitude<1e-6f||Vector3.Dot(radial,given[i])>0)outward++;}
        Check(outward>=pv.Length*.95f,$"Prop Forge: the kit's normals face out of the post ({outward}/{pv.Length})");
        Check(kit.MaterialsOf("post").Length==post.subMeshCount&&kit.file.materials.All(m=>PropKit.MaterialFor(m)!=null),"Prop Forge: every material class has a look, one per submesh");
        // (every installed kit's classes: the jerry can's red paint once had none, and the range failed at load, 18 Oct)
        var installed=Resources.LoadAll<TextAsset>("Props").Where(t=>!t.name.EndsWith("_mesh")&&t.name!="prop_manifest").Select(t=>t.name).ToList();
        bool Stands(string k)=>PropKit.Load(k).file.replaces!=null&&PropKit.Load(k).file.replaces.Length>0;
        Check(installed.Count>=5&&installed.Where(k=>!Stands(k)).All(k=>PropKit.Load(k).file.materials.All(m=>PropKit.MaterialFor(m)!=null)),$"Prop Forge: every installed kit's material classes have a look ({string.Join(", ",installed.Where(k=>!Stands(k)))})");
        // (a stand-in's materials are the game palette's names, coloured by BayviewKit: every one must be in the palette)
        var palette=new HashSet<string>(JsonUtility.FromJson<PaletteNames>(Resources.Load<TextAsset>("Bayview/palette").text).entries.Select(e=>e.name));
        var stands=installed.Where(Stands).ToList();var offPalette=stands.SelectMany(k=>PropKit.Load(k).file.materials).Where(m=>!palette.Contains(m)).Distinct().ToList();
        Check(offPalette.Count==0,$"Prop Forge: every stand-in kit ({string.Join(", ",stands)}) is coloured by palette names{(offPalette.Count>0?": not in the palette: "+string.Join(", ",offPalette):"")}");
        var ids=stands.SelectMany(k=>PropKit.Load(k).file.replaces).Select(r=>r.id).ToList();
        Check(ids.Count==ids.Distinct().Count(),$"Prop Forge: no game kit id has two stand-ins ({ids.Count} ids: {string.Join(", ",ids)})");
        bool unknown=false;try{PropKit.MaterialFor("chocolate");}catch(InvalidOperationException){unknown=true;}
        Check(unknown,"Prop Forge: an unknown material class fails loudly");
        bool missing=false;try{PropKit.Load("no_such_kit");}catch(InvalidOperationException){missing=true;}
        Check(missing,"Prop Forge: a kit that isn't installed fails loudly");

        // one ground
        Ground.Use(new Slope());
        try {
            Check(Mathf.Abs(Ground.Surface(10,5)-5)<1e-4f,"Ground: the provider in use answers Surface");
            var n=Ground.Normal(0,0);Check(Mathf.Abs(Vector3.Angle(n,new Vector3(-.3f,1,0).normalized))<.5f,"Ground: Normal follows the provider's slope");
        } finally { Ground.Reset(); }
        Check(!Ground.Custom,"Ground: Reset returns to the world's own fields");

        // the run test range
        RunRange.BuildRoads();
        Check(new[]{"straight","spur","drive","crest-hollow","bank-road","bends"}.All(n=>RunRange.Roads.Any(r=>r.name==n)),"Run range: every fixture road is laid");
        var bends=RunRange.Road("bends").line;float minR=float.MaxValue;
        for(int i=1;i+1<bends.Length;i++){Vector2 a=new(bends[i-1].x,bends[i-1].z),b=new(bends[i].x,bends[i].z),c=new(bends[i+1].x,bends[i+1].z);
            float ab=(b-a).magnitude,bc=(c-b).magnitude,ca=(a-c).magnitude,area=Mathf.Abs((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x))/2;if(area>1e-4f)minR=Mathf.Min(minR,ab*bc*ca/(4*area));}
        Check(minR>7.5f&&minR<8.5f,$"Run range: the tightest bend is 8 m ({minR:0.0} m)");
        Check(Mathf.Abs((RunRange.Land(61,0)-RunRange.Land(49,0))/12-.3f)<.01f,"Run range: the bank road runs along a 30% cross-slope");
        Check(RunRange.Land(-90,60)>2.9f&&RunRange.Land(-90,0)<-2.4f,"Run range: a 3 m crest and a 2.5 m hollow on the crest-hollow road");
        float y=RunRange.Road("crest-hollow").line[30].y;var p=RunRange.Road("crest-hollow").line[30];
        Check(Mathf.Abs(RunRange.Surface(p.x+2,p.z)-y)<.05f,"Run range: the ground is the road across its width (roads first)");

        // step 2: the post-and-rail fence solved on the range (layouts as data: no meshes needed)
        Ground.Use(new Range());
        try {
            PropKit.Forget();var fence=PropKit.Load("post_rail");var run=fence.file.run;
            var layouts=RunRange.Specs().Select(Runs.Solve).ToList();double ms=layouts.Sum(l=>l.solveMs);float metres=layouts.Sum(l=>l.length);
            foreach(var L in layouts) {
                string w=L.spec.name;
                Check(L.failures.Count==0,$"Runs: {w} lays clean ({string.Join("; ",L.failures)})");
                var posts=L.items.Where(i=>i.piece!="rail").ToList();var rails=L.items.Where(i=>i.piece=="rail").ToList();
                Check(posts.All(i=>Vector3.Angle(i.m.rotation*Vector3.up,Vector3.up)<.5f),$"Runs: {w}: every post plumb within 0.5 deg");
                Check(posts.All(i=>Mathf.Abs(i.m.GetColumn(3).y-Ground.Surface(i.m.GetColumn(3).x,i.m.GetColumn(3).z))<.01f),$"Runs: {w}: every post stands on the ground (sunk by its skirt)");
                if(L.spans>0)Check(L.minSpan>=Runs.MinSplit-1e-3f&&L.maxSpan<=run.spacing*fence.Piece("rail").stretch[1]+.02f,$"Runs: {w}: spans {L.minSpan:0.00}-{L.maxSpan:0.00} m, within the kit's stretch (a split span no shorter than 0.9 m)");
                bool raked=true;
                foreach(var rl in rails){var start=(Vector3)rl.m.GetColumn(3);var dir=rl.m.rotation*Vector3.forward;var foot=new Vector3(start.x,Ground.Surface(start.x,start.z),start.z);
                    float h=start.y-foot.y;if(!run.railHeights.Any(x=>Mathf.Abs(x-h)<.02f))raked=false;}
                Check(raked,$"Runs: {w}: every rail starts at a rail height above its post's foot (raked between the feet)");
                bool clear=true;
                foreach(var it in L.items){var at=(Vector3)it.m.GetColumn(3);if(it.piece=="rail")at=(Vector3)(it.m*new Vector4(0,0,fence.Piece("rail").length/2,1));
                    foreach(var road in RunRange.Roads)if(RunRange.Nearest(road,at.x,at.z,out _)<road.width*.5f-.01f)clear=false;}
                Check(clear,$"Runs: {w}: no piece inside any road");
                Check(L.Hash()==Runs.Solve(L.spec).Hash(),$"Runs: {w}: the solve is deterministic");
            }
            var straight=layouts.First(l=>l.spec.name=="straight left");
            Check(straight.openings>=2&&straight.gatePosts>=4,$"Runs: the straight's fence opens for the spur's junction and the drive, each framed by gate posts ({straight.openings} openings, {straight.gatePosts} gate posts)");
            var plain=layouts.First(l=>l.spec.name=="straight right");
            Check(Mathf.Abs(plain.minSpan-run.spacing)<.05f&&Mathf.Abs(plain.maxSpan-run.spacing)<.05f,$"Runs: posts keep the kit's own spacing where they can ({plain.minSpan:0.00}-{plain.maxSpan:0.00} m on the plain straight, kit {run.spacing:0.0} m)");
            var shortRun=layouts.First(l=>l.spec.name=="too short");
            Check(shortRun.tooShort&&shortRun.items.Count==0&&shortRun.notes.Count>0,"Runs: a run too short to lay is reported, with nothing laid");
            int tris=layouts.Sum(l=>l.items.Sum(i=>fence.Piece(i.piece).lods[0].triangles));
            Check(tris/metres<=250,$"Runs: the fence is {tris/metres:0} triangles a metre at LOD0 (at most 250)");
            checks.Add($"Runs: {layouts.Count} range runs, {metres:0} m, solved in {ms:0.0} ms ({ms/metres*100:0.00} ms per 100 m)");
            // (the same runs on a flat, constant-time ground: the solver's own cost, apart from the range's ground lookup,
            //  which scans every road; the live world's ground is a grid lookup)
            Ground.Use(new Slope());var flat=RunRange.Specs().Select(sp=>{sp.cut=null;return Runs.Solve(sp);}).ToList();double flatMs=flat.Sum(l=>l.solveMs);float flatM=flat.Sum(l=>l.length);
            checks.Add($"Runs: the solver alone, on a constant-time ground: {flatMs/flatM*100:0.00} ms per 100 m");
        } finally { Ground.Reset(); }

        // step 3: the dry-stone wall, a deforming kit, along every range road and the field wall's corners
        Ground.Use(new Range());
        try {
            PropKit.Forget();var wk=PropKit.Load("dry_stone");var run=wk.file.run;var wallPiece=wk.Piece("wall");
            Check(wk.file.kind=="deform"&&run.deform>0&&run.halfBase>0&&wallPiece.role=="middle","Walls: the dry-stone kit reads as a deforming kit (a bending middle, piers by role)");
            var walls=RunRange.Specs("dry_stone").Select(Runs.Solve).ToList();double ms=walls.Sum(l=>l.solveMs);float metres=walls.Sum(l=>l.length);
            foreach(var L in walls) {
                string w=L.spec.name;
                if(w=="too short"){Check(L.tooShort&&L.items.Count==0&&L.notes.Count>0,"Walls: a run too short to lay is reported, with nothing laid");continue;}
                Check(L.failures.Count==0,$"Walls: {w} lays clean ({string.Join("; ",L.failures)})");
                var piers=L.items.Where(i=>!i.deform).ToList();
                Check(piers.All(i=>Vector3.Angle(i.m.rotation*Vector3.up,Vector3.up)<.5f),$"Walls: {w}: every pier plumb within 0.5 deg");
                bool footed=true;
                foreach(var it in piers){var pc=wk.Piece(it.piece);float fy=it.m.GetColumn(3).y;foreach(var cp in pc.contact){var q=it.m.MultiplyPoint3x4(cp.V);float g=Ground.Surface(q.x,q.z);if(fy>g+.001f||g-fy>pc.skirt)footed=false;}}
                Check(footed,$"Walls: {w}: every pier's foot is in the ground at every corner, within its skirt");
                float air=-1,deep=0,slope=0;Vector3 worst=default;
                foreach(var pa in L.paths) {
                    for(int i=0;i<pa.p.Length;i++){var r=Runs.Right(pa.Tangent(pa.arc[i]));foreach(float side in new[]{-1f,1f}){var q=pa.p[i]+r*side*run.halfBase;float g=Ground.Surface(q.x,q.z);air=Mathf.Max(air,pa.baseY[i]-g);deep=Mathf.Max(deep,g-pa.baseY[i]);}}
                    for(float s=0;s+2<=pa.Length;s+=.5f){var a=pa.At(s);var b=pa.At(s+2);float gs=(Ground.Surface(b.x,b.z)-Ground.Surface(a.x,a.z))/2,ws=(pa.Base(s+2)-pa.Base(s))/2;float d=Mathf.Abs(Mathf.Atan(gs)-Mathf.Atan(ws))*Mathf.Rad2Deg;if(d>slope){slope=d;worst=a;}}
                }
                Check(air<=.005f&&deep<=wallPiece.skirt,$"Walls: {w}: the foot follows the ground across its width (at most {Mathf.Max(air,0)*1000:0} mm in the air, {deep:0.00} m buried, skirt {wallPiece.skirt:0.00} m)");
                Check(slope<=2,$"Walls: {w}: its top within 2 deg of the ground's slope over every 2 m ({slope:0.0} deg, worst at {worst.x:0},{worst.z:0})");
                bool clear=true;
                foreach(var pa in L.paths)for(int i=0;i<pa.p.Length;i+=2){var r=Runs.Right(pa.Tangent(pa.arc[i]));foreach(float side in new[]{-1f,1f}){var q=pa.p[i]+r*side*run.halfBase;foreach(var road in RunRange.Roads)if(RunRange.Nearest(road,q.x,q.z,out _)<road.width*.5f-.01f)clear=false;}}
                Check(clear,$"Walls: {w}: no wall inside any road");
                Check(L.Hash()==Runs.Solve(L.spec).Hash(),$"Walls: {w}: the solve is deterministic");
            }
            Check(walls.Where(l=>l.spec.name.StartsWith("bends")||l.spec.name.StartsWith("crest")||l.spec.name.StartsWith("bank")).All(l=>l.corners==0),"Walls: road bends down to 8 m bend the wall (no corner piers)");
            var field=walls.First(l=>l.spec.name=="field corners");
            Check(field.corners==4&&field.items.Count(i=>i.piece==Runs.Role(wk,"node"))==4,$"Walls: the field wall turns its corners of 76, 72, 36 and 101 deg on piers ({field.corners} corners)");
            bool inside=true;
            foreach(var L in walls)foreach(var pa in L.paths)foreach(var e in new[]{pa.p[0],pa.p[^1]})
                if(!L.items.Any(i=>!i.deform&&Vector2.Distance(new Vector2(i.m.m03,i.m.m23),new Vector2(e.x,e.z))<.01f))inside=false;
            Check(inside,"Walls: every wall ends at a pier's centre (its open end inside a square pier at any angle)");
            var straight=walls.First(l=>l.spec.name=="straight left");
            Check(straight.openings>=2&&straight.gatePosts>=4,$"Walls: the straight's wall opens for the junction and the drive, framed by gate piers ({straight.openings} openings, {straight.gatePosts} gate piers)");
            // joins: where one bent piece ends the next begins, on the 8 m bend; normals agree across each join
            var bent=walls.First(l=>l.spec.name=="bends left");var src=wk.Mesh("wall",0);var sv=src.vertices;var sn=src.normals;float len=wallPiece.length;
            float gap=0,turn=0;int joins=0;string why="";
            for(int i=0;i+1<bent.items.Count;i++) {
                var a=bent.items[i];var b=bent.items[i+1];if(!a.deform||!b.deform||a.path!=b.path)continue;
                var ma=Runs.Deform(bent,a,0);var mb=Runs.Deform(bent,b,0);var va=ma.vertices;var vb=mb.vertices;var na=ma.normals;var nb=mb.normals;
                for(int x=0;x<sv.Length;x++) {
                    if(Mathf.Abs(sv[x].z-len)>1e-4f)continue;
                    for(int z=0;z<sv.Length;z++){if(Mathf.Abs(sv[z].z)>1e-4f||Mathf.Abs(sv[z].x-sv[x].x)>1e-5f||Mathf.Abs(sv[z].y-sv[x].y)>1e-5f||Vector3.Dot(sn[x],sn[z])<.999f)continue;
                        gap=Mathf.Max(gap,Vector3.Distance(va[x],vb[z]));{float ang=Vector3.Angle(na[x],nb[z]);if(ang>turn){turn=ang;why=$" [local {sv[x]} n {sn[x]}; a {na[x]} b {nb[z]}; s {a.s1:0.000}/{b.s0:0.000}]";}}}
                }
                joins++;UnityEngine.Object.DestroyImmediate(ma);UnityEngine.Object.DestroyImmediate(mb);
            }
            Check(joins>=10&&gap<.002f&&turn<1f,$"Walls: joins: {joins} joins on the bends, the widest gap {gap*1000:0.00} mm (under 2), normals within {turn:0.00} deg{(turn<1f?"":why)}");
            int t0=0,t1=0;foreach(var L in walls)foreach(var it in L.items){t0+=wk.Piece(it.piece).lods[0].triangles;t1+=wk.Piece(it.piece).lods[1].triangles;}
            Check(t0/metres<=600&&t1/metres<=150,$"Walls: the wall is {t0/metres:0} triangles a metre at LOD0 (at most 600) and {t1/metres:0} at LOD1 (at most 150)");
            checks.Add($"Walls: {walls.Count} range runs, {metres:0} m, solved in {ms:0.0} ms ({ms/metres*100:0.00} ms per 100 m)");
        } finally { Ground.Reset(); }
        checks.Add(Slide(Check));

        // step 4: the hay bale family on the range, tilted and settled, under the family's repetition rules
        Ground.Use(new Range());
        try {
            PropKit.Forget();var bk=PropKit.Load("hay_bale");var family=bk.file.pieces.Where(p=>p.role=="object").ToList();var br=bk.file.run;
            Check(bk.file.kind=="object"&&family.Count>=8&&family.Select(p=>p.variant).Distinct().Count()==family.Count,$"Bales: the hay bale family reads ({family.Count} variants, each its own)");
            Check(family.All(p=>p.lods[0].triangles<=1500&&p.lods[1].triangles<=300),
                $"Bales: every variant within an object's budget (at most {family.Max(p=>p.lods[0].triangles)} triangles at LOD0 and {family.Max(p=>p.lods[1].triangles)} at LOD1; 1500 and 300)");
            var looks=family.Select(p=>(size:bk.Mesh(p.name).bounds.size,mats:string.Join(",",bk.MaterialsOf(p.name).Select(m=>m.name)))).ToList();bool distinct=true;
            for(int i=0;i<looks.Count;i++)for(int j=i+1;j<looks.Count;j++)if(looks[i].mats==looks[j].mats&&(looks[i].size-looks[j].size).magnitude<.03f)distinct=false;
            Check(distinct,"Bales: no two variants alike (each differs in size by 3 cm or more, or in material)");
            var specs=RunRange.Bales().Where(b=>b.kit=="hay_bale").ToList();var layouts=specs.Select(b=>Objects.Place(b.name,"hay_bale",b.spots,b.name,b.row,b.only)).ToList();
            foreach(var L in layouts) {
                Check(L.failures.Count==0,$"Bales: {L.name}: every bale within 4 deg of the ground's plane under it, sunk 2-8 cm, no contact over 2 cm in the air " +
                    $"(worst {L.placed.Max(o=>o.tilt):0.0} deg, {L.placed.Min(o=>o.sink)*100:0.0}-{L.placed.Max(o=>o.sink)*100:0.0} cm, {L.placed.Max(o=>o.air)*100:0.0} cm){(L.failures.Count>0?": "+string.Join("; ",L.failures.Take(3)):"")}");
                int clashes=0;
                for(int i=0;i<L.placed.Count;i++)for(int j=i+1;j<L.placed.Count;j++){var a=L.placed[i];var b=L.placed[j];
                    if(a.variant==b.variant&&new Vector2(a.at.x-b.at.x,a.at.z-b.at.z).magnitude<br.sameGap&&Objects.YawGap(a.yaw,b.yaw)<br.sameYaw)clashes++;}
                Check(clashes==0&&L.forced==0,$"Bales: {L.name}: no variant twice within {br.sameGap:0} m at a like yaw ({clashes} pairs, {L.forced} forced)");
                bool clear=L.placed.All(o=>RunRange.Roads.All(r=>RunRange.Nearest(r,o.at.x,o.at.z,out _)>r.width*.5f+1));
                Check(clear,$"Bales: {L.name}: none on or beside a road");
                var spec=specs.First(s=>s.name==L.name);Check(L.Hash()==Objects.Place(spec.name,"hay_bale",spec.spots,spec.name,spec.row,spec.only).Hash(),$"Bales: {L.name}: the placing is deterministic");
            }
            var row=layouts.First(l=>l.row);var seq=row.placed.Select(o=>o.variant).ToList();int repeats=0;for(int i=0;i<seq.Count;i++)repeats+=Objects.Repeats(seq.Take(i).ToList(),seq[i]);
            Check(repeats==0,$"Bales: the yard row has no variant twice running and no repeat of a two- or three-long run ({string.Concat(seq)})");
            var bank=layouts.First(l=>l.name=="bank bales");int downhill=0,rollers=0;
            foreach(var o in bank.placed) {
                if(!br.rolls.Contains(o.piece))continue;rollers++;
                var n=Ground.Normal(o.at.x,o.at.z,.5f);var axis=Quaternion.Euler(0,o.yaw,0)*Vector3.right;var down=new Vector3(n.x,0,n.z).normalized;
                if(Vector3.Angle(axis,down)<=12||Vector3.Angle(axis,-down)<=12)downhill++;
            }
            Check(rollers>0&&downhill==rollers,$"Bales: on the 30% bank every round bale's axis points downhill, so it can't roll ({downhill} of {rollers})");
            Check(layouts.SelectMany(l=>l.placed).Select(o=>o.variant).Distinct().Count()==family.Count,"Bales: every variant is used on the range");
            int placed=layouts.Sum(l=>l.placed.Count);double ms=layouts.Sum(l=>l.solveMs);
            checks.Add($"Bales: {placed} bales placed in {ms:0.0} ms ({ms/placed:0.00} ms each)");

            // step 5: the jerry can, one library object with two users: the range's quay, and the Cob (prop:jerry_can@1)
            PropKit.Forget();var jk=PropKit.Load("jerry_can");var can=jk.Mesh("jerry_can").bounds.size;
            Check(jk.file.kind=="object"&&jk.file.pieces.Length==1&&Mathf.Abs(can.y-.47f)<.01f,$"Library: the jerry can reads as an object, 0.47 m tall ({can.x:0.000} x {can.y:0.000} x {can.z:0.000} m)");
            var canSpec=RunRange.Bales().First(b=>b.kit=="jerry_can");var quay=Objects.Place(canSpec.name,canSpec.kit,canSpec.spots,canSpec.name);
            Check(quay.placed.Count==6&&quay.failures.Count==0,$"Library: six cans stand on the range's quay, level with the ground{(quay.failures.Count>0?": "+string.Join("; ",quay.failures):"")}");
            // (the Cob carried the can until the roster was cleared, 24 Oct; the car side runs again once a car carries one)
            var cob=Resources.Load<GameObject>("Cars/forge_cob");if(cob==null)checks.Add("Library: no installed car carries the can since 24 Oct (Car Forge's own tests check a car's can)");
            else {
            var mf=cob.GetComponentsInChildren<MeshFilter>(true).First(f=>f.name.StartsWith("Body"));var mesh=mf.sharedMesh;var mats=mf.GetComponent<MeshRenderer>().sharedMaterials;
            var v=mesh.vertices;float reach=v.Max(p=>Mathf.Abs(p.z));var pts=new List<Vector3>();   // (at either end, red: the calipers sit at the axles, well inside)
            for(int sm=0;sm<mesh.subMeshCount;sm++){if(sm>=mats.Length||mats[sm]==null||!mats[sm].name.StartsWith("CaliperRed"))continue;
                foreach(int i in mesh.GetTriangles(sm))if(Mathf.Abs(v[i].z)>reach-.45f&&v[i].y>.2f)pts.Add(v[i]);}
            var b2=new Bounds(pts.Count>0?pts[0]:Vector3.zero,Vector3.zero);foreach(var q in pts)b2.Encapsulate(q);var onCar=Vector3.Scale(b2.size,mf.transform.lossyScale);
            // (the red paint against the red paint: the handles and spout are bare steel, BrakeMetal on the car)
            var jm=jk.Mesh("jerry_can");var subs=jk.Piece("jerry_can").lods[0].submeshes;int red=Array.FindIndex(subs,x=>x.material=="red_paint");var jv=jm.vertices;
            var b3=new Bounds(jv[jm.GetTriangles(red)[0]],Vector3.zero);foreach(int i in jm.GetTriangles(red))b3.Encapsulate(jv[i]);var painted=b3.size;
            Check(pts.Count>0&&Mathf.Abs(onCar.x-painted.x)<.01f&&Mathf.Abs(onCar.y-painted.y)<.01f&&Mathf.Abs(onCar.z-painted.z)<.01f,
                $"Library: the can on the Cob's tail is the can on the quay, at world size (its paint {onCar.x:0.000} x {onCar.y:0.000} x {onCar.z:0.000} m on the car, {painted.x:0.000} x {painted.y:0.000} x {painted.z:0.000} on the quay)"+
                (pts.Count>0?"":$" [no can found: {mf.name} bounds {mesh.bounds}, materials {string.Join(",",mats.Select(m=>m==null?"-":m.name))}, scale {mf.transform.lossyScale}]"));
            }
        } finally { Ground.Reset(); }
        return checks;
    }

    // The wall-slide test (docs/77, "snags along walls"): a car-sized box, held level and on its heading, slides at 22 m/s
    // into a dry-stone wall at 8 deg, along a straight of many pieces and into a 40 m bend that turns across its path (it stops
    // short of the end pier). With no friction, a smooth wall keeps its speed; a seam or a step shows as a sudden loss in one step.
    sealed class Deep:Ground.IProvider{public float Height(float x,float z)=>-2000;}
    static string Slide(Action<bool,string> Check)
    {
        var mode=Physics.simulationMode;var objects=new List<UnityEngine.Object>();
        Ground.Use(new Deep());
        try {
            var line=new List<Vector3>();for(float z=0;z<=30;z+=2)line.Add(new Vector3(0,0,z));
            for(int i=1;i<=12;i++){float a=i*2.5f*Mathf.Deg2Rad;line.Add(new Vector3(-40+40*Mathf.Cos(a),0,30+40*Mathf.Sin(a)));}
            var L=Runs.Solve(new RunSpec{name="slide",kit="dry_stone",seed="slide",along=line.ToArray(),offset=0});
            var root=new GameObject("Wall slide test");objects.Add(root);Runs.Colliders(L,root.transform);
            var slick=new PhysicsMaterial("slick"){dynamicFriction=0,staticFriction=0,frictionCombine=PhysicsMaterialCombine.Minimum,bounciness=0,bounceCombine=PhysicsMaterialCombine.Minimum};objects.Add(slick);
            var car=new GameObject("Wall slide car");objects.Add(car);var box=car.AddComponent<BoxCollider>();box.size=new Vector3(1.8f,1f,4.2f);box.sharedMaterial=slick;
            var body=car.AddComponent<Rigidbody>();body.mass=1200;body.useGravity=false;body.constraints=RigidbodyConstraints.FreezePositionY|RigidbodyConstraints.FreezeRotation;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            car.transform.SetPositionAndRotation(new Vector3(-L.kit.file.run.halfBase-1.6f,-2000+.6f,3),Quaternion.Euler(0,8,0));
            Physics.simulationMode=SimulationMode.Script;Physics.SyncTransforms();body.linearVelocity=car.transform.forward*22;
            float before=22,contact=-1,worstDrop=0,lastZ=0;bool touched=false;
            for(int k=0;k<180;k++) {
                Physics.Simulate(.01f);float v=body.linearVelocity.magnitude;
                if(!touched&&v<before-.05f){touched=true;contact=v;before=v;continue;}
                if(touched)worstDrop=Mathf.Max(worstDrop,(before-v)/before);before=v;lastZ=car.transform.position.z;
            }
            float kept=touched?before/contact:0;
            Check(touched&&worstDrop<.02f&&kept>.85f,$"Walls: slide: a car scraping {lastZ:0} m along a wall (a straight into a 40 m bend, one merged collider) keeps {kept*100:0}% of its speed, never losing more than {worstDrop*100:0.0}% in one step (snag: 2%)");
            return $"Walls: slide test: speed after first contact {contact:0.0} m/s, at the end {before:0.0} m/s";
        } finally {
            Physics.simulationMode=mode;Ground.Reset();
            foreach(var o in objects)if(o!=null)UnityEngine.Object.DestroyImmediate(o);
        }
    }
}
