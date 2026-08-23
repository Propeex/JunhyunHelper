# RELEASE 1.4.3 — Scanner semantic candidate validation and OCR alphabet hardening

상태: `RELEASE PREP / NOT PUBLIC`

기준일: 2026-08-24

## 범위

v1.4.3은 새 사용자 기능을 추가하지 않는 PATCH 릴리즈다. v1.4.2까지의 실사용 Ground Truth에서 확인된 상세보기 창 후보 탈락 구조와 `r`/`0` 계열 glyph가 Unicode 문자·기호로 오인되는 OCR 문제를 대상으로 한다.

이번 변경은 Scanner의 구조적 책임을 다시 분리한다.

```text
capture
→ rectangle proposals
→ semantic inspect-header validation
→ title ROI
→ OCR
→ current official catalog validation
→ final Item ID
```

geometry는 상세창 identity를 확정하지 않고 가능한 사각형 proposal을 공급한다. 실제 상세창 여부는 기존 close-X + magnifier + header/title evidence 계약이 최종 결정한다.

Scanner 속도 최적화는 정확도 안정화 이후 작업으로 유지하며 이번 릴리즈 범위에 포함하지 않는다.

## 제품 변경

### 상세보기 창 candidate 구조

- 기존 상세창 `aspect ≈ 1.3` 선호는 강한 조건이 아니라 약한 proposal ordering hint로 낮춘다.
- tall/large inspect window가 구조 단계에서 사전 탈락하지 않도록 broad impossible-shape guard만 유지한다.
- 기존 `IoU >= 0.72`만으로 겹치는 후보를 semantic 검증 전에 제거하던 dedupe를 제거한다.
- 서로 크게 겹치더라도 top/bottom/left/right edge가 실질적으로 다르면 모두 semantic 검증까지 보존한다.
- 사실상 같은 사각형을 몇 px 차이로 반복 생성한 edge-jitter proposal만 near-duplicate로 정리한다.
- red-X proximity는 proposal ranking hint로만 사용하고 실제 close-X semantic proof로 간주하지 않는다.
- production OCR 진입은 계속 아래 계약을 모두 요구한다.

```text
TitleAnchorReason == HEADER_FRAME_LOCKED
AND TitleAnchorScore >= 0.68
AND magnifier evidence present
AND close-X evidence present
```

즉 stash/inventory의 큰 사각형이 geometry 점수에서 높더라도 실제 X/돋보기/header evidence를 통과하지 못하면 상세창으로 확정되지 않는다.

### OCR 문자·기호 정책

- Scanner는 범용 문장이 아니라 current official Tarkov item-name catalog를 읽는 closed-domain recognizer로 취급한다.
- punctuation뿐 아니라 Unicode letter/digit도 현재 공식 아이템명에서 실제 사용되는 문자 inventory와 대조한다.
- `Ø`처럼 현재 공식 아이템명에 없는 Unicode 글자가 slash-zero/좁은 Latin glyph 대신 OCR 결과에 나타나면 정상 identity 문자로 신뢰하지 않는다.
- 실제 카탈로그에 없는 embedded glyph는 특정 `r`, `0`, `I`, `l` 등으로 강제 치환하지 않고 `?` unknown-glyph evidence로 보존한다.
- 일반 ASCII letter/digit는 noisy fuzzy evidence로 유지한다. 실제 아이템명에 존재하는 정상 `r`, `0`, `I`, `l` 등을 제거하지 않는다.
- 따옴표, 하이픈, 괄호 등 실제 공식 아이템명에서 사용되는 기호는 current catalog inventory에 의해 보존한다.
- 현재 공식 카탈로그 전체에서 pattern candidate가 유일하고 runner-up과 충분히 분리된 경우에만 1~2 unknown glyph를 제한적으로 복구한다.
- wildcard pattern이 여러 공식 아이템과 일치하거나 global separation이 부족하면 계속 fail-closed 한다.
- 일반 fuzzy matcher minimum confidence/margin은 낮추지 않는다.

## 변경하지 않은 계약

- structural floor: `0.34`
- trusted header floor: `0.68`
- Windows ko-KR OCR primary/deep 정책
- Tarkov-font visual recovery
- current official Korean full item catalog authority
- false positive가 miss보다 더 나쁘다는 fail-closed 원칙
- 가격·플리 평균가·슬롯·필요 개수는 OCR 필드가 아니라 Item ID 확정 뒤 `mapped_data`로 조회하는 구조
- 실시간 스캔 순간 network 미사용
- game memory read / DLL injection / packet interception 미사용
- 연속 Scanner 속도 최적화는 이번 범위에서 제외

## Ground Truth / 설계 근거

v1.4.2까지의 실제 Tarkov 교정 데이터에서 다음 구조적 문제를 확인했다.

- 실제 detail rectangle과 잘못된 stash/inventory rectangle이 크게 겹칠 수 있다.
- geometry-only IoU dedupe가 semantic header validation 전에 정답 후보를 제거하면 이후 X/돋보기 검증 기회 자체가 사라진다.
- 상세창 높이는 item/stat panel 구성에 따라 달라져 historical `aspect ≈ 1.3`을 identity 조건으로 사용하면 tall/large window를 과도하게 제거할 수 있다.
- WinRT OCR은 좁은 Latin glyph나 slash-zero 형태를 current item catalog에 없는 Unicode letter/symbol로 출력할 수 있다.
- closed-domain catalog가 있으므로 catalog-impossible glyph를 정상 문자처럼 신뢰할 이유가 없지만, 특정 문자로 전역 치환하는 것도 false positive 위험이 있다.

따라서 v1.4.3은 geometry를 proposal 단계로 제한하고, 문자 정책도 current catalog가 허용하는 alphabet/symbol inventory를 authority로 사용한다.

설계 결정:

- `docs/DECISION_SCANNER_SEMANTIC_CANDIDATE_AND_OCR_ALPHABET_2026-08-24.md`

## 회귀 방지

추가된 자동 회귀 범위:

- tall detail rectangle proposal이 aspect prior 때문에 제거되지 않음
- high-IoU이지만 bottom/edge ownership이 다른 rectangle proposals를 둘 다 보존
- current catalog에 없는 Unicode letter를 정상 identity 문자로 신뢰하지 않음
- catalog-impossible embedded glyph를 unknown-glyph pattern으로 보존
- unique 1-unknown pattern bounded recovery
- 충분한 known context를 가진 unique 2-unknown pattern bounded recovery
- ambiguous exact wildcard pattern은 fail-closed

사용자 screenshot bytes 자체를 source test fixture로 저장하지 않는다.

## 구현 검증

Scanner 개선 PR #165 final head:

`df92c9e920598fb4a7d2950ea52697514a93e0e9`

PR #165 final CI:

`32660568132 — SUCCESS`

- Windows Desktop build: SUCCESS
- automated tests: `279 passed / 0 failed / 0 skipped`
- Windows x64 self-contained single-file publish: SUCCESS
- packaged Product UI + Map + Scanner smoke: SUCCESS
- graceful shutdown: SUCCESS

PR #165 merge commit:

`2ae6cd52b5b7783e92887becfdd24bcd96cfca3c`

## 릴리즈 준비 상태

Release-prep branch:

`release-v1.4.3-prep`

현재 release-prep에는 다음 identity가 반영되어 있다.

- Desktop project Version: `1.4.3`
- FIRST_RUN first line: `준현 헬퍼 v1.4.3 — Windows x64`
- version classification: PATCH
- release notes: 이 문서

Exact v1.4.3 public release source는 release-prep PR이 최종 CI를 통과하고 main에 merge된 뒤 그 merge SHA로 고정한다.

## 공개 릴리즈 게이트

아직 완료되지 않은 항목:

1. release-prep PR CI success
2. exact release source SHA 고정
3. tag `v1.4.3` exact source 생성
4. exact-source build + 279 tests
5. win-x64 self-contained single-file publish
6. package root audit
7. ProductVersion `1.4.3+<exact source sha>` 검증
8. packaged EXE Product UI + Map + Scanner smoke
9. ZIP + `SHA256SUMS.txt` 생성
10. draft asset re-download / hash / layout / EXE smoke
11. public release publish / latest 확인
12. public asset re-download / hash / layout / ProductVersion / EXE smoke
13. independent public verifier
14. durable `docs/.release-v1.4.3-status.json` 기록
15. one-shot release/verifier workflow 제거

이 게이트가 모두 완료되기 전에는 v1.4.3을 public stable/latest로 기록하지 않는다.

## 알려진 잔여 과제

- v1.4.1 case1 계열에서 semantic header/title이 복구되어도 structural bottom 보존 때문에 detail bottom이 실제보다 약 52 px 아래로 남을 수 있는 문제는 별도 Ground Truth가 더 필요하다.
- diagnostics의 `TITLE_ANCHOR_INCOMPLETE` stage classification이 일부 경우 OCR/preprocessing failure로 잘못 분류될 수 있는 문제는 이번 범위에 포함하지 않는다.
- new proposal policy는 현재 Ground Truth와 synthetic regression으로 검증했지만 추가 해상도/DPI/UI 배치의 live validation이 필요하다.
- `r`, `0`, complex Hangul 인식 자체를 OCR engine 수준에서 일반적으로 해결한 것은 아니다. current-catalog impossible glyph rejection과 bounded catalog recovery를 강화한 것이다.
- 일반 matcher threshold 완화는 추가 Ground Truth 없이는 수행하지 않는다.
- Scanner speed optimization은 안정성 검증 이후로 계속 보류한다.
