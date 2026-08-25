# Scanner Ground Truth / Correction Development Contract

기준일: 2026-08-26
상태: **v1.7.8 PUBLIC STABLE / MAINTENANCE ONLY**

이 문서는 준현 헬퍼 Scanner의 실사용 성공·실패를 재현 가능한 개발 데이터로 전환하는 공식 계약이다. `docs/SCANNER.md`의 production recognition pipeline을 폐기하지 않고 그 위에 진단, 사용자 교정, Ground Truth, replay regression 체계를 둔다.

가장 중요한 현재 계약은 **runtime diagnostic evidence와 durable Ground Truth의 소유권을 분리하는 것**이다.

```text
runtime frame / failure
≠ durable Ground Truth

user explicitly opens correction
+ user explicitly saves
= reviewed durable Ground Truth
```

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
→ optional user review/correction
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

진단 capture에는 process-local unique Case ID를 부여할 수 있다.

```text
case_YYYYMMDDHHMMSSfff_000142
```

Case ID는 가능한 경우 다음 evidence를 연결한다.

- latest exact in-memory diagnostic frame
- scanner.log activity
- 사용자가 저장한 durable Case directory
- case.json
- candidate_selection.json
- correction window
- regression output

시간 추정으로 다른 frame/image/log를 대신 연결하지 않는다.

v1.7.7 이후 일반 monitoring에서 Case ID가 존재한다는 사실만으로 durable directory가 존재하는 것은 아니다.

## 4. Storage ownership

Durable reviewed dataset root:

```text
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
```

사용자가 교정 저장한 Case의 representative layout:

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

정상 runtime monitoring은 위 image dataset을 자동으로 계속 생성하지 않는다.

## 5. Runtime diagnostic frame vs Ground Truth

현재 runtime flow:

```text
capture / recognition
→ latest exact diagnostic frame in memory
→ bounded runtime text diagnostics
→ user chooses current correction
→ user reviews/corrects
→ user saves
→ durable reviewed Ground Truth
```

상세창 없음, detail/header lock failure, OCR/matcher failure, ambiguity, low-confidence result 또는 반복 stationary failure만으로 durable Case를 자동 저장하지 않는다.

**Ground Truth는 사용자가 review/correction을 완료하고 저장한 Case만 의미한다.**

Automatic pipeline observation은 truth가 아니다.

## 6. Current correction UX — image-first candidate selection

Scanner 메인 상단의 `현재 결과 교정`은 `ScannerRecognitionDebugStore`가 보존한 최신 exact in-memory frame을 연다.

현재 기본 교정 흐름:

```text
exact current Case image 열기
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

오래된 activity가 durable reviewed Case도 아니고 현재 exact in-memory frame도 아니면 다른 frame으로 대체해 교정하지 않는다.

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

현재 `교정 데이터 관리`에서 기존 durable Case를 다시 열 수 있다.

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

v1.7.8 reviewed raid Case처럼 사용자에게는 “인식 실패”로 보이더라도 raw OCR이 empty이고 `HEADER_CLOSE_NOT_LOCKED`라면 실제 failure stage는 OCR 이전 semantic header일 수 있다. Ground Truth와 pipeline stage를 분리해 기록한다.

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

`scanner.log`는 bounded runtime diagnostic이고 Ground Truth dataset은 user-reviewed image/ROI/prediction/truth/candidate evidence persistence다.

현재 일반 Scanner page는 user-facing log-delete/developer export buttons를 노출하지 않는다.

Internal log retention/cleanup과 Ground Truth Case management는 별도 책임이다.

Correction dataset manager는:

- Case list
- Case re-open/re-edit
- selected Case delete
- full dataset delete

를 담당한다.

Case 삭제 후 index/summary는 remaining directories를 다시 스캔해 재생성한다. 연속 번호에 의존하지 않는다.

## 15. Export / support bundle separation

Scanner **성능 진단 자료 내보내기**는 runtime 환경/성능 trace와 bounded text logs를 지원용 ZIP으로 제공한다. 사용자의 Ground Truth image/dataset을 자동 포함하지 않는다.

Ground Truth dataset을 개발 evidence로 전달하는 별도 흐름이 존재하는 경우에도 사용자가 명시적으로 선택한 reviewed data만 대상으로 하며, runtime support bundle과 같은 것으로 취급하지 않는다.

자동 통계 예:

- reviewed total count
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

가능한 범위에서 user-saved Case에 processed ROI/variants를 보존한다.

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

### Reviewed Ground Truth

**automatic delete 금지.**

### New runtime monitoring

새 버전은 automatic diagnostic image Case를 durable storage에 생성하지 않는다.

### Legacy automatic Case cleanup

이전 버전에서 이미 존재하는 Case는 다음을 모두 증명할 때만 자동 삭제할 수 있다.

```text
retention == automatic_sample
AND review_status == unreviewed
AND recent write safety window >= 5 minutes
AND pre-delete metadata/state re-read confirms unchanged state
```

- reviewed/manual Case 자동 삭제 금지
- corrupt/unknown/unreadable metadata preserve fail closed
- delete 직전 state가 달라지면 보존
- cleanup은 recognition hot path 밖에서 수행
- legacy cleanup에 과거 30일/300개/512MiB cap을 current durable persistence 정책으로 사용하지 않음

Privacy/user control:

- user-saved full game/display pixels가 Case에 포함될 수 있음
- Case count/storage 확인 가능
- selected/all delete 가능
- exported copy는 internal delete가 자동 제거하지 않음
- dataset persistence failure가 Scanner recognition result를 바꾸면 안 됨

공식 결정: `docs/DECISION_SCANNER_STORAGE_AND_HOTKEYS_2026-08-26.md`.

## 19. Current completed scope

Ground Truth workflow에서 현재 구현된 범위:

1. Case ID/stage/log evidence linkage
2. latest exact runtime frame in-memory retention
3. user-selected durable correction save
4. original/detail/title/preprocessing/annotation artifacts for saved Case
5. user-reviewed Ground Truth separation
6. candidate-first detail/close/magnifier/title ROI correction
7. candidate box image-direct selection
8. image auto-fit + original coordinate preservation
9. explicit `없음`
10. manual rectangle fallback
11. correct item/text
12. candidate ID/rank/score/geometry persistence
13. matcher top candidates
14. raw/substituted/normalized OCR separation
15. summary/index/statistics
16. Case list/selective/full delete
17. saved Case reopen/re-edit
18. same Case ID reviewed update
19. full.png replay regression
20. fail-closed cleanup of legacy automatic unreviewed Cases
21. runtime support bundle and Ground Truth dataset lifetime separation

## 20. Current development loop

현재 maintenance loop:

```text
real Tarkov usage
→ representative correct result review
→ miss/wrong identity correction
→ reviewed Ground Truth accumulation
→ failure-stage clustering
→ affected stage only modification
→ full replay
→ REGRESSION=0
→ PATCH candidate
→ full Windows CI/publish/smoke/package gate
→ public release readback
```

관찰 우선순위는 특정 glyph나 stage를 미리 가정하지 않는다. 실제 reviewed Case/support evidence가 가리키는 failure stage를 먼저 확정한다.

## 21. 금지 사항

- 한 번의 user coordinate correction을 global offset으로 즉시 적용
- unreviewed automatic/runtime diagnostic을 truth로 취급
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
- 오래된 activity에 다른 current frame을 대신 연결해 교정
- runtime failure를 durable Case로 자동 축적하는 정책 재도입

특히 structural floor `0.34`, trusted header floor `0.68`, candidate caps, matcher confidence/margin은 새로운 reviewed Ground Truth evidence 없이 변경하지 않는다.
