using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FragmentRPG
{
    /// <summary>
    /// 넓어진 거점 '기억 수리점 대공방' 및 3대 스테이지(Ep.1 시계탑 대광장, Ep.2 달빛 화실 대정원, Ep.3 별빛 기억 대성소)의
    /// 공간 전환, 카메라 바운드 설정, 인벤토리, 3대 퍼즐 체인을 관리하는 UI 매니저.
    /// </summary>
    public class FragmentUIManager : MonoBehaviour
    {
        public static FragmentUIManager Instance { get; private set; }

        public const string SpaceRepairShop = "SPACE_REPAIR_SHOP";
        public const string SpaceMemoryMap = "SPACE_STAGE_1"; // 하위 호환
        public const string SpaceStage1 = "SPACE_STAGE_1";
        public const string SpaceStage2 = "SPACE_STAGE_2";
        public const string SpaceStage3 = "SPACE_STAGE_3";

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

        [Header("Space & Stage Roots")]
        [SerializeField] private GameObject repairShopRoot;
        [SerializeField] private GameObject stage1Root;
        [SerializeField] private GameObject stage2Root;
        [SerializeField] private GameObject stage3Root;
        [SerializeField] private GameObject pastMemoryStage1;
        [SerializeField] private GameObject pastMemoryStage2;
        [SerializeField] private GameObject pastMemoryStage3;
        [SerializeField] private SpriteRenderer mainFloorRenderer;

        private string currentSpaceId = SpaceRepairShop;
        private InteractableObject activeBlankSlot;

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
                PuzzleManager.Instance.OnStageMemoryMapCompleted += HandleStageMemoryMapCompleted;
                PuzzleManager.Instance.OnPuzzleOpened += HandleFinalPuzzleOpened;
                PuzzleManager.Instance.OnEpisodeFullyRepaired += HandleEpisodeFullyRepaired;
            }

            if (StorySaveManager.Instance != null)
            {
                StorySaveManager.Instance.ResetSaveData();
            }

            if (pastMemoryStage1 != null) pastMemoryStage1.SetActive(false);
            if (pastMemoryStage2 != null) pastMemoryStage2.SetActive(false);
            if (pastMemoryStage3 != null) pastMemoryStage3.SetActive(false);

            SwitchSpace(SpaceRepairShop);
        }

        public void BindMultiStageReferences(
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
            GameObject stg1Obj,
            GameObject stg2Obj,
            GameObject stg3Obj,
            GameObject past1Obj,
            GameObject past2Obj,
            GameObject past3Obj,
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
            stage1Root = stg1Obj;
            stage2Root = stg2Obj;
            stage3Root = stg3Obj;
            pastMemoryStage1 = past1Obj;
            pastMemoryStage2 = past2Obj;
            pastMemoryStage3 = past3Obj;
            mainFloorRenderer = floorSr;
        }

        /// <summary>
        /// 거점(기억 수리점 대공방)과 스테이지 1~3(시계공/화가/수리공 기억 공간) 사이를 전환하고
        /// 넓어진 맵 크기에 맞춰 카메라 추적 범위를 갱신합니다.
        /// </summary>
        public void SwitchSpace(string targetSpace)
        {
            if (targetSpace == "SPACE_MEMORY_MAP") targetSpace = SpaceStage1;
            currentSpaceId = targetSpace;

            bool inShop = currentSpaceId == SpaceRepairShop;
            bool inStg1 = currentSpaceId == SpaceStage1;
            bool inStg2 = currentSpaceId == SpaceStage2;
            bool inStg3 = currentSpaceId == SpaceStage3;

            int stageNum = inStg2 ? 2 : (inStg3 ? 3 : 1);
            if (PuzzleManager.Instance != null)
            {
                PuzzleManager.Instance.CurrentStageNumber = stageNum;
            }
            if (StorySaveManager.Instance != null)
            {
                StorySaveManager.Instance.SetCurrentEpisode(stageNum);
            }

            if (repairShopRoot != null) repairShopRoot.SetActive(inShop);
            if (stage1Root != null) stage1Root.SetActive(inStg1);
            if (stage2Root != null) stage2Root.SetActive(inStg2);
            if (stage3Root != null) stage3Root.SetActive(inStg3);

            if (slotChoicePanel != null) slotChoicePanel.SetActive(false);

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                player.IsMovementLocked = false;
                if (inShop)
                {
                    player.transform.position = new Vector3(1.2f, -2.6f, 0f);
                    // 대공방 맵 크기(16.0 x 11.5)에 맞춘 카메라 이동 범위
                    player.SetCameraBounds(new Vector2(-1.8f, -1.2f), new Vector2(3.8f, 1.2f), true);
                }
                else
                {
                    player.transform.position = new Vector3(1.2f, -3.2f, 0f);
                    // 확장된 스테이지 맵 크기(18.0 x 13.0)에 맞춘 카메라 이동 범위
                    player.SetCameraBounds(new Vector2(-2.6f, -2.0f), new Vector2(4.8f, 2.0f), true);
                }
            }

            if (dialogPanel != null)
            {
                dialogPanel.SetActive(true);
                if (inShop)
                {
                    dialogSpeakerText.text = "[거점: 기억 수리점 대공방]";
                    dialogBodyText.text = "넓어진 대공방에 3개의 의뢰 작업대가 배치되어 있습니다!\n• 왼쪽: Ep.1 시계공 노인 & 회중시계 포털  |  중앙: Ep.2 무명 화가 & 캔버스 포털  |  오른쪽: Ep.3 기억의 잔향 & 태엽 코어 포털";
                }
                else if (inStg1)
                {
                    dialogSpeakerText.text = "[Stage 1 — Ep.1 시계공의 황혼 시계탑 대광장]";
                    dialogBodyText.text = "넓어진 분수대 정원을 탐색해 [기억조각 1, 2, 3]을 모아 끊어진 운하 다리 3곳을 복원하세요!\n다리가 이어지면 위쪽 시계탑 대광장으로 건너가 과거의 기억(09:00 / 11:30 / 11:55)과 최종 수리 제단을 조사할 수 있습니다.";
                }
                else if (inStg2)
                {
                    dialogSpeakerText.text = "[Stage 2 — Ep.2 화가의 달빛 아틀리에 대정원]";
                    dialogBodyText.text = "보랏빛 달빛 정원에서 화가의 [기억조각 1, 2, 3]을 모아 은하수 연못의 끊어진 다리 3곳을 복원하세요!\n복원 후 위쪽 야외 화실 테라스에서 폭풍우 치던 밤의 진실과 최종 수리 제단을 조사할 수 있습니다.";
                }
                else if (inStg3)
                {
                    dialogSpeakerText.text = "[Stage 3 — Ep.3 수리공의 별빛 기억 대성소]";
                    dialogBodyText.text = "황금 기어 회로가 빛나는 별빛 성소에서 주인공 자신의 [기억조각 1, 2, 3]을 모아 차원 다리 3곳을 복원하세요!\n복원 후 위쪽 중앙 신전에서 기억 수리점의 탄생 비밀을 복원할 수 있습니다.";
                }
            }

            RefreshHUD();
            HandleTargetChanged(InteractionManager.Instance != null ? InteractionManager.Instance.CurrentTarget : null);
        }

        public void EnterRepairShop() => SwitchSpace(SpaceRepairShop);
        public void EnterStage1() => SwitchSpace(SpaceStage1);
        public void EnterStage2() => SwitchSpace(SpaceStage2);
        public void EnterStage3() => SwitchSpace(SpaceStage3);

        private void HandlePortalTriggered(string destinationSpaceId)
        {
            SwitchSpace(destinationSpaceId);
        }

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

            if (pastMemoryStage1 != null) pastMemoryStage1.SetActive(false);
            if (pastMemoryStage2 != null) pastMemoryStage2.SetActive(false);
            if (pastMemoryStage3 != null) pastMemoryStage3.SetActive(false);

            SwitchSpace(SpaceRepairShop);
        }

        public void RefreshHUD()
        {
            bool inShop = currentSpaceId == SpaceRepairShop;
            int stg = PuzzleManager.Instance != null ? PuzzleManager.Instance.CurrentStageNumber : 1;
            int filled = PuzzleManager.Instance != null ? PuzzleManager.Instance.FilledBlankSlots : 0;
            int total = 3;
            bool mapDone = PuzzleManager.Instance != null && PuzzleManager.Instance.IsMemoryMapCompleted;
            bool orderDone = PuzzleManager.Instance != null && PuzzleManager.Instance.IsOrderPuzzleSolved;
            bool contradictionDone = PuzzleManager.Instance != null && PuzzleManager.Instance.IsContradictionPuzzleSolved;
            bool allCleared = PuzzleManager.Instance != null && PuzzleManager.Instance.IsEpisodeFullyRepaired;

            string stageTitle = stg == 1 ? "Ep.1 시계탑 대광장" : (stg == 2 ? "Ep.2 달빛 화실 정원" : "Ep.3 별빛 기억 성소");

            if (headerTitleText != null)
            {
                if (inShop)
                {
                    headerTitleText.text = "[거점: 기억 수리점 대공방] 의뢰인과 대화하거나 작업대 포털(Ep.1 / Ep.2 / Ep.3)로 입장하세요!";
                }
                else
                {
                    headerTitleText.text = allCleared
                        ? $"★ [{stageTitle} — 모든 기억 수리 완료!] 귀환 포털로 수리점 공방에 복귀하세요!"
                        : mapDone
                            ? $"★ [{stageTitle} — 다리 복원 완료!] 위쪽 광장에서 과거의 기억 3곳을 본 뒤 [기억 수리 제단]을 조사하세요!"
                            : $"[{stageTitle}] 넓은 맵을 탐색해 기억조각 1, 2, 3을 모아 끊어진 배경({filled}/{total})을 복원하세요!";
                }
            }

            if (inventoryStatusText != null)
            {
                List<string> clues = StorySaveManager.Instance != null
                    ? StorySaveManager.Instance.CurrentSaveData.collectedClueIds
                    : new List<string>();

                string prefix = $"EP{stg}_FRAG_";
                bool has1 = clues.Contains(prefix + "01");
                bool has2 = clues.Contains(prefix + "02");
                bool has3 = clues.Contains(prefix + "03");

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine(inShop ? "<b>[위치: 기억 수리점 대공방]</b>" : $"<b>[위치: {stageTitle}]</b>");
                sb.AppendLine();
                sb.AppendLine($"<b>[Ep.{stg} 기억조각 인벤토리]</b>");
                sb.AppendLine(has1 ? "<color=#FFE066>■ 기억조각 1 (보유)</color>" : "<color=#778899>□ 기억조각 1 (미획득)</color>");
                sb.AppendLine(has2 ? "<color=#66FF99>■ 기억조각 2 (보유)</color>" : "<color=#778899>□ 기억조각 2 (미획득)</color>");
                sb.AppendLine(has3 ? "<color=#FF88EE>■ 기억조각 3 (보유)</color>" : "<color=#778899>□ 기억조각 3 (미획득)</color>");
                sb.AppendLine();
                sb.AppendLine($"<b>[Ep.{stg} 3대 퍼즐 진행도]</b>");
                sb.AppendLine(mapDone
                    ? $"<color=#55FF88>✔ ③ 배경 공간 복원 ({filled}/{total})</color>"
                    : $"<color=#FFD166>▶ ③ 배경 공간 복원 ({filled}/{total})</color>");
                sb.AppendLine(orderDone
                    ? "<color=#55FF88>✔ ① 기억 순서 맞추기</color>"
                    : "<color=#AABBCC>□ ① 기억 순서 맞추기</color>");
                sb.AppendLine(contradictionDone
                    ? "<color=#55FF88>✔ ② 잘못된 기억 찾기</color>"
                    : "<color=#AABBCC>□ ② 잘못된 기억 찾기</color>");

                sb.AppendLine();
                sb.AppendLine("<b>[전체 에피소드 클리어 현황]</b>");
                bool c1 = PuzzleManager.Instance != null && PuzzleManager.Instance.IsStageFullyRepaired(1);
                bool c2 = PuzzleManager.Instance != null && PuzzleManager.Instance.IsStageFullyRepaired(2);
                bool c3 = PuzzleManager.Instance != null && PuzzleManager.Instance.IsStageFullyRepaired(3);
                sb.AppendLine(c1 ? "<color=#55FF88>★ Ep.1 시계공 수리 완료</color>" : "• Ep.1 시계공 (진행중)");
                sb.AppendLine(c2 ? "<color=#55FF88>★ Ep.2 화가 수리 완료</color>" : "• Ep.2 화가 (진행중)");
                sb.AppendLine(c3 ? "<color=#55FF88>★ Ep.3 수리공 수리 완료</color>" : "• Ep.3 수리공 (진행중)");

                inventoryStatusText.text = sb.ToString();
            }
        }

        private void HandleTargetChanged(InteractableObject target)
        {
            if (promptText == null) return;

            if (target == null)
            {
                bool inShop = currentSpaceId == SpaceRepairShop;
                promptText.text = inShop
                    ? "WASD / 방향키로 대공방을 이동하며 의뢰인(NPC) 또는 작업대 위 포털(회중시계/캔버스/태엽코어)에서 [E / Space]를 누르세요."
                    : "넓어진 맵을 돌아다니며 [기억조각 1~3], [비어진 배경 1~3], [과거의 기억 환영], [기억 수리 제단]에서 [E / Space]를 누르세요.";
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

        private void HandleBlankSlotInteracted(InteractableObject blankSlot)
        {
            if (blankSlot == null || blankSlot.IsSlotFilled) return;

            activeBlankSlot = blankSlot;
            if (slotChoicePanel != null) slotChoicePanel.SetActive(true);

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.IsMovementLocked = true;

            int stg = PuzzleManager.Instance != null ? PuzzleManager.Instance.CurrentStageNumber : 1;
            if (slotChoiceTitleText != null)
            {
                slotChoiceTitleText.text = $"[Ep.{stg} 퍼즐 ③ 배경 복원] {blankSlot.DisplayName}";
            }
            if (slotChoiceDescText != null)
            {
                slotChoiceDescText.text = "보유 중인 기억조각(1, 2, 3) 중 이 빈칸의 배경에 알맞은 조각을 선택하세요.";
            }
            if (slotChoiceResultText != null)
            {
                slotChoiceResultText.text = "조각 버튼을 클릭해 끊어진 배경을 이어 주세요.";
            }

            RebuildSlotChoiceButtons(stg);
        }

        private void RebuildSlotChoiceButtons(int stg)
        {
            ClearContainer(slotChoiceButtonContainer);
            if (slotChoiceButtonContainer == null) return;

            List<string> collected = StorySaveManager.Instance != null
                ? StorySaveManager.Instance.CurrentSaveData.collectedClueIds
                : new List<string>();

            string prefix = $"EP{stg}_FRAG_";
            string desc1 = stg == 1 ? "왼쪽 가로등 산책로 다리" : (stg == 2 ? "왼쪽 붓꽃 정원 다리" : "왼쪽 태엽 기어 다리");
            string desc2 = stg == 1 ? "중앙 시계탑 대리석 다리" : (stg == 2 ? "중앙 팔레트 아치 다리" : "중앙 별빛 코어 다리");
            string desc3 = stg == 1 ? "오른쪽 벚꽃 벤치 다리" : (stg == 2 ? "오른쪽 조각상 테라스 다리" : "오른쪽 시간의 서고 다리");

            CreateFragmentOptionButton(prefix + "01", $"기억조각 1 ({desc1})", collected.Contains(prefix + "01"));
            CreateFragmentOptionButton(prefix + "02", $"기억조각 2 ({desc2})", collected.Contains(prefix + "02"));
            CreateFragmentOptionButton(prefix + "03", $"기억조각 3 ({desc3})", collected.Contains(prefix + "03"));

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
                    dialogBodyText.text = $"기억조각이 비어진 배경에 딱 맞아떨어지며 다리가 복원되었습니다! (진행도: {filled}/3)";
                }
                RefreshHUD();
            }
            else
            {
                if (slotChoiceResultText != null)
                {
                    slotChoiceResultText.text = $"<color=#FF6666>✕ 선택한 조각은 이 배경 빈칸({slot.DisplayName})과 맞지 않습니다! 다른 번호의 기억조각을 넣어보세요.</color>";
                }
            }
        }

        private void HandleStageMemoryMapCompleted(int stageNum)
        {
            if (stageNum == 1 && pastMemoryStage1 != null) pastMemoryStage1.SetActive(true);
            if (stageNum == 2 && pastMemoryStage2 != null) pastMemoryStage2.SetActive(true);
            if (stageNum == 3 && pastMemoryStage3 != null) pastMemoryStage3.SetActive(true);

            if (dialogPanel != null) dialogPanel.SetActive(true);
            if (dialogSpeakerText != null)
            {
                dialogSpeakerText.text = $"★ [Ep.{stageNum} 퍼즐 ③ 공간 복원 완료] 위쪽 광장에 과거의 기억이 나타났습니다!";
            }
            if (dialogBodyText != null)
            {
                dialogBodyText.text = "복원된 다리를 건너 위쪽 광장의 [과거의 기억 환영 3곳]을 확인한 뒤,\n광장 중앙의 [기억 수리 제단]에서 ① 기억 순서 맞추기와 ② 잘못된 기억 찾기를 완료하세요!";
            }

            RefreshHUD();
        }

        private void HandleFinalPuzzleOpened(string puzzleId, PuzzleType puzzleType)
        {
            if (slotChoicePanel == null) return;
            slotChoicePanel.SetActive(true);

            int stg = PuzzleManager.Instance != null ? PuzzleManager.Instance.CurrentStageNumber : 1;
            bool orderSolved = PuzzleManager.Instance != null && PuzzleManager.Instance.IsOrderPuzzleSolved;
            bool contradictionSolved = PuzzleManager.Instance != null && PuzzleManager.Instance.IsContradictionPuzzleSolved;

            if (!orderSolved)
            {
                BuildChronologicalOrderStageUI(stg);
            }
            else if (!contradictionSolved)
            {
                BuildContradictionSpottingStageUI(stg);
            }
            else
            {
                BuildEpisodeCompletedStageUI(stg);
            }
        }

        private void BuildChronologicalOrderStageUI(int stg)
        {
            submittedOrderList.Clear();
            ClearContainer(slotChoiceButtonContainer);

            if (slotChoiceTitleText != null)
            {
                slotChoiceTitleText.text = $"[Ep.{stg} 최종 수리 1/2단계 — ① 기억 순서 맞추기]";
            }
            if (slotChoiceDescText != null)
            {
                slotChoiceDescText.text = "위쪽 광장에서 확인한 과거의 기억 3장면을 시간의 흐름(1 -> 2 -> 3) 순서대로 클릭해 배열하세요.";
            }
            if (slotChoiceResultText != null)
            {
                slotChoiceResultText.text = "현재 배열 순서: (아직 선택되지 않음)";
            }

            if (stg == 1)
            {
                CreateOrderItemButton("STEP_2", "기억 B: [오전 11:30] 정오 기차로 떠나는 딸의 편지를 발견하고 다급해짐");
                CreateOrderItemButton("STEP_3", "기억 C: [오전 11:55] 딸이 탄 기차를 멈추려 시계탑 태엽에 스스로 멈춤핀을 꽂음");
                CreateOrderItemButton("STEP_1", "기억 A: [오전 09:00] 아침에 시계탑 문을 열고 하루의 태엽을 감기 시작함");
            }
            else if (stg == 2)
            {
                CreateOrderItemButton("STEP_2", "기억 B: [밤 10:00] 어두워진 화실에 폭풍우 속 달빛을 담으려 창문을 활짝 열어젖힘");
                CreateOrderItemButton("STEP_3", "기억 C: [밤 11:40] 들이친 빗물에 자화상 물감이 번지자 슬픔 속에 캔버스를 덮어둠");
                CreateOrderItemButton("STEP_1", "기억 A: [오후 06:00] 해 질 녘 화실 이젤 앞에 앉아 마지막 자화상 스케치를 시작함");
            }
            else
            {
                CreateOrderItemButton("STEP_2", "기억 B: [두 번째 기억] 시계공과 화가의 부서진 기억을 고치며 공명 에너지를 모음");
                CreateOrderItemButton("STEP_3", "기억 C: [세 번째 기억] 봉인된 서랍에서 자신의 진짜 이름이 적힌 수리공 명패를 되찾음");
                CreateOrderItemButton("STEP_1", "기억 A: [첫 번째 기억] 상처 입은 기억들을 치유하기 위해 '기억 수리점'의 문을 처음 엶");
            }

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
                List<string> correctOrder = new List<string> { "STEP_1", "STEP_2", "STEP_3" };
                bool ok = PuzzleManager.Instance != null &&
                          PuzzleManager.Instance.VerifyChronologicalOrder($"PUZ_EP{stg}_ORDER", submittedOrderList, correctOrder);

                if (ok)
                {
                    RefreshHUD();
                    BuildContradictionSpottingStageUI(stg);
                }
                else if (slotChoiceResultText != null)
                {
                    slotChoiceResultText.text = "<color=#FF6666>✕ 시간 순서가 맞지 않습니다! (기억 A -> 기억 B -> 기억 C 순서로 배열해 보세요)</color>";
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

        private void BuildContradictionSpottingStageUI(int stg)
        {
            selectedStatementId = "";
            selectedEvidenceId = "EVID_TRUE";
            ClearContainer(slotChoiceButtonContainer);

            if (slotChoiceTitleText != null)
            {
                slotChoiceTitleText.text = $"★ 1단계 성공! → [Ep.{stg} 최종 수리 2/2단계 — ② 잘못된 기억(모순) 찾기]";
            }
            if (slotChoiceDescText != null)
            {
                slotChoiceDescText.text = "의뢰인이 수리점에서 말했던 진술 중, 방금 확인한 과거의 진실과 모순되는 '잘못된 기억'을 지목하세요!";
            }
            if (slotChoiceResultText != null)
            {
                slotChoiceResultText.text = "<color=#55FF88>✔ 기억 시간 순서 배열 완료!</color> 아래 진술 중 잘못된 기억(모순)을 선택하세요.";
            }

            string stmtTrue = stg == 1
                ? "진술 ①: \"그날 아침 9시에 내가 직접 시계탑 문을 열고 태엽을 감았네.\""
                : stg == 2
                    ? "진술 ①: \"해 질 녘에 화실 이젤 앞에 앉아 마지막 자화상을 그리기 시작했어요.\""
                    : "진술 ①: \"타인의 상처 입은 기억을 고치기 위해 이 기억 수리점을 열었어.\"";

            string stmtFalse = stg == 1
                ? "진술 ②: \"그날 정오(12:00)에 시계탑 종을 정상적으로 울린 뒤에 기차역으로 갔네.\""
                : stg == 2
                    ? "진술 ②: \"폭풍우 치던 밤 내내 화실 창문을 굳게 닫아두어서 빗물이 들어올 리 없었어요.\""
                    : "진술 ②: \"나는 원래부터 이름도 과거도 없던 빈 껍데기 인형이었어.\"";

            CreateActionButton(slotChoiceButtonContainer, stmtTrue, () =>
            {
                selectedStatementId = "STMT_TRUE";
                if (slotChoiceResultText != null)
                {
                    slotChoiceResultText.text = "선택한 진술: 진술 ① — 이 진술이 과거의 기억과 모순될까요?";
                }
            }, new Color(0.22f, 0.32f, 0.48f, 0.95f));

            CreateActionButton(slotChoiceButtonContainer, stmtFalse, () =>
            {
                selectedStatementId = "STMT_CONTRADICTION";
                if (slotChoiceResultText != null)
                {
                    slotChoiceResultText.text = "선택한 진술: 진술 ② (과거의 기억 C와 정면으로 모순되는 거짓 기억!)";
                }
            }, new Color(0.22f, 0.32f, 0.48f, 0.95f));

            CreateActionButton(slotChoiceButtonContainer, "✔ 2단계: 잘못된 기억(모순) 정답 검증 및 기억 수리 완료!", () =>
            {
                bool ok = PuzzleManager.Instance != null &&
                          PuzzleManager.Instance.VerifyContradictionSpotting(
                              $"PUZ_EP{stg}_CONTRADICTION",
                              selectedStatementId,
                              selectedEvidenceId,
                              "STMT_CONTRADICTION",
                              "EVID_TRUE");

                if (ok)
                {
                    RefreshHUD();
                    BuildEpisodeCompletedStageUI(stg);
                }
                else if (slotChoiceResultText != null)
                {
                    slotChoiceResultText.text = "<color=#FF6666>✕ 선택한 진술은 모순이 아닙니다! 과거의 기억과 충돌하는 '진술 ②'를 지목해 보세요.</color>";
                }
            }, new Color(0.18f, 0.55f, 0.36f, 1f));

            CreateActionButton(slotChoiceButtonContainer, "✕ 닫기", CloseSlotChoicePanel, new Color(0.45f, 0.25f, 0.25f, 1f));
        }

        private void BuildEpisodeCompletedStageUI(int stg)
        {
            ClearContainer(slotChoiceButtonContainer);

            string epName = stg == 1 ? "Ep.1 시계공의 멈춘 시간" : (stg == 2 ? "Ep.2 화가의 지워진 캔버스" : "Ep.3 수리공의 잃어버린 방");
            if (slotChoiceTitleText != null)
            {
                slotChoiceTitleText.text = $"★★★ {epName} — 모든 기억 수리 완료! ★★★";
            }
            if (slotChoiceDescText != null)
            {
                slotChoiceDescText.text = "③ 배경 공간 복원 -> ① 기억 시간 순서 맞추기 -> ② 잘못된 기억(모순) 찾기를 모두 해결했습니다!";
            }
            if (slotChoiceResultText != null)
            {
                slotChoiceResultText.text = "<color=#55FF88>이제 '기억 수리점 대공방'으로 돌아가 의뢰인에게 수리 결과를 전하거나 다음 스테이지로 이동하세요!</color>";
            }

            CreateActionButton(slotChoiceButtonContainer, "★ 기억 수리점 대공방으로 즉시 귀환하기", () =>
            {
                CloseSlotChoicePanel();
                SwitchSpace(SpaceRepairShop);
            }, new Color(0.20f, 0.58f, 0.40f, 1f));

            CreateActionButton(slotChoiceButtonContainer, "✕ 창 닫고 완성된 맵 더 둘러보기", CloseSlotChoicePanel, new Color(0.35f, 0.38f, 0.48f, 1f));
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
