using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine.LowLevel;

namespace LittleRoad
{
    // Where a frame's time goes, in any build (docs/72; --load-timing only): a timestamp before every step of Unity's player
    // loop (scripts' Start and Update, physics, rendering, uploads...), and for the frames asked for, the longest steps.
    // The first frame after loading took ~560 ms with every camera off and no GC or physics cost; this names the step.
    public static class LoopProbe
    {
        struct Timestamp{}
        static readonly Stopwatch clock=Stopwatch.StartNew();
        static string[] names;static long[] at;static int frames;static readonly StringBuilder log=new();
        public static void Install()
        {
            if(names!=null)return;
            var root=PlayerLoop.GetCurrentPlayerLoop();var list=new List<string>();
            var phases=root.subSystemList;
            for(int p=0;p<phases.Length;p++) {
                var subs=phases[p].subSystemList;if(subs==null)continue;var wrapped=new List<PlayerLoopSystem>();
                foreach(var s in subs) {
                    int index=list.Count;list.Add(phases[p].type.Name+"."+(s.type!=null?s.type.Name:"?"));
                    wrapped.Add(new PlayerLoopSystem{type=typeof(Timestamp),updateDelegate=()=>Stamp(index)});wrapped.Add(s);
                }
                int end=list.Count;list.Add(phases[p].type.Name+".(end)");wrapped.Add(new PlayerLoopSystem{type=typeof(Timestamp),updateDelegate=()=>Stamp(end)});
                phases[p].subSystemList=wrapped.ToArray();
            }
            root.subSystemList=phases;PlayerLoop.SetPlayerLoop(root);names=list.ToArray();at=new long[names.Length];
        }
        static int last=-1;static long lastAt;
        static void Stamp(int index)
        {
            long now=clock.ElapsedTicks;
            if(frames>0&&last>=0&&index!=last){long ms=(now-lastAt)*1000/Stopwatch.Frequency;if(ms>=3)log.Append(names[last]).Append('=').Append(ms).Append(' ');}
            // (The time between the last step and the next frame's first counts to the last step: presenting the frame.)
            if(index==0&&frames>0){frames--;log.Append("| f").Append(UnityEngine.Time.frameCount).Append(' ');if(frames==0){UnityEngine.Debug.Log("LITTLE_ROAD_LOOP_PROBE "+log);log.Clear();}}
            last=index;lastAt=now;
        }
        // The step running now (to place a coroutine's resumption).
        public static string Where=>names!=null&&last>=0?names[last]+"@"+UnityEngine.Time.frameCount:"?";
        // Log the steps of 3 ms or more for the next n frames.
        public static void Watch(int n){if(names!=null){frames=n;log.Clear();}}
        // A note in the log at this point (load steps, to line them up with the frames).
        public static void Mark(string what){if(frames>0)log.Append('<').Append(what).Append("> ");}
        // Log what's been gathered so far and stop.
        public static void Flush(string label){if(names!=null&&log.Length>0){UnityEngine.Debug.Log($"LITTLE_ROAD_LOOP_PROBE {label} "+log);log.Clear();}frames=0;}
    }
}
