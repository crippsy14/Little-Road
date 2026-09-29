using UnityEngine;
using UnityEngine.Rendering;

namespace LittleRoad
{
    // Tyre marks (graphics scorecard item 33): rubber on tarmac while a car slides, ruts in the dirt
    // off road. One shared ring buffer of quads (oldest overwritten) in a single mesh, fading over
    // `Life` seconds; drawn with LittleRoad/SkidMark (multiply). DustTrail lays the segments.
    public sealed class SkidMarks:MonoBehaviour
    {
        const int Max=3000;const float Width=.24f,Life=40;
        static SkidMarks instance;
        Mesh mesh;readonly Vector3[] v=new Vector3[Max*4];readonly Color[] c=new Color[Max*4];readonly Vector2[] uv=new Vector2[Max*4];
        readonly float[] born=new float[Max];readonly float[] strength=new float[Max];int next;bool dirty;float fadeTimer;
        public static readonly Color Rubber=new(.30f,.30f,.32f),Rut=new(.55f,.47f,.36f);
        static SkidMarks Instance
        {
            get {
                if(instance!=null)return instance;var shader=Shader.Find("LittleRoad/SkidMark");if(shader==null)return null;
                var go=new GameObject("Tyre marks",typeof(MeshFilter),typeof(MeshRenderer));instance=go.AddComponent<SkidMarks>();
                instance.mesh=new Mesh{name="Tyre marks"};instance.mesh.MarkDynamic();
                var tris=new int[Max*6];for(int q=0;q<Max;q++){int a=q*4,k=q*6;tris[k]=a;tris[k+1]=a+2;tris[k+2]=a+1;tris[k+3]=a+1;tris[k+4]=a+2;tris[k+5]=a+3;}
                instance.mesh.vertices=instance.v;instance.mesh.colors=instance.c;instance.mesh.uv=instance.uv;instance.mesh.triangles=tris;
                instance.mesh.bounds=new Bounds(Vector3.zero,Vector3.one*1e5f);
                go.GetComponent<MeshFilter>().sharedMesh=instance.mesh;var r=go.GetComponent<MeshRenderer>();
                r.sharedMaterial=new Material(shader){name="Tyre marks"};r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
                return instance;
            }
        }
        // One tread segment from `from` to `to` (world, on the surface), lifted a hair above it.
        public static void Lay(Vector3 from,Vector3 to,Vector3 up,float amount,Color tint)
        {
            var s=Instance;if(s==null)return;var d=to-from;if(d.sqrMagnitude<.0025f)return;
            var side=Vector3.Cross(up,d.normalized)*Width*.5f;var lift=up*.025f;int q=s.next;s.next=(s.next+1)%Max;int a=q*4;
            s.v[a]=from-side+lift;s.v[a+1]=from+side+lift;s.v[a+2]=to-side+lift;s.v[a+3]=to+side+lift;
            float len=d.magnitude;s.uv[a]=new Vector2(0,0);s.uv[a+1]=new Vector2(1,0);s.uv[a+2]=new Vector2(0,len);s.uv[a+3]=new Vector2(1,len);
            s.born[q]=Time.time;s.strength[q]=Mathf.Clamp01(amount);for(int k=0;k<4;k++){var col=tint;col.a=s.strength[q];s.c[a+k]=col;}
            s.dirty=true;
        }
        void LateUpdate()
        {
            // Fade by age twice a second (cheap), upload when anything changed.
            fadeTimer-=Time.deltaTime;
            if(fadeTimer<=0){fadeTimer=.5f;float now=Time.time;
                for(int q=0;q<Max;q++){if(strength[q]<=0)continue;float left=1-(now-born[q])/Life;if(left<=0){strength[q]=0;for(int k=0;k<4;k++)c[q*4+k].a=0;}else for(int k=0;k<4;k++)c[q*4+k].a=strength[q]*Mathf.Min(1,left*3);}
                dirty=true;}
            if(!dirty)return;dirty=false;mesh.vertices=v;mesh.colors=c;mesh.uv=uv;
        }
    }
}
