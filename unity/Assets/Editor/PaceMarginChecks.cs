using System;
using System.Collections.Generic;
using LittleRoad;

// ai-racing-review goal 3: the AI's grip/braking use lives in PaceMargins.
// Without --pace-margins the values must stay the historical constants.
public static class PaceMarginChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string message){if(!ok)throw new Exception("Pace margins: "+message);checks.Add(message);}
        Check(PaceMargins.Corner==.78f&&PaceMargins.Steering==.85f&&PaceMargins.ProfileBrake==.85f&&PaceMargins.EnvelopeBrakeScale==1f,"Default grip and braking use equals the historical 0.78 / 0.85 / 0.85 / 1.0");
        Check(PaceMargins.Corner<PaceMargins.Steering&&PaceMargins.Steering<1&&PaceMargins.ProfileBrake<1,"Planned grip use stays below the physical limit, with the steering cap above the planned corner speed");
        return checks;
    }
}
