using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Turret : MonoBehaviour
{
    //Abstract class for turret
    //Provides methods to target enemies and decide when to shoot

    [SerializeField] private float shootDelay;
    [SerializeField] private float retargetDelay; //Delay before trying to find a new target to improve performance
    private float timeSinceLastShot = 0;

    private List<Spawner> spawners;
    private Game game;

    //Replacement tile when upgraded
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
        //Continuously try to find a new target
        while(true)
        {
            float closestPathLength = float.MaxValue;

            foreach (Spawner spawner in spawners)
            {
                foreach (Enemy enemy in spawner.activeEnemies)
                {
                    //Choose enemy that is closest to the base and in range
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

    //Check if enemy is in range of turret
    protected abstract bool isInRange(Enemy enemy);

    //Aim gun towards enemy
    protected abstract void aimTowardsTarget();

    //Check if ready to shoot
    protected abstract bool readyToShoot();

    //Create a shot
    protected abstract void shoot();
}
