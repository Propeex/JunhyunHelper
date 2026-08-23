# Current Scanner work

기준일: 2026-08-24

현재 작업: **v1.4.2 공개 완료 후 실제 Tarkov Ground Truth 추가 수집 및 evidence 기반 정확도 개선**

## 공개 기준선

현재 public stable / latest는 **v1.4.2**입니다.

```text
release source/tag: a2d939b5f28e0d6de2468312bdd11467e3b35622
fix PR #160 CI: 32656154735 — SUCCESS
release-prep PR #161 CI: 32656572239 — SUCCESS
272 tests / 0 failed / 0 skipped
release run: 32656993853 — SUCCESS
independent public verifier: 32657225090 — SUCCESS
asset: Junhyun-Helper-v1.4.2-win-x64.zip
bytes: 80,385,620
SHA-256: e6aa57ac9492ebc3438335a5e0f66e4daf18c2b87b2b61abcb141de0f0d810a8
ProductVersion: 1.4.2+a2d939b5f28e0d6de2468312bdd11467e3b35622
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download / SHA256SUMS / package layout: VERIFIED
public-downloaded EXE smoke: SUCCESS
one-shot release/verifier workflows: removed after durable evidence write
```

공식 릴리즈 기록:

- `docs/RELEASE_1.4.2.md`
- `docs/.release-v1.4.2-status.json`

## v1.4.2 실제 Ground Truth 수정 결과

v1.4.2는 v1.4.1 실제 Tarkov 교정 데이터 **61 Case / 16 reviewed Case**를 근거로 반복되는 실패 단계만 보완했습니다.

### 상세보기 창

실패는 단순히 상세창이 크다는 문제로 보지 않습니다. 일부 화면에서 stash/inventory의 큰 구조 프레임이 coarse detail 후보가 되고, 그 내부 수백 px 아래에 실제 상세창 header가 존재하는 패턴이 확인됐습니다.

Production 순서:

```text
기존 ScannerInspectHeaderLock
→ 실패 시 v1.4.1 live Ground Truth header refiner
→ 둘 다 실패한 oversized candidate에서만 contained-subpanel proposal
→ close X + magnifier + dark title field + text evidence 재검증
→ HEADER_FRAME_LOCKED >= 0.68
→ semantic OCR
```

contained-subpanel fallback이 생겼어도 stash/inventory frame 자체를 상세창으로 인정하지 않으며, 기존 semantic header evidence를 다시 통과해야 합니다.

### OCR / matcher

reviewed Case에서 다음 종류의 실제 오류가 확인됐습니다.

- `Grizzly`처럼 영문 glyph가 2개 정도 깨지는 경우
- `Emelya 에멜야 호밀 크루통`처럼 한글 일부가 동시에 잘못 읽히는 경우
- `Iskra`, `Axel`처럼 `l/I/r/0` 계열 형태가 헷갈리는 경우

정답 공식 아이템이 matcher top-1인데도 기존 confidence gate에서 fail-closed 된 사례가 있어 다음 bounded recovery를 추가했습니다.

- 현재 공식 카탈로그 전체에서 유일한 **2-edit candidate**
- 충분히 긴 suffix가 일치하고 카탈로그 전체에서 유일한 **2~3-edit candidate**

다만 다음은 그대로 유지합니다.

- 일반 confidence threshold 하향 없음
- global `r`, `0`, 한글 glyph replacement table 없음
- 근접 runner-up이 있는 후보는 fail closed
- 기존 low-evidence multi-edit 사례는 fail closed

### Scanner 단축키 창

`스캐너 ON/OFF` 세 번째 행이 창 하단에서 잘리는 UI clipping을 수정했습니다. 단축키 기능 계약 자체는 바뀌지 않았습니다.

## 현재 production recognition 기준선

- Scanner Lab 3.8 계열 structural detail candidate
- red close / neutral frame / magnifier / dark title field 기반 inspect-header lock
- `HEADER_FRAME_LOCKED` + anchor score `0.68` 미만은 semantic OCR 진입 금지
- structural floor `0.34`
- Windows ko-KR OCR primary/deep path
- Tarkov-font visual corroboration/recovery
- current official Korean item catalog exact/fuzzy/bounded recovery
- false positive보다 miss를 선호하는 fail-closed 정책
- v1.4.1 live header fallback 유지
- v1.4.2 contained-subpanel fallback은 앞선 header 경로가 모두 실패했을 때만 사용
- v1.4.2 unique 2-edit / long-suffix bounded matcher recovery
- 1회 고정밀 스캔은 최대 12 candidates, continuous Scanner는 최대 8 candidates
- Item ID 확정 뒤 highest trader / flea `avg24hPrice` / slots / `RequiredTotal` mapped presentation

추가 Ground Truth 없이 detector/header/OCR/matcher threshold를 임의로 낮추지 않습니다.

## Ground Truth 기반

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

### Dataset evidence

Case별 대표 보존물:

- `full.png`
- `detail_window.png`
- `annotated.png`
- detected/corrected item-name ROI
- OCR 전처리 재현 이미지
- `case.json`
- raw OCR / matcher text
- Item ID / 공식명
- confidence / second score / margin
- structural/header evidence
- ROI delta
- observed pipeline stage
- user Ground Truth
- mapped presentation

자동/미검증 Case의 pipeline stage와 사용자 검증 Ground Truth 오류 라벨은 분리합니다.

### 자동 통계

`summary.json` / `summary.md`:

- 전체/검증/최종검증 Case 수
- 최종 정확도
- Ground Truth 오류 유형
- observed pipeline stage
- detail ROI offset 평균/표준편차
- item-name ROI offset 평균/표준편차
- OCR observed → Ground Truth 문자 치환/삽입/누락 통계
- matcher top candidates

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
→ inspect-header lock / bounded contained-subpanel recovery
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

기존 정상 Case가 새 알고리즘에서 실패하면 평균 정확도가 좋아졌더라도 회귀로 취급합니다.

## Scanner 표시 데이터 의미

Production Scanner가 OCR하는 필드는 **아이템명(`item_name`) 하나**입니다.

아래 값은 Item ID 확정 이후 `mapped_data`로 계산/조회합니다.

- 최고 상점가
- 플리마켓 `avg24hPrice`
- slots / price per slot
- 현재 Needed Items의 `RequiredTotal`

따라서 가격·플리·슬롯·필요 개수의 화면 숫자 OCR 필드를 새로 만들지 않습니다.

## 현재 검증 상태

- PR #149 Ground Truth/correction/regression 구현 CI `32643727571`: SUCCESS
- PR #160 v1.4.1 실제 Tarkov Ground Truth 수정 CI `32656154735`: SUCCESS
- v1.4.2 release-prep PR #161 CI `32656572239`: SUCCESS
- automated tests: **272 passed / 0 failed / 0 skipped**
- exact-source public release run `32656993853`: SUCCESS
- independent public verifier `32657225090`: SUCCESS
- exact release source/tag: `a2d939b5f28e0d6de2468312bdd11467e3b35622`
- public/latest: VERIFIED
- public ZIP re-download/hash/SHA256SUMS/layout: VERIFIED
- ProductVersion/FIRST_RUN: VERIFIED
- public-downloaded EXE rendered UI/Map/Scanner smoke + graceful shutdown: SUCCESS
- durable status: `docs/.release-v1.4.2-status.json`
- completed v1.4.2 release/verifier one-shot workflows removed; normal `ci.yml`만 유지

따라서 **v1.4.2 release blocker는 없습니다.**

## 다음 실제 작업

```text
v1.4.2 실제 Tarkov 사용
→ 정상 결과는 대표 표본을 `맞음`으로 검증
→ 미인식/오인식은 문제 직후 `교정`에서 영역/텍스트 정답 입력
→ reviewed Ground Truth 추가 축적
→ summary / OCR confusion / ROI delta / matcher candidates 분석
→ `회귀 테스트`로 현재 기준선 확인
→ detail/header/ROI/OCR/matcher 중 실제 실패 단계 특정
→ 해당 단계만 수정
→ 전체 dataset replay
→ 기존 정상 REGRESSION=0 확인
```

현재 가장 중요한 다음 입력은 **v1.4.2의 실제 인게임 Ground Truth**입니다. 특히 v1.4.2가 추가한 contained-subpanel과 bounded OCR recovery가 실전에서 해결한 Case와 아직 남은 Case를 분리해서 평가합니다.

Scanner 인식 속도 최적화는 현재 의도적으로 보류되어 있습니다. 실제 정확도/안정성이 충분히 고정된 뒤 CPU/OCR 반복, candidate budget, capture pipeline 비용을 별도 측정해 최적화합니다.
