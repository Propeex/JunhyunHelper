# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 기계 판독 가능한 현재 사실값은 `docs/PROJECT_STATE.json`, 상세 설계와 이력은 `docs/STATE.md`, `docs/PRODUCT.md`, `docs/DECISIONS.md`, 전문 결정/릴리즈 문서를 참조합니다.

기준일: **2026-08-30 KST**

상태: **`v1.11.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable

```text
public stable/latest: v1.11.0
exact product release source/tag target:
e0a8dd8acc86f8c5675efd0b24cb3006c19ccb1d
PR validated exact-head CI: 33298972004 — SUCCESS
exact-main CI: 33299138580 — SUCCESS
exact-main Shutdown Race CI: 33299138567 — SUCCESS
exact-main Documentation Consistency: 33299138569 — SUCCESS
release workflow: 33299258838 — SUCCESS
release id: 379210317
published UTC: 2026-08-30T07:28:08Z
457 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 536298335
bytes: 80,550,542
SHA-256:
fb1d2f38ab26420d069fa8f0aab899c5e9776ffb072c83312e447289ef6f7c87

SHA256SUMS.txt
asset id: 536298334
bytes: 86
asset SHA-256:
277a5763796e0fc30f71ef959cb8a8ee18402a201c6042565a910368e70d89e8
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9728381122
archive bytes: 241,586,113
archive SHA-256:
e9f8ac2e6d0349f9b6b7a9856d7d5bae6f6af9f03a91934dacf8a5c8ad77623f
```

`/releases/latest`와 `refs/tags/v1.11.0` readback에서 release target/tag ref가 exact product source와 일치하고 `draft=false`, `prerelease=false`, latest stable임을 확인했다.

공개 증거:

- `docs/RELEASE_1.11.0.md`
- `docs/.release-v1.11.0-status.json`
- `docs/RELEASE_NOTES_V1.11.0.md`

## v1.11.0 핵심 변경

### Map / MiniMap

- MiniMap window가 아직 없더라도 최신 Main Map selection을 product registry에 보존하고 first register 시 replay한다.
- donor Extract checkbox가 product settings bridge보다 늦게 생성되는 경우에도 retry하여 marker 설정 목록에 반영한다.
- Player Marker Size 변경으로 donor가 marker visual tree를 갱신한 뒤 Junhyun marker/name/visibility/category/edge-label presentation 전체를 다시 projection한다.
- donor marker refresh가 container clear 뒤 취소될 수 있는 lifecycle race를 확인했고, 같은 map/floor에서 직전에 정상 marker가 있었는데 standard marker layer가 지속적으로 0개인 경우에만 bounded one-shot refresh recovery를 수행한다.

### Scanner

- `플리마켓 최저가` 사용자 표시를 제거했다. source/model/cache 호환 데이터는 유지한다.
- Scanner display settings schema는 v8이다.
- 설정 가능한 `교정 데이터 추가` global hotkey를 추가했다. 기본값은 `Ctrl+Alt+F9`이다.
- 최신 evidence가 없으면 `저장할 스캔 결과가 없습니다.`를 표시하고 Case를 만들지 않는다.
- 완전/불완전/미인식 evidence를 Saved Case로 저장할 수 있지만 hotkey는 Ground Truth를 생성·추측하지 않는다.
- 같은 최신 결과를 연속 저장해도 explicit save마다 별도 Case ID를 사용한다.

### Hideout FIR / Needed Items

- Hideout requirement의 FIR 의미를 requirement `attributes.foundInRaid`에서 canonical content로 보존한다.
- FIR requirement에는 non-FIR inventory가 충당되지 않는다.
- cleanup 판정도 현재 canonical FIR/non-FIR requirement를 기준으로 계산한다.

### Ammo pickup / Ammo Pack

- same-caliber penetration ranking과 현재 Trader LL을 사용해 Scanner/Mini Scanner에 pickup 판단을 제공한다.
- 현재 LL의 direct-money purchase만 구매 가능으로 인정한다.
- barter/craft/flea/higher-LL offer는 제외한다.
- quest-unlocked direct purchase는 현재 profile에서 해당 quest 완료가 확인된 경우에만 구매 가능으로 인정한다.
- Ammo Pack은 authoritative `containsItems` relation을 우선해 contained canonical ammo로 resolve한다.
- authoritative relation이 빈 경우에만 제한적인 name fallback을 허용한다.

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout / Needed Items | 구현 완료 / maintenance |
| Items / Ammo | 구현 완료 / v1.11 pickup evaluator 포함 |
| Map + MiniMap | 구현 완료 / v1.11 lifecycle repairs 포함 |
| Game Content Update | 구현 완료 / relationship LKG + FIR semantics + fail-closed |
| Program Update | 구현 완료 / stable ZIP checksum 계약 |
| Scanner + Mini Scanner | **FEATURE COMPLETE / MAINTENANCE** |
| Scanner Saved Case / Ground Truth | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner 아이템 정보 DB | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner Favorites / Recents | **IMPLEMENTED / PUBLIC STABLE** |

## Schema / compatibility

```text
Desktop target version: 1.11.0
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v8
Scanner catalog cache: v1~v4 readable, v4 written
Scanner item UI state: scanner-item-ui-state.json / canonical Item ID persistence
```

v1.10.1 → v1.11.0 mandatory Game Content schema migration: none  
v1.10.1 → v1.11.0 user.db migration: none

## 유지되는 핵심 계약

- Scanner false positive보다 miss를 선호한다.
- OCR/matcher/candidate cap/visual recovery acceptance는 reviewed actual Tarkov evidence 없이 완화하지 않는다.
- Scanner recognition은 외부 화면 pixels + OCR만 사용한다.
- game process memory read / injection / hook / kernel / input automation / network manipulation / anti-cheat bypass를 사용하지 않는다.
- Scanner current needed = `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`.
- Scanner source = `ItemsWorkspace.Plan.NeededItems[itemId].Sources`.
- price/needed/source/relationship metadata는 Item ID proof에 사용하지 않는다.
- Game Content candidate/LKG/completeness/fail-closed를 유지한다.
- Map/MiniMap donor pin은 `d933792b6042a51cea38dc44b686a096fe30de67`이다.
- user-visible WPF lifecycle 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE runtime evidence로 검증한다.

## 검증 상태

v1.11.0 product release source는 다음을 모두 통과했다.

- 457 automated tests
- Windows Release build
- Windows x64 self-contained publish
- actual published EXE Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- graceful shutdown
- active-async shutdown race
- release package root/dependency/checksum audit
- exact-main CI
- tag/release/assets public readback

사용자의 실제 PC/Tarkov 플레이 환경 실사용 검증은 자동화 검증과 별개이며 현재 **PENDING**이다.

## 다음 작업

v1.11.0 릴리즈 배치에 남은 제품 개발 작업은 없다. 기본 운영 모드는 유지보수다. 새 기능은 사용자가 명시적으로 제품 요구사항으로 확정할 때 시작한다.

이 문서와 이후 documentation-only commit은 v1.11.0 product release source가 아니다. v1.11.0 product source/tag/assets는 `e0a8dd8acc86f8c5675efd0b24cb3006c19ccb1d`에 고정한다.
