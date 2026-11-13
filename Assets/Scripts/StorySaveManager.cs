using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FragmentRPG
{
    [Serializable]
    public class UserSaveData
    {
        public int currentEpisode = 1; // 1: 시계공, 2: 화가, 3: 주인공
        public List<string> collectedClueIds = new List<string>();
        public List<string> solvedPuzzleIds = new List<string>();
        public string lastSavedTime;
    }

    /// <summary>
    /// 현재 에피소드(1~3), 수집한 기억 조각 리스트, 해결한 퍼즐 상태를
    /// User_Save_DB.json 파일로 저장하고 불러오는 세이브 매니저.
    /// </summary>
    public class StorySaveManager : MonoBehaviour
    {
        public static StorySaveManager Instance { get; private set; }

        private const string SaveFileName = "User_Save_DB.json";

        [Header("Save Data Preview")]
        [SerializeField] private UserSaveData currentSaveData = new UserSaveData();

        public UserSaveData CurrentSaveData => currentSaveData;
        public string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

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
            LoadGame();
        }

        /// <summary>
        /// 현재 에피소드를 설정합니다 (1~3 범위 제한).
        /// </summary>
        public void SetCurrentEpisode(int episodeNumber)
        {
            currentSaveData.currentEpisode = Mathf.Clamp(episodeNumber, 1, 3);
            Debug.Log($"[StorySaveManager] 현재 에피소드 변경: Ep.{currentSaveData.currentEpisode}");
        }

        /// <summary>
        /// 수집한 기억 조각(Clue) ID를 중복 없이 등록합니다.
        /// </summary>
        public void AddCollectedClue(string clueId)
        {
            if (string.IsNullOrEmpty(clueId)) return;

            if (!currentSaveData.collectedClueIds.Contains(clueId))
            {
                currentSaveData.collectedClueIds.Add(clueId);
                Debug.Log($"[StorySaveManager] 기억 조각 등록 완료: {clueId} (총 {currentSaveData.collectedClueIds.Count}개)");
            }
        }

        public bool HasCollectedClue(string clueId)
        {
            return currentSaveData.collectedClueIds.Contains(clueId);
        }

        /// <summary>
        /// 해결 완료한 퍼즐 ID를 중복 없이 등록합니다.
        /// </summary>
        public void MarkPuzzleSolved(string puzzleId)
        {
            if (string.IsNullOrEmpty(puzzleId)) return;

            if (!currentSaveData.solvedPuzzleIds.Contains(puzzleId))
            {
                currentSaveData.solvedPuzzleIds.Add(puzzleId);
                Debug.Log($"[StorySaveManager] 해결된 퍼즐 등록 완료: {puzzleId} (총 {currentSaveData.solvedPuzzleIds.Count}개)");
            }
        }

        public bool IsPuzzleSolved(string puzzleId)
        {
            return currentSaveData.solvedPuzzleIds.Contains(puzzleId);
        }

        /// <summary>
        /// 현재 진행 데이터를 User_Save_DB.json 파일로 직렬화하여 저장합니다.
        /// </summary>
        [ContextMenu("Save Game to JSON")]
        public void SaveGame()
        {
            try
            {
                currentSaveData.lastSavedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string json = JsonUtility.ToJson(currentSaveData, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[StorySaveManager] 저장 완료 -> {SaveFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StorySaveManager] JSON 저장 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// User_Save_DB.json 파일이 존재하면 읽어와 복원하고, 없으면 기본 세이브 파일을 생성합니다.
        /// </summary>
        [ContextMenu("Load Game from JSON")]
        public void LoadGame()
        {
            try
            {
                if (!File.Exists(SaveFilePath))
                {
                    Debug.Log($"[StorySaveManager] 기존 세이브 파일이 없어 새로 생성합니다: {SaveFilePath}");
                    currentSaveData = new UserSaveData
                    {
                        currentEpisode = 1,
                        lastSavedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    SaveGame();
                    return;
                }

                string json = File.ReadAllText(SaveFilePath);
                UserSaveData loaded = JsonUtility.FromJson<UserSaveData>(json);
                if (loaded != null)
                {
                    currentSaveData = loaded;
                    currentSaveData.currentEpisode = Mathf.Clamp(currentSaveData.currentEpisode, 1, 3);
                    Debug.Log($"[StorySaveManager] 불러오기 완료 (Ep.{currentSaveData.currentEpisode}, 조각: {currentSaveData.collectedClueIds.Count}개, 퍼즐: {currentSaveData.solvedPuzzleIds.Count}개)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StorySaveManager] JSON 불러오기 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 세이브 데이터를 초기화합니다.
        /// </summary>
        [ContextMenu("Reset Save Data")]
        public void ResetSaveData()
        {
            currentSaveData = new UserSaveData
            {
                currentEpisode = 1,
                lastSavedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            SaveGame();
            Debug.Log("[StorySaveManager] 세이브 데이터가 초기화되었습니다.");
        }
    }
}
