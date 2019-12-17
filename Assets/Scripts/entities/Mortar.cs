using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mortar : Turret
{
    [SerializeField] private GameObject gunBarrel;
    [SerializeField] private float maxRange;
    [SerializeField] private float minRange;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float shellVelocity;
    [SerializeField] protected MortarShell mortarShell;
    [SerializeField] protected float shellDamage;

    private Vector3 barrelLocation;
    private Transform bulletSpawnPoint;
    private ParticleSystem shootParticles;

    private Vector3 targetVelocity;
    private Quaternion targetRotation;
    private const float gravity = 9.81f;
    private float shellDetonateHeight;

    private void Start()
    {
        bulletSpawnPoint = gameObject.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0);
        shootParticles = GetComponentInChildren<ParticleSystem>();
        barrelLocation = transform.GetChild(0).position;
        shellDetonateHeight = barrelLocation.y;
        barrelLocation.y = bulletSpawnPoint.position.y;
    }

    override protected void shoot()
    {
        MortarShell shell = Instantiate(mortarShell, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        shell.setShot(shellDamage, targetVelocity, shellDetonateHeight);
        shootParticles.Play();
    }

    protected override bool isInRange(Enemy enemy)
    {
        float distance = Vector3.Distance(barrelLocation, enemy.transform.position);
        return distance <= maxRange && distance >= minRange;
    }

    protected override void aimTowardsTarget()
    {
        //Aim gun towards enemy first
        Quaternion lookRotation = Quaternion.LookRotation((targetEnemy.transform.position - barrelLocation).normalized);
        Quaternion oldRotation = gunBarrel.transform.rotation;

        gunBarrel.transform.rotation = lookRotation;
        if (solveShootAngle(targetEnemy.transform.position, out targetVelocity))
        {
            targetRotation = Quaternion.LookRotation(targetVelocity.normalized);
            gunBarrel.transform.rotation = Quaternion.RotateTowards(oldRotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            gunBarrel.transform.rotation = oldRotation;
        }
    }

    protected override bool readyToShoot()
    {
        return gunBarrel.transform.rotation.Equals(targetRotation);
    }

    private bool solveShootAngle(Vector3 target, out Vector3 angle)
    {
        // Solve firing angles for a ballistic projectile with speed and gravity to hit a fixed position.
        // return true if angle found, false if not
        // set angle to angle found if one exists

        // Derivation
        //   (1) x = v*t*cos O
        //   (2) y = v*t*sin O - .5*g*t^2
        // 
        //   (3) t = x/(cos O*v)                                        [solve t from (1)]
        //   (4) y = v*x*sin O/(cos O * v) - .5*g*x^2/(cos^2 O*v^2)     [plug t into y=...]
        //   (5) y = x*tan O - g*x^2/(2*v^2*cos^2 O)                    [reduce; cos/sin = tan]
        //   (6) y = x*tan O - (g*x^2/(2*v^2))*(1+tan^2 O)              [reduce; 1+tan O = 1/cos^2 O]
        //   (7) 0 = ((-g*x^2)/(2*v^2))*tan^2 O + x*tan O - (g*x^2)/(2*v^2) - y    [re-arrange]
        //   Quadratic! a*p^2 + b*p + c where p = tan O
        //
        //   (8) let gxv = -g*x*x/(2*v*v)
        //   (9) p = (-x +- sqrt(x*x - 4gxv*(gxv - y)))/2*gxv           [quadratic formula]
        //   (10) p = (v^2 +- sqrt(v^4 - g(g*x^2 + 2*y*v^2)))/gx        [multiply top/bottom by -2*v*v/x; move 4*v^4/x^2 into root]
        //   (11) O = atan(p)

        angle = Vector3.zero;

        Vector3 diff = target - bulletSpawnPoint.position;
        Vector3 diffXZ = new Vector3(diff.x, 0f, diff.z);
        float groundDist = diffXZ.magnitude;

        float speed2 = Mathf.Pow(shellVelocity, 2);
        float speed4 = Mathf.Pow(shellVelocity, 4);
        float y = diff.y;
        float x = groundDist;
        float gx = gravity * x;

        float root = speed4 - gravity * (gravity * x * x + 2 * y * speed2);

        // No solution
        if (root < 0)
            return false;

        root = Mathf.Sqrt(root);

        float lowAng = Mathf.Atan2(speed2 - root, gx);
        float highAng = Mathf.Atan2(speed2 + root, gx);
        int numSolutions = lowAng != highAng ? 2 : 1;

        Vector3 groundDir = diffXZ.normalized;

        //Return high angle if found
        if (numSolutions == 2)
        {
            angle = groundDir * Mathf.Cos(highAng) * shellVelocity + Vector3.up * Mathf.Sin(highAng) * shellVelocity;
            return true;
        }        
        //Return low angle if no high angle
        else if (numSolutions == 1)
        {
            angle = groundDir * Mathf.Cos(lowAng) * shellVelocity + Vector3.up * Mathf.Sin(lowAng) * shellVelocity;
            return true;
        }
        //Return false if no angle found
        return false;
    }
}
