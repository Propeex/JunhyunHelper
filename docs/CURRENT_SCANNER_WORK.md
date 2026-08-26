# Current Scanner Work

기준일: 2026-08-26
상태: **FEATURE COMPLETE / MAINTENANCE ONLY / v1.7.10 PUBLIC STABLE**

## 최종 결론

Scanner 기능 개발 단계는 종료됐다. 현재 기본 운영 모드는 **유지보수 전용**이다.

최근 안정화 이력:

- v1.7.6: 일부 실제 데스크톱의 5~13초 인식 지연 해결
- v1.7.7: durable automatic Case 폭증, 반복 로그, Scanner/Map hotkey 계약 정리
- v1.7.8: raid inventory inspect-header ownership 회귀 수정
- v1.7.9: recognition success 뒤 Mini Scanner 표시가 보조 OCR에 의해 veto되던 presentation 회귀 수정
- v1.7.10: 특정 PC가 아닌 공개 배포 범용성을 위한 item-title OCR 입력 환경 정규화

새 실제 회귀 증거가 없는 한 threshold, candidate cap, OCR/matcher/visual acceptance를 선제 조정하지 않는다.

## 현재 Public stable

```text
version: v1.7.10
exact product release source: a557daad5b37aca11a189524ecf256564d2b8ea4
main CI run: 32983155982 — SUCCESS
release workflow run: 32983498402 — SUCCESS
release id: 377231814
asset: Junhyun-Helper.zip
asset id: 530959212
bytes: 80,471,678
SHA-256: 6d4f3f8580318d05361cd4d62bf265c4590532722df22dc8b8d734fe8ec10eb9
389 passed / 0 failed / 0 skipped
```

GitHub `/releases/latest` readback에서 v1.7.10이 draft=false, prerelease=false, latest stable이며 tag target이 exact product release source와 일치함을 확인했다.

상세 공개 증거:

- `docs/RELEASE_1.7.10.md`
- `docs/.release-v1.7.10-status.json`
- `docs/RELEASE_NOTES_V1.7.10.md`

## 현재 Scanner pipeline

```text
Tarkov window pixels
→ capture
→ detail rectangle proposals
→ close-X / magnifier / inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ serialized Windows ko-KR OCR
→ optional user OCR substitution
→ conditional environment-aware title normalization
→ official-catalog normalization/matching
→ bounded recovery / optional current-pixel visual corroboration
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
```

Scanner는 closed-domain recognizer이며 current official Korean Tarkov full-item catalog가 Item identity authority다.

## v1.7.10 — 공개 배포 범용성

사용자 제품 결정:

> Scanner는 특정 사용자 PC, 해상도, HDR/SDR 또는 GPU 설정에 맞춘 도구가 아니라 공개 배포 가능한 범용 제품이어야 한다.

v1.7.10은 정상 성공 경로를 보존하면서 환경 이상이 감지될 때만 title normalization을 추가한다.

```text
normal OCR
→ text 있음: 기존 결과 즉시 사용
→ text 없음: title ROI luminance profile 분석
    → reference/flat: 기존 경로 유지
    → lifted/washed/low-contrast: adaptive normalized auxiliary OCR
→ existing bounded deep OCR
    → abnormal environment일 때만 normalized auxiliary evidence 추가
→ existing conservative catalog matching
→ Item ID or fail closed
```

정규화:

- P60: dark title field background 추정
- P99.75: sparse bright glyph foreground 추정
- usable contrast가 없는 flat input은 normalization하지 않음
- 정상 normal OCR 성공 시 histogram/copy/추가 OCR 자체를 생략
- 환경 정규화는 identity proof가 아님

Procedural regression matrix:

- reference SDR-like
- HDR→SDR-like lifted/washed
- lifted + compressed contrast
- low-contrast gamma/rendering variation
- 1080p/1440p/4K proportional title raster
- flat/no-contrast negative

기존 semantic/catalog/matcher/visual acceptance는 완화하지 않았다.

공식 결정: `docs/DECISION_SCANNER_CROSS_ENVIRONMENT_2026-08-26.md`.

## v1.7.9 — Mini Scanner presentation

문제:

```text
Scanner recognition success
→ Item ID confirmed
→ Mini Scanner가 별도 top-band inventory OCR 수행
→ 보조 OCR 실패
→ confirmed Item 표시까지 차단
```

현재 계약:

```text
Scanner semantic success
→ Item ID confirmed
→ presentation snapshot
→ Mini Scanner
   ├─ preview/display-test: show
   ├─ already visible: immediate update
   └─ hidden real Scanner:
        Tarkov foreground yes → show
        Tarkov foreground no  → hidden / fail closed
```

Auxiliary inventory-header OCR은 confirmed Item presentation을 veto할 권한이 없다.

Sticky presentation:

- success → 표시/갱신 + miss budget reset
- miss #1 → last good 유지
- miss #2 → last good 유지
- miss #3 → hide
- progress-only state는 miss로 세지 않음

## v1.7.8 — raid header ownership

사용자 reviewed 8 Case 중 6개 실제 실패는 OCR 이전 `HEADER_CLOSE_NOT_LOCKED` / `TITLE_ANCHOR_INCOMPLETE`였다.

원인: raid inventory neutral horizontal line과 inspect header가 이어져 기존 fallback의 header-left ownership이 실제 상세창보다 47~132px 왼쪽으로 확장됐다.

현재 recovery order:

```text
primary header lock
→ live Ground Truth recovery
→ v1.7.8 raid ownership recovery
→ contained-subpanel recovery
→ fail closed
```

raid recovery entry:

```text
candidate reason = RED_X_CANDIDATE
structural score >= 0.90
```

독립적으로 close-X, magnifier, neutral header, dark title field, text evidence와 final `HEADER_FRAME_LOCKED >= 0.68`을 다시 요구한다.

## v1.7.7 — Ground Truth / logs / hotkeys

현재 durable data contract:

```text
runtime capture / recognition
→ latest exact frame in memory
→ bounded runtime text log
→ user explicitly opens correction
→ user explicitly saves
→ reviewed durable Ground Truth
```

정상 monitoring은 실패/ambiguity만으로 durable Case를 생성하지 않는다.

Legacy cleanup은 다음 모두를 증명할 때만 허용한다.

```text
retention = automatic_sample
review_status = unreviewed
recent-write safety >= 5 minutes
pre-delete metadata/state re-read = unchanged
```

reviewed/manual/corrupt/unknown/state-changed Case는 preserve fail closed한다.

동일 Scanner activity failure는 30초 collapse한다.

Scanner/Map configurable hotkey:

- primary key + optional Ctrl/Alt/Shift
- bare key 허용
- Windows modifier 미지원
- Map bare NumPad0~5 direct floor 유지
- modifier+NumPad configurable action 허용

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
하프 마스크
10,840.877 ms → 70.603 ms

USB 보안 플래시 드라이브
12,686.278 ms → 1,354.775 ms
```

root cause는 Windows OCR이 아니라 같은 current frame visual evidence의 반복 계산이었다.

같은 Scanner latency cycle의 exact current-pixel evidence만 재사용하며 cycle이 바뀌면 폐기한다. cross-frame identity cache가 아니다.

## Scanner UI — current

일반 Scanner 화면 상단:

```text
스캐너 ON/OFF
설정
고급
현재 결과 교정
```

`현재 결과 교정`은 메모리에 보존된 최신 exact Scanner frame만 기존 correction window로 연다.

`고급`:

- Display Test / 테스트 스캐너
- 교정 데이터 관리
- Scanner 성능 진단 자료 내보내기

## 정확도·안전 불변식

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

- false positive보다 miss 우선
- current official Tarkov catalog가 identity authority
- geometry/environment normalization은 identity proof가 아님
- stale/cross-frame OCR/visual identity proof 금지
- matcher / visual acceptance 완화 없음
- Item ID 확정 전 mapped metadata를 identity proof로 사용 금지
- scan-time network identity work 없음
- reviewed Ground Truth 없이 threshold/candidate cap 완화 금지

## v1.7.10 CI / release proof

```text
PR #192 final head: 322c2e4e1dd641905411cc10fb9a81ba22816d33
PR CI: 32981693237 — SUCCESS
main source: a557daad5b37aca11a189524ecf256564d2b8ea4
main CI: 32983155982 — SUCCESS
Release workflow: 32983498402 — SUCCESS
389 passed / 0 failed / 0 skipped
```

## Maintenance workflow

```text
evidence
→ failure stage
→ root cause
→ affected layer only
→ reviewed replay where runnable
→ procedural regression where applicable
→ full Windows CI / publish / Product UI + Scanner + Map smoke
→ PATCH release
→ public release readback
→ canonical docs sync
```

Recognition 변경은 runnable reviewed dataset이 존재하면 `REGRESSION=0`을 요구한다. Private user images를 단순 CI 편의를 위해 public repo에 넣지 않는다.
