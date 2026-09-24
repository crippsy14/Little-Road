using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LittleRoad
{
    // Uses the same imported resources/material mapping as the actual race.
    // Images are direct engine renders, not illustration or retouched mockups.
    public sealed class CarGallery:MonoBehaviour
    {
        Camera cameraView;
        string folder;
        IEnumerator Start()
        {
            folder=RaceTrial.Arg("--trial-captures",Path.Combine(Application.persistentDataPath,"Gallery"));Directory.CreateDirectory(folder);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.63f,.70f,.78f);
            RenderSettings.ambientEquatorColor=new Color(.54f,.55f,.51f);RenderSettings.ambientGroundColor=new Color(.28f,.27f,.23f);
            var light=new GameObject("Gallery soft sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.8f;
            light.color=new Color(1,.92f,.80f);light.transform.rotation=Quaternion.Euler(42,-32,0);light.shadows=LightShadows.Soft;
            var fill=new GameObject("Gallery fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.35f;
            fill.color=new Color(.70f,.83f,1);fill.transform.rotation=Quaternion.Euler(23,140,0);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.065f,0);floor.transform.localScale=new Vector3(100,.1f,100);
            floor.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material("Warm studio floor",new Color(.69f,.66f,.57f));
            var volume=new GameObject("Gallery tone").AddComponent<Volume>();volume.isGlobal=true;volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var car=TrialWorld.Car("compact_v1",new Color(.055f,.44f,.39f));car.GetComponent<LODGroup>().ForceLOD(0);
            var old=TrialWorld.Car("little_car",new Color(.055f,.44f,.39f));old.SetActive(false);
            cameraView=new GameObject("Gallery camera").AddComponent<Camera>();cameraView.orthographic=true;cameraView.orthographicSize=1.85f;
            cameraView.clearFlags=CameraClearFlags.SolidColor;cameraView.backgroundColor=new Color(.76f,.74f,.67f);cameraView.nearClipPlane=.1f;
            var cameraData=cameraView.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=true;
            cameraData.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality=AntialiasingQuality.High;
            yield return null;
            yield return Capture("01-master-front",new Vector3(4.4f,2.7f,6),new Vector3(0,.8f,0));
            yield return Capture("02-master-rear",new Vector3(-4.4f,2.6f,-6),new Vector3(0,.8f,0));
            yield return Capture("03-master-side",new Vector3(7,1.8f,0),new Vector3(0,.8f,0));
            car.GetComponent<LODGroup>().ForceLOD(1);yield return Capture("04-lod1-front",new Vector3(4.4f,2.7f,6),new Vector3(0,.8f,0));
            car.GetComponent<LODGroup>().ForceLOD(2);yield return Capture("05-lod2-front",new Vector3(4.4f,2.7f,6),new Vector3(0,.8f,0));
            car.GetComponent<LODGroup>().ForceLOD(0);
            var wheels=new WheelVisuals(car);wheels.Pose(117,23);yield return Capture("06-steered-spinning-wheels",new Vector3(4.4f,2.7f,6),new Vector3(0,.8f,0));
            wheels.Pose(0,0);
            var lights=car.GetComponent<VehicleLights>();
            lights.SetState(true,false);yield return Capture("08-brake-lights",new Vector3(-4.4f,2.6f,-6),new Vector3(0,.8f,0));
            lights.SetState(false,true);yield return Capture("09-reversing-lights",new Vector3(-4.4f,2.6f,-6),new Vector3(0,.8f,0));
            lights.SetState(false,false);yield return Capture("10-rear-lights-idle",new Vector3(-4.4f,2.6f,-6),new Vector3(0,.8f,0));
            car.GetComponent<LODGroup>().ForceLOD(1);lights.SetState(true,false);
            yield return Capture("11-lod1-brake-lights",new Vector3(-4.4f,2.6f,-6),new Vector3(0,.8f,0));
            car.GetComponent<LODGroup>().ForceLOD(2);lights.SetState(false,true);
            yield return Capture("12-lod2-reversing-lights",new Vector3(-4.4f,2.6f,-6),new Vector3(0,.8f,0));
            car.GetComponent<LODGroup>().ForceLOD(0);lights.SetState(false,false);
            car.transform.position=new Vector3(-1.65f,0,0);old.transform.position=new Vector3(1.65f,0,0);old.SetActive(true);
            cameraView.orthographicSize=2.7f;yield return Capture("07-old-left-new-right",new Vector3(3,3.8f,9),new Vector3(0,.8f,0));
            Debug.Log("LITTLE_ROAD_GALLERY_COMPLETE "+folder);Application.Quit();
        }
        IEnumerator Capture(string name,Vector3 position,Vector3 target)
        {
            cameraView.transform.position=position;cameraView.transform.LookAt(target);yield return null;
            var rt=new RenderTexture(1600,1050,24);cameraView.targetTexture=rt;cameraView.Render();var prior=RenderTexture.active;RenderTexture.active=rt;
            var texture=new Texture2D(1600,1050,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,1050),0,0);texture.Apply();
            File.WriteAllBytes(Path.Combine(folder,name+".png"),texture.EncodeToPNG());cameraView.targetTexture=null;RenderTexture.active=prior;
            rt.Release();Destroy(rt);Destroy(texture);
        }
    }
}
