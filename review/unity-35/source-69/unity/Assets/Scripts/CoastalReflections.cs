using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleRoad
{
    public sealed class CoastalReflections : MonoBehaviour
    {
        IEnumerator Start()
        {
            // The imported quality presets disable realtime probes. These are
            // scripted, one-time environment captures, not per-frame updates.
            QualitySettings.realtimeReflectionProbes=true;
            // Render after the procedural world has been assembled. The probes
            // illuminate glossy surfaces with the actual village and landscape.
            yield return null;
            var coast=Probe("Landscape reflection",new Vector3(-30,7,-35),new Vector3(520,160,520),1,256,false);
            coast.RenderProbe();
            yield return null;
            var garage=Probe("Workshop reflection",new Vector3(-65,2.5f,-108),new Vector3(13,6,13),5,128,true);
            garage.RenderProbe();
        }
        ReflectionProbe Probe(string name,Vector3 position,Vector3 size,int importance,int resolution,bool box)
        {
            var go=new GameObject(name);go.transform.SetParent(transform);go.transform.position=position;
            var probe=go.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.AllFacesAtOnce;probe.resolution=resolution;probe.size=size;probe.importance=importance;
            probe.boxProjection=box;probe.blendDistance=box?1.5f:30;probe.intensity=.85f;probe.nearClipPlane=.25f;probe.farClipPlane=900;probe.hdr=true;
            return probe;
        }
    }
}
