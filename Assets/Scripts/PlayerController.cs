using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FragmentRPG
{
    /// <summary>
    /// Top-down 2D 플레이어 이동, 충돌 처리 및 넓어진 스테이지 맵을 위한 부드러운 카메라 추적(Camera Follow) 컨트롤러.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("플레이어 이동 속도")]
        [SerializeField] private float moveSpeed = 5.8f;

        [Tooltip("대화나 퍼즐 진행 중 이동 잠금 여부")]
        [SerializeField] private bool isMovementLocked = false;

        [Header("Camera Follow Settings (넓어진 맵 카메라 추적)")]
        [SerializeField] private bool enableCameraFollow = true;
        [SerializeField] private float cameraSmoothSpeed = 7.5f;
        [SerializeField] private Vector2 cameraBoundsMin = new Vector2(-2.2f, -1.8f);
        [SerializeField] private Vector2 cameraBoundsMax = new Vector2(4.4f, 1.8f);

        private Rigidbody2D rb;
        private SpriteRenderer sr;
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
            sr = GetComponent<SpriteRenderer>();
            ConfigureRigidbody();
        }

        private void Reset()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
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

        /// <summary>
        /// 현재 스테이지 맵 크기에 맞춰 카메라 이동 제한 영역을 설정합니다.
        /// </summary>
        public void SetCameraBounds(Vector2 minBounds, Vector2 maxBounds, bool snapImmediately = true)
        {
            cameraBoundsMin = minBounds;
            cameraBoundsMax = maxBounds;

            if (snapImmediately && Camera.main != null)
            {
                Vector3 targetPos = GetClampedCameraPosition(transform.position);
                Camera.main.transform.position = targetPos;
            }
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

                if (sr != null && Mathf.Abs(moveInput.x) > 0.05f)
                {
                    sr.flipX = moveInput.x < 0f;
                }
            }
        }

        private void LateUpdate()
        {
            if (!enableCameraFollow || Camera.main == null) return;

            Vector3 desiredPos = GetClampedCameraPosition(transform.position);
            Camera.main.transform.position = Vector3.Lerp(
                Camera.main.transform.position,
                desiredPos,
                Time.deltaTime * cameraSmoothSpeed
            );
        }

        private Vector3 GetClampedCameraPosition(Vector3 targetWorldPos)
        {
            // 좌측 UI 패널(화면 왼쪽 약 20%)을 고려해 카메라 중심을 살짝 왼쪽으로 오프셋하여 플레이어가 우측 게임 화면 중앙에 오도록 보정
            float desiredX = Mathf.Clamp(targetWorldPos.x - 0.9f, cameraBoundsMin.x, cameraBoundsMax.x);
            float desiredY = Mathf.Clamp(targetWorldPos.y, cameraBoundsMin.y, cameraBoundsMax.y);
            return new Vector3(desiredX, desiredY, -10f);
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
