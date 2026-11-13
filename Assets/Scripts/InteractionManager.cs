using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FragmentRPG
{
    /// <summary>
    /// 플레이어 주변 반경 내의 InteractableObject를 탐색하고
    /// E 키 또는 Space 키 입력으로 상호작용을 트리거하는 매니저.
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        [Header("Detection Settings")]
        [Tooltip("탐색 기준이 될 플레이어 Transform (비워두면 PlayerController를 자동 탐색)")]
        [SerializeField] private Transform playerTransform;

        [Tooltip("상호작용 감지 반경")]
        [SerializeField] private float detectRadius = 1.8f;

        [Tooltip("상호작용 대상 레이어 마스크 (기본: Everything)")]
        [SerializeField] private LayerMask interactableLayer = ~0;

        [Header("Current Target State")]
        [SerializeField] private InteractableObject currentTarget;

        public InteractableObject CurrentTarget => currentTarget;
        public event Action<InteractableObject> OnTargetChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }
        }

        private void Update()
        {
            DetectNearestInteractable();

            if (currentTarget != null && IsInteractKeyPressed())
            {
                currentTarget.Interact();
            }
        }

        private bool IsInteractKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                return Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// 플레이어 반경 내 가장 가까운 InteractableObject를 감지합니다.
        /// </summary>
        private void DetectNearestInteractable()
        {
            Vector2 center = playerTransform != null ? (Vector2)playerTransform.position : (Vector2)transform.position;
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, detectRadius, interactableLayer);

            InteractableObject nearest = null;
            float minDistanceSq = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                if (hit == null) continue;

                InteractableObject interactable = hit.GetComponent<InteractableObject>();
                if (interactable == null || !interactable.CanInteract) continue;

                float distSq = ((Vector2)interactable.transform.position - center).sqrMagnitude;
                if (distSq < minDistanceSq)
                {
                    minDistanceSq = distSq;
                    nearest = interactable;
                }
            }

            if (currentTarget != nearest)
            {
                if (currentTarget != null)
                {
                    currentTarget.OnFocusLost();
                }

                currentTarget = nearest;

                if (currentTarget != null)
                {
                    currentTarget.OnFocusGained();
                }

                OnTargetChanged?.Invoke(currentTarget);
            }
        }

        public void SetPlayerTransform(Transform target)
        {
            playerTransform = target;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = playerTransform != null ? playerTransform.position : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center, detectRadius);
        }
    }
}
