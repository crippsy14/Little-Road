using System;
using System.Collections.Generic;
using LittleRoad;

public static class FollowingFallbackSelectionChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string text){if(!ok)throw new Exception("Following scope: "+text);checks.Add(text);}
        Check(FollowingFallbackSelection.Applies(FollowingFallbackSelection.Parse("all"),0)&&FollowingFallbackSelection.Applies(FollowingFallbackScope.All,23),"Normal enabled scope includes own car and last rival");
        Check(FollowingFallbackSelection.Applies(FollowingFallbackSelection.Parse("car0"),0)&&!FollowingFallbackSelection.Applies(FollowingFallbackScope.Car0,1),"Own-car component test excludes rivals");
        Check(!FollowingFallbackSelection.Applies(FollowingFallbackSelection.Parse("rivals"),0)&&FollowingFallbackSelection.Applies(FollowingFallbackScope.Rivals,23),"Rival component test excludes own car");
        Check(!FollowingFallbackSelection.Applies(FollowingFallbackScope.All,-1),"Invalid driver identity cannot receive the component");
        bool rejected=false;try{FollowingFallbackSelection.Parse("typo");}catch(ArgumentException){rejected=true;}
        Check(rejected,"Unknown component scope fails explicitly");
        return checks;
    }
}
