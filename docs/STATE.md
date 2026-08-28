# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 현재 GitHub 상태가 프로젝트의 기준입니다.

기준일: 2026-08-29 KST  
상태: **v1.10.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다. 현재 확정 요구사항 범위의 제품과 Scanner는 완성 상태이며 기본 운영 모드는 유지보수다.

주요 기능은 GameMode별 Profile/User Progress, Quest/Hideout, Needed Items/Inventory, Items, Ammo, Map+MiniMap, Game Content 안전 업데이트, 사용자 동의형 Program Update, Scanner+Mini Scanner, Ground Truth/diagnostics, Scanner 아이템 정보 DB, Favorites/Recents다. Runtime GPT/AI 의존성은 없다.

기존 `Propeex/Tarkov-Helper`는 제품 사양의 권위가 아니다. Map/MiniMap에 한해 검증된 pinned donor source를 제한적으로 사용한다.

## 2. 현재 public stable

```text
version: v1.10.0
exact product release source/tag target:
a99540c4ae450f9f1995e5378919ae57f41ba930
main CI run: 33201929209 — SUCCESS
release workflow run: 33202187186 — SUCCESS
release id: 378705187
439 passed / 0 failed / 0 skipped
published UTC: 2026-08-28T19:04:46Z
```

Main-CI published ProductVersion:

```text
1.10.0+a99540c4ae450f9f1995e5378919ae57f41ba930
```

Main-CI release package:

```text
Junhyun-Helper.zip
bytes: 80,543,064
SHA-256:
65dd990e3c8b1c6faa7122ab1d809fae260c88cd10022eb7399ca6a2a3717639
```

Main-CI GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9698177979
artifact archive bytes: 241,564,056
artifact archive SHA-256:
72f42c6b507105ae5fb1dd20c597996d906a47a50c149a9ad3d197178e52d0c6
```

Public assets:

```text
Junhyun-Helper.zip
asset id: 534229631
bytes: 80,543,064
SHA-256:
65dd990e3c8b1c6faa7122ab1d809fae260c88cd10022eb7399ca6a2a3717639

SHA256SUMS.txt
asset id: 534229630
bytes: 86
SHA-256:
1c6fc4e5ecf9009d2eef3891f92748dd2d91ebdace2e4fc1f0c9876e4c00a832
```

GitHub `/releases/latest` 및 tag-ref readback:

- tag `v1.10.0`
- release target = exact product release source
- tag ref object = exact product release source
- draft = false
- prerelease = false
- latest stable = true
- ZIP + checksum assets present
- public ZIP bytes/digest = exact-main CI package bytes/SHA-256

공식 공개 증거:

- `docs/RELEASE_1.10.0.md`
- `docs/.release-v1.10.0-status.json`
- `docs/RELEASE_NOTES_V1.10.0.md`
- `docs/DECISION_V1.10.0_MINIMAP_REOPEN_MINISCANNER_FLEA_MINIMUM.md`

**중요:** 공개 뒤 생성되는 documentation-only commit은 v1.10.0 product release source가 아니다. 공개 source/tag/assets는 `a99540c4ae450f9f1995e5378919ae57f41ba930` 기준의 immutable historical product release다.

## 3. v1.10.0 변경 상태

### Main Map / MiniMap reopen synchronization

v1.9.1은 MiniMap 생성/Loaded 및 이미 열린 active-window 동기화를 보강했지만 donor `Hide()`가 loaded Window를 유지한 채 재사용하는 경로를 놓쳤다. v1.10.0은 `OverlayVisibilityChanged(true)`의 synchronous show boundary에서 visible Main Map selector를 canonical key로 다시 동기화해, A에서 B로 바꾼 직후 새로 열거나 다시 표시하는 MiniMap의 첫 visible frame이 B를 사용하도록 한다.

Exact-main published EXE runtime:

```text
main-map-selection-boundary=ok
active-minimap-map-sync=ok
reused-minimap-show-boundary=ok
rendered-minimap-map-sync=ok
```

검증은 동일 MiniMap Window에서 실제 A SVG를 렌더한 뒤 hide → visible Main Map selector B → same Window show → actual `MapSvg.Source`가 B로 변경됐는지 확인한 경우에만 성공 marker를 기록한다.

Factory 층 선택, marker/filter 의미, viewport 의미는 변경하지 않았다.

### Mini Scanner 플리마켓 최저가

- Scanner full-item catalog의 `lastLowPrice`를 `FleaMinimumPrice` presentation metadata로 저장한다.
- Item ID가 확정된 뒤에만 presentation join으로 Mini Scanner에 전달한다.
- Mini Scanner에 `플리마켓 최저가` 행을 추가했다.
- 설정에서 다른 정보 행과 동일하게 표시/숨김 및 순서 변경을 지원한다.
- 기존 v6 사용자 순서를 보존하고 새 field를 정확히 한 번 append한다.
- Scanner display settings schema: v7.
- Scanner catalog cache: v1~v4 readable / v4 written.
- 기존 v1~v3 cache는 오프라인 인식에 사용할 수 있으나 새 market field를 받기 위해 stale로 취급한다.
- scan-time network I/O는 추가하지 않았다.
- flea minimum price는 Item ID proof, OCR/matcher acceptance, candidate ranking에 사용하지 않는다.

## 4. Scanner / Game Content 유지 계약

v1.10.0은 다음을 변경하지 않았다.

- Scanner OCR threshold
- matcher / candidate cap
- visual corroboration / recovery acceptance
- capture geometry / Ground Truth
- Scanner Item ID identity policy
- Game Content schema / LKG / 50% completeness / fail-closed
- Scanner Favorites / Recents 의미
- canonical item-open boundary
- Ammo filtering / favorite persistence
- Factory floor / Map marker 의미

Scanner 표시 authority:

```text
needed quantity = ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
needed source   = ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

Price/needed/source/relationship metadata는 recognition identity proof에 사용하지 않는다.

Game Content schema는 v8이며 v3~v8 read compatibility를 유지한다. v1.10.0은 external Game Content importer/schema/validator 의미를 변경하지 않았다.

## 5. Schema / compatibility

```text
Desktop target version: 1.10.0
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v7
Scanner catalog cache: v1~v4 readable, v4 written
Scanner item UI state: scanner-item-ui-state.json / canonical Item ID persistence
```

## 6. 아키텍처 / ownership

```text
JunhyunHelper.Desktop
  ├─ JunhyunHelper.Application
  ├─ JunhyunHelper.Infrastructure
  ├─ JunhyunHelper.Core
  └─ pinned Tarkov-Helper Map/MiniMap donor source
```

- Core: canonical domain과 deterministic calculation/policy.
- Application: 사용자 use case와 authoritative mutation/workspace orchestration.
- Infrastructure: HTTP/source parsing, persistence, content/update I/O, relationship import/validation.
- Desktop: WPF UI, Scanner capture/OCR/runtime/diagnostics, Map bridge.
- Map/MiniMap donor: 제한적 compile-link 예외. donor updater/content ownership은 사용하지 않는다.

Pinned donor:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

## 7. 검증 원칙

```text
실사용 오류 / Tarkov 변화 / reviewed Scanner evidence
→ actual source/log/runtime 확인
→ failure stage/영향 범위 분류
→ 최소 수정
→ deterministic regression
→ published executable runtime smoke
→ 외부 schema/meaning 변경 시 current Regular/PvE live probe
→ exact-main release gate
```

사용자-visible WPF 변경은 source assertion만으로 완료 선언하지 않는다. 실제 published executable control tree/runtime evidence를 확보한다. v1.10.0에서는 MiniMap success marker를 actual same-window rendered A→B transition 뒤에만 기록하도록 강화했다.

## 8. 다음 작업

v1.10.0 릴리즈 배치에 남은 제품 작업은 없다. 기본 운영 모드는 유지보수다. 새 기능은 사용자가 명시적으로 제품 요구사항으로 결정할 때만 시작한다.

다음 세션은 `README.md` → `docs/CURRENT_STATE.md` → `docs/STATE.md` → `docs/PRODUCT.md` → `docs/DECISIONS.md` → 관련 전문 문서 → current GitHub state 순으로 복구한다.
