using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Authored identity is independent of equipment, body art and race result.
    public sealed class DriverIdentity
    {
        [Serializable] public sealed class Definition
        {
            public string id,name,style;
            public float braking,consistency;
            public int risk;
        }
        [Serializable] public sealed class Data {public int schema;public Definition[] drivers;}
        [Serializable] public sealed class Record
        {
            public int entrant,seed,risk;public string id,name,style;
            public float baseBraking,consistency,formAmplitude,formSample,effectiveBraking,targetAbility=-1;
            public bool variation;
        }
        static Data catalog;
        public readonly string Id,Name,Style;
        public readonly float BaseBraking,Consistency,FormAmplitude,FormSample;
        public readonly int Seed;
        public readonly bool Variation;
        // A field recipe's driver rating (FieldRecipe): the mean of the five racing abilities this driver is set to,
        // keeping the style's shape; negative for none.
        public readonly float TargetAbility;
        public readonly DriverPlanningSettings Planning;
        public DriverIdentity(Definition definition,int raceSeed,bool variation,float targetAbility=-1)
        {
            TargetAbility=targetAbility<0?-1:Mathf.Clamp01(targetAbility);
            Validate(definition);
            Id=definition.id;Name=definition.name;Style=definition.style;BaseBraking=definition.braking;Consistency=definition.consistency;Seed=raceSeed;Variation=variation;
            // Symmetric support stays inside existing judgement limits without
            // clipping/skew. Benchmark at .75 has zero form amplitude explicitly.
            FormAmplitude=variation?Mathf.Min(.03f*(1-Consistency),Mathf.Min(BaseBraking-.55f,.75f-BaseBraking)):0;
            FormSample=FormAmplitude==0?0:SignedSample(Id,raceSeed,"race-form-v1");
            Planning=new DriverPlanningSettings(BaseBraking+FormAmplitude*FormSample,definition.risk,true);
        }
        public Record Export(int entrant)=>new Record{entrant=entrant,seed=Seed,risk=Planning.Risk,id=Id,name=Name,style=Style,baseBraking=BaseBraking,consistency=Consistency,formAmplitude=FormAmplitude,formSample=FormSample,effectiveBraking=Planning.BrakeFraction,variation=Variation,targetAbility=TargetAbility};
        public static float SignedSample(string id,int seed,string channel)
        {
            // FNV-1a over UTF-16 code units, explicit byte order, then avalanche.
            // No process-dependent string hash, shared stream or entrant index.
            unchecked {
                uint hash=2166136261;
                foreach(char c in id){hash=(hash^(byte)c)*16777619;hash=(hash^(byte)(c>>8))*16777619;}
                hash=(hash^0xff)*16777619;
                foreach(char c in channel){hash=(hash^(byte)c)*16777619;hash=(hash^(byte)(c>>8))*16777619;}
                hash^=(uint)seed;hash^=hash>>16;hash*=0x7feb352d;hash^=hash>>15;hash*=0x846ca68b;hash^=hash>>16;
                return (hash>>8)*(2f/16777215f)-1f;
            }
        }
        public static void Validate(Definition d)
        {
            if(d==null||string.IsNullOrWhiteSpace(d.id)||string.IsNullOrWhiteSpace(d.name)||string.IsNullOrWhiteSpace(d.style)||!float.IsFinite(d.consistency)||d.consistency<0||d.consistency>1)
                throw new ArgumentException("Invalid driver identity.");
            _=new DriverPlanningSettings(d.braking,d.risk,true);
        }
        public static void Validate(Data data)
        {
            if(data==null||data.schema!=1||data.drivers==null||data.drivers.Length!=24)throw new ArgumentException("Driver roster requires24 valid identities.");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var d in data.drivers){Validate(d);if(!ids.Add(d.id))throw new ArgumentException("Duplicate driver identity: "+d.id);}
        }
        public static Definition At(int entrant)
        {
            if(catalog==null){var asset=Resources.Load<TextAsset>("Data/drivers");if(asset==null)throw new InvalidOperationException("Missing driver roster");var data=JsonUtility.FromJson<Data>(asset.text);Validate(data);catalog=data;}
            if(entrant<0||entrant>=catalog.drivers.Length)throw new ArgumentOutOfRangeException(nameof(entrant));
            // Return a copy; neither preview nor tests can mutate the catalogue.
            var d=catalog.drivers[entrant];return new Definition{id=d.id,name=d.name,style=d.style,braking=d.braking,risk=d.risk,consistency=d.consistency};
        }
        public static DriverIdentity ForRace(int entrant,int seed,string mode,bool variation,float targetAbility=-1)
        {
            var d=At(entrant);
            if(mode!="mixed") {
                d.style=mode;
                switch(mode) {
                    case "neutral":d.braking=.75f;d.risk=5;d.consistency=1;break;
                    case "cautious":d.braking=.73f;d.risk=0;d.consistency=1;break;
                    case "assertive":d.braking=.73f;d.risk=5;d.consistency=1;break;
                    case "daring":d.braking=.73f;d.risk=10;d.consistency=1;break;
                    case "early":d.braking=.65f;d.risk=5;d.consistency=1;break;
                    case "steady":d.braking=.72f;d.risk=5;d.consistency=.9f;break;
                    case "variable":d.braking=.72f;d.risk=5;d.consistency=.2f;break;
                    default:throw new ArgumentException("Unknown driver roster: "+mode);
                }
            }
            return new DriverIdentity(d,seed,variation,targetAbility);
        }
    }
}
