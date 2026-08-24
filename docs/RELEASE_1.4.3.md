# RELEASE 1.4.3 — Scanner semantic candidate validation and OCR alphabet hardening

상태: `PUBLIC RELEASE / VERIFIED`

기준일: 2026-08-24

## 범위

v1.4.3은 새 사용자 기능을 추가하지 않는 PATCH 릴리즈다. v1.4.2까지의 실제 Tarkov Ground Truth에서 확인된 상세보기 창 후보 탈락 구조와 `r`/`0` 계열 glyph가 Unicode 문자·기호로 오인되는 OCR 문제를 대상으로 한다.

이번 변경은 Scanner의 구조적 책임을 다음처럼 분리한다.

```text
capture
→ rectangle proposals
→ semantic inspect-header validation
→ title ROI
→ OCR
→ current official catalog validation
→ final Item ID
```

geometry는 상세창 identity를 확정하지 않고 가능한 rectangle proposal을 공급한다. 실제 상세창 여부는 close-X + magnifier + header/title evidence가 최종 결정한다. Scanner 속도 최적화는 정확도 안정화 이후 작업으로 유지한다.

## 제품 변경

### 상세보기 창 candidate 구조

- historical `aspect ≈ 1.3`은 identity 조건이 아니라 약한 proposal ordering hint다.
- tall/large inspect window가 구조 단계에서 사전 탈락하지 않도록 broad impossible-shape guard만 유지한다.
- `IoU >= 0.72`라는 이유만으로 겹치는 후보를 semantic 검증 전에 제거하던 방식은 제거했다.
- 서로 크게 겹쳐도 top/bottom/left/right edge가 실질적으로 다르면 semantic 검증까지 보존한다.
- 사실상 같은 rectangle의 몇 px edge-jitter만 near-duplicate로 정리한다.
- rough red-X proximity는 proposal ranking hint일 뿐 semantic close-X proof가 아니다.
- production OCR 진입은 계속 다음을 모두 요구한다.

```text
TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
AND magnifier evidence present
AND close-X evidence present
```

따라서 stash/inventory의 큰 사각형이 structural ranking에서 앞서도 실제 X/돋보기/header evidence를 통과하지 못하면 상세창으로 확정되지 않는다.

### OCR 문자·기호 정책

- Scanner는 범용 문장이 아니라 current official Tarkov item-name catalog를 읽는 closed-domain recognizer다.
- punctuation뿐 아니라 Unicode letter/digit도 현재 공식 아이템명에서 실제 사용하는 character inventory와 대조한다.
- `Ø`처럼 현재 공식 아이템명에 없는 Unicode 글자가 slash-zero/좁은 Latin glyph 대신 OCR 결과에 나타나면 정상 identity 문자로 신뢰하지 않는다.
- current catalog에 없는 embedded glyph는 특정 `r`, `0`, `I`, `l` 등으로 강제 치환하지 않고 `?` unknown-glyph evidence로 보존한다.
- 일반 ASCII letter/digit는 fuzzy matcher의 noisy evidence로 유지한다. 실제 공식명에 쓰이는 정상 `r`, `0`, `I`, `l` 등은 제거하지 않는다.
- 따옴표, 하이픈, 괄호 등 실제 공식 아이템명에서 사용하는 기호는 catalog inventory에 의해 보존한다.
- current catalog 전체에서 pattern candidate가 유일하고 runner-up과 충분히 분리된 경우에만 1~2 unknown glyph를 제한적으로 복구한다.
- wildcard pattern이 여러 공식 아이템과 일치하거나 global separation이 부족하면 fail-closed 한다.
- 일반 fuzzy matcher confidence/margin은 낮추지 않았다.

## 변경하지 않은 계약

- structural floor: `0.34`
- trusted header floor: `0.68`
- Windows ko-KR OCR primary/deep
- Tarkov-font visual recovery
- current official Korean full item catalog authority
- false positive보다 miss를 선호하는 fail-closed 원칙
- OCR production field는 `item_name` 하나
- 최고 상점가 / flea avg24hPrice / slots / RequiredTotal은 Item ID 확정 후 `mapped_data`
- scan-time network 없음
- game memory read / DLL injection / packet interception 없음
- Scanner 속도 최적화는 이번 범위에서 제외

## Ground Truth / 설계 근거

v1.4.2까지의 실제 교정 데이터에서 다음 문제를 확인했다.

- 실제 detail rectangle과 잘못된 stash/inventory rectangle이 크게 겹칠 수 있다.
- geometry-only IoU dedupe가 semantic validation 전에 정답 후보를 제거하면 이후 X/돋보기 검증 기회 자체가 사라진다.
- 상세창 높이는 item/stat panel에 따라 달라져 historical aspect prior를 identity 조건으로 쓰면 tall/large window를 과도하게 제거할 수 있다.
- WinRT OCR은 좁은 Latin glyph나 slash-zero 형태를 current catalog에 없는 Unicode letter/symbol로 출력할 수 있다.
- catalog-impossible glyph를 정상 문자로 신뢰할 이유는 없지만 특정 문자로 전역 치환하면 false positive 위험이 있다.

설계 결정:

- `docs/DECISION_SCANNER_SEMANTIC_CANDIDATE_AND_OCR_ALPHABET_2026-08-24.md`

## 회귀 방지

추가 자동 회귀:

- tall detail rectangle proposal 보존
- high-IoU이지만 edge ownership이 다른 rectangle proposals 보존
- catalog-impossible Unicode letter를 정상 identity로 신뢰하지 않음
- impossible embedded glyph를 unknown-glyph pattern으로 보존
- unique 1-unknown bounded recovery
- 충분한 known context의 unique 2-unknown bounded recovery
- ambiguous wildcard fail-closed

사용자 screenshot bytes 자체는 source test fixture로 저장하지 않는다.

## 구현 검증

Scanner 개선 PR #165:

```text
final head: df92c9e920598fb4a7d2950ea52697514a93e0e9
final CI: 32660568132 — SUCCESS
merge: 2ae6cd52b5b7783e92887becfdd24bcd96cfca3c
279 passed / 0 failed / 0 skipped
Windows build/publish: SUCCESS
Product UI + Map + Scanner smoke: SUCCESS
graceful shutdown: SUCCESS
```

Release-prep PR #166:

```text
head: 827470180d6d14cac14e791320a3fcf0e4445b78
CI: 32674399495 — SUCCESS
279 passed / 0 failed / 0 skipped
build/publish/package smoke: SUCCESS
```

Exact v1.4.3 public release source:

`f7e3870c81a7d7be025f1fe56d5b7f607546b250`

이 SHA는 Desktop 1.4.3, FIRST_RUN 1.4.3, Scanner 수정 및 모든 자동 테스트를 포함한 공개 바이너리의 유일한 source다.

## 공개 릴리즈 검증

Release controller:

```text
controller main SHA: da65a11ef2f63b4462df1b081fb2fdc996265338
release run: 32674812862 — SUCCESS
```

Independent public verifier:

```text
verifier run: 32675069359 — SUCCESS
verification status commit: b9b7f78876978be8aeb0dad4919a90ed47e5b319
```

검증된 공개 상태:

```text
version: v1.4.3
release source: f7e3870c81a7d7be025f1fe56d5b7f607546b250
public tag source: f7e3870c81a7d7be025f1fe56d5b7f607546b250
asset: Junhyun-Helper-v1.4.3-win-x64.zip
bytes: 80,389,336
SHA-256: fa5da9f2a6b9ea62f8a9a2ddfb1062bed81609fb96516a01089238b92067a8be
ProductVersion: 1.4.3+f7e3870c81a7d7be025f1fe56d5b7f607546b250
automated tests: 279 passed / 0 failed / 0 skipped
public/latest: VERIFIED
exact public tag source: VERIFIED
public ZIP re-download: VERIFIED
public SHA256SUMS: VERIFIED
public package layout: VERIFIED
public-downloaded EXE Product UI + Map + Scanner smoke: SUCCESS
graceful shutdown: SUCCESS
```

영구 검증 증거:

- `docs/.release-v1.4.3-status.json`

Verifier는 release controller와 별도로 public ZIP과 `SHA256SUMS.txt`를 다시 다운로드해 tag/source, SHA-256, package layout, ProductVersion, FIRST_RUN identity와 실제 EXE smoke를 확인했다.

## 릴리즈 게이트 결과

1. release-prep PR CI — **VERIFIED**
2. exact release source SHA — **VERIFIED**
3. tag `v1.4.3` exact source — **VERIFIED**
4. exact-source build + 279 tests — **VERIFIED**
5. win-x64 self-contained single-file publish — **VERIFIED**
6. package root audit / ProductVersion — **VERIFIED**
7. packaged EXE smoke / graceful shutdown — **VERIFIED**
8. ZIP + SHA256SUMS — **VERIFIED**
9. draft re-download / hash / layout / EXE smoke — **VERIFIED**
10. public stable/latest — **VERIFIED**
11. public re-download / hash / SHA256SUMS / layout — **VERIFIED**
12. public-downloaded EXE smoke — **VERIFIED**
13. independent public verifier — **VERIFIED**
14. durable status JSON — **VERIFIED**
15. one-shot release/verifier workflows — **REMOVED AFTER VERIFICATION**

v1.4.3 공개 릴리즈에는 남은 release blocker가 없다.

## 알려진 잔여 과제

- 일부 historical case에서 semantic header/title이 복구되어도 structural bottom 보존 때문에 detail bottom이 실제보다 낮게 남을 수 있는 문제는 추가 Ground Truth가 필요하다.
- diagnostics의 `TITLE_ANCHOR_INCOMPLETE` stage classification이 일부 경우 OCR/preprocessing failure로 잘못 분류될 수 있다.
- proposal policy는 현재 Ground Truth/synthetic regression으로 검증했지만 추가 해상도/DPI/UI 배치 live validation이 필요하다.
- `r`, `0`, complex Hangul 인식 자체를 OCR-engine 수준에서 일반적으로 해결한 것은 아니다. current-catalog impossible-glyph filtering과 bounded recovery를 강화한 것이다.
- 일반 matcher threshold 완화는 추가 Ground Truth 없이는 수행하지 않는다.
- Scanner speed optimization은 안정성 검증 이후로 계속 보류한다.
