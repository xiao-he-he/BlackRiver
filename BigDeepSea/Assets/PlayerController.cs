
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Animation Speed")]
    public float walkFPS = 8f;
    public float idleFPS = 4f;

    [Header("Idle Frames")]
    public Sprite[] idleDown;
    public Sprite[] idleUp;
    public Sprite[] idleLeft;
    public Sprite[] idleRight;

    [Header("Walk Frames")]
    public Sprite[] walkDown;
    public Sprite[] walkUp;
    public Sprite[] walkLeft;
    public Sprite[] walkRight;

    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private Vector2 lastDirection = Vector2.down;

    private Sprite[] currentFrames;
    private float frameTimer;
    private int frameIndex;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetAnimation(idleDown, idleFPS);
    }

    void Update()
    {
        // 获取 WASD 或方向键输入
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(x, y).normalized;

        if (moveInput != Vector2.zero)
        {
            // 记录最后移动方向，用于待机朝向
            if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
            {
                lastDirection = moveInput.x < 0
                    ? Vector2.left : Vector2.right;
            }
            else
            {
                lastDirection = moveInput.y < 0
                    ? Vector2.down : Vector2.up;
            }

            transform.position +=
                (Vector3)(moveInput * moveSpeed * Time.deltaTime);

            UpdateWalkAnimation();
        }
        else
        {
            UpdateIdleAnimation();
        }

        PlayCurrentAnimation();
    }

    void UpdateWalkAnimation()
    {
        if (lastDirection == Vector2.up)
            SetAnimationIfChanged(walkUp, walkFPS);
        else if (lastDirection == Vector2.down)
            SetAnimationIfChanged(walkDown, walkFPS);
        else if (lastDirection == Vector2.left)
            SetAnimationIfChanged(walkLeft, walkFPS);
        else
            SetAnimationIfChanged(walkRight, walkFPS);
    }

    void UpdateIdleAnimation()
    {
        if (lastDirection == Vector2.up)
            SetAnimationIfChanged(idleUp, idleFPS);
        else if (lastDirection == Vector2.down)
            SetAnimationIfChanged(idleDown, idleFPS);
        else if (lastDirection == Vector2.left)
            SetAnimationIfChanged(idleLeft, idleFPS);
        else
            SetAnimationIfChanged(idleRight, idleFPS);
    }

    void SetAnimationIfChanged(Sprite[] frames, float fps)
    {
        if (frames != currentFrames)
            SetAnimation(frames, fps);
    }

    void SetAnimation(Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0)
            return;

        currentFrames = frames;
        frameIndex = 0;
        frameTimer = 0f;
        spriteRenderer.sprite = currentFrames[0];
    }

    void PlayCurrentAnimation()
    {
        if (currentFrames == null || currentFrames.Length == 0)
            return;

        float fps = moveInput != Vector2.zero ? walkFPS : idleFPS;

        if (currentFrames.Length == 1)
        {
            spriteRenderer.sprite = currentFrames[0];
            return;
        }

        frameTimer += Time.deltaTime;

        if (frameTimer >= 1f / Mathf.Max(fps, 1f))
        {
            frameTimer = 0f;
            frameIndex = (frameIndex + 1) % currentFrames.Length;
            spriteRenderer.sprite = currentFrames[frameIndex];
        }
    }
}