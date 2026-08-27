# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md`, `docs/PRODUCT.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPER_REFERENCE.md`, 전문 결정/릴리즈 문서를 참조합니다.

기준일: 2026-08-27

상태: **`v1.7.14 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable

현재 공개 stable/latest는 **v1.7.14**다.

```text
public stable/latest: v1.7.14
exact product release source/tag target: 0a51375de36cd13047216006c2c0311728b1bd89
main CI run: 33060827905 — SUCCESS
release workflow run: 33061059154 — SUCCESS
release id: 377720327
stable asset: Junhyun-Helper.zip
stable asset id: 532104142
stable bytes: 80,488,363
stable SHA-256: 341ac502d2ace563ab2e7c8d7091a8e796cf87e7d1f5961edf869feab106e2fd
checksum asset id: 532104140
checksum asset SHA-256: 30e66cd988c85491d1a0f369dedec53ddb5afc430ce2bca65a47893ddc1d055d
407 passed / 0 failed / 0 skipped
Product UI / Scanner / Map / Factory / MiniMap / graceful shutdown smoke: SUCCESS
```

GitHub `/releases/latest` 및 tag-ref readback:

- tag `v1.7.14`
- release target/tag ref = exact product release source
- draft = false
- prerelease = false
- latest stable = true
- `Junhyun-Helper.zip` + `SHA256SUMS.txt` present
- public ZIP digest = exact main-CI package SHA-256

공개 증거:

- `docs/RELEASE_1.7.14.md`
- `docs/.release-v1.7.14-status.json`
- `docs/RELEASE_NOTES_V1.7.14.md`
- `docs/DECISION_V1.7.14_UI_CONSISTENCY.md`

이 문서 동기화 이후의 commit은 **v1.7.14 product release source가 아니다**. 제품 릴리즈 소스는 항상 위 `0a51375d...`로 고정한다. 이미 공개된 v1.7.14 tag/source/assets는 immutable historical product release로 취급한다.

## Schema / compatibility

```text
Desktop target version: 1.7.14
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

## v1.7.14 — UI consistency patch

v1.7.14는 도메인 계산이나 Scanner identity recognition을 바꾸지 않고 사용자-facing popup·overlay·검색 interaction을 한 제품 규칙으로 정리한 PATCH다.

- Ammo `즐겨찾기 선택` / `표시 열` popup은 같은 launcher 재클릭으로 실제 닫힘을 유지한다.
- MiniMap launcher 주변 donor 잔여 chrome과 숨긴 help-button 공간을 제거했다.
- `지도 마커` launcher를 JunhyunHelper 일반 Button chrome으로 정리했다.
- 지도 마커 panel은 접힌 상태에서 빈 panel chrome을 남기지 않고, 펼친 상태에서 일반 desktop viewport의 현재 checkbox를 가능한 한 한 화면에 보여줄 충분한 높이를 확보한다.
- Map/MiniMap Settings, Scanner Settings, Scanner Advanced, Profile Edit는 MainWindow shared in-app overlay interaction을 사용한다.
- 같은 launcher 재클릭, backdrop click, 공통 overlay X는 같은 dismiss path를 사용한다.
- child editor의 validation/save semantics는 overlay host가 재구현하지 않는다.
- Scanner hotkey 편집은 Scanner Settings 내부로 통합했다.
- 기존 전용 `ScannerHotkeySettingsWindow`는 제거했다.
- Quest / Hideout / Items / Ammo / Scanner 주요 검색창은 입력창 오른쪽 내부 `×` clear affordance를 사용한다.
- Scanner Advanced는 standalone Window가 아니라 실제 shared overlay host 상태에서 published EXE smoke를 수행한다.

Regression protection:

- `V1714UiConsistencyContractTests`
- 407 deterministic tests
- actual published Windows x64 Product UI / Scanner / Main Map / Factory / MiniMap smoke
- graceful shutdown / clean portable root
- package/ProductVersion/FIRST_RUN verification

## Scanner 현재 기준선

Scanner는 현재 **FEATURE COMPLETE / MAINTENANCE ONLY**다.

```text
Tarkov window pixels
→ detail rectangle proposals
→ inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ serialized Windows ko-KR OCR
→ optional user substitution
→ conditional environment-aware title normalization
→ conservative official-catalog matching / bounded recovery
→ optional current-pixel visual corroboration
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
```

불변 계약:

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
- stale/cross-frame OCR 또는 visual result를 현재 Item identity proof로 사용하지 않음
- Item ID 확정 전 price/needed/slot/previous-frame metadata를 identity evidence로 사용하지 않음
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
- normal snapshot shrink guard = baseline의 50%
- collection schema drift는 fail closed
- Wiki Ballistics enrichment는 fail-soft
- User Progress와 Game Content authority 분리

## 유지보수 원칙

새 작업은 기본적으로 다음 evidence-driven 순서를 따른다.

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

현재 이 v1.7.14 릴리즈 배치에 남은 제품 개발 작업은 없다. 이후 기본 모드는 유지보수다.
