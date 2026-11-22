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
    /// 확장된 맵 크기(18 x 13 월드 유닛)와 스테이지별 고유 고해상도 2D 아트 배경
    /// (거점 대공방 / Ep.1 시계탑 대광장 / Ep.2 달빛 화실 대정원 / Ep.3 별빛 기억 대성소)을
    /// 자동 생성 및 배치하는 에디터 빌더.
    /// </summary>
    [InitializeOnLoad]
    public static class Editor_RoomBuilder
    {
        private const string SpriteFolder = "Assets/Resources/GeneratedSprites";
        private const string AutoBuildKey = "FragmentRPG_LivingRoomSubwayFusion_v11";

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
                Arguments = $"/k cd /d \"{projectRoot}\" && echo [Fragment RPG] Staging, committing, and pushing to GitHub... && git add . && (git commit -m \"Update Fragment RPG project\" || echo No new changes to commit) && git push -u origin main --force",
                UseShellExecute = true
            });
        }

        private static void BuildDefaultRoomSceneInternal()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            EnsureRichArtSprites();

            Sprite repairShopBg = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_RepairShop_2DRPG.png");
            Sprite stage1Bg = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_Map1_LivingRoom.png");
            Sprite stage2Bg = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_Map2_SubwayStation.png");
            Sprite stage3Bg = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_Map3_MemoryFusion.png");

            Sprite voidSlotSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_VoidSlot.png");
            // null을 전달하면 복원 시 가림막이 사라지며 아래의 실제 2D RPG 맵 배경이 이음새 없이 완벽히 드러납니다!
            Sprite restoredLeftSprite = null;
            Sprite restoredCenterSprite = null;
            Sprite restoredRightSprite = null;

            Sprite puzzlePieceSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_PuzzlePiece.png");
            Sprite charSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_Character.png");
            Sprite clocktowerSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_Clocktower.png");
            Sprite treeSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_Tree.png");
            Sprite lampSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_StreetLamp.png");
            Sprite fountainSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_Fountain_HQ.png");
            Sprite easelSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/Spr_Easel_HQ.png");

            if (Camera.main != null)
            {
                Camera.main.orthographic = true;
                Camera.main.orthographicSize = 4.6f; // 넓어진 맵을 카메라가 부드럽게 추적하도록 설정
                Camera.main.backgroundColor = new Color(0.04f, 0.05f, 0.09f, 1f);
                Camera.main.transform.position = new Vector3(0.3f, -1.2f, -10f);
            }

            ClearSceneExceptMainCamera();

            // 1. 매니저 그룹
            GameObject managerRoot = new GameObject("@Managers");
            Undo.RegisterCreatedObjectUndo(managerRoot, "Create @Managers");

            InteractionManager interactionMgr = managerRoot.AddComponent<InteractionManager>();
            managerRoot.AddComponent<PuzzleManager>();
            managerRoot.AddComponent<StorySaveManager>();
            FragmentUIManager uiMgr = managerRoot.AddComponent<FragmentUIManager>();

            // 2. 플레이어 생성
            GameObject playerObj = new GameObject("Player");
            Undo.RegisterCreatedObjectUndo(playerObj, "Create Player");
            playerObj.transform.position = new Vector3(1.2f, -2.6f, 0f);
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
            CreateWorldLabel(playerObj.transform, "수리공(플레이어)", new Vector3(0f, 0.72f, 0f), new Color(0.55f, 0.95f, 1f, 1f), 0.062f);
            interactionMgr.SetPlayerTransform(playerObj.transform);

            // 3. 거점 공간: 넓어진 『기억 수리점 대공방 (RepairShop_Space)』 (16.0 x 11.33 월드 유닛)
            GameObject repairShopRoot = BuildGrandRepairShopSpace(
                repairShopBg,
                charSprite,
                puzzlePieceSprite,
                lampSprite,
                easelSprite
            );

            // 4. Stage 1 (Ep.1 시계공의 황혼 시계탑 대광장 — 18.0 x 13.0 월드 유닛)
            GameObject stage1Root = BuildExpandedStageMap(
                1,
                "Stage1_ClockTown_Space",
                stage1Bg,
                clocktowerSprite,
                fountainSprite,
                treeSprite,
                lampSprite,
                voidSlotSprite,
                restoredLeftSprite,
                restoredCenterSprite,
                restoredRightSprite,
                puzzlePieceSprite,
                charSprite,
                out GameObject pastMemoryStage1,
                out SpriteRenderer stage1FloorSr
            );

            // 5. Stage 2 (Ep.2 화가의 달빛 아틀리에 대정원 — 18.0 x 13.0 월드 유닛)
            GameObject stage2Root = BuildExpandedStageMap(
                2,
                "Stage2_AtelierGarden_Space",
                stage2Bg,
                easelSprite,
                fountainSprite,
                treeSprite,
                lampSprite,
                voidSlotSprite,
                restoredLeftSprite,
                restoredCenterSprite,
                restoredRightSprite,
                puzzlePieceSprite,
                charSprite,
                out GameObject pastMemoryStage2,
                out _
            );

            // 6. Stage 3 (Ep.3 수리공의 별빛 기억 대성소 — 18.0 x 13.0 월드 유닛)
            GameObject stage3Root = BuildExpandedStageMap(
                3,
                "Stage3_MemorySanctuary_Space",
                stage3Bg,
                clocktowerSprite,
                fountainSprite,
                treeSprite,
                lampSprite,
                voidSlotSprite,
                restoredLeftSprite,
                restoredCenterSprite,
                restoredRightSprite,
                puzzlePieceSprite,
                charSprite,
                out GameObject pastMemoryStage3,
                out _
            );

            repairShopRoot.SetActive(true);
            stage1Root.SetActive(false);
            stage2Root.SetActive(false);
            stage3Root.SetActive(false);

            // 7. 인게임 UI Canvas 생성
            BuildMultiStageUICanvas(
                uiMgr,
                repairShopRoot,
                stage1Root,
                stage2Root,
                stage3Root,
                pastMemoryStage1,
                pastMemoryStage2,
                pastMemoryStage3,
                stage1FloorSr
            );

            Selection.activeGameObject = playerObj;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("[Editor_RoomBuilder] ★ 넓어진 맵(18x13) + 부드러운 카메라 추적 + 스테이지 1~3 고해상도 배경 생성이 완료되었습니다!");
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
        /// 거점: 넓어진 『기억 수리점 대공방 (16.0 x 11.33 월드 유닛)』
        /// 좌측(Ep.1 시계공), 중앙(Ep.2 화가), 우측(Ep.3 수리공의 잔향) 3개 의뢰 구역과 포털을 배치합니다.
        /// </summary>
        private static GameObject BuildGrandRepairShopSpace(
            Sprite shopBgSprite,
            Sprite charSprite,
            Sprite portalSprite,
            Sprite lampSprite,
            Sprite easelSprite)
        {
            GameObject shopRoot = new GameObject("RepairShop_Space");
            Undo.RegisterCreatedObjectUndo(shopRoot, "Create RepairShop_Space");

            GameObject shopBg = new GameObject("GrandRepairShopInterior");
            shopBg.transform.SetParent(shopRoot.transform, false);
            shopBg.transform.position = new Vector3(1.2f, 0.0f, 0f);
            SpriteRenderer bgSr = shopBg.AddComponent<SpriteRenderer>();
            bgSr.sprite = shopBgSprite;
            bgSr.sortingOrder = -10;
            FitSpriteToWorldSize(shopBg.transform, shopBgSprite, 16.0f, 11.33f);

            CreateWorldLabel(shopRoot.transform, "『기억 수리점 대공방』 — 각 의뢰인과 대화하고 작업대 포털(Ep.1 / Ep.2 / Ep.3)을 조사해 기억 공간으로 입장하세요", new Vector3(1.2f, 4.9f, 0f), new Color(1f, 0.92f, 0.58f, 0.98f), 0.062f);

            // [좌측 의뢰 구역] Ep.1 시계공 노인 & 회중시계 포털
            CreateShopClientNPC(
                shopRoot.transform,
                "NPC_Client_Clockmaker",
                new Vector3(-4.2f, 0.7f, 0f),
                "NPC_SHOP_EP1",
                "Ep.1 의뢰인: 시계공 노인",
                new Color(1.0f, 0.80f, 0.38f, 1f),
                charSprite,
                new string[]
                {
                    "\"수리공 양반, 이 회중시계가 11시 55분에서 도통 움직이질 않네.\"",
                    "\"그날 아침 9시에 내가 시계탑 문을 열고, 정오(12:00)에 시계탑 종을 정상적으로 울린 뒤에 딸을 배웅하러 갔는데 말이야...\"",
                    "\"바로 옆 작업대의 [멈춘 회중시계]를 조사해 내 기억 공간(시계탑 대광장)을 수리해 주게!\""
                },
                new string[]
                {
                    "\"오오! 11시 55분에 멈춰 있던 회중시계 바늘이 다시 힘차게 움직이고 있어!\"",
                    "\"떠나는 딸의 기차를 붙잡고 싶어서 11시 55분에 내 손으로 태엽을 멈췄던 거였지... 고맙네, 수리공 양반!\""
                }
            );

            CreatePortalObject(
                shopRoot.transform,
                "Portal_Stage1_ClockTown",
                new Vector3(-2.3f, 0.7f, 0f),
                "PORTAL_STAGE_1",
                "멈춘 회중시계 (Ep.1 시계탑 대광장 입장)",
                FragmentUIManager.SpaceStage1,
                new Color(1.0f, 0.88f, 0.35f, 1.0f),
                portalSprite
            );

            // [중앙 의뢰 구역] Ep.2 무명 화가 & 캔버스 포털
            CreateShopClientNPC(
                shopRoot.transform,
                "NPC_Client_Painter",
                new Vector3(0.3f, 0.7f, 0f),
                "NPC_SHOP_EP2",
                "Ep.2 의뢰인: 무명 화가",
                new Color(0.95f, 0.55f, 0.78f, 1f),
                charSprite,
                new string[]
                {
                    "\"제 마지막 자화상 캔버스가 하얗게 지워져 버렸어요.\"",
                    "\"폭풍우 치던 밤 내내 화실 창문을 굳게 닫아두어서 빗물이 들어올 리 없었는데 왜 물감이 번졌을까요?\"",
                    "\"옆 작업대의 [지워진 캔버스]를 통해 제 기억 공간(달빛 화실 정원)으로 들어가 진실을 찾아주세요!\""
                },
                new string[]
                {
                    "\"맞아요... 폭풍우 속 달빛을 담으려고 제가 직접 창문을 활짝 열어젖혔었죠!\"",
                    "\"덕분에 지워졌던 자화상의 색채가 온전히 돌아왔어요. 정말 감사합니다!\""
                }
            );

            CreatePortalObject(
                shopRoot.transform,
                "Portal_Stage2_Atelier",
                new Vector3(2.1f, 0.7f, 0f),
                "PORTAL_STAGE_2",
                "지워진 캔버스 (Ep.2 달빛 화실 입장)",
                FragmentUIManager.SpaceStage2,
                new Color(0.75f, 0.55f, 1.0f, 1.0f),
                portalSprite
            );

            // [우측 의뢰 구역] Ep.3 기억의 잔향 & 태엽 코어 포털
            CreateShopClientNPC(
                shopRoot.transform,
                "NPC_Client_Echo",
                new Vector3(4.6f, 0.7f, 0f),
                "NPC_SHOP_EP3",
                "Ep.3 인도자: 기억의 잔향",
                new Color(0.55f, 0.95f, 1.0f, 1f),
                charSprite,
                new string[]
                {
                    "\"타인의 부서진 기억을 고치며 네가 찾아 헤맨 건, 결국 너 자신이 봉인해 두었던 기억이다.\"",
                    "\"넌 스스로를 '이름도 과거도 없던 빈 껍데기'라고 믿고 있지만, 그건 네가 만든 거짓 기억이야.\"",
                    "\"오른쪽 작업대의 [봉인된 태엽 코어]를 조사해 너 자신의 기억 성소로 들어가라.\""
                },
                new string[]
                {
                    "\"모든 기억 조각이 하나로 이어졌다. 이제 너는 기억 수리점의 진짜 주인으로 깨어났다!\""
                }
            );

            CreatePortalObject(
                shopRoot.transform,
                "Portal_Stage3_Sanctuary",
                new Vector3(6.4f, 0.7f, 0f),
                "PORTAL_STAGE_3",
                "봉인된 태엽 코어 (Ep.3 기억 성소 입장)",
                FragmentUIManager.SpaceStage3,
                new Color(0.45f, 1.0f, 0.85f, 1.0f),
                portalSprite
            );

            // 대공방 외곽 벽 콜라이더 (16.0 x 11.33 월드 유닛)
            CreateInvisibleWall(shopRoot.transform, "ShopBound_Top", new Vector3(1.2f, 5.2f, 0f), new Vector2(16.0f, 0.6f));
            CreateInvisibleWall(shopRoot.transform, "ShopBound_Bottom", new Vector3(1.2f, -5.2f, 0f), new Vector2(16.0f, 0.6f));
            CreateInvisibleWall(shopRoot.transform, "ShopBound_Left", new Vector3(-6.5f, 0f, 0f), new Vector2(0.6f, 11.2f));
            CreateInvisibleWall(shopRoot.transform, "ShopBound_Right", new Vector3(8.9f, 0f, 0f), new Vector2(0.6f, 11.2f));

            return shopRoot;
        }

        private static void CreateShopClientNPC(
            Transform parent,
            string objectName,
            Vector3 pos,
            string npcId,
            string displayName,
            Color tintColor,
            Sprite charSprite,
            string[] lines,
            string[] clearedLines)
        {
            GameObject npcObj = new GameObject(objectName);
            npcObj.transform.SetParent(parent, false);
            npcObj.transform.position = pos;

            SpriteRenderer sr = npcObj.AddComponent<SpriteRenderer>();
            sr.sprite = charSprite;
            sr.color = tintColor;
            sr.sortingOrder = 12;

            CircleCollider2D col = npcObj.AddComponent<CircleCollider2D>();
            col.isTrigger = false;
            col.radius = 0.48f;

            InteractableObject interact = npcObj.AddComponent<InteractableObject>();
            interact.SetupNPC(npcId, displayName, "DLG_" + npcId, lines, clearedLines);

            CreateWorldLabel(npcObj.transform, $"★ {displayName}", new Vector3(0f, 0.78f, 0f), tintColor, 0.06f);
        }

        /// <summary>
        /// 확장된 스테이지 맵(18.0 x 13.0 월드 유닛, 864 x 624 px)을 구축합니다.
        /// </summary>
        private static GameObject BuildExpandedStageMap(
            int stageNum,
            string rootName,
            Sprite stageBgSprite,
            Sprite centerMonumentSprite,
            Sprite fountainSprite,
            Sprite treeSprite,
            Sprite lampSprite,
            Sprite voidSlotSprite,
            Sprite restoredLeftSprite,
            Sprite restoredCenterSprite,
            Sprite restoredRightSprite,
            Sprite puzzlePieceSprite,
            Sprite charSprite,
            out GameObject pastMemoryRoot,
            out SpriteRenderer mapFloorSr)
        {
            GameObject stageRoot = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(stageRoot, $"Create {rootName}");

            GameObject envRoot = new GameObject("StageEnvironment");
            envRoot.transform.SetParent(stageRoot.transform, false);

            // 1. 고해상도 핸드페인팅 18.0 x 13.0 대형 배경 일러스트 캔버스
            GameObject mapCanvas = new GameObject("StageBackgroundCanvas");
            mapCanvas.transform.SetParent(envRoot.transform, false);
            mapCanvas.transform.position = new Vector3(1.2f, 0.0f, 0f);
            mapFloorSr = mapCanvas.AddComponent<SpriteRenderer>();
            mapFloorSr.sprite = stageBgSprite;
            mapFloorSr.color = Color.white;
            mapFloorSr.sortingOrder = -10;
            FitSpriteToWorldSize(mapCanvas.transform, stageBgSprite, 18.0f, 13.0f);

            // 5. 외곽 경계 벽 콜라이더 (18.0 x 12.0 월드 유닛 — 맵 전체를 자유롭게 이동하며 랜덤 빈 배경 3곳을 탐색!)
            CreateInvisibleWall(envRoot.transform, "Bound_Top", new Vector3(1.2f, 5.6f, 0f), new Vector2(18.0f, 0.5f));
            CreateInvisibleWall(envRoot.transform, "Bound_Bottom", new Vector3(1.2f, -5.6f, 0f), new Vector2(18.0f, 0.5f));
            CreateInvisibleWall(envRoot.transform, "Bound_Left", new Vector3(-7.4f, 0f, 0f), new Vector2(0.5f, 12.0f));
            CreateInvisibleWall(envRoot.transform, "Bound_Right", new Vector3(9.8f, 0f, 0f), new Vector2(0.5f, 12.0f));

            // 6. 맵 전체 랜덤 위치에 배치되는 비어진 배경 퍼즐 슬롯 3곳 (실행 시 FragmentUIManager가 맵 전체에서 랜덤 3곳으로 재배치)
            GameObject slotsRoot = new GameObject("BackgroundPuzzleSlots");
            slotsRoot.transform.SetParent(stageRoot.transform, false);

            string prefix = $"EP{stageNum}_";
            string s1Name = stageNum == 1 ? "거실 빈 배경 1" : (stageNum == 2 ? "지하철역 빈 배경 1" : "기억 공간 빈 배경 1");
            string s2Name = stageNum == 1 ? "거실 빈 배경 2" : (stageNum == 2 ? "지하철역 빈 배경 2" : "기억 공간 빈 배경 2");
            string s3Name = stageNum == 1 ? "거실 빈 배경 3" : (stageNum == 2 ? "지하철역 빈 배경 3" : "기억 공간 빈 배경 3");

            CreateRichBlankSlot(
                slotsRoot.transform,
                "BlankSlot_01",
                new Vector3(-4.2f, 2.8f, 0f),
                prefix + "SLOT_01",
                s1Name,
                "[빈 배경 1]",
                prefix + "FRAG_01",
                voidSlotSprite,
                restoredLeftSprite,
                null
            );

            CreateRichBlankSlot(
                slotsRoot.transform,
                "BlankSlot_02",
                new Vector3(1.2f, 0.4f, 0f),
                prefix + "SLOT_02",
                s2Name,
                "[빈 배경 2]",
                prefix + "FRAG_02",
                voidSlotSprite,
                restoredCenterSprite,
                null
            );

            CreateRichBlankSlot(
                slotsRoot.transform,
                "BlankSlot_03",
                new Vector3(6.4f, 2.8f, 0f),
                prefix + "SLOT_03",
                s3Name,
                "[빈 배경 3]",
                prefix + "FRAG_03",
                voidSlotSprite,
                restoredRightSprite,
                null
            );

            // 7. 넓어진 아래쪽 맵 곳곳에 기억조각 1, 2, 3 배치
            GameObject clueParent = new GameObject("ClueFragments");
            clueParent.transform.SetParent(stageRoot.transform, false);

            CreateClueObject(
                clueParent.transform,
                "Item_MemoryFragment_1",
                new Vector3(-4.8f, -3.5f, 0f),
                prefix + "FRAG_01",
                "기억조각 1",
                $"{s1Name}의 끊어진 공간을 복원하는 첫 번째 기억 퍼즐 조각입니다.",
                new Color(1.0f, 0.86f, 0.32f, 1.0f),
                puzzlePieceSprite
            );

            CreateClueObject(
                clueParent.transform,
                "Item_MemoryFragment_2",
                new Vector3(7.0f, -2.6f, 0f),
                prefix + "FRAG_02",
                "기억조각 2",
                $"{s2Name}의 끊어진 공간을 복원하는 두 번째 기억 퍼즐 조각입니다.",
                new Color(0.45f, 0.98f, 0.65f, 1.0f),
                puzzlePieceSprite
            );

            CreateClueObject(
                clueParent.transform,
                "Item_MemoryFragment_3",
                new Vector3(1.2f, -4.8f, 0f),
                prefix + "FRAG_03",
                "기억조각 3",
                $"{s3Name}의 끊어진 공간을 복원하는 세 번째 기억 퍼즐 조각입니다.",
                new Color(1.0f, 0.55f, 0.88f, 1.0f),
                puzzlePieceSprite
            );

            // 8. 수리점 귀환 포털
            CreatePortalObject(
                stageRoot.transform,
                "Portal_ReturnToShop",
                new Vector3(7.2f, -4.9f, 0f),
                $"PORTAL_RETURN_EP{stageNum}",
                "수리점 귀환 포털 ([E/Space] 대공방 복귀)",
                FragmentUIManager.SpaceRepairShop,
                new Color(0.45f, 0.95f, 1.0f, 0.95f),
                puzzlePieceSprite
            );

            // 9. 위쪽 광장 '과거의 기억 3장면 & 최종 수리 제단'
            pastMemoryRoot = BuildStagePastMemoryScene(stageRoot.transform, stageNum, charSprite, puzzlePieceSprite);
            pastMemoryRoot.SetActive(false);

            return stageRoot;
        }

        private static void CreateTintedProp(Transform parent, string name, Vector3 pos, Sprite sprite, Color tint, int sortingOrder)
        {
            GameObject prop = new GameObject(name);
            prop.transform.SetParent(parent, false);
            prop.transform.position = pos;
            SpriteRenderer sr = prop.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tint;
            sr.sortingOrder = sortingOrder;
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

            CreateWorldLabel(portalObj.transform, $"◆ {portalLabel}", new Vector3(0f, 0.82f, 0f), tintColor, 0.06f);
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

        private static GameObject BuildStagePastMemoryScene(Transform parent, int stageNum, Sprite charSprite, Sprite crystalSprite)
        {
            GameObject memoryRoot = new GameObject("PastMemoryScene");
            memoryRoot.transform.SetParent(parent, false);

            if (stageNum == 1)
            {
                CreateMemoryNPC(
                    memoryRoot.transform,
                    "Memory_Step1",
                    new Vector3(-3.4f, 3.0f, 0f),
                    "EP1_MEM_1",
                    "과거 기억 A [09:00 시계탑 개방]",
                    "DLG_EP1_M1",
                    new string[]
                    {
                        "[과거의 기억 A — 오전 09:00] 아침 햇살 속에서 시계공이 시계탑 문을 활짝 열고 하루의 태엽을 감기 시작했습니다."
                    },
                    new Color(1.0f, 0.88f, 0.42f, 0.95f),
                    charSprite
                );

                CreateMemoryNPC(
                    memoryRoot.transform,
                    "Memory_Step2",
                    new Vector3(5.8f, 3.0f, 0f),
                    "EP1_MEM_2",
                    "과거 기억 B [11:30 딸의 편지]",
                    "DLG_EP1_M2",
                    new string[]
                    {
                        "[과거의 기억 B — 오전 11:30] 시계공이 '정오 기차로 떠난다'는 딸의 편지를 뒤늦게 발견하고 다급해졌습니다!"
                    },
                    new Color(0.65f, 0.95f, 1.0f, 0.95f),
                    charSprite
                );

                CreateMemoryNPC(
                    memoryRoot.transform,
                    "Memory_Step3",
                    new Vector3(-0.8f, 2.5f, 0f),
                    "EP1_MEM_3",
                    "과거 기억 C [11:55 멈춤핀 고정]",
                    "DLG_EP1_M3",
                    new string[]
                    {
                        "[과거의 기억 C — 오전 11:55] 시계공은 딸이 탄 정오 기차가 떠나지 않기를 바라며 스스로 시계탑 태엽에 멈춤핀을 꽂아 시계를 멈췄습니다!"
                    },
                    new Color(1.0f, 0.62f, 0.88f, 0.95f),
                    charSprite
                );
            }
            else if (stageNum == 2)
            {
                CreateMemoryNPC(
                    memoryRoot.transform,
                    "Memory_Step1",
                    new Vector3(-3.4f, 3.0f, 0f),
                    "EP2_MEM_1",
                    "과거 기억 A [18:00 자화상 스케치]",
                    "DLG_EP2_M1",
                    new string[]
                    {
                        "[과거의 기억 A — 오후 06:00] 해 질 녘 화실 이젤 앞에 앉아 마지막 자화상 스케치를 시작했습니다."
                    },
                    new Color(1.0f, 0.82f, 0.45f, 0.95f),
                    charSprite
                );

                CreateMemoryNPC(
                    memoryRoot.transform,
                    "Memory_Step2",
                    new Vector3(5.8f, 3.0f, 0f),
                    "EP2_MEM_2",
                    "과거 기억 B [22:00 창문을 활짝 엶]",
                    "DLG_EP2_M2",
                    new string[]
                    {
                        "[과거의 기억 B — 밤 10:00] 폭풍우 속 달빛의 색채를 화폭에 담으려 화가 스스로 창문을 활짝 열어젖혔습니다! (창문을 닫아두었다는 진술과 모순!)"
                    },
                    new Color(0.75f, 0.65f, 1.0f, 0.95f),
                    charSprite
                );

                CreateMemoryNPC(
                    memoryRoot.transform,
                    "Memory_Step3",
                    new Vector3(-0.8f, 2.5f, 0f),
                    "EP2_MEM_3",
                    "과거 기억 C [23:40 빗물에 번진 화폭]",
                    "DLG_EP2_M3",
                    new string[]
                    {
                        "[과거의 기억 C — 밤 11:40] 열린 창문으로 들이친 빗물에 자화상 물감이 번지자 화가는 캔버스를 천으로 덮어두었습니다."
                    },
                    new Color(0.55f, 0.95f, 1.0f, 0.95f),
                    charSprite
                );
            }
            else
            {
                CreateMemoryNPC(
                    memoryRoot.transform,
                    "Memory_Step1",
                    new Vector3(-3.4f, 3.0f, 0f),
                    "EP3_MEM_1",
                    "과거 기억 A [기억 수리점의 시작]",
                    "DLG_EP3_M1",
                    new string[]
                    {
                        "[과거의 기억 A — 첫 번째 기억] 상처 입은 사람들의 기억을 치유하기 위해 주인공이 처음 기억 수리점의 문을 열었습니다."
                    },
                    new Color(1.0f, 0.90f, 0.45f, 0.95f),
                    charSprite
                );

                CreateMemoryNPC(
                    memoryRoot.transform,
                    "Memory_Step2",
                    new Vector3(5.8f, 3.0f, 0f),
                    "EP3_MEM_2",
                    "과거 기억 B [기억 에너지의 공명]",
                    "DLG_EP3_M2",
                    new string[]
                    {
                        "[과거의 기억 B — 두 번째 기억] 시계공과 화가의 기억을 복원하며 모인 공명 빛이 봉인된 성소의 문을 열었습니다."
                    },
                    new Color(0.55f, 0.95f, 1.0f, 0.95f),
                    charSprite
                );

                CreateMemoryNPC(
                    memoryRoot.transform,
                    "Memory_Step3",
                    new Vector3(-0.8f, 2.5f, 0f),
                    "EP3_MEM_3",
                    "과거 기억 C [수리공의 진짜 명패]",
                    "DLG_EP3_M3",
                    new string[]
                    {
                        "[과거의 기억 C — 세 번째 기억] 봉인된 서랍 속에는 주인공의 진짜 이름이 적힌 '수석 기억 수리공 명패'가 빛나고 있었습니다!"
                    },
                    new Color(1.0f, 0.65f, 0.92f, 0.95f),
                    charSprite
                );
            }

            // 중앙 최종 기억 수리 제단
            GameObject altarObj = new GameObject("Altar_FinalMemoryRepair");
            altarObj.transform.SetParent(memoryRoot.transform, false);
            altarObj.transform.position = new Vector3(2.5f, 2.5f, 0f);
            altarObj.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            SpriteRenderer altarSr = altarObj.AddComponent<SpriteRenderer>();
            altarSr.sprite = crystalSprite;
            altarSr.color = new Color(0.50f, 1.0f, 0.78f, 1.0f);
            altarSr.sortingOrder = 14;

            CircleCollider2D altarCol = altarObj.AddComponent<CircleCollider2D>();
            altarCol.isTrigger = false;
            altarCol.radius = 0.52f;

            InteractableObject altarInteract = altarObj.AddComponent<InteractableObject>();
            altarInteract.SetupPuzzleTrigger(
                $"ALTAR_EP{stageNum}_FINAL",
                $"Ep.{stageNum} 기억 수리 제단 (① 순서 & ② 모순 퍼즐)",
                $"PUZ_EP{stageNum}_ORDER",
                PuzzleType.ChronologicalOrder
            );

            CreateWorldLabel(altarObj.transform, $"★ [Ep.{stageNum} 기억 수리 제단]\n([E/Space] 최종 수리)", new Vector3(0f, 0.82f, 0f), new Color(0.50f, 1.0f, 0.78f, 1f), 0.062f);

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
            clueObj.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            SpriteRenderer sr = clueObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tintColor;
            sr.sortingOrder = 10;

            CircleCollider2D col = clueObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            InteractableObject interactable = clueObj.AddComponent<InteractableObject>();
            interactable.SetupClue(clueId, title, description);

            CreateWorldLabel(clueObj.transform, title, new Vector3(0f, 0.68f, 0f), tintColor, 0.062f);
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

            CreateWorldLabel(npcObj.transform, $"★ {npcName}", new Vector3(0f, 0.75f, 0f), tintColor, 0.06f);
        }

        private static void CreateWorldLabel(Transform parent, string text, Vector3 localOffset, Color color, float worldScale = 0.062f)
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

        private static void BuildMultiStageUICanvas(
            FragmentUIManager uiMgr,
            GameObject repairShopRoot,
            GameObject stage1Root,
            GameObject stage2Root,
            GameObject stage3Root,
            GameObject past1Root,
            GameObject past2Root,
            GameObject past3Root,
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

            // 상단 바 (거점 + Ep.1 + Ep.2 + Ep.3 + 리셋 버튼)
            GameObject topBar = CreateUIPanel(canvasObj.transform, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -46f), Vector2.zero, new Color(0.06f, 0.08f, 0.13f, 0.95f));
            Text headerTitleText = CreateUIText(topBar.transform, "HeaderTitleText", "[거점: 기억 수리점 대공방]", defaultFont, 13, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 0f), new Vector2(0.53f, 1f), new Vector2(14f, 0f), Vector2.zero);

            CreateTopNavButton(topBar.transform, "Btn_Shop", "거점: 수리점", defaultFont, new Vector2(0.54f, 0.15f), new Vector2(0.625f, 0.85f), new Color(0.42f, 0.28f, 0.16f, 1f), uiMgr.EnterRepairShop);
            CreateTopNavButton(topBar.transform, "Btn_Ep1", "1. 집 거실", defaultFont, new Vector2(0.63f, 0.15f), new Vector2(0.715f, 0.85f), new Color(0.56f, 0.36f, 0.20f, 1f), uiMgr.EnterStage1);
            CreateTopNavButton(topBar.transform, "Btn_Ep2", "2. 지하철역", defaultFont, new Vector2(0.72f, 0.15f), new Vector2(0.805f, 0.85f), new Color(0.20f, 0.38f, 0.56f, 1f), uiMgr.EnterStage2);
            CreateTopNavButton(topBar.transform, "Btn_Ep3", "3. 기억 공간", defaultFont, new Vector2(0.81f, 0.15f), new Vector2(0.895f, 0.85f), new Color(0.38f, 0.24f, 0.54f, 1f), uiMgr.EnterStage3);
            CreateTopNavButton(topBar.transform, "Btn_Reset", "전체 리셋", defaultFont, new Vector2(0.905f, 0.15f), new Vector2(0.988f, 0.85f), new Color(0.55f, 0.24f, 0.24f, 1f), uiMgr.ResetPuzzleMap);

            // 좌측 인벤토리 및 에피소드 체크리스트 패널
            GameObject leftPanel = CreateUIPanel(canvasObj.transform, "InventoryPanel", new Vector2(0f, 0.24f), new Vector2(0.20f, 0.91f), new Vector2(10f, 0f), Vector2.zero, new Color(0.07f, 0.10f, 0.17f, 0.88f));
            Text inventoryText = CreateUIText(leftPanel.transform, "InventoryStatusText", "[진행 현황]", defaultFont, 12, TextAnchor.UpperLeft, Color.white, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));

            // 하단 상호작용 프롬프트
            GameObject promptBar = CreateUIPanel(canvasObj.transform, "PromptBar", new Vector2(0.22f, 0.015f), new Vector2(0.98f, 0.07f), Vector2.zero, Vector2.zero, new Color(0.07f, 0.09f, 0.14f, 0.90f));
            Text promptText = CreateUIText(promptBar.transform, "PromptText", "상호작용 안내", defaultFont, 14, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.35f, 1f), Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));

            // 하단 대화창
            GameObject dialogPanel = CreateUIPanel(canvasObj.transform, "DialogPanel", new Vector2(0.22f, 0.08f), new Vector2(0.98f, 0.22f), Vector2.zero, Vector2.zero, new Color(0.10f, 0.13f, 0.22f, 0.95f));
            Text dialogSpeakerText = CreateUIText(dialogPanel.transform, "DialogSpeakerText", "알림", defaultFont, 15, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f, 1f), new Vector2(0f, 0.64f), new Vector2(1f, 1f), new Vector2(14f, 0f), new Vector2(-14f, -2f));
            Text dialogBodyText = CreateUIText(dialogPanel.transform, "DialogBodyText", "내용", defaultFont, 14, TextAnchor.UpperLeft, Color.white, new Vector2(0f, 0f), new Vector2(1f, 0.64f), new Vector2(14f, 4f), new Vector2(-14f, -2f));
            dialogPanel.SetActive(false);

            // 중앙 퍼즐 모달 창
            GameObject slotChoicePanel = CreateUIPanel(canvasObj.transform, "SlotChoicePanel", new Vector2(0.25f, 0.24f), new Vector2(0.96f, 0.89f), Vector2.zero, Vector2.zero, new Color(0.09f, 0.12f, 0.20f, 0.98f));
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

            uiMgr.BindMultiStageReferences(
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
                stage1Root,
                stage2Root,
                stage3Root,
                past1Root,
                past2Root,
                past3Root,
                mainFloorSr
            );
        }

        private static void CreateTopNavButton(Transform parent, string name, string label, Font font, Vector2 anchorMin, Vector2 anchorMax, Color bgColor, UnityEngine.Events.UnityAction action)
        {
            GameObject btnObj = CreateUIPanel(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero, bgColor);
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnObj.GetComponent<Image>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
            CreateUIText(btnObj.transform, "Text", label, font, 12, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
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
        // 고속 Color32[] 기반 고해상도 스테이지별 배경 & 프롭 생성기
        // =====================================================================
        private static void FitSpriteToWorldSize(Transform target, Sprite sprite, float targetWidth, float targetHeight)
        {
            if (target == null || sprite == null) return;
            Vector2 size = sprite.bounds.size;
            if (size.x > 0.001f && size.y > 0.001f)
            {
                target.localScale = new Vector3(targetWidth / size.x, targetHeight / size.y, 1f);
            }
        }

        private static void EnsureRichArtSprites()
        {
            if (!Directory.Exists(SpriteFolder))
            {
                Directory.CreateDirectory(SpriteFolder);
            }

            GenerateLivingRoomMapSprite($"{SpriteFolder}/Spr_Map1_LivingRoom.png");
            GenerateSubwayStationMapSprite($"{SpriteFolder}/Spr_Map2_SubwayStation.png");
            GenerateMemoryFusionMapSprite($"{SpriteFolder}/Spr_Map3_MemoryFusion.png");

            GenerateFountainPropSprite($"{SpriteFolder}/Spr_Fountain_HQ.png");
            GenerateEaselPropSprite($"{SpriteFolder}/Spr_Easel_HQ.png");

            GenerateVoidSlotSprite($"{SpriteFolder}/Spr_VoidSlot.png");
            GeneratePuzzlePieceSprite($"{SpriteFolder}/Spr_PuzzlePiece.png");
            GenerateCharacterSprite($"{SpriteFolder}/Spr_Character.png");
            GenerateClocktowerSprite($"{SpriteFolder}/Spr_Clocktower.png");
            GenerateTreeSprite($"{SpriteFolder}/Spr_Tree.png");
            GenerateStreetLampSprite($"{SpriteFolder}/Spr_StreetLamp.png");
            GenerateSimpleSquare($"{SpriteFolder}/Spr_Square.png");

            AssetDatabase.Refresh();

            ConfigureSpriteImporter($"{SpriteFolder}/Spr_RepairShop_2DRPG.png", 32f, FilterMode.Point, 1024);
            ConfigureSpriteImporter($"{SpriteFolder}/Spr_Map1_LivingRoom.png", 32f, FilterMode.Point, 1024);
            ConfigureSpriteImporter($"{SpriteFolder}/Spr_Map2_SubwayStation.png", 32f, FilterMode.Point, 1024);
            ConfigureSpriteImporter($"{SpriteFolder}/Spr_Map3_MemoryFusion.png", 32f, FilterMode.Point, 1024);
            ConfigureSpriteImporter($"{SpriteFolder}/Spr_Fountain_HQ.png", 48f, FilterMode.Point);
            ConfigureSpriteImporter($"{SpriteFolder}/Spr_Easel_HQ.png", 48f, FilterMode.Point);
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
        /// 거점: 넓어진 『기억 수리점 대공방』 고해상도 배경 (768 x 544 px, PPU 48 -> 16.0 x 11.33 월드 유닛)
        /// 헤링본 원목 마루, 스테인드글라스 창문 3개, 고서적 책장, 황금 자수 벨벳 카펫, 3개의 의뢰 작업대 테이블을 렌더링합니다.
        /// </summary>
        private static void GenerateGrandRepairShopCanvas(string path)
        {
            if (File.Exists(path)) return;

            int w = 768;
            int h = 544;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;

                    // 1. 외곽 원목 몰딩 벽 테두리 (22px)
                    if (x < 22 || x >= w - 22 || y < 22 || y >= h - 22)
                    {
                        bool pillar = (x % 64 < 6) || (y % 64 < 6);
                        pixels[idx] = pillar ? new Color32(42, 28, 18, 255) : new Color32(68, 45, 28, 255);
                        continue;
                    }

                    // 2. 상단 고풍스러운 공방 벽면 & 아치형 스테인드글라스 창문 + 책장 (y >= 425)
                    if (y >= 425)
                    {
                        // 3개의 아치형 스테인드글라스 창문 (x=165, 384, 603)
                        float w1 = Vector2.Distance(new Vector2(x, y), new Vector2(165f, 480f));
                        float w2 = Vector2.Distance(new Vector2(x, y), new Vector2(384f, 480f));
                        float w3 = Vector2.Distance(new Vector2(x, y), new Vector2(603f, 480f));
                        if (w1 < 34f || w2 < 38f || w3 < 34f)
                        {
                            bool lead = (x % 10 == 0) || (y % 10 == 0);
                            pixels[idx] = lead ? new Color32(45, 35, 25, 255) : new Color32(115, 185, 235, 255);
                            continue;
                        }

                        bool shelf = (y % 24 < 5);
                        if (shelf)
                        {
                            pixels[idx] = new Color32(88, 56, 32, 255);
                        }
                        else if (x >= 50 && x <= w - 50 && (x % 12 < 9))
                        {
                            int tone = (x / 12 + y / 24) % 5;
                            pixels[idx] = tone == 0 ? new Color32(168, 62, 62, 255) :
                                          tone == 1 ? new Color32(62, 122, 168, 255) :
                                          tone == 2 ? new Color32(72, 145, 95, 255) :
                                          tone == 3 ? new Color32(195, 162, 72, 255) :
                                                      new Color32(138, 82, 165, 255);
                        }
                        else
                        {
                            pixels[idx] = new Color32(52, 36, 26, 255);
                        }
                        continue;
                    }

                    // 3. 3개의 에피소드 의뢰 수리 작업대 테이블 (좌: Ep.1, 중: Ep.2, 우: Ep.3)
                    bool onTable1 = (x >= 185 && x <= 295 && y >= 280 && y <= 335);
                    bool onTable2 = (x >= 395 && x <= 505 && y >= 280 && y <= 335);
                    bool onTable3 = (x >= 600 && x <= 710 && y >= 280 && y <= 335);
                    if (onTable1 || onTable2 || onTable3)
                    {
                        bool edge = (y < 286 || y > 329 || x % 55 < 4);
                        pixels[idx] = edge ? new Color32(82, 50, 26, 255) : new Color32(138, 92, 52, 255);
                        continue;
                    }

                    // 4. 중앙 대형 벨벳 자수 카펫 러그 (x: 96 ~ 672, y: 88 ~ 370)
                    if (x >= 96 && x <= 672 && y >= 88 && y <= 370)
                    {
                        bool outerGold = (x < 104 || x > 664 || y < 96 || y > 362);
                        bool innerGold = (x >= 116 && x <= 652 && y >= 108 && y <= 350) &&
                                         (x < 120 || x > 648 || y < 112 || y > 346);
                        if (outerGold || innerGold)
                        {
                            pixels[idx] = new Color32(218, 182, 82, 255);
                        }
                        else
                        {
                            float centerDist = Vector2.Distance(new Vector2(x, y), new Vector2(384f, 229f));
                            if (Mathf.Abs(centerDist - 58f) < 3f || Mathf.Abs(centerDist - 32f) < 2.5f)
                            {
                                pixels[idx] = new Color32(205, 168, 78, 255); // 중앙 황금 태엽 문양
                            }
                            else
                            {
                                bool diamond = ((x + y) % 18 == 0) || ((x - y + 1024) % 18 == 0);
                                pixels[idx] = diamond ? new Color32(122, 48, 58, 255) : new Color32(96, 38, 46, 255);
                            }
                        }
                        continue;
                    }

                    // 5. 헤링본 원목 마룻바닥 + 따뜻한 램프 조명 그라데이션
                    bool plankJoint = (y % 18 == 0) || ((x + (y / 18) * 32) % 64 == 0);
                    int grain = ((x * 7 + y * 13) % 14);
                    Color32 wood = plankJoint
                        ? new Color32(54, 36, 24, 255)
                        : new Color32((byte)(86 + grain), (byte)(60 + grain), (byte)(40 + grain / 2), 255);
                    pixels[idx] = wood;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 확장된 스테이지 1~3 배경 캔버스 (864 x 624 px, PPU 48 -> 18.0 x 13.0 월드 유닛)
        /// stageType 1: 시계공의 황혼 시계탑 대광장
        /// stageType 2: 화가의 달빛 아틀리에 대정원
        /// stageType 3: 수리공의 별빛 기억 대성소
        /// </summary>
        private static void GenerateExpandedStageCanvas(string path, int stageType)
        {
            if (File.Exists(path)) return;

            int w = 864;
            int h = 624;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];

            // 가운데 수로(강물/연못/해자) Y 구간: 중심(y=312) 기준 276 ~ 372
            int canalMinY = 276;
            int canalMaxY = 372;

            // 3개 다리 X 중심: 왼쪽(x=211), 중앙(x=432), 오른쪽(x=653)
            float bridgeX1 = 211f;
            float bridgeX2 = 432f;
            float bridgeX3 = 653f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;

                    // 1. 외곽 성벽/화실담장/성소결계 테두리 (20px)
                    if (x < 20 || x >= w - 20 || y < 20 || y >= h - 20)
                    {
                        bool mortar = (y % 14 == 0) || ((x + (y / 14) * 18) % 32 == 0);
                        if (stageType == 1)
                            pixels[idx] = mortar ? new Color32(38, 44, 62, 255) : new Color32(78, 88, 116, 255);
                        else if (stageType == 2)
                            pixels[idx] = mortar ? new Color32(48, 36, 68, 255) : new Color32(92, 72, 128, 255);
                        else
                            pixels[idx] = mortar ? new Color32(28, 48, 64, 255) : new Color32(62, 108, 132, 255);
                        continue;
                    }

                    // 2. 가운데 수로 (운하 / 은하수 연못 / 차원 해자)
                    if (y >= canalMinY && y <= canalMaxY)
                    {
                        if (y < canalMinY + 9 || y > canalMaxY - 9)
                        {
                            // 석조 난간 강둑
                            bool joint = (x % 24 == 0);
                            Color32 bankCol = stageType == 1
                                ? (joint ? new Color32(58, 66, 84, 255) : new Color32(112, 124, 148, 255))
                                : stageType == 2
                                    ? (joint ? new Color32(72, 58, 96, 255) : new Color32(138, 118, 172, 255))
                                    : (joint ? new Color32(48, 82, 98, 255) : new Color32(185, 158, 82, 255));
                            pixels[idx] = bankCol;
                        }
                        else
                        {
                            float wave = Mathf.Sin((x * 0.06f) + (y * 0.14f)) * 0.5f + 0.5f;
                            Color cA = stageType == 1 ? new Color(0.09f, 0.28f, 0.48f) :
                                       stageType == 2 ? new Color(0.18f, 0.14f, 0.42f) :
                                                        new Color(0.05f, 0.12f, 0.24f);
                            Color cB = stageType == 1 ? new Color(0.18f, 0.46f, 0.70f) :
                                       stageType == 2 ? new Color(0.36f, 0.26f, 0.68f) :
                                                        new Color(0.12f, 0.34f, 0.52f);
                            Color water = Color.Lerp(cA, cB, wave);
                            if ((x * 3 + y * 5) % 89 == 0 && wave > 0.68f)
                            {
                                water = stageType == 2 ? new Color(0.95f, 0.75f, 1.0f) : new Color(0.55f, 0.88f, 1.0f);
                            }
                            pixels[idx] = water;
                        }
                        continue;
                    }

                    // 3. 아래쪽 탐색 대광장/정원 (y < canalMinY)
                    if (y < canalMinY)
                    {
                        // 중앙 분수대 원형 로터리 광장 (중심 x=432, y=155, 반경 74)
                        float rotaryDist = Vector2.Distance(new Vector2(x, y), new Vector2(432f, 155f));
                        bool onRotary = rotaryDist <= 74f;
                        bool onPath1 = Mathf.Abs(x - bridgeX1) < 42f && y >= 72;
                        bool onPath2 = Mathf.Abs(x - bridgeX2) < 46f && y >= 48;
                        bool onPath3 = Mathf.Abs(x - bridgeX3) < 42f && y >= 72;
                        bool onMainAve = (y >= 125 && y <= 185 && x >= 110 && x <= 754);
                        bool onLowerAve = (y >= 56 && y <= 92 && x >= 165 && x <= 699);

                        if (onRotary || onPath1 || onPath2 || onPath3 || onMainAve || onLowerAve)
                        {
                            int bx = x + ((y / 16) % 2) * 12;
                            bool joint = (y % 16 == 0) || (bx % 24 == 0);
                            // 로터리 황금/대리석 원형 테두리
                            if (Mathf.Abs(rotaryDist - 72f) < 3.0f || Mathf.Abs(rotaryDist - 42f) < 2.2f)
                            {
                                pixels[idx] = new Color32(212, 182, 92, 255);
                            }
                            else if (stageType == 1)
                            {
                                pixels[idx] = joint ? new Color32(54, 60, 76, 255) : new Color32(96, 104, 124, 255);
                            }
                            else if (stageType == 2)
                            {
                                pixels[idx] = joint ? new Color32(64, 52, 86, 255) : new Color32(112, 94, 142, 255);
                            }
                            else
                            {
                                pixels[idx] = joint ? new Color32(36, 62, 78, 255) : new Color32(68, 104, 126, 255);
                            }
                        }
                        else
                        {
                            // 정원 잔디밭 & 화단 디테일
                            int noise = ((x * 19 + y * 31) % 20);
                            if (stageType == 1)
                            {
                                Color32 grass = new Color32((byte)(34 + noise / 2), (byte)(76 + noise), (byte)(58 + noise / 2), 255);
                                if ((x * 13 + y * 29) % 173 == 0) grass = new Color32(245, 215, 85, 255);
                                else if ((x * 23 + y * 17) % 211 == 0) grass = new Color32(245, 135, 195, 255);
                                pixels[idx] = grass;
                            }
                            else if (stageType == 2)
                            {
                                // 보랏빛 라벤더 달빛 정원 + 물감 꽃송이
                                Color32 garden = new Color32((byte)(46 + noise / 2), (byte)(42 + noise / 2), (byte)(82 + noise), 255);
                                if ((x * 17 + y * 23) % 157 == 0) garden = new Color32(125, 225, 255, 255);
                                else if ((x * 29 + y * 13) % 181 == 0) garden = new Color32(255, 145, 225, 255);
                                pixels[idx] = garden;
                            }
                            else
                            {
                                // 별빛 성소 기어 회로 바닥
                                bool circuit = ((x % 48 == 0 && y % 24 < 18) || (y % 48 == 0 && x % 24 < 18));
                                pixels[idx] = circuit
                                    ? new Color32(165, 142, 68, 255)
                                    : new Color32((byte)(22 + noise / 3), (byte)(36 + noise / 2), (byte)(56 + noise / 2), 255);
                            }
                        }
                        continue;
                    }

                    // 4. 위쪽 과거의 기억 대광장/테라스/신전 (y > canalMaxY)
                    if (y > canalMaxY)
                    {
                        float cx = 432f;
                        float cy = 492f;
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                        bool inGrandPlaza = (x >= 76 && x <= w - 76 && y <= h - 34);

                        if (inGrandPlaza)
                        {
                            bool tileGrid = (x % 28 == 0) || (y % 28 == 0);
                            bool checker = ((x / 28 + y / 28) % 2 == 0);

                            // 대형 이중 마법진 / 시계 다이얼 / 팔레트 링
                            if (Mathf.Abs(dist - 88f) < 2.8f || Mathf.Abs(dist - 58f) < 2.4f)
                            {
                                pixels[idx] = new Color32(222, 188, 86, 255);
                            }
                            else if (stageType == 1)
                            {
                                pixels[idx] = tileGrid
                                    ? new Color32(58, 64, 86, 255)
                                    : (checker ? new Color32(84, 92, 118, 255) : new Color32(76, 84, 108, 255));
                            }
                            else if (stageType == 2)
                            {
                                pixels[idx] = tileGrid
                                    ? new Color32(68, 54, 96, 255)
                                    : (checker ? new Color32(106, 86, 142, 255) : new Color32(92, 74, 126, 255));
                            }
                            else
                            {
                                pixels[idx] = tileGrid
                                    ? new Color32(48, 78, 96, 255)
                                    : (checker ? new Color32(64, 102, 124, 255) : new Color32(54, 88, 108, 255));
                            }
                        }
                        else
                        {
                            int noise = ((x * 19 + y * 31) % 20);
                            pixels[idx] = stageType == 1
                                ? new Color32((byte)(34 + noise / 2), (byte)(76 + noise), (byte)(58 + noise / 2), 255)
                                : stageType == 2
                                    ? new Color32((byte)(46 + noise / 2), (byte)(42 + noise / 2), (byte)(82 + noise), 255)
                                    : new Color32((byte)(22 + noise / 3), (byte)(36 + noise / 2), (byte)(56 + noise / 2), 255);
                        }
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 광장 중앙 원형 석조 분수대 프롭 (72 x 72 px)
        /// </summary>
        private static void GenerateFountainPropSprite(string path)
        {
            if (File.Exists(path)) return;
            int size = 72;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = 35.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    if (d > 32f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (d >= 26f)
                    {
                        tex.SetPixel(x, y, new Color(0.62f, 0.66f, 0.76f, 1f)); // 석조 분수 테두리
                    }
                    else if (d <= 8f)
                    {
                        tex.SetPixel(x, y, new Color(0.85f, 0.92f, 1.0f, 1f)); // 중앙 물줄기 코어
                    }
                    else
                    {
                        float ripple = Mathf.Sin(d * 0.8f) * 0.5f + 0.5f;
                        tex.SetPixel(x, y, Color.Lerp(new Color(0.18f, 0.55f, 0.82f, 1f), new Color(0.45f, 0.85f, 0.98f, 1f), ripple));
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 화가의 대형 원목 이젤 & 캔버스 프롭 (64 x 80 px)
        /// </summary>
        private static void GenerateEaselPropSprite(string path)
        {
            if (File.Exists(path)) return;
            int w = 64;
            int h = 80;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float cx = 31.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // 캔버스 액자 (x: 12~52, y: 26~66)
                    if (x >= 12 && x <= 52 && y >= 26 && y <= 66)
                    {
                        bool frame = (x < 16 || x > 48 || y < 30 || y > 62);
                        if (frame)
                        {
                            tex.SetPixel(x, y, new Color(0.78f, 0.62f, 0.28f, 1f));
                        }
                        else
                        {
                            // 달빛 밤하늘 풍경화
                            float moon = Vector2.Distance(new Vector2(x, y), new Vector2(38f, 52f));
                            tex.SetPixel(x, y, moon < 6f ? new Color(1f, 0.95f, 0.65f, 1f) : new Color(0.24f, 0.32f, 0.62f, 1f));
                        }
                        continue;
                    }

                    // 이젤 나무 다리
                    bool leftLeg = Mathf.Abs((x - 18f) - (y - 6f) * 0.25f) < 2.2f && y >= 6 && y <= 74;
                    bool rightLeg = Mathf.Abs((x - 45f) + (y - 6f) * 0.25f) < 2.2f && y >= 6 && y <= 74;
                    bool centerLeg = Mathf.Abs(x - cx) < 2.0f && y >= 8 && y <= 76;
                    if (leftLeg || rightLeg || centerLeg)
                    {
                        tex.SetPixel(x, y, new Color(0.48f, 0.30f, 0.16f, 1f));
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
        /// 1번 맵: 『집 거실 (Living Room)』 32비트 2D RPG 도트 맵 (480 x 320 px)
        /// 따뜻한 원목 마루, 상단 베란다 창문과 커튼, TV 장식장, 책장, 중앙 L자 소파 & 러그 & 커피 테이블, 화분, 하단 현관문!
        /// </summary>
        private static void GenerateLivingRoomMapSprite(string path)
        {
            int w = 480;
            int h = 320;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = SampleLivingRoomPixel(x, y, w, h);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static Color32 SampleLivingRoomPixel(int x, int y, int w, int h)
        {
            // 외곽 집 벽면 테두리
            if (x < 12 || x >= w - 12 || y < 12 || y >= h - 12)
                return new Color32(62, 46, 38, 255);

            // 상단 거실 벽면 (y >= 242)
            if (y >= 242)
            {
                // 왼쪽 대형 창문 (x: 42~162, y: 256~302) - 노을빛 하늘과 커튼
                if (x >= 42 && x <= 162 && y >= 256 && y <= 302)
                {
                    if (x < 56 || x > 148) return (x % 4 < 2) ? new Color32(218, 175, 138, 255) : new Color32(195, 148, 112, 255); // 커튼
                    if (x < 58 || x > 146 || y < 260 || y > 298 || Mathf.Abs(x - 102) < 2) return new Color32(118, 86, 62, 255); // 창틀
                    float t = (y - 260f) / 38f;
                    return Color.Lerp(new Color(0.95f, 0.58f, 0.35f), new Color(0.28f, 0.35f, 0.62f), t); // 노을 창밖 풍경
                }

                // 중앙 TV 장식장 & TV (x: 192~288, y: 244~292)
                if (x >= 198 && x <= 282 && y >= 262 && y <= 296)
                {
                    bool bezel = (x < 203 || x > 277 || y < 267 || y > 291);
                    return bezel ? new Color32(32, 34, 42, 255) : new Color32(68, 92, 118, 255); // TV 화면
                }
                if (x >= 186 && x <= 294 && y >= 244 && y <= 261)
                {
                    return (y % 6 == 0) ? new Color32(92, 62, 40, 255) : new Color32(138, 96, 62, 255); // TV 콘솔장
                }

                // 오른쪽 원목 책장 (x: 330~442, y: 246~304)
                if (x >= 330 && x <= 442 && y >= 246 && y <= 304)
                {
                    if (x < 335 || x > 437 || y % 16 < 3) return new Color32(102, 68, 42, 255);
                    int b = (x / 7 + y / 16) % 4;
                    return b == 0 ? new Color32(185, 78, 72, 255) :
                           b == 1 ? new Color32(72, 132, 178, 255) :
                           b == 2 ? new Color32(85, 152, 98, 255) :
                                    new Color32(215, 178, 78, 255);
                }

                // 따뜻한 크림 베이지 벽지 + 하단 걸레받이 몰딩
                if (y < 248) return new Color32(128, 94, 66, 255);
                return (x % 16 == 0) ? new Color32(218, 202, 176, 255) : new Color32(232, 218, 194, 255);
            }

            // 하단 우측 현관 타일 & 현관문 (x: 365~462, y: 14~64)
            if (x >= 365 && x <= 462 && y <= 64)
            {
                bool tile = (x % 16 == 0) || (y % 16 == 0);
                return tile ? new Color32(82, 86, 94, 255) : new Color32(128, 134, 144, 255);
            }

            // 중앙 L자 패브릭 소파 (가로 등받이 x: 150~330, y: 170~208 / 세로 팔걸이 x: 150~184, y: 120~170)
            bool onSofaTop = (x >= 152 && x <= 328 && y >= 174 && y <= 212);
            bool onSofaLeft = (x >= 152 && x <= 186 && y >= 118 && y <= 174);
            if (onSofaTop || onSofaLeft)
            {
                bool cushionLine = (x % 44 == 0) || (y == 202) || (x == 158);
                return cushionLine ? new Color32(128, 92, 68, 255) : new Color32(198, 156, 118, 255);
            }

            // 중앙 원목 커피 테이블 (x: 208~304, y: 118~154)
            if (x >= 208 && x <= 304 && y >= 118 && y <= 154)
            {
                bool border = (x < 212 || x > 300 || y < 122 || y > 150);
                return border ? new Color32(96, 62, 36, 255) : new Color32(156, 108, 68, 255);
            }

            // 중앙 거실 따뜻한 패브릭 러그 카펫 (x: 132~348, y: 88~224)
            if (x >= 132 && x <= 348 && y >= 88 && y <= 224)
            {
                bool rugEdge = (x < 138 || x > 342 || y < 94 || y > 218);
                if (rugEdge) return new Color32(198, 152, 86, 255);
                bool weave = ((x + y) % 8 == 0);
                return weave ? new Color32(172, 112, 92, 255) : new Color32(156, 98, 82, 255);
            }

            // 좌측 하단 & 우측 상단 관엽식물 화분
            float p1 = Vector2.Distance(new Vector2(x, y), new Vector2(46f, 218f));
            float p2 = Vector2.Distance(new Vector2(x, y), new Vector2(436f, 218f));
            if (p1 < 14f || p2 < 14f)
            {
                return ((x + y) % 5 == 0) ? new Color32(46, 118, 68, 255) : new Color32(68, 158, 88, 255);
            }

            // 따뜻한 원목 마룻바닥
            bool plank = (y % 12 == 0) || ((x + (y / 12) * 24) % 48 == 0);
            int grain = (x * 5 + y * 9) % 10;
            return plank
                ? new Color32(118, 84, 54, 255)
                : new Color32((byte)(172 + grain), (byte)(132 + grain), (byte)(88 + grain / 2), 255);
        }

        /// <summary>
        /// 2번 맵: 『지하철역 (Subway Station)』 32비트 2D RPG 도트 맵 (480 x 320 px)
        /// 상단 선로(자갈·침목·레일)와 정차 전동차·스크린도어, 노란 점자블록 안전선, 승강장 타일, 역명판 기둥, 벤치, 하단 개찰구!
        /// </summary>
        private static void GenerateSubwayStationMapSprite(string path)
        {
            int w = 480;
            int h = 320;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = SampleSubwayPixel(x, y, w, h);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static Color32 SampleSubwayPixel(int x, int y, int w, int h)
        {
            // 외곽 콘크리트 벽
            if (x < 10 || x >= w - 10 || y < 10 || y >= h - 10)
                return new Color32(42, 48, 58, 255);

            // 상단 지하철 선로 & 정차한 전동차 구역 (y >= 238)
            if (y >= 238)
            {
                // 전동차 지붕 및 창문 실루엣 (x: 40~440, y: 266~306)
                if (x >= 40 && x <= 440 && y >= 268 && y <= 306)
                {
                    if (y >= 274 && y <= 284 && (x % 42 >= 8 && x % 42 <= 32))
                        return new Color32(165, 215, 245, 255); // 전동차 창문 불빛
                    if (y >= 268 && y <= 272)
                        return new Color32(42, 118, 205, 255); // 지하철 파란 띠
                    return (x % 84 < 2) ? new Color32(72, 78, 88, 255) : new Color32(182, 190, 202, 255);
                }

                // 선로 철도 레일 2줄 + 나무 침목 + 자갈
                if (y == 248 || y == 249 || y == 260 || y == 261)
                    return new Color32(168, 176, 188, 255); // 금속 레일
                if (x % 14 < 5 && y >= 244 && y <= 265)
                    return new Color32(78, 62, 52, 255); // 침목
                int gravel = (x * 17 + y * 29) % 24;
                return new Color32((byte)(44 + gravel), (byte)(46 + gravel), (byte)(54 + gravel), 255);
            }

            // 스크린도어 경계 라인 (y: 230~237)
            if (y >= 230 && y <= 237)
            {
                bool doorGlass = (x % 60 >= 10 && x % 60 <= 50);
                return doorGlass ? new Color32(118, 172, 205, 255) : new Color32(82, 92, 106, 255);
            }

            // 노란색 안전선 & 점자블록 (y: 212~224)
            if (y >= 212 && y <= 224)
            {
                bool dot = (x % 4 < 2) && (y % 4 < 2);
                return dot ? new Color32(248, 212, 52, 255) : new Color32(222, 178, 28, 255);
            }

            // 승강장 기둥 3개 (파란 역명판 띠 포함: x=100, 240, 380 / y=135~168)
            bool onPillar = (Mathf.Abs(x - 100) < 16 || Mathf.Abs(x - 240) < 16 || Mathf.Abs(x - 380) < 16) && (y >= 135 && y <= 168);
            if (onPillar)
            {
                if (y >= 148 && y <= 156) return new Color32(36, 112, 208, 255); // 파란 역명판 띠
                return new Color32(198, 204, 214, 255);
            }

            // 승강장 대기 벤치 2개 (x: 138~202, 278~342 / y: 144~160)
            bool onBench = ((x >= 138 && x <= 202) || (x >= 278 && x <= 342)) && (y >= 144 && y <= 160);
            if (onBench)
            {
                return (x % 16 < 2) ? new Color32(58, 66, 78, 255) : new Color32(72, 128, 178, 255);
            }

            // 하단 지하철 개찰구 게이트 (y: 24~56)
            if (y >= 24 && y <= 56 && x >= 70 && x <= 410)
            {
                if (x % 48 < 12)
                {
                    if (y >= 46 && y <= 52) return new Color32(58, 218, 112, 255); // 초록 통과등
                    return new Color32(158, 166, 178, 255); // 금속 개찰구 기둥
                }
            }

            // 지하철 승강장 밝은 회색 대리석 바닥 타일
            bool grid = (x % 20 == 0) || (y % 20 == 0);
            int shade = ((x / 20 + y / 20) % 2 == 0) ? 6 : 0;
            return grid
                ? new Color32(96, 104, 118, 255)
                : new Color32((byte)(136 + shade), (byte)(144 + shade), (byte)(158 + shade), 255);
        }

        /// <summary>
        /// 3번 맵: 『기억 속의 공간 (Memory Dreamscape)』 32비트 2D RPG 도트 맵 (480 x 320 px)
        /// 1번맵(집 거실)과 2번맵(지하철역)이 중앙의 황금빛 기억 경계를 중심으로 자연스럽게 어우러지는 초현실 융합 공간!
        /// </summary>
        private static void GenerateMemoryFusionMapSprite(string path)
        {
            int w = 480;
            int h = 320;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // 왼쪽은 집 거실, 오른쪽은 지하철역이 물결치는 기억 경계선으로 자연스럽게 융합
                    float waveBoundary = (w * 0.5f) + Mathf.Sin(y * 0.045f) * 32f + Mathf.Cos(y * 0.09f) * 12f;
                    float distFromBoundary = x - waveBoundary;

                    Color32 homeCol = SampleLivingRoomPixel(x, y, w, h);
                    Color32 subwayCol = SampleSubwayPixel(x, y, w, h);

                    if (Mathf.Abs(distFromBoundary) < 5f)
                    {
                        // 두 기억이 만나는 황금빛 별빛 균열 경계
                        pixels[y * w + x] = new Color32(248, 218, 112, 255);
                    }
                    else if (Mathf.Abs(distFromBoundary) < 26f)
                    {
                        // 자연스러운 그라데이션 블렌딩 + 몽환적인 보라/황금 기억 안개
                        float t = Mathf.Clamp01((distFromBoundary + 26f) / 52f);
                        Color blended = Color.Lerp(homeCol, subwayCol, t);
                        blended = Color.Lerp(blended, new Color(0.62f, 0.52f, 0.88f, 1f), 0.22f);
                        if ((x * 23 + y * 41) % 131 == 0) blended = new Color(1f, 0.92f, 0.55f, 1f);
                        pixels[y * w + x] = blended;
                    }
                    else if (distFromBoundary < 0f)
                    {
                        // 왼쪽: 따뜻한 노을/기억빛이 감도는 집 거실
                        Color c = Color.Lerp(homeCol, new Color(0.95f, 0.78f, 0.52f, 1f), 0.12f);
                        pixels[y * w + x] = c;
                    }
                    else
                    {
                        // 오른쪽: 몽환적인 푸른빛이 감도는 지하철역
                        Color c = Color.Lerp(subwayCol, new Color(0.38f, 0.52f, 0.88f, 1f), 0.16f);
                        pixels[y * w + x] = c;
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateVoidSlotSprite(string path)
        {
            int w = 132;
            int h = 112;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool onBorder = (x < 4 || x >= w - 4 || y < 4 || y >= h - 4);
                    bool dashed = ((x + y) / 10) % 2 == 0;

                    if (onBorder)
                    {
                        tex.SetPixel(x, y, dashed ? new Color(1.0f, 0.85f, 0.42f, 0.98f) : new Color(0.42f, 0.24f, 0.68f, 0.92f));
                        continue;
                    }

                    // 빈 배경 구멍(복원 시 이 가림막이 사라지며 아래의 실제 거실/지하철역 배경이 나타남!)
                    float nx = (x - w * 0.5f) / (w * 0.5f);
                    float ny = (y - h * 0.5f) / (h * 0.5f);
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    Color voidCol = Color.Lerp(new Color(0.06f, 0.05f, 0.12f, 0.94f), new Color(0.14f, 0.10f, 0.24f, 0.92f), Mathf.Clamp01(r));
                    if ((x * 37 + y * 53) % 181 == 0)
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

        private static void ConfigureSpriteImporter(string path, float ppu, FilterMode filterMode, int maxTexSize = 1024)
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
                if (importer.maxTextureSize < maxTexSize)
                {
                    importer.maxTextureSize = maxTexSize;
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
