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

    [SerializeField] private RectTransform toolSelector;
    [SerializeField] private RectTransform startPointSelect;
    [SerializeField] private RectTransform waveCount;
    [SerializeField] private RectTransform nextWaveButton;
    [SerializeField] private RectTransform startSelectHeading;
    [SerializeField] private RectTransform doubleSpeedButton;

    [SerializeField] private RectTransform toolsOffPosition;
    [SerializeField] private RectTransform toolsOnPosition;
    [SerializeField] private RectTransform nextWaveOffPosition;
    [SerializeField] private RectTransform nextWaveOnPosition;
    [SerializeField] private RectTransform waveCounterOffPosition;
    [SerializeField] private RectTransform waveCounterOnPosition;

    [SerializeField] private float gameOverDelay;
    [SerializeField] private GameObject gameOverCanvas;
    [SerializeField] private RectTransform gameOverScreen;
    [SerializeField] private RectTransform gameOverOffPosition;
    [SerializeField] private RectTransform gameOverOnPosition;

    [SerializeField] private float hudMoveTime;

    [SerializeField] private Toggle doubleSpeedToggle;
    private bool isDoubleSpeed;
    [SerializeField]private float doubleSpeedMultiplier;

    [SerializeField] private GameObject pauseMenu;

    private Vector3Int selectedLevelSize;
    private Vector2 selectedLevelGroundFillBounds;
    private Vector2 selectedLevelObstacleFillBounds;
    private float selectedLevelPreviewCameraSize;
    private int selectedLevelScaleFactor;

    private float obstacleFillRate;
    private float groundFillRate;

    private Coroutine currentLerpRoutine;

    [SerializeField] private Camera previewCamera;

    private Environment mMap;
    private Game game;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            resumeGame();
        }
    }

    private IEnumerator lerpMenuItem(RectTransform item, RectTransform start, RectTransform destination, float time, float delay = 0)
    {
        float elapsedTime = 0;
        float speed;

        while(elapsedTime < delay)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return new WaitForEndOfFrame();
        }

        elapsedTime = 0;

        while (elapsedTime < time)
        {
            item.position = Vector3.Lerp(start.position, destination.position, (elapsedTime / time));

            //Speed is 1 minus a point graph of x^2 so that speed is maximum at the midpoint of the lerp operation and minimum at the start and end points
            speed = Mathf.Max(1.0f - Mathf.Pow(((-time / 2.0f) + elapsedTime) / (-time / 2.0f), 2.0f), 0.05f);
            elapsedTime += Time.unscaledDeltaTime * speed;
            yield return new WaitForEndOfFrame();
        }

        //Ensure position is exact
        item.position = destination.position;
    }

    private IEnumerator lerpToPosition(Vector3 start, Vector3 destination, float time)
    {
        float elapsedTime = 0;
        float speed;

        while (elapsedTime < time)
        {
            transform.position = Vector3.Lerp(start, destination, (elapsedTime / time));

            //Speed is 1 minus a point graph of x^2 so that speed is maximum at the midpoint of the lerp operation and minimum at the start and end points
            speed = Mathf.Max(1.0f - Mathf.Pow(((-time / 2.0f) + elapsedTime) / (-time / 2.0f), 2.0f), 0.05f);
            elapsedTime += Time.unscaledDeltaTime * speed;
            yield return new WaitForEndOfFrame();
        }

        //Ensure position is exact
        transform.position = destination;
    }

    private IEnumerator screenFader(float startAlpha, float endAlpha, float time, Action callback = null, float delay = 0)
    {
        float elapsedTime = 0;
        float speed;
        fadeScreen.alpha = startAlpha;
        fadeScreen.blocksRaycasts = true;
        yield return new WaitForSecondsRealtime(delay);

        while (elapsedTime < time)
        {
            fadeScreen.alpha =  Mathf.Lerp(startAlpha, endAlpha, (elapsedTime / time));

            //Speed is 1 minus a point graph of x^2 so that speed is maximum at the midpoint of the lerp operation and minimum at the start and end points
            speed = Mathf.Max(1.0f - Mathf.Pow(((-time / 2.0f) + elapsedTime) / (-time / 2.0f), 2.0f), 0.5f);
            elapsedTime += Time.unscaledDeltaTime * speed;
            yield return new WaitForEndOfFrame();
        }

        //Ensure position is exact
        fadeScreen.alpha = endAlpha;
        fadeScreen.blocksRaycasts = false;
        callback?.Invoke();
    }

    public void backToTitle()
    {
        helpScreen.interactable = false;
        playScreen.interactable = false;
        titleScreeen.interactable = true;
        previewCamera.enabled = false;
        if(currentLerpRoutine != null)
        {
            StopCoroutine(currentLerpRoutine);
        }
        currentLerpRoutine = StartCoroutine(lerpToPosition(transform.position, mainScreenPosition.position, menuMoveTime));
    }

    private void Start()
    {
        StartCoroutine(screenFader(1, 0, fadeTime, delay: 1.0f));

        Time.timeScale = 1;

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
        if (currentLerpRoutine != null)
        {
            StopCoroutine(currentLerpRoutine);
        }
        currentLerpRoutine = StartCoroutine(lerpToPosition(transform.position, playScreenPosition.position, menuMoveTime));
    }

    public void helpButton()
    {
        helpScreen.interactable = true;
        titleScreeen.interactable = false;
        if (currentLerpRoutine != null)
        {
            StopCoroutine(currentLerpRoutine);
        }
        currentLerpRoutine = StartCoroutine(lerpToPosition(transform.position, helpScreenPosition.position, menuMoveTime));
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
            titleScreeen.gameObject.SetActive(false);
            helpScreen.gameObject.SetActive(false);
            playScreen.gameObject.SetActive(false);
            playHud.gameObject.SetActive(true);
            //Reset game hud to starting positions
            startPointSelect.GetComponentInChildren<Button>().interactable = true;
            nextWaveButton.GetComponent<Button>().interactable = true;
            toolSelector.GetComponent<CanvasGroup>().interactable = true;
            StartCoroutine(lerpMenuItem(toolSelector, toolsOnPosition, toolsOffPosition, hudMoveTime));
            StartCoroutine(lerpMenuItem(startPointSelect, toolsOffPosition, toolsOnPosition, hudMoveTime));
            StartCoroutine(lerpMenuItem(startSelectHeading, waveCounterOffPosition, waveCounterOnPosition, hudMoveTime));
            StartCoroutine(lerpMenuItem(nextWaveButton, nextWaveOnPosition, nextWaveOffPosition, hudMoveTime));
            StartCoroutine(lerpMenuItem(doubleSpeedButton, nextWaveOnPosition, nextWaveOffPosition, hudMoveTime));
            StartCoroutine(lerpMenuItem(waveCount, waveCounterOnPosition, waveCounterOffPosition, hudMoveTime));
            StartCoroutine(lerpMenuItem(gameOverScreen, gameOverOnPosition, gameOverOffPosition, hudMoveTime, gameOverDelay));

            //Game starts after screen fades out
            setNewMapParameters();
            game.startGame();

            StartCoroutine(screenFader(1, 0, fadeTime, delay: 1.0f));
        }

        StartCoroutine(screenFader(0, 1, fadeTime, playCallBack));
    }

    private void setNewMapParameters()
    {
        mMap.CleanUpWorld();

        mMap.initialSize = new Vector2Int(selectedLevelSize.x, selectedLevelSize.y);
        mMap.scaleUpFactor = selectedLevelSize.z;
        previewCamera.orthographicSize = selectedLevelPreviewCameraSize;
        mMap.LandFillPercent = selectedLevelGroundFillBounds.x + groundFillRate * (selectedLevelGroundFillBounds.y - selectedLevelGroundFillBounds.x);
        mMap.AccessiblePercentage = 1 - (selectedLevelObstacleFillBounds.x + obstacleFillRate * (selectedLevelObstacleFillBounds.y - selectedLevelObstacleFillBounds.x));
        mMap.seed = seedText.text;

        isDoubleSpeed = false;
    }

    public void previewMap()
    {
        setNewMapParameters();
        game.Generate();
    }

    //In game HUD methods
    public void startPlaced()
    {
        startPointSelect.GetComponentInChildren<Button>().interactable = false;

        StartCoroutine(lerpMenuItem(toolSelector, toolsOffPosition, toolsOnPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(startPointSelect, toolsOnPosition, toolsOffPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(startSelectHeading, waveCounterOnPosition, waveCounterOffPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(nextWaveButton, nextWaveOffPosition, nextWaveOnPosition, hudMoveTime));
    }

    public void startWave(bool earlyStart)
    {
        nextWaveButton.GetComponent<Button>().interactable = false;
        toolSelector.GetComponent<CanvasGroup>().interactable = false;
        doubleSpeedToggle.interactable = true;
        StartCoroutine(lerpMenuItem(nextWaveButton, nextWaveOnPosition, nextWaveOffPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(waveCount, waveCounterOffPosition, waveCounterOnPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(toolSelector, toolsOnPosition, toolsOffPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(doubleSpeedButton, nextWaveOffPosition, nextWaveOnPosition, hudMoveTime));

        game.startWave(earlyStart);
    }

    public void endWave()
    {
        nextWaveButton.GetComponent<Button>().interactable = true;
        toolSelector.GetComponent<CanvasGroup>().interactable = true;
        doubleSpeedToggle.interactable = false;
        StartCoroutine(lerpMenuItem(nextWaveButton, nextWaveOffPosition, nextWaveOnPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(waveCount, waveCounterOnPosition, waveCounterOffPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(toolSelector, toolsOffPosition, toolsOnPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(doubleSpeedButton, nextWaveOnPosition, nextWaveOffPosition, hudMoveTime));

        isDoubleSpeed = false;
        doubleSpeedToggle.isOn = false;
    }

    public void doubleSpeed(bool isDouble)
    {
        isDoubleSpeed = isDouble;
        if(isDouble)
        {
            Time.timeScale = doubleSpeedMultiplier;
        }
        else
        {
            Time.timeScale = 1;
        }
    }

    public void resumeGame()
    {
        pauseMenu.SetActive(false);
        doubleSpeed(isDoubleSpeed);
    }

    public void pauseGame()
    {
        if (pauseMenu.activeSelf)
        {
            resumeGame();
        }
        else
        {
            pauseMenu.SetActive(true);
            Time.timeScale = 0;
        }
    }

    public void quitToMenu()
    {
        void endCallBack()
        {
            //Game starts after screen fades out
            titleScreeen.gameObject.SetActive(true);
            helpScreen.gameObject.SetActive(true);
            playScreen.gameObject.SetActive(true);
            playHud.gameObject.SetActive(false);
            gameOverCanvas.SetActive(false);
            game.quitToMenu();
            resumeGame();
            backToTitle();
            previewMap();
            StartCoroutine(screenFader(1, 0, fadeTime, delay: 1.0f));
        }
        StartCoroutine(screenFader(0, 1, fadeTime, endCallBack));
    }

    public void gameOver()
    {
        gameOverCanvas.SetActive(true);
        doubleSpeed(false);
        StartCoroutine(lerpMenuItem(gameOverScreen, gameOverOffPosition, gameOverOnPosition, hudMoveTime, gameOverDelay));
    }
}
