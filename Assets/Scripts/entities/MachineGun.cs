using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineGun : Turret
{
    //Inherits from Turret base class

    [SerializeField] private GameObject gunBarrel;
    [SerializeField] private float range;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float enemyHeight;
    private Vector3 barrelLocation; //Location of the middle of the turret at the height of the gun barrel

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
        //Create new bullet aiming towards target enemy and particle effects
        Bullet b = Instantiate(bullet, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        b.setShot(bulletDamage);
        shootParticles.Play();
    }

    protected override bool isInRange(Enemy enemy)
    {
        //Check if enemy within max range
        return Vector3.Distance(barrelLocation, enemy.transform.position) <= range;
    }

    protected override void aimTowardsTarget()
    {
        //Gradually rotate gun barrel towards target enemy
        Quaternion lookRotation = Quaternion.LookRotation((targetEnemy.transform.position + new Vector3(0, enemyHeight, 0) - barrelLocation).normalized);
        gunBarrel.transform.rotation = Quaternion.RotateTowards(gunBarrel.transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }

    protected override bool readyToShoot()
    {
        //Can shoot when gun rotation is the same as the target rotation
        return gunBarrel.transform.rotation.Equals(Quaternion.LookRotation((targetEnemy.transform.position + new Vector3(0, enemyHeight, 0) - barrelLocation).normalized));
    }
}
