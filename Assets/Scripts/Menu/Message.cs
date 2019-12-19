using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Message : MonoBehaviour
{
    //Message that is spawned from the mouse position to give basic information about an action the player took
    //such as spending money or invalid object placement
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    [SerializeField] private float time; //Time spent fading out    
    [SerializeField] private float distance; //Distance to move when fading out

    public void setText(string text, Color colour)
    {
        //Sets the text of the message and starts fade out routine
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        TMP_Text tmpText = GetComponent<TMP_Text>();
        tmpText.text = text;
        tmpText.color = colour;

        StartCoroutine(LerpFadeOut());
    }

    private IEnumerator LerpFadeOut()
    {
        //Gradually moves message up and fades out before destroying self
        float elapsedTime = 0;
        Vector3 oldTransform = rectTransform.localPosition;
        Vector3 newTransform = rectTransform.localPosition + new Vector3(0, distance, 0);

        while (elapsedTime < time)
        {
            rectTransform.localPosition = Vector3.Lerp(oldTransform, newTransform, (elapsedTime / time));
            canvasGroup.alpha = 1.0f - (elapsedTime / time);

            elapsedTime += Time.unscaledDeltaTime;
            yield return new WaitForEndOfFrame();
        }

        Destroy(gameObject);
    }
}
