# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 현재 GitHub 상태가 프로젝트의 기준입니다.

기준일: 2026-08-27  
상태: **v1.7.14 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품

**준현 헬퍼**는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다.

현재 요구사항 범위의 제품은 완성 상태이며 기본 운영 모드는 **유지보수**다. 새 기능은 사용자가 새로운 제품 요구사항으로 명시적으로 결정할 때만 시작한다.

주요 기능:

- GameMode별 Profile / User Progress
- Quest availability / prerequisite / special trader / profile-variable
- Hideout
- Needed Items / FIR·일반 Inventory / consumption ledger
- Items / cross-navigation
- Ammo / favorites
- Map + MiniMap
- Game Content 안전 업데이트 / image cache
- 사용자 동의형 Program Update
- Scanner + Mini Scanner
- Scanner Ground Truth 교정 / diagnostic dataset / regression

Runtime GPT/AI 의존성은 없다.

기존 `Propeex/Tarkov-Helper`는 불완전한 프로토타입이며 새 제품 요구사항의 권위가 아니다. 검증된 Map/MiniMap donor source와 유용한 자산/아이디어만 제한적으로 사용한다.

## 2. 현재 public stable

```text
version: v1.7.14
exact product release source/tag target: 0a51375de36cd13047216006c2c0311728b1bd89
main CI run: 33060827905 — SUCCESS
release workflow run: 33061059154 — SUCCESS
release id: 377720327
asset: Junhyun-Helper.zip
asset id: 532104142
asset bytes: 80,488,363
asset SHA-256: 341ac502d2ace563ab2e7c8d7091a8e796cf87e7d1f5961edf869feab106e2fd
checksum asset: SHA256SUMS.txt
checksum asset id: 532104140
checksum asset bytes: 86
checksum asset SHA-256: 30e66cd988c85491d1a0f369dedec53ddb5afc430ce2bca65a47893ddc1d055d
407 passed / 0 failed / 0 skipped
published UTC: 2026-08-27T10:00:11Z
```

Main-CI published ProductVersion:

```text
1.7.14+0a51375de36cd13047216006c2c0311728b1bd89
```

Main-CI GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9641695152
artifact archive bytes: 241,396,019
artifact archive SHA-256: 43a0e4e68d578dfb458fdbd70764a34c21dc59bca4116c2a1ec63345f0aed3a7
```

GitHub `/releases/latest` 및 tag-ref readback:

- tag `v1.7.14`
- release target = exact product release source
- tag ref object = exact product release source
- draft = false
- prerelease = false
- latest stable = true
- ZIP + checksum assets present
- public `Junhyun-Helper.zip` digest = exact main-CI package SHA-256

상세 공개 증거:

- `docs/RELEASE_1.7.14.md`
- `docs/.release-v1.7.14-status.json`
- `docs/RELEASE_NOTES_V1.7.14.md`
- `docs/DECISION_V1.7.14_UI_CONSISTENCY.md`

이후 documentation-only commit은 v1.7.14 product release source가 아니다. 제품 release source/tag target은 위 `0a51375d...`로 고정한다. 이미 공개된 stable tag/source/assets는 immutable historical product release로 취급한다.

## 3. 아키텍처

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned Tarkov-Helper Map/MiniMap donor source
```

책임:

- **Core**: canonical domain, deterministic calculation, Quest 규칙, Scanner structural/normalization/matcher/presentation 정책
- **Application**: 사용자 use case, authoritative mutation, workspace orchestration
- **Infrastructure**: HTTP/source parsing, SQLite/file persistence, content/update I/O
- **Desktop**: WPF UI, presentation, Scanner capture/OCR/runtime/diagnostics, Map bridge
- **Map/MiniMap donor**: 제한적 compile-link 예외. donor updater/content ownership은 사용하지 않음

Domain truth를 WPF event handler에 복제하지 않는다.

### Desktop startup/composition ownership

`MainWindow.OnInitialized`가 product-window lifetime의 shared presentation composition owner다.

```text
MainWindow.OnInitialized
  → Quest / Hideout / Items / Ammo image cache
  → Ammo favorite store
  → cross-page content navigation
  → Scanner global-command lifetime wiring
```

개별 Page의 internal presentation initialization은 해당 Page가 직접 소유하며 unrelated Page의 `Loaded` 순서에 의존하지 않는다.

Ammo search/detail/grid presentation은 `AmmoPage.OnInitialized` + Loaded-priority dispatcher로 초기화한다.

### Shared in-app overlay ownership

v1.7.14에서 사용자-facing 설정/편집 interaction은 MainWindow shared overlay를 공통 owner로 사용한다.

```text
launcher
→ MainWindow shared overlay owner
→ existing editor / existing UIElement surface
→ existing validation/save semantics
```

현재 주요 surface:

- Profile Edit
- Scanner Settings
- Scanner Advanced
- Map / MiniMap Settings

공통 dismiss:

- same launcher 재클릭
- backdrop click
- common overlay X

Window-backed editor는 `ToggleInAppWindowAsync`로 host한다. 기존 visual tree의 UIElement surface는 `ShowInAppElementAsync`로 host한다. child editor의 저장/검증 의미는 `IInAppOverlayDialog` 또는 기존 authority가 소유하며 MainWindow가 재구현하지 않는다.

Map Settings처럼 기존 visual tree의 UIElement를 임시 re-parent하는 surface는 caller가 원래 parent/index로 복원한다.

## 4. Schema / 사용자 데이터

```text
Desktop target version: 1.7.14
Current public stable executable: 1.7.14
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
```

대표 저장 위치:

```text
%LocalAppData%/JunhyunHelper/user.db
%LocalAppData%/JunhyunHelper/content/
%LocalAppData%/JunhyunHelper/image-cache/
%LocalAppData%/JunhyunHelper/map-product-settings.json(.bak)
%LocalAppData%/JunhyunHelper/minimap-window-state.json
%LocalAppData%/JunhyunHelper/ammo-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
%LocalAppData%/JunhyunHelper/logs/
```

원칙:

- portable executable 옆에 mutable user data/log 생성 금지
- Program Update가 user.db, content/image cache, Map/MiniMap/Ammo/Scanner settings, Scanner logs/diagnostics/Ground Truth를 교체하지 않음
- user-reviewed Scanner Ground Truth는 자동 삭제하지 않음
- Scanner logs와 Ground Truth dataset lifetime 분리
- 정상 Scanner monitoring은 durable automatic correction Case를 생성하지 않음

## 5. Game Content / Scanner catalog

일반 Game Content와 Scanner catalog는 사용자에게 별도 관리 절차를 요구하지 않는다.

```text
remote Game Content
→ download / parse
→ canonical build
→ integrity/completeness validation
→ general content activation
→ Scanner catalog refresh
→ local last-known-good preservation on partial failure
```

Scanner scan 순간에는 local/memory catalog만 사용하며 identity 결정을 위해 network 요청을 시작하지 않는다.

공식 Korean Tarkov full-item catalog가 Scanner Item identity authority다. market/dimension coverage와 Item identity health는 분리한다.

Game Content 안전 계약:

- failed candidate는 last-known-good active content를 덮어쓰지 않음
- normal snapshot shrink guard = baseline의 50%
- collection schema drift는 fail closed
- Wiki Ballistics enrichment는 fail-soft
- User Progress와 Game Content authority 분리

## 6. Scanner — 현재 제품 계약

Scanner 상태: **FEATURE COMPLETE / MAINTENANCE ONLY**.

```text
Tarkov window pixels
→ capture
→ detail rectangle proposals
→ close-X / magnifier / inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ serialized Windows ko-KR OCR
→ optional user OCR substitution
→ conditional cross-environment title normalization
→ current-catalog sanitation / normalization
→ conservative catalog matching / bounded recovery
→ optional current-pixel visual corroboration
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
→ optional correction / Ground Truth
```

Scanner는 범용 OCR이 아니라 closed-domain recognizer다. false positive는 miss보다 나쁘다.

핵심 불변식:

```text
structural floor = 0.34
trusted HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

- geometry/environment normalization은 Item identity proof가 아님
- stale/cross-frame OCR/visual result를 current identity proof로 사용하지 않음
- Item ID 확정 전 price/needed/slot/previous-frame metadata를 identity evidence로 사용하지 않음
- current official catalog 밖 임의 Item 생성 금지
- reviewed evidence 없이 recognition threshold/candidate cap/matcher/visual acceptance 완화 금지
- Needed quantity authority = `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`
- Needed source authority = `ItemsWorkspace.Plan.NeededItems[itemId].Sources`

Scanner settings v1.7.14 presentation:

- Mini Scanner display configuration과 Scanner hotkey configuration은 Scanner Settings가 함께 소유
- 설정 변경은 기존 persistence authority를 사용
- Scanner Advanced는 shared overlay에 host
- old dedicated `ScannerHotkeySettingsWindow`는 제거됨

## 7. v1.7.14 — UI consistency

v1.7.14는 domain/recognition 의미를 바꾸지 않고 popup·overlay·search interaction을 통일한 patch다.

### Ammo

- `즐겨찾기 선택`, `표시 열` popup launcher는 true toggle이다.
- WPF `Popup.StaysOpen=False`의 자동 닫힘 뒤 기존 Button Click이 다시 여는 회귀를 Preview 단계에서 차단한다.

### Map / MiniMap

- MiniMap launcher 주변 donor 잔여 padding/background/help-button 공간 제거
- `지도 마커` launcher에 제품 기본 Button chrome 적용
- marker panel collapsed 상태에서 빈 min-width/padding/background/border 제거
- expanded 상태에서 viewport 기반 충분한 세로 공간 확보
- Map/MiniMap Settings를 MainWindow shared overlay에 표시
- pinned donor source 자체는 수정하지 않고 first-party customization boundary에서 적용

Pinned donor:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

### Scanner / Profile

- Scanner Advanced standalone Window 표시 제거 → shared overlay host
- Advanced 내용 내부 별도 close button 제거
- Scanner hotkey editor를 Scanner Settings에 통합
- Profile editor content card를 Scanner 설정과 같은 overlay/card 계열로 정리

### Search

Quest / Hideout / Items / Ammo / Scanner 주요 검색창은 입력창 우측 내부 `×` clear affordance를 사용한다. clear behavior는 presentation-only이며 기존 filtering logic을 변경하지 않는다.

### Regression

```text
V1714UiConsistencyContractTests
407 passed / 0 failed / 0 skipped
actual published EXE Product UI + Scanner + Map + Factory + MiniMap smoke
```

공식 결정: `docs/DECISION_V1.7.14_UI_CONSISTENCY.md`.

## 8. 현재 유지되는 이전 핵심 결정

### v1.7.13 UI simplification

- Items 용도 필터 제거 → canonical All
- Ammo 상세 기본 접힘
- Map marker selector 기본 접힘
- Map trail/clear-trail/hotkey 안내 제거
- Scanner searched needed-item source = existing `NeededItems[].Sources`
- Scanner correction action 우측 배치

### v1.7.12 lifecycle hardening

- MainWindow shared composition ownership 명시
- Ammo hidden WPF Loaded coupling 제거
- speculative workspace DB optimization 보류: runtime bottleneck evidence 없음

### v1.7.11 presentation/hotkey polish

- Scanner 필요 개수 = `RemainingTotal`
- configurable hotkey extra-modifier compatibility / most-specific-wins
- MiniMap first-open current-map synchronization
- MiniMap size persistence
- standard explanatory WPF ToolTip 비표시

### v1.7.10 cross-environment Scanner hardening

- normal OCR success는 그대로 즉시 사용
- miss/deep-pass에서만 environment-aware title normalization
- lifted/washed/low-contrast input에만 bounded auxiliary OCR evidence
- flat/no-contrast input은 fail closed

역사적 상세는 버전별 `DECISION_*` / `RELEASE_*` 문서를 사용한다.

## 9. Active technical debt / dead-code 판단

이름이나 겉모양만으로 제거하지 않는다.

현재 유지:

- `Legacy` Map/MiniMap bridge — active integration
- Factory/Map/MiniMap smoke — active regression gate
- Scanner diagnostic OCR reflection adapter — intentional technical debt
- original full-refresh mutation handlers + fast rebinding — lifecycle 관여 evidence가 있어 유지

현재 보류:

- workspace one-read/multi-build
- 추가 global cache
- speculative parallelization

`UserProfileStore.LoadAsync`는 첫 authoritative read/save 뒤 immutable in-process snapshot cache를 사용하므로 runtime trace 없이 반복 호출을 SQLite 병목으로 가정하지 않는다.

## 10. 릴리즈 계약

Public stable release는 공개 후 immutable historical product artifact로 취급한다.

Runtime 변경 PATCH의 full release gate:

```text
Release build
→ deterministic tests
→ win-x64 self-contained single-file publish
→ ProductVersion / FIRST_RUN identity verification
→ actual published EXE Product UI / Scanner / Map / Factory / MiniMap smoke
→ graceful shutdown
→ portable root/dependency verification
→ release package checksum verification
→ main CI exact-source rerun
→ Release workflow exact artifact verification
→ tag/release/public asset readback
```

Documentation-only main commit은 같은 assembly version에서 ProductVersion commit metadata 때문에 다른 bytes를 만들 수 있다. 이미 공개된 stable release를 교체하지 않는다. Release workflow의 immutable-existing-release path는 required assets 존재를 확인하고 성공 종료해야 한다.

## 11. 다음 작업

현재 v1.7.14 릴리즈 배치에는 남은 제품 개발 작업이 없다.

기본 다음 작업은 다음 중 실제 evidence가 생길 때만 시작한다.

- 실사용 오류
- Tarkov 데이터/schema 변화
- Program Update/배포 회귀
- Scanner reviewed Ground Truth가 입증한 회귀
- 사용자가 명시적으로 확정한 새 제품 요구사항

새 Scanner 문제는 다음 순서로 처리한다.

```text
runtime evidence 확보
→ failure stage 분류
→ root cause 확인
→ affected layer 최소 수정
→ reviewed regression 추가
→ full release gate
```

## 12. 다음 세션 복구 순서

`AGENTS.md`의 작업 규약을 먼저 적용하고, 그 문서가 정한 필수 복구 순서를 그대로 따른다.

1. `README.md`
2. `docs/STATE.md`
3. `docs/PRODUCT.md`
4. `docs/DECISIONS.md`
5. `docs/MAINTENANCE_CONTRACTS.md`
6. `docs/DEVELOPER_REFERENCE.md`
7. `docs/ARCHITECTURE.md`
8. `docs/VERSIONING.md`
9. `docs/DEVELOPMENT.md`
10. `docs/REFERENCE_POLICY.md`
11. 현재 작업과 관련된 전문 문서 / 코드 / 테스트 / 이슈 / PR / CI

새 대화는 과거 대화 기억을 신뢰하지 않고 위 순서로 현재 저장소 상태를 확인한다.
