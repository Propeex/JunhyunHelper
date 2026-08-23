# Current Scanner work

기준일: 2026-08-24

현재 작업: **v1.4.1 공개 완료 후 실제 Tarkov Ground Truth 추가 수집 및 evidence 기반 정확도 개선**

## 공개 기준선

현재 public stable / latest는 **v1.4.1**입니다.

```text
release source/tag: 8ff790cbcaa3172d068200d5b34de1ea4c142ac0
fix PR #155 CI: 32648713289 — SUCCESS
release-prep PR #156 CI: 32649049071 — SUCCESS
268 tests / 0 failed / 0 skipped
release run: 32652350079 — SUCCESS
independent public verifier: 32652827208 — SUCCESS
asset: Junhyun-Helper-v1.4.1-win-x64.zip
bytes: 80,379,956
SHA-256: 7f666e3348b3d87aae27e22de078c1b3f36458f107a662cae1c58df8cdfa3e6f
ProductVersion: 1.4.1+8ff790cbcaa3172d068200d5b34de1ea4c142ac0
public/latest: VERIFIED
public-downloaded EXE smoke: SUCCESS
one-shot release/verifier/finalizer workflows: removed after durable evidence write
```

공식 릴리즈 기록:

- `docs/RELEASE_1.4.1.md`
- `docs/.release-v1.4.1-status.json`

## 현재 production recognition 기준선

v1.4.1은 v1.4.0 recognition 구조를 유지하면서, primary header lock이 실패한 경우에만 실제 Tarkov Ground Truth 기반 fallback을 추가합니다.

- Scanner Lab 3.8 계열 structural detail candidate
- red close / neutral frame / magnifier / dark title field 기반 inspect-header lock
- `HEADER_FRAME_LOCKED` + anchor score 0.68 미만은 semantic OCR 진입 금지
- Windows ko-KR OCR primary/deep path
- Tarkov-font visual corroboration/recovery
- current official item catalog exact/fuzzy/bounded recovery
- false positive보다 miss를 선호하는 fail-closed 정책
- v1.4.1 live fallback: 어두운 red close X + gray 38~39 neutral top border + upper-right lens/lower-left handle magnifier + dark title field/text evidence를 함께 요구
- 1회 고정밀 스캔은 최대 12 candidates, continuous Scanner는 기존 최대 8 candidates 유지
- Item ID 확정 뒤 highest trader / flea `avg24hPrice` / slots / `RequiredTotal` mapped presentation

첫 4개 reviewed live failure Ground Truth로 header-lock 실패 원인을 수정했으며, 이후에도 추가 Ground Truth 없이 detector/header/OCR/matcher threshold를 임의로 낮추지 않습니다.

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
- PR #155 실제 Tarkov Ground Truth header fix 최종 CI `32648713289`: SUCCESS
- v1.4.1 release-prep PR #156 CI `32649049071`: SUCCESS
- automated tests: **268 passed / 0 failed / 0 skipped**
- exact-source public release run `32652350079`: SUCCESS
- independent public verifier `32652827208`: SUCCESS
- exact release source/tag: `8ff790cbcaa3172d068200d5b34de1ea4c142ac0`
- public/latest: VERIFIED
- public ZIP re-download/hash/SHA256SUMS/layout: VERIFIED
- ProductVersion/FIRST_RUN: VERIFIED
- public-downloaded EXE rendered UI/Map/Scanner Ground Truth smoke + graceful shutdown: SUCCESS
- durable status: `docs/.release-v1.4.1-status.json`
- completed v1.4.1 release/verifier/finalizer one-shot workflows removed; normal `ci.yml`만 유지

따라서 v1.4.1 release blocker는 없습니다.

## 다음 실제 작업

```text
v1.4.1 실제 Tarkov 사용
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
