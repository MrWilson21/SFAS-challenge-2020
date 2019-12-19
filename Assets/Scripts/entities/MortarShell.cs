using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MortarShell : MonoBehaviour
{
    //shell fired by mortar
    //Travels in an arc before exploding

    [SerializeField] private float speed;
    [SerializeField] private float lifeTimeAfterDetonation;

    [SerializeField] private ParticleSystem explodeParticles;
    [SerializeField] private GameObject bulletMesh;

    private float damage;
    private Vector3 velocity;
    private float detonateHeight; // Height needed before shell detonates
    private const float gravity = 9.81f;

    private bool detonated = false;

    private void Update()
    {
        //While not detonated continue moving in an arc
        if(!detonated)
        {
            transform.position += velocity * Time.deltaTime * speed;
            velocity.y -= gravity * speed * Time.deltaTime; //Accelerate by gravity amount

            //Aim towards direction of movement
            Quaternion lookRotation = Quaternion.LookRotation(velocity.normalized); 
            transform.rotation = lookRotation;

            //Detonate when detonation height reached
            if (transform.position.y <= detonateHeight)
            {
                detonate();
            }
        }
    }

    public void setShot(float damage, Vector3 velocity, float detonateHeight)
    {
        //Set velocity, damage and detonation height
        this.damage = damage;
        this.velocity = velocity;
        this.detonateHeight = detonateHeight;
    }

    private void detonate()
    {
        //Enable detonation collider and create particle effect
        GetComponent<Collider>().enabled = true;
        detonated = true;
        bulletMesh.SetActive(false);
        Instantiate(explodeParticles, transform.position, Quaternion.identity, transform);
        Destroy(gameObject, lifeTimeAfterDetonation);
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Damage enemies that go inside detonation colliders
        collision.gameObject.GetComponent<Enemy>().doDamage(damage);            
    }
}
