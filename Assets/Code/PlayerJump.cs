using UnityEngine;

public class PlayerJump : MonoBehaviour
{
    private static readonly int Ground = Animator.StringToHash("Ground");

    private enum Type { Velocity, Raycast, Overlap, Collider, IsTouch }
    [SerializeField] private Type logicType;
    [SerializeField] private LayerMask groundLayer;

    public bool isGround;

    [SerializeField] private float power;
    [SerializeField] private float minVelocityY;
    [SerializeField] private float maxVelocityY;

    private Rigidbody2D body;
    private Animator anim;
    private Collider2D coll;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        coll = FindFirstObjectByType<CompositeCollider2D>();
    }

    private void FixedUpdate()
    {
        switch (logicType) {            
            case Type.Raycast:
                RaycastHit2D hit = Physics2D.CircleCast(body.position + Vector2.down * 0.3f, 0.2f, Vector2.zero, 0f, groundLayer);
                if (hit.collider) {
                    isGround = true;
                }
                break;
            case Type.Overlap:
                Collider2D hitColl = Physics2D.OverlapCircle(body.position + Vector2.down * 0.3f, 0.2f, groundLayer);
                if (hitColl) {
                    isGround = true;
                }
                break;
            case Type.IsTouch:
                isGround = body.IsTouching(coll);
                break;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Ground") || logicType != Type.Collider)
            return;

        if (body.linearVelocityY > minVelocityY)
            return;

        isGround = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Ground") || logicType != Type.Collider)
            return;

        isGround = false;
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Ground") && logicType != Type.Velocity)
            return;
        
        if (body.linearVelocityY > 1)
            return;

        isGround = collision.contacts[0].normal.y > 0;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Ground") && logicType == Type.Collider)
            return;

        if (body.linearVelocityY < 1)
            return;

        isGround = false;
    }
    
    private void LateUpdate()
    {
        anim.SetBool(Ground, isGround);
    }

    private void OnJump()
    {
        if (!isGround)
            return;

        body.AddForceY(power, ForceMode2D.Impulse);
        // SendMessage("SetVertical", power);
    }
}
