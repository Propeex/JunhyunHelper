# DECISIONS — 현재 유효한 장기 결정

이 문서는 준현 헬퍼의 **현재 유효한 장기 결정과 supersession 관계**를 빠르게 복구하기 위한 기준입니다.

2026-08-09까지의 DEC-001~DEC-029 원문은 역사 보존을 위해 다음 파일에 **그대로** 보관합니다.

- [`DECISIONS_HISTORY_THROUGH_2026-08-09.md`](DECISIONS_HISTORY_THROUGH_2026-08-09.md)

과거 결정을 삭제한 것이 아닙니다. 현재 단계에서 오해하기 쉬운 phase-specific 문장과 이후 변경된 결정을 명시적으로 supersede하기 위해 active index와 최신 결정을 분리했습니다.

우선순위는 `AGENTS.md`를 따릅니다. 현재 사용자 요구와 아래의 더 새로운 결정이 과거 충돌 결정보다 우선합니다.

---

# 1. DEC-001~029 상태 인덱스

아래 결정의 상세 이유/대안/영향은 역사 파일의 동일 ID를 읽습니다.

- `DEC-001` — 새 제품은 처음부터 설계한다 — **CONFIRMED**
- `DEC-002` — 기존 Tarkov-Helper는 자동으로 제품 사양이 아니다 — **CONFIRMED**, 단 Map/MiniMap의 명시적 기준선 예외는 `DEC-031`
- `DEC-003` — GitHub 저장소를 프로젝트 기억의 공식 기반으로 사용 — **CONFIRMED**
- `DEC-004` — 사용자는 제품 판단에 집중하고 개발 절차는 개발자가 책임 — **CONFIRMED**
- `DEC-005` — 당시 Phase 1에서는 제품 코드를 작성하지 않고 설계를 선행 — **SUPERSEDED by DEC-030**
- `DEC-006` — 공식 제품명은 준현 헬퍼 — **CONFIRMED**
- `DEC-007` — 초기 상위 기능 영역 정의 — **CONFIRMED**, Scanner는 아직 PRODUCT OPEN
- `DEC-008` — 구두 의도는 해석을 맞춘 뒤 요구사항으로 확정 — **CONFIRMED**
- `DEC-009` — Quest 원천은 json.tarkov.dev → 내부 canonical model — **CONFIRMED**
- `DEC-010` — 수주 가능 Quest는 Helper에서 이미 수락한 것으로 간주 — **CONFIRMED**
- `DEC-011` — Quest 해금에 필요한 사용자 상태는 진행 profile에서 관리 — **CONFIRMED**
- `DEC-012` — GameMode별 캐릭터 진행은 독립 profile — **CONFIRMED**
- `DEC-013`~`DEC-019` — Quest/Hideout/Needed Items의 판정·미래 필요·보수적 cleanup 관련 결정 — **CONFIRMED**, 상세는 역사 파일 참조
- `DEC-020` — Inventory를 자동 추정하지 않는 초기 원칙 — **PARTIALLY SUPERSEDED by DEC-025/026**
- `DEC-021`~`DEC-024` — UI/navigation/Ammo source 및 표시 경계 관련 결정 — **CONFIRMED**, 상세는 역사 파일 참조
- `DEC-025` — Quest/Hideout 고정 소모 Item은 명시적 진행 조작과 함께 자동 차감 — **CONFIRMED**
- `DEC-026` — flexible hand-in의 실제 소비 Item은 자동 추정하지 않음 — **CONFIRMED**
- `DEC-027` — Wiki Ballistics membership과 effectiveness는 별도 canonical fact — **CONFIRMED**
- `DEC-028` — Prestige는 미입력 없이 0을 기본 사실로 취급 — **CONFIRMED**
- `DEC-029` — 제품 아이콘은 Game Content update 후 선다운로드 — **CONFIRMED**

---

## DEC-030 — 설계 우선 원칙은 새 기능에 적용하고, 확정 기능의 수정은 직접 진행한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10
- 결정: 준현 헬퍼는 더 이상 Phase 1의 "제품 코드를 작성하지 않는다" 상태가 아니다. 이미 확정·구현된 기능의 버그 수정, 회귀 수정, 성능 개선, 릴리즈 하드닝은 기존 요구사항/테스트/코드를 조사해 개발자가 직접 진행한다. 새 기능이나 제품 의미를 바꾸는 변경은 계속 설계와 사용자 의도 정렬을 먼저 한다.
- 이유: v0.1 핵심 기능이 구현된 뒤에도 DEC-005의 phase-specific 금지 문장이 현재 규칙처럼 남으면 유지보수와 릴리즈를 잘못 중단시킨다. 반대로 설계 우선이라는 원칙 자체는 새 기능에서 여전히 중요하다.
- 영향: `AGENTS.md`, `DEVELOPMENT.md`, `STATE.md`의 현재 단계 규칙을 따른다.
- 대체한 결정: `DEC-005`의 "현재 단계에서는 제품 코드를 작성하지 않는다" 부분 전체

## DEC-031 — Map/MiniMap은 사용자가 검증한 특정 Tarkov-Helper 기준선을 명시적으로 채택한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10
- 결정: Map/MiniMap에 한해 사용자가 Windows에서 실제 artwork/구조를 확인한 `Propeex/Tarkov-Helper`의 특정 source baseline을 JunhyunHelper 제품 기준선으로 채택한다. exact baseline은 `9371c4769d8da8acb9df864a2c88f83ecdd42818`, JunhyunHelper가 현재 pin한 product revision은 `d933792b6042a51cea38dc44b686a096fe30de67`이다.
- 이유: 사용자가 기존 Map의 시각 자산과 실제 사용 구조를 새 Map의 기준으로 명시적으로 선택했고, 처음부터 다시 만든 Map보다 검증된 기반을 제품화하는 방향을 확정했다.
- 영향: 이 예외는 Map/MiniMap 범위에만 적용한다. old Tarkov-Helper의 다른 기능, updater, 숨은 단축키, 로그, 데이터 규칙은 제품 사양으로 승계하지 않는다. JunhyunHelper 제품 요구사항이 source baseline보다 우선한다.
- 대체한 결정: `DEC-002`의 일반 참고 정책에 대한 명시적 범위 예외

## DEC-032 — Map subsystem은 독립이며 Quest만 JunhyunHelper 진행 데이터와 연결한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10
- 결정: Map artwork/config/general marker/MiniMap/screenshot tracking을 독립 subsystem으로 유지하고, JunhyunHelper Core 기능 중 Quest 진행 상태와 online Quest geometry만 연결한다.
- 이유: Map을 Hideout/Item/Ammo runtime과 결합하면 데이터 업데이트와 지도 안정성이 불필요하게 서로 영향을 준다.
- 영향: Quest geometry는 Content schema v4에 저장한다. Map bundle updater와 Game Content updater는 별도 시스템이다.
- 대체한 결정: 없음

## DEC-033 — v0.1.0에서는 미구현 Scanner를 public UI에 노출하지 않는다

- 상태: `SUPERSEDED by DEC-045`
- 날짜: 2026-08-10
- 결정: Scanner 요구사항이 확정되기 전까지 `준비 중` placeholder 탭을 v0.1.0 사용자 UI에 노출하지 않는다.
- 이유: 기능이 없는 탭은 제품이 미완성처럼 보이게 하고 사용자가 기대할 동작도 정의하지 못한다.
- 영향: 내부 placeholder는 후속 개발을 위해 남길 수 있지만 public navigation에서는 숨긴다. Scanner는 별도 제품 설계 후 다시 추가한다.
- 대체한 결정: 초기 navigation placeholder 노출 방식

## DEC-034 — JunhyunHelper가 release/update와 product hotkey를 소유하며 old application behavior를 승계하지 않는다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10
- 결정: JunhyunHelper의 program release/update와 Map product hotkey는 JunhyunHelper가 소유한다. transplanted Tarkov-Helper의 `UpdateService`, hidden command/easter egg, hidden Ctrl shortcut, legacy direct overlay dispatch, keyboard/foreground logging은 제품 동작이 아니며 release에서 제거한다.
- 이유: old updater는 `Propeex/Tarkov-Helper` release metadata를 가리키고, 숨은 입력 동작은 사용자에게 공개·설정되지 않은 상태에서 게임 플레이 중 의도치 않게 실행될 수 있다.
- 영향: Game Content update는 JunhyunHelper의 `데이터 업데이트` pipeline이 담당한다. program auto-update는 v0.1.0 범위가 아니다. global product hotkey는 JunhyunHelper-owned dispatcher가 담당하며 legacy compatibility hook은 필요한 direct NumPad floor-selection 계약만 유지한다.
- 대체한 결정: 없음

## DEC-035 — v0.1.0은 Windows x64 self-contained portable release로 배포한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10
- 결정: 첫 릴리즈는 installer 없는 Windows x64 self-contained portable ZIP으로 배포한다. 별도 .NET 설치와 관리자 권한을 요구하지 않는다.
- 이유: 현재 개인/초기 배포 단계에서 설치·업데이트 인프라보다 검증된 실행 가능성과 사용자 데이터 분리가 더 중요하다.
- 영향: code signing, installer, application auto-updater는 v0.1.0 blocker가 아니다. unsigned binary이므로 SmartScreen 경고가 있을 수 있다. 배포 규모가 커질 때 별도 설계한다.
- 대체한 결정: 없음

## DEC-036 — Release artifact에는 debug/legacy dependency를 포함하지 않고 공급망 취약점 감사를 gate로 둔다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10
- 결정: release publish에서 PDB와 제품에 사용하지 않는 AutoUpdater/WebView2/GraphX/QuikGraph dependency를 제외하고, direct/transitive NuGet vulnerability audit의 `NU1901`~`NU1904` 경고를 release-blocking으로 취급한다. GitHub Artifact에는 publish directory를 직접 올려 중첩 ZIP을 만들지 않는다.
- 이유: 불필요한 디버그/legacy 파일은 패키지 크기와 공격 표면을 늘리고, 중첩 ZIP은 사용자 배포 경험을 악화시킨다.
- 영향: CI가 이 조건을 자동 검증한다.
- 대체한 결정: 없음

## DEC-037 — Map bundle update는 같은 upstream revision의 원자적 bundle 단위로만 한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-10
- 결정: 향후 Map artwork/config/general-marker DB updater를 구현할 때 서로 다른 revision의 파일을 섞지 않고 동일 upstream revision의 bundle을 candidate/active 단위로 교체한다.
- 이유: 지도 이미지와 좌표 config/marker DB가 서로 다른 revision이면 위치가 맞지 않는 조용한 데이터 오류가 발생할 수 있다.
- 영향: v0.1.0은 검증된 pinned Map bundle을 포함하고 자동 Map bundle update는 후속 범위로 둔다.
- 대체한 결정: 없음

## DEC-038 — 불완전한 Quest availability source는 추측하지 않고 확정 가능한 gate만 보강한다

- 상태: `CONFIRMED / PARTIALLY SUPERSEDED by DEC-039 and DEC-043`
- 날짜: 2026-08-15
- 결정: 최신 Quest availability 판정에서 source가 의미를 명확히 제공하는 `taskRequirements`, level/faction/prestige/trader 조건은 자동 판정한다. 반면 `globalVariable`, `dialogue`, 실제 게임 완료 시각이 필요한 availability delay처럼 JunhyunHelper의 User Progress만으로 참/거짓을 증명할 수 없는 조건은 임의 추정하지 않고 `Indeterminate` 진단으로 보존한다. 특수 상인 접근 gate의 구체적인 보강 의미는 DEC-043을 따른다.
- 이유: 2026-08-15 live source에는 각 GameMode별 `globalVariable` 162건, `dialogue` 12건, delay Quest 13건이 존재한다. 이 조건을 Current로 조용히 확정하거나 UI 클릭 시각으로 타이머를 계산하면 실제 게임과 다른 availability를 만들 수 있다. 반대로 특수 상인 접근 gate 누락은 후속 Quest를 명백히 너무 일찍 열어주는 오류다.
- 영향: 당시 Content schema v5를 도입했다. 이후 special-trader semantics 정정으로 development main은 DEC-043의 Content schema v6를 사용한다. 세부 감사는 `docs/QUEST_PREREQUISITE_AUDIT_2026-08-15.md`와 `docs/QUEST_PREREQUISITE_SEMANTICS.md`를 따른다.
- 대체한 결정: 없음. 단, 기존의 `Indeterminate → optimistic Current` 제품 표시 정책은 DEC-039가 대체하고, 특수 상인에 일괄 Complete gate를 주입하는 부분은 DEC-043이 대체한다.

## DEC-039 — 프로그램이 입증할 수 없는 Quest availability는 `확인 필요`로 분리한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-15
- 결정: Core가 `Indeterminate`로 판정한 Quest를 Application에서 `Current`로 바꾸지 않는다. 사용자에게는 `확인 필요` 상태로 표시하고 `진행 중` 수치와 Map Current Quest sidebar에서 제외한다. 이를 `잠김`으로도 거짓 확정하지 않는다. 사용자가 실제 게임에서 Quest의 완료 또는 비재시작형 영구 실패 사실을 알고 있을 때는 해당 `확인 필요` Quest를 수동으로 완료/실패 동기화할 수 있다.
- 이유: 판별할 수 없는 `globalVariable`, `dialogue`, 실제 게임 완료 시각 기반 delay 등이 200개 이상의 Quest를 `진행 중`처럼 보이게 하여 Current의 의미를 훼손했다. 프로그램이 모르는 사실은 별도 상태로 드러내는 편이 정확하다.
- 영향: Future Needed Items는 기존 `IndeterminatePotential`을 계속 보수적으로 포함하여 사용자가 잠재적으로 필요한 Item을 잘못 버리지 않게 한다. Content schema와 `user.db` schema는 변경하지 않는다.
- 대체한 결정: `DEC-038`의 residual Indeterminate를 Application에서 optimistic Current로 표시한다는 부분

## DEC-040 — Map의 floor 관계는 visibility가 아니라 presentation이다

- 상태: `CONFIRMED / PARTIALLY SUPERSEDED by DEC-041`
- 날짜: 2026-08-15
- 결정: 사용자가 category/faction을 켠 Main Map/MiniMap marker와 extract는 다른 floor라는 이유로 `Collapsed`하지 않는다. 현재 선택 floor와 marker floor의 `Floor.Order` 관계를 presentation으로 표현한다. marker 고유 type/icon 색은 유지하고 floor 관계는 작은 ring으로 표현한다: 현재층=초록, 위층=빨강, 아래층=파랑. 알려진 타층은 약 75% opacity와 매우 작은 방향 glyph를 보조적으로 사용하며, floor가 불명확하면 관계를 추측하지 않는다. Main Map에서 같은 type의 서로 다른 floor marker가 사실상 같은 X/Z에 겹치는 vertical stack은 현재층을 우선하고, 현재층이 없으면 선택 floor와 가장 가까운 `Floor.Order`의 하나를 대표로 표시한다.
- 이유: current-floor-only visibility policy가 타층 marker를 완전히 숨겼고, 큰 화살표와 겹친 회색/초록 marker는 지도 가독성을 떨어뜨렸다. 색상 ring은 기존 marker 의미를 보존하면서 층 관계를 즉시 구분할 수 있다.
- 영향: 지도 artwork 자체는 계속 선택 floor만 표시한다. floor 관계와 category/faction visibility 책임을 분리한다. permanent full-tree polling으로 이 정책을 유지하지 않고 실제 변화 후 bounded/event-driven stabilization을 사용한다.
- 대체한 결정: 과거 `other-floor opacity 50% + 큰 ↑/↓ badge` 표현 및 current-floor-only visibility 구현. 단, 일반 marker vertical-stack 대표 하나만 남기는 예외는 DEC-041이 대체한다.

## DEC-041 — 서로 다른 floor의 일반 marker는 X/Z가 겹쳐도 숨기지 않는다

- 상태: `CONFIRMED`
- 날짜: 2026-08-15
- 결정: Main Map의 일반 marker는 같은 type이고 서로 다른 known floor이며 X/Z가 같거나 가까워도 그 사실만으로 대표 하나만 남기지 않는다. category가 켜져 있고 현재 Map에 속하는 각 marker visual을 유지하고, 각자의 Current/Above/Below relation presentation을 적용한다. 다른 floor marker에 `Opacity=0` 또는 `Collapsed`를 적용하는 vertical-stack suppression은 사용하지 않는다. 같은 물리 항목의 source 중복이라고 의미적으로 확인할 수 있는 경우에만 별도 duplicate 정규화를 허용하며, Factory `Gate 3`처럼 같은 이름·같은 정규화 floor·거의 같은 위치의 extract 대표 visual 정규화는 유지한다.
- 이유: v0.1.4 실사용에서 legacy 일반 marker가 비동기로 추가된 뒤 vertical-stack pass가 타층 marker를 `Opacity=0`으로 바꾸어 `표시됨 → 깜박임 → 사라짐` 회귀가 발생했다. 서로 다른 floor라는 사실은 일반 marker를 같은 물리 항목으로 판단할 충분한 근거가 아니며, DEC-040의 핵심인 “floor는 visibility가 아니라 presentation”과도 충돌했다.
- 영향: `LegacyStandardMarkerFloorPresentationBridge`의 cross-floor near-overlap suppression을 제거한다. async settle 이후 실제 `MapMarkersContainer`의 known off-floor standard marker가 계속 visible/약 75% opacity인지 runtime smoke로 검증한다. 일부 다른 층 아이콘이 같은 X/Z에서 시각적으로 겹칠 수 있으나 floor ring/작은 방향 glyph로 구분하며 표시 자체를 보존한다.
- 대체한 결정: `DEC-040`의 일반 marker vertical-stack representative 예외

## DEC-042 — 층 변경은 Main Map과 MiniMap의 현재 viewport를 보존한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-15
- 결정: floor up/down product hotkey와 NumPad 0~5 direct floor selection은 층 artwork를 바꾸는 동작일 뿐, 사용자가 보고 있던 지도 위치를 재중앙화하는 동작이 아니다. Main Map과 MiniMap은 각각 floor 변경 직전의 live zoom과 viewport 중앙의 map-space 좌표를 캡처하고, 해당 floor render가 끝난 뒤 같은 zoom과 같은 map-space 중심을 복원한다. MiniMap `PlayerTracking`에서는 persisted `MapOffsetX/Y`보다 실제 `MapTranslate`가 현재 viewport의 권위값이며, floor SVG 교체가 stale persisted offset을 다시 적용해 중심을 초기화해서는 안 된다.
- 이유: v0.1.4 실사용에서 MiniMap의 player-centered live transform은 `MapTranslate`에만 갱신되는 반면 floor renderer의 `UpdateMapView()`가 과거 `_settings.MapOffsetX/Y`를 재적용하여 층 변경 때 지도 중심이 초기/이전 위치로 점프했다.
- 영향: JunhyunHelper가 MiniMap floor 변경의 viewport-safe async 경로를 소유한다. floor render 직전 live transform을 persisted offset에도 동기화하여 중간 점프를 막고, render 완료 후 map-space 중심을 복원한다. 실제 Map 변경이나 새로운 screenshot player position처럼 의미상 중심이 바뀌어야 하는 이벤트는 이 규칙의 대상이 아니다. Windows runtime smoke에서 stale persisted offset을 의도적으로 만들어도 floor 변경 전후 MiniMap zoom과 map-space 중심이 동일한지 검증한다.
- 대체한 결정: 없음. 기존 Main Map viewport 보존 계약을 MiniMap까지 명시적으로 완성한다.

## DEC-043 — 특수 상인 접근은 upstream 조건을 보존하고 recoverable 접근은 별도 상태로 모델링한다

- 상태: `CONFIRMED`
- 날짜: 2026-08-15
- 결정: Quest compatibility overlay는 upstream이 이미 제공한 직접 prerequisite를 덮어쓰지 않는다. BTR Driver의 누락 gate는 `A Helping Hand = Active`로만 보강하고, Ref의 누락 gate는 현재 GameMode의 검증된 unlock Quest `Complete`로 보강한다. Lightkeeper는 최초 Getting Acquainted 완료 이후에도 접근을 잃고 Make Amends로 복구할 수 있으므로 모든 후속 Quest에 `Getting Acquainted = Complete`를 ordinary prerequisite로 영구 주입하지 않고 `QuestSpecialTraderAccessRequirement`로 분리한다. 최초 unlock 전에는 수동 접근 동기화로 진행을 우회할 수 없으며, unlock이 완료 또는 실제 영구 실패로 종결된 뒤 실제 게임에서 접근 상실/복구가 발생한 경우에만 sparse profile fact를 기록한다.
- 이유: 2026-08-15 live raw source에서 `Shipping Delay - Part 2`는 `A Helping Hand = Active`인데 기존 overlay가 이를 `Complete`로 덮어써 실제보다 늦게 열었다. 또한 Lightkeeper의 실제 접근권은 monotonic Quest 완료 집합만으로 항상 복원할 수 없어서 Getting Acquainted 완료 하나를 영구 gate로 쓰면 실패→Make Amends 복구 경로를 막는다.
- 영향: development main의 Content schema는 v6이며 v3~v5 snapshot은 읽는 시점에 legacy special-trader overlay를 메모리에서 정규화한다. `user.db` SQLite schema는 v1을 유지하고 optional `SpecialTraderAccessOverrides` JSON fact만 추가한다. recoverable 접근 상실은 영구 `Unavailable`이 아니라 `Locked`다. 일반 Quest에는 별도 `수주 가능` 상태를 추가하지 않으며 DEC-010 자동 수락 원칙을 유지한다. 상세 규칙은 `docs/QUEST_PREREQUISITE_SEMANTICS.md`를 따른다.
- 대체한 결정: `DEC-038`의 Lightkeeper/BTR Driver/Ref에 일괄적으로 `Complete` prerequisite를 보강한다는 부분

---

# 현재 결정 확인 방법

- 제품 요구사항: `docs/PRODUCT.md`
- 기술 경계: `docs/ARCHITECTURE.md`
- 현재 구현/릴리즈 상태: `docs/STATE.md`
- Quest 선행조건/특수 상인 접근: `docs/QUEST_PREREQUISITE_SEMANTICS.md`
- Map 세부 계약: `docs/MAP_PRODUCT_REQUIREMENTS.md`
- 기존 구현 예외/참고 정책: `docs/REFERENCE_POLICY.md`

과거 DEC-001~029의 원문 이유가 필요하면 반드시 역사 파일의 같은 ID를 확인합니다.

## DEC-044 — EFT profile-variable Quest gate는 정확한 read-side 조건을 지원하고 미관측 값은 추측하지 않는다

- 상태: `CONFIRMED`
- 날짜: 2026-08-17
- 결정: `json.tarkov.dev`의 `globalVariable` availability requirement를 더 이상 opaque unsupported 문자열로 축약하지 않고 `variableId`, 비교 연산자, 요구값을 canonical Quest 조건으로 보존한다. 현재 EFT profile의 동일 변수 정수값이 관측되어 있으면 그 값으로 gate를 정확히 판정하고, 값이 요구 임계값보다 낮으면 `Locked`, 충족하면 해당 gate를 통과한 것으로 판정한다. 현재 변수값을 관측할 수 없으면 0이나 완료 Quest 수로 임의 재구성하지 않고 해당 fact만 `Indeterminate`로 남긴다. 향후 scanner/importer가 EFT profile payload의 `Variables` 값을 안전하게 확보하면 동일 user profile fact에 동기화한다.
- 이유: 2026-08-17 live 감사에서 162개의 `globalVariable` 사용은 27개의 trader-local 단계형 변수로 압축되며 EFT 1.1 side-task pool 구조와 강하게 일치했다. 그러나 공개 task feed에는 `X >= N`이라는 read-side 조건만 있고 어떤 Quest 완료가 X를 증가/설정하는지에 대한 authoritative server write rule은 없다. 반면 EFT client profile model에는 정수 `Variables` dictionary가 존재하므로 정확한 현재 값을 관측할 수 있는 source가 확보되면 server write rule을 역추정할 필요 없이 정확 판정할 수 있다.
- 영향: development Content schema는 v7이다. v3~v6 snapshot은 계속 읽을 수 있고, 새 정상 데이터 업데이트는 structured profile-variable requirement를 v7에 저장한다. `GameProfileSnapshot.ProfileVariables`는 optional exact fact이며 key 부재는 unknown을 뜻한다. user.db SQLite schema는 v1을 유지하고 optional JSON property로 값을 저장한다. 지원하지 않는 미래 연산자/변형된 globalVariable shape는 계속 fail-closed `확인 필요`로 처리한다.
- 대체한 결정: `DEC-038`의 “globalVariable 자체를 unsupported availability로 취급한다”는 부분을 대체한다. `프로그램이 증명할 수 없는 fact는 추측하지 않는다`는 보수적 정확도 원칙과 `DEC-039`의 `확인 필요` 정책은 유지한다.

## DEC-045 — Scanner는 미완성 placeholder 탭을 제품 UI에 유지하되 기능을 가장하지 않는다

- 상태: `CONFIRMED`
- 날짜: 2026-08-18
- 결정: Scanner의 실제 제품 요구사항이 확정되기 전까지 상단 `스캐너` 탭은 사용자 UI에 유지하고, 내용은 명확한 `준비 중` placeholder 상태로만 둔다. 실제 인식·스캔·import 동작은 별도 요구사항 확정 전까지 추가하지 않는다.
- 이유: 현재 사용자는 미완성 Scanner 탭 자체를 유지하기를 명시적으로 요구했고, 현행 `PRODUCT.md`와 공개 제품도 placeholder-visible 계약을 사용한다. 과거 v0.1.0 준비 단계의 숨김 결정은 현재 제품 계약과 충돌한다.
- 영향: Scanner placeholder의 존재는 프로그램 미완성 기능을 구현된 것으로 주장하지 않는다. maintenance/refactor 작업은 이 탭을 제거하거나 임의 기능으로 채우지 않는다.
- 대체한 결정: `DEC-033` 전체
