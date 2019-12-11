using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float SingleNodeMoveTime = 0.5f;
    [SerializeField] private float baseHp;
    [SerializeField] private float rewardMultiplier;

    private enemyHealthBar healthBar;
    private Animator animator;

    private float hp;

    public EnvironmentTile CurrentPosition { get; set; }
    public float pathLength { get; set; } = float.MaxValue;

    private int stepsLeft;

    private Spawner spawner;
    private Game game;

    public bool isDead { get; set; }

    public void setHealth(float healthMultiplier)
    {
        healthBar = GetComponentInChildren<enemyHealthBar>();
        hp = baseHp * healthMultiplier;
        healthBar.setMax(hp);
        healthBar.setValue(hp);
        healthBar.hide();
    }

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

    public void setGame(Game game)
    {
        this.game = game;
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("house"))
        {
            reachEnd();
        }
    }

    private void die()
    {
        spawner.activeEnemies.Remove(this);
        game.enemyDie(rewardMultiplier);
        animator.SetBool("isDead", true);
        healthBar.hide();
        isDead = true;
        GetComponent<Collider>().enabled = false;
        StopAllCoroutines();
    }

    public void finishDying()
    {
        Destroy(gameObject);
    }

    private void reachEnd()
    {
        if(!isDead)
        {
            game.enemyReachesEnd();
        }      
    }

    public void doDamage(float damage)
    {
        if (!isDead)
        {
            hp -= damage;
            healthBar.setValue(hp);
            healthBar.show();

            if (hp <= 0)
            {
                die();
            }
        }    
    }
}
