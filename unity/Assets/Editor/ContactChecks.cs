using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

// Every car sweeps its contacts (study 215): speculative contacts snag road-mesh edges while a car yaws.
public static class ContactChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        var car=TrialWorld.Car("compact_v1",Color.white);
        var race=new GameObject("Contact check owner").AddComponent<RaceTrial>();race.track=new Circuit();
        try {
            var driver=car.AddComponent<RoadCar>();driver.Setup(race,0,CarTuning.Stock,false,1,"contact check");
            Check(driver.body.collisionDetectionMode==CollisionDetectionMode.ContinuousDynamic,"AI and player cars use swept contacts by default (--speculative-contacts restores the old mode)");
        } finally {UnityEngine.Object.DestroyImmediate(car);UnityEngine.Object.DestroyImmediate(race.gameObject);}
        return checks;
    }
}
