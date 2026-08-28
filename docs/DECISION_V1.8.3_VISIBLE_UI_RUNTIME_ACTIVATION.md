# Decision — v1.8.3 Visible UI Runtime Activation Repair

기준일: 2026-08-28 KST

## 배경

v1.8.2 공개 후 v1.7.15에서 확정한 두 UI 요구사항이 소스 코드에는 존재하지만 실제 published executable에서 안정적으로 적용되지 않는 회귀가 다시 확인되었다.

1. Ammo의 구경/즐겨찾기 selector가 실제 제품 런타임에서 새 ComboBox/아이콘 UI로 초기화되지 않을 수 있었다.
2. 지도 마커 패널의 외곽 높이는 늘어났지만 체크박스 목록 viewport가 실제 남은 본문 높이를 소유하지 못해 불필요한 내부 스크롤이 남을 수 있었다.

이 PATCH의 목표는 기능 의미를 바꾸는 것이 아니라, 이미 확정된 UI가 실제 공개 실행 파일에서도 결정적으로 생성되고 검증되도록 runtime lifecycle ownership을 바로잡는 것이다.

## 1. Ammo visible dropdown activation

### 문제

기존 보정은 WPF routed `Loaded`/class-handler와 실행 순서에 의존했다. 소스 계약이나 별도의 smoke가 존재해도, 실제 `AmmoPage` 인스턴스가 만들어질 때 제품 UI 초기화가 완료되었다는 보장이 부족했다.

그 결과 다음 상태가 가능했다.

- 구경 selector가 fallback UI로 남음
- 즐겨찾기 selector가 새 ComboBox 대신 legacy surface에 머묾
- 두 selector의 구경별 탄약 아이콘 상태/애니메이션이 실제 제품 화면에 설치되지 않음

### 결정

Ammo visible dropdown은 `AmmoPage`의 실제 페이지 초기화 경계가 소유한다.

- 기존 `OnInitialized` 경로에서 `EnsureProductVisibleDropdownInitialization()`을 직접 실행한다.
- 구경 ComboBox에 제품 icon template을 적용한다.
- 즐겨찾기는 일반 ComboBox로 생성한다.
- 두 selector는 동일한 구경별 icon template/state와 순환 타이밍을 공유한다.
- legacy favorites menu는 숨김/비활성 상태를 유지한다.
- published executable smoke보다 먼저 별도 activation gate가 실제 초기화 상태를 확인한다. 따라서 smoke 자체가 누락된 초기화를 뒤늦게 보정해 결함을 숨길 수 없다.

구경 필터링과 즐겨찾기 저장 의미는 변경하지 않는다.

## 2. Map marker panel lifecycle ownership

### 문제

지도는 pinned donor `MapPage`를 제품 내부에 이식한 subsystem이다. 제품 보정은 donor constructor가 끝나기 전에 초기화를 앞당기면 안 된다.

초기 수정에서 다음 두 WPF lifecycle 가정이 실제 실행 파일에서 안전하지 않다는 것이 확인되었다.

1. routed `Loaded` class handler에서 `OriginalSource == MapPage`일 것이라는 가정
2. marker checkbox stack의 `FrameworkElement.Parent`가 제품 보정 시점에 반드시 원래 `StackPanel`일 것이라는 가정

첫 번째 가정은 실제 보정 자체를 건너뛸 수 있었고, 두 번째 가정은 logical-parent 상태에 따라 viewport 생성이 누락될 수 있었다.

### 결정

- donor constructor/초기 map load 순서는 그대로 유지한다.
- `Loaded` class handler는 `OriginalSource == page`를 요구하지 않는다.
- 실제 변경은 dispatcher의 Loaded/ContextIdle 경계에서 수행한다.
- marker viewport 소유 구조는 transient `MapMarkersContent.Parent`가 아니라 제품이 알고 있는 고정 앵커 `MapMarkersOverlay.Child`에서 해결한다.
- overlay의 child collection에 이미 `MapMarkersContent`를 담는 `ScrollViewer`가 있으면 해당 viewport를 재사용한다.
- 그렇지 않으면 overlay child collection에서 `MapMarkersContent`의 실제 index를 찾아 같은 위치에 제품 `ScrollViewer`를 삽입한다.
- viewport 해결에 성공하기 전에는 `_junhyunMarkerPanelPolishApplied`를 true로 만들거나 본문 layout activation을 실행하지 않는다.
- smoke 환경에서는 구조를 해결하지 못한 상태를 조용히 허용하지 않고 diagnostic을 남긴 뒤 실패한다.

이 방식은 donor 전체 구조를 변경하지 않고 제품이 소유하는 marker overlay의 국소 UI만 보정한다.

## 3. Marker checkbox body layout

확장된 지도 마커 패널에서는 체크박스 viewport가 헤더와 chrome을 제외한 **남은 본문 전체 높이**를 소유한다.

- 기존 content-sized Height 계산을 제품 full-body 계산으로 교체한다.
- panel이 제공하는 실제 높이에서 header/chrome을 제외한 값을 viewport Height/MaxHeight로 사용한다.
- `VerticalScrollBarVisibility = Auto`를 사용한다.
- 사전 `DesiredSize` 추정으로 스크롤바를 강제로 표시/숨김하지 않는다.
- 실제 WPF layout 이후 `ScrollableHeight`와 `ComputedVerticalScrollBarVisibility`가 일치하는지 smoke에서 확인한다.

따라서 현재 체크박스가 모두 본문 안에 들어가면 스크롤바가 없어야 하며, 향후 실제 콘텐츠가 가용 높이를 넘을 때만 스크롤바가 나타난다.

## 4. Published executable release gate

v1.8.3의 수정 완료 여부는 source inspection이나 unit test만으로 판단하지 않는다.

릴리즈 후보는 다음을 모두 통과해야 한다.

1. Release desktop build
2. full test suite
3. Windows x64 self-contained single-file publish
4. Ammo real initialization gate
5. rendered Ammo caliber/favorites ComboBox + shared icon/timer smoke
6. rendered Map marker panel body/overflow smoke
7. 기존 Main Map / Factory / MiniMap regression smoke
8. graceful MainWindow shutdown
9. clean portable root 및 release package/checksum validation
10. exact-main CI success
11. stable GitHub release/tag/assets readback

## 5. 변경하지 않는 계약

- Ammo 구경 필터/즐겨찾기 저장 의미
- 지도 marker 종류와 on/off 의미
- Map/MiniMap pinned donor revision
- Main Map/Factory 층 표시 및 marker presentation
- MiniMap 동작
- Quest/Hideout/Items/Profile 의미
- Game Content update candidate/LKG/fail-closed 계약
- Scanner OCR threshold, matcher, visual recovery, item database 의미

v1.8.3은 visible UI runtime activation과 marker panel layout을 교정하는 유지보수 PATCH다.
