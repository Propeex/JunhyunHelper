# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md`, `docs/PRODUCT.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPER_REFERENCE.md`, 전문 결정/릴리즈 문서를 참조합니다.

기준일: 2026-08-28 KST

상태: **`v1.8.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable

현재 공개 stable/latest는 **v1.8.0**이다.

```text
public stable/latest: v1.8.0
exact product release source/tag target: 8042e4612a54a6ec395a69d1be0700d844a1b210
main CI run: 33130057533 — SUCCESS
release workflow run: 33130212711 — SUCCESS
release id: 378197672
stable asset: Junhyun-Helper.zip
stable asset id: 533051783
stable bytes: 80,520,114
stable SHA-256: 4ecaf65068153a38a7a8613cfe2ae673aec191563f999f1cfbd10cb93d9437e0
checksum asset id: 533051782
checksum asset SHA-256: 6432c08261b1ca6dd093ff9e1864619951162300585d5cb2db082731bff3d3a1
413 passed / 0 failed / 0 skipped
Product UI / Main Map / Factory / MiniMap / graceful shutdown smoke: SUCCESS
```

GitHub `/releases/latest` 및 `refs/tags/v1.8.0` readback:

- tag `v1.8.0`
- release target/tag ref = exact product release source
- draft = false
- prerelease = false
- latest stable = true
- `Junhyun-Helper.zip` + `SHA256SUMS.txt` present
- public ZIP digest = exact main-CI package SHA-256

공개 증거:

- `docs/RELEASE_1.8.0.md`
- `docs/.release-v1.8.0-status.json`
- `docs/RELEASE_NOTES_V1.8.0.md`
- `docs/DECISION_V1.8.0_SCANNER_ITEM_DATABASE.md`

이 상태 문서 동기화 이후의 documentation-only commit은 **v1.8.0 product release source가 아니다**. 제품 릴리즈 소스는 항상 위 `8042e461...`로 고정한다. 이미 공개된 v1.8.0 tag/source/assets는 immutable historical product release로 취급한다.

## Schema / compatibility

```text
Desktop target version: 1.8.0
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
Scanner Ground Truth: explicit user-reviewed durable cases
```

v8 Content snapshot은 Scanner 아이템 정보 DB용 canonical trader purchase / barter / craft / flea relationship graph를 저장한다. v3~v7 snapshot은 계속 읽을 수 있으며, 관계 데이터가 없는 구형 snapshot과 실제 관계가 없는 아이템을 구분한다.

사용자 mutable data는 `%LocalAppData%/JunhyunHelper`에 둔다. Program Update는 user.db, content/image cache, Map/MiniMap/Ammo/Scanner 설정, Scanner logs/diagnostics/Ground Truth를 덮어쓰지 않는다.

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 |
| Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / stable smoke 유지 |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / verified stable ZIP contract |
| Scanner + Mini Scanner | **FEATURE COMPLETE / MAINTENANCE ONLY** |
| Scanner 아이템 정보 DB | **IMPLEMENTED / PUBLIC STABLE** |

## v1.8.0 — Scanner 아이템 정보 DB

Scanner 탭의 item search는 Item ID 기준의 로컬 관계 DB/detail view다.

선택 아이템에서 확인 가능한 정보:

- 기본 정보: 종류, 크기, 무게, 플리마켓 거래 가능 여부, 기본 가격
- 기존 표시 정보: 아이콘/공식 이름, flea 평균가, 최고 상인 판매가, 현재 필요 개수
- 퀘스트 사용처: 퀘스트명, 요구 수량, FIR
- 은신처 업그레이드 사용처: 시설, 목표 레벨, 요구 수량, FIR
- 제작 재료 사용처: 시설/레벨, 결과 아이템/수량, 전체 재료/도구
- 상인 교환 재료 사용처: 상인/LL, 결과 아이템/수량, 전체 재료
- 수급처: 상인 현금 구매, 상인 교환, 은신처 제작, 플리마켓, canonical 수급 관계가 없을 때 레이드 획득
- 상인 구매 가격/화폐/LL/구매 제한/upstream 재고 갱신 시각
- 제작 시간, 결과 수량, 비소모 도구
- 관련 아이템 클릭 시 같은 Scanner 상세 이동
- Quest/Hideout 사용처 클릭 시 기존 제품 화면 이동

관계 데이터 authority:

```text
normal Game Content update
→ Items / Barters / Crafts / Traders / Tasks / Hideout parse
→ canonical relationship graph
→ integrity/completeness validation
→ v8 snapshot activation
→ Scanner search presentation
```

Scanner 검색 중 관계 정보 때문에 별도 network I/O를 시작하지 않는다.

기존 `필요 개수` / `필요한 곳` authority는 그대로다.

```text
needed quantity = ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
needed source   = ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

## Scanner 현재 기준선

Scanner는 현재 **FEATURE COMPLETE / MAINTENANCE ONLY**다.

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

- false positive보다 miss 선호
- current official Korean Tarkov full-item catalog가 identity authority
- geometry/environment normalization은 identity proof가 아님
- stale/cross-frame OCR 또는 visual result를 current Item identity proof로 사용하지 않음
- Item ID 확정 전 price/needed/slot/source/relationship/previous-frame metadata를 identity evidence로 사용하지 않음
- scan 순간 network identity work 없음
- reviewed Ground Truth 없이 recognition threshold/candidate cap/matcher/visual acceptance 완화 금지

v1.8.0 아이템 DB는 **Item ID 확정 이후 presentation**에만 참여하며 recognition acceptance를 바꾸지 않는다.

## Map / MiniMap 기준선

Pinned donor:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

기존 `Propeex/Tarkov-Helper`를 제품 요구사항 권위로 사용하지 않는다. 검증된 donor Map/MiniMap source만 pinned compile-link 예외로 사용하고, JunhyunHelper 제품 변경은 first-party bridge/customization boundary에서 적용한다.

`Legacy` 이름이 붙은 Map/MiniMap bridge는 현재 active integration이므로 이름만 보고 dead code로 삭제하지 않는다.

## Game Content 기준선

```text
remote source
→ download / parse
→ canonical build
→ integrity/completeness validation
→ activate
```

- 실패 candidate는 last-known-good를 덮어쓰지 않음
- normal snapshot shrink guard = healthy baseline의 50%
- collection schema drift는 fail closed
- Wiki Ballistics enrichment는 fail-soft
- User Progress와 Game Content authority 분리
- v8 item relationship reference / price / count 무결성도 active 교체 전에 검증

## 유지보수 원칙

```text
실사용 오류 / Tarkov 변화 / reviewed Scanner evidence
→ 실제 source/log/runtime state 확인
→ failure stage와 영향 범위 분류
→ 최소한의 일관된 수정
→ deterministic regression
→ full Windows release gate
→ 필요 시 PATCH release
```

새 기능은 사용자가 제품 요구사항으로 명시적으로 결정할 때만 시작한다. Scanner threshold/candidate/matcher/visual policy는 reviewed evidence 없이 선제적으로 조정하지 않는다.

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

현재 **v1.8.0 릴리즈 배치에 남은 제품 개발 작업은 없다.** 이후 기본 모드는 유지보수다.
