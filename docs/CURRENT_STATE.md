# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md`, `docs/PRODUCT.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPER_REFERENCE.md`, 전문 결정/릴리즈 문서를 참조합니다.

기준일: 2026-08-28 KST

상태: **`v1.9.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable

현재 공개 stable/latest는 **v1.9.0**이다.

```text
public stable/latest: v1.9.0
exact product release source/tag target: e0b0d303141563af564cd71cf00d8c1bfeafe44d
main CI run: 33165706386 — SUCCESS
release workflow run: 33165905504 — SUCCESS
release id: 378431058
stable asset: Junhyun-Helper.zip
stable asset id: 533681571
stable bytes: 80,538,029
stable SHA-256: 9ee63042746aee27ddff4407e8240d65b3740696576fe7514b4f92fe8f1e1d44
checksum asset id: 533681572
checksum asset SHA-256: 2cd7157b4ebeaaa86fa73ee1eccbd1dedac8112089ad04994bd04228fcdcce32
432 passed / 0 failed / 0 skipped
Product UI / Ammo 700 ms runtime / Map extract filters / Scanner Favorites+Recents / Main Map / Factory / MiniMap / graceful shutdown: SUCCESS
```

Main-CI ProductVersion:

```text
1.9.0+e0b0d303141563af564cd71cf00d8c1bfeafe44d
```

Main-CI Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9683545225
archive bytes: 241,545,444
archive SHA-256: 098c74a99dc6d57c7a01b0e70c860c0d2925e6bbf4835ac2eacabf1f3e5d1bd8
```

GitHub `/releases/latest` 및 `refs/tags/v1.9.0` readback:

- release target/tag ref = exact product release source `e0b0d303...`
- draft = false
- prerelease = false
- latest stable = true
- `Junhyun-Helper.zip` + `SHA256SUMS.txt` present
- public ZIP bytes/digest = exact main-CI package bytes/SHA-256

공개 증거:

- `docs/RELEASE_1.9.0.md`
- `docs/.release-v1.9.0-status.json`
- `docs/RELEASE_NOTES_V1.9.0.md`
- `docs/DECISION_V1.9.0_SCANNER_FAVORITES_RECENTS_AND_UI_FIXES.md`
- `docs/RELEASE_1.8.4.md` — 이전 Ammo toolbar / Scanner item-detail 릴리즈
- `docs/RELEASE_1.8.2.md` — runtime UI/live Game Content 회귀 수정
- `docs/RELEASE_1.8.1.md` — relationship completeness hardening
- `docs/RELEASE_1.8.0.md` — Scanner 아이템 정보 DB

이 상태 문서 동기화 이후의 documentation-only commit은 **v1.9.0 product release source가 아니다**. 제품 릴리즈 source/tag/assets는 위 `e0b0d303...`로 고정하며 이미 공개된 release는 immutable historical product artifact로 취급한다.

## Schema / compatibility

```text
Desktop target version: 1.9.0
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
Scanner item UI state: scanner-item-ui-state.json / canonical Item ID persistence
Scanner Ground Truth: explicit user-reviewed durable cases
```

사용자 mutable data는 `%LocalAppData%/JunhyunHelper`에 둔다. Program Update는 `user.db`, content/image cache, Map/MiniMap/Ammo/Scanner 설정, Scanner item UI state, Scanner logs/diagnostics/Ground Truth를 덮어쓰지 않는다.

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 |
| Items | 구현 완료 |
| Ammo | 구현 완료 / shared 700 ms rendered-icon runtime smoke 유지 |
| Map + MiniMap | 구현 완료 / real extract-filter + stable runtime smoke 유지 |
| Game Content Update | 구현 완료 / relationship LKG + live contract 유지 |
| Program Update | 구현 완료 / verified stable ZIP contract |
| Scanner + Mini Scanner | **FEATURE COMPLETE / MAINTENANCE ONLY** |
| Scanner 아이템 정보 DB | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner Favorites / Recents | **IMPLEMENTED / PUBLIC STABLE** |

## v1.9.0 — Scanner Favorites / Recents + UI 회귀 수정

Scanner:

- 상세 상단 별 버튼으로 Favorites 등록/해제.
- 오른쪽 사용자 UI는 Favorites 약 2/3 + Recents 약 1/3.
- 두 목록은 독립 vertical scroll, horizontal scroll 없음, 긴 이름 ellipsis.
- Favorites/Recents는 canonical Item ID와 order만 저장하고 current GameMode catalog에서 presentation을 다시 resolve.
- Recents는 실제 상세 open 시에만 newest-first로 기록, deduplicate/reopen-top, 최대 50개.
- 개별 recent 삭제 / 전체 삭제는 Favorites와 독립.
- 검색어를 지워도 열린 상세 유지.
- direct search / recipe-barther relation / Favorites / Recents가 하나의 canonical item-open boundary를 사용.
- visible Scanner에서 PvP/PvE profile 전환 시 current-mode catalog로 목록/상세 재해석; 자동 재렌더는 recent 순서를 변경하지 않음.
- 기존 사용자용 Scanner 로그 영역은 숨기고 internal diagnostics/correction pipeline은 유지.

Map:

- donor의 실제 탈출구 master / PMC / SCAV / Transit checkbox를 marker 선택 패널에 복원.
- 기존 donor handler, settings persistence, marker render, MiniMap refresh 의미 유지.

Ammo:

- 구경/즐겨찾기 ComboBox의 shared runtime icon template/state 유지.
- shared icon cycle = **700 ms**.

Exact-main published executable evidence:

```text
Ammo:
product-lifecycle=ok
ammo-caliber-runtime-template=ok
favorites-shared-template=ok
rendered-caliber-image=ok
rendered-favorite-image=ok
shared-timer-cycle=ok
shared-cycle-ms=700

Map:
real-donor-checkboxes=ok
marker-panel-visible=ok
master-filter-render-state=ok
minimap-refresh-handler-preserved=ok

Scanner detail:
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

## Game Content / Scanner recognition 기준선

v1.9.0은 external Game Content importer/schema/validator 의미와 Scanner recognition acceptance를 변경하지 않았다. 따라서 새 network live probe는 필요하지 않았다.

마지막 schema-affecting release-readiness evidence:

```text
live probe run: 33151060959 — SUCCESS
Regular: items=5312 quests=517 objectives=1457 questItems=305 hideout=26 ammo=200 fatal=0
PvE:     items=5312 quests=514 objectives=1434 questItems=293 hideout=26 ammo=200 fatal=0
```

기존 안전 계약은 유지한다.

- failed candidate는 last-known-good를 덮어쓰지 않음
- normal snapshot + v8 relationship retained-floor = healthy baseline의 50%
- critical v8+ relationship collection 전면 empty = fail closed
- candidate read-back / activation / active recovery 관계 재검증
- v3~v7 relationship-null legacy compatibility
- audited Bitcoin passive production만 ordinary craft relationship에서 제외
- canonical-identical trader direct-purchase만 deduplicate
- Scanner scan/search 순간 identity/relationship network I/O 없음

Scanner recognition 기준:

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

- false positive보다 miss 선호
- current official Korean full-item catalog가 identity authority
- stale/cross-frame evidence 금지
- Item ID 확정 전 price/needed/source/relationship metadata를 identity evidence로 사용하지 않음
- reviewed evidence 없이 recognition threshold/candidate cap/matcher/visual acceptance 완화 금지

Scanner 표시 authority:

```text
needed quantity = ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
needed source   = ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

## Map / MiniMap 기준선

Pinned donor:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

Map/MiniMap은 검증된 donor source만 제한적으로 사용하며 JunhyunHelper 제품 의미는 first-party bridge/customization 경계가 소유한다. `Legacy` 이름이 붙은 active bridge를 이름만 보고 삭제하지 않는다.

## 유지보수 / 검증 원칙

```text
실사용 오류 / Tarkov 변화 / reviewed Scanner evidence
→ 실제 source/log/runtime state 확인
→ failure stage와 영향 범위 분류
→ 최소 수정
→ deterministic regression
→ published executable runtime smoke
→ 외부 schema/meaning 변경 시 current Regular/PvE live probe
→ exact-main release gate
```

사용자-visible WPF 변경은 source assertion만으로 완료 선언하지 않는다. lifecycle/runtime control을 건드린 경우 actual published executable의 control tree/runtime evidence를 확보한다.

새 기능은 사용자가 제품 요구사항으로 명시적으로 결정할 때만 시작한다.

## 다음 세션 복구 순서

1. `AGENTS.md`
2. `README.md`
3. `docs/CURRENT_STATE.md`
4. `docs/STATE.md`
5. `docs/PRODUCT.md`
6. `docs/DECISIONS.md`
7. `docs/MAINTENANCE_CONTRACTS.md`
8. `docs/DEVELOPER_REFERENCE.md`
9. 작업 영역 전문 문서
10. current code / current PR / current CI

현재 **v1.9.0 릴리즈 배치에 남은 제품 개발 작업은 없다.** 기본 운영 모드는 유지보수다.
