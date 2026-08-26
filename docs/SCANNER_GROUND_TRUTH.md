# Scanner Ground Truth / Correction Development Contract

기준일: 2026-08-26
상태: **v1.7.10 PUBLIC STABLE / MAINTENANCE ONLY**

이 문서는 준현 헬퍼 Scanner의 실사용 성공·실패를 재현 가능한 개발 데이터로 전환하는 공식 계약이다. `docs/SCANNER.md`의 production recognition pipeline을 폐기하지 않고 그 위에 진단, 사용자 교정, Ground Truth, replay regression 체계를 둔다.

가장 중요한 계약은 **runtime diagnostic evidence와 durable Ground Truth의 소유권을 분리하는 것**이다.

```text
runtime frame / automatic recognition / failure
≠ durable Ground Truth

user explicitly opens correction
+ user explicitly reviews/corrects
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
→ optional user substitution
→ conditional environment normalization
→ current official item catalog matching
→ optional current-pixel visual corroboration
→ Item ID or fail closed
→ local mapped presentation
→ optional user review/correction
→ reviewed Ground Truth dataset
→ full-pipeline replay regression
→ evidence-based algorithm change
```

핵심 정확도 지표는 최종 Item identity correctness다.

평가 stage:

- capture health
- detail proposal recall/ranking
- close-X / magnifier evidence
- header lock
- item-name ROI localization
- OCR recognition
- environment normalization activation/effect
- user substitution effect
- catalog matching / ambiguity
- visual recovery
- final Item ID
- mapped presentation
- Mini Scanner presentation/stale-state timing

## 2. Production field contract

화면에서 OCR하는 production text field는 `item_name` 하나다.

다음은 화면 숫자 OCR이 아니다.

- highest trader sell price
- best trader name
- flea 24h average
- slots / price per slot
- current required count

이 값은 `item_name → Item ID` 확정 뒤 local trusted data에서 동일 Item ID로 계산/조회한다.

## 3. Latest exact frame ownership

정상 monitoring은 automatic durable Case를 만들지 않는다.

```text
runtime capture
→ latest exact diagnostic frame in memory
→ bounded scanner.log activity
→ user may open current correction
```

Latest exact frame이 메모리에서 교체되면 과거 frame을 다른 frame으로 대체하여 교정하지 않는다.

Log entry가 가리키는 원본 durable Case가 없고 latest exact frame도 동일 Case ID가 아니면 correction은 unavailable로 표시한다.

## 4. Durable Ground Truth save

사용자가 correction window에서 명시적으로 저장한 Case만 durable reviewed Ground Truth가 된다.

저장 가능한 정보 예:

- full source image
- selected detail region
- title ROI
- close-X / magnifier region
- raw OCR
- corrected item name
- expected Item ID
- candidate selection
- runtime reason/scores
- correction metadata

저장은 사용자의 명시적 행동이며 normal monitoring의 side effect가 아니다.

## 5. Legacy automatic Case cleanup

과거 버전이 만든 automatic Case는 다음을 모두 증명할 때만 background cleanup한다.

```text
retention = automatic_sample
review_status = unreviewed
recent-write safety >= 5 minutes
pre-delete metadata/state re-read = unchanged
```

다음은 자동 삭제하지 않는다.

- reviewed
- manual
- corrupt/unreadable
- unknown retention/review state
- scan 이후 state가 변경된 Case
- recent write safety window 안의 Case

불확실하면 preserve fail closed한다.

## 6. Ground Truth authority

Ground Truth의 authoritative fields는 사용자가 명시적으로 확정한 값이다.

예:

```text
expected_item_id
corrected_item_name
selected_detail_bounds
selected_title_bounds
selected_candidate
```

프로그램의 과거 자동 추정치는 Ground Truth보다 우선하지 않는다.

사용자가 correction에서 일부만 확정한 경우 확정되지 않은 field를 임의로 정답으로 승격하지 않는다.

## 7. Replay regression

Recognition algorithm을 변경하고 runnable reviewed dataset이 존재하면 release 전 replay에서 다음을 요구한다.

```text
REGRESSION = 0
```

즉 기존 reviewed correct Item ID를 새 코드가 틀리게 만들면 release blocker다.

Replay는 가능하면 failure stage도 비교한다.

- detail proposal
- header lock
- title ROI
- OCR evidence
- normalization activation
- matcher result
- final Item ID

## 8. Procedural / synthetic environment matrix

v1.7.10부터 cross-environment robustness를 위해 deterministic procedural matrix를 별도로 사용한다.

예:

- reference SDR-like luminance
- lifted / washed HDR→SDR-like luminance
- compressed contrast
- low-contrast gamma/rendering shift
- 1080p / 1440p / 4K proportional title raster
- flat/no-contrast negative input

중요:

```text
procedural matrix
≠ user-reviewed Ground Truth
```

Procedural matrix는 환경 변형에 대한 regression/safety coverage다. 실제 Item identity 정답의 권위는 reviewed Ground Truth와 official catalog에 있다.

실제 reviewed source image가 있는 경우 파생 환경 variant를 만들 수 있지만 expected Item ID는 원본 reviewed 정답을 유지한다.

## 9. Private evidence policy

사용자 screenshot/Case image는 사용자-private evidence다.

- CI 편의를 위해 public repository에 commit하지 않는다.
- 필요하면 local reviewed dataset에서 replay한다.
- public CI는 procedural/synthetic smoke와 non-private deterministic fixtures를 사용한다.
- private evidence를 공개 release artifact에 포함하지 않는다.

## 10. Correction UI contract

일반 Scanner 상단 `현재 결과 교정`은 최신 exact in-memory frame을 연다.

Correction flow는 다음을 지원한다.

- source image 확인
- detected/detail/title regions 확인
- candidate 선택 교정
- OCR text 교정
- expected item 선택
- reviewed durable save
- 기존 saved Case reopen/re-edit

Saved Case reopen은 원본과 metadata가 불완전하면 다른 frame으로 대체하지 않고 fail closed한다.

## 11. Dataset quality

Reviewed dataset은 수량보다 정답 품질이 중요하다.

권장 분류:

- program correct
- detail proposal failure
- header lock failure
- title ROI failure
- OCR failure
- normalization-related OCR failure
- catalog matcher failure
- ambiguity/fail-closed correct
- Mini Scanner presentation failure

Correction metadata는 실제 failure stage와 일치해야 한다. 예를 들어 OCR이 실행되기 전에 header lock에서 실패했다면 `OCR_RECOGNITION`으로 단순 분류하지 않는다.

## 12. Maintenance use

새 문제를 받을 때:

```text
reviewed/diagnostic evidence 확보
→ 실제 failure stage 확인
→ root cause 확정
→ affected layer만 수정
→ reviewed replay where runnable
→ procedural regression where applicable
→ full Windows CI / product smoke / package
→ PATCH release
```

Ground Truth를 근거로 하지 않은 threshold/candidate cap 완화는 금지한다.
