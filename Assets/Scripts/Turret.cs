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
    private float timeSinceLastShot = 0;
    private Vector3 barrelLocation;

    private List<Spawner> spawners;
    private Enemy targetEnemy;

    private Transform bulletSpawnPoint;
    private ParticleSystem shootParticles;

    private void Start()
    {
        bulletSpawnPoint = gameObject.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0);
        shootParticles = GetComponentInChildren<ParticleSystem>();
        barrelLocation = transform.position;
        barrelLocation.y = bulletSpawnPoint.position.y;
    }

    public void setSpawners(List<Spawner> spawners)
    {
        this.spawners = spawners;
    }

    // Update is called once per frame
    void Update()
    {
        getTarget();

        if(timeSinceLastShot >= shootDelay)
        {
            if(targetEnemy != null && gunBarrel.transform.rotation.Equals(Quaternion.LookRotation((targetEnemy.transform.position - barrelLocation).normalized)))
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

    private void getTarget()
    {
        if (targetEnemy != null && Vector3.Distance(transform.transform.position, targetEnemy.transform.position) <= range)
        {
            Quaternion lookRotation = Quaternion.LookRotation((targetEnemy.transform.position - barrelLocation).normalized);
            gunBarrel.transform.rotation = Quaternion.RotateTowards(gunBarrel.transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            float closestPathLength = float.MaxValue;

            foreach (Spawner spawner in spawners)
            {
                foreach (Enemy enemy in spawner.activeEnemies)
                {
                    if (enemy.pathLength < closestPathLength && Vector3.Distance(transform.transform.position, enemy.transform.position) <= range)
                    {
                        targetEnemy = enemy;
                        closestPathLength = enemy.pathLength;
                    }
                }
            }
        }
    }

    private void shoot()
    {
        print(bullet);
        print(bulletSpawnPoint);
        Bullet b = Instantiate(bullet, bulletSpawnPoint.position, bulletSpawnPoint.rotation, bulletSpawnPoint);
        b.setShot(bulletDamage);
        shootParticles.Play();
    }
}
