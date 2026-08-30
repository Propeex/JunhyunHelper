# 준현 헬퍼 v1.11.3

## 목적

v1.11.3은 v1.11.2 실사용에서 확인된 UI 회귀와 Scanner 교정 workflow의 진단 데이터 유실을 수정하는 PATCH 유지보수 릴리즈다.

- Items / Hideout 검색창에서 입력 후에도 inline `×`가 나타나지 않던 문제
- Map 지도 마커 패널의 하단 탈출구 체크박스가 큰 창에서도 잘리던 문제
- Scanner 교정 스크린샷을 더 자세히 확인하기 위한 mouse-wheel zoom
- 교정 저장 직전에 완료된 OCR/matcher 분석이 다음 geometry capture에 덮여 Saved Case가 `NOT_RUN`으로 남던 진단 timing 문제

새로운 Scanner 인식 threshold를 완화하거나 game-content schema 의미를 변경하지 않는다.

## Items / Hideout 검색 clear

### 원인

제품의 공유 구현 `ProductSearchClearButtonBehavior` 자체는 Quest와 동일한 conditional inline clear 동작을 제공하고 있었다. 그러나 Items/Hideout는 실제 visible page lifecycle에서 이 behavior를 안정적으로 attach하지 못했다.

v1.11.2 published smoke에도 검증 결함이 있었다. smoke가 실제 페이지에서 생성된 `×`를 확인하는 대신 `ProductSearchClearButtonBehavior.Attach(searchBox)`를 직접 호출해 테스트가 검증 대상 UI를 스스로 만들어낼 수 있었다. 따라서 사용자 PC 회귀를 놓쳤다.

### 수정

- Items/Hideout의 real `Loaded` lifecycle과 `OnApplyTemplate` boundary에서 canonical behavior를 연결
- empty query → `×` hidden
- typed query → inline `×` visible
- click → query clear + TextBox focus 유지
- smoke에서 직접 `Attach`하던 repair 경로 제거
- 실제 page lifecycle이 생성한 clear glyph만 검사

## Map 지도 마커 패널

### 원인

기존 expanded marker panel은 `MapMarkersContent.DesiredSize`에 맞춰 높이를 정하는 content-sized popup 방식이었다. 탈출구 row 등 하단 content가 아직 완전히 layout/reparent되지 않은 시점의 작은 DesiredSize가 사용되면, 창 자체가 충분히 커도 패널 높이가 짧게 고정되어 이후 추가된 하단 항목이 잘릴 수 있었다.

창 높이를 줄였을 때만 ScrollViewer overflow가 활성화되어 잘린 탈출구 일부가 보이는 실사용 증상과 일치했다.

### 수정

- expanded marker panel을 available-height viewport로 변경
- 큰 창에서는 `MapViewerGrid`의 사용 가능한 세로 공간을 패널이 사용
- 내부 checkbox viewport가 panel body를 채움
- `VerticalScrollBarVisibility=Auto` 유지
- 실제 rendered overflow가 있을 때만 scrollbar 표시
- published smoke가 panel full-height와 rendered scrollbar 상태를 직접 검사

## Scanner 교정 이미지 확대/축소

교정 창의 screenshot 영역을 source-pixel canvas + ScrollViewer + display-only `LayoutTransform` 구조로 변경했다.

- 이미지 위 mouse wheel로 확대/축소
- 1.15배 step
- fit 상태부터 최대 8배까지 확대
- 확대 후 horizontal/vertical scroll 가능
- pointer 위치를 기준으로 viewport anchor를 최대한 보존
- `ImageCanvasHost`와 overlay는 원본 pixel width/height를 계속 사용
- Ground Truth rectangle과 직접 지정 좌표의 의미는 zoom에 따라 변하지 않음

초기 runtime smoke에서 Auto scrollbar가 생겼다가 사라지는 동안 `ViewportWidth/ViewportHeight`가 변해 fit scale이 0.573 → 0.596으로 달라지는 상태 의존 문제가 발견됐다. 최종 구현은 ScrollViewer의 안정된 arranged control bounds를 기준으로 fit scale을 계산하여 확대 후 다시 축소하면 동일 fit scale로 복귀한다.

## Scanner 교정 evidence 보존

사용자가 전달한 최신 diagnostics/calibration bundle에는 reviewed case 5건이 포함되어 있었다.

Ground Truth:

- `Wrench 렌치`
- `Nails 못 상자`
- `ELCAN Specter HCO holographic sight`
- `Corrugated hose 주름진 호스`
- `7.62x25mm TT P gl ammo pack (25 pcs)`

저장 JSON은 5건 모두 `RecognitionReason=NOT_RUN`, 빈 OCR text를 기록했지만 bundled runtime log에서는 적어도 마지막 두 case에 실제 OCR/matcher가 저장 직전에 실행됐다.

- `Corrugated hose 주름진 호스`: WinRT OCR이 선두 Latin glyph 일부를 Han/CJK glyph로 오인해 `OCR_INVALID_CHARACTERS`로 fail-closed
- `7.62x25mm TT P gl ammo pack (25 pcs)`: Ground Truth와 동일한 catalog item이 nearest candidate였지만 약 0.846 confidence / 약 0.038 margin으로 `LOW_CONFIDENCE` fail-closed

원인은 `ScannerRecognitionDebugStore`가 단일 latest frame만 유지하면서 분석 완료 frame 직후 새 geometry capture가 들어오면 의미 정보가 없는 `NOT_RUN` frame이 최신값을 덮어쓰는 timing defect였다.

v1.11.3은 correction snapshot에 한해서 다음 조건을 모두 만족할 때 직전 analyzed semantics를 보존한다.

- 현재 frame과 analyzed frame의 non-empty `TitleSignature`가 정확히 동일
- capture mode 동일
- analyzed frame이 현재 frame보다 미래가 아님
- 두 frame 간격 3초 이내

current screenshot/geometry는 항상 최신 frame을 사용한다. 보존된 OCR/matcher evidence는 교정/진단 저장 품질에만 사용하며 live Scanner 판정에는 재사용하지 않는다.

이 5건을 근거로 OCR 허용 문자나 confidence/margin threshold를 임의 완화하지 않는다. false-positive 우선 안전 계약을 유지한다.

## 회귀 검증

v1.11.3에는 다음 deterministic/runtime 계약을 추가·강화했다.

- Items/Hideout가 real page lifecycle에서 shared search clear behavior를 attach
- published smoke가 clear UI를 스스로 생성하지 못함
- expanded Map marker panel이 available height를 사용
- scrollbar가 actual rendered overflow와 일치
- correction mouse-wheel zoom 및 source-pixel coordinate 보존
- zoom-in → zoom-out 시 stable fit scale 복귀
- recent analyzed semantics가 동일 title signature/capture mode/3초 조건에서만 correction snapshot에 carry
- correction hotkey와 수동 latest correction 모두 safe correction snapshot 사용

최종 release candidate는 다음 게이트를 통과해야 한다.

- deterministic automated tests
- Windows Release desktop build
- Windows x64 self-contained publish
- actual published EXE startup
- Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- Items/Hideout search clear runtime behavior
- Map marker full-height/overflow behavior
- Scanner correction zoom runtime behavior
- graceful shutdown
- active-async Shutdown Race
- release package root/dependency/checksum audit
- exact-main validation
- public tag/release/assets readback

## Schema / compatibility

```text
Desktop target version: 1.11.3
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog cache: v1~v4 readable, v4 written
```

v1.11.2 → v1.11.3에서 mandatory Game Content migration, user.db migration, Scanner display settings migration은 없다.

사용자가 제공한 raw diagnostics ZIP 및 screenshots는 private evidence로 유지하며 public repository에 포함하지 않는다.
