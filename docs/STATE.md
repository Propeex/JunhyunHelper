# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 현재 GitHub 상태가 프로젝트의 기준입니다.

기준일: 2026-08-28 KST  
상태: **v1.8.4 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

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

Runtime GPT/AI 의존성은 없다.

기존 `Propeex/Tarkov-Helper`는 불완전한 프로토타입이며 새 제품 요구사항의 권위가 아니다. Map/MiniMap은 사용자가 검증한 pinned donor source만 제한적으로 사용한다.

## 2. 현재 public stable

```text
version: v1.8.4
exact product release source/tag target:
13af4e3a452139dedc32b2db9aa51266e2a01d2a
main CI run: 33153043430 — SUCCESS
release workflow run: 33153234911 — SUCCESS
release id: 378333813
424 passed / 0 failed / 0 skipped
published UTC: 2026-08-28T07:55:12Z
```

Main-CI published ProductVersion:

```text
1.8.4+13af4e3a452139dedc32b2db9aa51266e2a01d2a
```

Main-CI release package:

```text
Junhyun-Helper.zip
bytes: 80,528,868
SHA-256:
9e06c16e20a346ad7691dccfee9a2caebcdb6c0cd9a6a35859bcb97d8e03fa42
```

Main-CI GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9678543773
artifact archive bytes: 241,519,179
artifact archive SHA-256:
e966c849652f7408bba868e920ffd45a622bf195309385ea04aad6f1f8758bf0
```

Public assets:

```text
Junhyun-Helper.zip
asset id: 533461834
bytes: 80,528,868
SHA-256:
9e06c16e20a346ad7691dccfee9a2caebcdb6c0cd9a6a35859bcb97d8e03fa42

SHA256SUMS.txt
asset id: 533461832
bytes: 86
SHA-256:
535514fb48f23e1fe7834ba0cd5be54235f15922d036f5ad071c829ff80b4aad
```

GitHub `/releases/latest` 및 tag-ref readback:

- tag `v1.8.4`
- release target = `13af4e3a452139dedc32b2db9aa51266e2a01d2a`
- tag ref object = same exact product release source
- draft = false
- prerelease = false
- latest stable = true
- ZIP + checksum assets present
- public `Junhyun-Helper.zip` bytes/digest = exact main-CI package bytes/SHA-256

공식 공개 증거:

- `docs/RELEASE_1.8.4.md`
- `docs/.release-v1.8.4-status.json`
- `docs/RELEASE_NOTES_V1.8.4.md`
- `docs/DECISION_V1.8.4_AMMO_SCANNER_ITEM_DETAIL.md`
- `docs/RELEASE_1.8.2.md` — Ammo runtime / current live relationship 회귀 수정
- `docs/DECISION_V1.8.2_RUNTIME_LIVE_REGRESSIONS.md`
- `docs/RELEASE_1.8.1.md` — item relationship completeness hardening
- `docs/DECISION_V1.8.1_ITEM_RELATIONSHIP_COMPLETENESS.md`
- `docs/RELEASE_1.8.0.md` — Scanner item database 기능 릴리즈
- `docs/DECISION_V1.8.0_SCANNER_ITEM_DATABASE.md`

**중요:** 이 문서 갱신처럼 공개 이후 생성되는 documentation-only commit은 v1.8.4 product release source가 아니다. 공개 v1.8.4 source/tag/assets는 위 `13af4e3a...` 기준의 immutable historical product release다.

## 3. v1.8.4 Ammo Toolbar / Scanner Item Detail

### Ammo

- `즐겨찾기 선택`은 탄약 탭 왼쪽 선택 영역에 둔다.
- `표시 열` 버튼은 툴바 오른쪽 끝에 둔다.
- 구경과 즐겨찾기 ComboBox는 동일한 runtime icon template/state를 공유한다.
- 같은 구경 탄약 아이콘의 기존 순환 애니메이션/timing을 유지한다.
- filtering/favorite persistence 의미는 변경하지 않았다.

### Scanner item detail

상세 presentation은 하나의 세로 흐름으로 정리한다.

```text
기본 정보
→ 사용하는 곳
→ 수급처
```

기본 정보는 다음 네 항목만 표시한다.

- 크기
- 플리마켓 평균가
- 최고 상인 판매가
- 현재 필요한 개수

Quest/Hideout 사용처는 기존 navigation authority를 유지한다.

제작/교환 관계는 result item + complete materials를 recipe card로 표시한다. 좁은 폭에서는 materials가 다음 줄로 자연스럽게 wrapping된다. 관련 item button은 같은 Scanner item detail로 이동한다.

수급처는 canonical relation에 따라 다음 그룹으로만 표시한다.

- 제작
- 교환
- 구매
- 레이드 획득

실제 row가 없는 그룹은 empty shell을 표시하지 않는다.

### Published runtime evidence

Exact-main self-contained Windows executable에서 다음 evidence를 직접 확인했다.

```text
Ammo animated dropdown:
rendered-caliber-image=ok
rendered-favorite-image=ok
shared-timer-cycle=ok

Ammo toolbar:
favorite-selector-left=ok
displayed-columns-visible=ok
displayed-columns-right-edge=ok

Scanner item detail:
basic-four-fields=ok
empty-sections-hidden=ok
recipe-wrap=ok
related-item-buttons=ok
acquisition-groups=ok
```

동일 실행에서 Product UI / Main Map / Factory / MiniMap / graceful shutdown / clean portable root도 성공했다.

### Current live Game Content release-readiness

```text
live probe run: 33151060959 — SUCCESS
Regular: items=5312 quests=517 objectives=1457 questItems=305 hideout=26 ammo=200 validationIssues=0 fatal=0
PvE:     items=5312 quests=514 objectives=1434 questItems=293 hideout=26 ammo=200 validationIssues=0 fatal=0
```

각 mode의 `sourceWarnings=1`은 현재 Tarkov Wiki Ballistics coverage warning이며 canonical validation failure가 아니다.

### 유지되는 계약

- Scanner recognition identity proof
- OCR threshold / matcher / candidate cap / visual recovery acceptance
- Needed quantity authority = `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`
- Needed source authority = `ItemsWorkspace.Plan.NeededItems[itemId].Sources`
- Game Content candidate/LKG separation
- relationship reference/price/count/limit integrity
- normal 및 v8 relationship 50% completeness retained-floor
- critical relationship collection 전면 empty fail-closed
- candidate read-back / activation / active recovery revalidation
- v3~v7 relationship-null legacy compatibility
- Map/MiniMap donor revision 및 ownership boundary

## 4. 아키텍처

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
- **Infrastructure** — HTTP/source parsing, SQLite/file persistence, content/update I/O, relationship import/validation/snapshot
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

## 5. Schema / 사용자 데이터

```text
Desktop target version: 1.8.4
Current public stable executable: 1.8.4
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
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
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
%LocalAppData%/JunhyunHelper/logs/
```

원칙:

- portable executable 옆에 mutable user data/log 생성 금지
- Program Update가 user.db, content/image cache, Map/MiniMap/Ammo/Scanner settings, Scanner logs/diagnostics/Ground Truth를 교체하지 않음
- user-reviewed Scanner Ground Truth는 자동 삭제하지 않음
- 정상 Scanner monitoring은 durable automatic correction Case를 생성하지 않음

## 6. Game Content / Scanner catalog

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

## 7. Scanner — 현재 제품 계약

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

## 8. Map / MiniMap 기준선

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

`Legacy` 이름이 붙은 Map/MiniMap bridge는 active integration이므로 이름만 보고 dead code로 삭제하지 않는다.

## 9. Active technical debt / 유지 판단

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

## 10. 릴리즈 / 유지보수 검증 계약

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

## 11. 현재 유지되는 핵심 결정

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

## 12. 다음 작업

현재 **v1.8.4 릴리즈 배치에는 남은 제품 개발 작업이 없다.**

기본 다음 작업은 다음 중 실제 evidence가 생길 때만 시작한다.

- 실사용 오류
- Tarkov 데이터/schema 변화
- Program Update/배포 회귀
- Scanner reviewed Ground Truth가 입증한 회귀
- v1.8.x item relationship source/schema/completeness 또는 presentation 회귀
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

## 13. 다음 세션 복구 순서

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
