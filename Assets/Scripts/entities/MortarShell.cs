using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MortarShell : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float lifeTimeAfterDetonation;

    [SerializeField] private ParticleSystem explodeParticles;
    [SerializeField] private GameObject bulletMesh;

    private float damage;
    private Vector3 velocity;
    private float detonateHeight;
    private const float gravity = 9.81f;

    private bool detonated = false;

    private void Update()
    {
        if(!detonated)
        {
            transform.position += velocity * Time.deltaTime * speed;
            velocity.y -= gravity * speed * Time.deltaTime;

            Quaternion lookRotation = Quaternion.LookRotation(velocity.normalized);
            transform.rotation = lookRotation;

            if (transform.position.y <= detonateHeight)
            {
                detonate();
            }
        }
    }

    public void setShot(float damage, Vector3 velocity, float detonateHeight)
    {
        this.damage = damage;
        this.velocity = velocity;
        this.detonateHeight = detonateHeight;
    }

    private void detonate()
    {
        GetComponent<Collider>().enabled = true;
        detonated = true;
        bulletMesh.SetActive(false);
        explodeParticles.Play();
        Destroy(gameObject, lifeTimeAfterDetonation);
    }

    private void OnCollisionEnter(Collision collision)
    {
        collision.gameObject.GetComponent<Enemy>().doDamage(damage);            
    }
}
