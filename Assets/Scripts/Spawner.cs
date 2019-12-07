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

    public List<Enemy> activeEnemies;

    private List<Enemy> wave;
    [SerializeField] private float spawnDelay = 1;

    [SerializeField] private Enemy enemy;

    public void setWave(List<Enemy> wave)
    {
        StopAllCoroutines();
        this.wave = wave;
        activeEnemies = new List<Enemy>();
        StartCoroutine(doWave());
    }

    private IEnumerator doWave()
    {
        while (wave.Count > 0)
        {
            spawnEnemy();
            wave.RemoveAt(wave.Count - 1);
            yield return new WaitForSeconds(spawnDelay);
        }  
    }

    private void spawnEnemy()
    {
        Enemy e = Instantiate(wave[wave.Count - 1], spawnPoint.Position, Quaternion.identity, transform);
        activeEnemies.Add(e);

        e.setSpawner(this);

        List<EnvironmentTile> completeRoute = new List<EnvironmentTile>();
        completeRoute.AddRange(route);
        completeRoute.Insert(0, spawnPoint);
        completeRoute.Add(housePoint);
        e.GoTo(completeRoute);
    }
}
