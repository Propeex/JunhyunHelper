# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.7.13 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

현재 요구사항 범위의 제품과 Scanner는 완성 상태이며, 새로운 실제 회귀·호환성 변화 또는 사용자가 명시적으로 결정한 새 제품 요구사항이 없는 한 선제적 기능 추가나 인식 기준 변경을 시작하지 않습니다.

상세 상태:

- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
- `docs/CURRENT_SCANNER_WORK.md`
- `docs/SCANNER.md`

## 현재 공개 릴리즈

```text
version: v1.7.13
Desktop target version: 1.7.13
exact product release source/tag target: 16198c462a6be58d77dbe2dc27aa57eabfc7b9fd
main CI: 33051890329 — SUCCESS
Release workflow: 33052109161 — SUCCESS
release id: 377652938
stable asset: Junhyun-Helper.zip
asset id: 531953179
bytes: 80,486,670
SHA-256: d1cfcf1f606985485584f0e085e8821e0f62156a980f259a90144fd134a7eeb6
400 passed / 0 failed / 0 skipped
```

GitHub `/releases/latest` 및 tag-ref readback에서 v1.7.13이 draft=false, prerelease=false, latest stable이며 tag와 target이 위 exact product release source와 일치함을 확인했습니다. 공개 asset digest도 exact main CI에서 생성한 `Junhyun-Helper.zip`의 SHA-256과 일치합니다.

공식 릴리즈 기록:

- `docs/RELEASE_1.7.13.md`
- `docs/RELEASE_NOTES_V1.7.13.md`
- `docs/.release-v1.7.13-status.json`

이 문서와 이후 documentation-only commit은 v1.7.13 제품 릴리즈 소스가 아닙니다. v1.7.13 source/tag/assets는 위 `16198c46...` 기준으로 immutable historical product release로 취급합니다.

## 주요 기능

- GameMode별 Profile
- Quest availability / prerequisite / special trader / profile-variable
- Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / consumption ledger
- Items / cross-navigation
- Ammo / favorites
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Scanner + Mini Scanner
- Scanner Ground Truth 교정 / diagnostics / regression dataset
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## Scanner

Scanner는 Tarkov 화면 픽셀을 현재 공식 한국어 Tarkov full-item catalog의 Item ID에 연결하는 closed-domain recognizer입니다.

```text
Tarkov window pixels
→ detail rectangle proposals
→ inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ serialized Windows ko-KR OCR
→ optional user substitution
→ conditional environment-aware title normalization
→ conservative official-catalog matching / bounded recovery
→ optional current-pixel visual corroboration
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
```

### Scanner 안전 기준

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

- false positive보다 miss를 선호합니다.
- geometry와 환경 정규화는 Item identity proof가 아닙니다.
- stale/cross-frame OCR 또는 visual result를 현재 Item identity proof로 사용하지 않습니다.
- Item ID가 확정되기 전 price/needed/slot metadata를 identity evidence로 사용하지 않습니다.
- scan 순간 Item identity를 위해 network 요청을 시작하지 않습니다.
- 새로운 reviewed evidence 없이 threshold/candidate cap/matcher/visual acceptance를 낮추지 않습니다.

## v1.7.13 — UI 정리 패치

v1.7.13은 기존 도메인 의미와 Scanner 인식 정책을 유지하면서 반복 조작과 불필요한 UI를 줄였습니다.

- Items의 퀘스트용/은신처용 용도 선택을 제거하고 필요한 아이템 화면을 하나의 기준으로 단순화했습니다.
- Ammo 상단 조작 순서를 정리하고 상세정보를 기본 접힘으로 변경했으며 표 위 중복 요약을 제거했습니다.
- Map 지도 마커 선택/설정은 같은 launcher 재클릭으로 닫히며, 지도 마커 선택은 기본 접힘입니다. 경로 표시/지우기와 단축키 안내 문구는 제거했습니다.
- Scanner 설정은 변경 즉시 저장되며 단축키 설정은 기본 Scanner 화면으로 분리했습니다.
- Scanner 검색에서 필요한 아이템은 기존 `ItemsWorkspace.Plan.NeededItems`의 source를 이용해 관련 Quest/Hideout을 표시하고 이동할 수 있습니다.
- 프로필 편집과 Scanner 설정 등 사용자-facing 편집 화면을 MainWindow 내부 overlay interaction으로 통일했습니다.
- `현재 결과 교정`은 기본 Scanner 화면의 우측 조작 영역에 유지합니다.
- `V1713UiSimplificationContractTests`와 actual published EXE smoke가 Ammo 기본 접힘 왕복, Items 필터 제거, Scanner needed-source authority를 회귀 보호합니다.
- Scanner recognition threshold/candidate/matcher/visual acceptance, 200 ms observation target, Map/MiniMap donor revision, Game Content validation/LKG 계약은 변경하지 않았습니다.

제품 결정:

- `docs/DECISION_V1.7.13_UI_SIMPLIFICATION.md`

## v1.7.12 — 장기 유지보수 패치

v1.7.12는 새 사용자 기능 없이 Desktop lifecycle과 장기 유지보수 경계를 강화했습니다.

- Quest/Hideout/Items/Ammo의 공통 image-cache binding, Ammo favorite store, cross-page navigation wiring을 `MainWindow.OnInitialized`의 명시적인 product-window composition owner로 이동했습니다.
- 개별 탭의 `Loaded` 순서가 unrelated 화면의 infrastructure 준비 상태를 결정하지 않도록 했습니다.
- dead-code 정리 중 actual published EXE smoke가 발견한 Ammo의 hidden WPF lifecycle coupling을 수정했습니다.
- Ammo 검색·상세정보·grid presentation은 `AmmoPage.OnInitialized`가 직접 초기화를 소유합니다.
- source-level ownership regression과 실제 Product UI/Map/Factory/MiniMap smoke를 모두 통과했습니다.
- Scanner recognition threshold/candidate/matcher/visual acceptance, 200 ms observation target과 Map/MiniMap donor revision은 변경하지 않았습니다.

제품 결정:

- `docs/DECISION_LONG_TERM_MAINTENANCE_AUDIT_2026-08-27.md`
- `docs/DECISION_V1.7.12_MAINTENANCE.md`

## v1.7.11 — 유지보수 패치

v1.7.11은 Scanner identity recognition을 바꾸지 않고 기존 제품의 표시·입력·MiniMap 사용성 회귀를 수정했습니다.

- Scanner/Mini Scanner의 `필요 개수`는 전체 요구량이 아니라 현재 Inventory와 FIR 조건을 반영한 canonical `RemainingTotal`을 표시합니다.
- Configurable Map/Scanner hotkey는 등록하지 않은 Ctrl/Alt/Shift가 추가로 눌려도 동작하며, 같은 primary key에 여러 compatible binding이 있으면 더 구체적인 조합을 우선합니다.
- Windows modifier는 지원하지 않으며 Map bare NumPad0~5 직접 층 선택 계약은 유지합니다.
- MiniMap은 첫 표시 전에 현재 Main Map 선택을 동기화합니다.
- MiniMap width/height는 `%LocalAppData%/JunhyunHelper/minimap-window-state.json`에 저장되어 재시작 뒤 복원됩니다.
- 표준 WPF 설명 ToolTip은 제품 전역에서 표시하지 않으며 지도 marker detail 같은 기능성 custom Popup은 유지합니다.
- Scanner recognition threshold, candidate cap, matcher, visual recovery acceptance와 200ms observation target은 변경하지 않았습니다.

제품 결정:

- `docs/DECISION_V1.7.11_MAINTENANCE.md`

## v1.7.10 — 공개 배포 환경 대응

v1.7.10은 특정 사용자 PC에 맞춘 튜닝이 아니라 다양한 정상 Windows/Tarkov 환경에서 Scanner가 더 일관되게 동작하도록 item-title OCR 입력을 hardening했습니다.

```text
normal OCR success
→ 기존 결과 즉시 사용

normal OCR miss 또는 기존 bounded deep pass
→ title ROI luminance profile 분석
→ reference/flat input: 기존 경로 유지
→ lifted/washed/low-contrast input: adaptive normalized auxiliary OCR
→ 기존 conservative catalog matching
→ Item ID or fail closed
```

핵심:

- P60 기반 dark title-field background 추정
- P99.75 기반 sparse bright glyph foreground 추정
- usable contrast가 없는 flat input은 normalization 금지
- 정상 normal OCR 성공 시 histogram/copy/추가 OCR 자체를 생략
- 1080p / 1440p / 4K proportional title raster regression
- SDR-like / lifted / washed / compressed-contrast / low-contrast / flat regression
- 기존 semantic/catalog/matcher/visual acceptance 유지

제품 결정:

- `docs/DECISION_SCANNER_CROSS_ENVIRONMENT_2026-08-26.md`

## v1.7.9 — Mini Scanner 표시

Scanner Item ID가 이미 확정됐는데 Mini Scanner가 별도 inventory-header OCR 실패로 표시되지 않던 presentation 회귀를 수정했습니다.

현재는 confirmed Item identity가 presentation authority이며, 숨겨진 real Scanner의 최초 표시에서만 Tarkov가 foreground인지 확인합니다.

이미 표시 중인 Mini Scanner는 새 confirmed Item으로 즉시 갱신하며, 실제 miss 3회째에 숨깁니다.

## v1.7.8 — raid inspect-header ownership

Raid inventory 수평선이 inspect header와 이어져 header-left ownership이 실제 상세창보다 왼쪽으로 확장되던 문제를 user-reviewed Case로 수정했습니다.

Recovery는 기존 정상 header 경로 뒤에서만 동작하며, 강한 RED-X structural evidence와 기존 close-X/magnifier/header/title evidence, 최종 `HEADER_FRAME_LOCKED >= 0.68`을 모두 요구합니다.

## Scanner 성능 기준선

v1.7.6에서 동일 current-frame visual evidence가 여러 후보에서 반복 계산되며 5~13초까지 지연되던 문제를 수정했습니다.

문제 PC 재검증:

```text
Display Test — 하프 마스크
10,840.877 ms → 70.603 ms

Display Test — USB 보안 플래시 드라이브
12,686.278 ms → 1,354.775 ms
```

실제 Tarkov 성공 12건의 `ReadingTitle → ShowingItem`:

```text
minimum: 38.07 ms
median:  63.92 ms
maximum: 1.05 s
mean:    211.47 ms
```

같은 Scanner cycle의 exact current-pixel evidence만 재사용하며 cross-frame identity cache는 사용하지 않습니다.

## Scanner Ground Truth

정상 Scanner monitoring은 durable automatic correction Case를 만들지 않습니다.

```text
runtime recognition
→ latest exact frame in memory
→ user explicitly opens correction
→ user explicitly saves
→ reviewed durable Ground Truth
```

사용자가 직접 검토/교정한 Case만 Ground Truth입니다. Private user images는 CI 편의를 위해 public repository에 commit하지 않습니다.

## Scanner UI / hotkeys

일반 Scanner 화면은 ON/OFF, 표시 설정, 고급 기능, 단축키 설정, 현재 결과 교정, item search/log를 분리해 제공합니다. 표시 설정은 즉시 저장되며 `현재 결과 교정`은 우측 조작 영역에 둡니다.

기본 hotkey:

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

Configurable Scanner/Map gesture는 primary key + optional Ctrl/Alt/Shift를 사용합니다. 등록된 modifier는 모두 필요하지만 추가 Ctrl/Alt/Shift는 허용하며, 같은 primary key에서 여러 binding이 compatible하면 더 많은 modifier를 요구하는 binding을 우선합니다. Bare key도 허용하며 Windows modifier는 지원하지 않습니다.

## 업데이트 / 패키지

Release candidate는 Windows Release build, automated tests, self-contained win-x64 single-file publish, rendered Product UI/Scanner/Map smoke, graceful shutdown, package/checksum verification을 모두 통과해야 합니다.

Stable release는 main CI가 성공한 exact main commit의 artifact만 Release workflow가 게시합니다.

Mutable user data는 `%LocalAppData%/JunhyunHelper` 아래에 저장되며 Program Update가 기존 사용자 진행도·설정·reviewed Ground Truth를 덮어쓰지 않습니다.
