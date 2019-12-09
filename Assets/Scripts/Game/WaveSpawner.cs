using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [SerializeField] private List<int> enemyCount;
    [SerializeField] private List<Enemy> spawnerEnemies;

    private List<Spawner> spawners;
    private int numberOfActiveSpawners;
    private int maxNumberOfSpawners;
    private int totalNumberOfEnemies;

    private System.Random pseudoRandom = new System.Random();

    public void setSpawners(List<Spawner> spawners)
    {
        this.spawners = spawners;
        maxNumberOfSpawners = spawners.Count;
        numberOfActiveSpawners = 4;
    }

    public void makeWave()
    {
        totalNumberOfEnemies = 0;
        foreach(int i in enemyCount)
        {
            totalNumberOfEnemies += i;
        }

        List<List<Enemy>> enemiesForEachSpawner = new List<List<Enemy>>();

        for (int i = 0; i < numberOfActiveSpawners; i++)
        {
            enemiesForEachSpawner.Add(new List<Enemy>());
        }

        int count = 0;
        int spawnerIndex = 0;
        while(count != totalNumberOfEnemies)
        {
            int index = pseudoRandom.Next(0, spawnerEnemies.Count);

            if(enemyCount[index] > 0)
            {
                enemyCount[index] -= 1;
                enemiesForEachSpawner[spawnerIndex % numberOfActiveSpawners].Add(spawnerEnemies[index]);
                count++;
                spawnerIndex++;
            }
        }

        for (int i = 0; i < numberOfActiveSpawners; i++)
        {
            spawners[i].setWave(enemiesForEachSpawner[i]);
        }
    }
}
