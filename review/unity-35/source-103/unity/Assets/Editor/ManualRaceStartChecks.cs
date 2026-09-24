using System;
using System.Collections.Generic;
using LittleRoad;

public static class ManualRaceStartChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool value,string name){if(!value)throw new Exception(name);checks.Add(name);}
        var manual=new ManualRaceStart(true);
        Check(manual.Waiting&&!manual.TryStart(false,true)&&manual.Waiting,"Manual countdown cannot start from input while unfocused");
        Check(!manual.TryStart(true,false)&&manual.Waiting,"Window focus alone cannot start the manual countdown");
        Check(manual.TryStart(true,true)&&!manual.Waiting,"Fresh focused confirmation releases the manual start exactly once");
        Check(!manual.TryStart(true,true)&&!manual.Waiting,"Repeated confirmation cannot restart a running trial");
        var automatic=new ManualRaceStart(false);
        Check(!automatic.Waiting&&!automatic.TryStart(true,true),"Automated race countdown does not acquire a readiness gate");
        return checks;
    }
}
