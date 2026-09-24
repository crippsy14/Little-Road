using System;
using System.Collections.Generic;
using LittleRoad;

public static class CoastingEnvelopeChecks
{
    static void Require(bool value,string message){if(!value)throw new Exception("Coasting envelope: "+message);}
    public static List<string> Run()
    {
        var distances=new[]{0f,3f,6f,9f,12f};
        Require(CoastingEnvelope.Allows(20,1,25,distances,new[]{23f,22f,21f,20f,19f},5),"coasting below a reachable braking envelope rejected");
        Require(!CoastingEnvelope.Allows(20,1,25,distances,new[]{21f,18f,15f,15f,15f},5),"required corner braking removed");
        Require(!CoastingEnvelope.Allows(20,1,19,distances,new[]{25f,25f,25f,25f,25f},5),"current steering speed limit ignored");
        Require(!CoastingEnvelope.Allows(20,1,25,distances,new[]{19f,25f,25f,25f,25f},5),"current envelope overspeed ignored");
        Require(!CoastingEnvelope.Allows(20,1,25,new[]{0f,1f,2f},new[]{25f,25f,25f},3),"insufficient preview treated as clear");
        // A short dip between endpoints cannot be skipped by a long step.
        Require(!CoastingEnvelope.Allows(20,1,25,new[]{0f,.2f,.4f,12f},new[]{25f,10f,25f,25f},4),"interior speed restriction skipped");
        return new List<string>{"Coast release preserves reachable envelope; required corner braking retained",
            "Coast requires present steering and speed limits and sufficient preview",
            "Coast checks interior speed restrictions, not only interval endpoints"};
    }
}
