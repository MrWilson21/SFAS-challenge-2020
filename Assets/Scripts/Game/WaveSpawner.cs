using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [SerializeField] private List<Enemy> spawnerEnemies;
    private List<int> enemyCount;

    [SerializeField] private AnimationCurve normalEnemyCountCurve;
    [SerializeField] private AnimationCurve FastEnemyCountCurve;
    [SerializeField] private AnimationCurve enemyHealthCurve;
    [SerializeField] private AnimationCurve spawnRateCurve;

    private List<Spawner> spawners;
    private int numberOfActiveSpawners;
    private int maxNumberOfSpawners;
    public int totalNumberOfEnemies { get; set; }

    private System.Random pseudoRandom = new System.Random();

    public void setSpawners(List<Spawner> spawners)
    {
        this.spawners = spawners;
        maxNumberOfSpawners = spawners.Count;
    }

    public void makeWave(int waveCount)
    {
        numberOfActiveSpawners = Mathf.Clamp(waveCount / 5, 1, maxNumberOfSpawners);

        enemyCount = new List<int>();
        enemyCount.Add(Mathf.RoundToInt(normalEnemyCountCurve.Evaluate(waveCount)));
        enemyCount.Add(Mathf.RoundToInt(FastEnemyCountCurve.Evaluate(waveCount)));

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
            spawners[i].setWave(enemiesForEachSpawner[i], enemyHealthCurve.Evaluate(waveCount), spawnRateCurve.Evaluate(waveCount));
        }
    }
}
