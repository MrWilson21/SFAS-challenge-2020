using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float lifeSpan;

    private float damage;
    private bool hit = false;

    public void setShot(float damage)
    {
        this.damage = damage;
        GetComponent<Rigidbody>().AddRelativeForce(Vector3.forward * speed);

        Destroy(gameObject, lifeSpan);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(!hit)
        {
            collision.gameObject.GetComponent<Enemy>().doDamage(damage);
            hit = true;
        }      
        Destroy(gameObject);
    }
}
