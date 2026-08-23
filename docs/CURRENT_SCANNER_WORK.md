# Current Scanner work

기준일: 2026-08-23

현재 작업: **v1.4.0 공개 완료 후 실제 Tarkov Ground Truth 수집 및 evidence 기반 정확도 개선**

## 공개 기준선

현재 public stable은 **v1.4.0**입니다.

```text
release source/tag: 1b7f565adec9dfa2546fb959c813310707aabd32
release-prep CI: 32644579509 — SUCCESS
268 tests / 0 failed / 0 skipped
release run: 32644951640 — SUCCESS
independent public verifier: 32645536757 — SUCCESS
asset: Junhyun-Helper-v1.4.0-win-x64.zip
SHA-256: ef3676bbc7fb07fd45f4e9291e6fd4ef8a4a686a0f584cb1ddfdb6569376645f
public/latest: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

공식 릴리즈 기록:

- `docs/RELEASE_1.4.0.md`
- `docs/.release-v1.4.0-status.json`

## 현재 production recognition 기준선

v1.3.x에서 검증한 recognition 구조를 v1.4.0에서도 유지합니다.

- Scanner Lab 3.8 계열 structural detail candidate
- red close / neutral frame / magnifier / dark title field 기반 inspect-header lock
- `HEADER_FRAME_LOCKED` + anchor score 0.68 미만은 semantic OCR 진입 금지
- Windows ko-KR OCR primary/deep path
- Tarkov-font visual corroboration/recovery
- current official item catalog exact/fuzzy/bounded recovery
- false positive보다 miss를 선호하는 fail-closed 정책
- Item ID 확정 뒤 highest trader / flea `avg24hPrice` / slots / `RequiredTotal` mapped presentation

현재 Ground Truth가 없는 상태에서 detector/header/matcher threshold를 임의로 낮추지 않습니다.

## v1.4.0에서 제품화된 Ground Truth 기반

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
- 사용자 검증 Case는 background 자동 저장보다 우선하며 덮어쓰이지 않음

### Dataset

Case별로 다음 evidence를 보존합니다.

- `full.png`
- `detail_window.png`
- `annotated.png`
- detected/corrected item-name ROI
- OCR 전처리 재현 이미지
- `case.json`

Metadata에는 raw OCR, matcher text, Item ID, 공식명, confidence/second score/margin, structural/header evidence, ROI delta, pipeline stage, user Ground Truth, mapped presentation을 포함합니다.

자동/미검증 Case의 pipeline stage와 사용자 검증 Ground Truth 오류 라벨은 분리합니다.

### Matcher diagnostics

현재 acceptance 기준은 유지하면서 상위 후보 rank / Item ID / 공식명 / score를 diagnostic evidence로 전달합니다.

이 정보로 실제 정답이 2~5위에 반복적으로 존재하는지, 특정 후보 쌍이 구조적으로 충돌하는지 분석할 수 있습니다.

### 자동 통계

`summary.json` / `summary.md`에 다음을 생성합니다.

- 전체/검증/최종검증 Case 수
- 최종 정확도
- Ground Truth 오류 유형
- observed pipeline stage
- detail ROI offset 평균/표준편차
- item-name ROI offset 평균/표준편차
- OCR observed → Ground Truth 문자 치환/삽입/누락 통계

### 데이터 관리 / Export

Scanner UI에서:

- 저장 Case 수/용량 확인
- Case 목록 확인
- 선택 Case 삭제
- 전체 dataset 삭제
- 일반 Scanner 로그 별도 삭제
- `ScannerDiagnostics_YYYY-MM-DD.zip` export

을 수행합니다.

### Full-pipeline regression

`회귀 테스트`는 reviewed Ground Truth Case의 원본 `full.png`를 현재 production 경로로 재실행합니다.

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

을 `regression.json` / `regression.md`로 기록합니다.

실시간 Scanner가 실행 중이면 one-shot과 동일한 직렬화 경계에서 잠시 중단했다가 회귀 실행 후 복귀합니다.

## 현재 검증 상태

- PR #149 Ground Truth/correction/regression 구현 최종 CI `32643727571`: SUCCESS
- v1.4.0 release-prep CI `32644579509`: SUCCESS
- automated tests: **268 passed / 0 failed / 0 skipped**
- exact-source public release run `32644951640`: SUCCESS
- independent public verifier `32645536757`: SUCCESS
- exact release source/tag: `1b7f565adec9dfa2546fb959c813310707aabd32`
- public/latest: VERIFIED
- public ZIP SHA256SUMS/layout: VERIFIED
- public-downloaded EXE rendered UI/Map smoke + graceful shutdown: SUCCESS

따라서 v1.4.0 release blocker는 없습니다.

## 다음 실제 작업

```text
v1.4.0 실제 Tarkov 사용
→ 정상 결과는 필요할 때 `맞음`으로 표본 검증
→ 미인식/오인식은 문제 직후 `교정`에서 영역/텍스트 정답 입력
→ 충분한 reviewed Ground Truth 축적
→ summary / OCR confusion / ROI delta 분석
→ `회귀 테스트`로 현재 기준선 확인
→ detail/header/ROI/OCR/matcher 중 실제 실패 단계 특정
→ 해당 단계만 수정
→ 전체 dataset replay
→ 기존 정상 REGRESSION=0 확인
```

현재 가장 중요한 다음 입력은 **실제 인게임 Ground Truth**입니다. 데이터가 생기기 전에는 OCR 파라미터나 header/matcher threshold를 감으로 변경하지 않습니다.
