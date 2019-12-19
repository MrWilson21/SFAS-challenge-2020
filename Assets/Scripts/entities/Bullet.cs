using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    //Bullet that is fired from machine gun

    [SerializeField] private float speed;
    [SerializeField] private float lifeSpan;

    private float damage;
    private bool hit = false;

    public void setShot(float damage)
    {
        //Set up bullet with damage and velocity
        this.damage = damage;
        GetComponent<Rigidbody>().AddRelativeForce(Vector3.forward * speed);

        //Destroy bullet after lifespan over
        Destroy(gameObject, lifeSpan);
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Need to check if hit incase multiple enemies hit on a single frame
        if(!hit)
        {
            collision.gameObject.GetComponent<Enemy>().doDamage(damage);
            hit = true; //Set immediatly after doing first collision so subsiquent collisions in the same frame are not included
        }      
        Destroy(gameObject);
    }
}
