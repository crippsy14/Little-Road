using System.Diagnostics;
using UnityEngine;

namespace LittleRoad
{
    // Frame-probe runs only: steps physics from script, last in the fixed step (as the automatic step
    // would), and times it into FrameCost.Physics. Not used in normal play.
    [DefaultExecutionOrder(32000)]
    public sealed class PhysicsStepTimer:MonoBehaviour
    {
        public static void Attach(){if(Physics.simulationMode==SimulationMode.Script)return;Physics.simulationMode=SimulationMode.Script;var go=new GameObject("Physics step timer");DontDestroyOnLoad(go);go.AddComponent<PhysicsStepTimer>();}
        void FixedUpdate(){long start=Stopwatch.GetTimestamp();Physics.Simulate(Time.fixedDeltaTime);FrameCost.Physics+=Stopwatch.GetTimestamp()-start;}
    }
}
