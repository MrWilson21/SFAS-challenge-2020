﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Environment : MonoBehaviour
{
    //Environment stores all information about tiles, turrets and spawners on the map
    //Generates each world using cellular automation and marching squares
    //Does pathfinding for enemy spawners to the player base

    [SerializeField] private List<EnvironmentTile> AccessibleTiles;
    [SerializeField] private List<EnvironmentTile> InaccessibleTiles;
    private Vector2Int Size;
    public float AccessiblePercentage;
    public int costToRemoveObstacles { get; set; } 

    [Range(0, 1)] public float LandFillPercent; //Amount of land to fill when creating initial map
    [SerializeField] private int numberOfSmoothingIterations; 
    [SerializeField] private int numberOfSmoothingIterationsAfterScaleUp;

    private EnvironmentTile[][] mMap;
    private List<EnvironmentTile> mAll;
    private List<EnvironmentTile> mToBeTested;
    private List<EnvironmentTile> mLastSolution;

    [SerializeField] private EnvironmentTile enemySpawner;

    private List<EnvironmentTile> PotentialSpawnPointsUp;
    private List<EnvironmentTile> PotentialSpawnPointsDown;
    private List<EnvironmentTile> PotentialSpawnPointsLeft;
    private List<EnvironmentTile> PotentialSpawnPointsRight;

    private List<EnvironmentTile> spawnPoints;
    public EnvironmentTile houseEntrance { get; set; }

    private readonly Vector3 NodeSize = Vector3.one * 9.0f; 
    private const float TileSize = 10.0f;
    private const float TileHeight = 2.5f;

    public EnvironmentTile Start { get; private set; }

    private int[,] floorMap;
    [SerializeField] private List<EnvironmentTile> marchingSquareTiles;
    public Vector2Int initialSize; //Initial size of map
    public int scaleUpFactor; //Amount to scale generated map up by
    [SerializeField] private int borderSize;
    private System.Random pseudoRandom;
    [SerializeField] public string seed;
    [SerializeField] private bool randomSeed;

    [SerializeField] private ParticleSystem buildParticles; //Particles to produce when building new turrets

    public bool isFinishedGenerating { get; set; }

    private void Awake()
    {
        initialSetup();
    }

    private void OnDrawGizmos()
    {
        // Draw the environment nodes and connections if we have them
        if (mMap != null)
        {
            for (int x = 0; x < Size.x; ++x)
            {
                for (int y = 0; y < Size.y; ++y)
                {
                    if (mMap[x][y].Connections != null)
                    {
                        for (int n = 0; n < mMap[x][y].Connections.Count; ++n)
                        {
                            Gizmos.color = Color.blue;
                            Gizmos.DrawLine(mMap[x][y].Position, mMap[x][y].Connections[n].Position);
                        }
                    }

                    // Use different colours to represent the state of the nodes
                    Color c = Color.white;
                    if ( !mMap[x][y].IsAccessible )
                    {
                        c = Color.red;
                    }
                    else
                    {
                        if(mLastSolution != null && mLastSolution.Contains( mMap[x][y] ))
                        {
                            c = Color.green;
                        }
                        else if (mMap[x][y].Visited)
                        {
                            c = Color.yellow;
                        }
                    }

                    Gizmos.color = c;
                    Gizmos.DrawWireCube(mMap[x][y].Position, NodeSize);
                }
            }
        }
    }

    void randomFillMap(ref int[,] map)
    {
        //First stage of terrain generation
        //Fills floor map randomly with either 1 or 0 using the landFillPercent as a fill rate
        for (int x = 0; x < initialSize.x; x++)
        {
            for (int y = 0; y < initialSize.y; y++)
            {
                if (x <= borderSize || x >= initialSize.x - (1 + borderSize) || y <= borderSize || y >= initialSize.y - (1 + borderSize))
                {
                    map[x, y] = 0;
                }
                else
                {
                    map[x, y] = (pseudoRandom.NextDouble() < LandFillPercent) ? 1 : 0;
                }
            }
        }
    }

    void smoothMap(ref int[,] map, ref Vector2Int size)
    {
        //Second stage of terrain generation
        //Uses cellular automation rules to determine next state of floor map
        //Turns randomised map into smooth islands
        //If a tile has less than 4 active neighbours then it becomes inactive
        //If a tile has more than 4 active neighbours then it becomes active
        int[,] newMap = new int[size.x, size.y];

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                int neighbourWallTiles = getSurroundingWallCount(ref map, ref size, x, y);

                if (neighbourWallTiles > 4)
                {
                    newMap[x, y] = 1;
                }

                else if (neighbourWallTiles < 4)
                {
                    newMap[x, y] = 0;
                }
                else
                {
                    newMap[x, y] = map[x, y];
                }
            }
        }

        map = newMap;
    }

    int getSurroundingWallCount(ref int[,] map, ref Vector2Int size, int mapX, int mapY)
    {
        //Counts number of surrounding tiles that have a value of 1
        int wallCount = 0;
        for (int neighbourX = mapX - 1; neighbourX <= mapX + 1; neighbourX++)
        {
            for (int neighbourY = mapY - 1; neighbourY <= mapY + 1; neighbourY++)
            {
                if (neighbourX >= 0 && neighbourX < size.x && neighbourY >= 0 && neighbourY < size.y)
                {
                    if (neighbourX != mapX || neighbourY != mapY)
                    {
                        wallCount += map[neighbourX, neighbourY];
                    }
                }
            }
        }

        return wallCount;
    }

    void scaleUpMap()
    {
        //Takes current floor map and scales it up
        int[,] newMap = new int[Size.x, Size.y];
        for (int x = 0; x < initialSize.x; x++)
        {
            for (int y = 0; y < initialSize.y; y++)
            {
                for (int i = 0; i < scaleUpFactor; i++)
                {
                    for (int j = 0; j < scaleUpFactor; j++)
                    {
                        newMap[x * scaleUpFactor + i, y * scaleUpFactor + j] = floorMap[x, y];
                    }
                }
            }
        }

        floorMap = newMap;
    }

    private bool checkIfSpawnPointsLink()
    {
        //Pathfinds each spawn point to each other to check if they can all access each other
        //This check is to ensure spawner locations are valid
        if(spawnPoints.Count > 1)
        {
            for(int i = 1; i < spawnPoints.Count; i++)
            {
                if(Solve(spawnPoints[0], spawnPoints[i]) == null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool checkIfHouseAccesible()
    {
        //Pathfinds each spawner to the house to ensure all enemies will have a path
        if (spawnPoints.Count > 0)
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if(Solve(spawnPoints[i], houseEntrance) == null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public List<Spawner> placeSpawners(int numberOfSpawners)
    {
        //Place a set amount of enemy spawn points around the edges of the islands
        //Check a random potential tile from each side in order until the correct number of spawners is found or there are no more valid points to check
        //place spawner tile down and set the entrance point before checking if valid and clear tile if its not valid
        int count = 0;
        int emptySideCount = 0;
        List<Spawner> spawners = new List<Spawner>();

        //While there are still spawners to place and there are still available spaces to check
        while(count < numberOfSpawners || emptySideCount == 4)
        {
            EnvironmentTile tile;
            Vector2Int coord;
            Spawner spawner;

            //4 directions
            switch (count % 4)
            {
                case 0:
                    if(PotentialSpawnPointsLeft.Count > 0)
                    {
                        tile = PotentialSpawnPointsLeft[pseudoRandom.Next(0, PotentialSpawnPointsLeft.Count)];
                        PotentialSpawnPointsLeft.Remove(tile);
                        //If no more potential spaces left then increment empty side count
                        if (PotentialSpawnPointsLeft.Count == 0)
                        {
                            emptySideCount++;
                        }
                        coord = tile.coordinates;

                        //Check if tile is accessible 
                        if (mMap[coord.x - 1][coord.y].IsAccessible && mMap[coord.x - 2][coord.y].IsAccessible)
                        {
                            //Add spawn point to spawn points
                            spawnPoints.Add(mMap[coord.x - 2][coord.y]);
                            spawner = swapTile(mMap[coord.x - 1][coord.y], enemySpawner, false, false).gameObject.GetComponentInChildren<Spawner>();
                            //Check if spawn point is valid
                            if (checkIfSpawnPointsLink())
                            {         
                                //If valid then set up spawner route
                                mMap[coord.x - 1][coord.y].gameObject.transform.GetChild(0).Rotate(new Vector3(0, 1, 0), -90);
                                spawner.spawnPoint = mMap[coord.x - 1][coord.y];
                                spawner.spawnExitPoint = mMap[coord.x - 2][coord.y];
                                spawner.spawnExitPoint.canBeDestroyed = false;
                                spawners.Add(spawner);
                                count++;
                            }
                            else
                            {
                                //If not valid then remove spawn point
                                spawnPoints.Remove(mMap[coord.x - 2][coord.y]);
                                clearTile(mMap[coord.x - 1][coord.y]);
                            }
                        }
                    }
                    break;

                case 1:
                    if (PotentialSpawnPointsRight.Count > 0)
                    {
                        tile = PotentialSpawnPointsRight[pseudoRandom.Next(0, PotentialSpawnPointsRight.Count)];
                        PotentialSpawnPointsRight.Remove(tile);
                        //If no more potential spaces left then increment empty side count
                        if (PotentialSpawnPointsRight.Count == 0)
                        {
                            emptySideCount++;
                        }
                        coord = tile.coordinates;

                        //Check if tile is accessible 
                        if (mMap[coord.x + 1][coord.y].IsAccessible && mMap[coord.x + 2][coord.y].IsAccessible)
                        {
                            //Add spawn point to spawn points
                            spawnPoints.Add(mMap[coord.x + 2][coord.y]);
                            spawner = swapTile(mMap[coord.x + 1][coord.y], enemySpawner, false, false).gameObject.GetComponentInChildren<Spawner>();
                            //Check if spawn point is valid
                            if (checkIfSpawnPointsLink())
                            {
                                //If valid then set up spawner route
                                mMap[coord.x + 1][coord.y].gameObject.transform.GetChild(0).Rotate(new Vector3(0, 1, 0), 90);
                                spawner.spawnPoint = mMap[coord.x + 1][coord.y];
                                spawner.spawnExitPoint = mMap[coord.x + 2][coord.y];
                                spawner.spawnExitPoint.canBeDestroyed = false;
                                spawners.Add(spawner);
                                count++;
                            }
                            else
                            {
                                //If not valid then remove spawn point
                                spawnPoints.Remove(mMap[coord.x + 2][coord.y]);
                                clearTile(mMap[coord.x + 1][coord.y]);
                            }
                        }
                    }
                    break;

                case 2:
                    if (PotentialSpawnPointsUp.Count > 0)
                    {
                        tile = PotentialSpawnPointsUp[pseudoRandom.Next(0, PotentialSpawnPointsUp.Count)];
                        PotentialSpawnPointsUp.Remove(tile);
                        //If no more potential spaces left then increment empty side count
                        if (PotentialSpawnPointsUp.Count == 0)
                        {
                            emptySideCount++;
                        }
                        coord = tile.coordinates;

                        //Check if tile is accessible 
                        if (mMap[coord.x][coord.y + 1].IsAccessible && mMap[coord.x][coord.y + 2].IsAccessible)
                        {
                            //Add spawn point to spawn points
                            spawnPoints.Add(mMap[coord.x][coord.y + 2]);
                            spawner = swapTile(mMap[coord.x][coord.y + 1], enemySpawner, false, false).gameObject.GetComponentInChildren<Spawner>();
                            //Check if spawn point is valid
                            if (checkIfSpawnPointsLink())
                            {
                                //If valid then set up spawner route
                                spawner.spawnPoint = mMap[coord.x][coord.y + 1];
                                spawner.spawnExitPoint = mMap[coord.x][coord.y + 2];
                                spawner.spawnExitPoint.canBeDestroyed = false;
                                spawners.Add(spawner);
                                count++;
                            }
                            else
                            {
                                //If not valid then remove spawn point
                                spawnPoints.Remove(mMap[coord.x][coord.y + 2]);
                                clearTile(mMap[coord.x][coord.y + 1]);
                            }
                        }
                    }
                    break;

                case 3:
                    if (PotentialSpawnPointsDown.Count > 0)
                    {
                        tile = PotentialSpawnPointsDown[pseudoRandom.Next(0, PotentialSpawnPointsDown.Count)];
                        PotentialSpawnPointsDown.Remove(tile);
                        //If no more potential spaces left then increment empty side count
                        if (PotentialSpawnPointsDown.Count == 0)
                        {
                            emptySideCount++;
                        }
                        coord = tile.coordinates;

                        //Check if tile is accessible 
                        if (mMap[coord.x][coord.y - 1].IsAccessible && mMap[coord.x][coord.y - 2].IsAccessible)
                        {
                            //Add spawn point to spawn points
                            spawnPoints.Add(mMap[coord.x][coord.y - 2]);
                            spawner = swapTile(mMap[coord.x][coord.y - 1], enemySpawner, false, false).gameObject.GetComponentInChildren<Spawner>();
                            //Check if spawn point is valid
                            if (checkIfSpawnPointsLink())
                            {
                                //If valid then set up spawner route
                                mMap[coord.x][coord.y - 1].gameObject.transform.GetChild(0).Rotate(new Vector3(0, 1, 0), 180);
                                spawner.spawnPoint = mMap[coord.x][coord.y - 1];
                                spawner.spawnExitPoint = mMap[coord.x][coord.y - 2];
                                spawner.spawnExitPoint.canBeDestroyed = false;
                                spawners.Add(spawner);
                                count++;
                            }
                            else
                            {
                                //If not valid then remove spawn point
                                spawnPoints.Remove(mMap[coord.x][coord.y - 2]);
                                clearTile(mMap[coord.x][coord.y - 1]);
                            }
                        }
                    }
                    break;
            }
        }
        return spawners;
    }

    private EnvironmentTile getFloorTile(int x, int y, Vector3 position)
    {
        //Gets the correct tile from the generated floor tiles map
        //If the tile is a flat ground tile then chose a random obstacle
        int squareIndex = floorMap[x, y] + floorMap[x + 1, y] * 2 + floorMap[x + 1, y + 1] * 4 + floorMap[x, y + 1] * 8;
        EnvironmentTile tile;

        //If tile is a ground tile 
        if (squareIndex == 15)
        {
            //Randomly choose accessible tile or inaccessible tile
            bool isAccessible = pseudoRandom.NextDouble() < AccessiblePercentage;
            List<EnvironmentTile> tiles = isAccessible ? AccessibleTiles : InaccessibleTiles;
            EnvironmentTile prefab = tiles[pseudoRandom.Next(0, tiles.Count)];
            tile = Instantiate(prefab, transform);
            tile.transform.Translate(position, Space.World);
            tile.IsAccessible = isAccessible;
            tile.canBeDestroyed = true;
            tile.costToRemove = costToRemoveObstacles;
            tile.coordinates = new Vector2Int(x, y);

            return tile;
        }

        //If tile is not a ground tile 
        tile = Instantiate(marchingSquareTiles[squareIndex], transform);
        tile.IsAccessible = false;
        tile.canBeDestroyed = false;
        tile.coordinates = new Vector2Int(x, y);
        tile.transform.Translate(position, Space.World);

        //Get all side tiles to place enemy spawners later
        if (squareIndex == 3)
        {
            PotentialSpawnPointsDown.Add(tile);
        }
        else if (squareIndex == 6)
        {
            PotentialSpawnPointsRight.Add(tile);
        }
        else if(squareIndex == 9)
        {
            PotentialSpawnPointsLeft.Add(tile);
        }
        else if(squareIndex == 12)
        {
            PotentialSpawnPointsUp.Add(tile);
        }

        return tile;
    }

    private void initialSetup()
    {
        //Set up map variables
        isFinishedGenerating = false;

        mAll = new List<EnvironmentTile>();
        mToBeTested = new List<EnvironmentTile>();

        PotentialSpawnPointsUp = new List<EnvironmentTile>();
        PotentialSpawnPointsDown = new List<EnvironmentTile>();
        PotentialSpawnPointsLeft = new List<EnvironmentTile>();
        PotentialSpawnPointsRight = new List<EnvironmentTile>();

        spawnPoints = new List<EnvironmentTile>();
    }

    private void Generate()
    {
    // Setup the map of the environment tiles according to the specified width and height
    // Generate tiles from the list of accessible and inaccessible prefabs using a random
    // and the specified accessible percentage
        initialSetup();

        //Get new seed
        if(randomSeed)
        {
            seed = Random.value.ToString();
        }
        pseudoRandom = new System.Random(seed.GetHashCode());

        //Set up size
        Size = initialSize * scaleUpFactor;

        //Do cellular automation part of map generation
        floorMap = new int[Size.x + 1, Size.y + 1];
        randomFillMap(ref floorMap);
        for(int i = 0; i < numberOfSmoothingIterations; i++)
        {
            smoothMap(ref floorMap, ref initialSize);
        }

        //Scale map up
        scaleUpMap();
        for (int i = 0; i < numberOfSmoothingIterationsAfterScaleUp; i++)
        {
            smoothMap(ref floorMap, ref Size);
        }

        Size.y = Size.y - 1;
        Size.x = Size.x - 1;

        mMap = new EnvironmentTile[Size.x][];

        int halfWidth = Size.x / 2;
        int halfHeight = Size.y / 2;
        Vector3 position = new Vector3( -(halfWidth * TileSize), 0.0f, -(halfHeight * TileSize));
        bool start = true;

        //Do marching squares part of map generation
        //Iterate over each "square" of the map to find its tile
        //Each square is defined as the midpoint between 4 points on the floor map
        //Use each of the 4 points to determine the shape of the tile
        for ( int x = 0; x < Size.x; ++x)
        {
            mMap[x] = new EnvironmentTile[Size.y];
            for ( int y = 0; y < Size.y; ++y)
            {
                EnvironmentTile tile = getFloorTile(x, y, position);
                tile.Position = new Vector3( position.x + (TileSize / 2), TileHeight, position.z + (TileSize / 2)) + transform.position;               
                tile.gameObject.name = string.Format("Tile({0},{1})", x, y);
                mMap[x][y] = tile;
                mAll.Add(tile);

                if(start && tile.IsAccessible)
                {
                    start = false;
                    Start = tile;
                }

                position.z += TileSize;
            }

            position.x += TileSize;
            position.z = -(halfHeight * TileSize);
        }
    }

    private void SetupConnections()
    {
        // Currently we are only setting up connections between adjacnt nodes
        for (int x = 0; x < Size.x; ++x)
        {
            for (int y = 0; y < Size.y; ++y)
            {
                EnvironmentTile tile = mMap[x][y];
                tile.Connections = new List<EnvironmentTile>();

                for(int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        if (x + i > 0 && x + i < Size.x - 1 && y + j > 0 && y + j < Size.y - 1 && (y != 0  && x != 0))
                        {

                        }
                    }
                }

                if (x > 0)
                {
                    tile.Connections.Add(mMap[x - 1][y]);
                }

                if (x < Size.x - 1)
                {
                    tile.Connections.Add(mMap[x + 1][y]);
                }

                if (y > 0)
                {
                    tile.Connections.Add(mMap[x][y - 1]);
                }

                if (y < Size.y - 1)
                {
                    tile.Connections.Add(mMap[x][y + 1]);
                }
            }
        }
    }

    private float Distance(EnvironmentTile a, EnvironmentTile b)
    {
        // Use the length of the connection between these two nodes to find the distance, this 
        // is used to calculate the local goal during the search for a path to a location
        float result = float.MaxValue;
        EnvironmentTile directConnection = a.Connections.Find(c => c == b);
        if (directConnection != null)
        {
            result = TileSize;
        }
        return result;
    }

    private float Heuristic(EnvironmentTile a, EnvironmentTile b)
    {
        // Use the locations of the node to estimate how close they are by line of sight
        // experiment here with better ways of estimating the distance. This is used  to
        // calculate the global goal and work out the best order to prossess nodes in
        return Vector3.Distance(a.Position, b.Position);
    }

    public void GenerateWorld()
    {
        Generate();
        SetupConnections();
    }

    public void CleanUpWorld()
    {
        if (mMap != null)
        {
            for (int x = 0; x < Size.x; ++x)
            {
                for (int y = 0; y < Size.y; ++y)
                {
                    Destroy(mMap[x][y].gameObject);
                }
            }
        }
    }

    public List<EnvironmentTile> Solve(EnvironmentTile begin, EnvironmentTile destination)
    {
        List<EnvironmentTile> result = null;
        if (begin != null && destination != null)
        {
            // Nothing to solve if there is a direct connection between these two locations
            EnvironmentTile directConnection = begin.Connections.Find(c => c == destination);
            if (directConnection == null)
            {
                // Set all the state to its starting values
                mToBeTested.Clear();

                for( int count = 0; count < mAll.Count; ++count )
                {
                    mAll[count].Parent = null;
                    mAll[count].Global = float.MaxValue;
                    mAll[count].Local = float.MaxValue;
                    mAll[count].Visited = false;
                }

                // Setup the start node to be zero away from start and estimate distance to target
                EnvironmentTile currentNode = begin;
                currentNode.Local = 0.0f;
                currentNode.Global = Heuristic(begin, destination);

                // Maintain a list of nodes to be tested and begin with the start node, keep going
                // as long as we still have nodes to test and we haven't reached the destination
                mToBeTested.Add(currentNode);

                while (mToBeTested.Count > 0 && currentNode != destination)
                {
                    // Begin by sorting the list each time by the heuristic
                    mToBeTested.Sort((a, b) => (int)(a.Global - b.Global));

                    // Remove any tiles that have already been visited
                    mToBeTested.RemoveAll(n => n.Visited);

                    // Check that we still have locations to visit
                    if (mToBeTested.Count > 0)
                    {
                        // Mark this note visited and then process it
                        currentNode = mToBeTested[0];
                        currentNode.Visited = true;

                        // Check each neighbour, if it is accessible and hasn't already been 
                        // processed then add it to the list to be tested 
                        for (int count = 0; count < currentNode.Connections.Count; ++count)
                        {
                            EnvironmentTile neighbour = currentNode.Connections[count];

                            if (!neighbour.Visited && neighbour.IsAccessible)
                            {
                                mToBeTested.Add(neighbour);
                            }

                            // Calculate the local goal of this location from our current location and 
                            // test if it is lower than the local goal it currently holds, if so then
                            // we can update it to be owned by the current node instead 
                            float possibleLocalGoal = currentNode.Local + Distance(currentNode, neighbour);
                            if (possibleLocalGoal < neighbour.Local)
                            {
                                neighbour.Parent = currentNode;
                                neighbour.Local = possibleLocalGoal;
                                neighbour.Global = neighbour.Local + Heuristic(neighbour, destination);
                            }
                        }
                    }
                }

                // Build path if we found one, by checking if the destination was visited, if so then 
                // we have a solution, trace it back through the parents and return the reverse route
                if (destination.Visited)
                {
                    result = new List<EnvironmentTile>();
                    EnvironmentTile routeNode = destination;

                    while (routeNode.Parent != null)
                    {
                        result.Add(routeNode);
                        routeNode = routeNode.Parent;
                    }
                    result.Add(routeNode);
                    result.Reverse();

                    //Debug.LogFormat("Path Found: {0} steps {1} long", result.Count, destination.Local);
                }
                else
                {
                    Debug.LogWarning("Path Not Found");
                }
            }
            else
            {
                result = new List<EnvironmentTile>();
                result.Add(begin);
                result.Add(destination);
                Debug.LogFormat("Direct Connection: {0} <-> {1} {2} long", begin, destination, TileSize);
            }
        }
        else
        {
            Debug.LogWarning("Cannot find path for invalid nodes");
        }

        mLastSolution = result;

        return result;
    }

    public EnvironmentTile clearTile(EnvironmentTile tileToClear)
    {
        return swapTile(tileToClear, marchingSquareTiles[15], true, true);
    }

    public EnvironmentTile swapTile(EnvironmentTile tileToSwap, EnvironmentTile newTile, bool canBeDestoyed, bool isAccessible)
    {
        //Swap an environment tile with a new one
        //New tile is instantiated and its connections are calculated
        Vector2Int tileCoord = tileToSwap.coordinates;
        Vector3 position = tileToSwap.Position;

        //Create new tile
        EnvironmentTile newInstance = Instantiate(newTile, tileToSwap.gameObject.transform.position, newTile.gameObject.transform.rotation, transform);
        newInstance.gameObject.name = tileToSwap.gameObject.name;
        newInstance.IsAccessible = isAccessible;
        newInstance.canBeDestroyed = canBeDestoyed;
        newInstance.Position = position;
        newInstance.Connections = tileToSwap.Connections;
        newInstance.coordinates = tileToSwap.coordinates;

        //Remove old tile and replace
        mAll.Remove(tileToSwap);
        mAll.Add(newInstance);

        //Get new connections
        Destroy(tileToSwap.gameObject);
        for(int i = 0; i < tileToSwap.Connections.Count; i++)
        {
            for(int j = 0; j < tileToSwap.Connections[i].Connections.Count; j++)
            {
                if(tileToSwap.Connections[i].Connections[j] == tileToSwap)
                {
                    tileToSwap.Connections[i].Connections[j] = newInstance;
                }
            }
        }
        mMap[tileCoord.x][tileCoord.y] = newInstance;

        //Create build particles that are destroyed after 3 seconds
        if (isFinishedGenerating)
        {
            Destroy(Instantiate(buildParticles, newInstance.Position, buildParticles.gameObject.transform.rotation, newInstance.transform), 3.0f);
        }    

        return mMap[tileCoord.x][tileCoord.y];
    }

    public EnvironmentTile[][] getTileMap()
    {
        return mMap;
    }
}
