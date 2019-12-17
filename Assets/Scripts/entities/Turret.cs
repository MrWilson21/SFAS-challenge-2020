using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Turret : MonoBehaviour
{
    [SerializeField] private float shootDelay;
    private float timeSinceLastShot = 0;

    private List<Spawner> spawners;
    private Game game;
    protected Enemy targetEnemy;


    public void setSpawners(List<Spawner> spawners)
    {
        this.spawners = spawners;
    }

    public void setGame(Game game)
    {
        this.game = game;
    }

    void Update()
    {
        if(!game.gameOver)
        {
            getTarget();

            if (timeSinceLastShot >= shootDelay)
            {
                if (targetEnemy != null && readyToShoot())
                {
                    shoot();
                    timeSinceLastShot = 0;
                }
            }
            else
            {
                timeSinceLastShot += Time.deltaTime;
            }
        }
    }

    private void getTarget()
    {
        if (targetEnemy != null && !targetEnemy.isDead && isInRange(targetEnemy))
        {
            aimTowardsTarget();
        }
        else
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
        }
    }

    protected abstract bool isInRange(Enemy enemy);

    protected abstract void aimTowardsTarget();

    protected abstract bool readyToShoot();

    protected abstract void shoot();
}
