using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Gadgets (Gadgets.cs; docs/56 §19): the building blocks for jet packs, wings and the like.
public static class GadgetChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        Check(Gadgets.All.Count>0&&Gadgets.All.All(g=>Gadgets.Known(g.id)&&Gadgets.Create(g.id)!=null),$"Every gadget in the catalogue ({Gadgets.All.Count}) has a behaviour");
        Check(Gadgets.Known("")&&Gadgets.Known(null)&&!Gadgets.Known("rocket-shoes")&&Gadgets.Create(null)==null,"No gadget is valid (older saves); an unknown one isn't");
        var save=AdventureProgress.New();save.gadget="rocket-shoes";bool refused=false;try{save.Validate();}catch(System.IO.InvalidDataException){refused=true;}
        save.gadget=null;save.Validate();
        Check(refused,"A save naming an unknown gadget fails validation; one with none passes");
        Check(Controls.Of(Controls.Act.Gadget).padDefault=="rightShoulder"&&Controls.Of(Controls.Act.Gadget).keyDefault=="g","The gadget button defaults to R1 / RB and G, and is remappable");
        var host=new GameObject("Gadget check car");
        try {
            var car=host.AddComponent<RoadCar>();car.body=host.AddComponent<Rigidbody>();
            var hop=(HopGadget)Gadgets.Create("hop");hop.Fitted(car);
            float dt=1/120f;hop.Step(true,true,dt);
            for(int i=0;i<Mathf.CeilToInt(HopGadget.BurnTime/dt)+2;i++)hop.Step(true,false,dt);
            float spent=hop.Charge;hop.Step(true,true,dt);float stillSpent=hop.Charge;
            for(int i=0;i<Mathf.CeilToInt(HopGadget.Recharge/dt)+2;i++)hop.Step(false,false,dt);
            Check(spent<.05f&&stillSpent<.1f&&hop.Charge>=.999f,$"The test hop fires, empties its charge ({spent:0.00}), won't refire while recharging, and is ready again after {HopGadget.Recharge:0} s");
        } finally {UnityEngine.Object.DestroyImmediate(host);}
        return checks;
    }
}
