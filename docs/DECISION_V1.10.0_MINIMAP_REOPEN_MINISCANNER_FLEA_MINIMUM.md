# v1.10.0 MiniMap 재표시 동기화 / Mini Scanner 플리 최저가 결정

기준일: 2026-08-29 KST  
상태: **CONFIRMED / IMPLEMENTATION IN PROGRESS**

## 1. 목적

v1.10.0은 두 개의 사용자 요구사항을 반영한다.

1. Main Map에서 현재 지도를 변경한 뒤 MiniMap을 열거나 다시 표시할 때 MiniMap의 첫 visible/rendered 지도도 반드시 현재 Main Map과 동일해야 한다.
2. Mini Scanner에 현재 Scanner 카탈로그가 제공하는 플리마켓 최저가를 추가하고, 기존 정보 행과 동일하게 표시 여부와 순서를 사용자가 조정할 수 있게 한다.

Scanner 인식 정확도 정책, Item ID 판정, Game Content 관계 데이터, 기존 Map/Factory 의미는 변경하지 않는다.

## 2. MiniMap 실제 회귀와 원인

실사용 재현 계약:

```text
프로그램 시작
→ 지도 탭에서 맵 A가 선택되어 있음
→ visible Main Map selector에서 맵 B 선택
→ MiniMap 표시
→ 첫 visible/rendered MiniMap 지도는 B
```

v1.9.1은 MiniMap `SourceInitialized`/`Loaded` 초기화 경계와 이미 열린 MiniMap의 map-change 경로를 동기화했지만 실제 회귀를 완전히 막지 못했다.

Pinned donor `OverlayMiniMapService`는 `HideOverlay()`에서 Window를 닫지 않고 `Hide()`만 수행한다. 이후 `ShowOverlayCore()`는 같은 loaded Window 인스턴스를 재사용한다. 따라서 재표시 시에는 `SourceInitialized`와 `Loaded`가 다시 발생하지 않는다.

또한 Main Map selector 변경의 normal bridge는 Dispatcher queue를 사용할 수 있으므로 사용자가 지도를 바꾼 직후 MiniMap을 표시하면 재사용 Window가 이전 tracker/map 상태를 먼저 보일 수 있다.

## 3. MiniMap 수정 계약

- visible Main Map `CmbMapSelect`가 현재 제품 지도 선택의 권위다.
- canonical map key는 기존 `MapTrackerService`를 shared state로 계속 사용한다.
- MiniMap 생성 시 기존 SourceInitialized/Loaded 동기화는 유지한다.
- donor `OverlayVisibilityChanged(true)` 경계에서도 visible Main Map 선택을 **동기적으로** canonical tracker/active MiniMap에 반영한다.
- 이 이벤트는 donor `ShowOverlayCore()`의 `Show()` 직후, WPF가 다음 render turn으로 넘어가기 전에 동기적으로 발생하므로 hidden/reused Window 경로를 닫는다.
- 이미 열린 MiniMap의 이후 Main Map 변경 즉시 반영 계약도 유지한다.
- Factory 층, 자동/수동 floor selection, viewport, marker/filter 의미는 변경하지 않는다.

## 4. MiniMap 회귀 검증 계약

v1.9.1의 `current map key`/service-state-only smoke는 충분하지 않았으므로 v1.10.0부터 다음 published-EXE 경로를 직접 검증한다.

1. actual donor MiniMap Window를 표시해 맵 A의 actual `MapSvg.Source` 렌더를 확인한다.
2. `HideOverlay()`로 같은 loaded Window를 숨긴다.
3. visible Main Map selector를 B로 변경한다.
4. queued ContextIdle selection synchronization이 실행되기 전에 같은 MiniMap Window를 즉시 다시 `Show()`한다.
5. `Show()`가 반환되는 synchronous show boundary에서 active MiniMap key가 B인지 확인한다.
6. actual `MapSvg.Source`가 A의 source에서 B의 source로 바뀐 뒤에만 성공 처리한다.

required evidence:

```text
main-map-selection-boundary=ok
active-minimap-map-sync=ok
reused-minimap-show-boundary=ok
rendered-minimap-map-sync=ok
```

성공 marker는 위 actual reused-window render 검증만 기록할 수 있다. 단순 service/window-state 확인은 성공 marker를 생성하지 않는다.

## 5. Mini Scanner 플리 최저가 데이터 계약

새 필드의 authority는 Scanner full-item catalog의 `json.tarkov.dev ... /items` 응답 `lastLowPrice`다.

```text
Scanner confirmed Item ID
→ ScannerCatalogItem.FleaMinimumPrice
→ ScannerPresentationJoin
→ ScannerItemSnapshot
→ Mini Scanner presentation
```

- 값은 Scanner-confirmed Item ID 이후에만 presentation에 join한다.
- OCR/matcher/visual corroboration/acceptance에 가격을 사용하지 않는다.
- Scanner scan/search 중 새 네트워크 요청을 만들지 않는다.
- 잘못되거나 없는 가격은 해당 행만 숨기며 identity health를 실패시키지 않는다.
- `avg24hPrice` 기반 기존 플리 평균가는 그대로 유지한다.
- `lastLowPrice`를 별도의 플리 최저가로 표시한다.

## 6. Scanner cache / settings compatibility

Scanner catalog cache:

```text
v1~v4 readable
v4 written
```

- v1~v3 캐시는 오프라인 Item ID 인식을 위해 계속 읽는다.
- v1~v3은 새 시장 표시 필드가 없으므로 온라인 기회가 있으면 stale로 취급해 v4로 갱신한다.

Scanner display settings:

```text
v7 written
```

새 필드 key:

```text
flea_minimum_price
```

fresh default order:

```text
상인 판매가
플리마켓 평균가
플리마켓 최저가
상점가 / 칸
플리 평균가 / 칸
필요 개수
```

기존 v6 사용자 설정 migration:

- 기존 known 정보 행의 상대 순서를 그대로 보존한다.
- unknown/duplicate key를 제거한다.
- 새 플리 최저가 필드를 정확히 한 번만 뒤에 보완한다.
- 새 필드는 최초 migration 시 visible로 기본 활성화한다.
- 이후 사용자는 다른 정보 행과 동일하게 visibility/order를 직접 변경할 수 있다.

## 7. 비변경 영역

- Scanner OCR threshold
- Scanner matcher / candidate cap
- visual corroboration / recovery acceptance
- Scanner capture geometry / Ground Truth ownership
- canonical Item ID persistence
- Favorites / Recents semantics
- Game Content schema / LKG / relationship completeness / fail-closed
- Ammo behavior
- Map marker semantics
- Factory floor semantics
- donor pin `d933792b6042a51cea38dc44b686a096fe30de67`

## 8. 릴리즈 gate

v1.10.0 공개 전 최소 요구:

- Desktop Release build success
- full unit/regression tests success
- Scanner market parsing + Item-ID presentation join tests success
- Mini Scanner settings v6→v7 migration test success
- published EXE Mini Scanner rendered row smoke success
- published EXE exact MiniMap A→B reused-window `MapSvg` smoke success
- existing Product UI / Main Map / Factory / Scanner / graceful shutdown / clean portable root smoke success
- release package/checksum verification success
- exact-main CI success after merge
- public tag/release/assets exact source and checksum readback success
