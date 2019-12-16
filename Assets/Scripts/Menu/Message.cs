using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Message : MonoBehaviour
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    [SerializeField] private float time;
    [SerializeField] private float distance;

    public void setText(string text, Color colour)
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        TMP_Text tmpText = GetComponent<TMP_Text>();
        tmpText.text = text;
        tmpText.color = colour;

        StartCoroutine(LerpFadeOut());
    }

    private IEnumerator LerpFadeOut()
    {
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
