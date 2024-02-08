using System.Collections;
using System.Collections.Generic;
using Player;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    #region Variables

    private float currentHealth;
    public float maximumHealth;

    [SerializeField, Range(0f, 5f)] private float rotationSpeed;
    public float attackDistance;
    bool canAttack;
    public int damage;

    private CapsuleCollider collider;

    [SerializeField] private Vector2 rotateRandTime = new Vector2(1, 5);
    private bool rotating = false;
    private Coroutine move;

    NavMeshAgent agent;
    GameObject target;

    #endregion

    #region Methods

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        collider = GetComponent<CapsuleCollider>();
    }

    // Use this for initialization
    void Start()
    {
        

        currentHealth = maximumHealth;

        canAttack = true;
    }

    // Update is called once per frame
    void Update()
    {
        PlayerCollision();
        FindClosestTarget();
        MoveToTarget();
        DamageTarget();
    }

    void PlayerCollision()
    {
        if (PlayerController.instance.dashed)
        {
            collider.isTrigger = true;
        }
        else
        {
            collider.isTrigger = false;
        }
    }

    void FindClosestTarget()
    {
        float playerDistance = Vector3.Distance(PlayerController.instance.transform.position, transform.position);
        float baseDistance = Vector3.Distance(BaseManager.instance.transform.position, transform.position);

        if (playerDistance < baseDistance)
        {
            target = PlayerController.instance.gameObject;
        }
        else
        {
            target = BaseManager.instance.gameObject;
        }
    }

    void MoveToTarget()
    {
        if (target != null)
        {
            if (!rotating)
            {
                move = StartCoroutine(RotateToTarget(target.transform));
                rotating = true;
            }
        }
    }

    IEnumerator RotateToTarget(Transform target)
    {
        // Generates a random integer used as a variable for the wait time before the rest of the corountine can continue
        float time = Random.Range(rotateRandTime.x, rotateRandTime.y);
        yield return new WaitForSeconds(time);

        agent.SetDestination(transform.position);

        Vector3 targetPos = target.transform.position;
        Quaternion targetDirection = Quaternion.LookRotation(targetPos - transform.position);

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime;

            transform.rotation = Quaternion.Slerp(transform.rotation, targetDirection, rotationSpeed * Time.deltaTime);

            // Forces the coroutine to wait until the next frame until it can run again. This makes the while loop act as a update while active
            yield return null;
        }

        agent.SetDestination(targetPos);

        while (agent.pathStatus != NavMeshPathStatus.PathComplete) { yield return null; }

        // Sets idle action back to false so that the coroutine will be run again in the InputMagnitude() method
        rotating = false;

        // Ends the coroutine
        StopCoroutine(move);
    }

    public void Damage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            agent.Stop();
            StartCoroutine(Die());
        }
    }

    void DamageTarget()
    {
        if (Vector3.Distance(target.transform.position, transform.position) < attackDistance)
        {
            if (canAttack)
            {

                if (target.GetComponent<BaseManager>())
                {
                    BaseManager.instance.Damage(damage);
                }
                else if (target.GetComponent<PlayerController>())
                {
                    PlayerController.instance.Damaged(damage);
                }

                canAttack = false;
                Invoke("ReactiveAttack", 1.5f);
            }
        }
    }

    void ReactiveAttack()
    {
        canAttack = true;
    }

    IEnumerator Die()
    {
        Destroy(this.gameObject);
        yield return new WaitForSeconds(0f);
    }

    #endregion

    #region Editor
    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }

    #endregion
}
