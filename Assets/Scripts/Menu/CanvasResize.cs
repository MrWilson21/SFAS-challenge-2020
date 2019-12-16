using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasResize : MonoBehaviour
{
    [SerializeField] private List<RectTransform> canvases;

    [SerializeField] private float targetWidth;
    [SerializeField] private float targetHeight;
    float targetRatio;
    float oldRatio;

    // Start is called before the first frame update
    void Start()
    {
        targetRatio = targetWidth / targetHeight;
        oldRatio = Screen.width / Screen.height;
    }

    // Update is called once per frame
    void Update()
    {
        float width = Screen.width;
        float height = Screen.height;
        float currentRatio = width / height;

        if(currentRatio != oldRatio)
        {
            width = targetWidth * (currentRatio / targetRatio);
            height = targetHeight;

            foreach (RectTransform canvas in canvases)
            {
                canvas.sizeDelta = new Vector2(width, height);
            }
        }

        oldRatio = currentRatio;
    }
}
