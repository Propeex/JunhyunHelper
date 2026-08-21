# RELEASE 1.1.3 — Scanner Lab v3.8 인식 파이프라인 복원

기준일: 2026-08-21

상태: **`RELEASE CANDIDATE`**

## 목적

v1.1.3은 v1.1.2 공개 후 실제 사용자 검증에서 확인된 Scanner 인식 회귀를 수정하는 PATCH release입니다.

사용자가 보존하고 있던 `TarkovHelper-ScannerLab-v3.8` 원본을 다시 확보했고, 당시 실제로 잘 동작했던 화면 인식 구조를 현재 JunhyunHelper Scanner의 capture/catalog/presentation 경계 안으로 복원했습니다.

DEC-048에 따라 새 사용자 기능이 아니라 기존 Scanner 인식부의 회귀 복구이므로 PATCH +1입니다.

## 사용자 재현

v1.1.2에서 현재 Tarkov 상세창을 대상으로 Scanner/Test를 실행하면:

- 상세창이 있어도 제목 OCR 자체가 자주 수행되지 않음
- OCR이 되더라도 제목이 아닌 다른 행을 읽는 경우가 있음
- 과거 Scanner Lab v3.8에서 동일 계열 화면을 안정적으로 감지하던 수준보다 크게 낮은 인식률

이 현상은 catalog matcher 임계값 문제가 아니라 **상세창 후보 생성과 최종 후보 선택 구조가 v3.8보다 단순화된 통합 회귀**였습니다.

## 확인된 원인

Scanner Lab v3.8은 하나의 geometry rectangle을 즉시 상세창으로 확정하지 않았습니다.

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

반면 통합 초기 Scanner는 한때 다음처럼 단순화되었습니다.

```text
fixed/favored geometry candidate 1개
→ title ROI 1개
→ OCR
→ matcher
```

구조적으로 잘못된 사각형을 첫 후보로 확정하면 OCR/matcher가 회복할 기회가 없었습니다.

## v3.8 복원 내용

### Structural candidate generation

- dark-red `X` close control connected-component 탐지
- RED-X 위치를 anchor로 외곽 상세창 경계 탐색
- RED-X가 불완전하거나 없는 상황을 위한 rectangle/edge projection fallback
- window aspect/border continuity/interior darkness 기반 구조 점수
- IoU 기반 중복 후보 제거
- 최대 8개 후보 유지
- structural floor `0.34`

구조 점수는 **후보 순위**이며 최종 사실 판정이 아닙니다.

### Title ROI

Scanner Lab v3.8의 검증된 공식을 복원합니다.

```text
titleX = window.Left + window.Width * 0.032
titleY = window.Top - 1
titleWidth = window.Width * 0.64
titleHeight = max(12, window.Height * 0.052)
```

약 `674x514` 상세창에서는 제목 ROI가 약 `431x27`입니다.

### OCR

제목 높이에 따라 확대 배율을 선택합니다.

- `<= 14px` → 8x
- `<= 20px` → 6x
- 그 외 → 4x

1차 OCR에서 official item resolution이 실패하면 상위 3개 candidate에 deep OCR을 수행합니다.

1. enlarged original
2. high-contrast grayscale
3. binary white-on-black
4. inverse black-on-white

OCR이 한 아이템명을 여러 줄로 분리할 수 있으므로 개별 line뿐 아니라 인접 두 line의 결합 후보도 resolver에 전달합니다.

### Semantic candidate validation

- 최대 8개 structural candidate OCR
- 상위 3개 deep OCR
- current official Korean full-item catalog와 대조
- official item resolution을 통과한 candidate만 실제 inspect window로 확정
- semantic confidence + structural score를 함께 candidate 순위에 사용
- 이미 검증한 상세창의 title hash가 동일하면 OCR 반복 생략

## 유지하는 현재 제품 계약

변경하지 않습니다.

- Tarkov real mode의 Borderless client-area capture
- Test mode의 all-display capture
- Windows `ko-KR` OCR engine boundary
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

즉 인식률을 올리기 위해 matcher를 느슨하게 만든 것이 아니라, **잘못 단순화된 recognition architecture를 검증됐던 v3.8 구조로 복원**한 것입니다.

## 확보된 v3.8 회귀 기준

사용자 보존 원본과 기존 debug 자료를 기준으로:

- cropped `Ophthalmoscope 검안경` image
  - outer inspect 약 `x=3 y=3 w=672 h=514`
  - v3.8 score 약 `0.997`
- full `Water 0.6L 물병` screenshot
  - inspect 약 `x=622 y=282 w=674 h=514`
  - v3.8 score 약 `0.992`

현재 Core 회귀 테스트는 이 구조와 title ROI를 기준으로 고정합니다.

상세 reference: `docs/SCANNER_LAB_3_8_REFERENCE.md`

## 자동 검증

Scanner Lab v3.8 복원 제품 코드 기준 validation CI:

```text
CI run: #1222
run id: 32466187224
Windows Release build: SUCCESS
automated tests: 245 passed / 0 failed / 0 skipped
win-x64 self-contained single-file publish: SUCCESS
actual candidate EXE Product UI / Scanner / Main Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown / clean portable root: SUCCESS
```

추가/교체된 geometry regression:

- cropped Ophthalmoscope-shape outer inspect + title ROI
- full Water screenshot-shape central inspect + title ROI
- strong inner rectangle coexistence
- no RED-X rectangle fallback
- uniform frame fail-closed

## 실제 Tarkov 후속 검증

최신 Tarkov Borderless E2E는 공개 후 사용자 환경에서 다시 확인합니다.

우선순위:

1. `Ophthalmoscope 검안경` 화면이 v3.8 수준으로 다시 감지되는지
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

v1.1.3에서는 구조 후보와 candidate별 OCR pass/매칭/최종 선택 정보가 남아 다음 병목을 더 정확히 분리할 수 있습니다. screenshot/raw pixel은 저장하지 않습니다.

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
- [ ] v1.1.3 ProductVersion/FIRST_RUN final PR CI
- [ ] exact main release SHA fixed
- [ ] Draft ZIP + checksum/package/ProductVersion validation
- [ ] Draft-downloaded EXE smoke
- [ ] public/latest transition
- [ ] public asset re-download validation
- [ ] public-downloaded EXE smoke
- [ ] temporary release workflow cleanup
- [ ] final release SHA/hash/run record

## 최종 공개 기록

릴리즈 완료 후 기록합니다.

```text
release source SHA: PENDING
release verification run: PENDING
automated tests: 245 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.3-win-x64.zip
bytes: PENDING
SHA-256: PENDING
ProductVersion: PENDING
Draft downloaded EXE smoke: PENDING
public downloaded EXE smoke: PENDING
```
