# Scanner Ground Truth / Correction Development Contract

기준일: 2026-08-23

이 문서는 준현 헬퍼 Scanner의 실사용 실패를 재현 가능한 개발 데이터로 전환하는 공식 계약이다. 기존 `docs/SCANNER.md`의 production 인식 파이프라인을 폐기하지 않고 그 위에 진단·사용자 교정·Ground Truth·Export·회귀 개발 체계를 둔다.

## 1. 목표

Scanner는 범용 OCR이 아니라 Tarkov UI 전용 폐쇄형 인식 시스템으로 취급한다.

```text
capture
→ detail candidate
→ inspect-header lock
→ item-name ROI
→ Windows ko-KR OCR / Tarkov-font visual corroboration
→ current official item catalog matching
→ Item ID or fail closed
→ local mapped presentation data
→ user verification/correction
→ Ground Truth dataset
→ full-pipeline replay regression
→ evidence-based algorithm change
```

성능 평가는 OCR 문자열 정확도만으로 끝내지 않는다. 상세창 탐지, ROI, OCR, 후보 매칭, 최종 Item ID, mapped presentation을 서로 분리해 진단하고, 최종적으로는 사용자에게 표시되는 아이템 판정이 맞는지를 핵심 지표로 본다.

## 2. 현재 production 필드 계약

현재 Scanner가 게임 화면에서 OCR하는 텍스트 필드는 `item_name` 하나다.

다음 값은 게임 화면 숫자 OCR 결과가 아니다.

- 최고 상인 판매가
- 플리마켓 24시간 평균가
- 슬롯 / 슬롯당 가격
- 현재 필요한 개수

이 값은 `item_name → Item ID` 확정 뒤 기존 JunhyunHelper 로컬 데이터에서 계산/조회한다.

따라서 dataset도 다음처럼 분리한다.

```text
OCR / localization field: item_name
mapped_data: highest trader sell price, flea average, slots, price/slot, required total
```

존재하지 않는 가격/필요 개수 OCR ROI를 요구사항 문구에 맞추기 위해 인위적으로 만들지 않는다. 향후 실제 화면 숫자 인식이 제품 요구사항으로 추가될 때만 숫자 전용 recognizer/grammar를 별도로 설계한다.

## 3. Case ID와 로그 연결

진단 capture에는 프로세스 내 고유 Case ID를 부여한다.

```text
case_YYYYMMDDHHMMSSfff_000142
```

같은 Case ID가 최신 diagnostic frame, `scanner.log`, dataset 디렉터리, `case.json`을 연결한다. 개발자는 이미지와 로그를 시간 추정으로 맞출 필요가 없다.

## 4. 저장 위치와 구조

```text
%LocalAppData%/JunhyunHelper/scanner/diagnostics/
```

```text
diagnostics/
├─ README.md
├─ environment.json
├─ dataset.jsonl
├─ summary.json
├─ summary.md
├─ regression.json              # 회귀 테스트 실행 후
├─ regression.md                # 회귀 테스트 실행 후
└─ cases/
   └─ case_.../
      ├─ full.png
      ├─ detail_window.png
      ├─ annotated.png
      ├─ case.json
      └─ item_name/
         ├─ detected_roi.png
         ├─ corrected_roi.png       # 사용자가 영역을 수정한 경우
         ├─ processed_roi.png
         ├─ processed_variant_1.png
         ├─ processed_variant_2.png
         └─ processed_variant_3.png
```

`full.png`는 detector/OCR 전처리 전 원본 capture frame이다. 새 알고리즘으로 과거 실패를 실제 재실행하기 위해 반드시 보존한다.

## 5. 자동 보존 정책

자동 진단 저장이 350ms live Scanner loop를 직접 지연시키지 않도록 persistence는 background best-effort로 수행한다.

현재 자동 보존 대상:

- 상세창 탐지 실패 / header lock 실패 대표 사례
- identity failure / fail-closed semantic 결과
- 성공했지만 confidence < 0.93
- 높은 confidence 정상 성공의 title-signature 기반 약 1/20 deterministic sample

동일 source/title/reason/ROI fingerprint의 반복 저장은 프로세스 내에서 억제한다.

자동 Case는 `review_status=unreviewed`다. 사용자가 확인하지 않은 자동 Case를 Ground Truth 정답이나 오류 원인으로 사용하지 않는다.

## 6. 사용자 교정 UX

Scanner 탭의 `교정`은 최신 diagnostic frame을 연다.

```text
결과가 맞음
→ 맞음

상세창이 틀림
→ 상세보기 영역 수정
→ 실제 상세창 드래그
→ 저장

아이템명 ROI가 틀림
→ 아이템명 영역 수정
→ 실제 텍스트 영역 드래그
→ 저장

최종 아이템명이 틀림
→ 정답 아이템명 입력
→ 저장

영역 + 텍스트 둘 다 틀림
→ 필요한 영역 드래그 + 정답 입력
→ 한 Case로 저장
```

사용자가 JSON, 좌표, 파일명, 이미지 분류를 직접 관리하지 않는다.

## 7. Case metadata

`case.json`은 최소 다음을 보존한다.

- Case / dataset / program / scanner version
- timestamp / capture mode / capture source
- capture width/height/origin / system DPI
- detected/corrected detail ROI + normalized ratio + delta
- structural score/reason
- header score/reason
- detected/corrected item-name ROI + delta
- OCR raw text
- matcher/sanitized text
- program official-name result
- program Item ID
- user Ground Truth
- confidence / second score / margin / pass / reason
- matcher 상위 후보 rank / Item ID / 공식명 / score
- `pipeline.stage`
- `ground_truth_error_type`
- Item ID가 있을 때 current mapped presentation
- artifact paths
- reviewed/unreviewed / program-correct 상태

### 후보 ranking

최종 1위만 저장하지 않는다. Matcher는 acceptance 기준을 바꾸지 않은 채 상위 후보를 diagnostic evidence로 전달한다.

예:

```json
"top_candidates": [
  { "rank": 1, "item_id": "...", "official_name": "...", "score": 0.941 },
  { "rank": 2, "item_id": "...", "official_name": "...", "score": 0.918 }
]
```

이를 통해 정답이 2~5위 안에 있었는지, 1·2위 간 margin이 구조적으로 부족한지 분석할 수 있다.

## 8. Ground Truth 오류와 파이프라인 관찰 분리

`pipeline.stage`와 `ground_truth_error_type`은 의미가 다르다.

사용자 검증 Ground Truth에만 적용 가능한 오류 유형:

- `DETAIL_WINDOW_DETECTION`
- `FIELD_LOCALIZATION`
- `OCR_RECOGNITION`
- `CANDIDATE_MATCHING`
- `PARSING`
- `DATA_MAPPING`
- `UNKNOWN_MULTIPLE`
- `NONE`

현재 사용자 UI로 직접 확정 가능한 것은 detail/field/OCR/candidate 계열이다. 여러 층의 수정이 동시에 발생하면 원인을 억지로 하나로 귀속하지 않고 `UNKNOWN_MULTIPLE`로 둔다.

자동/미검증 Case는 오류 Ground Truth를 만들지 않는다. 대신 프로그램이 실제로 어디까지 갔는지만 기록한다.

- `DETAIL_WINDOW_DETECTION_FAILED`
- `DETAIL_HEADER_LOCK_FAILED`
- `OCR_OR_PREPROCESSING_FAILED`
- `IDENTITY_MATCH_FAILED`
- `FINALIZED`
- `NOT_RUN`

## 9. 일반 로그와 dataset 분리

`scanner.log`는 bounded runtime diagnostics이고, Ground Truth dataset은 원본 이미지·ROI·예측·정답을 포함하는 별도 persistence다.

- `로그 삭제`: scanner.log(.1), 최근 활동, 최신 메모리 diagnostic frame 삭제
- `교정 데이터 관리`: Case 목록 확인, 선택 Case 삭제, 전체 dataset 삭제
- 사용자가 이미 내보낸 ZIP은 어느 삭제 기능도 자동 삭제하지 않음

Case 삭제 후 `dataset.jsonl`, `summary.json`, `summary.md`는 디렉터리를 다시 스캔하여 재생성한다. 연속 번호에 의존하지 않는다.

## 10. Export

Scanner 탭의 `교정 데이터 내보내기`는 다음 ZIP을 생성한다.

```text
ScannerDiagnostics_YYYY-MM-DD.zip
```

포함 내용:

- `README.md`
- `summary.md`
- `summary.json`
- `environment.json`
- `dataset.jsonl`
- `regression.json` / `regression.md` (실행된 경우)
- `cases/**`
- `logs/scanner.log*` (존재할 때)

사용자는 ZIP 하나만 개발 분석에 전달하면 된다.

## 11. 자동 통계

Dataset index rebuild 시 다음을 집계한다.

- total cases
- user-reviewed cases
- final-result reviewed cases
- reviewed program-correct cases
- Ground Truth corrections
- reviewed final accuracy
- Ground Truth 오류 유형별 건수
- observed pipeline stage별 건수
- 상세보기 ROI `ΔX/ΔY/ΔW/ΔH` 평균/표준편차
- 아이템명 ROI `ΔX/ΔY/ΔW/ΔH` 평균/표준편차
- 사용자 검증 OCR 문자열 → Ground Truth의 반복 문자 혼동/삽입/누락

OCR 혼동 통계는 normalized observed text와 Ground Truth를 edit alignment하여 계산한다.

예:

```text
0 → o
1 → l
r → ∅
∅ → i
```

단순 문자 위치 비교보다 삽입/누락에 강한 진단 데이터를 만든다.

최종 정확도의 분모는 실제 최종 아이템 맞음/틀림이 확인된 Case만 사용한다. 영역만 교정하고 아이템 정답을 확정하지 않은 Case는 최종 정확도에 섞지 않는다.

## 12. 전처리 evidence

현재 production Windows OCR의 primary/deep preprocessing은 Scanner Lab 3.8 계열 규칙을 사용한다.

- title height에 따라 4x/6x/8x 확대
- contrast variant
- threshold variant
- inverse threshold variant

Case 저장 시 이 규칙을 재현한 `processed_roi.png`와 `processed_variant_*.png`를 함께 보존한다.

현재 기술 부채: 저장 계층이 production OCR 전처리 규칙을 재현하고 있으며, OCR engine이 실제 소비한 processed bitmap 자체를 evidence로 직접 발행하는 구조는 아직 아니다. OCR 전처리 구현을 변경할 때 이 재현 코드도 반드시 같이 검토한다.

## 13. Full-pipeline 회귀 테스트

Scanner 탭의 `회귀 테스트`는 최종 Ground Truth가 있는 reviewed Case의 `full.png`를 현재 production Scanner 경로에 다시 투입한다.

재실행 경로:

```text
full.png
→ ScannerDetailGeometryDetector
→ ScannerTitleAnchorRefiner / ScannerInspectHeaderLock
→ current title ROI crop
→ current OCR / deep OCR / Tarkov-font recovery
→ current official catalog matching
→ final recognition
```

실시간 Scanner가 켜져 있다면 one-shot과 같은 직렬화 경계에서 잠시 중단한 뒤 회귀 실행 후 원래 모드로 복귀한다.

결과 분류:

- `STILL_CORRECT`: 과거 정상 + 현재 정상
- `SOLVED`: 과거 오답 + 현재 정답
- `STILL_FAILING`: 과거 오답 + 현재도 오답
- `REGRESSION`: 과거 정상 + 현재 오답
- `ERROR`: Case 파일 또는 replay 자체 오류

Baseline이 충분하지 않은 초기 reviewed Case는 current-no-baseline 상태로 남길 수 있다.

`regression.json`에는 현재 OCR raw/normalized, Item ID, 공식명, confidence, second score, pass, 예측 ROI, corrected ROI 대비 IoU, top candidates, mapped-data 비교도 남긴다.

`program_correct=true`였던 Case가 `REGRESSION`이 되면 전체 평균 정확도가 상승했더라도 회귀로 취급한다.

### mapped_data 비교 주의

가격·플리 평균가 등 mapped data는 데이터 최신화로 정상적으로 변할 수 있다. 따라서 현재 replay의 `MAPPED_DATA_CHANGED`는 진단 신호이며, 그 자체로 OCR/identity 회귀를 판정하지 않는다.

## 14. 성능 / 저장 공간 / 개인정보

- dataset persistence 실패는 Scanner recognition 결과를 바꾸지 않음
- automatic persistence는 background best-effort
- 동일 실패 fingerprint 중복 억제
- Scanner 탭에 Case 수/총 용량 표시
- 개별 Case 삭제 가능
- 전체 dataset 삭제 가능
- export ZIP은 사용자 지정 위치에만 생성
- 전체 게임 화면 픽셀이 저장될 수 있음을 숨기지 않음

## 15. 현재 완료 범위

이번 Ground Truth foundation에서 구현된 범위:

1. Case ID / 단계 evidence / scanner.log 연결
2. 상세창·아이템명 ROI·텍스트 사용자 교정
3. 원본/ROI/전처리/annotation Ground Truth dataset 저장
4. 자동 실패/저신뢰/정상 sample 보존
5. matcher 상위 후보 evidence
6. 자동 OCR 혼동 통계
7. README/summary/index/ZIP export
8. 저장 용량 표시
9. Case 목록 / 개별 삭제 / 전체 삭제
10. 실제 `full.png` 기반 full-pipeline replay regression

## 16. 다음 개발 단계

다음 단계는 실제 Ground Truth가 축적된 뒤 데이터 근거로 진행한다.

```text
실사용 dataset 확보
→ detail/header/ROI failure cluster 분석
→ 해당 단계만 수정
→ OCR confusion / top-candidate pattern 분석
→ 필요한 경우 Tarkov font / real-render sample 활용 강화
→ full replay regression 실행
→ solved / still failing / regression 확인
→ 회귀가 없을 때만 배포 후보 승인
```

추가 기술 부채 후보:

- OCR engine이 실제 소비한 processed frame 직접 evidence 발행
- 반복 사용자 Ground Truth에서 실제 Tarkov 렌더 샘플 사전 구축
- detail/title 공통 projection helper를 live/replay가 공유하도록 중복 제거
- 데이터가 충분해지면 환경별(해상도/DPI/capture mode) 통계 분리

## 17. 금지 사항

- 한 번의 사용자 좌표 교정을 즉시 전역 offset으로 적용하지 않음
- unreviewed 자동 Case를 정답 학습 데이터로 취급하지 않음
- observed pipeline stage를 Ground Truth 오류 원인으로 바꾸지 않음
- 현재 존재하지 않는 숫자 OCR 필드를 억지로 추가하지 않음
- Ground Truth 없이 detector/header/matcher threshold를 완화하지 않음
- dataset persistence 실패를 Scanner identity 판단에 섞지 않음
- OCR 하나의 결과를 최종 권위로 승격하지 않음
- metadata-only 비교를 full-pipeline regression이라고 부르지 않음
- 평균 정확도 상승만 보고 `REGRESSION` Case를 무시하지 않음

특히 현재 detail structural floor, header lock 0.68, matcher confidence/margin은 실제 Ground Truth evidence 없이 변경하지 않는다.
