using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage, Vector2 knockback);
}