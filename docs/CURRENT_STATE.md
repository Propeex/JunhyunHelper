# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md`, `docs/SCANNER.md`, 버전별 결정/릴리즈 문서를 참조합니다.

기준일: 2026-08-26

상태: **`v1.7.10 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable

현재 공개 stable/latest는 **v1.7.10**이다.

```text
public stable/latest: v1.7.10
exact product release source/tag target: a557daad5b37aca11a189524ecf256564d2b8ea4
main CI run: 32983155982 — SUCCESS
release workflow run: 32983498402 — SUCCESS
release id: 377231814
stable asset: Junhyun-Helper.zip
stable asset id: 530959212
stable bytes: 80,471,678
stable SHA-256: 6d4f3f8580318d05361cd4d62bf265c4590532722df22dc8b8d734fe8ec10eb9
checksum asset id: 530959213
389 passed / 0 failed / 0 skipped
Product UI / Scanner / Map / Factory / MiniMap / graceful shutdown smoke: SUCCESS
```

GitHub `/releases/latest` readback:

- tag `v1.7.10`
- target = exact product release source
- draft = false
- prerelease = false
- latest stable = true
- `Junhyun-Helper.zip` + `SHA256SUMS.txt` present

공개 증거:

- `docs/RELEASE_1.7.10.md`
- `docs/.release-v1.7.10-status.json`
- `docs/RELEASE_NOTES_V1.7.10.md`

이 문서 동기화 이후의 commit은 **v1.7.10 product release source가 아니다**. 제품 릴리즈 소스는 항상 위 `a557daad...`로 고정한다.

## Schema / compatibility

```text
Desktop target version: 1.7.10
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
Scanner Ground Truth: explicit user-reviewed durable cases
```

사용자 mutable data는 `%LocalAppData%/JunhyunHelper`에 둔다. Program Update는 user.db, content/image cache, Map/Ammo/Scanner 설정, Scanner logs/diagnostics/Ground Truth를 덮어쓰지 않는다.

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
- Scanner/Map configurable action은 primary key + optional Ctrl/Alt/Shift
- bare key 허용, Windows modifier 미지원
- Map bare NumPad0~5 직접 층 선택 유지

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
