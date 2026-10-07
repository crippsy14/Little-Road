using System;
using System.Collections.Generic;
using LittleRoad;

// The challenge rules (doc 64): Farmer Giles's two farm challenges and the shared activity description, so a change
// that breaks a star ladder or a save id fails the build.
public static class ActivityChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        foreach(FarmChallenges.Kind k in Enum.GetValues(typeof(FarmChallenges.Kind))) {
            var m=FarmChallenges.Marks(k);
            Check(m.Length==3&&m[0]>0&&m[0]<m[1]&&m[1]<m[2]&&m[2]<=1,$"{FarmChallenges.Name(k)}: three rising star marks within 0-100% ({m[0]*100:0}/{m[1]*100:0}/{m[2]*100:0}%)");
            Check(FarmChallenges.Limit(k)>=45&&FarmChallenges.Limit(k)<=150,$"{FarmChallenges.Name(k)}: {FarmChallenges.Limit(k):0} s on the clock");
            Check(FarmChallenges.Known(FarmChallenges.Ids[(int)k]),$"{FarmChallenges.Name(k)}'s record id passes save validation");
        }
        Check(FarmChallenges.Prizes.Length==3&&FarmChallenges.Prizes[0]>0&&FarmChallenges.Prizes[0]<FarmChallenges.Prizes[1]&&FarmChallenges.Prizes[1]<FarmChallenges.Prizes[2],"Farm stars pay more for each higher star");
        var a=new Activity{marks=new[]{.3f,.5f,.7f},progress=()=>.55f};
        Check(a.Stars==2,"An activity at 55% with marks 30/50/70% has two stars");
        Check(FarmChallenges.Countdown>=3,"Farm challenges count down at least 3 s (the race-start 3-2-1)");
        return checks;
    }
}
