# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md`, `docs/PRODUCT.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPER_REFERENCE.md`, 전문 결정/릴리즈 문서를 참조합니다.

기준일: 2026-08-28 KST

상태: **`v1.8.4 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable

현재 공개 stable/latest는 **v1.8.4**다.

```text
public stable/latest: v1.8.4
exact product release source/tag target: 13af4e3a452139dedc32b2db9aa51266e2a01d2a
main CI run: 33153043430 — SUCCESS
release workflow run: 33153234911 — SUCCESS
release id: 378333813
stable asset: Junhyun-Helper.zip
stable asset id: 533461834
stable bytes: 80,528,868
stable SHA-256: 9e06c16e20a346ad7691dccfee9a2caebcdb6c0cd9a6a35859bcb97d8e03fa42
checksum asset id: 533461832
checksum asset SHA-256: 535514fb48f23e1fe7834ba0cd5be54235f15922d036f5ad071c829ff80b4aad
424 passed / 0 failed / 0 skipped
Product UI / Ammo runtime markers / Scanner item-detail runtime / Main Map / Factory / MiniMap / graceful shutdown: SUCCESS
Regular/PvE live-data fatal validation: 0 / 0
```

Main-CI ProductVersion:

```text
1.8.4+13af4e3a452139dedc32b2db9aa51266e2a01d2a
```

GitHub `/releases/latest` 및 `refs/tags/v1.8.4` readback:

- release target/tag ref = exact product release source
- draft = false
- prerelease = false
- latest stable = true
- `Junhyun-Helper.zip` + `SHA256SUMS.txt` present
- public ZIP bytes/digest = exact main-CI package bytes/SHA-256

공개 증거:

- `docs/RELEASE_1.8.4.md`
- `docs/.release-v1.8.4-status.json`
- `docs/RELEASE_NOTES_V1.8.4.md`
- `docs/DECISION_V1.8.4_AMMO_SCANNER_ITEM_DETAIL.md`
- `docs/RELEASE_1.8.2.md` — runtime UI/live Game Content 회귀 수정
- `docs/RELEASE_1.8.1.md` — relationship completeness hardening
- `docs/RELEASE_1.8.0.md` — Scanner 아이템 정보 DB

이 상태 문서 동기화 이후의 documentation-only commit은 **v1.8.4 product release source가 아니다**. 제품 릴리즈 source/tag/assets는 위 `13af4e3...`로 고정하며 이미 공개된 release는 immutable historical product artifact로 취급한다.

## Schema / compatibility

```text
Desktop target version: 1.8.4
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
Scanner Ground Truth: explicit user-reviewed durable cases
```

사용자 mutable data는 `%LocalAppData%/JunhyunHelper`에 둔다. Program Update는 `user.db`, content/image cache, Map/MiniMap/Ammo/Scanner 설정, Scanner logs/diagnostics/Ground Truth를 덮어쓰지 않는다.

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 |
| Items | 구현 완료 |
| Ammo | 구현 완료 / published rendered-icon + toolbar runtime smoke 유지 |
| Map + MiniMap | 구현 완료 / stable runtime smoke 유지 |
| Game Content Update | 구현 완료 / relationship LKG + live contract probe 유지 |
| Program Update | 구현 완료 / verified stable ZIP contract |
| Scanner + Mini Scanner | **FEATURE COMPLETE / MAINTENANCE ONLY** |
| Scanner 아이템 정보 DB | **IMPLEMENTED / PUBLIC STABLE** |

## v1.8.4 — Ammo toolbar / Scanner item detail

v1.8.4는 새 Scanner recognition 기능이 아니라 기존 UI와 presentation을 다듬는 PATCH다.

Ammo:

- 즐겨찾기 선택을 왼쪽 선택 영역에 유지.
- `표시 열` 버튼을 툴바 오른쪽 끝에 유지.
- 구경/즐겨찾기는 기존 shared animated icon state와 timer를 유지.

Scanner item detail:

- 한 줄기 세로 흐름: 기본 정보 → 사용처 → 수급처.
- 기본 정보는 크기 / 플리 평균가 / 최고 상인 판매가 / 현재 필요한 개수 네 항목.
- Quest/Hideout 기존 navigation 유지.
- craft/barter는 result + complete materials recipe card.
- 좁은 폭에서는 material row wrapping.
- 관련 item click → 같은 Scanner item detail.
- 수급처 = 제작 / 교환 / 구매 / 레이드 획득.
- empty relation group은 표시하지 않음.

Published executable CI는 다음 marker를 직접 요구한다.

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

## Current live Game Content

공개 직전 production canonical pipeline으로 현재 `json.tarkov.dev`를 확인했다.

```text
live probe run: 33151060959 — SUCCESS
Regular: items=5312 quests=517 objectives=1457 questItems=305 hideout=26 ammo=200 fatal=0
PvE:     items=5312 quests=514 objectives=1434 questItems=293 hideout=26 ammo=200 fatal=0
```

각 mode의 warning 1건은 Tarkov Wiki Ballistics coverage warning이며 validation failure가 아니다.

기존 안전 계약은 유지한다.

- failed candidate는 last-known-good를 덮어쓰지 않음
- normal snapshot + v8 relationship retained-floor = healthy baseline의 50%
- critical v8+ relationship collection 전면 empty = fail closed
- candidate read-back / activation / active recovery 관계 재검증
- v3~v7 relationship-null legacy compatibility
- audited Bitcoin passive production만 ordinary craft relationship에서 제외
- canonical-identical trader direct-purchase만 deduplicate
- Scanner scan/search 순간 identity/relationship network I/O 없음

## Scanner 현재 기준선

Scanner는 **FEATURE COMPLETE / MAINTENANCE ONLY**다.

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
→ 필요 시 current Regular/PvE live release-readiness probe
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

현재 **v1.8.4 릴리즈 배치에 남은 제품 개발 작업은 없다.** 이후 기본 모드는 유지보수다.
