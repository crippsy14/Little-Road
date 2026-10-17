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
        foreach(var id in roster){float top=CarBodies.Forged(id).top;Check(top>.9f&&top<2.6f,$"Forge roster: {id}'s collision box stops at its own top ({top:0.00} m), props left out");}
        for(int stars=1;stars<=10;stars++)
            foreach(bool gravel in new[]{false,true}) {
                var (quick,home,vet)=CarBodies.RosterPools(stars,gravel);
                Check(quick.Length>0&&home.Length>0&&vet.Length>0,$"Forge roster: {stars} stars on {(gravel?"gravel":"tarmac")} has quick, home and veteran cars ({quick.Length}/{home.Length}/{vet.Length})");
                Check(quick.Concat(home).Concat(vet).All(id=>CarBodies.RacesOn(CarBodies.Forged(id),gravel)),$"Forge roster: {stars} stars on {(gravel?"gravel":"tarmac")} takes only {(gravel?"gravel":"tarmac")} cars");
                // (the field itself, over several race seeds: the design's caps, docs/roster-c-design.md in Car Forge)
                for(int seed=1;seed<=12;seed++) {
                    bool topC=CarBodies.TopCOpen("C",stars);var field=RaceRoster.ForgeField(stars,gravel,topC,seed*7919);
                    string where=$"{stars} stars on {(gravel?"gravel":"tarmac")}, seed {seed}";
                    Check(field.Length==23&&field.All(id=>id!=null&&CarBodies.IsKnown(id)),"Forge roster field: 23 known rival bodies ("+where+")");
                    Check(field.GroupBy(id=>id).All(g=>g.Count()<=2),"Forge roster field: no model races more than twice ("+where+")");
                    Check(Enumerable.Range(1,22).All(i=>field[i]!=field[i-1]),"Forge roster field: no model in the slot behind itself ("+where+")");
                    Check(field.Distinct().Count()>=Math.Min(12,quick.Concat(home).Concat(vet).Distinct().Count()),"Forge roster field: at least 12 different models when the pools hold them ("+where+")");
                    Check(field.Take(7).Count(id=>quick.Contains(id))>=Math.Min(quick.Distinct().Count(),4)-(topC&&!gravel?1:0),"Forge roster field: the quick-looking cars start at the front ("+where+")");
                    if(topC&&!gravel)Check(field[0]==CarBodies.RosterFlagship,"Forge roster field: the Zenith on pole ("+where+")");
                }
            }
        // Class B (docs/roster-b-design.md in Car Forge): its own roster, every star of the class (1-8 and the 10-star
        // championship) on both surfaces, the same field rules, no class C car in a class B field and none the other way.
        var rosterB=CarBodies.RosterOf("B");
        Check(rosterB.Length>=40&&rosterB.All(id=>CarBodies.Class(id)=="B"),"Forge roster: the class B roster is installed ("+rosterB.Length+" cars, all class B)");
        Check(roster.All(id=>CarBodies.Class(id)=="C"),"Forge roster: the class C roster holds class C cars only");
        foreach(int stars in new[]{1,2,3,4,5,6,7,8,10})
            foreach(bool gravel in new[]{false,true})
                for(int seed=1;seed<=6;seed++) {
                    var field=RaceRoster.ForgeField(stars,gravel,false,seed*7919,"B");string where=$"class B {stars} stars on {(gravel?"gravel":"tarmac")}, seed {seed}";
                    Check(field.Length==23&&field.All(id=>id!=null&&CarBodies.Class(id)=="B"&&CarBodies.RacesOn(CarBodies.Forged(id),gravel)),"Forge roster field: 23 class B bodies for the surface ("+where+")");
                    Check(field.GroupBy(id=>id).All(g=>g.Count()<=2)&&Enumerable.Range(1,22).All(i=>field[i]!=field[i-1]),"Forge roster field: at most two of a model, never behind itself ("+where+")");
                }
        var (q1,h1,_)=CarBodies.RosterPools(1,false);
        Check(h1.All(id=>CarBodies.Forged(id).band[0]<=1),"Forge roster: a 1-star field's home cars all have 1 star in their band");
        Check(Array.IndexOf(roster,CarBodies.RosterFlagship)>=0,"Forge roster: the flagship (the Zenith) is installed");
        return checks;
    }
}
