using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LittleRoad
{
    public sealed class RacingLineOverlay:MonoBehaviour
    {
        GameObject root;RacingSpeedProfile profile;RaceTrial owner;
        public void Setup(RaceTrial race,RacingSpeedProfile speedProfile)
        {
            owner=race;profile=speedProfile;root=new GameObject("Candidate racing-line debug overlay");
            var cyan=TrialWorld.Material("Reference line",new Color(.05f,.90f,.95f));
            var brake=TrialWorld.Material("Reference braking",new Color(1,.18f,.12f));
            var power=TrialWorld.Material("Reference acceleration",new Color(.2f,.95f,.25f));
            var corridor=TrialWorld.Material("One metre tracking corridor",new Color(.11f,.45f,.48f));
            var centre=TrialWorld.Material("Road centre diagnostic",new Color(.67f,.68f,.60f));
            for(int layer=0;layer<6;layer++) {
                var vertices=new List<Vector3>();var triangles=new List<int>();
                for(int i=0;i<profile.line.Count;i++) {
                    float acceleration=profile.acceleration[i];int colour=acceleration< -1?1:acceleration>1?2:0;
                    if(layer<3&&colour!=layer)continue;if(layer==5&&i%8>=4)continue;
                    int next=(i+1)%profile.line.Count;var a=profile.line.points[i];var b=profile.line.points[next];
                    var right=Vector3.Cross(Vector3.up,(b-a).normalized);float width=layer<3?.18f:.055f;
                    if(layer==3||layer==4){float side=layer==3?-1:1;a+=right*side;b+=right*side;}
                    if(layer==5){a=race.track.At(i*profile.line.roadStep);b=race.track.At((i+1)*profile.line.roadStep);right=Vector3.Cross(Vector3.up,(b-a).normalized);}
                    int v=vertices.Count;vertices.Add(a-right*width*.5f+Vector3.up*.065f);vertices.Add(a+right*width*.5f+Vector3.up*.065f);
                    vertices.Add(b-right*width*.5f+Vector3.up*.065f);vertices.Add(b+right*width*.5f+Vector3.up*.065f);
                    triangles.AddRange(new[]{v,v+2,v+1,v+1,v+2,v+3});
                }
                var mesh=new Mesh{name="Reference debug strip",vertices=vertices.ToArray(),triangles=triangles.ToArray()};mesh.RecalculateNormals();
                var go=new GameObject("Line overlay "+layer);go.transform.SetParent(root.transform);go.AddComponent<MeshFilter>().sharedMesh=mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial=layer==0?cyan:layer==1?brake:layer==2?power:layer==5?centre:corridor;
            }
            root.SetActive(!RaceTrial.Has("--hide-racing-line"));
        }
        void Update(){if(Keyboard.current?.lKey.wasPressedThisFrame==true)root.SetActive(!root.activeSelf);}
        void OnGUI()
        {
            if(profile==null||owner.PreviewOpen)return;
            using var hud=new DisplaySettings.HudScale(true);
            GUI.color=new Color(.07f,.16f,.19f,.9f);GUI.DrawTexture(new Rect(20,172,790,56),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(36,177,750,24),$"CANDIDATE {profile.line.data.id} · predicted flying lap {profile.predictedLapTime:0.0}s · L: line overlay");
            GUI.Label(new Rect(36,199,750,24),"Red: braking · green: acceleration · cyan: reference · thin edges: ±1 m · dashed: road centre");
        }
    }
}
