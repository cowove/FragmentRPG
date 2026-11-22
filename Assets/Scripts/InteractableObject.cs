using System;
using UnityEngine;

namespace FragmentRPG
{
    public enum InteractionType
    {
        NPCDialog,      // NPC / 과거의 기억 대사 출력
        ClueFragment,   // 기억조각(1, 2, 3) 아이템 수집
        PuzzleTrigger,  // 최종 기억 수리 제단 (순서 맞추기 -> 잘못된 기억 찾기 연속 퍼즐 호출)
        MapBlankSlot,   // 배경의 비어진 퍼즐 빈칸 (기억조각 1, 2, 3을 끼워 넣어 맵을 완성하는 슬롯)
        SpacePortal     // 거점(기억 수리점) <-> 기억 공간(퍼즐맵) 이동 포털
    }

    /// <summary>
    /// 씬 내 상호작용 가능한 오브젝트 컴포넌트.
    /// NPC 대사, 기억조각 수집, 배경 빈칸 복원, 최종 퍼즐 호출, 공간 이동 포털을 담당합니다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class InteractableObject : MonoBehaviour
    {
        public static event Action<string, string, int, int> OnDialogPrinted;
        public static event Action<string, string, string> OnClueCollected;
        public static event Action<InteractableObject> OnBlankSlotInteracted;
        public static event Action<string> OnPortalTriggered;

        [Header("Interactable Info")]
        [SerializeField] private string objectId = "OBJ_001";
        [SerializeField] private string displayName = "기억 오브젝트";
        [SerializeField] private InteractionType interactionType = InteractionType.ClueFragment;
        [SerializeField] private bool canInteract = true;

        [Header("NPC / Memory Dialog Settings")]
        [SerializeField] private string dialogId = "DLG_MEM_01";
        [TextArea(2, 5)]
        [SerializeField] private string[] fallbackDialogLines = new string[]
        {
            "이곳에는 흐릿한 기억의 잔상이 남아 있다."
        };
        [TextArea(2, 5)]
        [SerializeField] private string[] clearedDialogLines = new string[0];
        private int currentDialogIndex = 0;

        [Header("Clue Fragment Settings (기억조각 1, 2, 3)")]
        [SerializeField] private string clueId = "FRAG_01";
        [SerializeField] private string clueTitle = "기억조각 1";
        [TextArea(2, 4)]
        [SerializeField] private string clueDescription = "비어진 배경의 왼쪽 공간을 채울 수 있는 첫 번째 기억조각.";
        [SerializeField] private bool deactivateOnCollect = true;

        [Header("Map Blank Slot Settings (배경의 비어진 퍼즐 칸)")]
        [SerializeField] private string slotId = "SLOT_BG_01";
        [SerializeField] private string requiredClueId = "FRAG_01";
        [SerializeField] private bool isSlotFilled = false;
        [SerializeField] private Sprite emptySlotSprite;
        [SerializeField] private Sprite restoredSlotSprite;
        [SerializeField] private Color filledBackgroundColor = Color.white;
        [SerializeField] private GameObject restoredVisualChild;

        [Header("Puzzle Trigger / Portal Settings")]
        [SerializeField] private string puzzleId = "PUZ_EP1_FINAL";
        [SerializeField] private PuzzleType puzzleType = PuzzleType.ChronologicalOrder;
        [SerializeField] private string targetSpaceId = "SPACE_MEMORY_MAP";

        public string ObjectId => objectId;
        public string DisplayName => displayName;
        public InteractionType Type => interactionType;
        public string ClueId => clueId;
        public string ClueTitle => clueTitle;
        public string ClueDescription => clueDescription;
        public string SlotId => slotId;
        public string RequiredClueId => requiredClueId;
        public bool IsSlotFilled => isSlotFilled;
        public string PuzzleId => puzzleId;
        public PuzzleType PuzzleKind => puzzleType;
        public string TargetSpaceId => targetSpaceId;
        public bool CanInteract => canInteract;

        public void SetupClue(string id, string title, string description)
        {
            objectId = id;
            clueId = id;
            displayName = title;
            clueTitle = title;
            clueDescription = description;
            interactionType = InteractionType.ClueFragment;
            canInteract = true;
        }

        public void SetupMapBlankSlot(
            string sId,
            string slotDisplayName,
            string reqClueId,
            Sprite emptySprite,
            Sprite restoredSprite,
            Color restoredColor,
            GameObject restoredVisual)
        {
            objectId = sId;
            slotId = sId;
            displayName = slotDisplayName;
            requiredClueId = reqClueId;
            emptySlotSprite = emptySprite;
            restoredSlotSprite = restoredSprite;
            filledBackgroundColor = restoredColor;
            restoredVisualChild = restoredVisual;
            isSlotFilled = false;
            interactionType = InteractionType.MapBlankSlot;
            canInteract = true;

            if (restoredVisualChild != null)
            {
                restoredVisualChild.SetActive(false);
            }
        }

        public void SetupNPC(string id, string npcName, string dlgId, string[] lines, string[] afterClearLines = null)
        {
            objectId = id;
            displayName = npcName;
            dialogId = dlgId;
            fallbackDialogLines = lines;
            clearedDialogLines = afterClearLines ?? new string[0];
            interactionType = InteractionType.NPCDialog;
            canInteract = true;
        }

        public void SetupPuzzleTrigger(string id, string triggerName, string targetPuzzleId, PuzzleType targetType)
        {
            objectId = id;
            displayName = triggerName;
            puzzleId = targetPuzzleId;
            puzzleType = targetType;
            interactionType = InteractionType.PuzzleTrigger;
            canInteract = true;
        }

        public void SetupSpacePortal(string id, string portalName, string destinationSpaceId)
        {
            objectId = id;
            displayName = portalName;
            targetSpaceId = destinationSpaceId;
            interactionType = InteractionType.SpacePortal;
            canInteract = true;
        }

        /// <summary>
        /// 알맞은 기억조각이 끼워졌을 때 비어진 구멍(가림막)을 제거하여 실제 맵 배경이 100% 자연스럽게 드러나게 합니다.
        /// </summary>
        public void FillBlankSlot()
        {
            isSlotFilled = true;
            canInteract = false;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (restoredSlotSprite != null)
                {
                    sr.sprite = restoredSlotSprite;
                    sr.color = filledBackgroundColor;
                }
                else
                {
                    // 가림막을 투명하게 꺼서 아래의 실제 고퀄리티 맵 배경이 이음새 없이 완벽히 드러나도록 처리
                    sr.enabled = false;
                }
            }

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            if (restoredVisualChild != null)
            {
                restoredVisualChild.SetActive(true);
            }

            Transform labelTr = transform.Find("Label");
            if (labelTr != null)
            {
                TextMesh tm = labelTr.GetComponent<TextMesh>();
                if (tm != null)
                {
                    tm.text = "";
                }
            }
        }

        public void ResetClueState()
        {
            canInteract = true;
            gameObject.SetActive(true);
        }

        public void ResetBlankSlotState(Color emptyColor, string emptyLabel)
        {
            isSlotFilled = false;
            canInteract = true;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = true;
                if (emptySlotSprite != null)
                {
                    sr.sprite = emptySlotSprite;
                }
                sr.color = Color.white;
            }

            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = false;
            }

            if (restoredVisualChild != null)
            {
                restoredVisualChild.SetActive(false);
            }

            Transform labelTr = transform.Find("Label");
            if (labelTr != null)
            {
                TextMesh tm = labelTr.GetComponent<TextMesh>();
                if (tm != null)
                {
                    tm.text = emptyLabel;
                    tm.color = new Color(1f, 0.78f, 0.78f, 0.95f);
                }
            }
        }

        public void OnFocusGained()
        {
            Debug.Log($"[Interactable] '{displayName}' 감지됨 — [E] 또는 [Space] 키를 눌러 상호작용");
        }

        public void OnFocusLost()
        {
            currentDialogIndex = 0;
        }

        public void Interact()
        {
            if (!canInteract) return;

            switch (interactionType)
            {
                case InteractionType.NPCDialog:
                    PrintNPCDialog();
                    break;

                case InteractionType.ClueFragment:
                    CollectClueFragment();
                    break;

                case InteractionType.MapBlankSlot:
                    InteractWithBlankSlot();
                    break;

                case InteractionType.PuzzleTrigger:
                    OpenPuzzleWindow();
                    break;

                case InteractionType.SpacePortal:
                    TriggerSpacePortal();
                    break;
            }
        }

        private void PrintNPCDialog()
        {
            bool isEpisodeCleared = PuzzleManager.Instance != null && PuzzleManager.Instance.IsEpisodeFullyRepaired;
            string[] activeLines = (isEpisodeCleared && clearedDialogLines != null && clearedDialogLines.Length > 0)
                ? clearedDialogLines
                : fallbackDialogLines;

            if (activeLines == null || activeLines.Length == 0)
            {
                OnDialogPrinted?.Invoke(displayName, "...", 1, 1);
                return;
            }

            if (currentDialogIndex >= activeLines.Length)
            {
                currentDialogIndex = 0;
            }

            string line = activeLines[currentDialogIndex];
            int step = currentDialogIndex + 1;
            int total = activeLines.Length;

            Debug.Log($"[대사 | {displayName}] ({step}/{total}): \"{line}\"");
            OnDialogPrinted?.Invoke(displayName, line, step, total);

            currentDialogIndex = (currentDialogIndex + 1) % activeLines.Length;
        }

        private void CollectClueFragment()
        {
            Debug.Log($"[기억조각 획득] '{clueTitle}' ({clueId}) 수집 완료! — {clueDescription}");

            if (StorySaveManager.Instance != null)
            {
                StorySaveManager.Instance.AddCollectedClue(clueId);
                StorySaveManager.Instance.SaveGame();
            }

            OnClueCollected?.Invoke(clueId, clueTitle, clueDescription);
            canInteract = false;

            if (deactivateOnCollect)
            {
                gameObject.SetActive(false);
            }
        }

        private void InteractWithBlankSlot()
        {
            if (isSlotFilled) return;
            Debug.Log($"[배경 빈칸 상호작용] '{displayName}' ({slotId}) — 필요한 조각: {requiredClueId}");
            OnBlankSlotInteracted?.Invoke(this);
        }

        private void OpenPuzzleWindow()
        {
            if (PuzzleManager.Instance != null)
            {
                PuzzleManager.Instance.OpenPuzzle(puzzleId, puzzleType);
            }
        }

        private void TriggerSpacePortal()
        {
            Debug.Log($"[공간 이동 포털] '{displayName}' -> 목표 공간: {targetSpaceId}");
            OnPortalTriggered?.Invoke(targetSpaceId);
        }
    }
}
