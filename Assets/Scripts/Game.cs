using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Game : MonoBehaviour
{
    [SerializeField] private Camera MainCamera;
    [SerializeField] private Character Character;
    [SerializeField] private Canvas Menu;
    [SerializeField] private Canvas Hud;
    [SerializeField] private Transform CharacterStart;
    [SerializeField] private int numberOfEnemySpawners;

    private RaycastHit[] mRaycastHits;
    private Character mCharacter;
    private Environment mMap;

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

    float a = 0;

    void Start()
    {
        mRaycastHits = new RaycastHit[NumberOfRaycastHits];
        mMap = GetComponentInChildren<Environment>();
        mCharacter = Instantiate(Character, transform); 
        ShowMenu(true);
    }

    private void Update()
    {
        if (playingGame)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                tileRotation = (tileRotation + 1) % 4;
            }

            if (!startPlaced)
            {
                placeStartPoint();
                return;
            }
            else
            {
                if(a > 0.5)
                {
                    foreach (Spawner spawner in spawners)
                    {
                        spawner.spawnEnemy();
                        a = 0;
                    }
                }
                a += Time.deltaTime;
            }

            if (currentTile != null && currentTile.canBeDestroyed)
            {
                currentTile.GetComponent<ColorSwapper>().restoreColour();
            }

            tileIsHighlighted = getMouseTile();
            objectToPlace.SetActive(false);

            if (Input.GetKeyDown(KeyCode.C))
            {
                cancelTool();
            }

            if (isUsingPlaceTool && tileIsHighlighted)
            {
                usePlaceTool();
            }
            else if (isUsingDestroyTool)
            {
                useDestroyTool();
            }

            // Check to see if the player has clicked a tile and if they have, try to find a path to that 
            // tile. If we find a path then the character will move along it to the clicked tile. 
            else if (Input.GetMouseButtonDown(0) && tileIsHighlighted)
            {
                List<EnvironmentTile> route = mMap.Solve(mCharacter.CurrentPosition, currentTile);
                mCharacter.GoTo(route);
            }
        }
    }

    private void usePlaceTool()
    {
        if (tileIsHighlighted)
        {
            objectToPlace.SetActive(true);
            objectToPlace.transform.position = currentTile.Position;
            if (currentTile.IsAccessible)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    mMap.swapTile(currentTile, tilePrefab, true, false);
                }
                objectToPlace.GetComponent<ColorSwapper>().swapColour(Color.green);
            }
            else
            {
                objectToPlace.GetComponent<ColorSwapper>().swapColour(Color.red);
            }
        }
    }

    private void useDestroyTool()
    {
        if (currentTile.canBeDestroyed)
        {
            currentTile.GetComponent<ColorSwapper>().swapColour(Color.red);
            if (Input.GetMouseButtonDown(0))
            {
                mMap.clearTile(currentTile);
            }
        }
    }

    private void placeStartPoint()
    {
        tileIsHighlighted = getMouseTile();
        if(tileIsHighlighted)
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
                    currentTile = mMap.swapTile(currentTile, tilePrefab, false, false);
                    if (mMap.checkIfHouseAccesible())
                    {
                        cancelTool();
                        startPlaced = true;

                        foreach (Spawner spawner in spawners)
                        {
                            spawner.housePoint = currentTile;
                            spawner.route = mMap.Solve(spawner.spawnExitPoint, mMap.houseEntrance);
                        }
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
    }

    private bool getMouseTile()
    {
        //Find which tile the mouse is hovering over
        //If there is one return true and set currentTile
        Ray screenClick = MainCamera.ScreenPointToRay(Input.mousePosition);
        int hits = Physics.RaycastNonAlloc(screenClick, mRaycastHits);
        if (hits > 0)
        {
            currentTile = mRaycastHits[0].transform.GetComponent<EnvironmentTile>();
            return true;
        }

        return false;
    }

    public void cancelTool()
    {
        isUsingPlaceTool = false;
        isUsingDestroyTool = false;
    }

    public void destroyTool()
    {
        isUsingPlaceTool = false;
        isUsingDestroyTool = true;
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
    }

    public void ShowMenu(bool show)
    {
        if (Menu != null && Hud != null)
        {
            Menu.enabled = show;
            Hud.enabled = !show;

            if( show )
            {
                mCharacter.transform.position = CharacterStart.position;
                mCharacter.transform.rotation = CharacterStart.rotation;
                playingGame = false;
                mMap.CleanUpWorld();
            }
            else
            {
                mCharacter.transform.position = mMap.Start.Position;
                mCharacter.transform.rotation = Quaternion.identity;
                mCharacter.CurrentPosition = mMap.Start;
            }
        }
    }

    public void startGame()
    {
        isUsingPlaceTool = false;
        isUsingDestroyTool = false;
        tileIsHighlighted = false;
        startPlaced = false;
        playingGame = true;

        spawners = new List<Spawner>();
        Generate();
        selectNewTile(0);
    }

    public void addSpawner(Spawner spawner)
    {
        spawners.Add(spawner);
    }

    private void Generate()
    {
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
