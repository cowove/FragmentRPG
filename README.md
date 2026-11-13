# Fragment RPG — 캡스톤디자인 프로젝트 (Team Re:Call)

## 1. 프로젝트 개요
- **작품명**: Fragment RPG
- **장르**: 2D 스토리 퍼즐 어드벤처 (Top-down 2D Adventure & Mystery Puzzle)
- **개발팀**: 팀 Re:Call
- **엔진 및 환경**: Unity 2D (C#)

---

## 2. 세계관 및 줄거리
기억을 수리하고 복원해 주는 신비한 공방 **'기억 수리점'**.
주인공(기억 수리공)은 손님들의 손상된 기억 공간 속으로 직접 들어가 흩어진 **기억 조각(Clue Fragment)**을 수집하고, 비어진 기억의 배경을 맞춰 온전한 기억으로 복원한다.

### 에피소드 구성
1. **Episode 1 — 시계공의 멈춘 시간**
   - 멈춰버린 회중시계를 들고 찾아온 노년의 시계공.
   - 시계탑 광장 기억 공간을 탐색하며, 끊어진 다리와 산책로의 기억 조각(1, 2, 3)을 모아 배경을 복원하고 그날의 기억을 되찾는다.
2. **Episode 2 — 화가의 지워진 캔버스**
   - 색채를 잃어버린 미완성 자화상을 의뢰한 화가.
   - 물감과 액자가 왜곡된 화실 기억 공간 속에서 모순된 기억을 찾아내고 사라진 오브제들을 원래 위치로 복원한다.
3. **Episode 3 — 수리공의 잃어버린 방**
   - 두 손님의 기억을 복원한 끝에 마주하게 되는 주인공 자신의 봉인된 기억 공간.

---

## 3. 핵심 시스템 구조
```text
FragmentRPG/
├── README.md
└── Assets/
    ├── Scripts/
    │   ├── PlayerController.cs       # Rigidbody2D 기반 Top-down 2D 상하좌우 이동 및 충돌 처리
    │   ├── InteractionManager.cs     # 플레이어 반경 내 상호작용 오브젝트 감지 (E / Space)
    │   ├── InteractableObject.cs     # 기억조각 수집, 비어진 배경 슬롯 복원, 대사 출력 컴포넌트
    │   ├── PuzzleManager.cs          # 배경 복원 퍼즐 및 3대 퍼즐 정답 검증 매니저
    │   ├── FragmentUIManager.cs      # 인벤토리, 대화창, 퍼즐 선택창 UI 매니저
    │   └── StorySaveManager.cs       # 진행 상태 JSON 저장 및 불러오기 매니저
    ├── Resources/
    │   └── FragmentMasterDB.json     # 대사, 기억 조각, 퍼즐 규칙 마스터 데이터
    └── Editor/
        └── Editor_RoomBuilder.cs     # 씬 오브젝트 및 배경 자동 배치 에디터 도구
```
