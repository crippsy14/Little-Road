using System;
using UnityEngine;

namespace LittleRoad
{
    // Golden Acre's orchard: rows of apple trees on the farm's hay meadow, with their apples. Scenery since the activities
    // were retired (Richard, 24 Oct); they were knocked down for the Apple Shake.
    public static class Orchard
    {
        // Rows of apple trees (farm-local coordinates through `at`).
        public static int Build(Transform parent,Func<float,float,Vector3> at,float yaw)
        {
            if(!BayviewKit.Has("tree_round"))return 0;var red=TrialWorld.Material("Apple",Toy.Hex(0xd62f2f),0,.55f);int planted=0;
            var holder=new GameObject("Golden Acre orchard").transform;holder.SetParent(parent);
            for(int r=0;r<4;r++)for(int c=0;c<13;c++) {
                float x=-30+c*9+(r%2)*4.5f,z=44+r*9;var p=at(x,z);
                var tree=BayviewKit.Spawn("tree_round",p+Vector3.down*.05f,yaw+(c*53+r*31)%360,holder,null,null,true,.5f+((c+r)%3)*.04f);if(tree==null)continue;tree.name="Apple tree";
                var b=new Bounds(p,Vector3.zero);foreach(var rend in tree.GetComponentsInChildren<Renderer>())b.Encapsulate(rend.bounds);
                for(int k=0;k<7;k++){float a=k*2.4f+r+c;var dir=new Vector3(Mathf.Cos(a),.2f+.5f*Mathf.Abs(Mathf.Sin(a*1.7f)),Mathf.Sin(a)).normalized;
                    var apple=GameObject.CreatePrimitive(PrimitiveType.Sphere);apple.name="Apple";UnityEngine.Object.Destroy(apple.GetComponent<Collider>());apple.transform.SetParent(tree.transform,true);
                    apple.transform.position=b.center+Vector3.Scale(dir,b.extents*.92f);apple.transform.localScale=Vector3.one*(.26f/Mathf.Max(.01f,tree.transform.lossyScale.x));apple.GetComponent<Renderer>().sharedMaterial=red;}
                planted++;
            }
            return planted;
        }
    }
}
