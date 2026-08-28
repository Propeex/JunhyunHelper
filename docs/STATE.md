# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 현재 GitHub 상태가 프로젝트의 기준입니다.

기준일: 2026-08-28 KST  
상태: **v1.9.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품

**준현 헬퍼**는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다.

현재 확정 요구사항 범위의 제품과 Scanner는 완성 상태이며 기본 운영 모드는 **유지보수**다. 새 기능은 사용자가 새로운 제품 요구사항으로 명시적으로 결정할 때만 시작한다.

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
- Scanner 아이템 정보 DB
- Scanner item Favorites / Recents

Runtime GPT/AI 의존성은 없다.

기존 `Propeex/Tarkov-Helper`는 불완전한 프로토타입이며 새 제품 요구사항의 권위가 아니다. Map/MiniMap은 사용자가 검증한 pinned donor source만 제한적으로 사용한다.

## 2. 현재 public stable

```text
version: v1.9.0
exact product release source/tag target:
e0b0d303141563af564cd71cf00d8c1bfeafe44d
main CI run: 33165706386 — SUCCESS
release workflow run: 33165905504 — SUCCESS
release id: 378431058
432 passed / 0 failed / 0 skipped
published UTC: 2026-08-28T11:08:59Z
```

Main-CI published ProductVersion:

```text
1.9.0+e0b0d303141563af564cd71cf00d8c1bfeafe44d
```

Main-CI release package:

```text
Junhyun-Helper.zip
bytes: 80,538,029
SHA-256:
9ee63042746aee27ddff4407e8240d65b3740696576fe7514b4f92fe8f1e1d44
```

Main-CI GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9683545225
artifact archive bytes: 241,545,444
artifact archive SHA-256:
098c74a99dc6d57c7a01b0e70c860c0d2925e6bbf4835ac2eacabf1f3e5d1bd8
```

Public assets:

```text
Junhyun-Helper.zip
asset id: 533681571
bytes: 80,538,029
SHA-256:
9ee63042746aee27ddff4407e8240d65b3740696576fe7514b4f92fe8f1e1d44

SHA256SUMS.txt
asset id: 533681572
bytes: 86
SHA-256:
2cd7157b4ebeaaa86fa73ee1eccbd1dedac8112089ad04994bd04228fcdcce32
```

GitHub `/releases/latest` 및 tag-ref readback:

- tag `v1.9.0`
- release target = `e0b0d303141563af564cd71cf00d8c1bfeafe44d`
- tag ref object = same exact product release source
- draft = false
- prerelease = false
- latest stable = true
- ZIP + checksum assets present
- public `Junhyun-Helper.zip` bytes/digest = exact main-CI package bytes/SHA-256

공식 공개 증거:

- `docs/RELEASE_1.9.0.md`
- `docs/.release-v1.9.0-status.json`
- `docs/RELEASE_NOTES_V1.9.0.md`
- `docs/DECISION_V1.9.0_SCANNER_FAVORITES_RECENTS_AND_UI_FIXES.md`
- `docs/RELEASE_1.8.4.md` — 이전 Ammo toolbar / Scanner item detail
- `docs/RELEASE_1.8.2.md` — Ammo runtime / current live relationship 회귀 수정
- `docs/DECISION_V1.8.2_RUNTIME_LIVE_REGRESSIONS.md`
- `docs/RELEASE_1.8.1.md` — item relationship completeness hardening
- `docs/DECISION_V1.8.1_ITEM_RELATIONSHIP_COMPLETENESS.md`
- `docs/RELEASE_1.8.0.md` — Scanner item database 기능 릴리즈
- `docs/DECISION_V1.8.0_SCANNER_ITEM_DATABASE.md`

**중요:** 이 문서 갱신처럼 공개 이후 생성되는 documentation-only commit은 v1.9.0 product release source가 아니다. 공개 v1.9.0 source/tag/assets는 위 `e0b0d303...` 기준의 immutable historical product release다.

## 3. v1.9.0 Scanner Favorites / Recents + UI regression fixes

### Favorites / Recents

- Scanner detail header에서 별 버튼으로 favorite 등록/해제한다.
- 오른쪽 사용자 영역은 Favorites 약 2/3 + Recents 약 1/3이다.
- 두 목록은 각각 독립 vertical scroll을 사용하고 horizontal scroll은 사용하지 않는다.
- 긴 이름은 ellipsis 처리한다.
- favorites/recents persistence는 canonical Item ID와 order만 저장한다.
- item name/icon/price/needed/relationship presentation은 현재 GameMode catalog/context에서 다시 resolve한다.
- recent는 실제 item detail open 시에만 기록한다.
- recent order = newest-first, duplicate reopen = move-to-top, max = 50.
- recent 개별 삭제와 전체 삭제는 favorite state와 독립적이다.
- 재시작 후 유지한다.

### Canonical Scanner item-open boundary

모든 실제 item detail navigation은 하나의 제품 경계로 수렴한다.

```text
direct search
related recipe/barter item
favorite row
recent row
→ OpenScannerItemDetails(details)
→ base detail render
→ relationship presentation
→ favorite state
→ recent record
```

GameMode 전환에 따른 자동 detail re-render는 history event가 아니므로 recent order를 다시 올리지 않는다.

Saved-list row resolve는 최대 50개에 대해 full relationship graph를 재구성하지 않고 current-mode catalog에서 이름/아이콘만 경량 resolve한다.

### Search / detail state separation

- search text/results/popup은 navigation surface다.
- open detail은 독립 selection state다.
- search text를 clear하거나 popup이 닫혀도 이미 열린 detail은 유지한다.

### GameMode transition

Favorites/Recents identity는 Regular/PvE 공통 canonical Item ID로 저장한다. 사용자가 Scanner 화면을 보고 있는 동안 active profile GameMode와 Scanner catalog mode가 다르면 current context로 refresh하고 saved lists 및 open detail을 current-mode presentation으로 다시 resolve한다.

Scanner runtime의 기존 context monitor와 catalog refresh gate/runtime start idempotence를 유지한다. UI guard는 visible presentation stale-state를 제거하는 역할이며 recognition authority를 바꾸지 않는다.

### 사용자용 Scanner 로그

기존 오른쪽 `로그` 영역은 user-facing UI에서 제거했다. 내부 diagnostic activity, correction, Ground Truth pipeline은 유지한다. 숨겨진 diagnostic host는 사용자 입력 surface가 아니다.

### Map extract filters

지도 marker 선택 패널에서 다음 donor controls를 실제 instance 그대로 사용한다.

- extract master
- PMC extracts
- SCAV extracts
- Transit extracts

별도 복제 checkbox를 만들지 않는다. donor의 기존 Checked/Unchecked handler, settings persistence, marker rendering, MiniMap refresh를 보존한다.

### Ammo dropdown cycle

- caliber ComboBox와 favorite-caliber ComboBox는 같은 runtime item template/icon state를 공유한다.
- shared icon cycle interval = **700 ms**.
- filtering/favorite persistence 의미는 변경하지 않았다.

### Published runtime evidence

Exact-main self-contained Windows executable에서 다음 evidence를 직접 확인했다.

```text
Ammo animated dropdown:
product-lifecycle=ok
ammo-caliber-runtime-template=ok
favorites-shared-template=ok
rendered-caliber-image=ok
rendered-favorite-image=ok
shared-timer-cycle=ok
shared-cycle-ms=700

Ammo toolbar:
favorite-selector-left=ok
displayed-columns-visible=ok
displayed-columns-right-edge=ok

Map extract filters:
real-donor-checkboxes=ok
marker-panel-visible=ok
master-filter-render-state=ok
minimap-refresh-handler-preserved=ok

Scanner item detail:
product-lifecycle=ok
canonical-open-boundary=ok
basic-four-fields=ok
empty-sections-hidden=ok
recipe-wrap=ok
related-item-buttons=ok
acquisition-groups=ok

Scanner Favorites / Recents:
search-clear-detail=ok
favorite-toggle-persistence=ok
recent-open-persistence=ok
right-pane-two-to-one=ok
independent-scroll=ok
user-log-pane-hidden=ok
canonical-item-id=ok
```

동일 실행에서 Product UI / Main Map / Factory / MiniMap / graceful shutdown / clean portable root도 성공했다.

## 4. Current live Game Content release-readiness

v1.9.0은 external Game Content importer/schema/validator 의미를 변경하지 않았기 때문에 새 live network probe를 release blocker로 요구하지 않았다.

마지막 schema-affecting 공개 검증은 다음과 같다.

```text
live probe run: 33151060959 — SUCCESS
Regular: items=5312 quests=517 objectives=1457 questItems=305 hideout=26 ammo=200 validationIssues=0 fatal=0
PvE:     items=5312 quests=514 objectives=1434 questItems=293 hideout=26 ammo=200 validationIssues=0 fatal=0
```

각 mode의 `sourceWarnings=1`은 당시 Tarkov Wiki Ballistics coverage warning이며 canonical validation failure가 아니다.

## 5. 아키텍처

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned Tarkov-Helper Map/MiniMap donor source
```

책임:

- **Core** — canonical domain, deterministic calculation, Quest/Needed Items/Scanner pure policy, item relationship canonical model/query
- **Application** — 사용자 use case, authoritative mutation, workspace orchestration
- **Infrastructure** — HTTP/source parsing, SQLite/file persistence, content/update I/O, relationship import/validation/snapshot, Scanner presentation-state persistence
- **Desktop** — WPF UI, presentation, Scanner capture/OCR/runtime/diagnostics, item database projection, Map bridge
- **Map/MiniMap donor** — 제한적 compile-link 예외. donor updater/content ownership은 사용하지 않음

Domain truth를 WPF event handler에 복제하지 않는다.

### Desktop startup/composition

`MainWindow.OnInitialized`가 product-window lifetime의 shared presentation composition owner다.

```text
MainWindow.OnInitialized
→ Quest / Hideout / Items / Ammo image cache
→ Ammo favorite store
→ cross-page navigation
→ Scanner global-command lifetime wiring
```

개별 Page의 internal presentation initialization은 해당 Page가 직접 소유한다. WPF type-level registration이 필요한 경우 해당 Page type initialization 경계에서 결정적으로 소유한다.

### Shared in-app overlay

현재 주요 surface:

- Profile Edit
- Scanner Settings
- Scanner Advanced
- Map / MiniMap Settings

공통 dismiss:

- same launcher 재클릭
- backdrop click
- common overlay X

Child editor의 validation/save semantics를 MainWindow가 재구현하지 않는다.

## 6. Schema / 사용자 데이터

```text
Desktop target version: 1.9.0
Current public stable executable: 1.9.0
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
Scanner item UI state: JSON / canonical Item ID + order
```

Content v8 delta:

- canonical trader direct-purchase relationships
- trader barter relationships
- hideout craft relationships
- flea acquisition item set
- item type/category/width/height/weight/base price/flea-tradable presentation fields
- trader reset-time presentation field

대표 저장 위치:

```text
%LocalAppData%/JunhyunHelper/user.db
%LocalAppData%/JunhyunHelper/content/
%LocalAppData%/JunhyunHelper/image-cache/
%LocalAppData%/JunhyunHelper/map-product-settings.json(.bak)
%LocalAppData%/JunhyunHelper/minimap-window-state.json
%LocalAppData%/JunhyunHelper/ammo-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/
%LocalAppData%/JunhyunHelper/scanner/scanner-item-ui-state.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
%LocalAppData%/JunhyunHelper/logs/
```

원칙:

- portable executable 옆에 mutable user data/log 생성 금지
- Program Update가 user.db, content/image cache, Map/MiniMap/Ammo/Scanner settings, Scanner item UI state, logs/diagnostics/Ground Truth를 교체하지 않음
- user-reviewed Scanner Ground Truth는 자동 삭제하지 않음
- 정상 Scanner monitoring은 durable automatic correction Case를 생성하지 않음

## 7. Game Content / Scanner catalog

```text
remote Game Content
→ download / parse
→ canonical build
→ integrity/completeness validation
→ general content activation
→ Scanner catalog refresh
→ local last-known-good preservation on partial failure
```

Game Content 안전 계약:

- failed candidate는 last-known-good active content를 덮어쓰지 않음
- normal snapshot shrink guard = healthy baseline의 50%
- collection schema drift는 fail closed
- Wiki Ballistics enrichment는 fail-soft
- User Progress와 Game Content authority 분리
- v8 relationship graph reference/price/count/limit integrity도 activation 전 검증
- healthy v8+ baseline이 있으면 purchase/barter/craft/flea relation과 barter/craft material edge를 각각 50% retained-floor로 비교
- fresh v8+ critical relationship collection의 전면 empty는 fail closed
- candidate persistence read-back에서 relationship integrity + completeness를 반복 검증
- activation/active recovery에서도 relationship integrity를 검증
- v3~v7 null relationship graph는 legacy compatibility로 허용
- audited Bitcoin passive production identity만 일반 craft relationship import에서 제외
- canonical-identical direct-purchase record만 deduplicate
- 그 외 empty-required craft 및 의미가 서로 다른 trader offer의 기존 보수적 의미는 유지

Scanner scan/search 순간에는 local/memory data만 사용하며 identity 또는 item relationship 조회를 위해 network 요청을 시작하지 않는다.

## 8. Scanner — 현재 제품 계약

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

핵심 불변식:

```text
structural floor = 0.34
trusted HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

- false positive보다 miss 선호
- geometry/environment normalization은 Item identity proof가 아님
- stale/cross-frame OCR/visual result를 current identity proof로 사용하지 않음
- Item ID 확정 전 price/needed/slot/source/relationship/previous-frame metadata를 identity evidence로 사용하지 않음
- current official Korean full-item catalog 밖 임의 Item 생성 금지
- reviewed evidence 없이 recognition threshold/candidate cap/matcher/visual acceptance 완화 금지
- Needed quantity authority = `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`
- Needed source authority = `ItemsWorkspace.Plan.NeededItems[itemId].Sources`
- `SerializedScannerOcrEngine` diagnostic reflection adapter는 intentional technical debt

Scanner Favorites/Recents는 presentation/navigation state이며 recognition identity proof에 참여하지 않는다.

## 9. Map / MiniMap 기준선

Pinned donor:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

Map/MiniMap은 donor source를 broad-edit하지 않고 JunhyunHelper first-party bridge/customization boundary에서 제품 delta를 적용한다.

- floor는 visibility filter가 아니라 presentation relation
- cross-floor enabled marker 유지
- Main Map floor change 시 zoom + map-space center 보존
- MiniMap floor change 시 transform 보존
- current Quest만 JunhyunHelper progress/content와 cross-feature bridge
- extract filter UI는 donor의 실제 checkbox instances/handlers를 재사용

`Legacy` 이름이 붙은 Map/MiniMap bridge는 active integration이므로 이름만 보고 dead code로 삭제하지 않는다.

## 10. Active technical debt / 유지 판단

현재 유지:

- active `Legacy` Map/MiniMap bridge
- Factory/Map/MiniMap actual smoke
- Scanner diagnostic OCR reflection adapter
- lifecycle-evidenced original full-refresh mutation handlers + fast rebinding

현재 runtime evidence 없이 보류:

- workspace one-read/multi-build
- 추가 global cache
- speculative parallelization

`UserProfileStore.LoadAsync`는 첫 authoritative read/save 뒤 immutable in-process snapshot cache를 사용하므로 runtime trace 없이 반복 호출을 SQLite 병목으로 가정하지 않는다.

## 11. 릴리즈 / 유지보수 검증 계약

Runtime 변경 release의 full release gate:

```text
Release build
→ deterministic tests
→ win-x64 self-contained single-file publish
→ ProductVersion / FIRST_RUN identity verification
→ actual published EXE Product UI / Scanner / Map / Factory / MiniMap smoke
→ feature-specific published runtime marker/evidence
→ graceful shutdown
→ portable root/dependency verification
→ release package checksum verification
→ external schema/meaning 변경 시 current Regular/PvE live release-readiness evidence
→ exact-main CI
→ Release workflow exact artifact verification
→ tag/release/public asset readback
```

사용자-visible WPF 변경은 source assertion이나 문서만으로 완료 판정하지 않는다. `Loaded`, type initializer, runtime `DataTemplate`, dynamic control tree, timer/animation 등에 의존하는 경우 actual published executable의 control tree/runtime evidence를 확보한다.

Smoke가 검증 대상 UI를 뒤늦게 생성/수정해 원래 초기화 실패를 가려서는 안 된다.

Game Content importer/schema/validator 의미 변경은 hermetic fixture test 외에도 공개 직전 current Regular/PvE live source를 canonical pipeline으로 확인한다. 이 live probe는 일반 PR CI의 상시 network dependency로 만들지 않는다.

Published stable release는 공개 후 immutable historical product artifact로 취급한다.

`actions/download-artifact@v8`의 upstream Node `Buffer()` deprecation warning은 현재 release correctness와 무관한 monitor-only upstream warning이다.

## 12. 현재 유지되는 핵심 결정

- **v1.9.0** — Scanner Favorites/Recents canonical Item ID persistence, search/detail separation, unified item-open boundary, current-GameMode re-resolution, user log UI removal, real Map extract filters, Ammo 700 ms shared icon cycle, strengthened published-runtime gate
- **v1.8.4** — Ammo toolbar placement + Scanner item detail vertical recipe/acquisition presentation + published executable runtime evidence gate
- **v1.8.2** — Ammo runtime initialization + rendered-icon/shared-cycle smoke + audited current live relationship normalization; fail-closed/LKG/Scanner recognition unchanged
- **v1.8.1** — item relationship top-level/nested completeness LKG hardening + persisted/active relationship revalidation
- **v1.8.0** — Scanner item search를 canonical local item database로 확장; recognition policy unchanged
- **v1.7.15** — version-only header, Items cleanup dot, Map marker selector polish, Ammo caliber/Favorites icon cycling
- **v1.7.14** — popup true-toggle, shared overlay, search clear consistency
- **v1.7.13** — Items purpose selector 제거, Ammo detail 기본 접힘, Map trail/hotkey copy 제거
- **v1.7.12** — Desktop lifecycle/composition ownership hardening
- **v1.7.11** — Scanner RemainingTotal, configurable hotkey compatibility, MiniMap sync/size persistence, standard ToolTip 비표시
- **v1.7.10** — cross-environment Scanner title normalization hardening

역사적 상세는 버전별 `DECISION_*` / `RELEASE_*` 문서를 사용한다.

## 13. 다음 작업

현재 **v1.9.0 릴리즈 배치에는 남은 제품 개발 작업이 없다.**

기본 다음 작업은 다음 중 실제 evidence가 생길 때만 시작한다.

- 실사용 오류
- Tarkov 데이터/schema 변화
- Program Update/배포 회귀
- Scanner reviewed Ground Truth가 입증한 회귀
- item relationship source/schema/completeness 또는 presentation 회귀
- 사용자가 명시적으로 확정한 새 제품 요구사항

새 Scanner 문제는 다음 순서로 처리한다.

```text
runtime evidence 확보
→ failure stage 분류
→ root cause 확인
→ affected layer 최소 수정
→ reviewed regression 추가
→ published executable runtime smoke
→ full release gate
```

## 14. 다음 세션 복구 순서

`AGENTS.md`의 필수 복구 순서를 그대로 따른다.

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

새 대화는 과거 대화 기억을 신뢰하지 않고 저장소의 현재 문서와 GitHub 상태를 확인한다.
