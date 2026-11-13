using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FragmentRPG
{
    /// <summary>
    /// '기억조각 1, 2, 3 수집 -> 배경의 비어진 부분에 알맞게 끼워 넣기 -> 하나의 맵 완성 -> 과거의 기억 보기'
    /// 퍼즐맵 전용 인게임 UI 및 연출 매니저.
    /// </summary>
    public class FragmentUIManager : MonoBehaviour
    {
        public static FragmentUIManager Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private Text headerTitleText;
        [SerializeField] private Text inventoryStatusText;
        [SerializeField] private Text promptText;
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private Text dialogSpeakerText;
        [SerializeField] private Text dialogBodyText;
        [SerializeField] private GameObject slotChoicePanel;
        [SerializeField] private Text slotChoiceTitleText;
        [SerializeField] private Text slotChoiceDescText;
        [SerializeField] private Text slotChoiceResultText;
        [SerializeField] private Transform slotChoiceButtonContainer;

        [Header("Map & Past Memory Objects")]
        [SerializeField] private GameObject pastMemoryRoot;
        [SerializeField] private SpriteRenderer mainFloorRenderer;

        private InteractableObject activeBlankSlot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            InteractableObject.OnDialogPrinted += HandleDialogPrinted;
            InteractableObject.OnClueCollected += HandleClueCollected;
            InteractableObject.OnBlankSlotInteracted += HandleBlankSlotInteracted;
        }

        private void OnDisable()
        {
            InteractableObject.OnDialogPrinted -= HandleDialogPrinted;
            InteractableObject.OnClueCollected -= HandleClueCollected;
            InteractableObject.OnBlankSlotInteracted -= HandleBlankSlotInteracted;
        }

        private void Start()
        {
            if (InteractionManager.Instance != null)
            {
                InteractionManager.Instance.OnTargetChanged += HandleTargetChanged;
            }

            if (PuzzleManager.Instance != null)
            {
                PuzzleManager.Instance.OnSlotPlacementAttempted += HandleSlotPlacementAttempted;
                PuzzleManager.Instance.OnMemoryMapCompleted += HandleMemoryMapCompleted;
            }

            // 시작 시 세이브 초기화하여 퍼즐맵을 처음부터 깔끔하게 테스트할 수 있도록 설정
            if (StorySaveManager.Instance != null)
            {
                StorySaveManager.Instance.ResetSaveData();
            }

            if (pastMemoryRoot != null)
            {
                pastMemoryRoot.SetActive(false);
            }

            RefreshHUD();
            HandleTargetChanged(InteractionManager.Instance != null ? InteractionManager.Instance.CurrentTarget : null);
        }

        public void BindPuzzleMapReferences(
            Text headerTxt,
            Text invTxt,
            Text promptTxt,
            GameObject dlgPanel,
            Text dlgSpeaker,
            Text dlgBody,
            GameObject choicePanel,
            Text choiceTitle,
            Text choiceDesc,
            Text choiceResult,
            Transform choiceBtnContainer,
            GameObject pastMemoryObj,
            SpriteRenderer floorSr)
        {
            headerTitleText = headerTxt;
            inventoryStatusText = invTxt;
            promptText = promptTxt;
            dialogPanel = dlgPanel;
            dialogSpeakerText = dlgSpeaker;
            dialogBodyText = dlgBody;
            slotChoicePanel = choicePanel;
            slotChoiceTitleText = choiceTitle;
            slotChoiceDescText = choiceDesc;
            slotChoiceResultText = choiceResult;
            slotChoiceButtonContainer = choiceBtnContainer;
            pastMemoryRoot = pastMemoryObj;
            mainFloorRenderer = floorSr;
        }

        /// <summary>
        /// 퍼즐맵 전체 상태를 처음 상태로 리셋합니다.
        /// </summary>
        public void ResetPuzzleMap()
        {
            if (StorySaveManager.Instance != null)
            {
                StorySaveManager.Instance.ResetSaveData();
            }

            if (PuzzleManager.Instance != null)
            {
                PuzzleManager.Instance.ResetMapPuzzleState();
            }

            InteractableObject[] allObjs = FindObjectsByType<InteractableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (InteractableObject obj in allObjs)
            {
                if (obj.Type == InteractionType.ClueFragment)
                {
                    obj.ResetClueState();
                }
                else if (obj.Type == InteractionType.MapBlankSlot)
                {
                    obj.ResetBlankSlotState(Color.white, $"[{obj.DisplayName}]");
                }
            }

            if (pastMemoryRoot != null)
            {
                pastMemoryRoot.SetActive(false);
            }

            if (mainFloorRenderer != null)
            {
                mainFloorRenderer.color = Color.white;
            }

            if (dialogPanel != null) dialogPanel.SetActive(false);
            if (slotChoicePanel != null) slotChoicePanel.SetActive(false);

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                player.IsMovementLocked = false;
                player.transform.position = new Vector3(0f, -2.5f, 0f);
            }

            RefreshHUD();
        }

        public void RefreshHUD()
        {
            int filled = PuzzleManager.Instance != null ? PuzzleManager.Instance.FilledBlankSlots : 0;
            int total = PuzzleManager.Instance != null ? PuzzleManager.Instance.TotalBlankSlots : 3;
            bool isCompleted = PuzzleManager.Instance != null && PuzzleManager.Instance.IsMemoryMapCompleted;

            if (headerTitleText != null)
            {
                headerTitleText.text = isCompleted
                    ? "★ [맵 완성 완료] 복원된 맵 위쪽으로 걸어가 '과거의 기억'을 확인하세요! (이동: WASD/방향키, 상호작용: E/Space)"
                    : $"[Fragment RPG 퍼즐맵] 기억조각 1, 2, 3을 모아 비어진 배경({filled}/{total})에 알맞게 끼워 넣으세요!";
            }

            if (inventoryStatusText != null)
            {
                List<string> clues = StorySaveManager.Instance != null
                    ? StorySaveManager.Instance.CurrentSaveData.collectedClueIds
                    : new List<string>();

                bool has1 = clues.Contains("FRAG_01");
                bool has2 = clues.Contains("FRAG_02");
                bool has3 = clues.Contains("FRAG_03");

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("<b>[보유 아이템 — 기억조각]</b>");
                sb.AppendLine(has1 ? "<color=#FFE066>■ 기억조각 1 (보유중)</color>\n   └ 왼쪽 가로등길 배경 조각" : "<color=#778899>□ 기억조각 1 (미획득)</color>");
                sb.AppendLine(has2 ? "<color=#66FF99>■ 기억조각 2 (보유중)</color>\n   └ 중앙 시계탑 광장 조각" : "<color=#778899>□ 기억조각 2 (미획득)</color>");
                sb.AppendLine(has3 ? "<color=#FF88EE>■ 기억조각 3 (보유중)</color>\n   └ 오른쪽 벚꽃 벤치 조각" : "<color=#778899>□ 기억조각 3 (미획득)</color>");
                sb.AppendLine();
                sb.AppendLine($"<b>[배경 복원 진행도: {filled} / {total}]</b>");
                sb.AppendLine(isCompleted
                    ? "<color=#55FF88>★ 하나의 맵 완성!\n★ 과거의 기억 활성화됨!</color>"
                    : "• 비어진 배경 칸 앞에서\n  [E] 또는 [Space] 키를 눌러\n  알맞은 기억조각을 넣으세요.");

                inventoryStatusText.text = sb.ToString();
            }
        }

        private void HandleTargetChanged(InteractableObject target)
        {
            if (promptText == null) return;

            if (target == null)
            {
                promptText.text = "맵 아래쪽에서 [기억조각 1, 2, 3]을 줍고, 가운데 [비어진 배경 1, 2, 3]에 다가가 [E] 또는 [Space]를 누르세요.";
                promptText.color = new Color(0.85f, 0.9f, 1f, 1f);
                return;
            }

            if (target.Type == InteractionType.ClueFragment)
            {
                promptText.text = $"▶ [E / Space] '{target.DisplayName}' 줍기";
                promptText.color = new Color(1f, 0.92f, 0.35f, 1f);
            }
            else if (target.Type == InteractionType.MapBlankSlot)
            {
                promptText.text = $"▶ [E / Space] '{target.DisplayName}'에 기억조각 끼워 넣기";
                promptText.color = new Color(0.45f, 0.95f, 1f, 1f);
            }
            else if (target.Type == InteractionType.NPCDialog)
            {
                promptText.text = $"▶ [E / Space] 과거의 기억 보기 — '{target.DisplayName}'";
                promptText.color = new Color(0.65f, 1f, 0.75f, 1f);
            }
        }

        private void HandleClueCollected(string clueId, string title, string description)
        {
            if (dialogPanel != null) dialogPanel.SetActive(true);
            if (dialogSpeakerText != null) dialogSpeakerText.text = $"아이템 획득: {title}";
            if (dialogBodyText != null) dialogBodyText.text = $"{description}\n(이제 비어진 배경 칸으로 가서 이 조각을 끼워 넣을 수 있습니다!)";
            RefreshHUD();
        }

        private void HandleDialogPrinted(string speaker, string line, int step, int total)
        {
            if (dialogPanel != null) dialogPanel.SetActive(true);
            if (dialogSpeakerText != null) dialogSpeakerText.text = $"[과거의 기억] {speaker} ({step}/{total})";
            if (dialogBodyText != null) dialogBodyText.text = line;
        }

        /// <summary>
        /// 플레이어가 비어진 배경 칸(1, 2, 3)과 상호작용했을 때 조각 선택 팝업을 엽니다.
        /// </summary>
        private void HandleBlankSlotInteracted(InteractableObject blankSlot)
        {
            if (blankSlot == null || blankSlot.IsSlotFilled) return;

            activeBlankSlot = blankSlot;
            if (slotChoicePanel != null) slotChoicePanel.SetActive(true);

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.IsMovementLocked = true;

            if (slotChoiceTitleText != null)
            {
                slotChoiceTitleText.text = $"[{blankSlot.DisplayName}] — 어떤 기억조각을 끼워 넣을까요?";
            }
            if (slotChoiceDescText != null)
            {
                slotChoiceDescText.text = "보유 중인 기억조각(1, 2, 3) 중 이 빈칸의 배경에 알맞은 조각을 선택하세요.";
            }
            if (slotChoiceResultText != null)
            {
                slotChoiceResultText.text = "조각 버튼을 클릭해 배경에 맞춰 보세요.";
            }

            RebuildSlotChoiceButtons();
        }

        private void RebuildSlotChoiceButtons()
        {
            if (slotChoiceButtonContainer == null) return;
            for (int i = slotChoiceButtonContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(slotChoiceButtonContainer.GetChild(i).gameObject);
            }

            List<string> collected = StorySaveManager.Instance != null
                ? StorySaveManager.Instance.CurrentSaveData.collectedClueIds
                : new List<string>();

            CreateFragmentOptionButton("FRAG_01", "기억조각 1 (왼쪽 가로등길 배경)", collected.Contains("FRAG_01"));
            CreateFragmentOptionButton("FRAG_02", "기억조각 2 (중앙 시계탑 광장 배경)", collected.Contains("FRAG_02"));
            CreateFragmentOptionButton("FRAG_03", "기억조각 3 (오른쪽 벚꽃 벤치 배경)", collected.Contains("FRAG_03"));

            CreateActionButton(slotChoiceButtonContainer, "✕ 닫기", () =>
            {
                CloseSlotChoicePanel();
            }, new Color(0.45f, 0.25f, 0.25f, 1f));
        }

        private void CreateFragmentOptionButton(string fragId, string label, bool isCollected)
        {
            if (!isCollected)
            {
                CreateActionButton(slotChoiceButtonContainer, $"[미보유] {label} — 먼저 맵에서 아이템을 주우세요", () =>
                {
                    if (slotChoiceResultText != null)
                    {
                        slotChoiceResultText.text = $"<color=#FF8888>아직 '{label}' 아이템을 줍지 않았습니다!</color>";
                    }
                }, new Color(0.22f, 0.24f, 0.28f, 0.85f));
                return;
            }

            CreateActionButton(slotChoiceButtonContainer, $"▶ {label} 끼워 넣기", () =>
            {
                if (PuzzleManager.Instance != null && activeBlankSlot != null)
                {
                    PuzzleManager.Instance.TryPlaceFragmentIntoBlankSlot(activeBlankSlot, fragId);
                }
            }, new Color(0.20f, 0.42f, 0.62f, 0.95f));
        }

        private void HandleSlotPlacementAttempted(InteractableObject slot, string triedFragId, bool success)
        {
            if (success)
            {
                CloseSlotChoicePanel();
                if (dialogPanel != null) dialogPanel.SetActive(true);
                if (dialogSpeakerText != null) dialogSpeakerText.text = $"배경 퍼즐 복원 성공! ({slot.DisplayName})";
                if (dialogBodyText != null)
                {
                    int filled = PuzzleManager.Instance != null ? PuzzleManager.Instance.FilledBlankSlots : 0;
                    int total = PuzzleManager.Instance != null ? PuzzleManager.Instance.TotalBlankSlots : 3;
                    dialogBodyText.text = $"'{triedFragId}' 조각이 비어진 배경에 딱 맞아떨어지며 공간이 복원되었습니다! (진행도: {filled}/{total})";
                }
                RefreshHUD();
            }
            else
            {
                if (slotChoiceResultText != null)
                {
                    slotChoiceResultText.text = $"<color=#FF6666>✕ '{triedFragId}' 조각은 이 배경 빈칸({slot.DisplayName})의 모양과 맞지 않습니다! 다른 기억조각을 넣어보세요.</color>";
                }
            }
        }

        /// <summary>
        /// 기억조각 1, 2, 3이 모두 알맞게 들어가 하나의 맵이 완성되었을 때 호출됩니다.
        /// 과거의 기억 오브젝트들을 활성화하고 연출 대사를 출력합니다.
        /// </summary>
        private void HandleMemoryMapCompleted()
        {
            if (pastMemoryRoot != null)
            {
                pastMemoryRoot.SetActive(true);
            }

            if (mainFloorRenderer != null)
            {
                mainFloorRenderer.color = new Color(1.0f, 0.98f, 0.90f, 1f);
            }

            if (dialogPanel != null) dialogPanel.SetActive(true);
            if (dialogSpeakerText != null)
            {
                dialogSpeakerText.text = "★ 하나의 맵 완성 — 과거의 기억이 나타납니다!";
            }
            if (dialogBodyText != null)
            {
                dialogBodyText.text = "기억조각 1, 2, 3이 모두 맞춰져 끊어졌던 배경이 하나의 온전한 맵으로 연결되었습니다!\n이제 복원된 배경 위로 걸어 올라가 나타난 '과거의 기억'을 확인해 보세요!";
            }

            RefreshHUD();
        }

        private void CloseSlotChoicePanel()
        {
            if (slotChoicePanel != null) slotChoicePanel.SetActive(false);
            activeBlankSlot = null;

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.IsMovementLocked = false;
        }

        private void CreateActionButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick, Color bgColor)
        {
            GameObject btnObj = new GameObject("Btn_" + text);
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 40f;
            le.flexibleWidth = 1f;

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform rt = txtObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(10f, 2f);
            rt.offsetMax = new Vector2(-10f, -2f);

            Text txt = txtObj.AddComponent<Text>();
            txt.font = headerTitleText != null && headerTitleText.font != null
                ? headerTitleText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = text;
            txt.fontSize = 15;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }
    }
}
