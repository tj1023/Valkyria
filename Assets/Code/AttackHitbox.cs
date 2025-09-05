using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private LayerMask targetLayer;

    private Collider2D col;
    private Transform owner; // 공격자
    
    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.enabled = false; // 기본 비활성화
        owner = transform.root; // 기본적으로 Player나 Enemy의 루트 오브젝트
    }

    public void EnableHitbox() => col.enabled = true;
    public void DisableHitbox() => col.enabled = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & targetLayer) == 0) return;
        if (!other.TryGetComponent<IDamageable>(out var target)) return;
        
        // 넉백 방향 계산
        Vector2 dir = (other.transform.position - owner.position).normalized;
        Vector2 knockback = dir * knockbackForce;

        target.TakeDamage(damage, knockback);
    }
}
