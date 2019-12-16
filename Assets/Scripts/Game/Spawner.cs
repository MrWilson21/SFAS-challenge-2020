using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    private Game game;

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
    private float enemyHealthMultiplier;
    public float spawnDelay { get; set; }

    public void setGame(Game game)
    {
        this.game = game;
    }

    public void setWave(List<Enemy> wave, float healthMultiplier, float spawnDelay)
    {
        StopAllCoroutines();
        this.wave = wave;
        this.spawnDelay = spawnDelay;
        enemyHealthMultiplier = healthMultiplier;
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
        e.setHealth(enemyHealthMultiplier);
        e.setGame(game);

        List<EnvironmentTile> completeRoute = new List<EnvironmentTile>();
        completeRoute.AddRange(route);
        completeRoute.Insert(0, spawnPoint);
        completeRoute.Add(housePoint);
        e.GoTo(completeRoute);
    }
}
