using System;
using UnityEngine;

namespace FragmentRPG
{
    public enum InteractionType
    {
        NPCDialog,      // NPC / 과거의 기억 대사 출력
        ClueFragment,   // 기억조각(1, 2, 3) 아이템 수집
        PuzzleTrigger,  // 퍼즐 창 호출
        MapBlankSlot    // 배경의 비어진 퍼즐 빈칸 (기억조각 1, 2, 3을 끼워 넣어 맵을 완성하는 슬롯)
    }

    /// <summary>
    /// 씬 내 상호작용 가능한 오브젝트 컴포넌트.
    /// 기억조각(1,2,3) 수집, 배경의 비어진 퍼즐 칸 복원, 과거의 기억(NPC) 대사 출력을 담당합니다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class InteractableObject : MonoBehaviour
    {
        public static event Action<string, string, int, int> OnDialogPrinted;
        public static event Action<string, string, string> OnClueCollected;
        public static event Action<InteractableObject> OnBlankSlotInteracted;

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

        [Header("Puzzle Trigger Settings")]
        [SerializeField] private string puzzleId = "PUZ_MAP_RESTORE";
        [SerializeField] private PuzzleType puzzleType = PuzzleType.SpatialRestoration;

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

        public void SetupNPC(string id, string npcName, string dlgId, string[] lines)
        {
            objectId = id;
            displayName = npcName;
            dialogId = dlgId;
            fallbackDialogLines = lines;
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

        /// <summary>
        /// 알맞은 기억조각이 끼워졌을 때 비어진 배경 칸을 실제 일러스트 배경 타일로 복원합니다.
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
                }
                sr.color = filledBackgroundColor;
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
            }
        }

        private void PrintNPCDialog()
        {
            if (fallbackDialogLines == null || fallbackDialogLines.Length == 0)
            {
                OnDialogPrinted?.Invoke(displayName, "...", 1, 1);
                return;
            }

            string line = fallbackDialogLines[currentDialogIndex];
            int step = currentDialogIndex + 1;
            int total = fallbackDialogLines.Length;

            Debug.Log($"[과거의 기억 / 대사 | {displayName}] ({step}/{total}): \"{line}\"");
            OnDialogPrinted?.Invoke(displayName, line, step, total);

            currentDialogIndex = (currentDialogIndex + 1) % fallbackDialogLines.Length;
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
    }
}
