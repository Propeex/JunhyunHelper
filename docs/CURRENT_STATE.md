# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-24

상태: **`v1.5.0 IMPLEMENTATION COMPLETE / FINAL RELEASE GATE`**

## 현재 공개 기준선

현재 public stable / latest는 **v1.4.4**입니다.

```text
public stable: v1.4.4
release source/tag: 0c7f31e118122ffef6e5999f7a20a77d823a450d
asset: Junhyun-Helper-v1.4.4-win-x64.zip
bytes: 80,391,895
SHA-256: 64320e36ba94b6f206ef997e3d42a809c7beef2c859f4bc7f53f704f74866f40
ProductVersion: 1.4.4+0c7f31e118122ffef6e5999f7a20a77d823a450d
release run: 32680058795 — SUCCESS
independent public verifier: 32680422756 — SUCCESS
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download / SHA256SUMS / package layout: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

공식 공개 검증: `docs/.release-v1.4.4-status.json`

## v1.5.0 release candidate

공식 결정:

- `docs/DECISION_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md`

구현/릴리즈 상태:

- `docs/STATUS_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md`
- `docs/RELEASE_NOTES_V1.5.0.md`

현재 branch / PR:

```text
branch: product/v1.5.0-usability-data-hardening
PR: #172 — Build v1.5.0 product finishing pass
Desktop Version: 1.5.0
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v5
Scanner catalog cache: v1/v2 readable, v2 written
Core tests: 296 passed / 0 failed / 0 skipped on full pre-final gate
```

GitHub PR HEAD와 CI가 이 문서보다 최신이면 항상 GitHub 상태를 우선합니다.

## v1.5.0 완료 범위

1. Scanner mapped market data repair
2. Quest `확인 필요` current live-data audit / safe compatibility
3. unified Game Data + Scanner catalog/market update
4. persistent user OCR substitutions with raw OCR preservation
5. candidate-based Ground Truth correction + manual/`없음` fallback
6. Scanner stage latency telemetry + exact same-cycle OCR reuse
7. conservative continuous result stabilization
8. automatic diagnostics/log retention while preserving reviewed GT
9. Scanner primary/settings/advanced UI separation + quick current-result correction
10. whole-product UI consistency audit

남은 작업은 새 기능 개발이 아니라 **final CI → PR merge → exact-source v1.5.0 public release → independent anonymous public redownload verification → release workflow cleanup**입니다.

## Scanner 핵심 계약

```text
capture
→ detail rectangle proposals
→ close-X + magnifier + neutral header semantic validation
→ HEADER_FRAME_LOCKED >= 0.68
→ item-name ROI
→ Windows ko-KR OCR
→ optional user substitution (single pass)
→ current-catalog sanitation / normalization
→ conservative catalog matching / bounded recovery
→ Item ID or fail closed
→ local mapped presentation
→ Mini Scanner
```

불변 정책:

- false positive보다 miss 선호
- geometry는 proposal이며 identity proof가 아님
- structural floor `0.34`
- continuous max `8` / one-shot max `12`
- magnifier + red close-X 필수
- current official Korean item catalog가 identity authority
- production OCR field는 item-name 하나
- price/flea/slots/needed는 Item ID 이후 mapped data
- scan-time network 금지
- game memory read / DLL injection / packet interception 금지
- 자동 global `r/0/한글` 강제 substitution table 금지

## Scanner v1.5.0 사용자 흐름

일반 화면:

- Scanner ON/OFF
- 1회 스캔
- 현재 결과 교정
- runtime status
- 최근 인식 기록

`설정`:

- global hotkeys
- OCR 문자/문자열 치환
- Mini Scanner 표시 항목

`고급 / 진단`:

- display test
- recognition image
- regression
- Ground Truth export/manage
- forced Scanner catalog refresh
- log clear

Mini Scanner는 우클릭 → `현재 결과 교정`을 지원합니다.

## Scanner 성능 / 안정화

Stage latency를 capture / rectangle proposal / semantic header / normal+deep OCR / visual recovery / catalog matching / presentation / end-to-end로 기록합니다.

동일 active scan-cycle의 OCR 입력 bitmap이 dimensions/format/pixels까지 완전히 같을 때만 normal/deep 결과를 각각 재사용합니다. Frame/cycle 사이 OCR cache는 없습니다.

검증된 item의 title glyph identity가 유지되는 동안에는 harmless dark-background/trailing-ROI variation으로 결과를 불필요하게 지우지 않습니다. 다른 title/identity evidence가 확인되면 기존 snapshot을 폐기하고 재검증합니다.

## Retention

자동 삭제 금지:

- user-reviewed Ground Truth
- review/ownership state를 확정할 수 없는 unknown/corrupt Case

자동 unreviewed diagnostic Case:

- 30 days
- 300 cases
- 512 MiB
- recent 2-hour safety window

Scanner/startup log도 bounded rotation을 사용합니다.

## 다음 단계

1. final PR HEAD CI green 확인
2. PR #172 merge
3. exact main merge SHA CI green 확인
4. exact product source SHA에 고정한 temporary v1.5.0 release workflow 실행
5. draft ZIP/SHA256/package/ProductVersion/full product smoke 검증
6. stable/latest publish
7. fresh Windows runner에서 인증 없이 public ZIP/SHA256SUMS 재다운로드 및 hash/layout/ProductVersion/full product smoke 검증
8. `docs/.release-v1.5.0-status.json` 기록
9. temporary release workflow 제거 및 canonical docs를 `PUBLIC RELEASE / VERIFIED`로 갱신
