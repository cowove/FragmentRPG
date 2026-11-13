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
    /// Fragment RPG 퍼즐 정답 검증 및 '기억조각 1, 2, 3 배경 빈칸 복원 -> 하나의 맵 완성 -> 과거의 기억 재생'
    /// 흐름을 관리하는 퍼즐 매니저.
    /// </summary>
    public class PuzzleManager : MonoBehaviour
    {
        public static PuzzleManager Instance { get; private set; }

        private const string MapRestorePuzzleId = "PUZ_MAP_RESTORE";

        [Header("Active Puzzle State")]
        [SerializeField] private string activePuzzleId;
        [SerializeField] private PuzzleType activePuzzleType;
        [SerializeField] private bool isPuzzleWindowOpen;

        [Header("Map Blank Slot Restoration State")]
        [SerializeField] private int totalBlankSlots = 3;
        [SerializeField] private int filledBlankSlots = 0;
        [SerializeField] private bool isMemoryMapCompleted = false;

        private readonly List<SpatialPlacementEntry> currentMapPlacements = new List<SpatialPlacementEntry>();
        private readonly List<SpatialTargetRule> defaultMapRules = new List<SpatialTargetRule>
        {
            new SpatialTargetRule { slotId = "SLOT_BG_01", requiredItemId = "FRAG_01", targetPosition = new Vector2(-3.2f, 0.8f), allowedTolerance = 1.0f },
            new SpatialTargetRule { slotId = "SLOT_BG_02", requiredItemId = "FRAG_02", targetPosition = new Vector2(0.0f, 0.8f), allowedTolerance = 1.0f },
            new SpatialTargetRule { slotId = "SLOT_BG_03", requiredItemId = "FRAG_03", targetPosition = new Vector2(3.2f, 0.8f), allowedTolerance = 1.0f }
        };

        public bool IsPuzzleWindowOpen => isPuzzleWindowOpen;
        public int TotalBlankSlots => totalBlankSlots;
        public int FilledBlankSlots => filledBlankSlots;
        public bool IsMemoryMapCompleted => isMemoryMapCompleted;

        public event Action<string, PuzzleType> OnPuzzleOpened;
        public event Action<string, bool> OnPuzzleEvaluated;
        public event Action<InteractableObject, string, bool> OnSlotPlacementAttempted;
        public event Action OnMemoryMapCompleted;

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
        /// 배경의 비어진 칸(MapBlankSlot)에 플레이어가 선택한 기억조각(FRAG_01~03)을 끼워 넣습니다.
        /// 알맞은 조각이면 해당 배경 칸이 복원되고, 3칸이 모두 채워지면 하나의 맵이 완성되어 과거의 기억이 열립니다.
        /// </summary>
        public bool TryPlaceFragmentIntoBlankSlot(InteractableObject blankSlot, string selectedClueId)
        {
            if (blankSlot == null || blankSlot.Type != InteractionType.MapBlankSlot || blankSlot.IsSlotFilled)
            {
                return false;
            }

            // 알맞은 기억조각인지 확인
            bool isMatch = string.Equals(blankSlot.RequiredClueId, selectedClueId, StringComparison.Ordinal);
            if (!isMatch)
            {
                Debug.Log($"[PuzzleManager] '{blankSlot.DisplayName}'에 '{selectedClueId}' 조각은 맞지 않습니다! (필요 조각: {blankSlot.RequiredClueId})");
                OnSlotPlacementAttempted?.Invoke(blankSlot, selectedClueId, false);
                return false;
            }

            // 빈칸 복원 처리
            blankSlot.FillBlankSlot();

            currentMapPlacements.RemoveAll(p => p.slotId == blankSlot.SlotId);
            currentMapPlacements.Add(new SpatialPlacementEntry(
                blankSlot.SlotId,
                selectedClueId,
                blankSlot.transform.position
            ));

            filledBlankSlots = currentMapPlacements.Count;
            Debug.Log($"[PuzzleManager] 배경 복원 성공! '{blankSlot.DisplayName}' <- {selectedClueId} ({filledBlankSlots}/{totalBlankSlots})");
            OnSlotPlacementAttempted?.Invoke(blankSlot, selectedClueId, true);

            // 3개의 비어진 배경 칸이 모두 채워졌는지 검증
            if (filledBlankSlots >= totalBlankSlots && !isMemoryMapCompleted)
            {
                bool verified = VerifySpatialRestoration(MapRestorePuzzleId, currentMapPlacements, defaultMapRules, false);
                if (verified)
                {
                    isMemoryMapCompleted = true;
                    Debug.Log("[PuzzleManager] ★★★ 기억조각 1, 2, 3이 모두 맞춰져 하나의 맵이 완성되었습니다! 과거의 기억이 나타납니다! ★★★");
                    OnMemoryMapCompleted?.Invoke();
                }
            }

            return true;
        }

        public void ResetMapPuzzleState()
        {
            currentMapPlacements.Clear();
            filledBlankSlots = 0;
            isMemoryMapCompleted = false;
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

            return HandlePuzzleResult(puzzleId, statementMatched && evidenceMatched);
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
            if (isSolved)
            {
                if (StorySaveManager.Instance != null)
                {
                    StorySaveManager.Instance.MarkPuzzleSolved(puzzleId);
                    StorySaveManager.Instance.SaveGame();
                }
                ClosePuzzle();
            }

            OnPuzzleEvaluated?.Invoke(puzzleId, isSolved);
            return isSolved;
        }
    }
}
