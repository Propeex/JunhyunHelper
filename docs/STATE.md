# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 현재 GitHub 상태가 프로젝트의 기준입니다.

기준일: 2026-08-29 KST  
상태: **v1.9.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램이다. 현재 확정 요구사항 범위의 제품과 Scanner는 완성 상태이며 기본 운영 모드는 유지보수다.

주요 기능은 GameMode별 Profile/User Progress, Quest/Hideout, Needed Items/Inventory, Items, Ammo, Map+MiniMap, Game Content 안전 업데이트, 사용자 동의형 Program Update, Scanner+Mini Scanner, Ground Truth/diagnostics, Scanner 아이템 정보 DB, Favorites/Recents다. Runtime GPT/AI 의존성은 없다.

기존 `Propeex/Tarkov-Helper`는 제품 사양의 권위가 아니다. Map/MiniMap에 한해 검증된 pinned donor source를 제한적으로 사용한다.

## 2. 현재 public stable

```text
version: v1.9.1
exact product release source/tag target:
723760910ff250a515ed8db456d3f045656ecacb
main CI run: 33184811972 — SUCCESS
release workflow run: 33185056113 — SUCCESS
release id: 378579142
435 passed / 0 failed / 0 skipped
published UTC: 2026-08-28T15:26:04Z
```

Main-CI published ProductVersion:

```text
1.9.1+723760910ff250a515ed8db456d3f045656ecacb
```

Main-CI release package:

```text
Junhyun-Helper.zip
bytes: 80,540,488
SHA-256:
7a282f58d6cf2e4916c55daddf828a70643b35669bc71fbeaca1e7a4e8176f54
```

Main-CI GitHub Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9691310332
artifact archive bytes: 241,554,536
artifact archive SHA-256:
e4ac36ef6968f10b8a5b03c1f8e73a95e308e96f19b65d40d7144c87bcee51b7
```

Public assets:

```text
Junhyun-Helper.zip
asset id: 533982952
bytes: 80,540,488
SHA-256:
7a282f58d6cf2e4916c55daddf828a70643b35669bc71fbeaca1e7a4e8176f54

SHA256SUMS.txt
asset id: 533982951
bytes: 86
SHA-256:
1a98310d28f954c36f400a69f9b6c546bc22137ebbef95bb52991bfff02de431
```

GitHub `/releases/latest` 및 tag-ref readback:

- tag `v1.9.1`
- release target = exact product release source
- tag ref object = exact product release source
- draft = false
- prerelease = false
- latest stable = true
- ZIP + checksum assets present
- public ZIP bytes/digest = exact-main CI package bytes/SHA-256

공식 공개 증거:

- `docs/RELEASE_1.9.1.md`
- `docs/.release-v1.9.1-status.json`
- `docs/RELEASE_NOTES_V1.9.1.md`
- `docs/DECISION_V1.9.1_FINAL_UI_MINIMAP.md`

**중요:** 공개 뒤 생성되는 documentation-only commit은 v1.9.1 product release source가 아니다. 공개 source/tag/assets는 `723760910ff250a515ed8db456d3f045656ecacb` 기준의 immutable historical product release다.

## 3. v1.9.1 변경 상태

### Scanner favorite/Wiki action

- 높이 34px.
- 별은 `Segoe UI Symbol`, zero padding, horizontal/vertical center.
- 실제 detail이 visible해진 뒤 Render priority에서 검증한다.
- favorite persistence/canonical Item ID 의미는 유지한다.

Runtime:

```text
favorite-wiki-height=34
favorite-symbol-font=ok
favorite-content-centered=ok
wiki-content-centered=ok
```

### Map extract filters

사용자-visible `탈출구` 그룹은 donor의 실제 checkbox 세 개만 사용한다.

```text
PMC 탈출구
Scav 탈출구
트랜짓 탈출구
```

visible master/duplicate checkbox를 만들지 않는다. donor master `ChkShowExtractMarkers`는 hidden internal render gate로 유지한다. donor handler/settings persistence/marker rendering/MiniMap refresh 의미를 보존한다.

Runtime:

```text
real-donor-checkboxes=ok
marker-panel-visible=ok
master-filter-render-state=ok
hidden-master-render-gate=ok
approved-three-filter-layout=ok
minimap-refresh-handler-preserved=ok
pmc-filter-render-state=ok
scav-filter-render-state=ok
transit-filter-render-state=ok
```

### Main Map / MiniMap sync

visible Main Map selection을 canonical map key로 해석해 `MapTrackerService`에 반영한 뒤 MiniMap registration을 진행한다. donor Loaded 뒤에도 동일 경계를 적용하며 이미 열린 MiniMap에도 map 변경을 즉시 전달한다.

Runtime:

```text
main-map-selection-boundary=ok
active-minimap-map-sync=ok
```

## 4. Scanner / Game Content 유지 계약

v1.9.1은 다음을 변경하지 않았다.

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

Game Content schema는 v8이며 v3~v8 read compatibility를 유지한다. v1.9.1은 external importer/schema/validator 의미를 변경하지 않아 새 network live probe를 release blocker로 요구하지 않았다. 마지막 schema-affecting probe run `33151060959`에서 Regular/PvE fatal은 0/0이었다.

## 5. 아키텍처 / ownership

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

## 6. 검증 원칙

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

사용자-visible WPF 변경은 source assertion만으로 완료 선언하지 않는다. 실제 published executable control tree/runtime evidence를 확보한다. v1.9.1에서는 Scanner detail render와 MiniMap map sync marker를 required fail-closed evidence로 강화했다.

## 7. 다음 작업

v1.9.1 릴리즈 배치에 남은 제품 작업은 없다. 기본 운영 모드는 유지보수다. 새 기능은 사용자가 명시적으로 제품 요구사항으로 결정할 때만 시작한다.

다음 세션은 `README.md` → `docs/CURRENT_STATE.md` → `docs/STATE.md` → `docs/PRODUCT.md` → `docs/DECISIONS.md` → 관련 전문 문서 → current GitHub state 순으로 복구한다.
