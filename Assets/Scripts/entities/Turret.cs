using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
    [SerializeField] private GameObject gunBarrel;
    [SerializeField] private float range;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private Bullet bullet;
    [SerializeField] private float shootDelay;
    [SerializeField] private float bulletDamage;
    [SerializeField] private float enemyHeight;
    private float timeSinceLastShot = 0;
    private Vector3 barrelLocation;

    private List<Spawner> spawners;
    private Game game;
    private Enemy targetEnemy;

    private Transform bulletSpawnPoint;
    private ParticleSystem shootParticles;

    private void Start()
    {
        bulletSpawnPoint = gameObject.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0);
        shootParticles = GetComponentInChildren<ParticleSystem>();
        barrelLocation = transform.GetChild(0).position;
        barrelLocation.y = bulletSpawnPoint.position.y;
    }

    public void setSpawners(List<Spawner> spawners)
    {
        this.spawners = spawners;
    }

    public void setGame(Game game)
    {
        this.game = game;
    }

    // Update is called once per frame
    void Update()
    {
        if(!game.gameOver)
        {
            getTarget();

            if (timeSinceLastShot >= shootDelay)
            {
                if (targetEnemy != null && gunBarrel.transform.rotation.Equals(Quaternion.LookRotation((targetEnemy.transform.position + new Vector3(0, enemyHeight, 0) - barrelLocation).normalized)))
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
        if (targetEnemy != null && !targetEnemy.isDead && Vector3.Distance(transform.position, targetEnemy.transform.position) <= range)
        {
            Quaternion lookRotation = Quaternion.LookRotation((targetEnemy.transform.position + new Vector3(0, enemyHeight, 0) - barrelLocation).normalized);
            gunBarrel.transform.rotation = Quaternion.RotateTowards(gunBarrel.transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            float closestPathLength = float.MaxValue;

            foreach (Spawner spawner in spawners)
            {
                foreach (Enemy enemy in spawner.activeEnemies)
                {
                    if (enemy.pathLength < closestPathLength && Vector3.Distance(transform.position, enemy.transform.position) <= range)
                    {
                        targetEnemy = enemy;
                        closestPathLength = enemy.pathLength;
                        break; //Enemies are already ordered by path length when they are added so we can break early once a target is found
                    }
                }
            }
        }
    }

    private void shoot()
    {
        Bullet b = Instantiate(bullet, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        b.setShot(bulletDamage);
        shootParticles.Play();
    }
}
