
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class move : MonoBehaviour
{
    [Header("角色移动")]
    public float moveSpeed = 3f;

    [Header("跳跃")]
    [Min(0f)] public float jumpHeight = 0.35f;
    public KeyCode jumpKey = KeyCode.Space;
    public GameObject jumpForward;
    public GameObject jumpBack;
    public GameObject jumpLeft;
    public GameObject jumpRight;

    [Header("跳跃跨越碰撞")]
    public string jumpGroundTag = "Ground";
    public LayerMask jumpGroundLayers;
    public Collider2D groundCheck;

    [Header("跳跃地面阴影")]
    public SpriteRenderer jumpShadow;
    public Vector2 jumpShadowOffset = new Vector2(0.01f, -0.68f);
    public Vector2 jumpShadowMaxSize = new Vector2(0.72f, 0.18f);
    [Range(0f, 1f)] public float jumpShadowOpacity = 0.45f;

    [Header("推箱子检测")]
    public float pushCheckDistance = 0.65f;
    public LayerMask boxLayer;

    [Header("推箱反馈")]
    [Min(0f)] public float pushHoldDuration = 0.5f;
    [Min(0f)] public float pushShakeDuration = 0.12f;
    [Min(0f)] public float pushShakeMagnitude = 0.04f;

    [Header("动画状态物体")]
    public GameObject upIdle;
    public GameObject downIdle;
    public GameObject leftIdle;
    public GameObject rightIdle;

    public GameObject upMove;
    public GameObject downMove;
    public GameObject leftMove;
    public GameObject rightMove;

    [Header("推箱子动画状态物体")]
    public GameObject upPush;
    public GameObject downPush;
    public GameObject leftPush;
    public GameObject rightPush;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private readonly RaycastHit2D[] pushHits = new RaycastHit2D[16];
    private readonly System.Collections.Generic.HashSet<Collider2D> pushContacts =
        new System.Collections.Generic.HashSet<Collider2D>();

    private Vector2 moveInput;
    private Vector2 lastDirection = Vector2.down;
    private Vector2 knockbackVelocity;
    private StaffAbility staffAbility;

    public Vector2 FacingDirection => lastDirection;
    public bool IsJumping => isJumping;
    public bool IsMoving => moveInput != Vector2.zero && !isPushing;

    public void SetFacingDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0f)
            lastDirection = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
                ? new Vector2(Mathf.Sign(direction.x), 0f)
                : new Vector2(0f, Mathf.Sign(direction.y));
    }

    private bool isPushing;
    private bool isJumping;
    private float jumpTimer;
    private GameObject activeJumpState;
    private Vector3 jumpStateStartPosition;
    private PushBox nearbyBox;
    private PushBox chargingBox;
    private Vector2 chargingDirection;
    private float pushHoldTimer;
    private readonly System.Collections.Generic.List<Collider2D> ignoredJumpColliders =
        new System.Collections.Generic.List<Collider2D>();
    private readonly System.Collections.Generic.List<Collider2D> landingColliders =
        new System.Collections.Generic.List<Collider2D>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        staffAbility = GetComponent<StaffAbility>();
        if (groundCheck == null)
        {
            Transform groundCheckTransform = transform.Find("GroundCheck");
            if (groundCheckTransform != null)
            {
                groundCheck = groundCheckTransform.GetComponent<Collider2D>();
            }
        }

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        UpdateJumpShadow(0f);
        ShowState(downIdle);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;
        if (staffAbility == null)
            staffAbility = GetComponent<StaffAbility>();
        if (staffAbility != null)
            staffAbility.TickCasting();
        if (staffAbility != null && staffAbility.IsCasting)
        {
            moveInput = Vector2.zero;
            isPushing = false;
            ResetPushCharge();
            UpdateAnimation();
            return;
        }
        ReadInput();
        if (staffAbility != null && Input.GetKeyDown(staffAbility.castKey) &&
            staffAbility.TryCast())
        {
            moveInput = Vector2.zero;
            isPushing = false;
            ResetPushCharge();
            UpdateAnimation();
            return;
        }
        if (Input.GetKeyDown(jumpKey) && !isJumping)
        {
            StartJump();
        }

        if (isJumping)
        {
            UpdateJump();
            isPushing = false;
            nearbyBox = null;
            ResetPushCharge();
        }
        else
        {
            CheckPushBox();
        }

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = (isPushing || (staffAbility != null && staffAbility.IsCasting)
            ? Vector2.zero : moveInput * moveSpeed)
            + knockbackVelocity;
        knockbackVelocity = Vector2.MoveTowards(
            knockbackVelocity, Vector2.zero, 10f * Time.fixedDeltaTime);
    }

    // External forces must be composed here; assigning Rigidbody velocity
    // from a damage script would be overwritten on the next physics frame.
    public void ApplyKnockback(Vector2 velocity)
    {
        knockbackVelocity = velocity;
    }

    private void ReadInput()
    {
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        // 保留原来上下左右单方向移动的方式
        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
        {
            moveInput.y = 0f;
        }
        else
        {
            moveInput.x = 0f;
        }

        if (moveInput != Vector2.zero)
        {
            lastDirection = moveInput.normalized;
        }

        isPushing = false;
    }

    private void CheckPushBox()
    {
        if (moveInput == Vector2.zero)
        {
            ResetPushCharge();
            nearbyBox = null;
            return;
        }

        Vector2 direction = moveInput.normalized;
        if (nearbyBox != null && nearbyBox.IsMoving &&
            Vector2.Dot((Vector2)nearbyBox.transform.position - rb.position, direction) > 0f)
        {
            isPushing = true;
            return;
        }

        nearbyBox = null;
        if (playerCollider == null)
        {
            ResetPushCharge();
            return;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(boxLayer);
        filter.useTriggers = true;
        int hitCount = playerCollider.Cast(direction, filter, pushHits,
            Mathf.Clamp(pushCheckDistance, 0f, 0.05f));
        float nearestDistance = float.PositiveInfinity;
        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            PushBox box = pushHits[hitIndex].collider.GetComponentInParent<PushBox>();
            Vector2 offset = (Vector2)pushHits[hitIndex].collider.bounds.center -
                (Vector2)playerCollider.bounds.center;
            if (Vector2.Dot(offset, direction) <= 0f)
            {
                continue;
            }
            if (box != null && pushHits[hitIndex].distance < nearestDistance)
            {
                nearbyBox = box;
                nearestDistance = pushHits[hitIndex].distance;
            }
        }

        foreach (Collider2D contact in pushContacts)
        {
            if (contact == null || !contact.enabled || !contact.gameObject.activeInHierarchy)
            {
                continue;
            }
            Vector2 offset = (Vector2)contact.bounds.center - (Vector2)playerCollider.bounds.center;
            if (offset.sqrMagnitude > 0f &&
                Vector2.Dot(offset.normalized, direction) > 0.5f &&
                offset.sqrMagnitude < nearestDistance * nearestDistance)
            {
                nearbyBox = contact.GetComponentInParent<PushBox>();
                nearestDistance = offset.magnitude;
            }
        }

        if (nearbyBox != null)
        {
            isPushing = true;

            if (chargingBox != nearbyBox || chargingDirection != direction)
            {
                chargingBox = nearbyBox;
                chargingDirection = direction;
                pushHoldTimer = 0f;
            }

            pushHoldTimer += Time.deltaTime;
            if (pushHoldTimer >= pushHoldDuration)
            {
                if (nearbyBox.TryPush(direction))
                {
                    CameraShake2D.ShakeMainCamera(pushShakeDuration, pushShakeMagnitude);
                }

                pushHoldTimer = 0f;
            }
        }
        else
        {
            ResetPushCharge();
        }
    }

    private void ResetPushCharge()
    {
        chargingBox = null;
        chargingDirection = Vector2.zero;
        pushHoldTimer = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((boxLayer.value & (1 << other.gameObject.layer)) != 0 &&
            other.GetComponentInParent<PushBox>() != null)
        {
            pushContacts.Add(other);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        pushContacts.Remove(other);
    }

    private void OnDisable()
    {
        knockbackVelocity = Vector2.zero;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        RestoreJumpGroundCollisions();

        if (activeJumpState != null)
        {
            activeJumpState.transform.localPosition = jumpStateStartPosition;
        }

        isJumping = false;
        activeJumpState = null;
        UpdateJumpShadow(0f);
        pushContacts.Clear();
        nearbyBox = null;
        ResetPushCharge();
    }

    private void UpdateAnimation()
    {
        if (staffAbility != null && staffAbility.IsCasting)
        {
            ShowState(staffAbility.CastState);
            return;
        }
        if (isJumping)
        {
            ShowState(activeJumpState);
            return;
        }

        if (isPushing)
        {
            if (lastDirection.y > 0f)
                ShowState(upPush);
            else if (lastDirection.y < 0f)
                ShowState(downPush);
            else if (lastDirection.x < 0f)
                ShowState(leftPush);
            else
                ShowState(rightPush);

            return;
        }

        if (moveInput == Vector2.zero)
        {
            if (staffAbility != null && staffAbility.TryGetLocomotionState(
                lastDirection, false, out GameObject staffIdle))
            {
                ShowState(staffIdle);
                return;
            }
            if (lastDirection.y > 0f)
                ShowState(upIdle);
            else if (lastDirection.y < 0f)
                ShowState(downIdle);
            else if (lastDirection.x < 0f)
                ShowState(leftIdle);
            else
                ShowState(rightIdle);
        }
        else
        {
            if (staffAbility != null && staffAbility.TryGetLocomotionState(
                lastDirection, true, out GameObject staffWalk))
            {
                ShowState(staffWalk);
                return;
            }
            if (moveInput.y > 0f)
                ShowState(upMove);
            else if (moveInput.y < 0f)
                ShowState(downMove);
            else if (moveInput.x < 0f)
                ShowState(leftMove);
            else
                ShowState(rightMove);
        }
    }

    private void ShowState(GameObject target)
    {
        if (staffAbility != null)
            staffAbility.HideStatesExcept(target);
        GameObject[] states =
        {
            upIdle, downIdle, leftIdle, rightIdle,
            upMove, downMove, leftMove, rightMove,
            upPush, downPush, leftPush, rightPush,
            jumpForward, jumpBack, jumpLeft, jumpRight
        };

        foreach (GameObject state in states)
        {
            if (state != null)
            {
                state.SetActive(state == target);
            }
        }
    }

    private void StartJump()
    {
        Vector2 direction = moveInput != Vector2.zero ? moveInput : lastDirection;

        if (direction.y > 0f)
            activeJumpState = jumpBack;
        else if (direction.y < 0f)
            activeJumpState = jumpForward;
        else if (direction.x < 0f)
            activeJumpState = jumpLeft;
        else
            activeJumpState = jumpRight;

        if (activeJumpState == null)
        {
            return;
        }

        IgnoreJumpGroundCollisions();
        isJumping = true;
        jumpTimer = 0f;
        jumpStateStartPosition = activeJumpState.transform.localPosition;
        ShowState(activeJumpState);
    }

    private void UpdateJump()
    {
        const float duration = 0.8f;
        jumpTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(jumpTimer / duration);
        float heightRatio = Mathf.Sin(progress * Mathf.PI);
        float height = heightRatio * jumpHeight;
        UpdateJumpShadow(jumpHeight > 0f ? heightRatio : 0f);

        if (activeJumpState != null)
        {
            activeJumpState.transform.localPosition =
                jumpStateStartPosition + Vector3.up * height;
        }

        if (jumpTimer >= duration)
        {
            if (activeJumpState != null)
            {
                activeJumpState.transform.localPosition = jumpStateStartPosition;
            }

            bool invalidLanding = HasInvalidJumpLanding();
            isJumping = false;
            activeJumpState = null;
            RestoreJumpGroundCollisions();
            UpdateJumpShadow(0f);

            if (invalidLanding)
            {
                RestartJumpParkScene();
            }
        }
    }

    private bool HasInvalidJumpLanding()
    {
        if (!IsJumpParkScene())
        {
            return false;
        }

        Physics2D.SyncTransforms();
        Vector2 footPosition;
        if (groundCheck != null && groundCheck.enabled &&
            groundCheck.gameObject.activeInHierarchy)
        {
            footPosition = groundCheck.bounds.center;
        }
        else if (playerCollider != null)
        {
            Bounds bounds = playerCollider.bounds;
            footPosition = new Vector2(bounds.center.x, bounds.min.y);
        }
        else
        {
            footPosition = transform.position;
        }
        return !IsSafeJumpLandingAt(footPosition);
    }

    // These top-down levels use Ground Tilemaps WITHOUT physics colliders.
    // Test the occupied cell at the feet, not a downward platformer raycast.
    public bool IsSafeJumpLandingAt(Vector2 footPosition)
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
            return false;

        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>())
            {
                if (!tilemap.isActiveAndEnabled ||
                    tilemap.gameObject.layer != groundLayer)
                    continue;

                Vector3 position = new Vector3(footPosition.x, footPosition.y,
                    tilemap.transform.position.z);
                if (tilemap.HasTile(tilemap.WorldToCell(position)))
                    return true;
            }
        }

        // Also support ordinary Ground-layer platforms, but never Ground tags
        // on Default-layer boundary colliders.
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(1 << groundLayer);
        filter.useTriggers = false;
        landingColliders.Clear();
        Physics2D.OverlapPoint(footPosition, filter, landingColliders);
        foreach (Collider2D collider in landingColliders)
        {
            if (collider.gameObject.scene == gameObject.scene &&
                !collider.transform.IsChildOf(transform) &&
                !(collider is TilemapCollider2D) &&
                collider.GetComponent<Tilemap>() == null)
                return true;
        }
        return false;
    }

    private bool IsJumpParkScene()
    {
        string sceneName = gameObject.scene.name;
        return sceneName == "跳跳乐1" ||
            sceneName == "跳跳乐2" ||
            sceneName == "跳跳乐3" ||
            sceneName == "跳跳乐4";
    }

    public void RestartJumpParkScene()
    {
        EndlessSurvivalMode endless = GetComponent<EndlessSurvivalMode>();
        if (endless != null && endless.isActiveAndEnabled)
        {
            endless.FinishRun();
            return;
        }
        Scene scene = gameObject.scene;
        if (!string.IsNullOrEmpty(scene.path))
        {
#if UNITY_EDITOR
            if (!Application.CanStreamedLevelBeLoaded(scene.path))
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                    scene.path, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            SceneManager.LoadScene(scene.path);
        }
    }

    private void IgnoreJumpGroundCollisions()
    {
        if (playerCollider == null)
        {
            return;
        }

        ignoredJumpColliders.Clear();
        Collider2D[] colliders =
            FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude);

        foreach (Collider2D collider in colliders)
        {
            if (collider == null || collider == playerCollider ||
                collider.isTrigger ||
                collider.transform.IsChildOf(transform) ||
                !IsJumpGround(collider))
            {
                continue;
            }

            Physics2D.IgnoreCollision(playerCollider, collider, true);
            ignoredJumpColliders.Add(collider);
        }
    }

    private bool IsJumpGround(Collider2D collider)
    {
        Transform current = collider.transform;
        while (current != null)
        {
            bool matchesTag =
                !string.IsNullOrEmpty(jumpGroundTag) &&
                current.gameObject.tag == jumpGroundTag;

            if (matchesTag || IsJumpGroundLayer(current.gameObject))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool IsJumpGroundLayer(GameObject target)
    {
        Transform current = target.transform;
        while (current != null)
        {
            bool matchesConfiguredLayer =
                (jumpGroundLayers.value & (1 << current.gameObject.layer)) != 0;
            bool matchesGroundLayer =
                current.gameObject.layer == LayerMask.NameToLayer("Ground");

            if (matchesConfiguredLayer || matchesGroundLayer)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void RestoreJumpGroundCollisions()
    {
        if (playerCollider != null)
        {
            foreach (Collider2D collider in ignoredJumpColliders)
            {
                if (collider != null)
                {
                    Physics2D.IgnoreCollision(playerCollider, collider, false);
                }
            }
        }

        ignoredJumpColliders.Clear();
    }

    private void UpdateJumpShadow(float heightRatio)
    {
        if (jumpShadow == null)
        {
            return;
        }

        heightRatio = Mathf.Clamp01(heightRatio);
        jumpShadow.enabled = heightRatio > 0f;
        jumpShadow.transform.localPosition = new Vector3(
            jumpShadowOffset.x, jumpShadowOffset.y, 0f);
        Color shadowColor = jumpShadow.color;
        shadowColor.a = jumpShadowOpacity * heightRatio;
        jumpShadow.color = shadowColor;

        if (jumpShadow.sprite != null)
        {
            Vector2 spriteSize = jumpShadow.sprite.bounds.size;
            jumpShadow.transform.localScale = new Vector3(
                Mathf.Max(0f, jumpShadowMaxSize.x) * heightRatio / Mathf.Max(spriteSize.x, 0.0001f),
                Mathf.Max(0f, jumpShadowMaxSize.y) * heightRatio / Mathf.Max(spriteSize.y, 0.0001f),
                1f);
        }
    }

}
