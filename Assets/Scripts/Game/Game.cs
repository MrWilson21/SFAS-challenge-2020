using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class Game : MonoBehaviour
{
    [SerializeField] private Camera MainCamera;
    private CameraController cameraController;
    [SerializeField] private int numberOfEnemySpawners;
    [SerializeField] private AnimationCurve moneyFromEnemiesCurve;

    private RaycastHit[] mRaycastHits;
    private Environment mMap;
    private MenuController menu;

    private readonly int NumberOfRaycastHits = 1;

    [SerializeField] private List<EnvironmentTile> environmentTileSelection;
    [SerializeField] private List<GameObject> turretSelection;

    //Current tile mouse is hovering over
    private EnvironmentTile currentTile;

    //Currently selected turret to add
    private GameObject turretPrefab;
    private GameObject objectToPlace; //Instance of turretPrefab

    //New tile to replace old one with
    private EnvironmentTile tilePrefab;

    private bool isUsingPlaceTool = false;
    private bool isUsingDestroyTool = false;
    private bool tileIsHighlighted = false;
    private bool startPlaced = false;

    private bool playingGame = false;

    private int tileRotation = 0; //0 up, 1 right, 2 down, 3 left

    private List<Spawner> spawners;

    private WaveSpawner waveSpawner;
    private float timeUntilWave;
    [SerializeField] private float waveDelay;
    private bool isDoingWave;
    private int waveCount;
    private int enemyCount;

    [SerializeField] private Button cancelToolButton;
    [SerializeField] private TMP_Text waveNumber;
    [SerializeField] private TMP_Text numberOfEnemies;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text nextWaveBonus;
    [SerializeField] private TMP_Text nextWaveCountdown;

    [SerializeField] private int startingHealth;
    [SerializeField] private int startingMoney;

    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image healthIcon;
    [SerializeField] private Image healthFill;
    [SerializeField] Gradient healthGradient;

    [SerializeField] private TMP_Text buyPriceText;
    [SerializeField] private RectTransform buyPrice;
    [SerializeField] private RectTransform hudCanvas;
    [SerializeField] private Color sellColour;
    [SerializeField] private Color buyColour;

    [SerializeField] private RectTransform destroyIcon;

    private int numberOfMachineGuns;
    [SerializeField] private int machineGunBaseCost;
    private int machineGunCost;
    [SerializeField] private float machineGunIncrement;
    private int numberOfMortars;
    [SerializeField] private TMP_Text machineGunCostText;
    [SerializeField] private int mortarBaseCost;
    private int mortarCost;
    [SerializeField] private float mortarIncrement;
    [SerializeField] private TMP_Text mortarCostText;

    [SerializeField] private int costToRemoveObstacles;

    [SerializeField] private Message messagePrefab;

    private int health;
    private int money;

    public bool gameOver { get; set; }

    void Start()
    {
        mRaycastHits = new RaycastHit[NumberOfRaycastHits];
        mMap = GetComponentInChildren<Environment>();
        menu = GetComponent<MenuController>();
        waveSpawner = GetComponent<WaveSpawner>();
        cameraController = MainCamera.GetComponent<CameraController>();
        mMap.costToRemoveObstacles = costToRemoveObstacles;
    }

    private void Update()
    {
        if (playingGame)
        {
            doGameUpdate();
        }
    }

    private void doGameUpdate()
    {
        if (currentTile != null && currentTile.canBeDestroyed && !currentTile.IsAccessible)
        {
            currentTile.GetComponent<ColorSwapper>().restoreColour();
        }
        tileIsHighlighted = getMouseTile();

        if(objectToPlace != null)
        {
            objectToPlace.SetActive(false);
            if (Input.GetKeyDown(KeyCode.R))
            {
                tileRotation = (tileRotation + 1) % 4;
                objectToPlace.transform.transform.Rotate(new Vector3(0, 1, 0), 90);
            }
        }

        buyPrice.gameObject.SetActive(false);
        destroyIcon.gameObject.SetActive(false);

        if (isUsingPlaceTool && tileIsHighlighted)
        {
            if(!startPlaced)
            {
                placeStartPoint();
            }
            else
            {
                usePlaceTool();
            }
        }
        else if (isUsingDestroyTool && tileIsHighlighted)
        {
            useDestroyTool();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            cancelTool();
        }

        if(!isDoingWave && startPlaced)
        {
            timeUntilWave -= Time.deltaTime;
            if (timeUntilWave <= 0)
            {
                menu.startWave(false);
            }
            else
            {
                nextWaveCountdown.text = "Next wave in " + (int)timeUntilWave;
                nextWaveBonus.text = "<< Start now for bonus $" + calculateNextWaveBonus();
            }
        }
    }

    private int calculateNextWaveBonus()
    {
        float money = Mathf.Log(timeUntilWave, 3);
        money *= moneyFromEnemiesCurve.Evaluate(waveCount);
        return (int)money;
    }

    private void updateMoney(int moneyToAdd)
    {
        money += moneyToAdd;
        moneyText.text = "$" + money;
        Message message = Instantiate(messagePrefab, hudCanvas);
        message.GetComponent<RectTransform>().localPosition = moneyText.GetComponent<RectTransform>().localPosition + new Vector3(120, 0, 0);
        message.setText((moneyToAdd >= 0 ? "+" : "-") + "$" + moneyToAdd.ToString(), moneyToAdd >= 0 ? sellColour : buyColour);
    }

    private void setHealth(int health)
    {
        this.health = health;
        healthSlider.value = health;
        healthText.text = health.ToString() + " / " + startingHealth.ToString();

        healthFill.color = healthGradient.Evaluate((float)health / (float)startingHealth);
        healthIcon.color = healthGradient.Evaluate((float)health / (float)startingHealth);
        healthText.color = healthGradient.Evaluate((float)health / (float)startingHealth);
    }

    private void setBuyText(int cost)
    {
        Vector3 pos = MainCamera.ScreenToViewportPoint(Input.mousePosition);
        pos.x = (pos.x - 0.5f) * hudCanvas.sizeDelta.x;
        pos.y = (pos.y - 0.5f) * hudCanvas.sizeDelta.y;
        buyPrice.localPosition = pos;

        buyPriceText.text = (Mathf.Sign(cost) == 0 ? "" : "-") +  "$" + Mathf.Abs(cost).ToString();

        buyPrice.gameObject.SetActive(true);
        if(cost > 0)
        {
            buyPriceText.color = buyColour;
        }
        else
        {
            buyPriceText.color = sellColour;
        }
    }

    private void buyItem(int cost)
    {
        updateMoney(-cost);
        sendMessage(buyPriceText.text, buyPriceText.color);
    }

    private void sendMessage(string text, Color? colour = null)
    {
        Message message = Instantiate(messagePrefab, hudCanvas);
        Vector3 pos = MainCamera.ScreenToViewportPoint(Input.mousePosition);
        pos.x = (pos.x - 0.5f) * hudCanvas.sizeDelta.x;
        pos.y = (pos.y - 0.5f) * hudCanvas.sizeDelta.y;
        message.GetComponent<RectTransform>().localPosition = pos;

        message.setText(text, colour ?? buyColour);
    }

    private void updateEnemyCount(int enemiesToRemove)
    {
        enemyCount -= enemiesToRemove;
        numberOfEnemies.text = "Enemies remaining: " + enemyCount;
    }

    private void usePlaceTool()
    {
        if (tileIsHighlighted)
        {
            objectToPlace.SetActive(true);
            objectToPlace.transform.position = currentTile.Position;
            if (currentTile.IsAccessible && currentTile.canBeDestroyed)
            {
                objectToPlace.GetComponent<ColorSwapper>().swapColour(Color.green);
                Turret turret = tilePrefab.GetComponentInChildren<Turret>();
                int turretCost = 0;
                if (turret != null)
                {
                    turretCost = getTurretCost(tilePrefab);
                    setBuyText(turretCost);
                }

                if (Input.GetMouseButtonDown(0) && turretCost <= money)
                {
                    currentTile = mMap.swapTile(currentTile, tilePrefab, true, false);
                    currentTile.transform.GetChild(0).transform.Rotate(new Vector3(0, 1, 0), 90 * tileRotation);

                    if (mMap.checkIfHouseAccesible())
                    {
                        turret = currentTile.GetComponentInChildren<Turret>();

                        if (turret != null)
                        {
                            turret.setSpawners(spawners);
                            turret.setGame(this);

                            int cost = getTurretCost(currentTile);
                            buyItem(cost);
                            incrementTurretCost(currentTile, 1);
                        }
                        foreach (Spawner spawner in spawners)
                        {
                            spawner.route = mMap.Solve(spawner.spawnExitPoint, mMap.houseEntrance);
                        }
                    }
                    else
                    {
                        sendMessage("Can't block path");
                        mMap.clearTile(currentTile);
                    }
                }
            }
            else
            {
                objectToPlace.GetComponent<ColorSwapper>().swapColour(Color.red);
            }
        }
    }

    private void useDestroyTool()
    {
        Vector3 pos = MainCamera.ScreenToViewportPoint(Input.mousePosition);
        pos.x = (pos.x - 0.5f) * hudCanvas.sizeDelta.x;
        pos.y = (pos.y - 0.5f) * hudCanvas.sizeDelta.y;
        destroyIcon.localPosition = pos;
        destroyIcon.gameObject.SetActive(true);

        if (currentTile.canBeDestroyed && !currentTile.IsAccessible)
        {
            int cost = getTileClearCost(currentTile);
            setBuyText(cost);
            currentTile.GetComponent<ColorSwapper>().swapColour(Color.red);
            if (Input.GetMouseButtonDown(0))
            {
                if(cost <= money)
                {                    
                    buyItem(cost);
                    mMap.clearTile(currentTile);

                    incrementTurretCost(currentTile, -1);
                }
                else
                {
                    sendMessage("Can't afford");
                }
            }
        }
    }

    private void placeStartPoint()
    {
        objectToPlace.SetActive(true);
        objectToPlace.transform.position = currentTile.Position;
        if (currentTile.IsAccessible)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2Int houseEntranceCoord = currentTile.coordinates;

                switch (tileRotation)
                {
                    case 0:
                        houseEntranceCoord += new Vector2Int(0, 1);
                        break;
                    case 1:
                        houseEntranceCoord += new Vector2Int(1, 0);
                        break;
                    case 2:
                        houseEntranceCoord += new Vector2Int(0, -1);
                        break;
                    case 3:
                        houseEntranceCoord += new Vector2Int(-1, 0);
                        break;
                }

                mMap.houseEntrance = mMap.getTileMap()[houseEntranceCoord.x][houseEntranceCoord.y];
                mMap.houseEntrance.canBeDestroyed = false;
                currentTile = mMap.swapTile(currentTile, tilePrefab, false, false);
                currentTile.transform.GetChild(0).transform.Rotate(new Vector3(0, 1, 0), 90 * tileRotation);
                if (mMap.checkIfHouseAccesible())
                {
                    cancelTool();
                    startPlaced = true;

                    foreach (Spawner spawner in spawners)
                    {
                        spawner.housePoint = currentTile;
                        spawner.setGame(this);
                        spawner.route = mMap.Solve(spawner.spawnExitPoint, mMap.houseEntrance);
                    }

                    waveSpawner.setSpawners(spawners);
                    timeUntilWave = waveDelay;
                    menu.startPlaced();
                }
                else
                {
                    mMap.clearTile(currentTile);
                }
            }
            objectToPlace.GetComponent<ColorSwapper>().swapColour(Color.green);
        }
        else
        {
            objectToPlace.GetComponent<ColorSwapper>().swapColour(Color.red);
        }        
    }

    private int getTileClearCost(EnvironmentTile tile)
    {
        incrementTurretCost(tile, -1);
        Turret turret = tile.GetComponentInChildren<Turret>();
        int cost;
        if (turret is MachineGun)
        {
            cost = -machineGunCost;
        }
        else if (turret is Mortar)
        {
            cost = -mortarCost;
        }
        else
        {
            cost = tile.costToRemove;
        }
        incrementTurretCost(tile, 1);
        return cost;
    }

    private int getTurretCost(EnvironmentTile tile)
    {
        Turret turret = tile.GetComponentInChildren<Turret>();
        if (turret is MachineGun)
        {
            return machineGunCost;
        }
        else if (turret is Mortar)
        {
            return mortarCost;
        }

        throw new System.Exception("No turret on selected tile");
    }

    private void incrementTurretCost(EnvironmentTile tile, int increment)
    {
        Turret turret = tile.GetComponentInChildren<Turret>();
        if(turret != null)
        {
            if (turret is MachineGun)
            {
                numberOfMachineGuns += increment;
                machineGunCost = (int)((float)machineGunBaseCost * Mathf.Pow(machineGunIncrement, numberOfMachineGuns));
                machineGunCostText.text = "$" + machineGunCost.ToString();
            }
            else if (turret is Mortar)
            {
                numberOfMortars += increment;
                mortarCost = (int)((float)mortarBaseCost * Mathf.Pow(mortarIncrement, numberOfMortars));
                mortarCostText.text = "$" + mortarCost.ToString();
            }
        }
    }

    private bool getMouseTile()
    {
        //Find which tile the mouse is hovering over
        //If there is one return true and set currentTile
        Ray screenClick = MainCamera.ScreenPointToRay(Input.mousePosition);
        int hits = Physics.RaycastNonAlloc(screenClick, mRaycastHits);
        if (hits > 0 && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            currentTile = mRaycastHits[0].transform.GetComponent<EnvironmentTile>();
            if(currentTile != null)
            {
                return true;               
            }         
        }

        return false;
    }

    public void cancelTool()
    {
        isUsingPlaceTool = false;
        isUsingDestroyTool = false;
        cancelToolButton.gameObject.SetActive(false);
    }

    public void destroyTool()
    {
        isUsingPlaceTool = false;
        isUsingDestroyTool = true;
        cancelToolButton.gameObject.SetActive(true);
    }

    public void selectNewTile(int tileIndex)
    {
        isUsingDestroyTool = false;
        isUsingPlaceTool = true;

        turretPrefab = turretSelection[tileIndex];
        tilePrefab = environmentTileSelection[tileIndex];

        Destroy(objectToPlace);
        objectToPlace = Instantiate(turretPrefab);
        objectToPlace.AddComponent<ColorSwapper>();
        objectToPlace.SetActive(false);
        objectToPlace.transform.transform.Rotate(new Vector3(0, 1, 0), 90 * tileRotation);

        cancelToolButton.gameObject.SetActive(true);
    }

    public void startWave(bool earlyStart)
    {
        waveNumber.text = "Wave " + waveCount;
        waveSpawner.makeWave(waveCount);
        enemyCount = waveSpawner.totalNumberOfEnemies;
        updateEnemyCount(0);
        timeUntilWave = waveDelay;
        isDoingWave = true;
        cancelTool();

        if(earlyStart)
        {
            updateMoney(calculateNextWaveBonus());
        }
    }

    private void endWave()
    {
        waveCount++;
        isDoingWave = false;
    }

    private void loseGame()
    {
        cancelTool();
        gameOver = true;
        menu.gameOver();
        cameraController.loseGame();

        foreach (Spawner spawner in spawners)
        {
            foreach (Enemy enemy in spawner.activeEnemies)
            {
                enemy.winGame();
            }
        }
    }

    public void enemyReachesEnd()
    {
        updateEnemyCount(1);
        setHealth(health - 1);
        if(health == 0)
        {
            loseGame();
        }
        else if (enemyCount == 0)
        {
            menu.endWave();
            endWave();
        }
    }

    public void enemyDie(float moneyMultiplier)
    {
        updateMoney((int)(moneyFromEnemiesCurve.Evaluate(waveCount) * moneyMultiplier));
        updateEnemyCount(1);
        if (enemyCount == 0)
        {
            menu.endWave();
            endWave();
        }
    }

    public void quitToMenu()
    {
        cameraController.endGame();
        playingGame = false;
        mMap.CleanUpWorld();
    }

    public void startGame()
    {
        //Set game variables to starting values
        isUsingPlaceTool = false;
        isUsingDestroyTool = false;
        tileIsHighlighted = false;
        startPlaced = false;
        playingGame = true;
        isDoingWave = false;
        waveCount = 1;
        money = 0;
        updateMoney(startingMoney);
        healthSlider.maxValue = startingHealth;
        setHealth(startingHealth);
        gameOver = false;
        cameraController.startGame();
        cancelToolButton.gameObject.SetActive(false);
        numberOfMachineGuns = 0;
        numberOfMortars = 0;
        machineGunCost = machineGunBaseCost;
        mortarCost = mortarBaseCost;
        machineGunCostText.text = "$" + machineGunCost.ToString();
        mortarCostText.text = "$" + mortarCost.ToString();

        Generate();
    }

    public void Generate()
    {
        mMap.CleanUpWorld();
        mMap.GenerateWorld();
        spawners = mMap.placeSpawners(numberOfEnemySpawners);
    }

    public void Exit()
    {
#if !UNITY_EDITOR
        Application.Quit();
#endif
    }
}
