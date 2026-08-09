# Map V2 Windows feedback — Quest rendering / floor / marker visibility — 2026-08-10

상태: `IMPLEMENTED / FINAL CI IN PROGRESS / WINDOWS USER VALIDATION NEXT`

## 사용자 실사용 피드백

1. Quest sidebar의 A/B/C badge 자체의 왼쪽 시작점이 서로 같아야 함.
   - 의도: `(A) Quest 1`, `(B) Quest 2`, `(C) Quest 3`에서 badge와 Quest 이름 기준선이 모두 동일.
   - checkbox 유무, Quest 이름 길이, badge 문자 때문에 badge가 가운데로 밀리면 안 됨.
2. sidebar에 `좌표 N개`가 표시되고 per-Quest/global marker checkbox가 ON이어도 A/B/C Quest marker가 실제 지도에 표시되지 않음.
3. floor 전환이 동작하지 않음.
4. 일부 일반 지도 marker가 표시되지 않는 사례가 있음.

## 확인된 Quest 데이터 상태

Quest 좌표는 실제로 수집되어 **화면 좌표 projection 단계까지 도달하고 있음**.

`LegacyMapQuestV2Controller.BuildEntries()` 경로:

```text
GameContentCatalog.QuestObjectives
→ QuestObjective.MapLocations
→ 현재 Map 일치 필터
→ MapTrackerService.TransformGameCoordinate(X, Z)
→ 성공한 projection만 entry.Markers에 저장
→ sidebar의 `좌표 N개` = entry.Markers.Count
```

따라서 사용자 Windows 화면의 `좌표 2개`, `좌표 3개` 표시는 단순 metadata count가 아니라 **Map coordinate transform까지 성공해 실제 렌더링 직전까지 준비된 Quest marker projection 수**임.

Quest importer는 online task objective의 `possibleLocations` / `zones`를 Quest domain에 저장함.

즉 이번 Quest marker 문제는 source/수집/좌표 변환 문제가 아니라 렌더링 문제로 확정함.

## Quest marker 렌더링 수정

문제 구조:

```text
Width=0 / Height=0 Grid
└─ 24px badge를 RenderTransform으로 이동
```

원본 Tarkov Helper에서 정상 동작하는 marker 구조:

```text
Width=0 / Height=0 Canvas anchor
└─ child icon/badge에 Canvas.Left / Canvas.Top offset
```

수정:

- Main Map Quest marker를 exact marker와 같은 `Canvas anchor` 방식의 V3 renderer로 교체.
- MiniMap도 동일 V3 Quest visual factory 사용.
- 기존 projection/checkbox/A-B-C identity는 유지하고 visual arrange 방식만 수정.
- CI Map smoke에서 V3 Quest visual이 실제 Canvas anchor + child badge로 생성되는지 검사.

## Quest sidebar 정렬 수정

이전 보정은 checkbox lane만 고정하고 A/B/C badge를 variable-width Button content 안에 남겨두어 Quest 이름 길이에 따라 badge가 가운데로 밀릴 수 있었음.

수정 구조:

```text
[28px checkbox lane] [29px A/B/C badge lane] [Quest text *]
```

- badge가 있는 row와 없는 row 모두 같은 3-column layout 사용.
- A/B/C badge 자체의 시작 x가 완전히 동일.
- Quest 이름 시작 x도 완전히 동일.
- 이전 build가 삽입한 transparent placeholder badge는 제거.

## Floor 구현 수정

V2 bridge에서 원본 `CmbFloorSelect`를 숨기고 별도의 복제 ComboBox를 생성해 양방향 동기화하고 있었음. 이 복제 계층을 폐기함.

원본 Tarkov Helper `CmbFloorSelect_SelectionChanged` 경로를 그대로 다시 사용함.

```text
currentFloorId 변경
→ selected floor SVG reload
→ extract refresh
→ Quest refresh
→ general marker refresh
```

추가 제품 정책:

- screenshot floor auto-detection은 계속 금지.
- 현재 사용자가 선택한 floor만 표시.
- exact loader가 기존 default floor를 반투명 background로 추가하지 않도록 floor selection 직전에 현재 선택 floor를 visual default로 지정.
- 따라서 non-selected floor는 0% 정책을 유지.
- CI Map smoke에서 실제로 `Customs`를 선택 → multi-floor selector 확인 → 다른 floor 선택 → SVG source 교체까지 검증.

## 일반 marker 데이터/표시 조사

직전 Windows 배포 artifact의 `Assets/tarkov_data.db` + `map_configs.json`을 직접 검사함.

```text
MapMarkers records: 454
playerMarkerTransform 후 map image bounds 밖: 0
multi-floor FloorId와 config layerId 불일치: 0
```

따라서 현재 bundle의 일반 marker에서 확인된 missing 현상도 좌표 자체의 손상이 원인은 아님.

확인된 V2 visibility 충돌:

- V2 interaction bridge가 200ms마다 일반 marker / extract의 `Visibility`를 floor 조건만으로 다시 기록함.
- 원본 marker manager가 checkbox/category 상태로 정한 visibility와 별개의 writer가 하나 더 생긴 상태였음.
- Extract 설정은 원본에서 `SettingsService`를 사용하고 일반 marker는 별도 MapSettings 경로도 있어, 다른 setting object를 읽어 덮어쓰는 방식도 안전하지 않았음.

수정:

```text
visible = 현재 선택 floor에 해당
          AND 화면의 실제 해당 category checkbox가 ON
```

직접 읽는 제품 UI toggle:

- PMC Spawn
- Sniper Scav
- Rogue
- Cultist
- Boss
- Lever
- PMC Extract
- Scav Extract
- Transit

Shared extract는 PMC 또는 Scav 중 하나가 ON이면 표시함.
Raider는 별도 product layer의 자체 filter/floor 로직을 유지함.

## 검증 기준

- Quest sidebar A/B/C badge x 좌표 통일.
- 사용자 화면에서 이미 `좌표 N개`인 Quest는 체크 시 Main Map에 A/B/C 실제 표시.
- 동일 Quest marker가 MiniMap에도 표시.
- multi-floor Map에서 원본 floor selector가 실제 SVG를 전환.
- screenshot이 floor를 변경하지 않음.
- 선택하지 않은 floor는 표시하지 않음.
- marker category checkbox와 floor-only 정책이 서로 덮어쓰지 않음.
- Desktop Release build / automated tests / Windows x64 publish / enhanced Startup + Map smoke 통과.
