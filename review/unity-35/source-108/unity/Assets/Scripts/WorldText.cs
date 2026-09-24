using UnityEngine;

namespace LittleRoad
{
    // Depth-tested world signs, independent of the screen-space UI font material.
    public sealed class WorldText:MonoBehaviour
    {
        TextMesh text;Material material;
        void Awake()
        {
            text=GetComponent<TextMesh>();material=new Material(Shader.Find("LittleRoad/WorldText"));
            material.mainTexture=text.font.material.mainTexture;GetComponent<Renderer>().sharedMaterial=material;Font.textureRebuilt+=Refresh;
        }
        void Refresh(Font font){if(font==text.font)material.mainTexture=font.material.mainTexture;}
        void OnDestroy(){Font.textureRebuilt-=Refresh;if(material!=null)Destroy(material);}
    }
}
