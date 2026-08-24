# Scanner Ground Truth / Correction Development Contract

기준일: 2026-08-24
상태: **v1.5.0 ACTIVE / PUBLIC VERIFIED**

이 문서는 준현 헬퍼 Scanner의 실사용 성공·실패를 재현 가능한 개발 데이터로 전환하는 공식 계약이다. `docs/SCANNER.md`의 production recognition pipeline을 폐기하지 않고 그 위에 진단, 사용자 교정, Ground Truth, export, regression 체계를 둔다.

## 1. 목표

Scanner는 범용 OCR이 아니라 Tarkov UI 전용 closed-domain recognizer로 취급한다.

```text
capture
→ detail proposals
→ inspect-header semantic lock
→ item-name ROI
→ Windows ko-KR OCR / optional user substitution / Tarkov-font corroboration
→ current official item catalog matching
→ Item ID or fail closed
→ local mapped presentation data
→ user verification/correction
→ reviewed Ground Truth dataset
→ full-pipeline replay regression
→ evidence-based algorithm change
```

성능 평가는 OCR 문자열 정확도만으로 끝내지 않는다.

다음 층을 분리한다.

- capture health
- detail proposal recall/ranking
- close-X semantic detection
- magnifier semantic detection
- header lock
- item-name ROI localization
- OCR recognition
- user substitution effect
- catalog candidate matching
- final Item ID
- mapped presentation

최종적으로 사용자에게 표시되는 Item identity가 맞는지를 핵심 정확도 지표로 본다.

## 2. Production 필드 계약

현재 Scanner가 게임 화면에서 OCR하는 production text field는 `item_name` 하나다.

다음 값은 화면 숫자 OCR이 아니다.

- 최고 상인 판매가
- 최고가 상인명
- 플리마켓 24시간 평균가
- slots / price per slot
- 현재 필요한 개수

이 값은 `item_name → Item ID` 확정 뒤 기존 JunhyunHelper local trusted data에서 계산/조회한다.

Dataset도 분리한다.

```text
localization/OCR field: item_name
mapped_data:
  highest trader sell price
  best trader name
  flea average
  slots
  trader/flea price per slot
  RequiredTotal
```

현재 존재하지 않는 가격/필요 개수 OCR ROI를 요구사항 문구에 맞추기 위해 인위적으로 만들지 않는다. 향후 실제 화면 숫자 인식이 별도 제품 요구사항으로 확정될 때만 숫자 recognizer/grammar를 설계한다.

## 3. Case ID와 evidence 연결

진단 capture에는 프로세스 내 고유 Case ID를 부여한다.

```text
case_YYYYMMDDHHMMSSfff_000142
```

동일 Case ID가:

- latest diagnostic frame
- `scanner.log`
- Case directory
- `case.json`
- correction window
- regression output

을 연결한다.

개발자는 이미지와 로그를 시간 추정으로 맞추지 않는다.

## 4. 저장 위치 / 기본 구조

```text
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
```

대표 구조:

```text
diagnostics/
├─ README.md
├─ environment.json
├─ dataset.jsonl
├─ summary.json
├─ summary.md
├─ regression.json
├─ regression.md
└─ cases/
   └─ case_.../
      ├─ full.png
      ├─ detail_window.png
      ├─ annotated.png
      ├─ case.json
      └─ item_name/
         ├─ detected_roi.png
         ├─ corrected_roi.png
         ├─ processed_roi.png
         └─ processed_variant_*.png
```

`full.png`는 detector/OCR 전처리 전 capture evidence다. 새 알고리즘으로 과거 Case를 실제 replay하려면 원본 full frame이 필요하다.

## 5. Automatic diagnostic Case와 Ground Truth 구분

Automatic diagnostic persistence는 Scanner recognition 결과를 바꾸지 않는 background best-effort다.

대표 automatic sample 대상:

- detail detection/header lock failure
- identity failure/fail-closed semantic result
- 성공했지만 저신뢰 결과
- 정상 성공의 bounded deterministic sample

동일 source/title/reason/ROI fingerprint 반복 저장은 프로세스 내에서 억제할 수 있다.

Automatic Case는 기본적으로:

```text
retention = automatic_sample
review_status = unreviewed
```

다.

**Automatic Case를 Ground Truth 정답 또는 오류 원인으로 취급하지 않는다.**

Ground Truth는 사용자가 review/correction을 완료한 Case만 의미한다.

## 6. Candidate-first 사용자 교정 UX

v1.5.0부터 교정 기본 경로는 manual drawing이 아니라 **현재 detector가 실제로 생성한 evidence 후보 선택**이다.

권장 흐름:

```text
최신 Case 열기
→ detail rectangle 후보 선택
→ red close-X 후보 선택
→ magnifier 후보 선택
→ item-name ROI 후보 선택
→ 정답 item/text 지정
→ 저장
```

각 semantic object 단계에서:

- 후보 중 정답 선택
- detector가 정답을 생성하지 않았다면 `없음`
- 후보가 없거나 geometry 자체를 직접 지정해야 하면 manual rectangle fallback

을 지원한다.

Manual rectangle은 제거하지 않는다. Candidate 선택이 기본 경로이고 manual drawing은 recall miss를 표현하기 위한 fallback이다.

사용자는 JSON, 좌표, candidate rank, 파일명을 직접 편집하지 않는다.

### 빠른 접근성

Scanner 일반 화면의 `현재 결과 교정`과 Mini Scanner 우클릭 `현재 결과 교정`이 최신 debug snapshot을 correction window로 전달한다.

오인식 직후 몇 초 안에 해당 Case를 reviewed Ground Truth로 남기는 것을 목표로 한다.

## 7. Candidate Ground Truth metadata

각 semantic candidate 선택은 가능한 경우 다음을 보존한다.

- candidate ID
- candidate type
- rank
- score
- rectangle geometry
- normalized geometry
- 선택 여부
- explicit `none` 여부
- manual fallback 여부

이를 통해 단순히 “ROI가 틀렸다”가 아니라 다음을 분리한다.

- proposal recall failure
- proposal ranking failure
- close-X semantic failure
- magnifier semantic failure
- title ROI candidate failure
- OCR/matcher failure

## 8. Case metadata

`case.json`은 최소 다음 종류의 evidence를 보존한다.

### Identity / environment

- Case / dataset / program / scanner version
- timestamp
- capture mode/source
- capture width/height/origin
- system DPI

### Geometry / semantic evidence

- detected/corrected detail ROI
- structural score/reason
- header score/reason
- close-X candidate/evidence
- magnifier candidate/evidence
- detected/corrected item-name ROI
- normalized ratios / deltas
- candidate ID/rank/score/geometry

### OCR / matcher evidence

- raw OCR text
- user-substituted OCR text when applicable
- normalized/sanitized matcher input
- OCR/deep/visual pass information
- matcher top candidates
- program official-name result
- program Item ID
- confidence / second score / margin / pass / reason

### User truth / pipeline

- user Ground Truth item/text
- user corrected rectangles/semantic selections
- reviewed/unreviewed state
- program-correct state
- `pipeline.stage`
- `ground_truth_error_type`

### Presentation / artifacts

- Item ID가 있을 때 current mapped presentation
- artifact paths
- retention/review metadata

## 9. Matcher top candidates

최종 1위만 저장하지 않는다.

Matcher acceptance 기준을 바꾸지 않은 채 상위 후보를 diagnostic evidence로 전달한다.

예:

```json
"top_candidates": [
  { "rank": 1, "item_id": "...", "official_name": "...", "score": 0.941 },
  { "rank": 2, "item_id": "...", "official_name": "...", "score": 0.918 }
]
```

이 evidence로:

- 정답이 top-N 안에 있었는지
- top1/top2 margin이 구조적으로 부족한지
- OCR text가 후보군을 어디까지 좁혔는지

를 분석한다.

## 10. Ground Truth 오류와 pipeline observation 분리

`pipeline.stage`와 `ground_truth_error_type`은 의미가 다르다.

### 사용자-reviewed Ground Truth 오류 유형

- `DETAIL_WINDOW_DETECTION`
- `FIELD_LOCALIZATION`
- `OCR_RECOGNITION`
- `CANDIDATE_MATCHING`
- `PARSING`
- `DATA_MAPPING`
- `UNKNOWN_MULTIPLE`
- `NONE`

Candidate-first evidence가 있으므로 detail/anchor/ROI failure를 이전보다 세분해 분석할 수 있지만, 저장 schema가 하나의 high-level error type을 요구할 경우 원인이 여러 층이면 억지로 하나로 귀속하지 않고 `UNKNOWN_MULTIPLE`을 사용한다.

### Automatic/unreviewed pipeline observation

- `DETAIL_WINDOW_DETECTION_FAILED`
- `DETAIL_HEADER_LOCK_FAILED`
- `OCR_OR_PREPROCESSING_FAILED`
- `IDENTITY_MATCH_FAILED`
- `FINALIZED`
- `NOT_RUN`

Automatic Case는 observed stage를 기록할 뿐 Ground Truth 오류 원인을 생성하지 않는다.

## 11. OCR substitution evidence

Scanner settings schema v5의 user substitution은 Ground Truth 분석에서 raw OCR과 분리한다.

```text
raw OCR
→ user substitution
→ normalized matcher input
→ final match
```

Case/diagnostics에서 가능한 한 다음을 별도로 남긴다.

- raw OCR
- substituted OCR
- normalized/sanitized text
- matched official name

따라서 사용자가 만든 규칙이 실제 반복 오인식을 보정했는지, 오히려 잘못된 후보를 만들었는지 replay에서 확인할 수 있어야 한다.

Raw OCR을 substitution 결과로 덮어쓰지 않는다.

## 12. 일반 로그와 dataset 분리

`scanner.log`는 bounded runtime diagnostics이고 Ground Truth dataset은 원본 이미지/ROI/예측/정답/candidate evidence를 포함하는 별도 persistence다.

- `로그 삭제`: scanner.log(.1), recent activity, latest in-memory diagnostic frame
- `교정 데이터 관리`: Case 목록, 선택 Case 삭제, 전체 dataset 삭제
- export ZIP: 사용자 지정 위치에 생성; program dataset 삭제가 자동 제거하지 않음

Case 삭제 후 dataset index/summary는 실제 remaining directories를 다시 스캔해 재생성한다. 연속 번호에 의존하지 않는다.

## 13. Export

Scanner `교정 데이터 내보내기`는 개발 분석용 ZIP을 생성한다.

예:

```text
ScannerDiagnostics_YYYY-MM-DD.zip
```

포함 가능 내용:

- README
- summary.md / summary.json
- environment.json
- dataset.jsonl
- regression.json / regression.md
- cases/**
- scanner.log* if present

사용자는 ZIP 하나를 개발 분석에 전달하면 된다.

## 14. 자동 통계

Dataset index rebuild 시 대표 집계:

- total cases
- automatic unreviewed cases
- user-reviewed cases
- final-result reviewed cases
- reviewed program-correct cases
- Ground Truth corrections
- reviewed final accuracy
- Ground Truth error types
- pipeline stages
- detail ROI delta statistics
- title ROI delta statistics
- candidate rank/recall pattern
- repeated OCR substitution/insertion/deletion patterns
- matcher top-candidate distribution

OCR confusion은 observed raw/substituted text와 Ground Truth를 edit alignment하여 계산할 수 있다.

예:

```text
0 → o
1 → l
r → ∅
∅ → i
「 → r   # 사용자가 실제 환경에서 검증한 경우의 관찰 통계 예
```

이 통계 자체가 automatic global substitution rule을 생성하는 근거는 아니다.

최종 정확도의 분모는 실제 final Item correctness가 사용자에 의해 확인된 Case만 사용한다. 영역만 교정하고 최종 Item truth가 없는 Case는 final accuracy에 섞지 않는다.

## 15. OCR preprocessing evidence

Production OCR은 title size와 pass에 따라 확대/contrast/binary/inverse 등 preprocessing을 사용한다.

Case 저장 시 가능한 범위에서 processed ROI/variant를 보존한다.

현재 남은 기술 부채:

- 저장 계층이 production preprocessing을 재현하는 경로가 일부 존재함
- OCR engine이 실제 소비한 processed bitmap 자체를 evidence로 직접 발행하는 구조는 개선 여지가 있음

OCR preprocessing 구현을 변경할 때 diagnostic persistence/replay evidence가 같은 의미를 유지하는지 함께 검토한다.

## 16. Full-pipeline regression

Reviewed Ground Truth가 있는 `full.png`를 현재 production pipeline에 다시 투입한다.

```text
full.png
→ current structural proposals
→ current inspect-header semantic lock
→ current title ROI
→ current OCR/deep OCR
→ current user-substitution behavior
→ current Tarkov-font recovery
→ current official catalog matching
→ final recognition
```

실시간 Scanner가 켜져 있으면 one-shot과 같은 serialization/lifecycle boundary에서 안전하게 중단/복귀한다.

Result classification:

- `STILL_CORRECT`
- `SOLVED`
- `STILL_FAILING`
- `REGRESSION`
- `ERROR`

Baseline이 충분하지 않은 reviewed Case는 current-no-baseline 상태로 남길 수 있다.

Regression output에는 가능한 경우:

- current raw/substituted/normalized OCR
- Item ID / official name
- confidence / second score / margin
- predicted/corrected geometry
- candidate evidence
- top candidates
- mapped-data comparison

을 보존한다.

`program_correct=true`였던 Case가 현재 실패하면 전체 평균 정확도가 올라가도 `REGRESSION`이다.

### mapped_data 비교

가격/flea average 등 mapped data는 최신 Game Content로 정상 변화할 수 있다.

`MAPPED_DATA_CHANGED`는 diagnostic signal이며 그 자체로 OCR/identity regression은 아니다.

## 17. Retention / 저장 공간 / 개인정보

### Reviewed Ground Truth

**자동 삭제 금지.**

### Automatic diagnostic Case

Auto-delete eligibility는 다음 둘을 동시에 만족해야 한다.

```text
retention == automatic_sample
AND review_status == unreviewed
```

Default bounds:

```text
max age: 30 days
max automatic cases: 300
max automatic bytes: 512 MiB
recent-case safety window: 2 hours
```

- recent safety window 안의 Case는 자동 삭제하지 않음
- deletion 직전 metadata를 다시 읽어 correction/delete race를 줄임
- corrupt/unknown metadata는 fail closed하여 보존
- retention delete 발생 시 diagnostic log에 기록

### Privacy / user control

- full game/display pixels가 Case에 포함될 수 있음을 숨기지 않음
- Case 목록/용량을 UI에서 확인 가능
- 선택 Case 삭제 가능
- 전체 dataset 삭제 가능
- export ZIP은 사용자 지정 위치에만 생성
- export된 ZIP은 internal dataset delete 기능이 자동 제거하지 않음

Dataset persistence 실패는 Scanner recognition result를 변경하지 않는다.

## 18. v1.5.0 완료 범위

현재 구현된 Ground Truth 관련 범위:

1. Case ID / stage evidence / scanner.log 연결
2. original/detail/title/preprocessing/annotation Case artifact
3. automatic failure/low-confidence/normal sampling
4. user-reviewed Ground Truth separation
5. candidate-first detail/close/magnifier/title ROI correction
6. explicit `없음` semantic-object truth
7. manual rectangle fallback
8. correct item/text input
9. candidate ID/rank/score/geometry persistence
10. matcher top-candidate evidence
11. raw/substituted/normalized OCR evidence separation
12. automatic OCR confusion statistics
13. summary/index/export
14. Case count/storage UI
15. Case list/selective delete/full delete
16. full.png-based replay regression
17. automatic unreviewed retention bounds
18. Mini Scanner quick correction entry

## 19. 다음 개발 단계

v1.5.0 public baseline 이후:

```text
real usage
→ 정상 대표 Case `맞음`
→ miss/wrong identity 즉시 교정
→ reviewed Ground Truth 축적
→ candidate recall/ranking + OCR/matcher cluster 분석
→ 실제 failure stage 특정
→ 해당 stage만 수정
→ full replay regression
→ REGRESSION=0 확인
→ PATCH candidate
```

관찰 우선순위:

- 다양한 resolution/DPI/UI scale
- detail proposal recall/ranking
- close-X/magnifier semantic miss
- short/sparse title OCR
- `r`, `0`, slash-zero-like glyph, complex Hangul
- near-name ambiguity
- mapped market data completeness
- 빠른 Item 전환 stale-result isolation
- telemetry 기반 latency bottleneck

추가 기술 부채 후보:

- OCR engine actual-consumed processed bitmap 직접 evidence 발행
- 반복 reviewed Ground Truth에서 real-render sample dictionary 구축
- live/replay 공통 projection helper 중복 축소
- 환경별 statistics segmentation

## 20. 금지 사항

- 한 번의 사용자 좌표 교정을 즉시 global offset으로 적용하지 않음
- unreviewed automatic Case를 truth로 취급하지 않음
- observed pipeline stage를 Ground Truth 오류 원인으로 자동 변환하지 않음
- 현재 존재하지 않는 숫자 OCR field를 억지로 추가하지 않음
- Ground Truth 없이 detector/header/matcher threshold 완화하지 않음
- dataset persistence failure를 Item identity 판단에 섞지 않음
- OCR 하나의 결과를 최종 권위로 승격하지 않음
- metadata-only comparison을 full-pipeline regression이라고 부르지 않음
- 평균 정확도 상승만 보고 `REGRESSION` Case를 무시하지 않음
- reviewed Ground Truth를 automatic retention 대상으로 분류하지 않음
- candidate selection을 manual correction의 유일한 경로로 강제하지 않음
- user substitution 결과로 raw OCR evidence를 덮어쓰지 않음

특히 structural floor `0.34`, trusted header floor `0.68`, candidate caps, matcher confidence/margin은 새로운 reviewed Ground Truth evidence 없이 변경하지 않는다.
