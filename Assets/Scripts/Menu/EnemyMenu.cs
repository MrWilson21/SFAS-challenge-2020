using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMenu : MonoBehaviour
{
    //Movement script for enemies found on menu screen
    //Moves enemies around in a circle 

    [SerializeField] private float speed;
    [SerializeField] private float turnSpeed;

    void Update()
    {
        transform.Translate(new Vector3(0, 0, speed * Time.deltaTime), Space.Self);
        transform.Rotate(new Vector3(0, turnSpeed * Time.deltaTime, 0));
    }
}
