using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    [SerializeField] private Canvas fadeScreen;

    [SerializeField] private Transform mainScreenPosition;
    [SerializeField] private Transform helpScreenPosition;
    [SerializeField] private float menuMoveTime;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void toggleScreenFader(bool enable)
    {
        fadeScreen.enabled = enable;
    }

    public void exitGame()
    {

    }

    public void playButton()
    {

    }

    public void helpButton()
    {
        StopAllCoroutines();
        StartCoroutine(lerpToPosition(transform.position, helpScreenPosition.position, menuMoveTime));
    }

    public void backToTitle()
    {
        StopAllCoroutines();
        StartCoroutine(lerpToPosition(transform.position, mainScreenPosition.position, menuMoveTime));
    }

    private IEnumerator lerpToPosition(Vector3 start, Vector3 destination, float time)
    {
        float elapsedTime = 0;
        float speed;

        while (elapsedTime < time)
        {
            transform.position = Vector3.Lerp(start, destination, (elapsedTime / time));

            //Speed is 1 minus a point graph of x^2 so that speed is maximum at the midpoint of the lerp operation and minimum at the start and end points
            speed = Mathf.Max(1.0f - Mathf.Pow(((-time/2.0f) + elapsedTime) / (-time / 2.0f), 2.0f), 0.5f);
            elapsedTime += Time.deltaTime * speed;
            yield return new WaitForEndOfFrame();
        }

        //Ensure position is exact
        transform.position = destination;
    }

}
