# Scanner Ground Truth / Correction Development Contract

기준일: 2026-08-25
상태: **v1.7.0 PUBLIC RELEASE VERIFIED / ACTIVE**

이 문서는 준현 헬퍼 Scanner의 실사용 성공·실패를 재현 가능한 개발 데이터로 전환하는 공식 계약이다. `docs/SCANNER.md`의 production recognition pipeline을 폐기하지 않고 그 위에 진단, 사용자 교정, Ground Truth, replay regression 체계를 둔다. v1.7.0부터 recognition log의 교정 action도 동일 Case ID의 저장된 diagnostic evidence 또는 정확히 같은 current frame이 있을 때만 이 계약으로 진입한다.

## 1. 목표

Scanner는 Tarkov UI 전용 closed-domain recognizer다.

```text
capture
→ detail proposals
→ inspect-header semantic lock
→ item-name ROI
→ Windows ko-KR OCR
→ optional user substitution / visual corroboration
→ current official item catalog matching
→ Item ID or fail closed
→ local mapped presentation
→ user review/correction
→ reviewed Ground Truth dataset
→ full-pipeline replay regression
→ evidence-based algorithm change
```

평가 층:

- capture health
- detail proposal recall/ranking
- close-X semantic detection
- magnifier semantic detection
- header lock
- item-name ROI localization
- OCR recognition
- user substitution effect
- catalog matching
- visual recovery
- final Item ID
- mapped presentation
- overlay/stale-state timing

핵심 정확도 지표는 최종 Item identity correctness다.

## 2. Production field contract

화면에서 OCR하는 production text field는 `item_name` 하나다.

다음은 화면 숫자 OCR이 아니다.

- highest trader sell price
- best trader name
- flea 24h average
- slots / price per slot
- current required count

이 값은 `item_name → Item ID` 확정 뒤 local trusted data에서 계산/조회한다.

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

현재 존재하지 않는 숫자 OCR ROI를 억지로 만들지 않는다.

## 3. Case ID / evidence link

진단 capture에는 process-local unique Case ID를 부여한다.

```text
case_YYYYMMDDHHMMSSfff_000142
```

동일 Case ID가 다음을 연결한다.

- latest diagnostic frame
- scanner.log
- Case directory
- case.json
- candidate_selection.json
- correction window
- regression output

시간 추정으로 image/log를 맞추지 않는다.

## 4. Storage

Root:

```text
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
```

Representative layout:

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
      ├─ candidate_selection.json
      └─ item_name/
         ├─ detected_roi.png
         ├─ corrected_roi.png
         ├─ processed_roi.png
         └─ processed_variant_*.png
```

`full.png`는 full-pipeline replay의 핵심 원본 evidence다.

## 5. Automatic diagnostic Case vs Ground Truth

Automatic diagnostic persistence는 recognition result를 바꾸지 않는 best-effort background work다.

대표 automatic sample:

- detail/header lock failure
- fail-closed identity failure
- low-confidence success
- bounded deterministic normal sample

Automatic Case 기본 metadata:

```text
retention = automatic_sample
review_status = unreviewed
```

**Automatic Case는 Ground Truth가 아니다.**

Ground Truth는 사용자가 review/correction을 완료한 Case만 의미한다.

## 6. Current correction UX — image-first candidate selection

현재 기본 교정 흐름:

```text
Case image 열기
→ detail rectangle candidate 직접 클릭
→ red close-X candidate 직접 클릭
→ magnifier candidate 직접 클릭
→ item-name ROI candidate 직접 클릭
→ correct item/text
→ save
```

드롭다운 candidate list보다 **image 위 candidate rectangle 직접 선택**을 기본 UX로 한다.

각 stage에서:

- correct candidate 존재 → candidate box 클릭
- detector가 object를 만들지 못함 → explicit `없음`
- candidate set에 정답 geometry가 없음 → manual rectangle fallback

Candidate selection은 manual rectangle의 대체가 아니라 recall/ranking evidence를 더 잘 보존하기 위한 기본 경로다.

사용자는 JSON/candidate rank/coordinate를 직접 편집하지 않는다.

## 7. Image auto-fit / coordinate contract

큰 Tarkov screenshot도 correction window/monitor viewport 안에 전체가 보이도록 auto-fit한다.

중요:

- displayed scale과 Ground Truth coordinate를 분리한다.
- selection hit-test에서 display coordinate → original image coordinate 변환을 명시적으로 수행한다.
- saved ROI는 항상 **original full.png pixel coordinate**다.
- 화면 축소 때문에 GT precision을 잃지 않는다.
- manual rectangle도 original pixel coordinate로 변환해 저장한다.

UI scale change는 dataset geometry 의미를 바꾸면 안 된다.

## 8. Saved Case re-edit

현재 `교정 데이터 관리`에서 기존 Case를 다시 열 수 있다.

복원 source:

```text
case.json
+ full.png
+ candidate_selection.json
```

복원 대상:

- Case ID
- original image
- program candidate/result
- existing Ground Truth item/text
- candidate selections
- corrected geometry where representable

Re-edit save contract:

- same Case ID 유지
- reviewed Ground Truth 갱신
- candidate_selection 갱신
- dataset index/summary는 실제 Case state에서 재생성 가능
- restore failure 시 original Case를 overwrite/delete하지 않음
- partial/corrupt evidence는 fail closed

## 9. Candidate Ground Truth metadata

각 selection은 가능한 경우 다음을 보존한다.

- candidate ID
- candidate type
- rank
- score
- rectangle geometry
- normalized geometry
- selection mode
- explicit none
- manual fallback

Failure analysis:

- proposal recall failure
- proposal ranking failure
- close-X semantic failure
- magnifier semantic failure
- title ROI candidate failure
- OCR/matcher failure

을 분리한다.

## 10. Case metadata

### Identity / environment

- Case / dataset / program / scanner version
- timestamp
- capture mode/source
- capture width/height/origin
- DPI/system environment

### Geometry / semantic evidence

- detected/corrected detail ROI
- structural score/reason
- header score/reason
- close-X evidence
- magnifier evidence
- detected/corrected item-name ROI
- normalized ratios / deltas
- candidate ID/rank/score/geometry

### OCR / matcher evidence

- raw OCR
- user-substituted OCR
- normalized/sanitized matcher input
- pass information
- matcher top candidates
- program official-name result
- program Item ID
- confidence / second / margin / reason

### User truth / pipeline

- Ground Truth item/text
- corrected rectangles/selections
- review status
- program-correct state
- pipeline.stage
- ground_truth_error_type

### Presentation / artifacts

- mapped presentation where Item ID exists
- artifact paths
- retention/review metadata

## 11. Matcher top candidates

최종 top1만 저장하지 않는다.

Example:

```json
"top_candidates": [
  { "rank": 1, "item_id": "...", "official_name": "...", "score": 0.941 },
  { "rank": 2, "item_id": "...", "official_name": "...", "score": 0.918 }
]
```

분석 목적:

- truth가 top-N 안에 있었는지
- top1/top2 margin 구조가 부족했는지
- OCR evidence가 후보군을 어떻게 좁혔는지

## 12. Ground Truth error vs pipeline observation

Ground Truth high-level error types:

- DETAIL_WINDOW_DETECTION
- FIELD_LOCALIZATION
- OCR_RECOGNITION
- CANDIDATE_MATCHING
- PARSING
- DATA_MAPPING
- UNKNOWN_MULTIPLE
- NONE

원인이 여러 layer이면 억지로 하나에 귀속하지 않고 `UNKNOWN_MULTIPLE`을 사용한다.

Automatic pipeline observation:

- DETAIL_WINDOW_DETECTION_FAILED
- DETAIL_HEADER_LOCK_FAILED
- OCR_OR_PREPROCESSING_FAILED
- IDENTITY_MATCH_FAILED
- FINALIZED
- NOT_RUN

Automatic observed stage를 Ground Truth cause로 자동 변환하지 않는다.

## 13. User OCR substitution evidence — settings schema v6

Scanner display settings schema는 v6이다.

Substitution engine data flow:

```text
raw OCR
→ user substitutions (single ordered pass)
→ normalized matcher input
→ final match
```

Case/replay에서 별도 보존:

- raw OCR
- substituted OCR
- normalized/sanitized text
- matched official name

Raw OCR을 substitution result로 덮어쓰지 않는다.

v5 이하 user substitution data는 v6 migration에서 보존한다.

Substitution statistics가 product-wide automatic correction table을 자동 생성하는 근거는 아니다.

## 14. Logs / dataset management separation

`scanner.log`는 bounded runtime diagnostic이고 Ground Truth dataset은 image/ROI/prediction/truth/candidate evidence persistence다.

현재 일반 Scanner page는 user-facing log-delete/developer export buttons를 노출하지 않는다.

Internal log retention/cleanup과 Ground Truth Case management는 계속 별도 책임이다.

Correction dataset manager는:

- Case list
- Case re-open/re-edit
- selected Case delete
- full dataset delete

를 담당한다.

Case 삭제 후 index/summary는 remaining directories를 다시 스캔해 재생성한다. 연속 번호에 의존하지 않는다.

## 15. Export / statistics

기존 diagnostic export capability가 사용되는 개발 흐름에서는 ZIP 하나로 dataset/evidence/log를 전달할 수 있다.

Representative contents:

- README
- summary.md/json
- environment.json
- dataset.jsonl
- regression.json/md
- cases/**
- scanner.log* if included

자동 통계 예:

- total / automatic / reviewed counts
- reviewed final accuracy
- Ground Truth error types
- pipeline stages
- detail/title ROI deltas
- candidate rank/recall pattern
- OCR confusion pattern
- matcher top-candidate distribution

Final accuracy denominator에는 user-confirmed final Item truth가 있는 Case만 사용한다.

## 16. OCR preprocessing evidence

Production OCR은 title size/pass에 따라 scale/contrast/binary/inverse preprocessing을 사용할 수 있다.

가능한 범위에서 processed ROI/variants를 Case에 보존한다.

남은 개선 여지:

- OCR engine이 실제 소비한 processed bitmap 자체를 직접 evidence로 발행

Preprocessing 변경 시 persistence/replay evidence 의미가 유지되는지 확인한다.

## 17. Full-pipeline replay regression

Reviewed `full.png`를 current production pipeline에 다시 투입한다.

```text
full.png
→ current proposals
→ current semantic header
→ current title ROI
→ current OCR/deep
→ current user substitution
→ current visual recovery
→ current catalog matching
→ final recognition
```

Result:

- STILL_CORRECT
- SOLVED
- STILL_FAILING
- REGRESSION
- ERROR

Possible output evidence:

- current raw/substituted/normalized OCR
- Item ID/name
- confidence/second/margin
- geometry
- candidate evidence
- top candidates
- mapped-data comparison

`program_correct=true`였던 reviewed Case가 현재 실패하면 전체 average가 좋아져도 REGRESSION이다.

Mapped price/flea data는 latest Game Content로 정상 변화할 수 있다. `MAPPED_DATA_CHANGED` 자체는 OCR/identity regression이 아니다.

## 18. Retention / privacy

Reviewed Ground Truth:

**automatic delete 금지.**

Automatic Case delete eligibility:

```text
retention == automatic_sample
AND review_status == unreviewed
```

Default bounds:

```text
max age: 30 days
max automatic cases: 300
max automatic bytes: 512 MiB
recent safety window: 2 hours
```

- recent Case 자동 삭제 금지
- delete 직전 metadata re-read
- corrupt/unknown metadata preserve fail closed
- retention delete는 diagnostic log에 기록

Privacy/user control:

- full game/display pixels가 Case에 포함될 수 있음
- Case count/storage 확인 가능
- selected/all delete 가능
- exported copy는 internal delete가 자동 제거하지 않음
- dataset persistence failure가 Scanner recognition result를 바꾸면 안 됨

## 19. Current completed scope

Ground Truth workflow에서 구현된 범위:

1. Case ID/stage/log evidence linkage
2. original/detail/title/preprocessing/annotation artifacts
3. automatic failure/low-confidence/normal sampling
4. user-reviewed Ground Truth separation
5. candidate-first detail/close/magnifier/title ROI correction
6. candidate box image-direct selection
7. image auto-fit + original coordinate preservation
8. explicit `없음`
9. manual rectangle fallback
10. correct item/text
11. candidate ID/rank/score/geometry persistence
12. matcher top candidates
13. raw/substituted/normalized OCR separation
14. summary/index/statistics
15. Case list/selective/full delete
16. saved Case reopen/re-edit
17. same Case ID reviewed update
18. full.png replay regression
19. automatic unreviewed retention bounds

## 20. Current development loop

공개 검증 후:

```text
real Tarkov usage
→ representative correct Case review
→ miss/wrong identity immediate correction
→ reviewed Ground Truth accumulation
→ failure-stage clustering
→ affected stage only modification
→ full replay
→ REGRESSION=0
→ PATCH candidate
```

관찰 우선순위:

- resolution/DPI/UI scale
- detail proposal recall/ranking
- close-X/magnifier semantic miss
- short/sparse title OCR
- r / 0 / slash-zero-like glyph / complex Hangul
- near-name ambiguity
- mapped market completeness
- rapid Item transition stale-state isolation
- telemetry latency bottleneck

## 21. 금지 사항

- 한 번의 user coordinate correction을 global offset으로 즉시 적용
- unreviewed automatic Case를 truth로 취급
- observed pipeline stage를 GT cause로 자동 변환
- 존재하지 않는 숫자 OCR field를 억지로 추가
- GT 없이 detector/header/matcher threshold 완화
- dataset persistence failure를 Item identity 판단에 섞기
- OCR 하나의 result를 최종 권위로 승격
- metadata-only comparison을 full-pipeline regression이라 부르기
- average accuracy만 보고 REGRESSION 무시
- reviewed GT를 automatic retention 대상으로 분류
- candidate selection을 유일한 correction path로 강제
- substitution 결과로 raw OCR overwrite
- UI display scale 좌표를 original GT pixel coordinate로 저장
- saved Case restore failure에서 existing GT overwrite/delete

특히 structural floor `0.34`, trusted header floor `0.68`, candidate caps, matcher confidence/margin은 새로운 reviewed Ground Truth evidence 없이 변경하지 않는다.
