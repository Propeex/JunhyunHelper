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

Cross-page dependency wiring은 **product `MainWindow` lifetime**이 소유한다.

`MainWindow.OnInitialized`에서 다음을 한 번 연결한다.

```text
Quest / Hideout / Items / Ammo image cache
Ammo favorites store
cross-page content navigation event wiring
```

Page 내부 presentation 준비는 해당 Page가 직접 소유한다. 특히 Ammo의 runtime search/detail presentation은 `AmmoPage.OnInitialized`가 `DispatcherPriority.Loaded` 작업으로 명시적으로 예약하며, 부모 XAML의 incidental `Loaded` subscription이나 class-level `Loaded` handler에 의존하지 않는다.

사용자-visible 동작, Core/Application/Infrastructure data ownership, Scanner recognition, Map/MiniMap donor behavior는 변경하지 않는다.

## 3. Dead-code audit classification

이번 audit에서 분류한 대표 항목:

| 후보 | 분류 | 처리 |
|---|---|---|
| `ItemsPage_Loaded` / `HideoutPage_Loaded` / `AmmoPage_Loaded` | ownership 이동과 Ammo self-initialization 명시화 후 실제 dead event handlers | XAML 연결과 함께 제거 |
| Ammo class-level `Loaded` presentation hook | parent Loaded subscription 존재 여부에 간접 의존하던 hidden lifecycle coupling | `OnInitialized` + dispatcher-owned explicit initialization으로 대체 |
| `Legacy` Map host/adapter/runtime | active donor compatibility/integration | 유지 |
| Factory/Map/MiniMap smoke 코드 | historical name을 가진 active regression evidence | 유지 |
| Scanner diagnostic OCR reflection adapter | 의도적으로 유지하는 technical debt | 유지 |
| original full-refresh mutation handlers + fast rebinding | duplicate/superseded-looking path지만 lifecycle rebinding에 아직 관여 | 이번 audit에서 삭제하지 않음 |

### Smoke가 증명한 WPF hidden dependency

첫 정리안은 세 parent page `Loaded` handler의 body가 중복이라고 판단해 제거했다. 자동 테스트와 Desktop build/publish는 통과했지만 실제 published EXE Product UI smoke에서 Ammo detail toggle이 `▼ → ▲` 상태 전환을 수행하지 못했다.

원인은 `AmmoPage.ProductSearchAndDetails.cs`가 `EventManager.RegisterClassHandler(... LoadedEvent ...)`로 presentation 준비를 시작하고 있었고, 부모 XAML의 instance `Loaded` subscription 제거가 WPF의 Loaded delivery 최적화와 결합해 해당 class handler 실행을 더 이상 신뢰할 수 없게 만든 것이었다.

따라서 이 사건은 다음 dead-code 규칙의 실제 회귀 증거다.

```text
handler body가 중복처럼 보임
≠ lifecycle에서 죽은 코드임
```

수정 후 Ammo presentation은 Page 자신의 `OnInitialized`에서 Loaded-priority dispatcher work로 예약된다. parent handler는 더 이상 다른 initialization을 우연히 깨우는 역할을 하지 않는다.

마지막 mutation-handler 항목은 `OnContentRendered`에서 `EnableFastMutationHandlers()`가 다시 실행되어 최종 handler state를 정리한다. 따라서 이름/중복만 보고 dead라고 판정하지 않는다. 별도 변경은 lifecycle 증거와 regression이 충분할 때만 한다.

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
- XAML이 제거된 parent page `Loaded` handlers를 다시 참조하지 않음
- 제거된 handlers가 `MainWindow.Images.cs`에 되살아나지 않음
- Ammo presentation initialization이 `AmmoPage.OnInitialized`에서 명시적으로 예약됨
- Ammo가 다시 class-level `Loaded` handler를 hidden initializer로 사용하지 않음

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
