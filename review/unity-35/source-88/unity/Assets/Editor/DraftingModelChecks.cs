using System;
using System.Collections.Generic;
using System.IO;
using LittleRoad;
using UnityEngine;

public static class DraftingModelChecks
{
    static ObservedCar Car(int id,float z,float side=0,float speed=30,float height=0,bool reverse=false)
        =>new ObservedCar(id,new Vector3(side,height,z),Vector3.forward*speed,reverse?Vector3.back:Vector3.forward,
            reverse?Vector3.left:Vector3.right,z,side,speed,1.2f,2.3f);
    static void Require(bool ok,string why){if(!ok)throw new Exception("Drafting model: "+why);}
    public static List<string> Run()
    {
        var checks=new List<string>();var own=Car(0,0);var lead=Car(1,9);
        float full=DraftingModel.Reduction(in own,new[]{own,lead},out int leader);
        Require(full==DraftingModel.MaximumReduction&&leader==1,"aligned close wake missing");
        checks.Add("Aligned high-speed following reaches the8% aerodynamic-reduction cap");
        foreach(var other in new[]{Car(1,-10),Car(1,3),Car(1,50),Car(1,9,4),Car(1,9,0,30,2),Car(1,9,0,30,0,true),Car(1,9,0,0)}) {
            Require(DraftingModel.Reduction(in own,new[]{own,other},out leader)==0&&leader==-1,"invalid wake grants assistance");
        }
        checks.Add("Behind, overlap, distant, side-by-side, vertically separate, opposing and stopped sources give no tow");
        var slow=Car(0,0,0,12);Require(DraftingModel.Reduction(in slow,new[]{lead},out _) ==0,"low-speed assistance");
        Require(DraftingModel.Reduction(in own,new[]{own},out _) ==0,"self tow");
        checks.Add("No self drafting or low-speed launch boost");
        var third=Car(2,9);
        Require(DraftingModel.Reduction(in own,new[]{third,lead},out leader)==full&&leader==1,"stacked/order-dependent draft");
        Require(DraftingModel.Reduction(in own,new[]{lead,third},out leader)==full&&leader==1,"reordered tie differs");
        checks.Add("Several wakes never stack and source ties use stable driver identity");
        float previous=full;
        for(int i=1;i<=90;i++) {
            var displaced=Car(1,9,i*.02f);float reduction=DraftingModel.Reduction(in own,new[]{displaced},out _);
            Require(reduction>=0&&reduction<=previous&&previous-reduction<.002f,"lateral exit is discontinuous/nonmonotonic");previous=reduction;
        }
        Require(previous==0,"tow persists outside wake");checks.Add("Lateral pull-out smoothly loses the aerodynamic benefit");
        previous=full;
        for(int i=1;i<=150;i++) {
            var distant=Car(1,9.6f+i*.2f);float reduction=DraftingModel.Reduction(in own,new[]{distant},out _);
            Require(reduction>=0&&reduction<=previous&&previous-reduction<.002f,"longitudinal exit is discontinuous/nonmonotonic");previous=reduction;
        }
        Require(previous==0,"far tow persists");checks.Add("Increasing bumper gap smoothly loses the benefit by35m");
        foreach(float mass in new[]{.8f,1f,1.4f})foreach(float speed in new[]{-7f,0f,12f,20f,30f,45f}) {
            Require(DraftingModel.Resistance(speed,mass,0)==DriveModel.Resistance(speed,mass),"zero draft changed baseline drag");
            float resistance=DraftingModel.Resistance(speed,mass,full);
            Require(resistance>=.25f/mass&&resistance<=DriveModel.Resistance(speed,mass),"draft alters rolling loss or adds force");
        }
        checks.Add("Zero effect preserves exact resistance and positive effect reduces only aerodynamic loss");
        var traffic=new[]{own,lead};DraftingModel.Reduction(in own,traffic,out _);
        long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)DraftingModel.Reduction(in own,traffic,out _);
        Require(GC.GetAllocatedBytesForCurrentThread()==bytes,"per-evaluation managed allocation");
        checks.Add("Wake evaluation has zero warm managed allocation");
        return checks;
    }
    [Serializable] public sealed class Evidence{public string scope="Pure drafting kernel only. Not enabled in live cars, controller forecasts or safety yet; no gameplay or pace acceptance claimed.";public string[] checks;public float maximumReduction;}
    public static void Review()
    {
        var checks=Run();string output=RaceTrial.Arg("--drafting-review-output");
        if(output==null)throw new ArgumentException("Output required.");
        Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(new Evidence{checks=checks.ToArray(),maximumReduction=DraftingModel.MaximumReduction},true));
        Debug.Log("DRAFTING_MODEL_CHECKS passed="+checks.Count);
    }
}
