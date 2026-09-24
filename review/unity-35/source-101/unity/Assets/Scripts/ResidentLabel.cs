using UnityEngine;

namespace LittleRoad
{
    public sealed class ResidentLabel:MonoBehaviour
    {
        Renderer label;
        void Awake()=>label=GetComponent<Renderer>();
        void LateUpdate()
        {
            var cameraView=Camera.main;if(cameraView==null){label.enabled=false;return;}
            Vector3 cameraPosition=cameraView.transform.position;
            label.enabled=Vector3.Distance(cameraPosition,transform.position)<24&&!Physics.Linecast(cameraPosition,transform.position);
            transform.rotation=cameraView.transform.rotation;
        }
    }
}
