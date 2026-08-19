# MAP_RUNTIME_COMPATIBILITY — pinned donor와 JunhyunHelper 제품 정책의 runtime 경계

상태: `ACTIVE / v1.0.0 BASELINE`

기준일: 2026-08-19

이 문서는 JunhyunHelper가 pinned `Tarkov-Helper` Map/MiniMap donor source를 그대로 사용하면서, donor의 과거 동작 중 현재 JunhyunHelper 제품 계약과 충돌하는 부분을 first-party compatibility layer에서 어떻게 제한하는지 설명합니다.

제품 요구사항의 권위는 `MAP_PRODUCT_REQUIREMENTS.md`, donor 취급 원칙의 권위는 `REFERENCE_POLICY.md`입니다.

---

## 1. Source identity

현재 Map/MiniMap source pin:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

재현 가능한 fetch origin:

```text
https://github.com/SIGDrone/Tarkov-Helper.git
```

fetch origin은 source identity가 아닙니다. source identity는 gitlink SHA입니다. v1.0.0에서 원격만 과거 작업 fork에서 공개 upstream으로 바뀌었고 gitlink SHA는 그대로 유지되었습니다.

---

## 2. JunhyunHelper floor 제품 계약

Map floor는 marker visibility filter가 아니라 **presentation relation**입니다.

활성화된 marker는 현재 선택 floor와 다르다는 이유만으로 숨기지 않습니다.

- 현재 floor: 기본 presentation
- 위 floor: visible + 약 75% opacity + above relation ring
- 아래 floor: visible + 약 75% opacity + below relation ring
- cross-floor X/Z near-overlap 자체는 duplicate 증거가 아님

Main Map floor 전환은 zoom + map-space viewport center를 보존하고, MiniMap은 exact live Scale/Translate frame을 보존합니다.

---

## 3. donor에 남아 있는 legacy current-floor filter

pinned donor `MapPage.SharedFloor.cs`에는 과거 current-floor-only marker filter가 남아 있습니다.

핵심 상태:

- `_sharedFloorHiddenMarkers`
- `_sharedMarkerFilterTimer`
- `_sharedMarkerFilterTicksRemaining`

대표 흐름:

```text
Map/floor/position/render 변화
→ ScheduleSharedMarkerFilter()
→ 200 ms interval timer
→ 최대 12회 filter tick (~2.4 s)
→ current floor가 아닌 Visible marker를
   _sharedFloorHiddenMarkers에 기록
→ Visibility = Collapsed
```

이 동작은 현재 JunhyunHelper 제품 계약과 직접 충돌합니다.

---

## 4. v1.0.0 release smoke에서 발견된 race

일반 PR CI에서는 통과했지만 exact-release baseline으로 실제 Windows executable을 publish해 실행한 최종 release smoke에서 다음 상태가 관찰되었습니다.

```text
other-floor standard marker
Visibility = Visible
Opacity = 0.50
Relation = Above
```

기존 `LegacyStandardMarkerFloorPresentationBridge`는 90 ms 간격의 bounded settle 동안 JunhyunHelper presentation을 재적용했지만, donor filter는 그보다 긴 약 2.4초 window 동안 뒤늦게 재실행될 수 있었습니다. position/map event가 filter window를 다시 예약할 수도 있어 최종 상태 race가 남았습니다.

release pipeline은 public Release 생성 전에 이 상태를 blocker로 검출하고 중단했습니다.

---

## 5. first-party compatibility layer

관련 파일:

- `src/JunhyunHelper.Desktop/Map/MapPage.JunhyunCrossFloorMarkerPolicy.cs`
- `src/JunhyunHelper.Desktop/Map/LegacyStandardMarkerFloorPresentationBridge.cs`

### 5.1 핵심 원칙

category/faction visibility를 JunhyunHelper가 다시 계산하지 않습니다.

복구 대상은 donor 자신이 `_sharedFloorHiddenMarkers`에 넣은 요소만입니다. 이 set은 donor가 직전에 **Visible → Collapsed를 floor 이유로 직접 수행한 요소**의 권위 목록입니다.

따라서:

```text
donor filter tick
→ donor가 floor 때문에 숨긴 element만 set에 기록
→ JunhyunHelper post-filter callback
→ 해당 set의 element만 Visibility=Visible 복구
→ set clear
→ existing floor presentation Apply()
→ 75% opacity + above/below relation 재적용
```

category filter, faction filter 또는 사용자가 끈 marker를 임의로 Visible로 만들지 않습니다.

### 5.2 timer ownership

새로운 영구 polling timer를 만들지 않습니다.

compatibility layer는 donor가 원래 사용하는 bounded `_sharedMarkerFilterTimer`에 post-filter callback을 붙입니다. donor timer가 동작하지 않으면 JunhyunHelper callback도 반복 실행되지 않습니다.

### 5.3 event ordering

동일 `Tick` invocation 안에서 donor handler의 등록 순서에 의존하지 않도록 callback은 dispatcher에 correction을 queue합니다. 따라서 current donor tick이 끝난 직후 visibility 복구와 product presentation 재적용이 수행됩니다.

### 5.4 page unload/reload

donor는 `Unloaded`에서 shared marker timer를 null로 만들 수 있습니다. 따라서 product partial은 `Loaded` 시점에 callback이 현재 timer에 다시 연결되어 있는지 확인합니다.

Dispose 시에는:

- product Loaded handler 제거
- donor timer callback 제거
- 남아 있는 floor-only suppression 복구
- presentation callback reference 제거

---

## 6. LegacyStandardMarkerFloorPresentationBridge 책임

이 bridge는 donor의 marker 좌표, marker type, category visibility를 소유하지 않습니다.

책임:

1. current map/floor relation 계산
2. `JunhyunFloorPresentation.Resolve` 사용
3. standard marker의 product opacity/ring presentation 적용
4. donor floor-only suppression compatibility policy attach/detach
5. 실제 marker/map/floor 변화 주변에서만 bounded settle 수행

`JunhyunFloorPresentation`이 presentation semantics의 single point입니다.

---

## 7. 검증 계약

v1.0.0 release smoke는 source inspection이 아니라 실제 publish된 Windows executable에서 검증합니다.

최소 확인:

- Factory Main Map load
- 타층 standard marker 존재
- donor settle window가 지난 뒤에도 marker visible
- product opacity가 약 0.75 범위
- above/below relation ring 유지
- floor switch 후 viewport contract 유지
- Main Map / MiniMap 모두 정상
- 정상 MainWindow close 후 process 종료

late-state 검증은 donor filter window보다 긴 약 3.2초 settle 뒤 수행합니다. 이 시간을 줄이거나 opacity threshold를 완화해서 회귀를 숨기지 않습니다.

---

## 8. 변경 시 영향

### donor revision을 바꾸는 경우

반드시 먼저 donor의 다음 구조가 여전히 존재/동일 의미인지 확인합니다.

- `_sharedFloorHiddenMarkers`
- `_sharedMarkerFilterTimer`
- current-floor-only filtering
- Unloaded timer lifecycle

구조가 바뀌면 compatibility layer를 그대로 유지하거나 삭제할지 다시 판단해야 합니다.

### category/marker filtering을 바꾸는 경우

`_sharedFloorHiddenMarkers`가 여전히 “floor 때문에 donor가 직접 숨긴 요소만” 포함하는지 확인합니다. 이 invariant가 깨지면 현재 restore 전략을 그대로 사용할 수 없습니다.

### floor presentation을 바꾸는 경우

`JunhyunFloorPresentation` → `LegacyStandardMarkerFloorPresentationBridge` → actual release smoke까지 함께 검증합니다.

---

## 9. 하지 말아야 할 것

- donor source pin을 이유 없이 변경
- donor의 모든 Collapsed marker를 전부 Visible로 복구
- marker category/faction visibility를 별도 first-party loop에서 중복 계산
- permanent full-tree polling으로 race를 덮기
- smoke opacity threshold를 낮춰 기존 legacy 상태를 통과시키기
- source만 보고 final runtime state를 검증했다고 간주

v1.0.0의 해결책은 **donor source를 재작성하지 않고, donor가 floor 때문에 변경한 정확한 요소 집합만 제품 정책에 맞게 복구하는 최소 compatibility layer**입니다.
