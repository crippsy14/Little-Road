using UnityEngine;
using UnityEngine.Rendering;

namespace LittleRoad
{
    // The finish of a timed run (Richard, 1 Oct: for the Castle Climb and Beat the Train "you have to guess and drive
    // around until it says you've been successful"): a column of light tall enough to see over the town, and a pulsing
    // band of light laid on the ground at the edge of the finish zone. Additive and unlit, with the night light trails'
    // shader (soft edges across each strip, vertex alpha fading the column out with height): the lit transparent
    // materials read as a dull tint, and one variant rendered solid in the player. Dimmer at night, when the exposure
    // lifts glow about 5x (docs/51, the fireworks).
    public sealed class FinishBeacon:MonoBehaviour
    {
        MeshFilter ring;Material beamMaterial,haloMaterial,ringMaterial;Light glow;bool shown;
        static readonly Color Gold=new(1,.74f,.2f);
        public void Show(Vector3 at,float radius)
        {
            if(ring==null)Build();
            float y=Ground(at)??at.y;transform.position=new Vector3(at.x,y,at.z);
            ring.sharedMesh=Ring(radius);gameObject.SetActive(true);shown=true;
            Debug.Log($"LITTLE_ROAD_FINISH_BEACON at ({at.x:0},{y:0.0},{at.z:0}) radius {radius:0}");
        }
        public void Hide(){shown=false;if(ring!=null)gameObject.SetActive(false);}
        // The highest solid surface under a point (roads, paving and terrain; never a car or a trigger).
        static float? Ground(Vector3 at)
        {
            float best=float.MinValue;
            foreach(var h in Physics.RaycastAll(at+Vector3.up*8,Vector3.down,40))if(!h.collider.isTrigger&&h.collider.GetComponentInParent<RoadCar>()==null)best=Mathf.Max(best,h.point.y);
            return best>float.MinValue?best:null;
        }
        // A 2 m band at the zone's edge (uv.y across it for the soft edges), each vertex dropped onto the ground.
        Mesh Ring(float radius)
        {
            const int N=72;var v=new Vector3[N*2+2];var uv=new Vector2[v.Length];var c=new Color[v.Length];var t=new int[N*6];var o=transform.position;
            for(int i=0;i<=N;i++) {
                float a=i*Mathf.PI*2/N;var d=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                for(int k=0;k<2;k++){var p=o+d*(radius-2*k);float y=Ground(p)??o.y;v[i*2+k]=new Vector3(p.x-o.x,y-o.y+.12f,p.z-o.z);uv[i*2+k]=new Vector2(i/(float)N,k);c[i*2+k]=Color.white;}
                if(i<N){int b=i*2;t[i*6]=b;t[i*6+1]=b+2;t[i*6+2]=b+1;t[i*6+3]=b+1;t[i*6+4]=b+2;t[i*6+5]=b+3;}
            }
            var m=new Mesh{name="Finish ring",vertices=v,uv=uv,colors=c,triangles=t};m.RecalculateBounds();return m;
        }
        // Three crossed vertical strips, soft at their edges and fading out with height: a column from any side.
        static Mesh Column(float width,float height)
        {
            var v=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var c=new System.Collections.Generic.List<Color>();var t=new System.Collections.Generic.List<int>();
            float[] rows={0,height*.15f,height*.6f,height};float[] alpha={1,.9f,.6f,0};
            for(int q=0;q<3;q++) {
                var across=Quaternion.Euler(0,q*60,0)*Vector3.right*(width*.5f);int b=v.Count;
                for(int r=0;r<rows.Length;r++)for(int s=0;s<2;s++){v.Add((s==0?-across:across)+Vector3.up*rows[r]);uv.Add(new Vector2(r,s));c.Add(new Color(1,1,1,alpha[r]));}
                for(int r=0;r<rows.Length-1;r++){int a=b+r*2;t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
            }
            var m=new Mesh{name="Finish column"};m.SetVertices(v);m.SetUVs(0,uv);m.SetColors(c);m.SetTriangles(t,0);m.RecalculateBounds();return m;
        }
        void Build()
        {
            var shader=Shader.Find("LittleRoad/LightTrail");
            beamMaterial=new Material(shader){name="Finish beam"};haloMaterial=new Material(shader){name="Finish halo"};ringMaterial=new Material(shader){name="Finish ring"};
            Strip("Finish beam",Column(2.2f,70),beamMaterial);Strip("Finish halo",Column(7,45),haloMaterial);
            ring=Strip("Finish ring",null,ringMaterial);
            glow=new GameObject("Finish glow").AddComponent<Light>();glow.transform.SetParent(transform,false);glow.transform.localPosition=Vector3.up*3;
            glow.type=LightType.Point;glow.color=Gold;glow.range=24;glow.intensity=2;glow.shadows=LightShadows.None;
        }
        MeshFilter Strip(string name,Mesh mesh,Material m)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);var f=go.GetComponent<MeshFilter>();f.sharedMesh=mesh;
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return f;
        }
        void Update()
        {
            if(!shown)return;float pulse=.5f+.5f*Mathf.Sin(Time.time*3.2f),day=Mathf.Lerp(1,.3f,TimeOfDay.Night);
            beamMaterial.SetColor("_Tint",Gold*(day*(.7f+.25f*pulse)));haloMaterial.SetColor("_Tint",Gold*(day*(.18f+.12f*pulse)));
            ringMaterial.SetColor("_Tint",Gold*(day*(.8f+.6f*pulse)));glow.intensity=1.5f+1.5f*pulse;
        }
    }
}
