using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Collider2D))]
public class Player : MonoBehaviour, IDamageable
{
    // Animator 파라미터 해시값
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Ground = Animator.StringToHash("Ground");
    private static readonly int AttackCount = Animator.StringToHash("AttackCount");
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int DashTrigger = Animator.StringToHash("Dash");
    private static readonly int DashAttackTrigger = Animator.StringToHash("DashAttack");
    private static readonly int HurtTrigger = Animator.StringToHash("Hurt");
    private static readonly int DeadTrigger = Animator.StringToHash("Dead");

    [Header("이동/점프")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpPower = 10f;
    [SerializeField] private LayerMask groundLayer;

    [Header("대쉬")]
    [SerializeField] private float dashPower = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("체력")]
    [SerializeField] private int maxHp = 3;
    private int currentHp;

    [Header("공격")]
    [SerializeField] private AttackHitbox attackHitbox;
    [SerializeField] private float attackPush = 2f;
    
    private Rigidbody2D body;
    private Animator anim;
    private Collider2D col;

    private float moveInput;
    private bool isGround;

    // 상태 플래그
    private bool isAttacking;
    private bool isDashing;
    private bool isDashAttacking;
    private bool isHurt;
    private bool isDead;

    private float dashEndTime;
    private float lastDashTime;

    // 콤보
    private int attackCount; // 0=기본, 1=첫타, 2=둘째타
    private float comboTimer;
    private const float ComboWindow = 0.6f; // 2타 입력 가능한 시간
    
    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        currentHp = maxHp;
    }

    private void Update()
    {
        if (comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f && attackCount == 1)
            {
                // 1타에서 입력 안 들어왔으면 콤보 종료
                attackCount = 0;
                anim.SetInteger(AttackCount, attackCount);
            }
        }
    }
    
    private void FixedUpdate()
    {
        if (isDead || isHurt) return;

        if (isDashing)
        {
            body.linearVelocityX = transform.localScale.x * dashPower; // 대쉬 중엔 강제 속도 유지
            return;
        }

        if (isDashAttacking)
        {
            body.linearVelocityX *= 0.9f;
            return;
        }
        
        if (isAttacking)
        {
            body.linearVelocityX = 0; // 공격 중 정지
            return;
        }
        
        // 일반 이동 (공격 중엔 speedMultiplier 적용)
        float vx = moveInput * moveSpeed;
        body.linearVelocity = new Vector2(vx, body.linearVelocity.y);
    }

    private void LateUpdate()
    {
        anim.SetFloat(Speed, Mathf.Abs(moveInput));
        anim.SetBool(Ground, isGround);

        if (!isAttacking && !isDashing && !isDashAttacking && moveInput != 0 && !isDead)
        {
            Vector3 scale = transform.localScale;
            scale.x = (moveInput > 0) ? 1 : -1;
            transform.localScale = scale;
        }

        if (isDashing && Time.time >= dashEndTime)
            isDashing = false;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Ground")) return;
        if (body.linearVelocityY > 1) return;
        isGround = collision.contacts[0].normal.y > 0;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Ground")) return;
        if (Physics2D.OverlapCircle(body.position + Vector2.down * 0.65f + Vector2.left * 0.235f * transform.localScale.x, 0.2f, groundLayer)) return;
        isGround = false;
    }

    // isGround 디버그용
    private void OnDrawGizmosSelected()
    {
        if (body == null) return;

        // OverlapCircle 위치 계산
        Vector2 circlePos = body.position + Vector2.down * 0.65f + Vector2.left * 0.235f * transform.localScale.x;
        float radius = 0.2f;

        // Gizmos 색상
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(circlePos, radius);

        // 땅 레이어와 겹치는지 확인
        bool hit = Physics2D.OverlapCircle(circlePos, radius, groundLayer);
        if (hit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(circlePos, radius); // 겹치면 빨간색으로 표시
        }
    }
    
    // -----------------------
    // Input System
    // -----------------------

    private void OnMove(InputValue value)
    {
        if (isDead) return;
        moveInput = value.Get<Vector2>().x;
    }

    private void OnJump()
    {
        if (isDead || isHurt || isAttacking || isDashing || !isGround) return;
        body.AddForceY(jumpPower, ForceMode2D.Impulse);
    }

    private void OnAttack()
    {
        if (isDead || isHurt || !isGround) return;

        // 대쉬 중 공격
        if (isDashing)
        {
            // 대쉬 공격
            isDashing = false;
            isAttacking = true;
            isDashAttacking = true;
            anim.SetTrigger(DashAttackTrigger);

            return;
        }
        
        // 첫 공격
        if (!isAttacking)
        {
            attackCount = 1;
            anim.SetInteger(AttackCount, attackCount);
            anim.SetTrigger(AttackTrigger);
            isAttacking = true;
            comboTimer = ComboWindow;  // 2타 입력 대기 시작
        }
        // 2타 입력 (타이머 안에서만 가능)
        else if (attackCount == 1 && comboTimer > 0f)
        {
            attackCount = 2;
            anim.SetInteger(AttackCount, attackCount);
            anim.SetTrigger(AttackTrigger);
            comboTimer = 0f; // 2타까지이므로 타이머 리셋
        }
    }

    private void OnDash()
    {
        if (isDead || isHurt || isAttacking || !isGround) return;
        if (Time.time - lastDashTime < dashCooldown) return;

        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        lastDashTime = Time.time;

        anim.SetTrigger(DashTrigger);
    }

    private void OnDebug()
    {
        Debug.Log($"isAttacking : {isAttacking}");
        Debug.Log($"isDashing :  {isDashing}");
        Debug.Log($"isDashAttacking : {isDashAttacking}");
    }
    
    // -----------------------
    // HP / Damage / Death
    // -----------------------

    public void TakeDamage(int damage, Vector2 knockback)
    {
        if (isDead) return;

        currentHp -= damage;
        isHurt = true;
        EndAttack();
        anim.SetTrigger(HurtTrigger);

        // 넉백 적용
        body.linearVelocity = Vector2.zero; // 기존 속도 리셋
        body.AddForce(knockback, ForceMode2D.Impulse);
        
        if (currentHp <= 0) Die();
    }

    private void Die()
    {
        isDead = true;
        anim.SetTrigger(DeadTrigger);
        body.simulated = false;
        col.enabled = false;
    }

    // -----------------------
    // 애니메이션 이벤트
    // -----------------------

    public void StartAttack()
    {
        isAttacking = true;

        if(!isDashAttacking)
            body.linearVelocityX = 0; // 기존 수평 속도 초기화
        
        // 공격 시 앞으로 밀림
        Vector2 push = new Vector2(transform.localScale.x * attackPush, 0f);
        body.AddForce(push, ForceMode2D.Impulse);
    }

    public void HitAttack()
    {
        attackHitbox?.EnableHitbox();
    }
    
    public void EndAttack()
    {
        attackHitbox?.DisableHitbox();

        if (isDashAttacking)
        {
            isDashAttacking = false;
            isAttacking = false;
        }
        else
        {
            isAttacking = false;
            
            // 2타까지 끝나면 완전히 초기화
            if (attackCount >= 2)
            {
                attackCount = 0;
                anim.SetInteger(AttackCount, attackCount);
                comboTimer = 0f;
            }
        }
    }

    public void EndHurt()
    {
        isHurt = false; // Hurt 애니메이션 끝날 때 호출
    }
}
