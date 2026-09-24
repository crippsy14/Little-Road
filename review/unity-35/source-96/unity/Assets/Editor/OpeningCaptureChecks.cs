using System;
using System.Collections.Generic;
using LittleRoad;

public static class OpeningCaptureChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string message){if(!ok)throw new Exception("Opening capture: "+message);checks.Add(message);}
        var window=new OpeningCaptureWindow();
        Check(!window.Capture(1,1,20,false)&&!window.Capture(0,2,0,false),"Other cars and stationary countdown do not consume the window");
        Check(window.Capture(0,5,2,false),"First moving own observation starts capture");
        Check(!window.Capture(0,5.1,20,false)&&window.Capture(0,5.25,0,false),"Cadence is bounded and subsequent stops remain visible");
        Check(!window.Capture(0,5.5,20,true)&&window.Capture(0,5.5,20,false),"Finished observations do not consume a sample");
        Check(window.Capture(0,45,20,false)&&!window.Capture(0,45.3,20,false),"Forty-second window expires without restarting");
        var dense=new OpeningCaptureWindow();int count=0;
        for(int i=0;i<10000;i++)if(dense.Capture(0,i*.01,10,false))count++;
        Check(count<=161&&count>120,"Dense decisions remain below the192-record diagnostic capacity");
        return checks;
    }
}
