using UnityEngine;
using UnityEngine.Rendering;

namespace LittleRoad
{
    // Pickup flourish: a burst of paper confetti where a postcard or stamp was collected.
    public sealed class Confetti:MonoBehaviour
    {
        const int Pieces=46;const float Life=1.6f;
        static Material[] materials;
        readonly Transform[] pieces=new Transform[Pieces];readonly Vector3[] velocity=new Vector3[Pieces],spin=new Vector3[Pieces];float age;
        public static void Burst(Vector3 at)
        {
            if(materials==null){var colours=new[]{Toy.Hex(0xff4d5e),Toy.Hex(0xffb400),Toy.Hex(0x3ee6a4),Toy.Hex(0x5ec8ff),Toy.Hex(0xff7ac8),Toy.Hex(0xfff8ec)};
                materials=new Material[colours.Length];for(int i=0;i<colours.Length;i++)materials[i]=TrialWorld.Material("Confetti",colours[i],0,.4f);}
            var go=new GameObject("Confetti");go.transform.position=at;go.AddComponent<Confetti>().Build();
        }
        void Build()
        {
            for(int i=0;i<Pieces;i++) {
                // DestroyImmediate: Destroy waits for the end of the frame, and bursts spawned in a physics step (a speed trap's
                // star) left 46 quad colliders on the car for the rest of it: it stopped dead "like a crash" (Richard, 1 Oct).
                var p=GameObject.CreatePrimitive(PrimitiveType.Quad);DestroyImmediate(p.GetComponent<Collider>());p.transform.SetParent(transform,false);
                p.transform.localScale=new Vector3(.14f,.09f,1);var r=p.GetComponent<Renderer>();r.sharedMaterial=materials[i%materials.Length];r.shadowCastingMode=ShadowCastingMode.Off;
                var dir=Random.onUnitSphere;dir.y=Mathf.Abs(dir.y)*1.4f+.4f;velocity[i]=dir.normalized*Random.Range(3.5f,6.5f);spin[i]=Random.insideUnitSphere*720;
                p.transform.rotation=Random.rotation;pieces[i]=p.transform;
            }
        }
        void Update()
        {
            float dt=Time.deltaTime;age+=dt;
            for(int i=0;i<Pieces;i++) {
                velocity[i]+=Vector3.down*6*dt;velocity[i]*=1-1.6f*dt; // light paper: gravity and air drag
                pieces[i].position+=velocity[i]*dt;pieces[i].Rotate(spin[i]*dt,Space.Self);
                if(age>Life-.4f)pieces[i].localScale*=1-dt*2.5f;
            }
            if(age>Life)Destroy(gameObject);
        }
    }
}
