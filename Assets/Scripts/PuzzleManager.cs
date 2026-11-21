using System;
using System.Collections.Generic;
using UnityEngine;

namespace FragmentRPG
{
    public enum PuzzleType
    {
        ChronologicalOrder,     // ① 기억 순서 맞추기 (시간순 배열)
        ContradictionSpotting,  // ② 잘못된 기억 찾기 (모순 탐색)
        SpatialRestoration      // ③ 사라진 물건·공간 복원 (기억조각 1,2,3 배경 빈칸 배치)
    }

    [Serializable]
    public class SpatialPlacementEntry
    {
        public string slotId;
        public string placedItemId;
        public Vector2 placedPosition;

        public SpatialPlacementEntry() { }

        public SpatialPlacementEntry(string slotId, string placedItemId, Vector2 placedPosition)
        {
            this.slotId = slotId;
            this.placedItemId = placedItemId;
            this.placedPosition = placedPosition;
        }
    }

    [Serializable]
    public class SpatialTargetRule
    {
        public string slotId;
        public string requiredItemId;
        public Vector2 targetPosition;
        public float allowedTolerance = 0.5f;
    }

    /// <summary>
    /// 스테이지별(Ep.1 시계공 / Ep.2 화가 / Ep.3 수리공) 3대 퍼즐 검증 및
    /// [배경 복원(③) -> 과거의 기억 관람 -> 기억 순서 맞추기(①) -> 잘못된 기억 찾기(②) -> 최종 기억 수리 완료]
    /// 전체 퍼즐 체인을 관리하는 매니저.
    /// </summary>
    public class PuzzleManager : MonoBehaviour
    {
        public static PuzzleManager Instance { get; private set; }

        public const string OrderPuzzleId = "PUZ_EP1_ORDER";
        public const string ContradictionPuzzleId = "PUZ_EP1_CONTRADICTION";

        [Header("Active Stage & Puzzle State")]
        [SerializeField] private int currentStageNumber = 1; // 1: 시계공, 2: 화가, 3: 수리공
        [SerializeField] private string activePuzzleId;
        [SerializeField] private PuzzleType activePuzzleType;
        [SerializeField] private bool isPuzzleWindowOpen;

        [Header("Per-Stage Progress (Index 1~3)")]
        [SerializeField] private int[] stageFilledSlots = new int[4];
        [SerializeField] private bool[] stageMapCompleted = new bool[4];
        [SerializeField] private bool[] stageOrderSolved = new bool[4];
        [SerializeField] private bool[] stageContradictionSolved = new bool[4];
        [SerializeField] private bool[] stageFullyRepaired = new bool[4];

        private readonly Dictionary<int, List<SpatialPlacementEntry>> stagePlacements = new Dictionary<int, List<SpatialPlacementEntry>>
        {
            { 1, new List<SpatialPlacementEntry>() },
            { 2, new List<SpatialPlacementEntry>() },
            { 3, new List<SpatialPlacementEntry>() }
        };

        public int CurrentStageNumber
        {
            get => currentStageNumber;
            set => currentStageNumber = Mathf.Clamp(value, 1, 3);
        }

        public bool IsPuzzleWindowOpen => isPuzzleWindowOpen;
        public int TotalBlankSlots => 3;
        public int FilledBlankSlots => stageFilledSlots[currentStageNumber];
        public bool IsMemoryMapCompleted => stageMapCompleted[currentStageNumber];
        public bool IsOrderPuzzleSolved => stageOrderSolved[currentStageNumber];
        public bool IsContradictionPuzzleSolved => stageContradictionSolved[currentStageNumber];
        public bool IsEpisodeFullyRepaired => stageFullyRepaired[currentStageNumber];

        public bool IsStageMapCompleted(int stage) => stage >= 1 && stage <= 3 && stageMapCompleted[stage];
        public bool IsStageFullyRepaired(int stage) => stage >= 1 && stage <= 3 && stageFullyRepaired[stage];
        public int GetStageFilledSlots(int stage) => stage >= 1 && stage <= 3 ? stageFilledSlots[stage] : 0;

        public event Action<string, PuzzleType> OnPuzzleOpened;
        public event Action<string, bool> OnPuzzleEvaluated;
        public event Action<InteractableObject, string, bool> OnSlotPlacementAttempted;
        public event Action<int> OnStageMemoryMapCompleted;
        public event Action OnMemoryMapCompleted;
        public event Action OnEpisodeFullyRepaired;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 배경의 비어진 칸(MapBlankSlot)에 플레이어가 선택한 기억조각을 끼워 넣습니다.
        /// </summary>
        public bool TryPlaceFragmentIntoBlankSlot(InteractableObject blankSlot, string selectedClueId)
        {
            if (blankSlot == null || blankSlot.Type != InteractionType.MapBlankSlot || blankSlot.IsSlotFilled)
            {
                return false;
            }

            bool isMatch = string.Equals(blankSlot.RequiredClueId, selectedClueId, StringComparison.Ordinal);
            if (!isMatch)
            {
                Debug.Log($"[PuzzleManager] '{blankSlot.DisplayName}'에 '{selectedClueId}' 조각은 맞지 않습니다! (필요 조각: {blankSlot.RequiredClueId})");
                OnSlotPlacementAttempted?.Invoke(blankSlot, selectedClueId, false);
                return false;
            }

            blankSlot.FillBlankSlot();

            int stg = currentStageNumber;
            if (!stagePlacements.ContainsKey(stg))
            {
                stagePlacements[stg] = new List<SpatialPlacementEntry>();
            }

            stagePlacements[stg].RemoveAll(p => p.slotId == blankSlot.SlotId);
            stagePlacements[stg].Add(new SpatialPlacementEntry(
                blankSlot.SlotId,
                selectedClueId,
                blankSlot.transform.position
            ));

            stageFilledSlots[stg] = stagePlacements[stg].Count;
            OnSlotPlacementAttempted?.Invoke(blankSlot, selectedClueId, true);

            if (stageFilledSlots[stg] >= TotalBlankSlots && !stageMapCompleted[stg])
            {
                stageMapCompleted[stg] = true;
                if (StorySaveManager.Instance != null)
                {
                    StorySaveManager.Instance.MarkPuzzleSolved($"PUZ_EP{stg}_MAP_RESTORE");
                    StorySaveManager.Instance.SaveGame();
                }

                Debug.Log($"[PuzzleManager] ★ Stage {stg} 배경 복원 완료! 위쪽 광장에 과거의 기억이 나타났습니다!");
                OnStageMemoryMapCompleted?.Invoke(stg);
                OnMemoryMapCompleted?.Invoke();
            }

            return true;
        }

        public void ResetMapPuzzleState()
        {
            for (int i = 1; i <= 3; i++)
            {
                stagePlacements[i].Clear();
                stageFilledSlots[i] = 0;
                stageMapCompleted[i] = false;
                stageOrderSolved[i] = false;
                stageContradictionSolved[i] = false;
                stageFullyRepaired[i] = false;
            }
            isPuzzleWindowOpen = false;
        }

        public void OpenPuzzle(string puzzleId, PuzzleType puzzleType)
        {
            activePuzzleId = puzzleId;
            activePuzzleType = puzzleType;
            isPuzzleWindowOpen = true;

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                player.IsMovementLocked = true;
            }

            OnPuzzleOpened?.Invoke(puzzleId, puzzleType);
        }

        public void ClosePuzzle()
        {
            isPuzzleWindowOpen = false;

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                player.IsMovementLocked = false;
            }
        }

        /// <summary>
        /// ① 기억 순서 맞추기(시간순 배열) 정답 검증 함수.
        /// </summary>
        public bool VerifyChronologicalOrder(string puzzleId, IList<string> submittedSequence, IList<string> correctSequence)
        {
            if (submittedSequence == null || correctSequence == null || submittedSequence.Count != correctSequence.Count)
            {
                return HandlePuzzleResult(puzzleId, false);
            }

            for (int i = 0; i < correctSequence.Count; i++)
            {
                if (!string.Equals(submittedSequence[i], correctSequence[i], StringComparison.Ordinal))
                {
                    return HandlePuzzleResult(puzzleId, false);
                }
            }

            stageOrderSolved[currentStageNumber] = true;
            return HandlePuzzleResult(puzzleId, true);
        }

        /// <summary>
        /// ② 잘못된 기억 찾기(모순 탐색) 정답 검증 함수.
        /// </summary>
        public bool VerifyContradictionSpotting(
            string puzzleId,
            string selectedStatementId,
            string presentedEvidenceClueId,
            string correctStatementId,
            string requiredEvidenceClueId)
        {
            bool statementMatched = string.Equals(selectedStatementId, correctStatementId, StringComparison.Ordinal);
            bool evidenceMatched = string.IsNullOrEmpty(requiredEvidenceClueId) ||
                                   string.Equals(presentedEvidenceClueId, requiredEvidenceClueId, StringComparison.Ordinal);

            bool solved = statementMatched && evidenceMatched;
            if (solved)
            {
                int stg = currentStageNumber;
                stageContradictionSolved[stg] = true;
                if (stageOrderSolved[stg] && !stageFullyRepaired[stg])
                {
                    stageFullyRepaired[stg] = true;
                    Debug.Log($"[PuzzleManager] ★★★ Stage {stg} 모든 기억 수리(공간 복원 + 시간 순서 + 모순 탐색) 완료! ★★★");
                    OnEpisodeFullyRepaired?.Invoke();
                }
            }

            return HandlePuzzleResult(puzzleId, solved);
        }

        /// <summary>
        /// ③ 사라진 물건·공간 복원(위치 배치) 정답 검증 함수.
        /// </summary>
        public bool VerifySpatialRestoration(
            string puzzleId,
            IList<SpatialPlacementEntry> placements,
            IList<SpatialTargetRule> targetRules,
            bool checkCoordinates = false)
        {
            if (placements == null || targetRules == null || placements.Count < targetRules.Count)
            {
                return HandlePuzzleResult(puzzleId, false);
            }

            Dictionary<string, SpatialPlacementEntry> placementMap = new Dictionary<string, SpatialPlacementEntry>();
            foreach (SpatialPlacementEntry entry in placements)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.slotId))
                {
                    placementMap[entry.slotId] = entry;
                }
            }

            foreach (SpatialTargetRule rule in targetRules)
            {
                if (!placementMap.TryGetValue(rule.slotId, out SpatialPlacementEntry submitted))
                {
                    return HandlePuzzleResult(puzzleId, false);
                }

                if (!string.Equals(submitted.placedItemId, rule.requiredItemId, StringComparison.Ordinal))
                {
                    return HandlePuzzleResult(puzzleId, false);
                }

                if (checkCoordinates)
                {
                    float distance = Vector2.Distance(submitted.placedPosition, rule.targetPosition);
                    if (distance > rule.allowedTolerance)
                    {
                        return HandlePuzzleResult(puzzleId, false);
                    }
                }
            }

            return HandlePuzzleResult(puzzleId, true);
        }

        private bool HandlePuzzleResult(string puzzleId, bool isSolved)
        {
            if (isSolved && StorySaveManager.Instance != null)
            {
                StorySaveManager.Instance.MarkPuzzleSolved(puzzleId);
                StorySaveManager.Instance.SaveGame();
            }

            OnPuzzleEvaluated?.Invoke(puzzleId, isSolved);
            return isSolved;
        }
    }
}
