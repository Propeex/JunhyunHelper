# 준현 헬퍼 v1.10.0

## 목적

v1.10.0은 실사용에서 다시 확인된 MiniMap 첫 표시 동기화 회귀를 실제 Window 재사용 경로에서 수정하고, Mini Scanner에 플리마켓 최저가 표시를 추가하는 MINOR 릴리즈다.

## MiniMap A → B 후 첫 표시 동기화

실제 문제:

1. 프로그램을 실행하고 지도 탭에 들어간다.
2. Main Map이 맵 A인 상태에서 맵 B로 변경한다.
3. MiniMap을 표시한다.
4. v1.9.1에서는 특정 실제 사용 경로에서 MiniMap이 여전히 A를 표시할 수 있었다.

원인은 donor MiniMap 서비스가 창을 닫지 않고 `Hide()`한 뒤 같은 loaded Window를 다시 `Show()`할 수 있다는 점이다. 이 재사용 경로에서는 `SourceInitialized`와 `Loaded`가 다시 발생하지 않아 v1.9.1 초기화 동기화가 실행되지 않았다.

v1.10.0은 MiniMap이 사용자에게 다시 표시되는 synchronous show boundary에서도 현재 visible Main Map 선택을 즉시 반영한다.

공개 후보 검증은 단순 current-map 변수만 확인하지 않는다. 실제 donor MiniMap Window를 A로 렌더한 뒤 숨기고, Main Map을 B로 변경한 직후 같은 Window를 다시 표시해 actual `MapSvg.Source`가 B로 바뀐 경우에만 성공한다.

예정 required runtime evidence:

```text
main-map-selection-boundary=ok
active-minimap-map-sync=ok
reused-minimap-show-boundary=ok
rendered-minimap-map-sync=ok
```

## Mini Scanner 플리마켓 최저가

Mini Scanner에 기존 정보 행과 동일한 형태의 **플리마켓 최저가**를 추가한다.

- 데이터는 Scanner full-item catalog의 `lastLowPrice`를 사용한다.
- 기존 `avg24hPrice` 기반 플리마켓 평균가는 그대로 유지한다.
- 플리 최저가는 Item ID가 확정된 뒤 presentation 단계에서만 연결한다.
- 가격은 OCR, matcher, 후보 순위, visual corroboration, acceptance에 사용하지 않는다.
- 스캔/검색 순간 새 네트워크 요청을 만들지 않는다.
- 가격이 없으면 해당 행만 표시하지 않는다.

기본 Mini Scanner 정보 순서:

```text
상인 판매가
플리마켓 평균가
플리마켓 최저가
상점가 / 칸
플리 평균가 / 칸
필요 개수
```

기존 사용자 설정은 기존 행의 상대 순서를 보존하고 새 플리 최저가 행을 정확히 한 번 보완한다. 설정 화면에서 다른 행과 동일하게 표시 여부와 순서를 변경할 수 있다.

## Schema / compatibility

```text
Scanner display settings: v7 written
Scanner catalog cache: v1~v4 readable, v4 written
```

기존 v1~v3 Scanner catalog cache는 오프라인 Item ID 인식을 위해 계속 읽을 수 있다. 다만 새 시장 표시 필드가 없으므로 온라인 기회가 있으면 stale로 간주해 v4로 갱신한다.

## 변경하지 않은 것

- Scanner OCR threshold
- matcher / candidate cap
- visual corroboration / recovery acceptance
- Scanner capture geometry / Ground Truth
- Scanner canonical Item ID authority
- Favorites / Recents semantics
- Game Content schema / LKG / relationship completeness / fail-closed
- Ammo 기능
- Map marker 의미
- Factory floor 의미
- pinned donor commit

## 릴리즈 검증

최종 exact source, PR/main CI run, 테스트 수, published EXE runtime evidence, package byte size/SHA-256, GitHub Release/tag/asset readback은 v1.10.0이 `main`에서 검증·공개된 뒤 이 문서와 `docs/RELEASE_1.10.0.md`에 기록한다.
