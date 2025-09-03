using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpriteRenderer))]
public class PlayerMove : MonoBehaviour
{
    private static readonly int Speed = Animator.StringToHash("Speed");

    private enum MoveType { Velocity, MovePosition, AddForce, Slide }

    [SerializeField] private MoveType moveType;
    [SerializeField] private float speed = 5f;

    private float inputValue;
    private float verticalPower;

    private Rigidbody2D body;
    private Animator anim;
    private SpriteRenderer sprite;

    [SerializeField] private GameObject breaker;

    // Slide용 구조체
    private struct SlideData { public Vector2 SurfaceAnchor; }
    private SlideData slide;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        switch (moveType)
        {
            case MoveType.Velocity:
                body.linearVelocity = new Vector2(inputValue * speed, body.linearVelocity.y);
                break;

            case MoveType.AddForce:
                body.AddForce(Vector2.right * (inputValue * speed));
                break;

            case MoveType.MovePosition:
                verticalPower = Mathf.MoveTowards(verticalPower, Physics2D.gravity.y, 0.5f);
                body.MovePosition(body.position +
                    Vector2.right * (inputValue * speed * Time.fixedDeltaTime) +
                    Vector2.up * (verticalPower * Time.fixedDeltaTime));
                break;

            case MoveType.Slide:
                verticalPower = Mathf.MoveTowards(verticalPower, 0, 0.5f);
                Vector2 move = Vector2.right * (inputValue * speed) + Vector2.up * verticalPower;
                // Slide 구현: surfaceAnchor와 deltaTime 기반
                body.MovePosition(body.position + move * Time.fixedDeltaTime);
                break;
        }
    }

    private void LateUpdate()
    {
        anim.SetFloat(Speed, Mathf.Abs(inputValue));
        if (inputValue != 0) sprite.flipX = inputValue < 0;
    }

    private void OnMove(InputValue value)
    {
        inputValue = value.Get<Vector2>().x;
        if (breaker != null) breaker.SetActive(inputValue == 0);
    }

    public void SetVertical(float power)
    {
        slide.SurfaceAnchor = Vector2.zero;
        verticalPower = power;
    }
}
