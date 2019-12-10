using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class MenuController : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeScreen;
    [SerializeField] private CanvasGroup exitConfirm;
    [SerializeField] private CanvasGroup titleScreeen;
    [SerializeField] private CanvasGroup helpScreen;
    [SerializeField] private CanvasGroup playScreen;
    [SerializeField] private CanvasGroup playHud;

    [SerializeField] private Transform mainScreenPosition;
    [SerializeField] private Transform helpScreenPosition;
    [SerializeField] private Transform playScreenPosition;
    [SerializeField] private float menuMoveTime;
    [SerializeField] private float fadeTime;

    [SerializeField] Slider groundFillSlider;
    [SerializeField] Slider obstacleFillSlider;
    [SerializeField] TMP_InputField seedText;

    [SerializeField] private float smallLevelPreviewCameraSize;
    [SerializeField] private Vector3Int smallLevelSize;
    [SerializeField] private Vector2 smallLevelGroundFillBounds;
    [SerializeField] private Vector2 smallLevelObstacleFillBounds;
    [SerializeField] private float mediumLevelPreviewCameraSize;
    [SerializeField] private Vector3Int mediumLevelSize;
    [SerializeField] private Vector2 mediumLevelGroundFillBounds;
    [SerializeField] private Vector2 mediumLevelObstacleFillBounds;
    [SerializeField] private float largeLevelPreviewCameraSize;
    [SerializeField] private Vector3Int largeLevelSize;
    [SerializeField] private Vector2 largeLevelGroundFillBounds;
    [SerializeField] private Vector2 largeLevelObstacleFillBounds;

    private Vector3Int selectedLevelSize;
    private Vector2 selectedLevelGroundFillBounds;
    private Vector2 selectedLevelObstacleFillBounds;
    private float selectedLevelPreviewCameraSize;
    private int selectedLevelScaleFactor;

    private float obstacleFillRate;
    private float groundFillRate;

    [SerializeField] private Camera previewCamera;

    private Environment mMap;
    private Game game;

    private IEnumerator lerpToPosition(Vector3 start, Vector3 destination, float time)
    {
        float elapsedTime = 0;
        float speed;

        while (elapsedTime < time)
        {
            transform.position = Vector3.Lerp(start, destination, (elapsedTime / time));

            //Speed is 1 minus a point graph of x^2 so that speed is maximum at the midpoint of the lerp operation and minimum at the start and end points
            speed = Mathf.Max(1.0f - Mathf.Pow(((-time / 2.0f) + elapsedTime) / (-time / 2.0f), 2.0f), 0.5f);
            elapsedTime += Time.deltaTime * speed;
            yield return new WaitForEndOfFrame();
        }

        //Ensure position is exact
        transform.position = destination;
    }

    private IEnumerator screenFader(float startAlpha, float endAlpha, float time, Action callback = null)
    {
        float elapsedTime = 0;
        float speed;

        while (elapsedTime < time)
        {
            fadeScreen.alpha =  Mathf.Lerp(startAlpha, endAlpha, (elapsedTime / time));

            //Speed is 1 minus a point graph of x^2 so that speed is maximum at the midpoint of the lerp operation and minimum at the start and end points
            speed = Mathf.Max(1.0f - Mathf.Pow(((-time / 2.0f) + elapsedTime) / (-time / 2.0f), 2.0f), 0.5f);
            elapsedTime += Time.deltaTime * speed;
            yield return new WaitForEndOfFrame();
        }

        //Ensure position is exact
        fadeScreen.alpha = endAlpha;

        callback?.Invoke();
    }

    public void backToTitle()
    {
        helpScreen.interactable = false;
        playScreen.interactable = false;
        titleScreeen.interactable = true;
        previewCamera.enabled = false;
        StopAllCoroutines();
        StartCoroutine(lerpToPosition(transform.position, mainScreenPosition.position, menuMoveTime));
    }

    private void Start()
    {
        StartCoroutine(screenFader(1, 0, fadeTime));

        mMap = GetComponentInChildren<Environment>();
        game = GetComponent<Game>();
        changeWorldSize(0);
        changeGroundFillSlider();
        changeObstacleFillSlider();
        randomSeed();
        previewMap();
    }

    //Methods used by main menu screen

    public void openExitConfirmMenu()
    {
        titleScreeen.interactable = false;
        exitConfirm.interactable = true;
        exitConfirm.blocksRaycasts = true;
        exitConfirm.alpha = 1;
    }

    public void cancelExit()
    {
        titleScreeen.interactable = true;
        exitConfirm.interactable = false;
        exitConfirm.blocksRaycasts = false;
        exitConfirm.alpha = 0;
    }

    public void exitGame()
    {
        print("exit");
    }

    public void playButton()
    {
        playScreen.interactable = true;
        titleScreeen.interactable = false;
        previewCamera.enabled = true;
        StopAllCoroutines();
        StartCoroutine(lerpToPosition(transform.position, playScreenPosition.position, menuMoveTime));
    }

    public void helpButton()
    {
        helpScreen.interactable = true;
        titleScreeen.interactable = false;
        StopAllCoroutines();
        StartCoroutine(lerpToPosition(transform.position, helpScreenPosition.position, menuMoveTime));
    }

    //Play menu methods
    public void changeGroundFillSlider()
    {
        groundFillRate = groundFillSlider.value;
    }

    public void changeObstacleFillSlider()
    {
        obstacleFillRate = obstacleFillSlider.value;
    }

    public void changeWorldSize(int size)
    {
        switch (size)
        {
            case 0:
                selectedLevelSize = smallLevelSize;
                selectedLevelGroundFillBounds = smallLevelGroundFillBounds;
                selectedLevelObstacleFillBounds = smallLevelObstacleFillBounds;
                selectedLevelPreviewCameraSize = smallLevelPreviewCameraSize;

                break;
            case 1:
                selectedLevelSize = mediumLevelSize;
                selectedLevelGroundFillBounds = mediumLevelGroundFillBounds;
                selectedLevelObstacleFillBounds = mediumLevelObstacleFillBounds;
                selectedLevelPreviewCameraSize = mediumLevelPreviewCameraSize;
                break;
            case 2:
                selectedLevelSize = largeLevelSize;
                selectedLevelGroundFillBounds = largeLevelGroundFillBounds;
                selectedLevelObstacleFillBounds = largeLevelObstacleFillBounds;
                selectedLevelPreviewCameraSize = largeLevelPreviewCameraSize;
                break;
        }
    }

    public void randomSeed()
    {
        seedText.text = UnityEngine.Random.value.ToString();
    }

    public void play()
    {
        void playCallBack()
        {
            //Game starts after screen fades out
            setNewParameters();
            game.startGame();
            titleScreeen.gameObject.SetActive(false);
            helpScreen.gameObject.SetActive(false);
            playScreen.gameObject.SetActive(false);
            playHud.gameObject.SetActive(true);
            StartCoroutine(screenFader(1, 0, fadeTime));
        }

        StartCoroutine(screenFader(0, 1, fadeTime, playCallBack));
    }

    private void setNewParameters()
    {
        mMap.CleanUpWorld();

        mMap.initialSize = new Vector2Int(selectedLevelSize.x, selectedLevelSize.y);
        mMap.scaleUpFactor = selectedLevelSize.z;
        previewCamera.orthographicSize = selectedLevelPreviewCameraSize;
        mMap.LandFillPercent = selectedLevelGroundFillBounds.x + groundFillRate * (selectedLevelGroundFillBounds.y - selectedLevelGroundFillBounds.x);
        mMap.AccessiblePercentage = 1 - (selectedLevelObstacleFillBounds.x + obstacleFillRate * (selectedLevelObstacleFillBounds.y - selectedLevelObstacleFillBounds.x));
        mMap.seed = seedText.text;
    }

    public void previewMap()
    {
        setNewParameters();
        game.Generate();
    }
}
