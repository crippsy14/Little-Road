using UnityEngine;

namespace LittleRoad
{
    // The in-game scorecard's test drive (docs/55, --sound-drive with --trial-auto on the test loop): the AI steers, a
    // fixed script works the pedal, so every engine is heard doing the same things through the game's own gearbox,
    // physics and sound. The script:
    //   full throttle from the start; in 3rd, at 90% of the redline, lift for 2 s (a lift-off at high revs);
    //   back on to top gear and its top speed (95% of the redline, or the speed levelling off);
    //   lift 3 s, back on 2.5 s (tip-in), lift 4 s, brake to a stop, idle 3 s, then quit (the capture is written).
    // When the AI brakes hard (a turn coming), it brakes: the scorecard reads what happened from the capture log.
    public static class SoundDrive
    {
        public static readonly bool On=RaceTrial.Has("--sound-drive");
        enum Phase{Pull,LiftHigh,PullTop,Lift,Tip,LiftLong,Brake,Idle,Done}
        static Phase phase=Phase.Pull;static float since,lastSpeed=-1,lastCheck;
        public static float Pedal(RoadCar car,float ai,float elapsed)
        {
            if(elapsed<0)return ai;
            var box=car.gears;var train=car.tuning.drivetrain;
            float redline=train!=null?train.redline:6000;int top=train!=null?train.Gears-1:3;
            int gear=box!=null?box.Gear:0;float rpm=box!=null?box.Rpm:0;float speed=Mathf.Abs(car.speed);
            float t=elapsed-since;float pedal=0;
            void Next(Phase p){phase=p;since=elapsed;Debug.Log($"LITTLE_ROAD_SOUND_DRIVE phase={p} t={elapsed:0.00} gear={gear+1} rpm={rpm:0} speed={speed:0.0}");}
            switch(phase) {
                case Phase.Pull: pedal=1;if((gear>=2&&rpm>=redline*.9f)||t>40)Next(Phase.LiftHigh);break;
                case Phase.LiftHigh: pedal=0;if(t>2)Next(Phase.PullTop);break;
                case Phase.PullTop:
                    pedal=1;
                    if(elapsed-lastCheck>=1.5f){bool flat=lastSpeed>=0&&speed-lastSpeed<.2f&&gear>=top;lastSpeed=speed;lastCheck=elapsed;if(flat)Next(Phase.Lift);}
                    if((gear>=top&&rpm>=redline*.95f)||t>45)Next(Phase.Lift);
                    break;
                case Phase.Lift: pedal=0;if(t>3)Next(Phase.Tip);break;
                case Phase.Tip: pedal=1;if(t>2.5f)Next(Phase.LiftLong);break;
                case Phase.LiftLong: pedal=0;if(t>4)Next(Phase.Brake);break;
                case Phase.Brake: pedal=-1;if(speed<1.5f)Next(Phase.Idle);break;
                case Phase.Idle: pedal=0;if(t>3){Next(Phase.Done);Application.Quit();}break;
                default: pedal=0;break;
            }
            return ai<-.3f&&phase<Phase.Brake?ai:pedal;
        }
    }
}
