using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float SingleNodeMoveTime = 0.5f;

    public EnvironmentTile CurrentPosition { get; set; }
    public float pathLength { get; set; } = float.MaxValue;

    private int stepsLeft;

    private Spawner spawner;

    private IEnumerator DoMove(Vector3 position, Vector3 destination)
    {
        // Move between the two specified positions over the specified amount of time
        if (position != destination)
        {
            transform.rotation = Quaternion.LookRotation(destination - position, Vector3.up);

            Vector3 p = transform.position;
            float t = 0.0f;

            while (t < SingleNodeMoveTime)
            {
                t += Time.deltaTime;
                p = Vector3.Lerp(position, destination, t / SingleNodeMoveTime);
                transform.position = p;
                pathLength = (float)stepsLeft - t;
                yield return null;
            }
        }
    }

    private IEnumerator DoGoTo(List<EnvironmentTile> route)
    {
        // Move through each tile in the given route
        if (route != null)
        {
            stepsLeft = route.Count;
            Vector3 position = transform.position; // CurrentPosition.Position;
            for (int count = 0; count < route.Count; ++count)
            {
                Vector3 next = route[count].Position;
                yield return DoMove(position, next);
                stepsLeft--;
                CurrentPosition = route[count];
                position = next;
            }
        }
    }

    public void GoTo(List<EnvironmentTile> route)
    {
        // Clear all coroutines before starting the new route so 
        // that clicks can interupt any current route animation
        StopAllCoroutines();
        StartCoroutine(DoGoTo(route));
    }

    public void setSpawner(Spawner spawner)
    {
        this.spawner = spawner;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("house"))
        {
            die();
        }
    }

    private void die()
    {
        spawner.activeEnemies.Remove(this);
        Destroy(gameObject);
    }

    public void doDamage(float damage)
    {
        die();
    }
}
