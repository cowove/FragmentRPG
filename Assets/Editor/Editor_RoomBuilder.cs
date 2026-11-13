#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FragmentRPG.EditorTools
{
    /// <summary>
    /// 고해상도 프로시저럴 2D 픽셀/아트 텍스처(돌바닥 광장, 시계탑, 강물, 가로등길, 아치 다리, 벚꽃 벤치,
    /// 퍼즐 빈칸 균열, 직소 퍼즐 조각, RPG 캐릭터)를 생성하여 그럴싸한 2D 퍼즐맵 배경을 구축하는 빌더.
    /// </summary>
    public static class Editor_RoomBuilder
    {
        private const string SpriteFolder = "Assets/Resources/GeneratedSprites";

        [MenuItem("Fragment RPG/1. 기본 씬 오브젝트 자동 생성", false, 1)]
        public static void BuildDefaultRoomScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Editor_RoomBuilder] 플레이 모드를 먼저 종료한 뒤 실행해 주세요.");
                return;
            }
            BuildDefaultRoomSceneInternal();
        }

        [MenuItem("Fragment RPG/2. GitHub 업로드 (Push) 실행", false, 2)]
        public static void PushToGitHubFromEditor()
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/k cd /d \"C:\\Users\\wjdtk\\FragmentRPG\" && echo [Fragment RPG] Uploading to GitHub... && git push -u origin main",
                UseShellExecute = true
            });
        }

        private static void BuildDefaultRoomSceneInternal()
        {
            if (EditorApplication.isPlaying) return;

            EnsureRichArtSprites();

            Sprite fullMapBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_FullMapCanvas.png");
            Sprite voidSlotSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_VoidSlot.png");
            Sprite restoredLeftSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_Restored_Left.png");
            Sprite restoredCenterSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_Restored_Center.png");
            Sprite restoredRightSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_Restored_Right.png");
            Sprite puzzlePieceSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_PuzzlePiece.png");
            Sprite charSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_Character.png");
            Sprite clocktowerSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_Clocktower.png");
            Sprite treeSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_Tree.png");
            Sprite lampSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_StreetLamp.png");
            Sprite squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_Square.png");

            if (Camera.main != null)
            {
                Camera.main.orthographic = true;
                Camera.main.orthographicSize = 5.4f;
                Camera.main.backgroundColor = new Color(0.05f, 0.06f, 0.10f, 1f);
                Camera.main.transform.position = new Vector3(0f, 0f, -10f);
            }

            ClearSceneExceptMainCamera();

            // 1. 매니저 생성
            GameObject managerRoot = new GameObject("@Managers");
            Undo.RegisterCreatedObjectUndo(managerRoot, "Create @Managers");

            InteractionManager interactionMgr = managerRoot.AddComponent<InteractionManager>();
            managerRoot.AddComponent<PuzzleManager>();
            managerRoot.AddComponent<StorySaveManager>();
            FragmentUIManager uiMgr = managerRoot.AddComponent<FragmentUIManager>();

            // 2. 그럴싸한 2D 타운/광장 배경 환경 구축
            SpriteRenderer mainFloorSr = BuildDetailedTownEnvironment(
                fullMapBgSprite,
                clocktowerSprite,
                treeSprite,
                lampSprite,
                squareSprite
            );

            // 3. 배경의 비어진 부분 3곳 (강물과 산책로가 끊겨 있는 퍼즐 빈칸 1, 2, 3)
            GameObject slotsRoot = new GameObject("BackgroundPuzzleSlots");
            Undo.RegisterCreatedObjectUndo(slotsRoot, "Create BackgroundPuzzleSlots");

            // 비어진 배경 1 (왼쪽: 가로등 산책로와 꽃길 다리 <- 기억조각 1)
            CreateRichBlankSlot(
                slotsRoot.transform,
                "BlankSlot_01",
                new Vector3(-2.1f, 0.15f, 0f),
                "SLOT_BG_01",
                "비어진 배경 1 (가로등 산책로)",
                "[빈 배경 1]",
                "FRAG_01",
                voidSlotSprite,
                restoredLeftSprite,
                lampSprite
            );

            // 비어진 배경 2 (중앙: 시계탑 광장 중앙 석조 다리 <- 기억조각 2)
            CreateRichBlankSlot(
                slotsRoot.transform,
                "BlankSlot_02",
                new Vector3(1.1f, 0.15f, 0f),
                "SLOT_BG_02",
                "비어진 배경 2 (중앙 시계탑 다리)",
                "[빈 배경 2]",
                "FRAG_02",
                voidSlotSprite,
                restoredCenterSprite,
                null
            );

            // 비어진 배경 3 (오른쪽: 벚꽃나무와 강변 벤치길 <- 기억조각 3)
            CreateRichBlankSlot(
                slotsRoot.transform,
                "BlankSlot_03",
                new Vector3(4.3f, 0.15f, 0f),
                "SLOT_BG_03",
                "비어진 배경 3 (벚꽃나무 벤치길)",
                "[빈 배경 3]",
                "FRAG_03",
                voidSlotSprite,
                restoredRightSprite,
                treeSprite
            );

            // 4. 플레이어 생성 (RPG 캐릭터 스프라이트)
            GameObject playerObj = new GameObject("Player");
            Undo.RegisterCreatedObjectUndo(playerObj, "Create Player");
            playerObj.transform.position = new Vector3(1.1f, -2.0f, 0f);
            playerObj.tag = "Player";

            SpriteRenderer playerSprite = playerObj.AddComponent<SpriteRenderer>();
            playerSprite.sprite = charSprite;
            playerSprite.color = new Color(0.45f, 0.90f, 1.0f, 1.0f);
            playerSprite.sortingOrder = 20;

            Rigidbody2D rb = playerObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D playerCollider = playerObj.AddComponent<BoxCollider2D>();
            playerCollider.size = new Vector2(0.65f, 0.65f);

            playerObj.AddComponent<PlayerController>();
            CreateWorldLabel(playerObj.transform, "수리공(플레이어)", new Vector3(0f, 0.72f, 0f), new Color(0.55f, 0.95f, 1f, 1f), 0.065f);
            interactionMgr.SetPlayerTransform(playerObj.transform);

            // 5. 기억조각 1, 2, 3 아이템 (직소 퍼즐 조각 아이콘)
            GameObject clueParent = new GameObject("ClueFragments");
            Undo.RegisterCreatedObjectUndo(clueParent, "Create ClueFragments");

            CreateClueObject(
                clueParent.transform,
                "Item_MemoryFragment_1",
                new Vector3(-2.2f, -2.3f, 0f),
                "FRAG_01",
                "기억조각 1",
                "비어진 배경 1(왼쪽 가로등 산책로)의 끊어진 배경을 이어 주는 첫 번째 퍼즐 조각입니다.",
                new Color(1.0f, 0.86f, 0.32f, 1.0f),
                puzzlePieceSprite
            );

            CreateClueObject(
                clueParent.transform,
                "Item_MemoryFragment_2",
                new Vector3(4.4f, -2.3f, 0f),
                "FRAG_02",
                "기억조각 2",
                "비어진 배경 2(중앙 시계탑 석조 다리)의 끊어진 배경을 이어 주는 두 번째 퍼즐 조각입니다.",
                new Color(0.45f, 0.98f, 0.65f, 1.0f),
                puzzlePieceSprite
            );

            CreateClueObject(
                clueParent.transform,
                "Item_MemoryFragment_3",
                new Vector3(1.1f, -3.1f, 0f),
                "FRAG_03",
                "기억조각 3",
                "비어진 배경 3(오른쪽 벚꽃 벤치길)의 끊어진 배경을 이어 주는 세 번째 퍼즐 조각입니다.",
                new Color(1.0f, 0.55f, 0.88f, 1.0f),
                puzzlePieceSprite
            );

            // 6. 맵 완성 시 위쪽 시계탑 광장에 나타나는 '과거의 기억' 장면
            GameObject pastMemoryRoot = BuildPastMemoryScene(charSprite, puzzlePieceSprite);
            pastMemoryRoot.SetActive(false);

            // 7. 인게임 UI Canvas 생성
            BuildPuzzleMapUICanvas(uiMgr, pastMemoryRoot, mainFloorSr);

            Selection.activeGameObject = playerObj;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("[Editor_RoomBuilder] ★ 고해상도 2D 타운/광장 배경과 퍼즐 조각 아트가 적용된 맵 생성이 완료되었습니다!");
        }

        private static void ClearSceneExceptMainCamera()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid()) return;

            GameObject[] roots = activeScene.GetRootGameObjects();
            for (int i = roots.Length - 1; i >= 0; i--)
            {
                GameObject go = roots[i];
                if (go == null) continue;
                if (go.GetComponent<Camera>() != null || go.name == "Main Camera") continue;
                Object.DestroyImmediate(go);
            }
        }

        private static SpriteRenderer BuildDetailedTownEnvironment(
            Sprite fullMapBgSprite,
            Sprite clocktowerSprite,
            Sprite treeSprite,
            Sprite lampSprite,
            Sprite squareSprite)
        {
            GameObject roomRoot = new GameObject("RoomEnvironment");
            Undo.RegisterCreatedObjectUndo(roomRoot, "Create RoomEnvironment");

            // 1. 고해상도 전체 타운 배경 일러스트 캔버스 (11.2 x 8.0 월드 유닛)
            // 위쪽: 시계탑 석조 광장과 황금 문양 바닥 / 가운데: 푸른 수로(강물) / 아래쪽: 잔디 정원과 조약돌 산책로
            GameObject mapCanvas = new GameObject("TownMapBackground");
            mapCanvas.transform.SetParent(roomRoot.transform, false);
            mapCanvas.transform.position = new Vector3(1.1f, 0.0f, 0f);
            mapCanvas.transform.localScale = new Vector3(1f, 1f, 1f);
            SpriteRenderer mapSr = mapCanvas.AddComponent<SpriteRenderer>();
            mapSr.sprite = fullMapBgSprite;
            mapSr.color = Color.white;
            mapSr.sortingOrder = -10;

            // 2. 위쪽 광장 중앙에 우뚝 솟은 고풍스러운 시계탑 오브젝트
            GameObject clocktower = new GameObject("Prop_GrandClocktower");
            clocktower.transform.SetParent(roomRoot.transform, false);
            clocktower.transform.position = new Vector3(1.1f, 3.05f, 0f);
            SpriteRenderer towerSr = clocktower.AddComponent<SpriteRenderer>();
            towerSr.sprite = clocktowerSprite;
            towerSr.sortingOrder = 4;
            BoxCollider2D towerCol = clocktower.AddComponent<BoxCollider2D>();
            towerCol.size = new Vector2(1.2f, 0.9f);

            // 3. 주변 환경 프롭 배치 (가로수 나무, 가로등)
            CreateEnvironmentProp(roomRoot.transform, "Tree_UpperLeft", new Vector3(-3.3f, 3.0f, 0f), treeSprite, 3);
            CreateEnvironmentProp(roomRoot.transform, "Tree_UpperRight", new Vector3(5.5f, 3.0f, 0f), treeSprite, 3);
            CreateEnvironmentProp(roomRoot.transform, "Tree_LowerLeft", new Vector3(-3.5f, -3.1f, 0f), treeSprite, 3);
            CreateEnvironmentProp(roomRoot.transform, "Tree_LowerRight", new Vector3(5.7f, -3.1f, 0f), treeSprite, 3);

            CreateEnvironmentProp(roomRoot.transform, "Lamp_UpperLeft", new Vector3(-0.6f, 2.75f, 0f), lampSprite, 4);
            CreateEnvironmentProp(roomRoot.transform, "Lamp_UpperRight", new Vector3(2.8f, 2.75f, 0f), lampSprite, 4);
            CreateEnvironmentProp(roomRoot.transform, "Lamp_LowerLeft", new Vector3(-0.6f, -1.55f, 0f), lampSprite, 4);
            CreateEnvironmentProp(roomRoot.transform, "Lamp_LowerRight", new Vector3(2.8f, -1.55f, 0f), lampSprite, 4);

            // 4. 외곽 경계 물리 콜라이더 벽 (투명 콜라이더로 자연스럽게 이동 범위 제한)
            CreateInvisibleWall(roomRoot.transform, "Bound_Top", new Vector3(1.1f, 3.95f, 0f), new Vector2(11.2f, 0.4f));
            CreateInvisibleWall(roomRoot.transform, "Bound_Bottom", new Vector3(1.1f, -3.95f, 0f), new Vector2(11.2f, 0.4f));
            CreateInvisibleWall(roomRoot.transform, "Bound_Left", new Vector3(-4.35f, 0f, 0f), new Vector2(0.4f, 8.2f));
            CreateInvisibleWall(roomRoot.transform, "Bound_Right", new Vector3(6.55f, 0f, 0f), new Vector2(0.4f, 8.2f));

            // 강물(수로) 사이 틈새 막음 콜라이더 (복원된 다리 슬롯 1, 2, 3 위로만 건널 수 있게 설정)
            CreateInvisibleWall(roomRoot.transform, "RiverBank_LeftEdge", new Vector3(-3.95f, 0.15f, 0f), new Vector2(0.7f, 1.9f));
            CreateInvisibleWall(roomRoot.transform, "RiverBank_Mid1", new Vector3(-0.5f, 0.15f, 0f), new Vector2(0.35f, 1.9f));
            CreateInvisibleWall(roomRoot.transform, "RiverBank_Mid2", new Vector3(2.7f, 0.15f, 0f), new Vector2(0.35f, 1.9f));
            CreateInvisibleWall(roomRoot.transform, "RiverBank_RightEdge", new Vector3(6.15f, 0.15f, 0f), new Vector2(0.7f, 1.9f));

            return mapSr;
        }

        private static void CreateEnvironmentProp(Transform parent, string name, Vector3 pos, Sprite sprite, int sortingOrder)
        {
            GameObject prop = new GameObject(name);
            prop.transform.SetParent(parent, false);
            prop.transform.position = pos;
            SpriteRenderer sr = prop.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
        }

        private static void CreateInvisibleWall(Transform parent, string name, Vector3 pos, Vector2 size)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.position = pos;
            BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        private static void CreateRichBlankSlot(
            Transform parent,
            string objectName,
            Vector3 position,
            string slotId,
            string displayName,
            string shortSlotLabel,
            string requiredClueId,
            Sprite emptyVoidSprite,
            Sprite restoredArtSprite,
            Sprite bonusPropSprite)
        {
            GameObject slotObj = new GameObject(objectName);
            slotObj.transform.SetParent(parent, false);
            slotObj.transform.position = position;
            slotObj.transform.localScale = Vector3.one; // 스프라이트 자체 크기가 2.8 x 2.0 월드 유닛

            SpriteRenderer sr = slotObj.AddComponent<SpriteRenderer>();
            sr.sprite = emptyVoidSprite;
            sr.color = Color.white;
            sr.sortingOrder = -5;

            BoxCollider2D col = slotObj.AddComponent<BoxCollider2D>();
            col.isTrigger = false;
            col.size = new Vector2(2.75f, 1.9f);

            // 복원 시 함께 나타나는 입체 장식 프롭
            GameObject restoredVisual = new GameObject("RestoredBackgroundDetail");
            restoredVisual.transform.SetParent(slotObj.transform, false);
            restoredVisual.transform.localPosition = new Vector3(-0.85f, 0.2f, 0f);
            if (bonusPropSprite != null)
            {
                SpriteRenderer propSr = restoredVisual.AddComponent<SpriteRenderer>();
                propSr.sprite = bonusPropSprite;
                propSr.sortingOrder = 5;
            }
            restoredVisual.SetActive(false);

            InteractableObject interactable = slotObj.AddComponent<InteractableObject>();
            interactable.SetupMapBlankSlot(
                slotId,
                displayName,
                requiredClueId,
                emptyVoidSprite,
                restoredArtSprite,
                Color.white,
                restoredVisual
            );

            CreateWorldLabel(slotObj.transform, shortSlotLabel, Vector3.zero, new Color(1f, 0.78f, 0.78f, 0.95f), 0.065f);
        }

        private static GameObject BuildPastMemoryScene(Sprite charSprite, Sprite crystalSprite)
        {
            GameObject memoryRoot = new GameObject("PastMemoryScene");
            Undo.RegisterCreatedObjectUndo(memoryRoot, "Create PastMemoryScene");

            // 과거의 기억 인물 1: 과거의 시계공
            CreateMemoryNPC(
                memoryRoot.transform,
                "Memory_Clockmaker",
                new Vector3(-1.2f, 2.15f, 0f),
                "MEM_NPC_01",
                "과거의 시계공",
                "DLG_PAST_01",
                new string[]
                {
                    "\"드디어 이 시계탑 광장 다리가 모두 이어졌구나.\"",
                    "\"네가 기차를 타고 떠나기 전에, 우리가 함께 걸었던 이 가로등길과 벚꽃 벤치를 꼭 기억해 주렴.\""
                },
                new Color(1.0f, 0.86f, 0.42f, 0.95f),
                charSprite
            );

            // 과거의 기억 인물 2: 과거의 아이
            CreateMemoryNPC(
                memoryRoot.transform,
                "Memory_Child",
                new Vector3(3.4f, 2.15f, 0f),
                "MEM_NPC_02",
                "과거의 아이",
                "DLG_PAST_02",
                new string[]
                {
                    "\"고마워요, 할아버지! 할아버지가 주신 이 회중시계를 평생 소중히 간직할게요!\"",
                    "\"비어 있던 배경이 모두 이어져서 드디어 그날의 따뜻한 기억이 온전히 되살아났어요!\""
                },
                new Color(0.65f, 0.95f, 1.0f, 0.95f),
                charSprite
            );

            // 중앙 기억의 결정체
            CreateMemoryNPC(
                memoryRoot.transform,
                "Memory_CoreCrystal",
                new Vector3(1.1f, 2.0f, 0f),
                "MEM_CORE",
                "복원된 기억의 결정체",
                "DLG_PAST_CORE",
                new string[]
                {
                    "[과거의 기억 재생 완료] 기억조각 1, 2, 3으로 복원된 시계탑 광장에서 두 사람의 마지막 인사가 따뜻하게 울려 퍼집니다.",
                    "축하합니다! 첫 번째 퍼즐맵의 배경 복원과 과거의 기억 관람을 완수했습니다!"
                },
                new Color(0.55f, 1.0f, 0.78f, 1.0f),
                crystalSprite
            );

            return memoryRoot;
        }

        private static void CreateClueObject(
            Transform parent,
            string objectName,
            Vector3 position,
            string clueId,
            string title,
            string description,
            Color tintColor,
            Sprite sprite)
        {
            GameObject clueObj = new GameObject(objectName);
            clueObj.transform.SetParent(parent, false);
            clueObj.transform.position = position;
            clueObj.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            SpriteRenderer sr = clueObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tintColor;
            sr.sortingOrder = 10;

            CircleCollider2D col = clueObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            InteractableObject interactable = clueObj.AddComponent<InteractableObject>();
            interactable.SetupClue(clueId, title, description);

            CreateWorldLabel(clueObj.transform, title, new Vector3(0f, 0.68f, 0f), tintColor, 0.065f);
        }

        private static void CreateMemoryNPC(
            Transform parent,
            string objectName,
            Vector3 position,
            string npcId,
            string npcName,
            string dialogId,
            string[] lines,
            Color tintColor,
            Sprite sprite)
        {
            GameObject npcObj = new GameObject(objectName);
            npcObj.transform.SetParent(parent, false);
            npcObj.transform.position = position;
            npcObj.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

            SpriteRenderer sr = npcObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tintColor;
            sr.sortingOrder = 12;

            CircleCollider2D col = npcObj.AddComponent<CircleCollider2D>();
            col.isTrigger = false;
            col.radius = 0.45f;

            InteractableObject interactable = npcObj.AddComponent<InteractableObject>();
            interactable.SetupNPC(npcId, npcName, dialogId, lines);

            CreateWorldLabel(npcObj.transform, $"★ {npcName}", new Vector3(0f, 0.72f, 0f), tintColor, 0.065f);
        }

        private static void CreateWorldLabel(Transform parent, string text, Vector3 localOffset, Color color, float worldScale = 0.065f)
        {
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(parent, false);
            labelObj.transform.localPosition = localOffset;

            Vector3 lossy = parent.lossyScale;
            float sx = Mathf.Abs(lossy.x) > 0.001f ? worldScale / lossy.x : worldScale;
            float sy = Mathf.Abs(lossy.y) > 0.001f ? worldScale / lossy.y : worldScale;
            labelObj.transform.localScale = new Vector3(sx, sy, 1f);

            TextMesh tm = labelObj.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 24;
            tm.characterSize = 1f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;

            MeshRenderer mr = labelObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 25;
            }
        }

        private static void BuildPuzzleMapUICanvas(
            FragmentUIManager uiMgr,
            GameObject pastMemoryRoot,
            SpriteRenderer mainFloorSr)
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject esObj = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            System.Type inputSysModule = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSysModule != null)
            {
                esObj.AddComponent(inputSysModule);
            }
            else
            {
                esObj.AddComponent<StandaloneInputModule>();
            }
#else
            esObj.AddComponent<StandaloneInputModule>();
#endif

            GameObject canvasObj = new GameObject("FragmentUI_Canvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create FragmentUI_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject topBar = CreateUIPanel(canvasObj.transform, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -46f), Vector2.zero, new Color(0.07f, 0.09f, 0.14f, 0.94f));
            Text headerTitleText = CreateUIText(topBar.transform, "HeaderTitleText", "[Fragment RPG 퍼즐맵] 기억조각 1, 2, 3을 모아 비어진 배경에 알맞게 끼워 넣으세요!", defaultFont, 15, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 0f), new Vector2(0.86f, 1f), new Vector2(18f, 0f), Vector2.zero);

            GameObject resetBtnObj = CreateUIPanel(topBar.transform, "Btn_ResetMap", new Vector2(0.87f, 0.15f), new Vector2(0.985f, 0.85f), Vector2.zero, Vector2.zero, new Color(0.55f, 0.25f, 0.25f, 1f));
            Button resetBtn = resetBtnObj.AddComponent<Button>();
            resetBtn.targetGraphic = resetBtnObj.GetComponent<Image>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(resetBtn.onClick, uiMgr.ResetPuzzleMap);
            CreateUIText(resetBtnObj.transform, "Text", "퍼즐 처음부터", defaultFont, 13, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject leftPanel = CreateUIPanel(canvasObj.transform, "InventoryPanel", new Vector2(0f, 0.26f), new Vector2(0.21f, 0.91f), new Vector2(10f, 0f), Vector2.zero, new Color(0.08f, 0.11f, 0.18f, 0.88f));
            Text inventoryText = CreateUIText(leftPanel.transform, "InventoryStatusText", "[보유 아이템 — 기억조각]", defaultFont, 13, TextAnchor.UpperLeft, Color.white, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));

            GameObject promptBar = CreateUIPanel(canvasObj.transform, "PromptBar", new Vector2(0.23f, 0.015f), new Vector2(0.98f, 0.07f), Vector2.zero, Vector2.zero, new Color(0.07f, 0.09f, 0.14f, 0.90f));
            Text promptText = CreateUIText(promptBar.transform, "PromptText", "맵 아래쪽에서 [기억조각 1, 2, 3]을 줍고, 가운데 [비어진 배경 1, 2, 3]에 다가가 [E] 또는 [Space]를 누르세요.", defaultFont, 14, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.35f, 1f), Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));

            GameObject dialogPanel = CreateUIPanel(canvasObj.transform, "DialogPanel", new Vector2(0.23f, 0.08f), new Vector2(0.98f, 0.21f), Vector2.zero, Vector2.zero, new Color(0.10f, 0.13f, 0.22f, 0.95f));
            Text dialogSpeakerText = CreateUIText(dialogPanel.transform, "DialogSpeakerText", "알림", defaultFont, 15, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f, 1f), new Vector2(0f, 0.64f), new Vector2(1f, 1f), new Vector2(14f, 0f), new Vector2(-14f, -2f));
            Text dialogBodyText = CreateUIText(dialogPanel.transform, "DialogBodyText", "내용", defaultFont, 14, TextAnchor.UpperLeft, Color.white, new Vector2(0f, 0f), new Vector2(1f, 0.64f), new Vector2(14f, 4f), new Vector2(-14f, -2f));
            dialogPanel.SetActive(false);

            GameObject slotChoicePanel = CreateUIPanel(canvasObj.transform, "SlotChoicePanel", new Vector2(0.28f, 0.26f), new Vector2(0.95f, 0.88f), Vector2.zero, Vector2.zero, new Color(0.09f, 0.12f, 0.20f, 0.97f));
            Text choiceTitle = CreateUIText(slotChoicePanel.transform, "ChoiceTitleText", "비어진 배경에 기억조각 넣기", defaultFont, 18, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.35f, 1f), new Vector2(0f, 0.84f), new Vector2(1f, 0.98f), new Vector2(16f, 0f), new Vector2(-16f, 0f));
            Text choiceDesc = CreateUIText(slotChoicePanel.transform, "ChoiceDescText", "이 빈칸에 알맞은 기억조각을 선택하세요.", defaultFont, 14, TextAnchor.MiddleCenter, new Color(0.85f, 0.9f, 1f, 1f), new Vector2(0f, 0.72f), new Vector2(1f, 0.84f), new Vector2(16f, 0f), new Vector2(-16f, 0f));
            Text choiceResult = CreateUIText(slotChoicePanel.transform, "ChoiceResultText", "조각 버튼을 눌러 배경을 복원하세요.", defaultFont, 15, TextAnchor.MiddleCenter, Color.cyan, new Vector2(0f, 0.58f), new Vector2(1f, 0.72f), new Vector2(16f, 0f), new Vector2(-16f, 0f));

            GameObject btnContainer = new GameObject("ChoiceButtonContainer");
            btnContainer.transform.SetParent(slotChoicePanel.transform, false);
            RectTransform containerRt = btnContainer.AddComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.08f, 0.06f);
            containerRt.anchorMax = new Vector2(0.92f, 0.56f);
            containerRt.offsetMin = Vector2.zero;
            containerRt.offsetMax = Vector2.zero;
            VerticalLayoutGroup vlg = btnContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            slotChoicePanel.SetActive(false);

            uiMgr.BindPuzzleMapReferences(
                headerTitleText,
                inventoryText,
                promptText,
                dialogPanel,
                dialogSpeakerText,
                dialogBodyText,
                slotChoicePanel,
                choiceTitle,
                choiceDesc,
                choiceResult,
                btnContainer.transform,
                pastMemoryRoot,
                mainFloorSr
            );
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            Image img = panel.AddComponent<Image>();
            img.color = color;
            return panel;
        }

        private static Text CreateUIText(Transform parent, string name, string content, Font font, int fontSize, TextAnchor anchor, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject txtObj = new GameObject(name);
            txtObj.transform.SetParent(parent, false);
            RectTransform rt = txtObj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = font;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = color;
            txt.supportRichText = true;
            return txt;
        }

        // =====================================================================
        // 고해상도 프로시저럴 2D 배경 & 퍼즐 조각 & 오브젝트 아트 생성기
        // =====================================================================
        private static void EnsureRichArtSprites()
        {
            if (!Directory.Exists(SpriteFolder))
            {
                Directory.CreateDirectory(SpriteFolder);
            }

            GenerateFullTownBackgroundCanvas($"{SpriteFolder}/Spr_FullMapCanvas.png");
            GenerateVoidSlotSprite($"{SpriteFolder}/Spr_VoidSlot.png");
            GenerateRestoredSlotSprite($"{SpriteFolder}/Spr_Restored_Left.png", 1);
            GenerateRestoredSlotSprite($"{SpriteFolder}/Spr_Restored_Center.png", 2);
            GenerateRestoredSlotSprite($"{SpriteFolder}/Spr_Restored_Right.png", 3);
            GeneratePuzzlePieceSprite($"{SpriteFolder}/Spr_PuzzlePiece.png");
            GenerateCharacterSprite($"{SpriteFolder}/Spr_Character.png");
            GenerateClocktowerSprite($"{SpriteFolder}/Spr_Clocktower.png");
            GenerateTreeSprite($"{SpriteFolder}/Spr_Tree.png");
            GenerateStreetLampSprite($"{SpriteFolder}/Spr_StreetLamp.png");
            GenerateSimpleSquare($"{SpriteFolder}/Spr_Square.png");

            AssetDatabase.Refresh();

            ConfigureSpriteImporter($"{SpriteFolder}/Spr_FullMapCanvas.png", 48f, FilterMode.Point);
            ConfigureSpriteImporter($"{SpriteFolder}/Spr_VoidSlot.png", 48f, FilterMode.Bilinear);
            ConfigureSpriteImporter($"{SpriteFolder}/Spr_Restored_Left.png", 48f, FilterMode.Point);
            ConfigureSpriteImporter($"{SpriteFolder}/Spr_Restored_Center.png", 48f, FilterMode.Point);
            ConfigureSpriteImporter($"{SpriteFolder}/Spr_Restored_Right.png", 48f, FilterMode.Point);
            ConfigureSpriteImporter($"{SpriteFolder}/Spr_PuzzlePiece.png", 64f, FilterMode.Bilinear);
            ConfigureSpriteImporter($"{SpriteFolder}/Spr_Character.png", 64f, FilterMode.Point);
            ConfigureSpriteImporter($"{SpriteFolder}/Spr_Clocktower.png", 48f, FilterMode.Point);
            ConfigureSpriteImporter($"{SpriteFolder}/Spr_Tree.png", 48f, FilterMode.Point);
            ConfigureSpriteImporter($"{SpriteFolder}/Spr_StreetLamp.png", 48f, FilterMode.Point);
            ConfigureSpriteImporter($"{SpriteFolder}/Spr_Square.png", 64f, FilterMode.Bilinear);
        }

        /// <summary>
        /// 전체 맵 배경(528 x 384 px, PPU 48 -> 11.0 x 8.0 월드 유닛):
        /// 외곽 성벽 테두리, 아래쪽 잔디 정원과 조약돌 산책로, 가운데 흐르는 푸른 수로(강물), 위쪽 시계탑 석조 광장과 황금 시계 문양 바닥을 그립니다.
        /// </summary>
        private static void GenerateFullTownBackgroundCanvas(string path)
        {
            int w = 528;
            int h = 384;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            // 수로(강물) Y 구간: 중심(y=192) 근처 156 ~ 244 구간
            int riverMinY = 152;
            int riverMaxY = 248;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // 1. 외곽 석조 성벽 테두리 (두께 16px)
                    if (x < 16 || x >= w - 16 || y < 16 || y >= h - 16)
                    {
                        bool mortar = (y % 12 == 0) || ((x + (y / 12) * 16) % 28 == 0);
                        Color wallCol = mortar ? new Color(0.16f, 0.18f, 0.25f, 1f) : new Color(0.32f, 0.36f, 0.48f, 1f);
                        if (x == 15 || x == w - 16 || y == 15 || y == h - 16)
                        {
                            wallCol = new Color(0.48f, 0.54f, 0.68f, 1f);
                        }
                        tex.SetPixel(x, y, wallCol);
                        continue;
                    }

                    // 2. 가운데 수로(강물) 및 석조 강둑 난간
                    if (y >= riverMinY && y <= riverMaxY)
                    {
                        if (y < riverMinY + 8 || y > riverMaxY - 8)
                        {
                            // 석조 강둑 테두리
                            bool stoneJoint = (x % 24 == 0);
                            tex.SetPixel(x, y, stoneJoint ? new Color(0.25f, 0.28f, 0.36f, 1f) : new Color(0.45f, 0.50f, 0.60f, 1f));
                        }
                        else
                        {
                            // 흐르는 강물 물결 패턴
                            float wave = Mathf.Sin((x * 0.08f) + (y * 0.15f)) * 0.5f + 0.5f;
                            Color waterCol = Color.Lerp(new Color(0.10f, 0.28f, 0.48f, 1f), new Color(0.18f, 0.44f, 0.68f, 1f), wave);
                            if ((x + y * 2) % 37 == 0 && wave > 0.7f)
                            {
                                waterCol = new Color(0.45f, 0.75f, 0.95f, 1f); // 물결 반짝임
                            }
                            tex.SetPixel(x, y, waterCol);
                        }
                        continue;
                    }

                    // 3. 아래쪽 구역 (잔디 정원 + 3갈래 조약돌 산책로)
                    if (y < riverMinY)
                    {
                        // 왼쪽(x=110), 중앙(x=264), 오른쪽(x=418)으로 이어지는 산책로 + 가로 연결 길(y=65~105)
                        bool onLeftPath = Mathf.Abs(x - 110) < 38 && y >= 65;
                        bool onCenterPath = Mathf.Abs(x - 264) < 42 && y >= 45;
                        bool onRightPath = Mathf.Abs(x - 418) < 38 && y >= 65;
                        bool onCrossPath = (y >= 62 && y <= 102 && x >= 72 && x <= 456);

                        if (onLeftPath || onCenterPath || onRightPath || onCrossPath)
                        {
                            // 조약돌 포장도로 질감
                            int brickX = x + ((y / 14) % 2) * 11;
                            bool joint = (y % 14 == 0) || (brickX % 22 == 0);
                            float shade = ((x * 7 + y * 13) % 17) * 0.004f;
                            Color cobble = joint
                                ? new Color(0.22f, 0.24f, 0.30f, 1f)
                                : new Color(0.38f + shade, 0.40f + shade, 0.48f + shade, 1f);
                            tex.SetPixel(x, y, cobble);
                        }
                        else
                        {
                            // 밤하늘 아래 푸른 잔디밭 + 꽃송이 디테일
                            float grassNoise = ((x * 19 + y * 31) % 23) / 23f;
                            Color grass = Color.Lerp(new Color(0.14f, 0.30f, 0.24f, 1f), new Color(0.19f, 0.38f, 0.30f, 1f), grassNoise);
                            if ((x * 11 + y * 29) % 197 == 0)
                            {
                                grass = new Color(0.95f, 0.85f, 0.35f, 1f); // 작은 노란 야생화
                            }
                            else if ((x * 23 + y * 17) % 241 == 0)
                            {
                                grass = new Color(0.95f, 0.55f, 0.80f, 1f); // 작은 분홍 꽃
                            }
                            tex.SetPixel(x, y, grass);
                        }
                        continue;
                    }

                    // 4. 위쪽 구역 (고풍스러운 시계탑 석조 광장 + 황금 시계 다이얼 문양)
                    if (y > riverMaxY)
                    {
                        float cx = 264f;
                        float cy = 312f;
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));

                        bool inPlaza = (x >= 56 && x <= w - 56 && y <= h - 28);
                        if (inPlaza)
                        {
                            bool tileGrid = (x % 24 == 0) || (y % 24 == 0);
                            Color plazaCol = tileGrid
                                ? new Color(0.24f, 0.26f, 0.36f, 1f)
                                : new Color(0.33f, 0.36f, 0.48f, 1f);

                            // 광장 바닥의 황금 시계 마법진 링 문양
                            if (Mathf.Abs(dist - 52f) < 2.2f || Mathf.Abs(dist - 36f) < 1.8f)
                            {
                                plazaCol = new Color(0.78f, 0.66f, 0.32f, 1f);
                            }
                            tex.SetPixel(x, y, plazaCol);
                        }
                        else
                        {
                            float grassNoise = ((x * 19 + y * 31) % 23) / 23f;
                            tex.SetPixel(x, y, Color.Lerp(new Color(0.14f, 0.30f, 0.24f, 1f), new Color(0.19f, 0.38f, 0.30f, 1f), grassNoise));
                        }
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 비어진 배경 슬롯(132 x 96 px, PPU 48 -> 2.75 x 2.0 월드 유닛):
        /// 기억이 뜯겨 나간 심연의 보랏빛 균열 구멍과 황금 점선 퍼즐 테두리를 그립니다.
        /// </summary>
        private static void GenerateVoidSlotSprite(string path)
        {
            int w = 132;
            int h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool onBorder = (x < 4 || x >= w - 4 || y < 4 || y >= h - 4);
                    bool dashed = ((x + y) / 8) % 2 == 0;

                    if (onBorder)
                    {
                        tex.SetPixel(x, y, dashed ? new Color(1.0f, 0.72f, 0.35f, 0.95f) : new Color(0.35f, 0.20f, 0.45f, 0.85f));
                        continue;
                    }

                    // 심연 소용돌이 및 별빛 입자
                    float nx = (x - w * 0.5f) / (w * 0.5f);
                    float ny = (y - h * 0.5f) / (h * 0.5f);
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    Color voidCol = Color.Lerp(new Color(0.05f, 0.04f, 0.11f, 0.98f), new Color(0.14f, 0.09f, 0.24f, 0.96f), Mathf.Clamp01(r));

                    // 균열 번개선 느낌
                    if (Mathf.Abs((y - h * 0.5f) - Mathf.Sin(x * 0.12f) * 14f) < 1.2f)
                    {
                        voidCol = new Color(0.45f, 0.28f, 0.68f, 0.9f);
                    }
                    else if ((x * 37 + y * 53) % 181 == 0)
                    {
                        voidCol = new Color(0.75f, 0.65f, 1.0f, 0.9f);
                    }

                    tex.SetPixel(x, y, voidCol);
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 복원된 배경 퍼즐 조각 스프라이트(132 x 96 px, PPU 48):
        /// 강물 위로 놓인 다리, 난간, 꽃밭/시계문양/벚꽃벤치가 직접 그려진 배경 타일을 생성합니다.
        /// </summary>
        private static void GenerateRestoredSlotSprite(string path, int slotType)
        {
            int w = 132;
            int h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // 퍼즐 조각 완성 골드 테두리 (1px 은은한 빛)
                    if (x == 0 || x == w - 1 || y == 0 || y == h - 1)
                    {
                        tex.SetPixel(x, y, new Color(0.95f, 0.85f, 0.45f, 0.85f));
                        continue;
                    }

                    // 중앙을 세로로 가로지르는 다리 폭 (x: 26 ~ 106)
                    bool onBridge = (x >= 24 && x <= w - 24);
                    bool onBridgeRail = (x >= 20 && x < 24) || (x > w - 24 && x <= w - 20);

                    if (onBridgeRail)
                    {
                        // 다리 난간 기둥
                        bool post = (y % 18 < 6);
                        tex.SetPixel(x, y, post ? new Color(0.78f, 0.72f, 0.58f, 1f) : new Color(0.52f, 0.48f, 0.42f, 1f));
                        continue;
                    }

                    if (onBridge)
                    {
                        if (slotType == 1) // 왼쪽: 따뜻한 목조 산책로 다리 & 가로등 불빛
                        {
                            bool plank = (y % 8 == 0);
                            Color wood = plank ? new Color(0.32f, 0.22f, 0.14f, 1f) : new Color(0.52f, 0.36f, 0.22f, 1f);
                            float lampGlow = Vector2.Distance(new Vector2(x, y), new Vector2(40f, 52f));
                            if (lampGlow < 28f)
                            {
                                wood = Color.Lerp(new Color(1.0f, 0.88f, 0.45f, 1f), wood, lampGlow / 28f);
                            }
                            tex.SetPixel(x, y, wood);
                        }
                        else if (slotType == 2) // 중앙: 시계탑 황금 문양 대리석 다리
                        {
                            bool marbleGrid = (y % 12 == 0) || (x % 16 == 0);
                            Color marble = marbleGrid ? new Color(0.35f, 0.42f, 0.48f, 1f) : new Color(0.52f, 0.60f, 0.66f, 1f);
                            // 중앙 황금 레드카펫 러그
                            if (x >= 48 && x <= w - 48)
                            {
                                marble = (x == 48 || x == w - 48)
                                    ? new Color(0.95f, 0.82f, 0.35f, 1f)
                                    : new Color(0.28f, 0.58f, 0.48f, 1f);
                            }
                            tex.SetPixel(x, y, marble);
                        }
                        else // 오른쪽: 벚꽃잎이 흩날리는 돌다리와 원목 벤치
                        {
                            bool stone = (y % 10 == 0) || ((x + (y / 10) * 8) % 16 == 0);
                            Color pathCol = stone ? new Color(0.34f, 0.28f, 0.38f, 1f) : new Color(0.50f, 0.42f, 0.54f, 1f);
                            // 오른쪽 벤치 그림 (x: 78~98, y: 36~64)
                            if (x >= 78 && x <= 98 && y >= 36 && y <= 64)
                            {
                                pathCol = (y % 6 == 0) ? new Color(0.35f, 0.20f, 0.12f, 1f) : new Color(0.62f, 0.38f, 0.22f, 1f);
                            }
                            else if ((x * 17 + y * 29) % 43 == 0)
                            {
                                pathCol = new Color(1.0f, 0.68f, 0.86f, 1f); // 떨어진 벚꽃잎
                            }
                            tex.SetPixel(x, y, pathCol);
                        }
                    }
                    else
                    {
                        // 다리 양옆의 복원된 맑은 강물
                        float wave = Mathf.Sin((x * 0.1f) + (y * 0.15f)) * 0.5f + 0.5f;
                        Color water = Color.Lerp(new Color(0.14f, 0.38f, 0.62f, 1f), new Color(0.24f, 0.56f, 0.80f, 1f), wave);
                        tex.SetPixel(x, y, water);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 실제 직소 퍼즐 조각(Jigsaw Puzzle Piece) 모양 아이콘 생성 (64 x 64 px)
        /// </summary>
        private static void GeneratePuzzlePieceSprite(string path)
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = 31.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inBody = (x >= 14 && x <= 49 && y >= 14 && y <= 49);
                    bool inTopKnob = Vector2.Distance(new Vector2(x, y), new Vector2(c, 52f)) <= 7.5f;
                    bool inRightKnob = Vector2.Distance(new Vector2(x, y), new Vector2(52f, c)) <= 7.5f;
                    bool inBottomHole = Vector2.Distance(new Vector2(x, y), new Vector2(c, 15f)) <= 6.2f;

                    bool inside = (inBody || inTopKnob || inRightKnob) && !inBottomHole;
                    if (!inside)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    bool border = (x <= 16 || x >= 47 || y <= 16 || y >= 47);
                    tex.SetPixel(x, y, border ? Color.white : new Color(0.92f, 0.92f, 0.92f, 1f));
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 2D Top-down RPG 캐릭터(머리, 모자/머리카락, 눈, 코트/망토) 스프라이트 생성 (64 x 64 px)
        /// </summary>
        private static void GenerateCharacterSprite(string path)
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float cx = 31.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float headDist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, 41f));
                    float bodyDist = Vector2.Distance(new Vector2((x - cx) * 0.85f, (y - 21f) * 1.1f), Vector2.zero);
                    float shadowDist = Vector2.Distance(new Vector2((x - cx) * 0.7f, (y - 9f) * 2.2f), Vector2.zero);

                    if (headDist <= 12.5f)
                    {
                        if (headDist >= 10.8f)
                        {
                            tex.SetPixel(x, y, new Color(0.15f, 0.18f, 0.25f, 1f)); // 외곽선
                        }
                        else if (y >= 43)
                        {
                            tex.SetPixel(x, y, new Color(0.75f, 0.82f, 0.95f, 1f)); // 모자/머리카락
                        }
                        else if ((Mathf.Abs(x - 27f) <= 1.6f || Mathf.Abs(x - 36f) <= 1.6f) && Mathf.Abs(y - 38f) <= 2.2f)
                        {
                            tex.SetPixel(x, y, new Color(0.12f, 0.15f, 0.22f, 1f)); // 눈
                        }
                        else
                        {
                            tex.SetPixel(x, y, new Color(1.0f, 0.94f, 0.88f, 1f)); // 얼굴
                        }
                    }
                    else if (bodyDist <= 13.5f && y <= 34)
                    {
                        if (bodyDist >= 11.8f)
                        {
                            tex.SetPixel(x, y, new Color(0.15f, 0.18f, 0.25f, 1f));
                        }
                        else if (Mathf.Abs(x - cx) <= 2.5f)
                        {
                            tex.SetPixel(x, y, new Color(1.0f, 0.90f, 0.55f, 1f)); // 스카프/넥타이 포인트
                        }
                        else
                        {
                            tex.SetPixel(x, y, new Color(0.88f, 0.92f, 0.98f, 1f)); // 망토/코트(색상은 SpriteRenderer.color로 틴트)
                        }
                    }
                    else if (shadowDist <= 11f)
                    {
                        tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0.35f)); // 발밑 그림자
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 위쪽 광장 중앙에 배치되는 고풍스러운 시계탑(11시 55분 시계판 포함) 스프라이트 (96 x 112 px)
        /// </summary>
        private static void GenerateClocktowerSprite(string path)
        {
            int w = 96;
            int h = 112;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float cx = 47.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // 뾰족한 지붕 (y: 78 ~ 108)
                    if (y >= 78 && y <= 108)
                    {
                        float halfWidth = (108 - y) * 1.15f;
                        if (Mathf.Abs(x - cx) <= halfWidth)
                        {
                            tex.SetPixel(x, y, new Color(0.22f, 0.32f, 0.52f, 1f));
                            continue;
                        }
                    }

                    // 시계탑 본체 기둥 (x: 20 ~ 76, y: 8 ~ 78)
                    if (x >= 20 && x <= 76 && y >= 8 && y <= 78)
                    {
                        float clockDist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, 54f));
                        if (clockDist <= 18f)
                        {
                            // 황금 테두리 시계판
                            if (clockDist >= 15f)
                            {
                                tex.SetPixel(x, y, new Color(0.92f, 0.76f, 0.30f, 1f));
                            }
                            else
                            {
                                // 11시 55분을 가리키는 시침/분침
                                bool hand = (Mathf.Abs(x - cx) <= 1.5f && y >= 54 && y <= 66) ||
                                            (Mathf.Abs((x - cx) + (y - 54) * 0.7f) <= 1.5f && y >= 54 && y <= 63);
                                tex.SetPixel(x, y, hand ? new Color(0.15f, 0.15f, 0.20f, 1f) : new Color(0.98f, 0.95f, 0.85f, 1f));
                            }
                            continue;
                        }

                        // 아치형 입구 문 (y: 8 ~ 28)
                        if (Mathf.Abs(x - cx) <= 10f && y <= 26)
                        {
                            tex.SetPixel(x, y, new Color(0.25f, 0.18f, 0.12f, 1f));
                            continue;
                        }

                        bool brick = (y % 8 == 0) || ((x + (y / 8) * 10) % 18 == 0);
                        tex.SetPixel(x, y, brick ? new Color(0.32f, 0.35f, 0.44f, 1f) : new Color(0.46f, 0.50f, 0.60f, 1f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 광장 주변에 배치되는 가로수/벚꽃나무 스프라이트 (64 x 72 px)
        /// </summary>
        private static void GenerateTreeSprite(string path)
        {
            int w = 64;
            int h = 72;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float cx = 31.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float c1 = Vector2.Distance(new Vector2(x, y), new Vector2(cx, 46f));
                    float c2 = Vector2.Distance(new Vector2(x, y), new Vector2(cx - 11f, 38f));
                    float c3 = Vector2.Distance(new Vector2(x, y), new Vector2(cx + 11f, 38f));

                    if (c1 <= 17f || c2 <= 14f || c3 <= 14f)
                    {
                        float shade = (y - 24f) / 40f;
                        Color leaf = Color.Lerp(new Color(0.18f, 0.42f, 0.32f, 1f), new Color(0.32f, 0.68f, 0.48f, 1f), Mathf.Clamp01(shade));
                        if ((x * 13 + y * 29) % 53 == 0)
                        {
                            leaf = new Color(0.98f, 0.72f, 0.88f, 1f); // 꽃잎 포인트
                        }
                        tex.SetPixel(x, y, leaf);
                    }
                    else if (Mathf.Abs(x - cx) <= 4.5f && y >= 8 && y <= 32)
                    {
                        tex.SetPixel(x, y, new Color(0.42f, 0.26f, 0.16f, 1f)); // 나무 줄기
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 따뜻한 불빛 후광이 비치는 가로등 프롭 스프라이트 (48 x 64 px)
        /// </summary>
        private static void GenerateStreetLampSprite(string path)
        {
            int w = 48;
            int h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float cx = 23.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float lanternDist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, 48f));
                    if (lanternDist <= 6.5f)
                    {
                        tex.SetPixel(x, y, new Color(1.0f, 0.92f, 0.55f, 1f)); // 등불 코어
                    }
                    else if (Mathf.Abs(x - cx) <= 2.0f && y >= 6 && y <= 46)
                    {
                        tex.SetPixel(x, y, new Color(0.25f, 0.28f, 0.35f, 1f)); // 가로등 기둥
                    }
                    else if (lanternDist <= 18f)
                    {
                        float alpha = (1f - lanternDist / 18f) * 0.38f;
                        tex.SetPixel(x, y, new Color(1.0f, 0.85f, 0.35f, alpha)); // 은은한 불빛 후광
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateSimpleSquare(string path)
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureSpriteImporter(string path, float ppu, FilterMode filterMode)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = ppu;
                importer.filterMode = filterMode;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
