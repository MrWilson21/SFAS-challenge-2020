using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    //Creates waves of enemies and sends them to the spawners

    [SerializeField] private List<Enemy> spawnerEnemies;
    private List<int> enemyCount;

    //Curves to determine the amount and strength of enemies depending on the current wave
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
        //Get the list of spawners
        this.spawners = spawners;
        maxNumberOfSpawners = spawners.Count;
    }

    public void makeWave(int waveCount)
    {
        //Make new wave depending on wave number
        //new spawner is added every 5 rounds up to the maximum
        numberOfActiveSpawners = Mathf.Clamp(waveCount / 5, 1, maxNumberOfSpawners);

        //Amount of each enemy to spawn
        enemyCount = new List<int>();
        enemyCount.Add(Mathf.RoundToInt(normalEnemyCountCurve.Evaluate(waveCount)));
        enemyCount.Add(Mathf.RoundToInt(FastEnemyCountCurve.Evaluate(waveCount)));

        //get total number needed to spawn
        totalNumberOfEnemies = 0;
        foreach(int i in enemyCount)
        {
            totalNumberOfEnemies += i;
        }

        //Create a list of enemies for each active spawner
        List<List<Enemy>> enemiesForEachSpawner = new List<List<Enemy>>();

        for (int i = 0; i < numberOfActiveSpawners; i++)
        {
            enemiesForEachSpawner.Add(new List<Enemy>());
        }

        //Choose a random enemy to add to spawner and then decrement the count for that enemy
        //Stop when no more enemies to choose add
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

        //Send wave to each spawner
        for (int i = 0; i < numberOfActiveSpawners; i++)
        {
            spawners[i].setWave(enemiesForEachSpawner[i], enemyHealthCurve.Evaluate(waveCount), spawnRateCurve.Evaluate(waveCount));
        }
    }
}
