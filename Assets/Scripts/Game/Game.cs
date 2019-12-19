using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class Game : MonoBehaviour
{
    //Controls the flow of the game and takes player inputs

    [SerializeField] private Camera MainCamera;
    private CameraController cameraController;
    [SerializeField] private int numberOfEnemySpawners;
    [SerializeField] private AnimationCurve moneyFromEnemiesCurve;
    [SerializeField] private float nextWaveBonusMultiplier;
    [Range(0,1)][SerializeField] private float sellMoneyBackPercent;

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
    private bool isUsingUpgradeTool = false;
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
    [SerializeField] private RectTransform rotateIcon;
    [SerializeField] private RectTransform upgradeIcon;

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

    [SerializeField] private TMP_Text gameOverText;

    private List<Turret> turrets;
    private Turret currentTurret;

    private int health;
    private int money;

    public bool gameOver { get; set; }

    void Start()
    {
        //Set inital variables up
        mRaycastHits = new RaycastHit[NumberOfRaycastHits];
        mMap = GetComponentInChildren<Environment>();
        menu = GetComponent<MenuController>();
        waveSpawner = GetComponent<WaveSpawner>();
        cameraController = MainCamera.GetComponent<CameraController>();
        mMap.costToRemoveObstacles = costToRemoveObstacles;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
    }

    private void Update()
    {
        //If game is being played
        if (playingGame)
        {
            doGameUpdate();
        }
    }

    private void doGameUpdate()
    {
        //If a tile was selected in the last frame then restore its colour
        if (currentTile != null && currentTile.canBeDestroyed && !currentTile.IsAccessible)
        {
            currentTile.GetComponent<ColorSwapper>().restoreColour();
        }
        //If a turret was selected in the last frame then hide its range
        if(currentTurret != null)
        {
            showRange(currentTurret, false);
        }
        //Get the current tile that the mouse is hovering over
        tileIsHighlighted = getMouseTile();

        //If the place tool is being used
        if(objectToPlace != null)
        {
            //disable the object
            objectToPlace.SetActive(false);
            //Rotate the object if rotate key pressed
            if (Input.GetKeyDown(KeyCode.R))
            {
                tileRotation = (tileRotation + 1) % 4;
                objectToPlace.transform.transform.Rotate(new Vector3(0, 1, 0), 90);
            }
        }

        //Disable mouse icons
        buyPrice.gameObject.SetActive(false);
        destroyIcon.gameObject.SetActive(false);
        rotateIcon.gameObject.SetActive(false);
        upgradeIcon.gameObject.SetActive(false);

        //If mouse is hovering over a tile
        if (tileIsHighlighted)
        {
            currentTurret = currentTile.GetComponent<Turret>();

            //If place tool is being used
            if (isUsingPlaceTool)
            {
                if (!startPlaced)
                {
                    placeStartPoint();
                }
                else
                {
                    usePlaceTool();
                }
            }
            //If destroy tool is being used
            else if (isUsingDestroyTool)
            {
                useDestroyTool();
            }
            //If a turret is highlighted show its range
            else if (currentTurret != null)
            {
                showRange(currentTurret, true);
            }

            //If upgrade tool is being used
            if (isUsingUpgradeTool)
            {
                useUpgradeTool();
            }
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            cancelTool();
        }

        if(!isDoingWave && startPlaced)
        {
            //Start wave when timer reaches 0
            timeUntilWave -= Time.deltaTime;
            if (timeUntilWave <= 0)
            {
                menu.startWave(false);
            }
            else
            {
                //Update timer text
                nextWaveCountdown.text = "Next wave in " + (int)timeUntilWave;
                nextWaveBonus.text = "<< Start now for bonus $" + calculateNextWaveBonus();
            }
        }
    }

    private int calculateNextWaveBonus()
    {
        //Get bonus for starting a round early
        float money = Mathf.Log(Mathf.Clamp(timeUntilWave, 1, float.MaxValue));
        money *= moneyFromEnemiesCurve.Evaluate(waveCount) * nextWaveBonusMultiplier;
        return (int)money;
    }

    private void updateMoney(int moneyToAdd)
    {
        //Increment money and update money text
        money += moneyToAdd;
        moneyText.text = "$" + money;
        Message message = Instantiate(messagePrefab, hudCanvas);
        message.GetComponent<RectTransform>().localPosition = moneyText.GetComponent<RectTransform>().localPosition + new Vector3(120, 0, 0);
        message.setText((moneyToAdd >= 0 ? "+" : "-") + "$" + moneyToAdd.ToString(), moneyToAdd >= 0 ? sellColour : buyColour);
    }

    private void setHealth(int health)
    {
        //Decrement health and update health slider 
        this.health = health;
        healthSlider.value = health;
        healthText.text = health.ToString() + " / " + startingHealth.ToString();

        //Change colour based on health
        healthFill.color = healthGradient.Evaluate((float)health / (float)startingHealth);
        healthIcon.color = healthGradient.Evaluate((float)health / (float)startingHealth);
        healthText.color = healthGradient.Evaluate((float)health / (float)startingHealth);
    }

    private void setBuyText(int cost)
    {
        //Set the text over the mouse position with the amount of money to buy/sell an object
        Vector3 pos = MainCamera.ScreenToViewportPoint(Input.mousePosition);
        pos.x = (pos.x - 0.5f) * hudCanvas.sizeDelta.x;
        pos.y = (pos.y - 0.5f) * hudCanvas.sizeDelta.y;
        buyPrice.localPosition = pos;

        buyPriceText.text = (Mathf.Sign(cost) == 0 ? "" : "-") +  "$" + Mathf.Abs(cost).ToString();

        //set the colour depending on buying or selling
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
        //Creating a piecing of text at the mouse position that fades out gradually
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
        //Place a turret down on the selected tile if possible

        //Show rotate icon
        Vector3 pos = MainCamera.ScreenToViewportPoint(Input.mousePosition);
        pos.x = (pos.x - 0.5f) * hudCanvas.sizeDelta.x;
        pos.y = (pos.y - 0.5f) * hudCanvas.sizeDelta.y;
        rotateIcon.localPosition = pos;
        rotateIcon.gameObject.SetActive(true);

        //Enable the turret preview object
        objectToPlace.SetActive(true);
        objectToPlace.transform.position = currentTile.Position;
        //Check if the tile is accessible
        if (currentTile.IsAccessible && currentTile.canBeDestroyed)
        {
            //set preview object to green if it is valid
            objectToPlace.GetComponent<ColorSwapper>().swapColour(Color.green);
            Turret turret = tilePrefab.GetComponentInChildren<Turret>();
            int turretCost = 0;
            //Set the cost of the turret
            if (turret != null)
            {
                turretCost = getTurretCost(tilePrefab);
                setBuyText(turretCost);
            }

            //If left click pressed
            if (Input.GetMouseButtonDown(0))
            {
                //If player can afford it
                if(turretCost <= money)
                {
                    //Swap tile for turret
                    currentTile = mMap.swapTile(currentTile, tilePrefab, true, false);
                    currentTile.transform.GetChild(0).transform.Rotate(new Vector3(0, 1, 0), 90 * tileRotation);

                    //Check if tile position is valid
                    if (mMap.checkIfHouseAccesible())
                    {
                        //If position is valid then set the turret up and recalculate enemy routes
                        turret = currentTile.GetComponentInChildren<Turret>();

                        if (turret != null)
                        {
                            turret.setSpawners(spawners);
                            turret.setGame(this);

                            int cost = getTurretCost(currentTile);
                            buyItem(cost);
                            incrementTurretCost(currentTile, 1);

                            turrets.Add(turret);
                            showRange(turret, false);
                        }
                        foreach (Spawner spawner in spawners)
                        {
                            spawner.route = mMap.Solve(spawner.spawnExitPoint, mMap.houseEntrance);
                        }
                    }
                    else
                    {
                        //If position isn't valid then reset the tile
                        sendMessage("Can't block path");
                        mMap.clearTile(currentTile);
                    }
                }
                else
                {
                    //If player can't afford then send error message
                    sendMessage("Can't afford");
                }                  
            }
        }
        else
        {
            //Set preview object to red if not a valid position
            objectToPlace.GetComponent<ColorSwapper>().swapColour(Color.red);
        }
    }

    private void useDestroyTool()
    {
        //Remove a turret or obstacle when player clicks on it

        //Enable destroy icon
        Vector3 pos = MainCamera.ScreenToViewportPoint(Input.mousePosition);
        pos.x = (pos.x - 0.5f) * hudCanvas.sizeDelta.x;
        pos.y = (pos.y - 0.5f) * hudCanvas.sizeDelta.y;
        destroyIcon.localPosition = pos;
        destroyIcon.gameObject.SetActive(true);

        //Check if highlighted tile can be cleared
        if (currentTile.canBeDestroyed && !currentTile.IsAccessible)
        {
            //Get cost to remove tile and colour it red
            int cost = getTileClearCost(currentTile);
            setBuyText(cost);
            currentTile.GetComponent<ColorSwapper>().swapColour(Color.red);
            //If left click pressed
            if (Input.GetMouseButtonDown(0))
            {
                //Check if player can afford it
                if(cost <= money)
                {              
                    //If player can afford it then update player money and clear the tile
                    buyItem(cost);
                    mMap.clearTile(currentTile);

                    incrementTurretCost(currentTile, -1);

                    Turret turret = currentTile.GetComponentInChildren<Turret>();
                    if(turret != null)
                    {
                        turrets.Remove(turret);
                    }

                    //Re calculate enemy route
                    foreach (Spawner spawner in spawners)
                    {
                        spawner.route = mMap.Solve(spawner.spawnExitPoint, mMap.houseEntrance);
                    }
                }
                else
                {
                    sendMessage("Can't afford");
                }
            }
        }
    }

    private void useUpgradeTool()
    {
        //Upgrade a turret when the player clicks on it

        //Enable upgrade icon
        Vector3 pos = MainCamera.ScreenToViewportPoint(Input.mousePosition);
        pos.x = (pos.x - 0.5f) * hudCanvas.sizeDelta.x;
        pos.y = (pos.y - 0.5f) * hudCanvas.sizeDelta.y;
        upgradeIcon.localPosition = pos;
        upgradeIcon.gameObject.SetActive(true);

        //Get the turret on the highlighted tile
        Turret turret = currentTile.GetComponent<Turret>();

        if(turret != null)
        {
            //Check if turret can be upgraded
            if(turret.turretUpgrade != null)
            {
                //Highlight green and get cost
                currentTile.GetComponent<ColorSwapper>().swapColour(Color.green);
                int cost = turret.upgradeCost;
                setBuyText(cost);
                //If left mouse button clicked
                if (Input.GetMouseButtonDown(0))
                {
                    //If player can afford it
                    if (cost <= money)
                    {
                        //Update money and replace turret with new one
                        buyItem(cost);
                        turrets.Remove(turret);
                        currentTile = mMap.swapTile(currentTile, turret.turretUpgrade, true, false);

                        turret = currentTile.GetComponent<Turret>();
                        turret.setSpawners(spawners);
                        turret.setGame(this);
                        turrets.Add(turret);
                        showRange(turret, false);
                    }
                    else
                    {
                        sendMessage("Can't afford");
                    }
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                //Send message if turret already max level
                sendMessage("Max level");
            }
        }
    }

    private void placeStartPoint()
    {
        //Place the starting base

        //Enable rotate icon
        Vector3 pos = MainCamera.ScreenToViewportPoint(Input.mousePosition);
        pos.x = (pos.x - 0.5f) * hudCanvas.sizeDelta.x;
        pos.y = (pos.y - 0.5f) * hudCanvas.sizeDelta.y;
        rotateIcon.localPosition = pos;
        rotateIcon.gameObject.SetActive(true);


        objectToPlace.SetActive(true);
        objectToPlace.transform.position = currentTile.Position;
        //Check if the tile is accessible
        if (currentTile.IsAccessible && currentTile.canBeDestroyed)
        {
            if (Input.GetMouseButtonDown(0))
            {
                //Get the coordinate of the tile outside of the front door of the base
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
                //Check if base entrance is accessible
                if (mMap.checkIfHouseAccesible())
                {
                    //If accessible then set up spawner routes and change ui layout
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
                    //If not then clear tile
                    sendMessage("Can't place here");
                    mMap.houseEntrance.canBeDestroyed = true;
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
        //Get the cost to remove the tile or the sell price of the turret if tile is a turret

        //Decrement turret cost of the tile to get the price that was paid for it
        incrementTurretCost(tile, -1);
        Turret turret = tile.GetComponentInChildren<Turret>();
        int cost;
        if (turret is MachineGun)
        {
            cost = (int)(-machineGunCost * sellMoneyBackPercent);
        }
        else if (turret is Mortar)
        {
            cost = (int)(-mortarCost * sellMoneyBackPercent);
        }
        else
        {
            cost = tile.costToRemove;
        }
        //Increment cost again to ensure turret cost stays the same
        incrementTurretCost(tile, 1);
        return cost;
    }

    private int getTurretCost(EnvironmentTile tile)
    {
        //get the cost to build a turret

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
        //Increase or decrease cost of turrets by increment

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
        //Deselect all tools
        isUsingPlaceTool = false;
        isUsingDestroyTool = false;
        isUsingUpgradeTool = false;
        cancelToolButton.gameObject.SetActive(false);
    }

    public void destroyTool()
    {
        //Select destroy tool
        isUsingPlaceTool = false;
        isUsingDestroyTool = true;
        isUsingUpgradeTool = false;
        cancelToolButton.gameObject.SetActive(true);
    }

    public void upgradeTool()
    {
        //Select upgrade tool
        isUsingPlaceTool = false;
        isUsingDestroyTool = false;
        isUsingUpgradeTool = true;
        cancelToolButton.gameObject.SetActive(true);
    }

    private void showRange(Turret turret, bool show)
    {
        //Show the range circle of a turret
        if(show)
        {
            foreach (RangeCircle range in turret.GetComponentsInChildren<RangeCircle>())
            {
                range.showRadius();
            }
        }
        else
        {
            foreach (RangeCircle range in turret.GetComponentsInChildren<RangeCircle>())
            {
                range.hideRadius();
            }
        }
    }

    public void selectNewTile(int tileIndex)
    {
        //Select the place tool and an object to place
        isUsingDestroyTool = false;
        isUsingUpgradeTool = false;
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
        //Start a new wave
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
        //End the current wave
        waveCount++;
        isDoingWave = false;
    }

    private void loseGame()
    {
        //Bring up the game over screen and update enemy animations
        cancelTool();
        gameOver = true;
        menu.gameOver();
        cameraController.loseGame();
        gameOverText.text = "Waves survived: " + (waveCount-1).ToString();

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
        //Decrement health and check if game is lost or wave is over
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
        //Reward player with money and check if wave is finished
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
        isUsingUpgradeTool = false;
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
        turrets = new List<Turret>();

        //Create new world
        Generate();
    }

    public void Generate()
    {
        //Generate new world and place spawners
        mMap.CleanUpWorld();
        mMap.GenerateWorld();
        spawners = mMap.placeSpawners(numberOfEnemySpawners);
        mMap.isFinishedGenerating = true;
    }

    public void Exit()
    {
#if !UNITY_EDITOR
        Application.Quit();
#endif
    }
}
