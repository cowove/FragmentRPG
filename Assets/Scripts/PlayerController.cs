using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FragmentRPG
{
    /// <summary>
    /// Top-down 2D 플레이어 이동 및 충돌 처리 컨트롤러.
    /// Rigidbody2D(gravityScale = 0) 기반으로 WASD / 방향키 이동을 수행합니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("플레이어 이동 속도")]
        [SerializeField] private float moveSpeed = 5.0f;

        [Tooltip("대화나 퍼즐 진행 중 이동 잠금 여부")]
        [SerializeField] private bool isMovementLocked = false;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private Vector2 lastLookDirection = Vector2.down;

        public Vector2 LastLookDirection => lastLookDirection;
        public bool IsMovementLocked
        {
            get => isMovementLocked;
            set
            {
                isMovementLocked = value;
                if (isMovementLocked && rb != null)
                {
                    moveInput = Vector2.zero;
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            ConfigureRigidbody();
        }

        private void Reset()
        {
            rb = GetComponent<Rigidbody2D>();
            ConfigureRigidbody();
        }

        private void ConfigureRigidbody()
        {
            if (rb == null) return;

            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void Update()
        {
            if (isMovementLocked)
            {
                moveInput = Vector2.zero;
                return;
            }

            Vector2 rawInput = ReadMovementInput();
            moveInput = rawInput.normalized;

            if (moveInput.sqrMagnitude > 0.01f)
            {
                lastLookDirection = moveInput;
            }
        }

        private Vector2 ReadMovementInput()
        {
            float horizontal = 0f;
            float vertical = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;
            }
            return new Vector2(horizontal, vertical);
#elif ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                horizontal = Input.GetAxisRaw("Horizontal");
                vertical = Input.GetAxisRaw("Vertical");
            }
            catch
            {
                // Ignore if Legacy Input Manager is inactive in current Editor session
            }
            return new Vector2(horizontal, vertical);
#else
            return Vector2.zero;
#endif
        }

        private void FixedUpdate()
        {
            if (isMovementLocked)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            rb.linearVelocity = moveInput * moveSpeed;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Debug.Log($"[PlayerController] 충돌 감지: {collision.gameObject.name}");
        }
    }
}
