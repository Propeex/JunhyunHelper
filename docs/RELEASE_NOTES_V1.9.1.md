# 준현 헬퍼 v1.9.1

## 목적

v1.9.1은 v1.9.0 공개 후 실사용에서 확인된 세 가지 소규모 UI/동기화 회귀만 수정하는 PATCH 릴리즈다. 기능 확장이나 Scanner 인식 알고리즘 변경은 포함하지 않는다.

## Scanner 즐겨찾기 버튼

- Scanner 아이템 상세 상단의 즐겨찾기 별 버튼을 인접한 Wiki 버튼과 동일한 34px 높이로 맞춘다.
- 별 글리프는 `Segoe UI Symbol`, 18px, zero padding, 수평/수직 중앙 정렬을 사용해 잘림을 제거한다.
- 즐겨찾기 등록/해제, canonical Item ID 저장, persistence 의미는 변경하지 않는다.
- published runtime smoke는 아이템 상세가 실제로 표시된 Render 시점에 두 버튼의 실제 높이와 정렬을 검증한다. Collapsed 상태의 `ActualHeight=0`을 정상 UI 실패로 오판하지 않는다.

## 지도 마커 탈출구 목록

최종 사용자 표시 계약은 다음과 같다.

```text
퀘스트 마커 표시

전투 / 스폰
  PMC 스폰
  스나이퍼 스캐브
  로그
  컬티스트
  보스
  레이더

지도 요소
  레버

탈출구
  PMC 탈출구
  Scav 탈출구
  트랜짓 탈출구
```

- 화면에는 `탈출구` 그룹과 위 세 체크박스만 표시한다.
- v1.9.0에서 중복 노출되던 `탈출 / 이동`, 빈 회색 wrapper 행 세 개, 일반/master `탈출구` 체크박스는 사용자 UI에 표시하지 않는다.
- 기존 `LegacyMapMarkerSettingsV2Bridge`를 다른 marker group과 동일한 카드/행 presentation의 단일 권위로 유지한다.
- PMC/Scav/Transit은 donor의 실제 `ChkShowPmcExtracts`, `ChkShowScavExtracts`, `ChkShowTransitExtracts` 인스턴스를 그대로 사용한다.
- donor master `ChkShowExtractMarkers`는 사용자에게 보이지 않는 내부 render gate로만 유지하고 활성 상태를 보장한다.
- 기존 donor Checked/Unchecked handler, marker rendering, 설정 persistence와 MiniMap refresh 의미를 유지한다.

## Main Map / MiniMap 현재 지도 동기화

재현 시나리오:

1. 프로그램이 이전에 저장된 맵 A로 시작한다.
2. 사용자가 지도 탭의 실제 맵 selector에서 맵 B를 선택한다.
3. MiniMap을 연다.
4. MiniMap은 저장된 A가 아니라 현재 화면의 B를 첫 표시부터 사용해야 한다.

구현 계약:

- visible Main Map `CmbMapSelect`가 현재 지도 선택의 제품 경계다.
- 선택값은 canonical map key로 정규화해 `MapTrackerService`에 즉시 반영한다.
- MiniMap `SourceInitialized` 등록 경계에서 visible Main Map 선택을 tracker에 동기화한 뒤 MiniMap을 active product window로 등록한다.
- donor Loaded 뒤에도 한 번 더 동일 경계를 통과해 첫 프레임/초기화 순서 차이로 stale map이 남지 않게 한다.
- 이미 열려 있는 MiniMap도 이후 Main Map 변경을 즉시 반영한다.
- 기존 Factory 층 처리, floor selection, marker filters와 Map/MiniMap 기능 의미는 변경하지 않는다.

## 비대상

- Scanner OCR threshold
- OCR matcher / candidate cap
- visual corroboration / recovery acceptance
- Scanner capture geometry / Ground Truth
- Game Content schema / LKG / fail-closed 정책
- 다른 탭 UI 또는 신규 기능

## Release candidate 검증

버전 승격 전 기능 후보 `9d64bd8059f59e00a7b879e3b1a8dd3313b34e56` 기준 CI run `33175230665`가 성공했다.

```text
Release build: SUCCESS
435 passed / 0 failed / 0 skipped
win-x64 self-contained publish: SUCCESS
published EXE Product UI / Map / Factory / MiniMap / Scanner smoke: SUCCESS
release package/checksum verification: SUCCESS
graceful shutdown + clean portable root: SUCCESS
```

해당 published EXE의 지도 탈출구 runtime evidence에는 다음이 포함된다.

```text
real-donor-checkboxes=ok
marker-panel-visible=ok
master-filter-render-state=ok
hidden-master-render-gate=ok
approved-three-filter-layout=ok
minimap-refresh-handler-preserved=ok
pmc-filter-render-state=ok
scav-filter-render-state=ok
transit-filter-render-state=ok
```

최종 공개 증거는 v1.9.1 version-bump branch gate, squash-merge 후 exact-main gate, Release workflow와 public asset readback이 모두 끝난 뒤 이 문서와 공식 상태 문서에 추가한다.
