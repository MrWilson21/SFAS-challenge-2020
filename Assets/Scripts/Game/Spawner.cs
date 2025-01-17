﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    //Creates waves of enemies and gives them paths to follow

    private Game game;

    //Position enemies start at
    public EnvironmentTile spawnPoint { get; set; }
    //Initial target for enemies to go to leave spawn point;
    public EnvironmentTile spawnExitPoint { get; set; }
    //Route for enemies to take after exiting spawnPoint
    public List<EnvironmentTile> route { get; set; }
    //Final tile for enemies to walk to to reach house
    public EnvironmentTile housePoint { get; set; }

    //Currently alive enemies 
    public List<Enemy> activeEnemies; 

    //Current wave to spawn
    private List<Enemy> wave;
    private float enemyHealthMultiplier;
    public float spawnDelay { get; set; }

    public void setGame(Game game)
    {
        this.game = game;
    }

    public void setWave(List<Enemy> wave, float healthMultiplier, float spawnDelay)
    {
        //Gets wave of enemies to start spawning
        StopAllCoroutines();
        this.wave = wave;
        this.spawnDelay = spawnDelay;
        enemyHealthMultiplier = healthMultiplier;
        activeEnemies = new List<Enemy>();
        StartCoroutine(doWave());
    }

    public void endWave()
    {
        StopAllCoroutines();
    }


    private IEnumerator doWave()
    {
        //Spawns enemies with a set delay until none left to spawn
        while (wave.Count > 0)
        {
            spawnEnemy();
            wave.RemoveAt(wave.Count - 1);
            yield return new WaitForSeconds(spawnDelay);
        }  
    }

    private void spawnEnemy()
    {
        //Create enemy and set its health and path to follow
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
