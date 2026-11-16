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
    /// [2순위] 거점 '기억 수리점 공방(RepairShop_Space)' <-> '시계공의 기억 공간(MemoryMap_Space)' 및
    /// [1순위] 배경 복원(③) -> 과거의 기억 관람 -> 기억 순서 맞추기(①) -> 잘못된 기억 찾기(②)
    /// 전체 씬 구조를 자동 생성하는 에디터 빌더.
    /// </summary>
    [InitializeOnLoad]
    public static class Editor_RoomBuilder
    {
        private const string SpriteFolder = "Assets/Resources/GeneratedSprites";
        private const string AutoBuildKey = "FragmentRPG_Priority1And2_Built_v6";

        static Editor_RoomBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!SessionState.GetBool(AutoBuildKey, false))
                {
                    SessionState.SetBool(AutoBuildKey, true);
                    BuildDefaultRoomSceneInternal();
                }
            };
        }

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
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k cd /d \"{projectRoot}\" && echo [Fragment RPG] Staging, committing, and pushing to GitHub... && git add . && (git commit -m \"Update Fragment RPG project\" || echo No new changes to commit) && git push -u origin main",
                UseShellExecute = true
            });
        }

        private static void BuildDefaultRoomSceneInternal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            EnsureRichArtSprites();

            Sprite repairShopBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_RepairShopCanvas.png");
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

            if (Camera.main != null)
            {
                Camera.main.orthographic = true;
                Camera.main.orthographicSize = 5.4f;
                Camera.main.backgroundColor = new Color(0.05f, 0.06f, 0.10f, 1f);
                Camera.main.transform.position = new Vector3(0f, 0f, -10f);
            }

            ClearSceneExceptMainCamera();

            // 1. 매니저 오브젝트 생성
            GameObject managerRoot = new GameObject("@Managers");
            Undo.RegisterCreatedObjectUndo(managerRoot, "Create @Managers");

            InteractionManager interactionMgr = managerRoot.AddComponent<InteractionManager>();
            managerRoot.AddComponent<PuzzleManager>();
            managerRoot.AddComponent<StorySaveManager>();
            FragmentUIManager uiMgr = managerRoot.AddComponent<FragmentUIManager>();

            // 2. 플레이어 생성 (거점과 기억 공간 모두에서 활약하는 주인공 수리공)
            GameObject playerObj = new GameObject("Player");
            Undo.RegisterCreatedObjectUndo(playerObj, "Create Player");
            playerObj.transform.position = new Vector3(1.1f, -1.8f, 0f);
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

            // 3. [2순위] 거점 공간: 『기억 수리점 공방 (RepairShop_Space)』 생성
            GameObject repairShopRoot = BuildRepairShopSpace(
                repairShopBgSprite,
                charSprite,
                puzzlePieceSprite,
                lampSprite
            );

            // 4. [1순위 & 2순위] 손님의 기억 공간: 『시계공의 기억 공간 퍼즐맵 (MemoryMap_Space)』 생성
            GameObject memoryMapRoot = new GameObject("MemoryMap_Space");
            Undo.RegisterCreatedObjectUndo(memoryMapRoot, "Create MemoryMap_Space");

            SpriteRenderer mainFloorSr = BuildDetailedTownEnvironment(
                memoryMapRoot.transform,
                fullMapBgSprite,
                clocktowerSprite,
                treeSprite,
                lampSprite
            );

            // 4-1. 배경의 비어진 부분 3곳 (퍼즐 ③ 공간 복원 슬롯)
            GameObject slotsRoot = new GameObject("BackgroundPuzzleSlots");
            slotsRoot.transform.SetParent(memoryMapRoot.transform, false);

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

            // 4-2. 기억조각 1, 2, 3 아이템 배치
            GameObject clueParent = new GameObject("ClueFragments");
            clueParent.transform.SetParent(memoryMapRoot.transform, false);

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

            // 4-3. 기억 공간에서 수리점으로 돌아가는 귀환 포털
            CreatePortalObject(
                memoryMapRoot.transform,
                "Portal_ReturnToShop",
                new Vector3(5.3f, -3.1f, 0f),
                "PORTAL_TO_SHOP",
                "수리점 귀환 문 ([E/Space] 공방 복귀)",
                FragmentUIManager.SpaceRepairShop,
                new Color(0.45f, 0.95f, 1.0f, 0.95f),
                puzzlePieceSprite
            );

            // 4-4. [1순위 핵심] 배경 복원 완성 시 위쪽 광장에 나타나는 '과거의 기억 3장면 & 최종 수리 제단'
            GameObject pastMemoryRoot = BuildPastMemoryScene(memoryMapRoot.transform, charSprite, puzzlePieceSprite);
            pastMemoryRoot.SetActive(false);

            // 시작 시에는 거점(기억 수리점)이 활성화되고 기억 공간 맵은 비활성화
            repairShopRoot.SetActive(true);
            memoryMapRoot.SetActive(false);

            // 5. 인게임 UI Canvas 생성
            BuildPuzzleMapUICanvas(uiMgr, repairShopRoot, memoryMapRoot, pastMemoryRoot, mainFloorSr);

            Selection.activeGameObject = playerObj;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("[Editor_RoomBuilder] ★ 1순위(과거의 기억 후속 퍼즐 체인) & 2순위(기억 수리점 <-> 기억 공간 포털 루프) 구축이 완료되었습니다!");
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

        /// <summary>
        /// [2순위] 주인공의 거점 공간: 『기억 수리점 공방 (RepairShop_Space)』 구축
        /// </summary>
        private static GameObject BuildRepairShopSpace(
            Sprite shopBgSprite,
            Sprite charSprite,
            Sprite watchPortalSprite,
            Sprite lampSprite)
        {
            GameObject shopRoot = new GameObject("RepairShop_Space");
            Undo.RegisterCreatedObjectUndo(shopRoot, "Create RepairShop_Space");

            // 수리점 실내 배경 캔버스
            GameObject shopBg = new GameObject("RepairShopInterior");
            shopBg.transform.SetParent(shopRoot.transform, false);
            shopBg.transform.position = new Vector3(1.1f, 0.0f, 0f);
            SpriteRenderer bgSr = shopBg.AddComponent<SpriteRenderer>();
            bgSr.sprite = shopBgSprite;
            bgSr.sortingOrder = -10;

            CreateWorldLabel(shopRoot.transform, "『기억 수리점 공방』 — 의뢰인(시계공)과 대화 후 중앙 작업대의 [멈춘 회중시계]로 기억 공간에 입장하세요", new Vector3(1.1f, 3.4f, 0f), new Color(1f, 0.90f, 0.55f, 0.95f), 0.065f);

            // 실내 조명 가로등 장식
            CreateEnvironmentProp(shopRoot.transform, "ShopLamp_Left", new Vector3(-2.8f, 2.2f, 0f), lampSprite, 4);
            CreateEnvironmentProp(shopRoot.transform, "ShopLamp_Right", new Vector3(5.0f, 2.2f, 0f), lampSprite, 4);

            // 의뢰인 NPC: 시계공 노인 (의뢰 전 대사 & 기억 수리 완료 후 에필로그 대사 보유!)
            GameObject clientNpc = new GameObject("NPC_Client_Clockmaker");
            clientNpc.transform.SetParent(shopRoot.transform, false);
            clientNpc.transform.position = new Vector3(-1.5f, 0.4f, 0f);
            clientNpc.transform.localScale = new Vector3(1.0f, 1.0f, 1f);

            SpriteRenderer npcSr = clientNpc.AddComponent<SpriteRenderer>();
            npcSr.sprite = charSprite;
            npcSr.color = new Color(1.0f, 0.78f, 0.38f, 1.0f);
            npcSr.sortingOrder = 12;

            CircleCollider2D npcCol = clientNpc.AddComponent<CircleCollider2D>();
            npcCol.isTrigger = false;
            npcCol.radius = 0.48f;

            InteractableObject npcInteract = clientNpc.AddComponent<InteractableObject>();
            npcInteract.SetupNPC(
                "NPC_SHOP_CLOCKMAKER",
                "의뢰인: 시계공 노인",
                "DLG_SHOP_EP1",
                new string[]
                {
                    "\"수리공 양반, 이 회중시계가 11시 55분에서 도통 움직이질 않네.\"",
                    "\"그날 아침 9시에 내가 시계탑 문을 열고, 정오(12:00)에 시계탑 종을 정상적으로 울린 뒤에 딸을 배웅하러 갔는데 말이야...\"",
                    "\"오른쪽 수리 작업대 위에 놓인 [멈춘 회중시계]를 조사해 내 기억 공간으로 들어가 수리해 주게!\""
                },
                new string[]
                {
                    "\"오오...! 11시 55분에 멈춰 있던 회중시계 태엽이 다시 힘차게 돌아가고 있어!\"",
                    "\"맞아... 정오에 종을 울렸다는 건 내 착각이었어. 떠나는 딸의 기차를 붙잡고 싶어서 11시 55분에 내 손으로 태엽을 멈췄던 거였지.\"",
                    "\"고맙네, 기억 수리공 양반! 덕분에 딸아이와의 가장 소중한 기억을 온전히 되찾았어! (Ep.1 클리어 완료!)\""
                }
            );
            CreateWorldLabel(clientNpc.transform, "★ 의뢰인: 시계공 노인\n([E/Space] 대화)", new Vector3(0f, 0.82f, 0f), new Color(1f, 0.88f, 0.45f, 1f), 0.065f);

            // 중앙 수리 작업대 위 '멈춘 회중시계 (기억 공간 입장 포털)'
            CreatePortalObject(
                shopRoot.transform,
                "Portal_BrokenPocketWatch",
                new Vector3(2.2f, 0.4f, 0f),
                "PORTAL_TO_MEMORY_MAP",
                "멈춘 회중시계 ([E/Space] 기억 공간 입장)",
                FragmentUIManager.SpaceMemoryMap,
                new Color(0.55f, 0.95f, 1.0f, 1.0f),
                watchPortalSprite
            );

            // 공방 외벽 콜라이더
            CreateInvisibleWall(shopRoot.transform, "ShopBound_Top", new Vector3(1.1f, 3.5f, 0f), new Vector2(11.2f, 0.5f));
            CreateInvisibleWall(shopRoot.transform, "ShopBound_Bottom", new Vector3(1.1f, -3.5f, 0f), new Vector2(11.2f, 0.5f));
            CreateInvisibleWall(shopRoot.transform, "ShopBound_Left", new Vector3(-4.1f, 0f, 0f), new Vector2(0.5f, 7.5f));
            CreateInvisibleWall(shopRoot.transform, "ShopBound_Right", new Vector3(6.3f, 0f, 0f), new Vector2(0.5f, 7.5f));

            return shopRoot;
        }

        private static void CreatePortalObject(
            Transform parent,
            string objectName,
            Vector3 position,
            string portalId,
            string portalLabel,
            string destinationSpaceId,
            Color tintColor,
            Sprite sprite)
        {
            GameObject portalObj = new GameObject(objectName);
            portalObj.transform.SetParent(parent, false);
            portalObj.transform.position = position;
            portalObj.transform.localScale = new Vector3(1.05f, 1.05f, 1f);

            SpriteRenderer sr = portalObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tintColor;
            sr.sortingOrder = 12;

            CircleCollider2D col = portalObj.AddComponent<CircleCollider2D>();
            col.isTrigger = false;
            col.radius = 0.5f;

            InteractableObject interactable = portalObj.AddComponent<InteractableObject>();
            interactable.SetupSpacePortal(portalId, portalLabel, destinationSpaceId);

            CreateWorldLabel(portalObj.transform, $"◆ {portalLabel}", new Vector3(0f, 0.82f, 0f), tintColor, 0.065f);
        }

        private static SpriteRenderer BuildDetailedTownEnvironment(
            Transform parent,
            Sprite fullMapBgSprite,
            Sprite clocktowerSprite,
            Sprite treeSprite,
            Sprite lampSprite)
        {
            GameObject roomRoot = new GameObject("RoomEnvironment");
            roomRoot.transform.SetParent(parent, false);

            GameObject mapCanvas = new GameObject("TownMapBackground");
            mapCanvas.transform.SetParent(roomRoot.transform, false);
            mapCanvas.transform.position = new Vector3(1.1f, 0.0f, 0f);
            SpriteRenderer mapSr = mapCanvas.AddComponent<SpriteRenderer>();
            mapSr.sprite = fullMapBgSprite;
            mapSr.color = Color.white;
            mapSr.sortingOrder = -10;

            GameObject clocktower = new GameObject("Prop_GrandClocktower");
            clocktower.transform.SetParent(roomRoot.transform, false);
            clocktower.transform.position = new Vector3(1.1f, 3.15f, 0f);
            SpriteRenderer towerSr = clocktower.AddComponent<SpriteRenderer>();
            towerSr.sprite = clocktowerSprite;
            towerSr.sortingOrder = 4;
            BoxCollider2D towerCol = clocktower.AddComponent<BoxCollider2D>();
            towerCol.size = new Vector2(1.2f, 0.9f);

            CreateEnvironmentProp(roomRoot.transform, "Tree_UpperLeft", new Vector3(-3.3f, 3.0f, 0f), treeSprite, 3);
            CreateEnvironmentProp(roomRoot.transform, "Tree_UpperRight", new Vector3(5.5f, 3.0f, 0f), treeSprite, 3);
            CreateEnvironmentProp(roomRoot.transform, "Tree_LowerLeft", new Vector3(-3.5f, -3.1f, 0f), treeSprite, 3);
            CreateEnvironmentProp(roomRoot.transform, "Tree_LowerRight", new Vector3(5.7f, -3.1f, 0f), treeSprite, 3);

            CreateEnvironmentProp(roomRoot.transform, "Lamp_UpperLeft", new Vector3(-0.6f, 2.75f, 0f), lampSprite, 4);
            CreateEnvironmentProp(roomRoot.transform, "Lamp_UpperRight", new Vector3(2.8f, 2.75f, 0f), lampSprite, 4);
            CreateEnvironmentProp(roomRoot.transform, "Lamp_LowerLeft", new Vector3(-0.6f, -1.55f, 0f), lampSprite, 4);
            CreateEnvironmentProp(roomRoot.transform, "Lamp_LowerRight", new Vector3(2.8f, -1.55f, 0f), lampSprite, 4);

            CreateInvisibleWall(roomRoot.transform, "Bound_Top", new Vector3(1.1f, 3.95f, 0f), new Vector2(11.2f, 0.4f));
            CreateInvisibleWall(roomRoot.transform, "Bound_Bottom", new Vector3(1.1f, -3.95f, 0f), new Vector2(11.2f, 0.4f));
            CreateInvisibleWall(roomRoot.transform, "Bound_Left", new Vector3(-4.35f, 0f, 0f), new Vector2(0.4f, 8.2f));
            CreateInvisibleWall(roomRoot.transform, "Bound_Right", new Vector3(6.55f, 0f, 0f), new Vector2(0.4f, 8.2f));

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
            slotObj.transform.localScale = Vector3.one;

            SpriteRenderer sr = slotObj.AddComponent<SpriteRenderer>();
            sr.sprite = emptyVoidSprite;
            sr.color = Color.white;
            sr.sortingOrder = -5;

            BoxCollider2D col = slotObj.AddComponent<BoxCollider2D>();
            col.isTrigger = false;
            col.size = new Vector2(2.75f, 1.9f);

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

        /// <summary>
        /// [1순위 핵심] 배경 복원 후 나타나는 과거의 기억 3장면(09:00 / 11:30 / 11:55)과
        /// 중앙 '최종 기억 수리 제단(순서 맞추기 + 모순 찾기 퍼즐 호출)'
        /// </summary>
        private static GameObject BuildPastMemoryScene(Transform parent, Sprite charSprite, Sprite crystalSprite)
        {
            GameObject memoryRoot = new GameObject("PastMemoryScene");
            memoryRoot.transform.SetParent(parent, false);

            // 과거의 기억 장면 1 (오전 09:00)
            CreateMemoryNPC(
                memoryRoot.transform,
                "Memory_0900_Morning",
                new Vector3(-2.1f, 2.15f, 0f),
                "MEM_NPC_0900",
                "과거 기억 [09:00]",
                "DLG_PAST_0900",
                new string[]
                {
                    "[과거의 기억 — 오전 09:00] 아침 햇살 속에서 시계공이 시계탑 문을 활짝 열고 하루의 태엽을 감기 시작했습니다.",
                    "시계공: \"오늘도 정오에 정확한 종소리를 울려야지.\""
                },
                new Color(1.0f, 0.88f, 0.42f, 0.95f),
                charSprite
            );

            // 과거의 기억 장면 2 (오전 11:30)
            CreateMemoryNPC(
                memoryRoot.transform,
                "Memory_1130_Letter",
                new Vector3(4.3f, 2.15f, 0f),
                "MEM_NPC_1130",
                "과거 기억 [11:30]",
                "DLG_PAST_1130",
                new string[]
                {
                    "[과거의 기억 — 오전 11:30] 시계공이 작업대 위에서 '정오(12:00) 기차로 멀리 떠난다'는 딸의 편지를 뒤늦게 발견했습니다!",
                    "시계공: \"정오 기차라니...! 지금 당장 가지 않으면 딸아이를 영영 배웅하지 못해!\""
                },
                new Color(0.65f, 0.95f, 1.0f, 0.95f),
                charSprite
            );

            // 과거의 기억 장면 3 (오전 11:55 - 결정적 모순 단서!)
            CreateMemoryNPC(
                memoryRoot.transform,
                "Memory_1155_StopPin",
                new Vector3(-0.5f, 1.85f, 0f),
                "MEM_NPC_1155",
                "과거 기억 [11:55]",
                "DLG_PAST_1155",
                new string[]
                {
                    "[과거의 기억 — 오전 11:55] 정오가 되기 5분 전, 시계공은 딸이 탄 기차가 떠나지 않기를 바라며 스스로 시계탑 주 태엽에 '멈춤핀'을 꽂아 시계를 멈춰 세웠습니다!",
                    "(단서 발견: 시계공은 정오에 종을 울린 적이 없으며, 11시 55분에 스스로 시계를 멈췄습니다!)"
                },
                new Color(1.0f, 0.62f, 0.88f, 0.95f),
                charSprite
            );

            // 중앙 최종 기억 수리 제단 (퍼즐 ① 기억 순서 맞추기 -> 퍼즐 ② 잘못된 기억 찾기 연속 호출!)
            GameObject altarObj = new GameObject("Altar_FinalMemoryRepair");
            altarObj.transform.SetParent(memoryRoot.transform, false);
            altarObj.transform.position = new Vector3(1.8f, 1.95f, 0f);
            altarObj.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

            SpriteRenderer altarSr = altarObj.AddComponent<SpriteRenderer>();
            altarSr.sprite = crystalSprite;
            altarSr.color = new Color(0.50f, 1.0f, 0.78f, 1.0f);
            altarSr.sortingOrder = 14;

            CircleCollider2D altarCol = altarObj.AddComponent<CircleCollider2D>();
            altarCol.isTrigger = false;
            altarCol.radius = 0.52f;

            InteractableObject altarInteract = altarObj.AddComponent<InteractableObject>();
            altarInteract.SetupPuzzleTrigger(
                "ALTAR_EP1_FINAL",
                "기억 수리 제단 (① 순서 맞추기 & ② 모순 찾기)",
                PuzzleManager.OrderPuzzleId,
                PuzzleType.ChronologicalOrder
            );

            CreateWorldLabel(altarObj.transform, "★ [기억 수리 제단]\n([E/Space] 최종 퍼즐 풀기)", new Vector3(0f, 0.82f, 0f), new Color(0.50f, 1.0f, 0.78f, 1f), 0.065f);

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
            npcObj.transform.localScale = new Vector3(0.92f, 0.92f, 1f);

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
            GameObject repairShopRoot,
            GameObject memoryMapRoot,
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

            // 상단 바 (현재 공간 표시 + 거점/기억공간 이동 버튼 + 리셋 버튼)
            GameObject topBar = CreateUIPanel(canvasObj.transform, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -46f), Vector2.zero, new Color(0.07f, 0.09f, 0.14f, 0.94f));
            Text headerTitleText = CreateUIText(topBar.transform, "HeaderTitleText", "[거점: 기억 수리점 공방]", defaultFont, 14, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 0f), new Vector2(0.64f, 1f), new Vector2(16f, 0f), Vector2.zero);

            GameObject shopBtnObj = CreateUIPanel(topBar.transform, "Btn_GoShop", new Vector2(0.65f, 0.15f), new Vector2(0.755f, 0.85f), Vector2.zero, Vector2.zero, new Color(0.38f, 0.28f, 0.18f, 1f));
            Button shopBtn = shopBtnObj.AddComponent<Button>();
            shopBtn.targetGraphic = shopBtnObj.GetComponent<Image>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(shopBtn.onClick, uiMgr.EnterRepairShop);
            CreateUIText(shopBtnObj.transform, "Text", "거점: 기억 수리점", defaultFont, 12, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject mapBtnObj = CreateUIPanel(topBar.transform, "Btn_GoMemoryMap", new Vector2(0.765f, 0.15f), new Vector2(0.87f, 0.85f), Vector2.zero, Vector2.zero, new Color(0.20f, 0.36f, 0.56f, 1f));
            Button mapBtn = mapBtnObj.AddComponent<Button>();
            mapBtn.targetGraphic = mapBtnObj.GetComponent<Image>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(mapBtn.onClick, uiMgr.EnterMemoryMap);
            CreateUIText(mapBtnObj.transform, "Text", "기억 공간: 퍼즐맵", defaultFont, 12, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject resetBtnObj = CreateUIPanel(topBar.transform, "Btn_ResetMap", new Vector2(0.88f, 0.15f), new Vector2(0.985f, 0.85f), Vector2.zero, Vector2.zero, new Color(0.55f, 0.25f, 0.25f, 1f));
            Button resetBtn = resetBtnObj.AddComponent<Button>();
            resetBtn.targetGraphic = resetBtnObj.GetComponent<Image>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(resetBtn.onClick, uiMgr.ResetPuzzleMap);
            CreateUIText(resetBtnObj.transform, "Text", "처음부터 리셋", defaultFont, 12, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // 좌측 인벤토리 및 3대 퍼즐 체크리스트 현황판
            GameObject leftPanel = CreateUIPanel(canvasObj.transform, "InventoryPanel", new Vector2(0f, 0.26f), new Vector2(0.21f, 0.91f), new Vector2(10f, 0f), Vector2.zero, new Color(0.08f, 0.11f, 0.18f, 0.88f));
            Text inventoryText = CreateUIText(leftPanel.transform, "InventoryStatusText", "[진행 현황]", defaultFont, 13, TextAnchor.UpperLeft, Color.white, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));

            // 하단 상호작용 안내 프롬프트
            GameObject promptBar = CreateUIPanel(canvasObj.transform, "PromptBar", new Vector2(0.23f, 0.015f), new Vector2(0.98f, 0.07f), Vector2.zero, Vector2.zero, new Color(0.07f, 0.09f, 0.14f, 0.90f));
            Text promptText = CreateUIText(promptBar.transform, "PromptText", "상호작용 안내", defaultFont, 14, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.35f, 1f), Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));

            // 하단 대화창
            GameObject dialogPanel = CreateUIPanel(canvasObj.transform, "DialogPanel", new Vector2(0.23f, 0.08f), new Vector2(0.98f, 0.22f), Vector2.zero, Vector2.zero, new Color(0.10f, 0.13f, 0.22f, 0.95f));
            Text dialogSpeakerText = CreateUIText(dialogPanel.transform, "DialogSpeakerText", "알림", defaultFont, 15, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f, 1f), new Vector2(0f, 0.64f), new Vector2(1f, 1f), new Vector2(14f, 0f), new Vector2(-14f, -2f));
            Text dialogBodyText = CreateUIText(dialogPanel.transform, "DialogBodyText", "내용", defaultFont, 14, TextAnchor.UpperLeft, Color.white, new Vector2(0f, 0f), new Vector2(1f, 0.64f), new Vector2(14f, 4f), new Vector2(-14f, -2f));
            dialogPanel.SetActive(false);

            // 중앙 퍼즐 모달 창 (③ 배경 조각 끼우기 / ① 기억 순서 맞추기 / ② 잘못된 기억 찾기 공용)
            GameObject slotChoicePanel = CreateUIPanel(canvasObj.transform, "SlotChoicePanel", new Vector2(0.26f, 0.24f), new Vector2(0.96f, 0.89f), Vector2.zero, Vector2.zero, new Color(0.09f, 0.12f, 0.20f, 0.98f));
            Text choiceTitle = CreateUIText(slotChoicePanel.transform, "ChoiceTitleText", "퍼즐 제목", defaultFont, 17, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.35f, 1f), new Vector2(0f, 0.85f), new Vector2(1f, 0.98f), new Vector2(16f, 0f), new Vector2(-16f, 0f));
            Text choiceDesc = CreateUIText(slotChoicePanel.transform, "ChoiceDescText", "퍼즐 설명", defaultFont, 13, TextAnchor.MiddleCenter, new Color(0.85f, 0.9f, 1f, 1f), new Vector2(0f, 0.72f), new Vector2(1f, 0.85f), new Vector2(16f, 0f), new Vector2(-16f, 0f));
            Text choiceResult = CreateUIText(slotChoicePanel.transform, "ChoiceResultText", "퍼즐 상태", defaultFont, 14, TextAnchor.MiddleCenter, Color.cyan, new Vector2(0f, 0.59f), new Vector2(1f, 0.72f), new Vector2(16f, 0f), new Vector2(-16f, 0f));

            GameObject btnContainer = new GameObject("ChoiceButtonContainer");
            btnContainer.transform.SetParent(slotChoicePanel.transform, false);
            RectTransform containerRt = btnContainer.AddComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.06f, 0.05f);
            containerRt.anchorMax = new Vector2(0.94f, 0.57f);
            containerRt.offsetMin = Vector2.zero;
            containerRt.offsetMax = Vector2.zero;
            VerticalLayoutGroup vlg = btnContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
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
                repairShopRoot,
                memoryMapRoot,
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

        private static void EnsureRichArtSprites()
        {
            if (!Directory.Exists(SpriteFolder))
            {
                Directory.CreateDirectory(SpriteFolder);
            }

            GenerateRepairShopInteriorCanvas($"{SpriteFolder}/Spr_RepairShopCanvas.png");
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

            ConfigureSpriteImporter($"{SpriteFolder}/Spr_RepairShopCanvas.png", 48f, FilterMode.Point);
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
        /// [2순위] 거점 '기억 수리점 공방' 실내 배경 생성 (528 x 384 px, PPU 48):
        /// 따뜻한 원목 마룻바닥, 앤티크 붉은 양탄자, 상단 고서적 책장/벽시계 선반, 중앙 수리 작업대 테이블을 그립니다.
        /// </summary>
        private static void GenerateRepairShopInteriorCanvas(string path)
        {
            int w = 528;
            int h = 384;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // 외곽 공방 나무 벽 테두리
                    if (x < 18 || x >= w - 18 || y < 18 || y >= h - 18)
                    {
                        bool beam = (x % 32 == 0) || (y % 32 == 0);
                        tex.SetPixel(x, y, beam ? new Color(0.18f, 0.12f, 0.08f, 1f) : new Color(0.28f, 0.19f, 0.12f, 1f));
                        continue;
                    }

                    // 상단 벽면 & 책장 선반 구역 (y >= 296)
                    if (y >= 296)
                    {
                        bool shelf = (y % 22 < 4);
                        if (shelf)
                        {
                            tex.SetPixel(x, y, new Color(0.35f, 0.22f, 0.12f, 1f));
                        }
                        else if (x >= 60 && x <= w - 60 && (x % 14 < 10))
                        {
                            // 알록달록한 기억 보관병 & 고서적 책등
                            int bookTone = (x / 14 + y / 22) % 4;
                            Color bookCol = bookTone == 0 ? new Color(0.65f, 0.25f, 0.25f, 1f) :
                                            bookTone == 1 ? new Color(0.25f, 0.48f, 0.62f, 1f) :
                                            bookTone == 2 ? new Color(0.28f, 0.55f, 0.36f, 1f) :
                                                            new Color(0.75f, 0.62f, 0.28f, 1f);
                            tex.SetPixel(x, y, bookCol);
                        }
                        else
                        {
                            tex.SetPixel(x, y, new Color(0.22f, 0.16f, 0.12f, 1f));
                        }
                        continue;
                    }

                    // 중앙 수리 작업대 카운터 테이블 (x: 300~410, y: 185~235)
                    if (x >= 300 && x <= 410 && y >= 185 && y <= 235)
                    {
                        bool tableBorder = (x < 305 || x > 405 || y < 190 || y > 230);
                        tex.SetPixel(x, y, tableBorder ? new Color(0.32f, 0.20f, 0.10f, 1f) : new Color(0.52f, 0.35f, 0.20f, 1f));
                        continue;
                    }

                    // 중앙 앤티크 자수 카펫 러그 (x: 110~418, y: 80~255)
                    if (x >= 110 && x <= 418 && y >= 80 && y <= 255)
                    {
                        bool rugBorder = (x < 120 || x > 408 || y < 90 || y > 245);
                        if (rugBorder)
                        {
                            tex.SetPixel(x, y, new Color(0.85f, 0.72f, 0.32f, 1f)); // 황금 자수 테두리
                        }
                        else
                        {
                            bool pattern = ((x + y) % 16 == 0) || ((x - y + 512) % 16 == 0);
                            tex.SetPixel(x, y, pattern ? new Color(0.48f, 0.20f, 0.24f, 1f) : new Color(0.38f, 0.15f, 0.19f, 1f));
                        }
                        continue;
                    }

                    // 따뜻한 오크 원목 마룻바닥 패턴
                    bool plankLine = (y % 16 == 0) || ((x + (y / 16) * 28) % 56 == 0);
                    float grain = ((x * 5 + y * 9) % 13) * 0.005f;
                    Color woodFloor = plankLine
                        ? new Color(0.22f, 0.15f, 0.10f, 1f)
                        : new Color(0.34f + grain, 0.24f + grain, 0.16f + grain, 1f);
                    tex.SetPixel(x, y, woodFloor);
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateFullTownBackgroundCanvas(string path)
        {
            if (File.Exists(path)) return;

            int w = 528;
            int h = 384;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            int riverMinY = 152;
            int riverMaxY = 248;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
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

                    if (y >= riverMinY && y <= riverMaxY)
                    {
                        if (y < riverMinY + 8 || y > riverMaxY - 8)
                        {
                            bool stoneJoint = (x % 24 == 0);
                            tex.SetPixel(x, y, stoneJoint ? new Color(0.25f, 0.28f, 0.36f, 1f) : new Color(0.45f, 0.50f, 0.60f, 1f));
                        }
                        else
                        {
                            float wave = Mathf.Sin((x * 0.08f) + (y * 0.15f)) * 0.5f + 0.5f;
                            Color waterCol = Color.Lerp(new Color(0.10f, 0.28f, 0.48f, 1f), new Color(0.18f, 0.44f, 0.68f, 1f), wave);
                            if ((x + y * 2) % 37 == 0 && wave > 0.7f)
                            {
                                waterCol = new Color(0.45f, 0.75f, 0.95f, 1f);
                            }
                            tex.SetPixel(x, y, waterCol);
                        }
                        continue;
                    }

                    if (y < riverMinY)
                    {
                        bool onLeftPath = Mathf.Abs(x - 110) < 38 && y >= 65;
                        bool onCenterPath = Mathf.Abs(x - 264) < 42 && y >= 45;
                        bool onRightPath = Mathf.Abs(x - 418) < 38 && y >= 65;
                        bool onCrossPath = (y >= 62 && y <= 102 && x >= 72 && x <= 456);

                        if (onLeftPath || onCenterPath || onRightPath || onCrossPath)
                        {
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
                            float grassNoise = ((x * 19 + y * 31) % 23) / 23f;
                            Color grass = Color.Lerp(new Color(0.14f, 0.30f, 0.24f, 1f), new Color(0.19f, 0.38f, 0.30f, 1f), grassNoise);
                            if ((x * 11 + y * 29) % 197 == 0)
                            {
                                grass = new Color(0.95f, 0.85f, 0.35f, 1f);
                            }
                            else if ((x * 23 + y * 17) % 241 == 0)
                            {
                                grass = new Color(0.95f, 0.55f, 0.80f, 1f);
                            }
                            tex.SetPixel(x, y, grass);
                        }
                        continue;
                    }

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

        private static void GenerateVoidSlotSprite(string path)
        {
            if (File.Exists(path)) return;
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

                    float nx = (x - w * 0.5f) / (w * 0.5f);
                    float ny = (y - h * 0.5f) / (h * 0.5f);
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    Color voidCol = Color.Lerp(new Color(0.05f, 0.04f, 0.11f, 0.98f), new Color(0.14f, 0.09f, 0.24f, 0.96f), Mathf.Clamp01(r));

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

        private static void GenerateRestoredSlotSprite(string path, int slotType)
        {
            if (File.Exists(path)) return;
            int w = 132;
            int h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x == 0 || x == w - 1 || y == 0 || y == h - 1)
                    {
                        tex.SetPixel(x, y, new Color(0.95f, 0.85f, 0.45f, 0.85f));
                        continue;
                    }

                    bool onBridge = (x >= 24 && x <= w - 24);
                    bool onBridgeRail = (x >= 20 && x < 24) || (x > w - 24 && x <= w - 20);

                    if (onBridgeRail)
                    {
                        bool post = (y % 18 < 6);
                        tex.SetPixel(x, y, post ? new Color(0.78f, 0.72f, 0.58f, 1f) : new Color(0.52f, 0.48f, 0.42f, 1f));
                        continue;
                    }

                    if (onBridge)
                    {
                        if (slotType == 1)
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
                        else if (slotType == 2)
                        {
                            bool marbleGrid = (y % 12 == 0) || (x % 16 == 0);
                            Color marble = marbleGrid ? new Color(0.35f, 0.42f, 0.48f, 1f) : new Color(0.52f, 0.60f, 0.66f, 1f);
                            if (x >= 48 && x <= w - 48)
                            {
                                marble = (x == 48 || x == w - 48)
                                    ? new Color(0.95f, 0.82f, 0.35f, 1f)
                                    : new Color(0.28f, 0.58f, 0.48f, 1f);
                            }
                            tex.SetPixel(x, y, marble);
                        }
                        else
                        {
                            bool stone = (y % 10 == 0) || ((x + (y / 10) * 8) % 16 == 0);
                            Color pathCol = stone ? new Color(0.34f, 0.28f, 0.38f, 1f) : new Color(0.50f, 0.42f, 0.54f, 1f);
                            if (x >= 78 && x <= 98 && y >= 36 && y <= 64)
                            {
                                pathCol = (y % 6 == 0) ? new Color(0.35f, 0.20f, 0.12f, 1f) : new Color(0.62f, 0.38f, 0.22f, 1f);
                            }
                            else if ((x * 17 + y * 29) % 43 == 0)
                            {
                                pathCol = new Color(1.0f, 0.68f, 0.86f, 1f);
                            }
                            tex.SetPixel(x, y, pathCol);
                        }
                    }
                    else
                    {
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

        private static void GeneratePuzzlePieceSprite(string path)
        {
            if (File.Exists(path)) return;
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

        private static void GenerateCharacterSprite(string path)
        {
            if (File.Exists(path)) return;
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
                            tex.SetPixel(x, y, new Color(0.15f, 0.18f, 0.25f, 1f));
                        }
                        else if (y >= 43)
                        {
                            tex.SetPixel(x, y, new Color(0.75f, 0.82f, 0.95f, 1f));
                        }
                        else if ((Mathf.Abs(x - 27f) <= 1.6f || Mathf.Abs(x - 36f) <= 1.6f) && Mathf.Abs(y - 38f) <= 2.2f)
                        {
                            tex.SetPixel(x, y, new Color(0.12f, 0.15f, 0.22f, 1f));
                        }
                        else
                        {
                            tex.SetPixel(x, y, new Color(1.0f, 0.94f, 0.88f, 1f));
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
                            tex.SetPixel(x, y, new Color(1.0f, 0.90f, 0.55f, 1f));
                        }
                        else
                        {
                            tex.SetPixel(x, y, new Color(0.88f, 0.92f, 0.98f, 1f));
                        }
                    }
                    else if (shadowDist <= 11f)
                    {
                        tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0.35f));
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

        private static void GenerateClocktowerSprite(string path)
        {
            if (File.Exists(path)) return;
            int w = 96;
            int h = 112;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float cx = 47.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (y >= 78 && y <= 108)
                    {
                        float halfWidth = (108 - y) * 1.15f;
                        if (Mathf.Abs(x - cx) <= halfWidth)
                        {
                            tex.SetPixel(x, y, new Color(0.22f, 0.32f, 0.52f, 1f));
                            continue;
                        }
                    }

                    if (x >= 20 && x <= 76 && y >= 8 && y <= 78)
                    {
                        float clockDist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, 54f));
                        if (clockDist <= 18f)
                        {
                            if (clockDist >= 15f)
                            {
                                tex.SetPixel(x, y, new Color(0.92f, 0.76f, 0.30f, 1f));
                            }
                            else
                            {
                                bool hand = (Mathf.Abs(x - cx) <= 1.5f && y >= 54 && y <= 66) ||
                                            (Mathf.Abs((x - cx) + (y - 54) * 0.7f) <= 1.5f && y >= 54 && y <= 63);
                                tex.SetPixel(x, y, hand ? new Color(0.15f, 0.15f, 0.20f, 1f) : new Color(0.98f, 0.95f, 0.85f, 1f));
                            }
                            continue;
                        }

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

        private static void GenerateTreeSprite(string path)
        {
            if (File.Exists(path)) return;
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
                            leaf = new Color(0.98f, 0.72f, 0.88f, 1f);
                        }
                        tex.SetPixel(x, y, leaf);
                    }
                    else if (Mathf.Abs(x - cx) <= 4.5f && y >= 8 && y <= 32)
                    {
                        tex.SetPixel(x, y, new Color(0.42f, 0.26f, 0.16f, 1f));
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

        private static void GenerateStreetLampSprite(string path)
        {
            if (File.Exists(path)) return;
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
                        tex.SetPixel(x, y, new Color(1.0f, 0.92f, 0.55f, 1f));
                    }
                    else if (Mathf.Abs(x - cx) <= 2.0f && y >= 6 && y <= 46)
                    {
                        tex.SetPixel(x, y, new Color(0.25f, 0.28f, 0.35f, 1f));
                    }
                    else if (lanternDist <= 18f)
                    {
                        float alpha = (1f - lanternDist / 18f) * 0.38f;
                        tex.SetPixel(x, y, new Color(1.0f, 0.85f, 0.35f, alpha));
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
            if (File.Exists(path)) return;
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
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }
                if (Mathf.Abs(importer.spritePixelsPerUnit - ppu) > 0.01f)
                {
                    importer.spritePixelsPerUnit = ppu;
                    changed = true;
                }
                if (importer.filterMode != filterMode)
                {
                    importer.filterMode = filterMode;
                    changed = true;
                }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                }
                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
#endif
