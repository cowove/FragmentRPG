using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FragmentRPG
{
    /// <summary>
    /// [2순위] 거점 '기억 수리점' <-> '시계공의 기억 공간(퍼즐맵)' 공간 이동 루프 및
    /// [1순위] 배경 복원(③) -> 과거의 기억 관람 -> 기억 순서 맞추기(①) -> 잘못된 기억 찾기(②)
    /// 전체 스토리·퍼즐 흐름을 통합 관리하는 UI 매니저.
    /// </summary>
    public class FragmentUIManager : MonoBehaviour
    {
        public static FragmentUIManager Instance { get; private set; }

        public const string SpaceRepairShop = "SPACE_REPAIR_SHOP";
        public const string SpaceMemoryMap = "SPACE_MEMORY_MAP";

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

        [Header("Space & Scene Roots")]
        [SerializeField] private GameObject repairShopRoot;
        [SerializeField] private GameObject memoryMapRoot;
        [SerializeField] private GameObject pastMemoryRoot;
        [SerializeField] private SpriteRenderer mainFloorRenderer;

        private string currentSpaceId = SpaceRepairShop;
        private InteractableObject activeBlankSlot;

        // 1순위 최종 수리 퍼즐(① 순서 맞추기 & ② 모순 찾기) 진행용 상태
        private readonly List<string> submittedOrderList = new List<string>();
        private string selectedStatementId = "";
        private string selectedEvidenceId = "";

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
            InteractableObject.OnPortalTriggered += HandlePortalTriggered;
        }

        private void OnDisable()
        {
            InteractableObject.OnDialogPrinted -= HandleDialogPrinted;
            InteractableObject.OnClueCollected -= HandleClueCollected;
            InteractableObject.OnBlankSlotInteracted -= HandleBlankSlotInteracted;
            InteractableObject.OnPortalTriggered -= HandlePortalTriggered;
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
                PuzzleManager.Instance.OnPuzzleOpened += HandleFinalPuzzleOpened;
                PuzzleManager.Instance.OnEpisodeFullyRepaired += HandleEpisodeFullyRepaired;
            }

            if (StorySaveManager.Instance != null)
            {
                StorySaveManager.Instance.ResetSaveData();
            }

            if (pastMemoryRoot != null)
            {
                pastMemoryRoot.SetActive(false);
            }

            // 시작 시 주인공의 거점인 '기억 수리점 공방'에서 시작!
            SwitchSpace(SpaceRepairShop);
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
            GameObject shopObj,
            GameObject mapObj,
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
            repairShopRoot = shopObj;
            memoryMapRoot = mapObj;
            pastMemoryRoot = pastMemoryObj;
            mainFloorRenderer = floorSr;
        }

        /// <summary>
        /// 거점 '기억 수리점(SPACE_REPAIR_SHOP)'과 '시계공의 기억 공간(SPACE_MEMORY_MAP)' 사이를 전환합니다.
        /// </summary>
        public void SwitchSpace(string targetSpace)
        {
            currentSpaceId = targetSpace;
            bool inShop = string.Equals(currentSpaceId, SpaceRepairShop, StringComparison.Ordinal);

            if (repairShopRoot != null) repairShopRoot.SetActive(inShop);
            if (memoryMapRoot != null) memoryMapRoot.SetActive(!inShop);

            if (slotChoicePanel != null) slotChoicePanel.SetActive(false);

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                player.IsMovementLocked = false;
                player.transform.position = inShop ? new Vector3(1.1f, -1.8f, 0f) : new Vector3(1.1f, -2.0f, 0f);
            }

            if (dialogPanel != null)
            {
                dialogPanel.SetActive(true);
                if (inShop)
                {
                    bool cleared = PuzzleManager.Instance != null && PuzzleManager.Instance.IsEpisodeFullyRepaired;
                    dialogSpeakerText.text = cleared
                        ? "★ [기억 수리점 공방] 의뢰 완료!"
                        : "[현재 위치: 기억 수리점 공방]";
                    dialogBodyText.text = cleared
                        ? "시계공의 기억을 모두 수리하고 공방으로 돌아왔습니다! 왼쪽의 [시계공 노인]에게 말을 걸어 에필로그 대화를 확인하세요."
                        : "왼쪽의 [시계공 노인]과 대화해 의뢰 내용을 들은 뒤, 중앙 작업대의 [멈춘 회중시계]에서 [E / Space]를 눌러 기억 공간으로 입장하세요!";
                }
                else
                {
                    dialogSpeakerText.text = "[기억 다이브 완료 — 시계공의 기억 공간]";
                    dialogBodyText.text = "1단계: 아래쪽에서 [기억조각 1, 2, 3]을 주워 가운데 [비어진 배경 1, 2, 3]을 복원하세요.\n2단계: 복원된 다리를 건너 [과거의 기억]을 본 뒤, 중앙 제단에서 [기억 순서 맞추기]와 [잘못된 기억 찾기]를 해결하세요!";
                }
            }

            RefreshHUD();
            HandleTargetChanged(InteractionManager.Instance != null ? InteractionManager.Instance.CurrentTarget : null);
        }

        public void EnterRepairShop() => SwitchSpace(SpaceRepairShop);
        public void EnterMemoryMap() => SwitchSpace(SpaceMemoryMap);

        private void HandlePortalTriggered(string destinationSpaceId)
        {
            SwitchSpace(destinationSpaceId);
        }

        /// <summary>
        /// 전체 게임 및 퍼즐 상태를 초기화하고 기억 수리점에서 다시 시작합니다.
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

            SwitchSpace(SpaceRepairShop);
        }

        public void RefreshHUD()
        {
            bool inShop = string.Equals(currentSpaceId, SpaceRepairShop, StringComparison.Ordinal);
            int filled = PuzzleManager.Instance != null ? PuzzleManager.Instance.FilledBlankSlots : 0;
            int total = PuzzleManager.Instance != null ? PuzzleManager.Instance.TotalBlankSlots : 3;
            bool mapDone = PuzzleManager.Instance != null && PuzzleManager.Instance.IsMemoryMapCompleted;
            bool orderDone = PuzzleManager.Instance != null && PuzzleManager.Instance.IsOrderPuzzleSolved;
            bool contradictionDone = PuzzleManager.Instance != null && PuzzleManager.Instance.IsContradictionPuzzleSolved;
            bool allCleared = PuzzleManager.Instance != null && PuzzleManager.Instance.IsEpisodeFullyRepaired;

            if (headerTitleText != null)
            {
                if (inShop)
                {
                    headerTitleText.text = allCleared
                        ? "★ [기억 수리점 공방 — Ep.1 수리 완료!] 시계공 노인과 대화해 보세요!"
                        : "[거점: 기억 수리점 공방] 시계공 노인과 대화 후 작업대의 '멈춘 회중시계'를 조사하세요!";
                }
                else
                {
                    headerTitleText.text = allCleared
                        ? "★ [모든 기억 수리 완료!] 우측 포털 또는 상단 버튼으로 '기억 수리점'에 복귀하세요!"
                        : mapDone
                            ? "★ [배경 복원 완료!] 위쪽 광장에서 과거의 기억(09:00 / 11:30 / 11:55)을 본 뒤 [기억 수리 제단]을 조사하세요!"
                            : $"[시계공의 기억 공간] 기억조각 1, 2, 3을 모아 비어진 배경({filled}/{total})을 복원하세요!";
                }
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
                sb.AppendLine(inShop ? "<b>[현재 공간: 기억 수리점]</b>" : "<b>[현재 공간: 기억 퍼즐맵]</b>");
                sb.AppendLine();
                sb.AppendLine("<b>[보유 아이템 — 기억조각]</b>");
                sb.AppendLine(has1 ? "<color=#FFE066>■ 기억조각 1 (보유)</color>" : "<color=#778899>□ 기억조각 1 (미획득)</color>");
                sb.AppendLine(has2 ? "<color=#66FF99>■ 기억조각 2 (보유)</color>" : "<color=#778899>□ 기억조각 2 (미획득)</color>");
                sb.AppendLine(has3 ? "<color=#FF88EE>■ 기억조각 3 (보유)</color>" : "<color=#778899>□ 기억조각 3 (미획득)</color>");
                sb.AppendLine();
                sb.AppendLine("<b>[Ep.1 3대 퍼즐 진행도]</b>");
                sb.AppendLine(mapDone
                    ? $"<color=#55FF88>✔ ③ 배경 공간 복원 ({filled}/{total})</color>"
                    : $"<color=#FFD166>▶ ③ 배경 공간 복원 ({filled}/{total})</color>");
                sb.AppendLine(orderDone
                    ? "<color=#55FF88>✔ ① 기억 순서 맞추기</color>"
                    : "<color=#AABBCC>□ ① 기억 순서 맞추기</color>");
                sb.AppendLine(contradictionDone
                    ? "<color=#55FF88>✔ ② 잘못된 기억 찾기</color>"
                    : "<color=#AABBCC>□ ② 잘못된 기억 찾기</color>");

                if (allCleared)
                {
                    sb.AppendLine();
                    sb.AppendLine("<color=#55FF88><b>★ 시계공 기억 수리 완료!</b></color>");
                }

                inventoryStatusText.text = sb.ToString();
            }
        }

        private void HandleTargetChanged(InteractableObject target)
        {
            if (promptText == null) return;

            if (target == null)
            {
                bool inShop = string.Equals(currentSpaceId, SpaceRepairShop, StringComparison.Ordinal);
                promptText.text = inShop
                    ? "수리점 안에서 [시계공 노인]과 대화하거나, 중앙 [멈춘 회중시계]에서 [E / Space]를 눌러 기억 공간으로 이동하세요."
                    : "주변의 [기억조각], [비어진 배경], [과거의 기억 환영], 또는 [기억 수리 제단]에서 [E / Space]를 누르세요.";
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
                promptText.text = $"▶ [E / Space] 대화 / 기억 확인 — '{target.DisplayName}'";
                promptText.color = new Color(0.65f, 1f, 0.75f, 1f);
            }
            else if (target.Type == InteractionType.PuzzleTrigger)
            {
                promptText.text = $"▶ [E / Space] 최종 기억 수리 시작 — '{target.DisplayName}'";
                promptText.color = new Color(1f, 0.75f, 0.95f, 1f);
            }
            else if (target.Type == InteractionType.SpacePortal)
            {
                promptText.text = $"▶ [E / Space] 공간 이동 — '{target.DisplayName}'";
                promptText.color = new Color(0.55f, 1f, 0.95f, 1f);
            }
        }

        private void HandleClueCollected(string clueId, string title, string description)
        {
            if (dialogPanel != null) dialogPanel.SetActive(true);
            if (dialogSpeakerText != null) dialogSpeakerText.text = $"아이템 획득: {title}";
            if (dialogBodyText != null) dialogBodyText.text = $"{description}\n(가운데 비어진 배경 칸으로 가서 [E / Space]를 눌러 끼워 넣으세요!)";
            RefreshHUD();
        }

        private void HandleDialogPrinted(string speaker, string line, int step, int total)
        {
            if (dialogPanel != null) dialogPanel.SetActive(true);
            if (dialogSpeakerText != null) dialogSpeakerText.text = $"{speaker} ({step}/{total})";
            if (dialogBodyText != null) dialogBodyText.text = line;
        }

        // =====================================================================
        // ③ 비어진 배경 빈칸(1, 2, 3)에 기억조각 끼워 넣기 UI
        // =====================================================================
        private void HandleBlankSlotInteracted(InteractableObject blankSlot)
        {
            if (blankSlot == null || blankSlot.IsSlotFilled) return;

            activeBlankSlot = blankSlot;
            if (slotChoicePanel != null) slotChoicePanel.SetActive(true);

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.IsMovementLocked = true;

            if (slotChoiceTitleText != null)
            {
                slotChoiceTitleText.text = $"[퍼즐 ③ 배경 복원] {blankSlot.DisplayName}";
            }
            if (slotChoiceDescText != null)
            {
                slotChoiceDescText.text = "보유 중인 기억조각(1, 2, 3) 중 이 빈칸의 배경에 알맞은 조각을 선택하세요.";
            }
            if (slotChoiceResultText != null)
            {
                slotChoiceResultText.text = "조각 버튼을 클릭해 끊어진 배경을 이어 주세요.";
            }

            RebuildSlotChoiceButtons();
        }

        private void RebuildSlotChoiceButtons()
        {
            ClearContainer(slotChoiceButtonContainer);
            if (slotChoiceButtonContainer == null) return;

            List<string> collected = StorySaveManager.Instance != null
                ? StorySaveManager.Instance.CurrentSaveData.collectedClueIds
                : new List<string>();

            CreateFragmentOptionButton("FRAG_01", "기억조각 1 (왼쪽 가로등길 배경)", collected.Contains("FRAG_01"));
            CreateFragmentOptionButton("FRAG_02", "기억조각 2 (중앙 시계탑 다리 배경)", collected.Contains("FRAG_02"));
            CreateFragmentOptionButton("FRAG_03", "기억조각 3 (오른쪽 벚꽃 벤치 배경)", collected.Contains("FRAG_03"));

            CreateActionButton(slotChoiceButtonContainer, "✕ 닫기", CloseSlotChoicePanel, new Color(0.45f, 0.25f, 0.25f, 1f));
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
                    dialogBodyText.text = $"'{triedFragId}' 조각이 비어진 배경에 딱 맞아떨어지며 다리가 복원되었습니다! (진행도: {filled}/{total})";
                }
                RefreshHUD();
            }
            else
            {
                if (slotChoiceResultText != null)
                {
                    slotChoiceResultText.text = $"<color=#FF6666>✕ '{triedFragId}' 조각은 이 배경 빈칸({slot.DisplayName})과 맞지 않습니다! 다른 조각을 선택하세요.</color>";
                }
            }
        }

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
                dialogSpeakerText.text = "★ [퍼즐 ③ 공간 복원 완료] 과거의 기억이 나타났습니다!";
            }
            if (dialogBodyText != null)
            {
                dialogBodyText.text = "복원된 다리를 건너 위쪽 광장의 [과거의 기억 환영 3곳(09:00 / 11:30 / 11:55)]을 확인한 뒤,\n광장 중앙의 [기억 수리 제단]에서 ① 기억 순서 맞추기와 ② 잘못된 기억 찾기를 완료하세요!";
            }

            RefreshHUD();
        }

        // =====================================================================
        // 1순위 핵심: 완성된 맵에서 [① 기억 순서 맞추기] -> [② 잘못된 기억 찾기] 연속 해결
        // =====================================================================
        private void HandleFinalPuzzleOpened(string puzzleId, PuzzleType puzzleType)
        {
            if (slotChoicePanel == null) return;
            slotChoicePanel.SetActive(true);

            bool orderSolved = PuzzleManager.Instance != null && PuzzleManager.Instance.IsOrderPuzzleSolved;
            bool contradictionSolved = PuzzleManager.Instance != null && PuzzleManager.Instance.IsContradictionPuzzleSolved;

            if (!orderSolved)
            {
                BuildChronologicalOrderStageUI();
            }
            else if (!contradictionSolved)
            {
                BuildContradictionSpottingStageUI();
            }
            else
            {
                BuildEpisodeCompletedStageUI();
            }
        }

        /// <summary>
        /// 최종 수리 1단계: ① 기억 순서 맞추기 (시간순 배열: 09:00 -> 11:30 -> 11:55)
        /// </summary>
        private void BuildChronologicalOrderStageUI()
        {
            submittedOrderList.Clear();
            ClearContainer(slotChoiceButtonContainer);

            if (slotChoiceTitleText != null)
            {
                slotChoiceTitleText.text = "[최종 수리 1/2단계 — ① 기억 순서 맞추기]";
            }
            if (slotChoiceDescText != null)
            {
                slotChoiceDescText.text = "광장에서 확인한 시계공의 그날 기억 3가지를 시간 순서(오전 9시 -> 11시 30분 -> 11시 55분)대로 클릭해 배열하세요.";
            }
            if (slotChoiceResultText != null)
            {
                slotChoiceResultText.text = "현재 배열 순서: (아직 선택되지 않음)";
            }

            // 순서를 섞어서 버튼 배치 (2 -> 3 -> 1 순서)
            CreateOrderItemButton("MEM_1130", "기억 조각 B: [오전 11:30] 정오 기차로 떠나는 딸의 편지를 발견하고 눈물 흘림");
            CreateOrderItemButton("MEM_1155", "기억 조각 C: [오전 11:55] 딸이 탄 기차를 멈추려 시계탑 태엽에 스스로 멈춤핀을 꽂음");
            CreateOrderItemButton("MEM_0900", "기억 조각 A: [오전 09:00] 아침에 시계탑 문을 열고 하루의 태엽을 감기 시작함");

            CreateActionButton(slotChoiceButtonContainer, "↺ 순서 다시 선택하기 (초기화)", () =>
            {
                submittedOrderList.Clear();
                if (slotChoiceResultText != null)
                {
                    slotChoiceResultText.text = "배열이 초기화되었습니다. 시간 순서대로 다시 클릭하세요.";
                }
            }, new Color(0.32f, 0.34f, 0.42f, 0.95f));

            CreateActionButton(slotChoiceButtonContainer, "✔ 1단계: 기억 순서 정답 검증하기", () =>
            {
                List<string> correctOrder = new List<string> { "MEM_0900", "MEM_1130", "MEM_1155" };
                bool ok = PuzzleManager.Instance != null &&
                          PuzzleManager.Instance.VerifyChronologicalOrder(PuzzleManager.OrderPuzzleId, submittedOrderList, correctOrder);

                if (ok)
                {
                    RefreshHUD();
                    BuildContradictionSpottingStageUI();
                }
                else if (slotChoiceResultText != null)
                {
                    slotChoiceResultText.text = "<color=#FF6666>✕ 시간 순서가 맞지 않습니다! (09:00 -> 11:30 -> 11:55 순서로 배열해 보세요)</color>";
                }
            }, new Color(0.18f, 0.55f, 0.36f, 1f));

            CreateActionButton(slotChoiceButtonContainer, "✕ 닫기", CloseSlotChoicePanel, new Color(0.45f, 0.25f, 0.25f, 1f));
        }

        private void CreateOrderItemButton(string itemId, string label)
        {
            CreateActionButton(slotChoiceButtonContainer, $"＋ {label}", () =>
            {
                if (!submittedOrderList.Contains(itemId))
                {
                    submittedOrderList.Add(itemId);
                    if (slotChoiceResultText != null)
                    {
                        slotChoiceResultText.text = $"현재 배열 순서 ({submittedOrderList.Count}/3): " + string.Join(" → ", submittedOrderList);
                    }
                }
            }, new Color(0.20f, 0.36f, 0.56f, 0.95f));
        }

        /// <summary>
        /// 최종 수리 2단계: ② 잘못된 기억 찾기 (모순 탐색)
        /// </summary>
        private void BuildContradictionSpottingStageUI()
        {
            selectedStatementId = "";
            selectedEvidenceId = "EVID_1155_PIN";
            ClearContainer(slotChoiceButtonContainer);

            if (slotChoiceTitleText != null)
            {
                slotChoiceTitleText.text = "★ 1단계 성공! → [최종 수리 2/2단계 — ② 잘못된 기억(모순) 찾기]";
            }
            if (slotChoiceDescText != null)
            {
                slotChoiceDescText.text = "시계공 노인이 수리점에서 말했던 기억 중, 방금 복원한 과거의 진실(11시 55분에 스스로 태엽을 멈춤)과 모순되는 '잘못된 기억'을 지목하세요!";
            }
            if (slotChoiceResultText != null)
            {
                slotChoiceResultText.text = "<color=#55FF88>✔ 기억 시간 순서 배열 완료!</color> 아래 진술 중 잘못된 기억(모순)을 하나 선택하세요.";
            }

            CreateActionButton(slotChoiceButtonContainer, "진술 ①: \"그날 아침 9시에 내가 직접 시계탑 문을 열고 태엽을 감았네.\"", () =>
            {
                selectedStatementId = "STMT_0900_TRUE";
                if (slotChoiceResultText != null)
                {
                    slotChoiceResultText.text = "선택한 진술: 진술 ① (아침 9시 시계탑 개방) — 이 진술이 거짓 기억일까요?";
                }
            }, new Color(0.22f, 0.32f, 0.48f, 0.95f));

            CreateActionButton(slotChoiceButtonContainer, "진술 ②: \"그날 정오(12:00)에 시계탑 종을 정상적으로 울린 뒤에 기차역으로 갔네.\"", () =>
            {
                selectedStatementId = "STMT_1200_CONTRADICTION";
                if (slotChoiceResultText != null)
                {
                    slotChoiceResultText.text = "선택한 진술: 진술 ② (정오에 종을 울렸다는 기억) + 근거: [11:55 멈춤핀 기억]";
                }
            }, new Color(0.22f, 0.32f, 0.48f, 0.95f));

            CreateActionButton(slotChoiceButtonContainer, "✔ 2단계: 잘못된 기억(모순) 정답 검증 및 기억 수리 완료!", () =>
            {
                bool ok = PuzzleManager.Instance != null &&
                          PuzzleManager.Instance.VerifyContradictionSpotting(
                              PuzzleManager.ContradictionPuzzleId,
                              selectedStatementId,
                              selectedEvidenceId,
                              "STMT_1200_CONTRADICTION",
                              "EVID_1155_PIN");

                if (ok)
                {
                    RefreshHUD();
                    BuildEpisodeCompletedStageUI();
                }
                else if (slotChoiceResultText != null)
                {
                    slotChoiceResultText.text = "<color=#FF6666>✕ 선택한 진술은 모순이 아닙니다! '정오에 종을 울렸다'는 진술 ②를 지목해 보세요.</color>";
                }
            }, new Color(0.18f, 0.55f, 0.36f, 1f));

            CreateActionButton(slotChoiceButtonContainer, "✕ 닫기", CloseSlotChoicePanel, new Color(0.45f, 0.25f, 0.25f, 1f));
        }

        private void BuildEpisodeCompletedStageUI()
        {
            ClearContainer(slotChoiceButtonContainer);

            if (slotChoiceTitleText != null)
            {
                slotChoiceTitleText.text = "★★★ Ep.1 시계공의 멈춘 시간 — 모든 기억 수리 완료! ★★★";
            }
            if (slotChoiceDescText != null)
            {
                slotChoiceDescText.text = "③ 배경 공간 복원 -> ① 기억 시간 순서 맞추기 -> ② 잘못된 기억(모순) 찾기를 모두 해결하여 시계공의 회중시계가 다시 움직이기 시작합니다!";
            }
            if (slotChoiceResultText != null)
            {
                slotChoiceResultText.text = "<color=#55FF88>이제 '기억 수리점 공방'으로 돌아가 시계공 노인에게 수리된 회중시계를 건네주세요!</color>";
            }

            CreateActionButton(slotChoiceButtonContainer, "★ 기억 수리점 공방으로 즉시 귀환하기", () =>
            {
                CloseSlotChoicePanel();
                SwitchSpace(SpaceRepairShop);
            }, new Color(0.20f, 0.58f, 0.40f, 1f));

            CreateActionButton(slotChoiceButtonContainer, "✕ 창 닫고 광장 더 둘러보기", CloseSlotChoicePanel, new Color(0.35f, 0.38f, 0.48f, 1f));
        }

        private void HandleEpisodeFullyRepaired()
        {
            RefreshHUD();
        }

        private void CloseSlotChoicePanel()
        {
            if (slotChoicePanel != null) slotChoicePanel.SetActive(false);
            activeBlankSlot = null;

            if (PuzzleManager.Instance != null && PuzzleManager.Instance.IsPuzzleWindowOpen)
            {
                PuzzleManager.Instance.ClosePuzzle();
            }

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.IsMovementLocked = false;
        }

        private void ClearContainer(Transform container)
        {
            if (container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
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
            le.preferredHeight = 36f;
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
            txt.fontSize = 14;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }
    }
}
