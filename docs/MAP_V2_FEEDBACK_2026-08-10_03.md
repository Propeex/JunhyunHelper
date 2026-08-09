# Map V2 Windows feedback — Quest rendering / floor / marker visibility — 2026-08-10

상태: `USER CONFIRMED / IMPLEMENTATION IN PROGRESS`

## 사용자 실사용 피드백

1. Quest sidebar의 A/B/C badge 자체의 왼쪽 시작점이 서로 같아야 함.
   - 의도: `(A) Quest 1`, `(B) Quest 2`, `(C) Quest 3`에서 badge와 Quest 이름 기준선이 모두 동일.
   - checkbox 유무, Quest 이름 길이, badge 문자 때문에 badge가 가운데로 밀리면 안 됨.
2. sidebar에 `좌표 N개`가 표시되고 per-Quest/global marker checkbox가 ON이어도 A/B/C Quest marker가 실제 지도에 표시되지 않음.
3. floor 전환이 동작하지 않음.
4. 일부 일반 지도 marker가 표시되지 않는 사례가 있음.

## 확인된 데이터 상태

Quest 좌표는 실제로 수집되어 화면 좌표 projection 단계까지 도달하고 있음.

`LegacyMapQuestV2Controller.BuildEntries()`는 다음 경로를 사용함.

```text
GameContentCatalog.QuestObjectives
→ QuestObjective.MapLocations
→ 현재 Map 일치 필터
→ MapTrackerService.TransformGameCoordinate(X, Z)
→ 성공한 projection만 entry.Markers에 저장
→ sidebar의 `좌표 N개` = entry.Markers.Count
```

따라서 Windows 화면의 `좌표 2개`, `좌표 3개` 표시는 단순 metadata count가 아니라 **Map coordinate transform까지 성공한 Quest marker projection 수**임.

Quest source importer는 online task objective의 `possibleLocations` / `zones`를 Quest domain에 저장함.

## 확인된 Quest marker 렌더링 문제

현재 Quest visual factory는 `Width=0`, `Height=0`인 `Grid` 안에 24px badge를 넣고 RenderTransform으로 이동함.

원본 Tarkov Helper에서 정상 동작하는 marker는 `Width=0`, `Height=0`인 `Canvas`를 anchor로 사용하고 child에 `Canvas.Left/Top` offset을 적용함.

제품 Quest marker도 원본 marker와 동일한 anchor pattern으로 변경함.

## Floor 구현 정리

V2 bridge에서 원본 `CmbFloorSelect`를 숨기고 별도의 복제 ComboBox를 생성해 양방향 동기화하고 있었음. 원본 Tarkov Helper는 이미 `CmbFloorSelect_SelectionChanged`에서 다음을 수행함.

```text
currentFloorId 변경
→ selected floor SVG reload
→ extract refresh
→ Quest refresh
→ general marker refresh
```

따라서 복제 floor selector를 폐기하고 **원본 selector 자체를 제품 수동 floor selector로 사용**함.

Screenshot floor auto-detection은 계속 금지함.

## Marker visibility 정리

V2 interaction bridge가 200ms마다 일반 marker / extract의 Visibility를 floor 기준만으로 덮어쓰고 있었음. 이 동작은 원본 marker manager의 category checkbox visibility와 충돌할 수 있음.

수정 원칙:

```text
visible = 현재 선택 floor에 해당
          AND 해당 marker category checkbox가 ON
```

- PMC Spawn / Sniper Scav / Rogue / Cultist / Boss / Lever는 MapSettings의 해당 visibility를 존중함.
- PMC / Scav / Shared / Transit extract는 각각의 extract visibility를 존중함.
- 별도 Raider layer는 자체 floor/filter 로직을 유지함.

## 구현 기준

- Quest sidebar: checkbox lane + badge lane + Quest text lane을 고정 column으로 사용.
- A/B/C badge는 항상 동일한 x 좌표.
- Quest text도 항상 동일한 x 좌표.
- Quest visual factory는 Canvas anchor 방식.
- 원본 floor selector 직접 사용.
- screenshot은 Map/position/heading만, floor는 절대 자동 변경하지 않음.
- floor-only 정책이 category filter state를 덮어쓰지 않음.
- 가능한 범위에서 자동 회귀 테스트 및 Windows Map smoke를 추가/유지함.
