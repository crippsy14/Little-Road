using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

// StopLook (on by default since 20 Oct; --no-stop-look): stopped dead, the right stick swings the free-roam camera round the car; moving, it does
// nothing; driving off brings the view back behind the car.
public static class StopLookChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        const float dt=1/60f;var chase=new Vector3(0,2.5f,-6.5f);float elev=StopLook.Elevation(chase);
        StopLook.State Run(StopLook.State s,float speed,Vector2 stick,bool free,float seconds){for(float t=0;t<seconds;t+=dt)s=StopLook.Step(s,speed,stick,free,dt,elev);return s;}
        Check(StopLook.Enabled,"Stop look is on by default (Richard, 20 Oct; --no-stop-look turns it off)");
        var moving=Run(default,8,Vector2.right,true,1);
        Check(moving.weight==0&&moving.yaw==0&&StopLook.Orbit(chase,moving)==chase,"Stop look: moving, the right stick leaves the chase camera alone");
        var held=Run(default,0,Vector2.zero,false,.2f);held=Run(held,0,Vector2.right,false,1);
        Check(held.weight==0&&held.yaw==0,"Stop look: with a menu open or looking behind, the stick leaves the view alone");
        var round=Run(default,0,Vector2.right,true,2);round=Run(round,0,Vector2.zero,true,2);
        var o=StopLook.Orbit(chase,round);
        Check(round.stopped&&round.weight==1&&Mathf.Abs(Mathf.DeltaAngle(round.yaw,180))<2&&o.z>6&&Mathf.Abs(o.magnitude-chase.magnitude)<1e-3f,"Stop look: stopped, two seconds of right stick swing the camera round to the front of the car, at the same distance, and it stays there");
        var up=Run(round,0,Vector2.down,true,4);var down=Run(round,0,Vector2.up,true,4);
        Check(Mathf.Abs(StopLook.Elevation(StopLook.Orbit(chase,up))-StopLook.MaxElevation)<.5f&&Mathf.Abs(StopLook.Elevation(StopLook.Orbit(chase,down))-StopLook.MinElevation)<.5f,"Stop look: the camera rises to look down from above and drops to near the ground, no further");
        var settle=Run(round,.4f,Vector2.zero,true,1);
        Check(settle.stopped&&settle.weight==1,"Stop look: a car settling on its springs (0.4 m/s) keeps the view");
        var away=Run(round,5,Vector2.zero,true,3);
        Check(!away.stopped&&away.weight==0&&away.yaw==0&&away.pitch==0,"Stop look: driving off brings the view back behind the car");
        return checks;
    }
}
