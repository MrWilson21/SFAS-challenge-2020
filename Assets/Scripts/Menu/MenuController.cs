using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class MenuController : MonoBehaviour
{
    //Controls the flow of the menu system and handles the pausing and speeding up of gameplay
    //Moves ui elements in and out of view as they are needed

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

    private Vector3Int selectedLevelSize; //width, height and scale up factor of currently selected world size
    private Vector2 selectedLevelGroundFillBounds; //Minimum and maximum ground fill rate of currently selected world size
    private Vector2 selectedLevelObstacleFillBounds; //Minimum and maximum obstacle fill rate of currently selected world size
    private float selectedLevelPreviewCameraSize; //Size of camera when capturing preview image
    private int selectedLevelScaleFactor;

    private float obstacleFillRate;
    private float groundFillRate;

    private Coroutine currentLerpRoutine; //Current routine should be stopped before starting a new one

    [SerializeField] private Camera previewCamera;

    private Environment mMap;
    private Game game;

    private AudioSource audioSource;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip playMusic;
    [SerializeField] private AudioClip waveMusic;
    [SerializeField] private AudioSource waveStartSound;
    [SerializeField] private AudioSource waveEndSound;
    [SerializeField] private AudioSource buttonPressSound;
    [SerializeField] private AudioSource loseGameSound;
    [Range(0,1)][SerializeField] private float musicVolume;
    [SerializeField] private float musicFadeTime;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            resumeGame();
        }
    }

    private IEnumerator musicFader(float startVolume, float endVolume, float time, float delay = 0, Action callBack = null)
    {
        //Fades music in and out again
        //callback function used to do action after fading
        float elapsedTime = 0;
        audioSource.volume = startVolume;
        yield return new WaitForSecondsRealtime(delay);

        while (elapsedTime < time)
        {
            audioSource.volume = Mathf.Lerp(startVolume, endVolume, (elapsedTime / time));
            elapsedTime += Time.unscaledDeltaTime;
            yield return new WaitForEndOfFrame();
        }

        //Ensure volume is exact
        audioSource.volume = endVolume;

        callBack?.Invoke();
    }

    private IEnumerator lerpMenuItem(RectTransform item, RectTransform start, RectTransform destination, float time, float delay = 0)
    {
        //Smoothly moves UI item to a new location
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
        //Smoothly moves whole menu to a new location to change screens
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
        //Fade screen to a white background 
        //callback function used to do action after fading
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

        //Ensure fade is exact
        fadeScreen.alpha = endAlpha;
        fadeScreen.blocksRaycasts = false;
        callback?.Invoke();
    }

    public void backToTitle()
    {
        //Moves to title screen
        helpScreen.interactable = false;
        playScreen.interactable = false;
        titleScreeen.interactable = true;
        previewCamera.enabled = false;
        //End current lerp routine before starting new one
        if (currentLerpRoutine != null)
        {
            StopCoroutine(currentLerpRoutine);
        }
        currentLerpRoutine = StartCoroutine(lerpToPosition(transform.position, mainScreenPosition.position, menuMoveTime));
    }

    private void Start()
    {
        //Get component values and set up initial preview map
        StartCoroutine(screenFader(1, 0, fadeTime, delay: 1.0f));
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = menuMusic;
        audioSource.Play();
        StartCoroutine(musicFader(0, musicVolume, musicFadeTime));

        Time.timeScale = 1;

        mMap = GetComponentInChildren<Environment>();
        game = GetComponent<Game>();
        changeWorldSize(0);
        changeGroundFillSlider();
        changeObstacleFillSlider();
        randomSeed();
        previewMap();
    }

    public void pressButton()
    {
        //Create button press sound
        Destroy(Instantiate(buttonPressSound, transform), 10.0f);
    }

    //Methods used by main menu screen

    public void openExitConfirmMenu()
    {
        //Opens menu to exit game 
        titleScreeen.interactable = false;
        exitConfirm.interactable = true;
        exitConfirm.blocksRaycasts = true;
        exitConfirm.alpha = 1;
    }

    public void cancelExit()
    {
        //Closes menu to exit game
        titleScreeen.interactable = true;
        exitConfirm.interactable = false;
        exitConfirm.blocksRaycasts = false;
        exitConfirm.alpha = 0;
    }

    public void exitGame()
    {
        //Ends application
        Application.Quit();
    }

    public void goToPlayScreen()
    {
        //Moves to play screen
        playScreen.interactable = true;
        titleScreeen.interactable = false;
        previewCamera.enabled = true;
        //End current lerp routine before starting new one
        if (currentLerpRoutine != null)
        {
            StopCoroutine(currentLerpRoutine);
        }
        currentLerpRoutine = StartCoroutine(lerpToPosition(transform.position, playScreenPosition.position, menuMoveTime));
    }

    public void helpButton()
    {
        //Moves to help screen
        helpScreen.interactable = true;
        titleScreeen.interactable = false;
        //End current lerp routine before starting new one
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
        //Select new world size and update world parameters
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
        //callback after fading screen
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
            //Change music
            audioSource.clip = playMusic;
            audioSource.Play();
            StartCoroutine(musicFader(0, musicVolume, musicFadeTime));
        }

        //Fade screen
        StartCoroutine(screenFader(0, 1, fadeTime, playCallBack));
        StartCoroutine(musicFader(musicVolume, 0, musicFadeTime));
    }

    private void setNewMapParameters()
    {
        //Cleans world before giving new parameters
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
        //create new preview image
        setNewMapParameters();
        game.Generate();
    }

    //In game HUD methods
    public void startPlaced()
    {
        //Replace starting ui items with build ui items
        startPointSelect.GetComponentInChildren<Button>().interactable = false;

        StartCoroutine(lerpMenuItem(toolSelector, toolsOffPosition, toolsOnPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(startPointSelect, toolsOnPosition, toolsOffPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(startSelectHeading, waveCounterOnPosition, waveCounterOffPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(nextWaveButton, nextWaveOffPosition, nextWaveOnPosition, hudMoveTime));
    }

    public void startWave(bool earlyStart)
    {
        //Begin new wave
        //Move build ui items away and wave ui items in
        nextWaveButton.GetComponent<Button>().interactable = false;
        toolSelector.GetComponent<CanvasGroup>().interactable = false;
        doubleSpeedToggle.interactable = true;
        StartCoroutine(lerpMenuItem(nextWaveButton, nextWaveOnPosition, nextWaveOffPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(waveCount, waveCounterOffPosition, waveCounterOnPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(toolSelector, toolsOnPosition, toolsOffPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(doubleSpeedButton, nextWaveOffPosition, nextWaveOnPosition, hudMoveTime));

        //Change to wave music
        void musicChangeCallback()
        {
            audioSource.clip = waveMusic;
            audioSource.Play();
            StartCoroutine(musicFader(0, musicVolume, musicFadeTime));
        }
        StartCoroutine(musicFader(musicVolume, 0, musicFadeTime, callBack: musicChangeCallback));
        //Create wave start sound
        Destroy(Instantiate(waveStartSound, transform), 10.0f);

        game.startWave(earlyStart);
    }

    public void endWave()
    {
        //End wave
        //Move wave ui items away and bring build ui items back in
        nextWaveButton.GetComponent<Button>().interactable = true;
        toolSelector.GetComponent<CanvasGroup>().interactable = true;
        doubleSpeedToggle.interactable = false;
        StartCoroutine(lerpMenuItem(nextWaveButton, nextWaveOffPosition, nextWaveOnPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(waveCount, waveCounterOnPosition, waveCounterOffPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(toolSelector, toolsOffPosition, toolsOnPosition, hudMoveTime));
        StartCoroutine(lerpMenuItem(doubleSpeedButton, nextWaveOnPosition, nextWaveOffPosition, hudMoveTime));

        isDoubleSpeed = false;
        doubleSpeedToggle.isOn = false;

        //change music to build music
        void musicChangeCallback()
        {
            audioSource.clip = playMusic;
            audioSource.Play();
            StartCoroutine(musicFader(0, musicVolume, musicFadeTime));
        }
        StartCoroutine(musicFader(musicVolume, 0, musicFadeTime, callBack: musicChangeCallback));

        Destroy(Instantiate(waveEndSound, transform), 10.0f);
    }

    public void doubleSpeed(bool isDouble)
    {
        //toggles double speed
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
        //End game and bring title screen up
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
            audioSource.clip = menuMusic;
            audioSource.Play();
            StartCoroutine(musicFader(0, musicVolume, musicFadeTime));
        }
        doubleSpeed(false);
        StartCoroutine(screenFader(0, 1, fadeTime, endCallBack));
        StartCoroutine(musicFader(musicVolume, 0, musicFadeTime));
    }

    public void gameOver()
    {
        //Show game over menu
        gameOverCanvas.SetActive(true);
        doubleSpeed(false);
        StartCoroutine(lerpMenuItem(gameOverScreen, gameOverOffPosition, gameOverOnPosition, hudMoveTime, gameOverDelay));
        Destroy(Instantiate(loseGameSound, transform), 10.0f);
    }
}
