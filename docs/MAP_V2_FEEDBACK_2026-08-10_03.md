# Map V2 Windows feedback — Quest rendering / floor / marker visibility — 2026-08-10

상태: `MERGED / AUTOMATED VALIDATION PASSED / WINDOWS USER VALIDATION NEXT`

## 사용자 실사용 피드백

1. Quest sidebar의 A/B/C badge 자체의 왼쪽 시작점이 서로 같아야 함.
2. sidebar에 `좌표 N개`가 표시되고 checkbox가 ON이어도 A/B/C Quest marker가 실제 지도에 표시되지 않음.
3. floor 전환이 동작하지 않음.
4. 일부 일반 지도 marker가 표시되지 않는 사례가 있음.

## Quest 좌표 데이터 확인

Quest 좌표는 실제로 수집되어 화면 좌표 projection 단계까지 도달하고 있었습니다.

```text
GameContentCatalog.QuestObjectives
→ QuestObjective.MapLocations
→ 현재 Map 일치 필터
→ MapTrackerService.TransformGameCoordinate(X, Z)
→ 성공한 projection만 entry.Markers
→ sidebar `좌표 N개` = entry.Markers.Count
```

따라서 사용자 화면의 `좌표 2개`, `좌표 3개`는 단순 metadata가 아니라 **Map coordinate transform까지 성공해 render 직전까지 준비된 marker 수**입니다.

Quest importer는 online task objective의 `possibleLocations` / `zones`를 Quest domain에 저장합니다.

## Quest marker 원인 / 수정

문제 구조:

```text
0x0 Grid
└─ 24px badge를 RenderTransform으로 이동
```

원본 Tarkov Helper의 검증된 marker 구조:

```text
0x0 Canvas anchor
└─ child에 Canvas.Left / Canvas.Top offset
```

수정:

- Main Map에 V3 Quest renderer 추가.
- Quest badge를 exact marker와 같은 Canvas anchor 방식으로 렌더링.
- MiniMap도 동일 V3 visual factory 사용.
- 기존 projection / checkbox / A-B-C identity는 유지.

## Quest sidebar 정렬

이전에는 A/B/C badge가 variable-width Button content 안에 있어 Quest 이름 길이에 따라 badge가 밀릴 수 있었습니다.

현재 구조:

```text
[28px checkbox] [29px A/B/C badge] [Quest text *]
```

- badge 자체의 X 위치 통일.
- Quest 이름 시작 X 통일.
- 좌표 없는 Quest도 동일 lane 유지.

## Floor 수정

V2에서 원본 `CmbFloorSelect`를 숨기고 만든 복제 ComboBox를 폐기했습니다.

현재는 **exact Tarkov Helper 원본 floor selector + 원본 `CmbFloorSelect_SelectionChanged` 경로**를 직접 사용합니다.

```text
currentFloorId 변경
→ selected floor SVG reload
→ extract refresh
→ Quest refresh
→ general marker refresh
```

제품 정책:

- screenshot floor auto-detection은 계속 금지.
- 현재 사용자가 선택한 floor만 표시.
- floor selection 직전 현재 선택 floor를 visual default로 지정하여 old default floor가 반투명 background로 추가되지 않게 함.
- non-selected floor opacity 0% 유지.

## 일반 marker 조사 / 수정

직전 Windows artifact의 `Assets/tarkov_data.db` + `map_configs.json` 직접 검사:

```text
MapMarkers records: 454
playerMarkerTransform 후 image bounds 밖: 0
multi-floor FloorId / config layerId 불일치: 0
```

따라서 확인된 missing 현상은 marker 좌표 손상이 아니었습니다.

V2 interaction bridge가 주기적으로 marker `Visibility`를 floor 조건만으로 덮어써 원본 category checkbox 상태와 충돌할 수 있었습니다.

현재 규칙:

```text
visible = 현재 선택 floor
          AND 실제 화면 category checkbox ON
```

적용 대상:

- PMC Spawn
- Sniper Scav
- Rogue
- Cultist
- Boss
- Lever
- PMC Extract
- Scav Extract
- Transit

Shared extract는 PMC 또는 Scav 중 하나가 ON이면 표시합니다. Raider는 별도 product layer의 자체 filter/floor 로직을 유지합니다.

## Git / 검증

```text
PR #67: Fix Quest marker rendering and restore floor switching
merge: 7d248d7346760d126b839d69318648e504ac39fc
final head: 81cddd9bcd151a9b4bea19d764e00cc1798f7d65
CI: 31328655090
artifact: 9042291967
artifact digest: sha256:bd0b18d3a9d54bd12b3e797f8f2b898a9fc57326b34d783a48e286dcf1a232bc
```

검증 결과:

- Desktop Release build: success
- automated tests: success
- Windows x64 self-contained publish: success
- ZIP creation/upload: success
- enhanced Startup + Map smoke: success

강화된 Map smoke는 다음을 실제 WPF runtime에서 수행했습니다.

- V3 Quest marker가 Canvas anchor + child badge로 생성되는지 검사.
- Customs 선택.
- multi-floor selector가 실제 생성/표시되는지 검사.
- 다른 floor를 실제 선택.
- floor 변경 후 SVG source가 실제 교체되는지 검사.

## Windows 사용자 검증 항목

- A/B/C badge가 동일 X 위치에 정렬되는지.
- `좌표 N개`인 Quest의 A/B/C marker가 Main Map에 실제 표시되는지.
- MiniMap에도 동일 Quest marker가 표시되는지.
- Customs / Reserve / Factory 등에서 floor 전환이 실제 동작하는지.
- 선택하지 않은 floor가 표시되지 않는지.
- 일반 marker checkbox와 marker 표시가 일치하는지.
