using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    //Position enemies start at
    public EnvironmentTile spawnPoint { get; set; }
    //Initial target for enemies to go to leave spawn point;
    public EnvironmentTile spawnExitPoint { get; set; }
    //Route for enemies to take after exiting spawnPoint
    public List<EnvironmentTile> route { get; set; }
    //Final tile for enemies to walk to to reach house
    public EnvironmentTile housePoint { get; set; }

    [SerializeField] private Character enemy;

    public void spawnEnemy()
    {
        Character e = Instantiate(enemy, spawnPoint.Position, Quaternion.identity, transform);

        List<EnvironmentTile> completeRoute = route;
        completeRoute.Insert(0, spawnPoint);
        completeRoute.Add(housePoint);
        e.GoTo(route);
    }
}
