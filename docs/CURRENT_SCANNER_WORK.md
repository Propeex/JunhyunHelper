# Current Scanner work

기준일: 2026-08-23

현재 작업: **실제 Tarkov Ground Truth 수집이 가능한 Scanner 진단/교정/회귀 기반 완성 및 검증**

## 현재 production recognition 기준선

기존 v1.3.x에서 검증한 recognition 구조는 유지한다.

- Scanner Lab 3.8 계열 structural detail candidate
- red close / neutral frame / magnifier / dark title field 기반 inspect-header lock
- `HEADER_FRAME_LOCKED` + anchor score 0.68 미만은 semantic OCR 진입 금지
- Windows ko-KR OCR primary/deep path
- Tarkov-font visual corroboration/recovery
- current official item catalog exact/fuzzy/bounded recovery
- false positive보다 miss를 선호하는 fail-closed 정책
- Item ID 확정 뒤 highest trader / flea `avg24hPrice` / slots / `RequiredTotal` mapped presentation

현재 Ground Truth가 없는 상태에서 detector/header/matcher threshold를 임의로 낮추지 않는다.

## 이번 개발에서 추가한 기반

공식 계약: `docs/SCANNER_GROUND_TRUTH.md`

### 진단 / 교정

- 모든 최신 diagnostic capture에 Case ID 부여
- Case ID를 `scanner.log`와 연결
- 상세창 탐지 실패, header 실패, identity 실패, 저신뢰 결과의 대표 Case 자동 보존
- 높은 confidence 정상 결과 일부 deterministic sampling
- 상세보기 영역 사용자 드래그 교정
- 아이템명 ROI 사용자 드래그 교정
- 정답 아이템명 입력
- `맞음` 사용자 검증

### Dataset

Case별로 다음 evidence를 보존한다.

- `full.png`
- `detail_window.png`
- `annotated.png`
- detected/corrected item-name ROI
- OCR 전처리 재현 이미지
- `case.json`

Metadata에는 raw OCR, matcher text, Item ID, 공식명, confidence/second score/margin, structural/header evidence, ROI delta, pipeline stage, user Ground Truth, mapped presentation을 포함한다.

자동/미검증 Case의 pipeline stage와 사용자 검증 Ground Truth 오류 라벨은 분리한다.

### Matcher diagnostics

현재 acceptance 기준은 유지하면서 상위 후보 rank / Item ID / 공식명 / score를 diagnostic evidence로 전달한다.

이 정보로 실제 정답이 2~5위에 반복적으로 존재하는지, 특정 후보 쌍이 구조적으로 충돌하는지 분석할 수 있다.

### 자동 통계

`summary.json` / `summary.md`에 다음을 생성한다.

- 전체/검증/최종검증 Case 수
- 최종 정확도
- Ground Truth 오류 유형
- observed pipeline stage
- detail ROI offset 평균/표준편차
- item-name ROI offset 평균/표준편차
- OCR observed → Ground Truth 문자 혼동/삽입/누락 통계

### 데이터 관리 / Export

Scanner UI에서:

- 저장 Case 수/용량 확인
- Case 목록 확인
- 선택 Case 삭제
- 전체 dataset 삭제
- 일반 Scanner 로그 별도 삭제
- `ScannerDiagnostics_YYYY-MM-DD.zip` export

을 수행한다.

### Full-pipeline regression

`회귀 테스트`는 reviewed Ground Truth Case의 원본 `full.png`를 현재 production 경로로 재실행한다.

```text
full.png
→ detail geometry
→ inspect-header lock
→ title ROI
→ current OCR/deep OCR/font recovery
→ current catalog matching
→ final Item ID
```

결과:

- STILL_CORRECT
- SOLVED
- STILL_FAILING
- REGRESSION
- ERROR

을 `regression.json` / `regression.md`로 기록한다.

실시간 Scanner가 실행 중이면 one-shot과 동일한 직렬화 경계에서 잠시 중단했다가 회귀 실행 후 복귀한다.

## 현재 검증 상태

- Ground Truth foundation 초기 구현은 Windows CI build/test/package/product-smoke를 통과한 이력이 있다.
- full-pipeline regression 추가 후 발견된 C# result factory 이름 충돌은 수정했다.
- 최신 전체 변경은 Windows CI에서 다시 검증한다.
- CI가 최종 통과하기 전에는 PR #149를 main에 병합하지 않는다.

## 다음 실제 작업

```text
최신 CI 통과
→ PR #149 코드/문서 최종 점검
→ main 반영
→ 실제 Tarkov 사용
→ 틀린 결과만 최소 행동으로 교정
→ 충분한 Ground Truth 축적
→ ZIP / regression 결과 분석
→ detail/header/ROI/OCR/matcher 중 실제 실패 단계 특정
→ 해당 단계만 수정
→ 전체 dataset replay
→ REGRESSION=0 확인
```

현재 가장 중요한 다음 입력은 실제 인게임 Ground Truth다. 데이터가 생기기 전에는 OCR 파라미터나 header/matcher threshold를 감으로 변경하지 않는다.
