# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md`, `docs/SCANNER.md`, 버전별 결정/릴리즈 문서를 참조합니다.

기준일: 2026-08-27

상태: **`v1.7.12 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable

현재 공개 stable/latest는 **v1.7.12**다.

```text
public stable/latest: v1.7.12
exact product release source/tag target: d8d0f8eb1ffdd9b8c4ec890277a7b209b2458c2b
main CI run: 33042307773 — SUCCESS
release workflow run: 33042464642 — SUCCESS
release id: 377581895
stable asset: Junhyun-Helper.zip
stable asset id: 531791229
stable bytes: 80,477,641
stable SHA-256: 3f0d57f8a5dc92611bc8648a423c43d65917e63e0d73a771b559153803186fa1
checksum asset id: 531791226
checksum asset SHA-256: 97cf0d26c1d6c91c5876ee02f829225a23221e2bc893659d211055aa6af6a99d
397 passed / 0 failed / 0 skipped
Product UI / Scanner / Map / Factory / MiniMap / graceful shutdown smoke: SUCCESS
```

GitHub `/releases/latest` 및 tag-ref readback:

- tag `v1.7.12`
- target/tag ref = exact product release source
- draft = false
- prerelease = false
- latest stable = true
- `Junhyun-Helper.zip` + `SHA256SUMS.txt` present
- public ZIP digest = exact main-CI package SHA-256

공개 증거:

- `docs/RELEASE_1.7.12.md`
- `docs/.release-v1.7.12-status.json`
- `docs/RELEASE_NOTES_V1.7.12.md`

이 문서 동기화 이후의 commit은 **v1.7.12 product release source가 아니다**. 제품 릴리즈 소스는 항상 위 `d8d0f8eb...`로 고정한다.

## Schema / compatibility

```text
Desktop target version: 1.7.12
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

## v1.7.12 — long-term maintenance hardening

v1.7.12는 새 사용자 기능 없이 장기 유지보수성을 높인 patch다.

- Quest/Hideout/Items/Ammo 공통 image-cache binding, Ammo favorite store, cross-page navigation wiring의 owner를 `MainWindow.OnInitialized`로 명시했다.
- 개별 Page가 우연히 `Loaded`되는 순서가 다른 Page infrastructure 준비 상태를 결정하지 않는다.
- dead-code audit에서 MainWindow page Loaded handlers를 제거하는 과정에서 actual published EXE smoke가 Ammo detail collapse/expand 초기화 회귀를 탐지했다.
- root cause는 Ammo class-level `Loaded` handler가 부모 Loaded subscription 존재 여부에 간접 의존하던 hidden WPF lifecycle coupling이었다.
- Ammo search/detail/grid presentation을 `AmmoPage.OnInitialized` + Loaded dispatcher priority가 직접 소유하도록 수정해 부모 event handler 의존성을 제거했다.
- `DesktopStartupWiringContractTests`와 actual published EXE smoke가 이 ownership 경계를 함께 보호한다.
- Scanner recognition 기준, Map/MiniMap donor revision, Game Content validation/LKG 계약은 변경하지 않았다.

Audit 판단:

- workspace의 반복 profile `LoadAsync`는 `UserProfileStore` immutable in-process snapshot cache 때문에 현재 evidence만으로 SQLite 병목으로 판정하지 않음
- additional global cache / 병렬화 / one-read-multi-build는 실제 runtime trace 전까지 보류
- `Legacy` Map bridge, Map/Factory/MiniMap smoke, Scanner diagnostic reflection adapter는 현재 dead code가 아님
- original full-refresh mutation handler + fast rebinding은 lifecycle 관여 증거가 있어 삭제 보류

공식 결정:

- `docs/DECISION_LONG_TERM_MAINTENANCE_AUDIT_2026-08-27.md`
- `docs/DECISION_V1.7.12_MAINTENANCE.md`

## v1.7.11 — maintenance polish

v1.7.11은 Scanner recognition 기준을 조정하지 않고 기존 제품의 표시·입력·MiniMap 사용성 문제를 수정했다.

- Scanner / Mini Scanner `필요 개수` = canonical `NeededItems[itemId].RemainingTotal`
  - 현재 Inventory와 FIR 조건 반영
  - `RequiredTotal`은 전체 요구량이며 Scanner 표시값이 아님
  - Item ID 확정 뒤에만 presentation에 사용
- Configurable Map / Scanner hotkey
  - 등록된 Ctrl/Alt/Shift는 모두 필요
  - 추가 Ctrl/Alt/Shift 허용
  - 같은 primary key에서 여러 compatible binding이 있으면 더 구체적인 binding 우선
  - Windows modifier 미지원
  - Map bare NumPad0~5 direct floor 유지
- MiniMap 첫 표시 전에 현재 Main Map 선택을 `MapTrackerService`에 동기화
- MiniMap width/height를 `%LocalAppData%/JunhyunHelper/minimap-window-state.json`에 저장·복원
- standard WPF 설명 ToolTip은 전역에서 열지 않음
- 기능성 custom Popup은 유지

공식 결정: `docs/DECISION_V1.7.11_MAINTENANCE.md`.

## Scanner 현재 기준선

```text
Tarkov window pixels
→ detail rectangle proposals
→ inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ serialized Windows ko-KR OCR
→ optional user substitution
→ environment-aware title normalization when needed
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
- Item ID 확정 전 price/needed/slot metadata를 identity evidence로 사용하지 않음
- scan 순간 network identity work 없음
- reviewed Ground Truth 없이 recognition threshold/candidate cap/matcher/visual acceptance 완화 금지

## v1.7.10 — cross-environment hardening

사용자 결정: Scanner는 특정 사용자 PC/해상도/HDR 설정에 맞춘 도구가 아니라 공개 배포 가능한 범용 제품이어야 한다.

v1.7.10은 item-title OCR 입력의 밝기 환경 차이를 조건부 정규화한다.

```text
normal OCR success
→ 기존 결과 즉시 사용

normal OCR miss 또는 기존 bounded deep pass
→ title ROI luminance profile 분석
→ reference/flat input: 기존 경로 유지
→ lifted/washed/low-contrast input: normalized auxiliary OCR
→ 기존 conservative catalog matching
→ Item ID or fail closed
```

- P60: dark title-field background 추정
- P99.75: sparse bright glyph foreground 추정
- usable contrast가 없는 flat input은 normalization하지 않음
- 정상 normal OCR 성공 시 histogram/copy/추가 OCR 비용도 발생시키지 않음
- deterministic regression: 1080p/1440p/4K proportional raster + reference/lifted/washed/compressed-contrast/low-contrast/flat cases
- existing semantic/catalog/matcher/visual acceptance는 낮추지 않음

공식 결정: `docs/DECISION_SCANNER_CROSS_ENVIRONMENT_2026-08-26.md`

## v1.7.9 — Mini Scanner presentation authority

v1.7.9는 Scanner 로그에는 Item recognition success가 기록됐지만 Mini Scanner가 열리지 않던 presentation-only 회귀를 수정했다.

원인은 Item ID 확정 후 Mini Scanner가 별도 top-band inventory OCR을 다시 수행하고, 그 보조 OCR 실패가 이미 확정된 Item 표시를 veto하던 구조였다.

현재 계약:

```text
Scanner semantic success
→ Item ID confirmed
→ presentation snapshot
→ Mini Scanner
   ├─ preview/display-test: show
   ├─ already visible: authoritative Item result로 즉시 update
   └─ hidden real Scanner:
        Tarkov foreground yes → show
        Tarkov foreground no  → hidden / fail closed
```

보조 inventory-header OCR은 confirmed Item presentation을 veto할 권한이 없다.

Sticky presentation:

```text
success → show/update + miss budget reset
miss #1 → last good 유지
miss #2 → last good 유지
miss #3 → hide
```

## v1.7.8 — raid inspect-header ownership

레이드 인벤토리의 수평선이 inspect header와 이어져 header-left ownership이 실제 상세창보다 47~132px 왼쪽으로 확장되는 회귀를 user-reviewed 8 Case로 확인했다.

수정 순서:

```text
primary header lock
→ live Ground Truth recovery
→ v1.7.8 raid ownership recovery
→ contained-subpanel recovery
→ fail closed
```

raid recovery는 강한 `RED_X_CANDIDATE >= 0.90`에서만 진입하며 close-X, magnifier, neutral header, dark title field, text evidence와 최종 `HEADER_FRAME_LOCKED >= 0.68`을 모두 다시 요구한다.

## v1.7.7 — Ground Truth / logs / hotkeys

- 정상 monitoring은 durable automatic Case를 만들지 않음
- latest exact frame은 current correction용 메모리 상태로만 유지
- 사용자 명시적 저장만 reviewed durable Ground Truth
- legacy `automatic_sample + unreviewed`만 recent-write safety 및 metadata/state 재확인 후 cleanup
- reviewed/manual/corrupt/unknown/state-changed Case는 preserve fail closed
- 동일 Scanner activity failure는 30초 동안 collapse
- primary key + optional Ctrl/Alt/Shift 구성
- bare key 허용, Windows modifier 미지원
- Map bare NumPad0~5 직접 층 선택 유지

Modifier matching의 현재 동작은 v1.7.11의 extra-modifier compatibility / most-specific-wins 계약이 우선한다.

## v1.7.6 — performance baseline

문제 PC actual Tarkov `ReadingTitle → ShowingItem` 성공 12건:

```text
minimum 38.07 ms
median  63.92 ms
maximum 1.05 s
mean    211.47 ms
```

Display Test:

```text
하프 마스크: 10,840.877 ms → 70.603 ms
USB 보안 플래시 드라이브: 12,686.278 ms → 1,354.775 ms
```

root cause는 Windows OCR 자체가 아니라 같은 cycle의 exact current-pixel visual evidence 반복 계산이었다. 현재 재사용은 동일 Scanner cycle + exact pixel identity에만 한정하며 cross-frame identity cache가 아니다.

## 유지보수 workflow

새 Scanner 문제는 다음 순서로 처리한다.

```text
runtime evidence 확보
→ failure stage 분류
→ root cause 확정
→ affected layer만 최소 수정
→ reviewed regression where runnable
→ deterministic procedural regression
→ full Windows CI / publish / Product UI + Scanner + Map smoke
→ PATCH release
→ public release readback
→ canonical docs sync
```

새 실제 evidence 없이 threshold/candidate cap/OCR/matcher/visual acceptance를 선제 조정하지 않는다.
