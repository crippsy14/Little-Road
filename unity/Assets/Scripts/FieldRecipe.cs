using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LittleRoad
{
    // Race fields by recipe (docs/70; Richard, 7 Oct). The default since 7 Oct ("make the recipe default");
    // --no-field-recipe races the old fields for comparison. One rule for every class C and B race on
    // the planned map, from three things: the race's class, its stars (1-8, 10 for the championship) and its surface.
    //   Drivers: skill (DR, 1-10; Richard, 7 Oct) C 2-3, B 4-5, A 6-7, S 8-9, E 9-10, the same everywhere in a class.
    //   The best drivers start at or near the front, with a few good ones in the middle and at the back.
    //   Parts: each slot's ladder (worst first), cut to the parts you could have by then, the ceiling. The floor rises
    //   with the stars, so a 1-star field runs from the bottom to the top of what's open, and the hardest fields run
    //   near the top. Class C opens by discovery: Bayview's garage from the start, Hillcrest's valley parts from 2
    //   stars, the Quarry Hills gravel kit from 3. Class B opens by stars won (stars:B:N in parts.json), taking the
    //   stars you'd have won before a race as all the easier races on the map (docs/data/race-stars.json). Class B's
    //   bottom rung is the best class C part, so Bayview's first B race mixes the best of the garage with Good.
    // A car's kit and its driver are rolled separately, both biased to the front of the grid.
    public static class FieldRecipe
    {
        public static bool On=>!RaceTrial.Has("--no-field-recipe");
        public static (float lo,float hi) DriverBand(string raceClass)=>raceClass switch{"B"=>(4f,5f),"A"=>(6f,7f),"S"=>(8f,9f),"E"=>(9f,10f),_=>(2f,3f)};
        // Gravel tracks (docs/data/race-stars.json "disc"); built so far: Quarry Hills.
        public static string Discipline(string track)=>track=="quarry"?"gravel":"road";

        // 0 at the back of the grid, 1 on pole, with seeded spread: grid order counts for 70%. Three wildcards per
        // race (good drivers starting mid-pack or at the back) get at least 0.8 in the driver roll.
        public static float Roll(int slot,int seed,string channel,bool wildcards)
        {
            float grid=1-Mathf.Clamp01(slot/22f),luck=DriverIdentity.SignedSample("slot-"+slot,seed,channel)*.5f+.5f;
            float q=Mathf.Clamp01(.7f*grid+.3f*luck);
            if(wildcards&&slot>=8&&Wildcard(slot,seed))q=Mathf.Max(q,.8f+.2f*luck);
            return q;
        }
        static bool Wildcard(int slot,int seed)=>Enumerable.Range(8,15).OrderByDescending(s=>DriverIdentity.SignedSample("wild-"+s,seed,"field-wildcard")).Take(3).Contains(slot);
        public static float TargetRating(string raceClass,int slot,int seed){var (lo,hi)=DriverBand(raceClass);return lo+(hi-lo)*Roll(slot,seed,"field-driver",true);}
        // The ability mean that shows as that skill.
        public static float TargetAbility(float rating)=>RacerAbilities.SkillAbility(rating);
        public static DriverIdentity Driver(int entrant,int seed,string raceClass,int slot,bool form)=>
            DriverIdentity.ForRace(entrant,seed,"mixed",form,TargetAbility(TargetRating(raceClass,slot,seed)));

        // Class B stars you'd have won before a race of each star level (index = stars): every easier race on the
        // planned map (docs/data/race-stars.json; FieldRecipeChecks keeps them in step).
        public static readonly int[] BStarsBefore={0,0,2,8,23,35,50,68,96,120,120};

        static bool GravelTyre(EquipmentCatalog.Part p)=>p.slot=="tyres"&&p.dirt>p.road;
        static IEnumerable<EquipmentCatalog.Part> Tier(string tier,string slot)=>EquipmentCatalog.All.parts.Where(p=>p.slot==slot&&(string.IsNullOrEmpty(p.tier)?"C":p.tier)==tier);
        public static int UnlockStars(EquipmentCatalog.Part p)=>p.unlock!=null&&p.unlock.StartsWith("stars:")?int.Parse(p.unlock.Split(':')[2]):0;
        // Each slot's whole ladder for a class and surface, worst first. Ice tyres are planned (the parts shop's
        // "Ice · soon"): ice races use the road ladder until they exist.
        public static string[] Ladder(string raceClass,string disc,string slot)
        {
            bool gravel=disc=="gravel";
            if(raceClass=="C") {
                var parts=Tier("C",slot).Where(p=>slot=="tyres"?GravelTyre(p)==gravel:p.shop!="quarry"||gravel);
                return (slot=="tyres"?parts.OrderBy(p=>gravel?p.dirt:p.road):parts.OrderBy(p=>p.price)).Select(p=>p.id).ToArray();
            }
            // Tyres never drop to class C on a B car: a C tyre is the biggest single loss (road grip 10.2 against 14.5)
            // and, mixed into a B field, spread Bayview's first B race enough for the leaders to lap the back of the
            // field in three laps (docs/70). The gravel field runs the B rally tyre.
            if(slot=="tyres")return Tier("B",slot).Where(p=>gravel?p.id=="tyres_b_rally":p.id!="tyres_b_rally").OrderBy(UnlockStars).Select(p=>p.id).ToArray();
            return new[]{Ladder("C",disc,slot).Last()}.Concat(EquipmentCatalog.TunerSteps[slot]).ToArray();
        }
        // Open by a race of `stars` (class C by discovery, class B by stars won before it).
        public static bool Open(string raceClass,int stars,EquipmentCatalog.Part p)
        {
            if(raceClass=="B")return RaceCalendar.Rank(p.tier)==0||UnlockStars(p)<=BStarsBefore[Mathf.Clamp(stars,0,10)];
            return p.motorKw>0?false:p.shop=="hillcrest"?stars>=2:p.shop=="quarry"?stars>=3:string.IsNullOrEmpty(p.unlock);   // (the hybrid prototype: not in fields; --hybrid-everyone tests it)
        }
        // (A ladder's first rung is always open, so every slot has a part: the B rally tyre is the gravel field's tyre
        // from the first B race, as it was before recipes.)
        public static string[] Available(string raceClass,string disc,int stars,string slot)=>Ladder(raceClass,disc,slot).Where((id,i)=>i==0||Open(raceClass,stars,EquipmentCatalog.Get(id))).ToArray();
        // The floor's share of the open ladder: 0 at 1 star, 0.7 at 8, 0.9 at 10.
        public static float FloorShare(int stars)=>Mathf.Clamp(.1f*(stars-1),0,.9f);
        // The window in rungs of the open ladder. The floor is continuous (FloorAt), so short ladders still rise a little at
        // every star level; `floor` is the lowest rung a car can get.
        public static float FloorAt(string raceClass,string disc,int stars,string slot)=>FloorShare(stars)*(Available(raceClass,disc,stars,slot).Length-1);
        public static (int floor,int ceiling) Window(string raceClass,string disc,int stars,string slot)
        {
            int top=Available(raceClass,disc,stars,slot).Length-1;return (Mathf.FloorToInt(FloorAt(raceClass,disc,stars,slot)+.0001f),top);
        }
        // A car's kit: the kit roll picks a rung in every slot's window, each slot nudged by its own draw (up to a rung).
        public static Dictionary<string,string> Kit(string raceClass,string disc,int stars,int slot,int seed,out float level)
        {
            level=Roll(slot,seed,"field-kit",false);var fit=EquipmentCatalog.StockLoadout();
            foreach(string s in EquipmentCatalog.All.slots) {
                var open=Available(raceClass,disc,stars,s);var (lo,hi)=Window(raceClass,disc,stars,s);float floor=FloorAt(raceClass,disc,stars,s);
                float nudge=DriverIdentity.SignedSample(s+"-"+slot,seed,"field-part")*.6f;
                fit[s]=open[Mathf.Clamp(Mathf.RoundToInt(floor+level*(hi-floor)+nudge),lo,hi)];
            }
            return fit;
        }
    }
}
