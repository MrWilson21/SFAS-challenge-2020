using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Environment : MonoBehaviour
{
    [SerializeField] private List<EnvironmentTile> AccessibleTiles;
    [SerializeField] private List<EnvironmentTile> InaccessibleTiles;
    private Vector2Int Size;
    [SerializeField] private float AccessiblePercentage;

    [Range(0, 1)] [SerializeField] private float LandFillPercent;
    [SerializeField] private int numberOfSmoothingIterations;
    [SerializeField] private int numberOfSmoothingIterationsAfterScaleUp;

    private EnvironmentTile[][] mMap;
    private List<EnvironmentTile> mAll;
    private List<EnvironmentTile> mToBeTested;
    private List<EnvironmentTile> mLastSolution;

    private readonly Vector3 NodeSize = Vector3.one * 9.0f; 
    private const float TileSize = 10.0f;
    private const float TileHeight = 2.5f;

    public EnvironmentTile Start { get; private set; }

    private int[,] floorMap;
    [SerializeField] private List<EnvironmentTile> marchingSquareTiles;
    [SerializeField] private Vector2Int initialSize;
    [SerializeField] private int scaleUpFactor;
    [SerializeField] private int borderSize;
    private System.Random pseudoRandom;
    [SerializeField] private string seed;
    [SerializeField] private bool randomSeed;


    private void Awake()
    {
        mAll = new List<EnvironmentTile>();
        mToBeTested = new List<EnvironmentTile>();
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
                else
                {
                    //wallCount++;
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

    private EnvironmentTile getFloorTile(int x, int y, Vector3 position)
    {
        //Gets the correct tile from the generated floor tiles map
        //If the tile is a flat ground tile then chose a random obstacle
        int squareIndex = floorMap[x, y] + floorMap[x + 1, y] * 2 + floorMap[x + 1, y + 1] * 4 + floorMap[x, y + 1] * 8;
        EnvironmentTile tile;

        if (squareIndex == 15)
        {
            bool isAccessible = Random.value < AccessiblePercentage;
            List<EnvironmentTile> tiles = isAccessible ? AccessibleTiles : InaccessibleTiles;
            EnvironmentTile prefab = tiles[Random.Range(0, tiles.Count)];
            tile = Instantiate(prefab, position, Quaternion.identity, transform);
            tile.IsAccessible = isAccessible;
            tile.canBeDestroyed = !isAccessible;
            tile.coordinates = new Vector2Int(x, y);

            return tile;
        }

        tile = Instantiate(marchingSquareTiles[squareIndex], transform);
        tile.IsAccessible = false;
        tile.canBeDestroyed = false;
        tile.coordinates = new Vector2Int(x, y);
        tile.transform.Translate(position, Space.World);
        return tile;
    }

    private void Generate()
    {
        // Setup the map of the environment tiles according to the specified width and height
        // Generate tiles from the list of accessible and inaccessible prefabs using a random
        // and the specified accessible percentage
        if(randomSeed)
        {
            seed = Random.value.ToString();
        }
        pseudoRandom = new System.Random(seed.GetHashCode());
        Size = initialSize * scaleUpFactor;

        floorMap = new int[Size.x + 1, Size.y + 1];
        randomFillMap(ref floorMap);
        for(int i = 0; i < numberOfSmoothingIterations; i++)
        {
            smoothMap(ref floorMap, ref initialSize);
        }

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
        Vector3 position = new Vector3( -(halfWidth * TileSize), 0.0f, -(halfHeight * TileSize) );
        bool start = true;

        for ( int x = 0; x < Size.x; ++x)
        {
            mMap[x] = new EnvironmentTile[Size.y];
            for ( int y = 0; y < Size.y; ++y)
            {
                EnvironmentTile tile = getFloorTile(x, y, position);
                tile.Position = new Vector3( position.x + (TileSize / 2), TileHeight, position.z + (TileSize / 2));               
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

                    Debug.LogFormat("Path Found: {0} steps {1} long", result.Count, destination.Local);
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

    public void clearTile(EnvironmentTile tileToClear)
    {
        swapTile(tileToClear, marchingSquareTiles[15], false, true);
    }

    public void swapTile(EnvironmentTile tileToSwap, EnvironmentTile newTile, bool canBeDestoyed, bool isAccessible)
    {
        //Swap an environment tile with a new one
        //New tile is instantiated and 
        Vector2Int tileCoord = tileToSwap.coordinates;
        Vector3 position = tileToSwap.Position;

        EnvironmentTile newInstance = Instantiate(newTile, tileToSwap.gameObject.transform.position, Quaternion.identity, transform);
        newInstance.gameObject.name = tileToSwap.gameObject.name;
        newInstance.IsAccessible = isAccessible;
        newInstance.canBeDestroyed = canBeDestoyed;
        newInstance.Position = position;
        newInstance.Connections = tileToSwap.Connections;
        newInstance.coordinates = tileToSwap.coordinates;

        mAll.Remove(tileToSwap);
        mAll.Add(newInstance);

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
    }
}
