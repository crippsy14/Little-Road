using UnityEngine;

namespace LittleRoad
{
    // A horn (or the train's whistle) on a car: HornSynth through a 3D AudioSource, the same constant-clip carrier as
    // CarAudio so Unity's distance and panning still apply (docs/51). Hold() follows a button; Toot() plays a pattern
    // of short blasts (the traffic's "beep-beep").
    public sealed class Horn:MonoBehaviour
    {
        HornSynth synth;AudioSource source;bool held;float volume;
        readonly System.Collections.Generic.Queue<(float on,float off)> pattern=new();float phaseUntil;bool phaseOn;
        static AudioClip dc;
        public static Horn Attach(GameObject go,float pitch,bool whistle=false,bool player=false)
        {
            if(!AudioDirector.Enabled)return null;
            // Its own child object: a filter runs on every AudioSource of its GameObject, and the car's engine voice
            // (CarAudio) must not be multiplied by the horn.
            var child=new GameObject(whistle?"Whistle":"Horn");child.transform.SetParent(go.transform,false);child.transform.localPosition=Vector3.up*(whistle?4.5f:.8f);
            go=child;
            var h=go.AddComponent<Horn>();h.synth=new HornSynth(AudioSettings.outputSampleRate,pitch,whistle,(uint)go.name.GetHashCode()^(uint)(go.transform.position.x*131));h.volume=whistle?.75f:player?.55f:.5f;
            var s=h.source=go.AddComponent<AudioSource>();s.clip=Carrier();s.loop=true;s.playOnAwake=false;s.spatialBlend=player?.4f:1;s.dopplerLevel=player?0:.5f;
            s.rolloffMode=AudioRolloffMode.Logarithmic;s.minDistance=whistle?30:player?10:8;s.maxDistance=whistle?600:250;s.priority=64;
            return h;
        }
        // The horn's pitch for a body: big vehicles lower, small ones higher.
        public static float PitchFor(string body)=>body switch{"minibus"=>.78f,"camper"=>.8f,"postvan"=>.84f,"marlow"=>.9f,"bubble"=>1.3f,"pebble"=>1.22f,"doodle"=>1.18f,"beetle"=>1.12f,_=>1f};
        static AudioClip Carrier()
        {
            if(dc!=null)return dc;int fs=AudioSettings.outputSampleRate;var ones=new float[fs];for(int i=0;i<fs;i++)ones[i]=1;
            dc=AudioClip.Create("Horn carrier",fs,1,fs,false);dc.SetData(ones,0);return dc;
        }
        public void Hold(bool on)=>held=on;
        public void Toot(params float[] onOff){pattern.Clear();for(int i=0;i+1<onOff.Length;i+=2)pattern.Enqueue((onOff[i],onOff[i+1]));phaseUntil=0;phaseOn=false;}
        public bool Busy=>pattern.Count>0||phaseOn||synth.Sounding;
        void Update()
        {
            if(synth==null)return;
            if(Time.time>=phaseUntil) {
                if(phaseOn){phaseOn=false;phaseUntil=Time.time+(lastOff);}
                else if(pattern.Count>0){var (onFor,offFor)=pattern.Dequeue();phaseOn=true;lastOff=offFor;phaseUntil=Time.time+onFor;}
            }
            bool on=held||phaseOn;synth.Press(on);
            // A voice only while sounding (Unity mixes ~32 real voices; a dozen idle horns would crowd the engines out).
            if(on){lastOn=Time.time;if(!source.isPlaying)source.Play();}else if(source.isPlaying&&Time.time>lastOn+.6f)source.Stop();
        }
        float lastOn=-9;
        float lastOff;
        void OnAudioFilterRead(float[] data,int channels)
        {
            var s=synth;if(s==null||AudioDirector.Muted){System.Array.Clear(data,0,data.Length);return;}
            float gain=volume*effects*AudioDirector.SceneFade;
            for(int i=0,k=0;i<data.Length/channels;i++){float x=s.Next()*gain;for(int c=0;c<channels;c++,k++)data[k]*=x;}
        }
        float effects=1;
        void LateUpdate()=>effects=AudioOptions.Level(AudioOptions.Effects);
    }
}
