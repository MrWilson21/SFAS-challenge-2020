using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class LoadTexture : MonoBehaviour
{
    //Script to quickly get the preview image of a prefab and save it as a texture
    [SerializeField] private GameObject texture;
    [SerializeField] private string imageName;
    private Texture2D gameObjectTex;

    // Use this for initialization
    void Start()
    {
        var getImage = UnityEditor.AssetPreview.GetAssetPreview(texture);
        print(getImage);

        gameObjectTex = getImage;

        byte[] bytes = gameObjectTex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + imageName, bytes);
    }
}
