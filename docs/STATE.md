# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 현재 GitHub 상태가 프로젝트의 기준입니다.

기준일: 2026-08-28 KST  
상태: **v1.7.15 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

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

Runtime GPT/AI 의존성은 없다.

기존 `Propeex/Tarkov-Helper`는 불완전한 프로토타입이며 새 제품 요구사항의 권위가 아니다. Map/MiniMap은 사용자가 검증한 pinned donor source만 제한적으로 사용한다.

## 2. 현재 public stable

```text
version: v1.7.15
exact product release source/tag target:
4bf5e3a567d3ce9563657bbb3b90bec0871c06b4
main CI run: 33086901217 — SUCCESS
release workflow run: 33087185178 — SUCCESS
release id: 377926863
410 passed / 0 failed / 0 skipped
published UTC: 2026-08-27T15:19:55Z
```

Main-CI published ProductVersion:

```text
1.7.15+4bf5e3a567d3ce9563657bbb3b90bec0871c06b4
```

Main-CI release package:

```text
Junhyun-Helper.zip
bytes: 80,492,565
SHA-256:
9ac3276a1a4a20905b0aa3d6452f50d5259f724ed8f960b7cfbad39f8c619f2f
```

Main-CI GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9652666398
artifact archive bytes: 241,410,930
artifact archive SHA-256:
cf3802eb6cba359e46eaa55bf48cc89bed33bf7902129e17f31b48065cf94e04
```

Public assets:

```text
Junhyun-Helper.zip
asset id: 532481010
bytes: 80,492,565
SHA-256:
9ac3276a1a4a20905b0aa3d6452f50d5259f724ed8f960b7cfbad39f8c619f2f

SHA256SUMS.txt
asset id: 532481008
bytes: 86
SHA-256:
84fbabe5ef2c41d28a00305c0cd7b8ee7575fbe3c1c64fa83f7ead1c75494580
```

GitHub `/releases/latest` 및 tag-ref readback:

- tag `v1.7.15`
- release target = exact product release source
- tag ref object = exact product release source
- draft = false
- prerelease = false
- latest stable = true
- ZIP + checksum assets present
- public `Junhyun-Helper.zip` digest = exact main-CI package SHA-256

공식 공개 증거:

- `docs/RELEASE_1.7.15.md`
- `docs/.release-v1.7.15-status.json`
- `docs/RELEASE_NOTES_V1.7.15.md`
- `docs/DECISION_V1.7.15_UI_REFINEMENTS.md`

**중요:** 이 문서 갱신처럼 공개 이후 생성되는 documentation-only commit은 v1.7.15 product release source가 아니다. 공개 v1.7.15 source/tag/assets는 위 `4bf5e3a...` 기준으로 immutable historical product release다.

## 3. v1.7.15 사용자-facing 변경

### Main header / cleanup attention

- main header의 version/status 영역은 사용자에게 **version만 표시**한다.
- `정리 필요` 문자열은 표시하지 않는다.
- 현재 `ItemsWorkspace.Plan.CleanupItems.Count > 0`이면 Items 탭 우측 상단에 작은 orange dot을 표시한다.
- cleanup 대상이 없어지면 indicator도 즉시 사라진다.
- Game Content update progress는 기존 전용 progress overlay가 담당하며 version 영역에 진행 문구를 노출하지 않는다.

### Map marker selector

- 바깥 panel뿐 아니라 내부 checkbox list viewport도 현재 Map viewport의 available height를 사용한다.
- marker content가 available height 안에 들어오면 vertical scrollbar는 hidden이다.
- 실제로 content가 넘칠 때만 scrolling을 허용한다.
- 기존 `지도 마커` launcher 재클릭 toggle을 유지한다.
- panel outside click으로도 닫힌다.
- outside-click dismiss는 marker enable/check state를 변경하지 않는다.
- dismiss click은 handled로 소비하지 않아 가능한 한 원래 Map/control interaction이 이어지게 한다.

### Ammo caliber / Favorites

- `즐겨찾기 선택`은 standard ComboBox dropdown을 사용한다.
- 기존 Ammo favorites persistence와 caliber filtering authority는 그대로다.
- caliber dropdown과 Favorites dropdown은 같은 item presentation과 caliber별 animation state를 공유한다.
- 각 caliber 왼쪽 icon은 해당 `RawCaliber`에 실제 속한 `AmmoRow.Icon`만 순환한다.
- 특정 ammo 하나를 caliber의 영구 대표 icon으로 고정하지 않는다.
- cadence는 1.4초이며 두 dropdown이 모두 닫혀 있으면 timer를 중지한다.
- icon byte는 기존 `ImageCacheService`/Ammo row icon loading 결과를 재사용한다.

Regression:

```text
V1715UiRefinementsContractTests
410 passed / 0 failed / 0 skipped
actual published EXE Product UI / Main Map / Factory / MiniMap smoke
```

## 4. 아키텍처

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned Tarkov-Helper Map/MiniMap donor source
```

책임:

- **Core** — canonical domain, deterministic calculation, Quest/Needed Items/Scanner pure policy
- **Application** — 사용자 use case, authoritative mutation, workspace orchestration
- **Infrastructure** — HTTP/source parsing, SQLite/file persistence, content/update I/O
- **Desktop** — WPF UI, presentation, Scanner capture/OCR/runtime/diagnostics, Map bridge
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

개별 Page의 internal presentation initialization은 해당 Page가 직접 소유한다.

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
Desktop target version: 1.7.15
Current public stable executable: 1.7.15
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

Scanner scan 순간에는 local/memory catalog만 사용하며 identity 결정을 위해 network 요청을 시작하지 않는다.

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
- Item ID 확정 전 price/needed/slot/source/previous-frame metadata를 identity evidence로 사용하지 않음
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

## 10. 릴리즈 계약

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
→ exact-main CI
→ Release workflow exact artifact verification
→ tag/release/public asset readback
```

Published stable release는 공개 후 immutable historical product artifact로 취급한다.

`actions/download-artifact@v8`의 upstream Node `Buffer()` deprecation warning은 현재 release correctness와 무관한 monitor-only upstream warning이다.

## 11. 현재 유지되는 이전 핵심 결정

- **v1.7.14** — popup true-toggle, shared overlay, search clear consistency
- **v1.7.13** — Items purpose selector 제거, Ammo detail 기본 접힘, Map trail/hotkey copy 제거
- **v1.7.12** — Desktop lifecycle/composition ownership hardening
- **v1.7.11** — Scanner RemainingTotal, configurable hotkey compatibility, MiniMap sync/size persistence, standard ToolTip 비표시
- **v1.7.10** — cross-environment Scanner title normalization hardening

역사적 상세는 버전별 `DECISION_*` / `RELEASE_*` 문서를 사용한다.

## 12. 다음 작업

현재 **v1.7.15 릴리즈 배치에는 남은 제품 개발 작업이 없다.**

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
