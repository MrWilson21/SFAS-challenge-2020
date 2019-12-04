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

    //Current tile mouse is hovering over
    private EnvironmentTile currentTile;
    [SerializeField] private GameObject turretPrefab;
    private GameObject objectToPlace;
    [SerializeField] private EnvironmentTile tilePrefab;

    void Start()
    {
        mRaycastHits = new RaycastHit[NumberOfRaycastHits];
        mMap = GetComponentInChildren<Environment>();
        mCharacter = Instantiate(Character, transform); 
        ShowMenu(true);

        objectToPlace = Instantiate(turretPrefab);
        objectToPlace.AddComponent<ColorSwapper>();
        objectToPlace.SetActive(false);
    }

    private void Update()
    {       
        // Check to see if the player has clicked a tile and if they have, try to find a path to that 
        // tile. If we find a path then the character will move along it to the clicked tile. 
        if (Input.GetMouseButtonDown(0))
        {
            if (getMouseTile())
            {
                List<EnvironmentTile> route = mMap.Solve(mCharacter.CurrentPosition, currentTile);
                mCharacter.GoTo(route);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (getMouseTile())
            {
                if (currentTile.IsAccessible)
                {
                    mMap.swapTile(currentTile, tilePrefab);
                }
            }
        }

        if (getMouseTile())
        {
            objectToPlace.SetActive(true);
            objectToPlace.transform.position = currentTile.Position;
            if(currentTile.IsAccessible)
            {
                objectToPlace.GetComponent<ColorSwapper>().swapColour(Color.green);
            }
            else
            {
                objectToPlace.GetComponent<ColorSwapper>().swapColour(Color.red);
            }
        }
        else
        {
            objectToPlace.SetActive(false);
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
