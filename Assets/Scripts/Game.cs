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

    void Start()
    {
        mRaycastHits = new RaycastHit[NumberOfRaycastHits];
        mMap = GetComponentInChildren<Environment>();
        mCharacter = Instantiate(Character, transform); 
        ShowMenu(true);

        selectNewTile(0);
        //cancelTool();
    }

    private void Update()
    {
        if(!startPlaced)
        {
            placeStartPoint();
            return;
        }

        if (currentTile != null && currentTile.canBeDestroyed)
        {
            print(currentTile);
            currentTile.GetComponent<ColorSwapper>().restoreColour();
        }

        tileIsHighlighted = getMouseTile();
        objectToPlace.SetActive(false);

        if (Input.GetKeyDown(KeyCode.C))
        {
            cancelTool();
        }

        if(isUsingPlaceTool && tileIsHighlighted)
        {
            if(tileIsHighlighted)
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
        else if(isUsingDestroyTool)
        {
            if(currentTile.canBeDestroyed)
            {
                currentTile.GetComponent<ColorSwapper>().swapColour(Color.red);
                if (Input.GetMouseButtonDown(0))
                {
                    mMap.clearTile(currentTile);
                }            
            }
        }
        // Check to see if the player has clicked a tile and if they have, try to find a path to that 
        // tile. If we find a path then the character will move along it to the clicked tile. 
        else if (Input.GetMouseButtonDown(0) && tileIsHighlighted)
        {
            List<EnvironmentTile> route = mMap.Solve(mCharacter.CurrentPosition, currentTile);
            mCharacter.GoTo(route);
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
                    mMap.swapTile(currentTile, tilePrefab, false, false);
                    cancelTool();
                    startPlaced = true;
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

    public void Generate()
    {
        mMap.GenerateWorld();
    }

    public void Exit()
    {
#if !UNITY_EDITOR
        Application.Quit();
#endif
    }
}
