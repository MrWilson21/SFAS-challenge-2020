using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineGun : Turret
{
    [SerializeField] private GameObject gunBarrel;
    [SerializeField] private float range;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float enemyHeight;
    private Vector3 barrelLocation;

    [SerializeField] protected Bullet bullet;
    [SerializeField] protected float bulletDamage;

    private Transform bulletSpawnPoint;
    private ParticleSystem shootParticles;

    private void Start()
    {
        bulletSpawnPoint = gameObject.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0);
        shootParticles = GetComponentInChildren<ParticleSystem>();
        barrelLocation = transform.GetChild(0).position;
        barrelLocation.y = bulletSpawnPoint.position.y;
    }


    override protected void shoot()
    {
        Bullet b = Instantiate(bullet, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        b.setShot(bulletDamage);
        shootParticles.Play();
    }

    protected override bool isInRange(Enemy enemy)
    {
        return Vector3.Distance(barrelLocation, enemy.transform.position) <= range;
    }

    protected override void aimTowardsTarget()
    {
        Quaternion lookRotation = Quaternion.LookRotation((targetEnemy.transform.position + new Vector3(0, enemyHeight, 0) - barrelLocation).normalized);
        gunBarrel.transform.rotation = Quaternion.RotateTowards(gunBarrel.transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }

    protected override bool readyToShoot()
    {
        return gunBarrel.transform.rotation.Equals(Quaternion.LookRotation((targetEnemy.transform.position + new Vector3(0, enemyHeight, 0) - barrelLocation).normalized));
    }
}
