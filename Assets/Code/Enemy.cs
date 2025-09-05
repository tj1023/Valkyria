using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Collider2D))]
public class Enemy : MonoBehaviour, IDamageable
{
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int HurtTrigger = Animator.StringToHash("Hurt");
    private static readonly int DeadTrigger = Animator.StringToHash("Dead");

    private enum State { Patrol, Chase, Attack, Hurt, Dead }
    private State currentState = State.Patrol;

    [Header("이동 관련")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("플레이어 추적/공격")]
    [SerializeField] private Transform player;
    [SerializeField] private float chaseRange = 5f;
    [SerializeField] private float stopDistance = 1.2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private AttackHitbox attackHitbox;

    [Header("체력 관련")]
    [SerializeField] private int maxHp = 3;
    private int currentHp;

    private Rigidbody2D body;
    private Animator anim;
    private Collider2D col;
    private float direction = 1f;
    private float lastAttackTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        currentHp = maxHp;
    }

    private void FixedUpdate()
    {
        if (currentState == State.Dead || currentState == State.Hurt) return;

        if (!player)
        {
            currentState = State.Patrol;
            RunState();
            return;
        }

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(player.position.y - transform.position.y);

        if (distanceX <= stopDistance && distanceY < 1f)
            currentState = State.Attack;
        else if (distanceX <= chaseRange && distanceY < 1f)
            currentState = State.Chase;
        else
            currentState = State.Patrol;

        RunState();
    }

    private void LateUpdate()
    {
        anim.SetFloat(Speed, Mathf.Abs(body.linearVelocity.x));

        if (Mathf.Abs(body.linearVelocity.x) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = (direction > 0) ? 1 : -1;
            transform.localScale = scale;
        }
    }

    private void RunState()
    {
        switch (currentState)
        {
            case State.Patrol: Patrol(); break;
            case State.Chase: Chase(); break;
            case State.Attack: Attack(); break;
        }
    }

    private void Patrol()
    {
        body.linearVelocity = new Vector2(direction * speed, body.linearVelocity.y);

        if (!IsGroundAhead())
            direction *= -1f;
    }

    private void Chase()
    {
        direction = (player.position.x > transform.position.x) ? 1f : -1f;
        body.linearVelocity = new Vector2(direction * speed, body.linearVelocity.y);
    }

    private void Attack()
    {
        body.linearVelocity = new Vector2(0, body.linearVelocity.y);
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            anim.SetTrigger(AttackTrigger);
            lastAttackTime = Time.time;
        }
    }

    private bool IsGroundAhead()
    {
        Vector2 origin = col.bounds.center;
        origin.y = col.bounds.min.y; 
        origin.x += direction * (col.bounds.extents.x * 0.9f); 

        return Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
    }

    private void OnDrawGizmosSelected()
    {
        if (col != null)
        {
            Vector2 origin = col.bounds.center;
            origin.y = col.bounds.min.y;
            origin.x += direction * (col.bounds.extents.x * 0.9f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + Vector2.down * groundCheckDistance);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }

    // -----------------------
    // 피격/죽음 로직
    // -----------------------
    
    public void TakeDamage(int damage, Vector2 knockback)
    {
        if (currentState == State.Dead) return;

        currentHp -= damage;
        EndAttack();
        anim.SetTrigger(HurtTrigger);
        currentState = State.Hurt;
        
        // 넉백 적용
        body.linearVelocity = Vector2.zero;
        body.AddForce(knockback, ForceMode2D.Impulse);
        
        if (currentHp <= 0)
            Die();
    }

    private void Die()
    {
        currentState = State.Dead;
        anim.SetTrigger(DeadTrigger);
        body.simulated = false;
        col.enabled = false;
        // 여기서 점수 추가, 아이템 드롭 같은 처리 가능
    }

    // -----------------------
    // 애니메이션 이벤트
    // -----------------------
    
    public void StartAttack()
    {
        attackHitbox?.EnableHitbox();
    }

    public void EndAttack()
    {
        attackHitbox?.DisableHitbox();
    }
    
    public void EndHurt()
    {
        if (currentState == State.Hurt && currentHp > 0)
            currentState = State.Patrol; // 다시 기본 상태로 복귀
    }
}
