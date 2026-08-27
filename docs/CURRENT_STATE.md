# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md`, `docs/PRODUCT.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPER_REFERENCE.md`, 전문 결정/릴리즈 문서를 참조합니다.

기준일: 2026-08-28 KST

상태: **`v1.7.15 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable

현재 공개 stable/latest는 **v1.7.15**다.

```text
public stable/latest: v1.7.15
exact product release source/tag target: 4bf5e3a567d3ce9563657bbb3b90bec0871c06b4
main CI run: 33086901217 — SUCCESS
release workflow run: 33087185178 — SUCCESS
release id: 377926863
stable asset: Junhyun-Helper.zip
stable asset id: 532481010
stable bytes: 80,492,565
stable SHA-256: 9ac3276a1a4a20905b0aa3d6452f50d5259f724ed8f960b7cfbad39f8c619f2f
checksum asset id: 532481008
checksum asset SHA-256: 84fbabe5ef2c41d28a00305c0cd7b8ee7575fbe3c1c64fa83f7ead1c75494580
410 passed / 0 failed / 0 skipped
Product UI / Main Map / Factory / MiniMap / graceful shutdown smoke: SUCCESS
```

GitHub `/releases/latest` 및 `refs/tags/v1.7.15` readback:

- tag `v1.7.15`
- release target/tag ref = exact product release source
- draft = false
- prerelease = false
- latest stable = true
- `Junhyun-Helper.zip` + `SHA256SUMS.txt` present
- public ZIP digest = exact main-CI package SHA-256

공개 증거:

- `docs/RELEASE_1.7.15.md`
- `docs/.release-v1.7.15-status.json`
- `docs/RELEASE_NOTES_V1.7.15.md`
- `docs/DECISION_V1.7.15_UI_REFINEMENTS.md`

이 상태 문서 동기화 이후의 documentation-only commit은 **v1.7.15 product release source가 아니다**. 제품 릴리즈 소스는 항상 위 `4bf5e3a...`로 고정한다. 이미 공개된 v1.7.15 tag/source/assets는 immutable historical product release로 취급한다.

## Schema / compatibility

```text
Desktop target version: 1.7.15
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
Scanner Ground Truth: explicit user-reviewed durable cases
```

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

## v1.7.15 — UI refinement patch

- main header는 version-only presentation을 사용한다.
- cleanup 대상이 있으면 Items 탭 우측 상단에 작은 orange dot을 표시한다.
- Map marker checkbox list는 panel의 실제 available height를 사용한다.
- marker list가 공간 안에 들어오면 scrollbar를 숨기고, 실제로 넘칠 때만 scrolling한다.
- Map marker selector는 launcher 재클릭과 panel outside click으로 닫힌다.
- outside dismiss click은 marker state를 변경하지 않고 가능한 한 원래 Map/control interaction을 유지한다.
- Ammo `즐겨찾기 선택`은 standard dropdown이다.
- caliber/Favorites dropdown은 같은 caliber별 animation state를 공유하며 해당 caliber에 실제 속한 ammo icon만 순환한다.
- 특정 ammo 하나를 caliber 영구 대표 icon으로 고정하지 않는다.
- icon cadence는 1.4초이며 dropdown 둘 다 닫히면 timer를 중지한다.

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
- Item ID 확정 전 price/needed/slot/source/previous-frame metadata를 identity evidence로 사용하지 않음
- scan 순간 network identity work 없음
- reviewed Ground Truth 없이 recognition threshold/candidate cap/matcher/visual acceptance 완화 금지
- needed quantity authority = `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`
- needed source authority = `ItemsWorkspace.Plan.NeededItems[itemId].Sources`

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

현재 **v1.7.15 릴리즈 배치에 남은 제품 개발 작업은 없다.** 이후 기본 모드는 유지보수다.
