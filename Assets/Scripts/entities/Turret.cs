using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Turret : MonoBehaviour
{
    [SerializeField] private float shootDelay;
    [SerializeField] private float retargetDelay;
    private float timeSinceLastShot = 0;

    private List<Spawner> spawners;
    private Game game;

    public EnvironmentTile turretUpgrade;
    public int upgradeCost;

    protected Enemy targetEnemy;

    [SerializeField] private AudioClip shootSound;
    private AudioSource soundSource;

    public void setSpawners(List<Spawner> spawners)
    {
        this.spawners = spawners;
        soundSource = GetComponent<AudioSource>();
        soundSource.clip = shootSound;
        StartCoroutine(getTarget());
    }

    public void setGame(Game game)
    {
        this.game = game;
    }

    void Update()
    {
        if(!game.gameOver)
        {
            if (targetEnemy != null && !targetEnemy.isDead && isInRange(targetEnemy))
            {
                aimTowardsTarget();
            }

            if (timeSinceLastShot >= shootDelay)
            {
                if (targetEnemy != null && !targetEnemy.isDead && readyToShoot())
                {
                    shoot();
                    soundSource.Play();
                    timeSinceLastShot = 0;
                }
            }
            else
            {
                timeSinceLastShot += Time.deltaTime;
            }
        }
    }

    private IEnumerator getTarget()
    {
        while(true)
        {
            float closestPathLength = float.MaxValue;

            foreach (Spawner spawner in spawners)
            {
                foreach (Enemy enemy in spawner.activeEnemies)
                {
                    if (enemy.pathLength < closestPathLength && isInRange(enemy))
                    {
                        targetEnemy = enemy;
                        closestPathLength = enemy.pathLength;
                        break; //Enemies are already ordered by path length when they are added so we can break early once a target is found
                    }
                }
            }

            yield return new WaitForSeconds(retargetDelay);
        }     
    }

    protected abstract bool isInRange(Enemy enemy);

    protected abstract void aimTowardsTarget();

    protected abstract bool readyToShoot();

    protected abstract void shoot();
}
