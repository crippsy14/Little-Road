using UnityEngine;

namespace LittleRoad
{
    // Frozen legacy decision equations. State and RNG order match RoadCar.Plan;
    // the policy has no Rigidbody, Transform, GameObject or RoadCar reference.
    public sealed class LegacyDriverPolicy:IDriverPolicy
    {
        readonly System.Random random;
        float variation,variationGoal,variationClock,lane,laneHold,chosenLane;
        // Preserve the legacy field stores at intermediate speed/steering
        // assignments; small float-rounding changes can alter traffic later.
        float predictedOffset,targetSpeed,steering;
        static readonly float[] LaneOptions={-5.2f,-2.6f,0,2.6f,5.2f};
        public LegacyDriverPolicy(System.Random random){this.random=random;}
        public DriveCommand Decide(in DriverObservation observation)
        {
            var self=observation.self;var tuning=observation.tuning;
            float pathDistance=self.route,offset=self.offset,speed=self.speed,pace=observation.pace,preferredOffset=observation.preferredOffset;
            bool finished=observation.finished;

            variationClock-=observation.stepSeconds;
            if(variationClock<=0) {variationClock=1.5f+(float)random.NextDouble()*3;variationGoal=(float)(random.NextDouble()-.5)*.055f;}
            variation=Mathf.MoveTowards(variation,variationGoal,observation.stepSeconds*.012f);
            float desiredLane=preferredOffset;laneHold-=observation.stepSeconds;
            float nearest=100;
            ObservedCar? ahead=null;
            foreach(var other in observation.traffic) {if(other.id==self.id)continue;Vector3 delta=other.position-self.position;
                float forward=Vector3.Dot(delta,self.forward),side=Vector3.Dot(delta,self.right);
                if(forward>0 && forward<nearest && Mathf.Abs(side)<2.4f) {nearest=forward;ahead=other;}}
            if(ahead!=null && nearest<22&&laneHold<=0) {
                float best=-1e9f;
                foreach(float candidate in LaneOptions) {
                    float score= -Mathf.Abs(candidate-preferredOffset)*.15f;
                    foreach(var other in observation.traffic) {if(other.id==self.id)continue;
                        float along=Mathf.Repeat(other.route-pathDistance+observation.route.Length*.5f,observation.route.Length)-observation.route.Length*.5f;
                        if(along> -6 && along<28)score-=Mathf.Max(0,3-Mathf.Abs(other.offset-candidate))*(along<8?5:1);}
                    if(score>best){best=score;desiredLane=candidate;}}
                chosenLane=desiredLane;laneHold=.8f;
            }
            if(laneHold>0)desiredLane=chosenLane;
            float future=pathDistance+Mathf.Max(speed,0)*.55f;
            predictedOffset=Vector3.Dot(self.position+self.velocity*.55f-observation.route.At(future),observation.route.Right(future));
            bool edgeRisk=Mathf.Abs(predictedOffset)>6.25f;
            if(edgeRisk)desiredLane=Mathf.Clamp(desiredLane,-3.3f,3.3f);
            lane=Mathf.MoveTowards(lane,desiredLane,observation.stepSeconds*1.8f);
            float look=Mathf.Clamp(6+Mathf.Max(speed,0)*.65f,7,24);
            Vector3 aim=observation.route.At(pathDistance+look)+observation.route.Right(pathDistance+look)*lane;
            float angle=Vector3.SignedAngle(self.forward,aim-self.position,Vector3.up)*Mathf.Deg2Rad;
            float pursuitCurvature=2*Mathf.Sin(angle)/Mathf.Max(3,Vector3.Distance(aim,self.position));
            float pursuitYaw=pursuitCurvature*Mathf.Max(speed,3)*1.09f;
            steering=Mathf.Clamp(pursuitYaw/tuning.yaw,-1,1);
            if(Mathf.Abs(angle)>.9f)steering=Mathf.Sign(angle);
            targetSpeed=tuning.AttainableSpeed*(pace+variation);
            // The same shared traction budget used by the physical controller
            // limits braking in a turn. Assuming full straight-line brakes here
            // previously sent cars wide at the final bend.
            float budget=tuning.roadGrip*1.65f;
            float turnFraction=Mathf.Clamp(Mathf.Abs(steering*tuning.yaw*speed)/budget,.70f,.95f);
            float brakingReserve=Mathf.Min(tuning.braking,budget*Mathf.Sqrt(1-turnFraction*turnFraction))*.85f;
            float reactionDistance=4+Mathf.Max(speed,0)*.30f;
            // Look through the braking distance, then solve a reachable speed
            // for every upcoming bend rather than braking at a fixed waypoint.
            for(float d=4;d<65;d+=6) {
                float k=Vector3.SignedAngle(observation.route.Forward(pathDistance+d-4),observation.route.Forward(pathDistance+d+4),Vector3.up)*Mathf.Deg2Rad/8;
                float curve=Mathf.Abs(k)/Mathf.Max(.65f,1-k*lane);
                float corner=Mathf.Sqrt(tuning.roadGrip*1.65f*.70f/Mathf.Max(.001f,curve))*(pace+variation);
                float reachable=Mathf.Sqrt(corner*corner+2*brakingReserve*Mathf.Max(0,d-reactionDistance));
                targetSpeed=Mathf.Min(targetSpeed,reachable);}
            targetSpeed=Mathf.Min(targetSpeed,Mathf.Sqrt(budget*.73f/Mathf.Max(.001f,Mathf.Abs(pursuitCurvature))));
            if(edgeRisk)targetSpeed=Mathf.Min(targetSpeed,Mathf.Lerp(15,6,Mathf.InverseLerp(6.25f,10,Mathf.Abs(predictedOffset))));
            if(Mathf.Abs(offset)>7)targetSpeed=Mathf.Min(targetSpeed,Mathf.Abs(offset)>14?7:12);
            if(Mathf.Abs(angle)>.65f)targetSpeed=Mathf.Min(targetSpeed,8);
            if(ahead!=null && nearest<9 && Mathf.Abs(ahead.Value.offset-lane)<2.3f)
                targetSpeed=Mathf.Min(targetSpeed,Mathf.Max(0,ahead.Value.speed+(nearest-5)*.9f));
            // Finishers keep circulating; parking on the timing line would
            // obstruct the rest of the field and corrupt later finish times.
            if(finished)targetSpeed=Mathf.Min(targetSpeed,18);
            return new DriveCommand(DriveModel.PedalForSpeed(tuning,speed,targetSpeed),steering,targetSpeed,predictedOffset,chosenLane);
        }
    }
}
