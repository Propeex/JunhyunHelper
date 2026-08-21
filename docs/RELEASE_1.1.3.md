# RELEASE 1.1.3 — Scanner Lab v3.8 인식 파이프라인 복원

기준일: 2026-08-21

상태: **`PUBLIC RELEASE / VERIFIED`**

## 목적

v1.1.3은 v1.1.2 공개 후 실제 사용자 검증에서 확인된 Scanner 인식 회귀를 수정한 PATCH release입니다.

사용자가 보존하고 있던 `TarkovHelper-ScannerLab-v3.8` 원본을 다시 확보했고, 당시 실제로 잘 동작했던 화면 인식 구조를 현재 JunhyunHelper Scanner의 capture/catalog/presentation 경계 안으로 복원했습니다.

DEC-048에 따라 새 사용자 기능이 아니라 기존 Scanner 인식부의 회귀 복구이므로 PATCH +1입니다.

## 사용자 재현과 원인

v1.1.2에서는 현재 Tarkov 상세창을 대상으로 Scanner/Test를 실행할 때 상세창이 있어도 제목 OCR 자체가 자주 수행되지 않거나, OCR이 제목 대신 아래의 분류 행을 읽는 문제가 있었습니다.

원인은 catalog matcher 임계값이 아니라 **상세창 후보 생성과 최종 후보 선택 구조가 Scanner Lab v3.8보다 단순화된 통합 회귀**였습니다.

Scanner Lab v3.8은 하나의 geometry rectangle을 즉시 상세창으로 확정하지 않습니다.

```text
capture
→ RED-X connected-component candidates
+
→ edge/rectangle structural fallback candidates
→ candidate deduplication
→ 여러 title ROI OCR
→ official item catalog resolution
→ 필요 시 deep OCR preprocessing
→ official item으로 안전하게 resolve된 candidate만 실제 inspect window로 채택
```

## v3.8 복원 내용

### Structural candidate generation

- dark-red `X` close control connected-component 탐지
- RED-X 위치를 anchor로 외곽 상세창 경계 탐색
- RED-X가 불완전하거나 없는 상황을 위한 rectangle/edge projection fallback
- window aspect/border continuity/interior darkness 기반 구조 점수
- IoU 기반 중복 후보 제거
- 최대 8개 후보 유지
- structural floor `0.34`

구조 점수는 후보 순위이며 최종 사실 판정이 아닙니다.

### Title ROI

Scanner Lab v3.8의 검증된 공식을 복원합니다.

```text
titleX = window.Left + window.Width * 0.032
titleY = window.Top - 1
titleWidth = window.Width * 0.64
titleHeight = max(12, window.Height * 0.052)
```

약 `674x514` 상세창에서는 제목 ROI가 약 `431x27`입니다.

### OCR / semantic validation

제목 높이에 따라 4x / 6x / 8x 확대를 선택합니다. 1차 OCR에서 official item resolution이 실패하면 상위 3개 candidate에 다음 deep OCR을 수행합니다.

1. enlarged original
2. high-contrast grayscale
3. binary white-on-black
4. inverse black-on-white

OCR이 한 아이템명을 여러 줄로 분리할 수 있으므로 개별 line과 인접 두 line의 결합 후보를 모두 resolver에 전달합니다.

최대 8개 structural candidate를 OCR하고, **current official Korean full-item catalog에 안전하게 resolve되는 후보만 최종 inspect window로 확정**합니다.

## 유지한 제품 계약

변경하지 않았습니다.

- Tarkov real mode Borderless client-area capture
- Test mode all-display capture
- Windows `ko-KR` OCR boundary
- current official Korean full-item catalog
- exact-first conservative matcher
- fuzzy confidence threshold / top1-top2 margin
- false-positive보다 miss 선호
- historical alias production 금지
- scan-time network 금지
- Item ID 이후 existing JunhyunHelper data bridge
- `RequiredTotal` 기반 현재 필요한 수량
- Scanner 탭 UI / 최근 인식 기록
- Mini Scanner 직접 drag / Topmost / no-activate
- game memory / DLL injection / packet interception / icon identity 금지

즉 인식률을 올리기 위해 matcher를 느슨하게 만든 것이 아니라, **잘못 단순화된 recognition architecture를 검증됐던 v3.8 구조로 복원**했습니다.

## 회귀 기준

사용자 보존 원본과 기존 debug 자료를 기준으로:

- cropped `Ophthalmoscope 검안경`
  - outer inspect 약 `x=3 y=3 w=672 h=514`
  - v3.8 score 약 `0.997`
- full `Water 0.6L 물병` screenshot
  - inspect 약 `x=622 y=282 w=674 h=514`
  - v3.8 score 약 `0.992`

현재 Core 회귀 테스트는 이 구조와 title ROI를 기준으로 고정합니다.

상세 reference: `docs/SCANNER_LAB_3_8_REFERENCE.md`

## 검증 기록

제품 코드 사전 validation CI:

```text
CI run: #1222
run id: 32466187224
Windows Release build: SUCCESS
automated tests: 245 passed / 0 failed / 0 skipped
win-x64 self-contained single-file publish: SUCCESS
actual candidate EXE Product UI / Scanner / Main Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
```

릴리즈 직전 PR 재검증에서도 동일 245 tests / publish / actual EXE smoke를 통과했습니다. 최초 Map smoke의 Factory floor timeout은 동일 후보 재실행에서 통과해 일시적 runner timing으로 판정했습니다.

### 최종 public release

```text
release source SHA: 8803f899341859887281ad50135911f4625a64f3
release verification run: 32470606548
release job: 96736389584
automated tests: 245 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.3-win-x64.zip
bytes: 80,251,960
SHA-256: 419f6288aa3202f10868f2fe6a4ccac40475753ce4ba8c8c2d9985396c4bf493
published EXE bytes: 83,826,070
ProductVersion: 1.1.3+8803f899341859887281ad50135911f4625a64f3
Draft downloaded package validation: SUCCESS
Draft downloaded EXE smoke: SUCCESS
public/latest exact tag verification: SUCCESS
public downloaded package validation: SUCCESS
public downloaded EXE smoke: SUCCESS
```

공개 tag가 위 exact source SHA를 가리키는지 GitHub API로 검증했습니다. 공개 ZIP을 다시 다운로드해 checksum/size/ProductVersion을 확인한 뒤 실제 EXE를 다시 실행하여 Product UI + Scanner + Main Map + Factory + MiniMap + 정상 종료 smoke까지 통과했습니다.

릴리즈 검증 중 발견된 오류들은 제품 코드가 아니라 one-shot release automation의 null 처리와 PowerShell git refspec 보간 문제였습니다. v3 release workflow에서 GitHub API 기반 exact-tag 검증으로 교체해 전체 gate를 성공시킨 뒤 임시 release/diagnostic/dispatch workflow를 제거했습니다.

## 실제 Tarkov 후속 검증

최신 Tarkov Borderless E2E는 DEC-051에 따라 공개 후 사용자 환경에서 계속 확인합니다.

우선순위:

1. `Ophthalmoscope 검안경` 화면이 Scanner Lab v3.8 수준으로 다시 감지되는지
2. 실제 제목 OCR 문자열
3. candidate별 semantic match와 최종 selected candidate
4. 다른 아이템 상세창 인식률
5. false positive / miss
6. 장시간 CPU/memory 영향
7. Mini Scanner / MiniMap / Alt+Tab 공존

개발자 로그:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

구조 후보와 candidate별 OCR pass/매칭/최종 선택 정보로 detector/OCR/matcher 병목을 분리합니다. screenshot/raw pixel은 저장하지 않습니다.

## release gate

- [x] Scanner Lab v3.8 source recovered and documented
- [x] RED-X + rectangle structural candidates restored
- [x] multi-candidate semantic validation restored
- [x] adaptive 4x/6x/8x OCR restored
- [x] deep preprocessing OCR restored
- [x] Windows Release build
- [x] 245 automated tests
- [x] win-x64 self-contained publish
- [x] actual candidate EXE Product UI / Scanner / Map / Factory / MiniMap smoke
- [x] graceful shutdown / clean portable root
- [x] v1.1.3 ProductVersion/FIRST_RUN final validation
- [x] exact release source fixed
- [x] Draft ZIP + checksum/package/ProductVersion validation
- [x] Draft-downloaded EXE smoke
- [x] public/latest transition
- [x] exact public tag verification
- [x] public asset re-download validation
- [x] public-downloaded EXE smoke
- [x] temporary release workflow cleanup
- [x] final release SHA/hash/run record
