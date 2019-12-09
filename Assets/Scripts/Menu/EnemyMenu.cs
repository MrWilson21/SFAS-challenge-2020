using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMenu : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float turnSpeed;

    // Update is called once per frame
    void Update()
    {
        transform.Translate(new Vector3(0, 0, speed * Time.deltaTime), Space.Self);
        transform.Rotate(new Vector3(0, turnSpeed * Time.deltaTime, 0));
    }
}
