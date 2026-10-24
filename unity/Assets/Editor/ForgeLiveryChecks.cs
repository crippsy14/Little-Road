using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

// --livery (ForgeLivery): Car Forge bodies carry a Livery UV map into the shared atlas, and the composed livery lands
// where the atlas says: stripes over the top, a roundel on each door, paint everywhere else; without the flag nothing
// changes.
public static class ForgeLiveryChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        Color paint=new(.8f,.2f,.12f),stripe=Color.white;
        var tex=ForgeLivery.Compose(paint,stripe,7);
        Color At(Vector2 p)=>tex.GetPixel(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y));
        bool Near(Color a,Color b)=>Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)<.05f;
        var top=ForgeLivery.Top;int S=ForgeLivery.Size;
        float stripeB=.5f+.5f*(ForgeLivery.StripeIn+ForgeLivery.StripeOut)/2/(ForgeLivery.W/2);
        Check(Near(At(new Vector2(top.center.x*S,(top.yMin+top.height*stripeB)*S)),stripe)&&Near(At(top.center*S),paint),"Livery stripes run either side of the top's centre line");
        foreach(var row in new[]{ForgeLivery.Right,ForgeLivery.Left}) {
            var c=ForgeLivery.SidePixel(row,ForgeLivery.RoundelS,ForgeLivery.RoundelUp);float r=ForgeLivery.RoundelR*S*ForgeLivery.K;
            Check(Near(At(c+new Vector2(r*.85f,0)),stripe)&&Near(At(c+new Vector2(r*1.4f,0)),paint),"Livery roundel sits on each door, paint around it");
        }
        Check(ForgeLivery.Number("forge_closed_hatch")==ForgeLivery.Number("forge_closed_hatch")&&ForgeLivery.Number("x")>=1&&ForgeLivery.Number("x")<=99,"Livery numbers are stable, 1-99");
        UnityEngine.Object.DestroyImmediate(tex);
        // (the livery styles, 17 Oct: each lands where it should, and plain changes nothing)
        {
            var t=ForgeLivery.Compose(paint,stripe,7,"centre");Color A(Vector2 q)=>t.GetPixel(Mathf.RoundToInt(q.x),Mathf.RoundToInt(q.y));
            Check(Near(A(top.center*S),stripe),"Livery centre stripe runs down the top's centre line");UnityEngine.Object.DestroyImmediate(t);
            t=ForgeLivery.Compose(paint,stripe,7,"side");var r0=ForgeLivery.Right;
            Check(Near(A(ForgeLivery.SidePixel(r0,.15f,(ForgeLivery.SideLo+ForgeLivery.SideHi)/2)),stripe)&&Near(A(ForgeLivery.SidePixel(r0,.15f,.8f)),paint)&&Near(A(top.center*S),paint),"Livery side stripe runs along each flank");UnityEngine.Object.DestroyImmediate(t);
            t=ForgeLivery.Compose(paint,stripe,7,"chequer");float up=ForgeLivery.CheckLo+ForgeLivery.Check*.5f,sa=.15f,sb=sa+ForgeLivery.Check/ForgeLivery.L;
            Check(!Near(A(ForgeLivery.SidePixel(r0,sa,up)),A(ForgeLivery.SidePixel(r0,sb,up))),"Livery chequer alternates along each flank");UnityEngine.Object.DestroyImmediate(t);
            t=ForgeLivery.Compose(paint,stripe,7,"roundel");Check(Near(A(top.center*S),paint)&&Near(A(new Vector2(top.center.x*S,(top.yMin+top.height*stripeB)*S)),paint),"Livery roundel style: no stripes");UnityEngine.Object.DestroyImmediate(t);
            var probe=new GameObject("livery-probe");try{Check(Array.IndexOf(ForgeLivery.Styles,"plain")>=0&&ForgeLivery.Apply(probe,paint,stripe,7,"plain")==0,"Livery plain leaves the paint alone");}finally{UnityEngine.Object.DestroyImmediate(probe);}
        }
        // (--forge-drive: a Forge body as the player's car, in a blank save of its own; off, no save may hold one of the engine's
        //  test designs. The roster's cars are the body shops' range since 23 Oct, so a save may own them.)
        Check(AdventureSession.ForgeDrive!=null||!CarBodies.Ownable("forge_closed_hatch"),"The Forge engine's test designs can't be owned without --forge-drive");
        Check(CarBodies.Ownable("forge_snoozer")&&CarBodies.Ownable(CarBodies.ForgeStarter),"The Forge roster's cars can be owned (the body shops sell them)");
        Check(System.IO.Path.GetFileName(AdventureSession.ForgeDrivePath)=="little-road-forge-drive.json"&&AdventureSession.ForgeDrivePath!=SaveSlots.LivePath,"The Forge test drive saves to its own file, never a slot");
        // (paint types, 24 Oct: each body file names a known one or none, and they differ: a matte paint is duller than a
        //  solid one, a metallic one more metal, a faded one paler)
        {
            Color c0=new Color(.8f,.2f,.1f);float m0=.05f,s0=.56f;
            Color cm=c0;float mm=m0,sm=s0;TrialWorld.PaintFinish("matte",ref cm,ref mm,ref sm);
            Color cx=c0;float mx=m0,sx=s0;TrialWorld.PaintFinish("metallic",ref cx,ref mx,ref sx);
            Color cf=c0;float mf=m0,sf=s0;TrialWorld.PaintFinish("faded",ref cf,ref mf,ref sf);
            Color cs=c0;float ms=m0,ss=s0;TrialWorld.PaintFinish("solid",ref cs,ref ms,ref ss);
            Check(sm<s0&&mx>m0&&cf.grayscale>c0.grayscale&&cs==c0&&ms==m0&&ss==s0,"Paint types: matte duller, metallic more metal, faded paler, solid unchanged");
            foreach(var id in CarBodies.Forge){var f=CarBodies.Forged(id)?.finish;Check(f==null||Array.IndexOf(TrialWorld.PaintFinishes,f)>=0,$"Paint type of {id} is a known one ({f??"none"})");}
        }
        foreach(var id in CarBodies.Forge) {
            var model=Resources.Load<GameObject>("Cars/"+id);Check(model!=null,"Forge body model: "+id);
            int painted=0,outside=0;
            foreach(var mf in model.GetComponentsInChildren<MeshFilter>(true)){var mesh=mf.sharedMesh;var mats=mf.GetComponent<Renderer>().sharedMaterials;var uv=mesh.uv;
                for(int s=0;s<mesh.subMeshCount&&s<mats.Length;s++){if(mats[s]==null||!mats[s].name.Contains("BodyPaint"))continue;
                    foreach(int i in mesh.GetTriangles(s)){painted++;if(uv.Length==0||uv[i].x<-.001f||uv[i].x>1.001f||uv[i].y<-.001f||uv[i].y>1.001f)outside++;}}}
            Check(painted>0&&outside==0,"Livery UVs on every body-paint face, inside the atlas: "+id+" ("+painted+" corners)");
            var car=TrialWorld.Car(id,paint);
            try {
                foreach(var r in car.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)
                    if(m!=null&&m.name.Contains("BodyPaint")&&m.GetTexture("_BaseMap")!=null)throw new InvalidOperationException("Livery applied without --livery: "+id);
                int n=ForgeLivery.Apply(car,paint,stripe,ForgeLivery.Number(id));
                bool stripeKept=true;
                foreach(var r in car.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)
                    if(m!=null&&m.name.Contains("StripeColour")&&m.GetTexture("_BaseMap")!=null)stripeKept=false;
                Check(n>0&&stripeKept,"Livery goes on the body paint only (every LOD), never the second colour: "+id);
                // (the body's paint two-sided, so no strip of the shell is ever see-through)
                foreach(var r in car.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)
                    if(m!=null&&m.name.Contains("BodyPaint")&&m.name.Contains("_forge_"))Check(m.GetFloat("_Cull")==0,"Forge body paint is two-sided: "+id);
                // (lamp lenses: glass you see the lamp through; tail and indicator lenses tinted red and amber)
                foreach(var r in car.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials) {
                    if(m==null||!m.name.Contains("LampGlass"))continue;var c=m.color;
                    bool ok=c.a<.7f&&m.renderQueue>=3000&&(!m.name.Contains("TailLight")||(c.r>.5f&&c.g<.2f))&&(!m.name.Contains("Amber")||(c.r>.8f&&c.g>.25f&&c.b<.2f));
                    Check(ok,"Lamp lens is see-through glass, tinted where coloured: "+id+" "+m.name);
                }
            } finally {UnityEngine.Object.DestroyImmediate(car);}
        }
        return checks;
    }
}
