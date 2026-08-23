# Scanner Ground Truth / Correction Development Contract

기준일: 2026-08-23

이 문서는 준현 헬퍼 Scanner의 실사용 실패를 재현 가능한 개발 데이터로 전환하는 공식 계약입니다. 기존 `docs/SCANNER.md`의 인식 파이프라인을 폐기하지 않고 그 위에 진단·교정·Ground Truth·Export·회귀 개발 체계를 추가합니다.

## 1. 목표

Scanner는 범용 OCR이 아니라 Tarkov UI 전용 폐쇄형 인식 시스템입니다.

```text
capture
→ detail candidate / inspect-header lock
→ title ROI
→ Windows ko-KR OCR / visual corroboration
→ current official item catalog matching
→ Item ID or fail closed
→ local mapped presentation data
→ user verification/correction
→ Ground Truth dataset
→ developer analysis
→ later algorithm change
→ whole-dataset regression
```

성능 평가는 OCR 문자열만으로 끝내지 않습니다. 최종 Item ID와 사용자에게 표시되는 데이터가 맞는지가 최종 기준이며, 상세창 탐지·ROI·OCR·후보 매칭·데이터 매핑을 서로 분리해 진단합니다.

## 2. 현재 실제 필드 계약

현재 production Scanner가 화면에서 읽는 텍스트 필드는 `item_name` 하나입니다.

- 최고 상인 판매가
- 플리마켓 24시간 평균가
- 슬롯 / 슬롯당 가격
- 현재 필요한 개수

위 값은 화면 숫자 OCR 결과가 아닙니다. `item_name → Item ID` 확정 뒤 기존 JunhyunHelper 로컬 데이터에서 계산/조회합니다.

따라서 Ground Truth dataset은 현재 다음처럼 구분합니다.

```text
OCR / localization field: item_name
mapped_data: highest trader sell price, flea average, slots, price/slot, required total
```

존재하지 않는 가격/필요 개수 OCR ROI를 새로 만들지 않습니다. 향후 실제 화면 숫자를 읽는 제품 요구사항이 생길 때만 숫자 전용 recognizer/grammar를 별도 설계합니다.

## 3. Case ID

최신 diagnostic capture에는 프로세스 내에서 충돌하지 않는 Case ID를 부여합니다.

예:

```text
case_20260823130543127_000142
```

Case ID는 `scanner.log`의 최신 Scanner runtime event에도 자동으로 연결됩니다. 사용자가 교정/확정하거나 자동 보존 정책에 걸리면 같은 ID가 dataset 디렉터리 이름과 `case.json`의 식별자가 됩니다.

## 4. 저장 위치

```text
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
```

구조:

```text
diagnostics/
├─ README.md
├─ environment.json
├─ dataset.jsonl
├─ summary.json
├─ summary.md
└─ cases/
   └─ case_.../
      ├─ full.png
      ├─ detail_window.png
      ├─ annotated.png
      ├─ case.json
      └─ item_name/
         ├─ detected_roi.png
         ├─ corrected_roi.png          # 사용자가 영역을 수정한 경우
         ├─ processed_roi.png          # 현재 primary OCR 확대 입력 재현
         ├─ processed_variant_1.png    # contrast variant
         ├─ processed_variant_2.png    # binary variant
         └─ processed_variant_3.png    # inverse variant
```

`full.png`는 detector/OCR 전처리 전 capture frame입니다. 과거 Case를 향후 새 알고리즘으로 다시 분석할 수 있도록 원본을 보존합니다.

## 5. 자동 보존 정책

진단 저장 때문에 350ms Scanner loop가 직접 느려지지 않도록 자동 보존은 background best-effort로 수행하고 fingerprint 중복을 억제합니다.

현재 정책:

- 상세창 탐지 실패 / header lock 실패: 대표 사례 보존
- identity failure / fail-closed semantic 결과: 보존
- 성공이지만 confidence < 0.93: 보존
- 높은 confidence 정상 성공: title signature 기반 약 1/20 deterministic sampling
- 같은 source/title/reason/ROI fingerprint 반복: 프로세스 내 중복 보존 억제

자동 보존 Case는 `review_status=unreviewed`입니다. 사용자가 정답을 지정하지 않은 자동 Case는 Ground Truth 정답으로 취급하지 않습니다.

## 6. 사용자 교정 UX

Scanner 탭의 `교정` 버튼은 최신 diagnostic frame을 엽니다.

사용자는 필요한 작업만 합니다.

```text
정상 결과
→ 맞음

상세창 영역이 틀림
→ 상세보기 영역 수정
→ 실제 상세창을 드래그
→ 교정 저장

아이템명 ROI가 틀림
→ 아이템명 영역 수정
→ 실제 텍스트 영역을 드래그
→ 교정 저장

텍스트/최종 아이템명이 틀림
→ 정답 아이템명 입력
→ 교정 저장

영역과 텍스트가 모두 틀림
→ 영역 드래그 + 정답 아이템명 입력
→ 한 Case로 교정 저장
```

사용자가 JSON, 좌표, 파일명, 이미지 분류를 직접 관리하지 않습니다.

프로그램 검출 영역은 기존 색상으로 유지하고 사용자 corrected detail/title ROI는 별도 색상으로 overlay합니다.

## 7. Case metadata

`case.json`은 최소 다음 evidence를 보존합니다.

- Case / dataset / program / scanner version
- timestamp / capture mode / capture source
- capture width/height/origin / system DPI
- detected/corrected detail ROI + normalized ratio + delta
- structural score/reason
- header score/reason
- detected/corrected item-name ROI + delta
- OCR raw
- matcher/sanitized text
- program official-name result
- program Item ID
- user ground truth
- confidence / second score / margin / pass / reason
- `pipeline.stage`: 실제 실행이 도달하거나 실패한 단계에 대한 관찰값
- `ground_truth_error_type`: 사용자가 검증한 경우에만 존재하는 오류 라벨
- current mapped presentation values when Item ID is available
- artifact paths
- reviewed/unreviewed 및 program-correct 상태

`pipeline.stage`와 `ground_truth_error_type`은 서로 다른 의미입니다. 자동 Case가 상세창 탐지 단계에서 실패했다는 사실은 기록할 수 있지만, 사용자가 정답을 지정하지 않은 상태에서 그 실패의 진짜 원인을 Ground Truth 오류로 단정하지 않습니다.

## 8. 오류 분류

사용자 검증 Ground Truth에 적용 가능한 분류:

- `DETAIL_WINDOW_DETECTION`
- `FIELD_LOCALIZATION`
- `OCR_RECOGNITION`
- `CANDIDATE_MATCHING`
- `PARSING`
- `DATA_MAPPING`
- `UNKNOWN_MULTIPLE`
- `NONE`

현재 사용자 교정으로 확실하게 판별할 수 있는 것은 detail/field/OCR/candidate 계열입니다. Parsing/Data Mapping은 해당 Ground Truth 입력이 실제로 존재할 때 사용합니다. 여러 층의 교정이 동시에 발생하면 원인을 억지로 단일 단계에 귀속하지 않고 `UNKNOWN_MULTIPLE`로 둡니다.

자동/미검증 Case는 위 오류 라벨을 생성하지 않습니다. 대신 현재 구현은 관찰 가능한 파이프라인 상태를 다음처럼 기록합니다.

- `DETAIL_WINDOW_DETECTION_FAILED`
- `DETAIL_HEADER_LOCK_FAILED`
- `OCR_OR_PREPROCESSING_FAILED`
- `IDENTITY_MATCH_FAILED`
- `FINALIZED`
- `NOT_RUN`

이 상태명은 문제 원인의 정답이 아니라 “프로그램이 어디까지 진행했는가”를 나타냅니다.

## 9. 일반 로그와 dataset 분리

`scanner.log`는 bounded runtime diagnostics입니다.

Ground Truth dataset은 원본 이미지/ROI/예측/정답을 포함하는 별도 persistence입니다.

- `로그 삭제`: scanner.log(.1), 최근 활동, 최신 메모리 diagnostic frame 삭제
- `교정 데이터 삭제`: Ground Truth/diagnostic dataset만 삭제
- 사용자가 이미 내보낸 ZIP은 어느 삭제 버튼도 자동 삭제하지 않음

이 분리는 제품 UI에서 명시합니다.

## 10. Export

Scanner 탭의 `교정 데이터 내보내기`는 다음 ZIP을 생성합니다.

```text
ScannerDiagnostics_YYYY-MM-DD.zip
```

ZIP은 dataset 전체와 export 시점의 scanner.log(.1)를 함께 포함합니다.

자동 생성 파일:

- `README.md`
- `summary.md`
- `summary.json`
- `environment.json`
- `dataset.jsonl`
- `cases/**`
- `logs/scanner.log*` (존재할 때)

사용자는 ZIP 하나만 개발 분석에 전달하면 됩니다.

## 11. 자동 통계

dataset index rebuild 시 다음을 집계합니다.

- total cases
- user-reviewed cases
- final-result reviewed cases
- reviewed program-correct cases
- Ground Truth corrections
- reviewed final accuracy
- 사용자 검증 Ground Truth 오류 유형별 건수
- 자동/수동 전체 Case의 observed pipeline stage 건수
- 상세보기 창 ROI 교정 offset 평균/표준편차
- 아이템명 ROI 교정 offset 평균/표준편차

ROI 통계는 detail-window와 item-name을 합치지 않습니다. 각각 독립적으로 `Samples`, 평균 `ΔX/ΔY/ΔW/ΔH`, 표준편차를 산출합니다.

최종 정확도 분모는 단순 `reviewed cases` 전체가 아니라 실제 최종 아이템 판정의 맞음/틀림 여부가 존재하는 Case만 사용합니다. 영역만 교정했고 최종 아이템 정답을 확정하지 않은 Case가 최종 정확도를 인위적으로 낮추지 않도록 하기 위함입니다.

## 12. 전처리 evidence

현재 production Windows OCR의 primary/deep preprocessing 규칙은 Scanner Lab 3.8과 동일하게 다음입니다.

- title height에 따라 4x/6x/8x 확대
- contrast variant
- threshold variant
- inverse threshold variant

Case 저장 시 이 규칙을 재현해 `processed_roi.png` 및 `processed_variant_*.png`를 함께 보존합니다. 이 재현 코드는 OCR 전처리 변경 시 반드시 함께 변경해야 하며, 이후 가능하면 OCR engine이 실제 소비한 processed frame을 직접 evidence로 발행하도록 통합하는 것이 기술 부채 후속 항목입니다.

## 13. 성능 / 저장 공간 / 개인정보

- dataset persistence failure는 Scanner recognition을 실패시키지 않음
- automatic persistence는 background best-effort
- 동일 실패 fingerprint 중복 억제
- Scanner 탭에 현재 Case 수/총 용량 표시
- 전체 dataset 삭제 가능
- export ZIP은 사용자 지정 위치에만 생성
- 전체 화면/게임 창 pixel이 저장될 수 있다는 사실을 제품 문서와 UI에서 숨기지 않음

개별 Case 삭제 UI는 초기 foundation 이후 데이터 검토 UX와 함께 추가할 수 있습니다. dataset index는 directory scan으로 재생성되므로 Case 디렉터리가 없어져도 연속 번호 의존성으로 깨지지 않습니다.

## 14. 개발 우선순위와 현재 단계

이번 foundation의 완료 범위:

1. Case ID / 단계 evidence 연결
2. 사용자 detail/title/text 교정
3. Ground Truth/diagnostic dataset 저장
4. 자동 README/summary/index 및 ZIP export
5. 저장 용량/전체 삭제 관리

다음 단계는 실제 데이터 없이 임의 구현하지 않습니다.

```text
실사용 dataset 확보
→ detail/header/ROI failure cluster 분석
→ 해당 단계만 수정
→ OCR/matcher confusion 분석
→ 필요한 경우 Tarkov font / real-render sample 활용 강화
→ full replay regression runner 구축/강화
→ before/after + regression gate
```

특히 detector threshold, header lock 0.68, matcher confidence/margin은 Ground Truth evidence 없이 완화하지 않습니다.

## 15. 회귀 개발 계약

알고리즘 변경은 최종적으로 다음 지표를 비교해야 합니다.

```text
OLD vs NEW
- detail-window result
- header/title ROI result
- OCR raw/normalized
- final Item ID
- final official name
- confidence/margin
- mapped data
```

그리고 Case를 다음 세 집합으로 분리합니다.

- 새롭게 해결됨
- 여전히 실패
- 새롭게 실패(regression)

`program_correct=true`였던 reviewed Case가 새 알고리즘에서 실패하면 전체 평균이 올라도 회귀로 취급합니다.

현재 production scanner는 arbitrary offline bitmap replay API를 아직 제공하지 않으므로 full-pipeline regression runner는 Ground Truth가 축적된 뒤 detector/OCR replay boundary를 먼저 추출하여 구현합니다. metadata/matcher-only 비교를 full regression으로 속이지 않습니다.

## 16. 금지 사항

- 한 번의 사용자 좌표 교정을 전역 offset으로 즉시 적용하지 않음
- 자동 보존 unreviewed Case를 정답으로 학습하지 않음
- 자동 보존 Case의 observed pipeline stage를 Ground Truth 오류 원인으로 취급하지 않음
- 현재 존재하지 않는 숫자 OCR 필드를 문서 요구사항에 맞추기 위해 인위적으로 추가하지 않음
- Ground Truth 없이 confidence/header/matcher threshold를 완화하지 않음
- dataset persistence 실패를 Scanner identity 판정에 섞지 않음
- OCR 하나의 결과를 최종 권위로 승격하지 않음
