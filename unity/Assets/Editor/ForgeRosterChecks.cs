using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;

// --forge-roster (CarBodies.RosterPools, RaceRoster.Recipe): class C fields drawn from the Car Forge roster by star band.
// Every star of the ladder, on tarmac and on gravel, has cars in every pool; the pools respect band and surface; the flag
// is off by default and the roster's bodies race in the standard envelope.
public static class ForgeRosterChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        Check(!CarBodies.RosterEnabled,"Forge roster is off without --forge-roster");
        var roster=CarBodies.Roster;
        Check(roster.Length>=40,"Forge roster: the class C roster is installed ("+roster.Length+" cars with a star band)");
        foreach(var id in roster)Check(CarBodies.IsKnown(id)&&CarBodies.UsesStandardEnvelope(id),"Forge roster: "+id+" is a known body in the standard envelope");
        for(int stars=1;stars<=10;stars++)
            foreach(bool gravel in new[]{false,true}) {
                var (quick,home,vet)=CarBodies.RosterPools(stars,gravel);
                Check(quick.Length>0&&home.Length>0&&vet.Length>0,$"Forge roster: {stars} stars on {(gravel?"gravel":"tarmac")} has quick, home and veteran cars ({quick.Length}/{home.Length}/{vet.Length})");
                Check(quick.Concat(home).Concat(vet).All(id=>(CarBodies.Forged(id).surface=="gravel")==gravel),$"Forge roster: {stars} stars on {(gravel?"gravel":"tarmac")} takes only {(gravel?"gravel":"tarmac")} cars");
            }
        var (q1,h1,_)=CarBodies.RosterPools(1,false);
        Check(h1.All(id=>CarBodies.Forged(id).band[0]<=1),"Forge roster: a 1-star field's home cars all have 1 star in their band");
        Check(Array.IndexOf(roster,CarBodies.RosterFlagship)>=0,"Forge roster: the flagship (the Zenith) is installed");
        return checks;
    }
}
