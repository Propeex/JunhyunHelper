# DECISION — Long-term maintenance audit baseline

기준일: 2026-08-27  
상태: **CONFIRMED MAINTENANCE DIRECTION / IMPLEMENTATION PR #197**

## 1. 목적

제품 기능을 추가하지 않고 장기 유지보수 품질을 높이기 위한 첫 성능·dead code·architecture audit 결과를 기록한다.

이번 결정은 다음 원칙을 고정한다.

```text
측정/증거
→ owner와 영향 범위 확인
→ 필요한 구간만 변경
→ 결정론적 regression
→ Windows product smoke/package 검증
```

코드가 낡아 보이거나 `Legacy`라는 이름이 있다는 이유만으로 삭제하거나, 느릴 것 같다는 추측만으로 cache/병렬화/재계산 구조를 바꾸지 않는다.

## 2. Architecture audit — Desktop page infrastructure ownership

### 발견

`MainWindow.Images.cs`의 page infrastructure wiring 일부가 `ItemsPage`, `HideoutPage`, `AmmoPage`의 WPF `Loaded` event에 분산되어 있었다.

특히 Quest image cache가 `ItemsPage_Loaded`의 부수효과로 연결되고 cross-page navigation event wiring도 어떤 page가 먼저 Loaded 되는지에 의해 시작되었다.

이 구조는 domain truth 오류는 아니지만 다음 유지보수 위험을 만든다.

- infrastructure owner가 실제 기능 owner가 아니라 page lifecycle event로 보임
- 특정 tab의 Loaded 순서가 다른 tab의 준비 상태에 영향을 줄 수 있음
- 초기화 경로를 추적할 때 XAML event와 code-behind를 함께 추론해야 함
- 이후 UI composition 변경이 unrelated navigation/image behavior에 회귀를 만들 수 있음

### 결정

Page-level infrastructure는 **product `MainWindow` lifetime**이 소유한다.

`MainWindow.OnInitialized`에서 다음을 한 번 연결한다.

```text
Quest / Hideout / Items / Ammo image cache
Ammo favorites store
cross-page content navigation event wiring
```

개별 page `Loaded` event는 이 infrastructure를 소유하지 않는다.

사용자-visible 동작, Core/Application/Infrastructure data ownership, Scanner recognition, Map/MiniMap donor behavior는 변경하지 않는다.

## 3. Dead-code audit classification

이번 audit에서 분류한 대표 항목:

| 후보 | 분류 | 처리 |
|---|---|---|
| `ItemsPage_Loaded` / `HideoutPage_Loaded` / `AmmoPage_Loaded` | startup ownership 이동 후 실제 dead event handlers | XAML 연결과 함께 제거 |
| `Legacy` Map host/adapter/runtime | active donor compatibility/integration | 유지 |
| Factory/Map/MiniMap smoke 코드 | historical name을 가진 active regression evidence | 유지 |
| Scanner diagnostic OCR reflection adapter | 의도적으로 유지하는 technical debt | 유지 |
| original full-refresh mutation handlers + fast rebinding | duplicate/superseded-looking path지만 lifecycle rebinding에 아직 관여 | 이번 audit에서 삭제하지 않음 |

마지막 항목은 `OnContentRendered`에서 `EnableFastMutationHandlers()`가 다시 실행되어 최종 handler state를 정리한다. 따라서 이름/중복만 보고 dead라고 판정하지 않는다. 별도 변경은 lifecycle 증거와 regression이 충분할 때만 한다.

## 4. Performance audit classification

### Workspace profile reads

`RefreshActiveWorkspacesAsync`는 Quest/Hideout/Items `LoadAsync`를 각각 호출한다. 표면적으로는 동일 profile을 반복 읽는 것처럼 보이지만 `UserProfileStore.LoadAsync`는 첫 authoritative read/save 뒤 `ConcurrentDictionary` memory cache의 동일 immutable snapshot을 반환한다.

따라서 현재 증거만으로 이를 SQLite I/O 병목이라고 간주하지 않는다.

결정:

- 이번 runtime 변경에서 workspace read 구조를 바꾸지 않음
- 실제 startup/profile/workspace trace에서 병목으로 확인될 때만 one-read/multi-build 구조를 검토
- 단순 호출 수만 보고 global cache나 추가 mutable cache를 만들지 않음

### Scanner

Scanner는 v1.7.6에서 실제 latency telemetry로 병목을 증명하고 same-cycle exact-pixel 계산만 재사용한 선례를 유지한다.

이번 audit에서는 recognition threshold, candidate cap, OCR/matcher/visual acceptance, cross-frame caching을 변경하지 않는다.

## 5. Regression protection

`DesktopStartupWiringContractTests`는 다음 구조 계약을 고정한다.

- 네 page의 image-cache binding과 Ammo favorites store가 product initialization에 존재
- cross-page navigation wiring이 product initialization에서 연결
- XAML이 제거된 page `Loaded` handlers를 다시 참조하지 않음
- 제거된 handlers가 `MainWindow.Images.cs`에 되살아나지 않음

이 source-level contract는 WPF Desktop을 일반 Core test assembly에 새로 참조시키지 않으면서 composition ownership 회귀를 탐지하기 위한 제한된 architecture regression이다.

실제 runtime 동작은 기존 Windows published EXE Product UI/Scanner/Map/Factory/MiniMap smoke와 graceful shutdown/package verification으로 함께 검증한다.

## 6. 다음 성능 측정 우선순위

추후 실제 evidence를 수집할 때 우선순위는 다음과 같다.

1. process start → MainWindow usable
2. profile/content initial load
3. Quest/Hideout/Items workspace refresh
4. data update 후 workspace refresh
5. Map/MiniMap first materialization
6. image-cache cold/warm behavior
7. Scanner는 기존 latency telemetry 유지
8. idle CPU/memory는 장시간 실사용 evidence가 있을 때 조사

CI host wall-clock을 곧바로 제품 성능 합격선으로 사용하지 않는다. 재현 가능한 operation count/policy regression과 실제 runtime trace를 우선한다.

## 7. 릴리즈 의미

PR #197은 runtime composition code를 변경하므로 merge 후에도 기존 `v1.7.11` tag/source/assets를 수정하지 않는다.

이 변경은 다음 PATCH release 후보에 포함될 수 있지만, 새 릴리즈 필요성은 full Windows CI/product smoke/package 검증과 이후 실제 변경 묶음을 기준으로 판단한다.
