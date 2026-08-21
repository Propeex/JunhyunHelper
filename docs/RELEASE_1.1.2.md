# RELEASE 1.1.2 — Scanner 제목 영역 회귀 수정

기준일: 2026-08-21

상태: **`RELEASE CANDIDATE`**

## 목적

v1.1.2는 v1.1.1에서 실제 사용자 검증으로 확인된 Scanner 상세창/제목 ROI 회귀를 수정하는 PATCH release입니다.

DEC-048에 따라 기존 기능의 버그 수정이므로 PATCH +1입니다.

## 사용자 재현

현재 한국어 Tarkov 상세창 예시:

```text
Ophthalmoscope 검안경
교환용 물품 > 의료용품
```

v1.1.1에서 Scanner가 자주 `아이템 이름을 읽지 못해 식별을 보류했습니다.`를 표시했습니다.

일부 시도에서는 실제 아이템 제목이 아니라 바로 아래 분류 행을 OCR했고, 예를 들어 `교환용 물품 > 의료용품`을 깨진 문자열로 읽었습니다.

## 원인

카탈로그/matcher가 아니라 OCR보다 앞단의 **detail geometry + title ROI 통합 회귀**였습니다.

v1.1.1 통합 detector에는 다음 문제가 있었습니다.

1. 상세창 canonical height가 현재 관측 구조와 맞지 않는 값으로 들어가 있었습니다.
2. 외곽 상세창보다 내부의 작은 고대비 사각형이 detector score에서 이길 수 있었습니다.
3. 그 잘못된 영역에서 계산한 title ROI가 실제 분류 행 높이에 걸렸습니다.
4. 외곽 상세창을 잡더라도 title ROI 높이가 넓어 제목 아래 분류 행까지 포함할 수 있었습니다.

사용자가 제공한 `Ophthalmoscope 검안경` 현재 클라이언트 이미지로 이 증상을 재현했습니다.

## 수정

### 상세창 구조

- 현재 관측 상세창 구조를 약 `676x522 @ 1920x1080 UI scale` 기준으로 보정
- 상/하/좌/우 외곽 테두리를 모두 요구
- 우상단 닫기 영역 신호를 필수로 요구
- 구조 점수가 비슷하면 작은 내부 사각형이 아니라 큰 외곽 프레임을 우선
- 엄격한 경계 조건에서도 프레임을 놓치지 않도록 중심 탐색을 픽셀 단위로 세분화
- border probe 반경을 조정해 몇 px의 탐색 오차를 흡수
- 닫기 영역 및 각 테두리를 순차적으로 검사해 대부분의 잘못된 후보를 일찍 탈락시킴

### 제목 ROI

현재 `676x522` 상세창 기준 대략:

```text
x = 24
y = 1
width = 602
height = 25
```

영역만 OCR에 전달합니다.

사용자가 제공한 화면에 이 ROI를 직접 적용하면:

```text
Ophthalmoscope 검안경
```

한 줄만 포함되고 아래:

```text
교환용 물품 > 의료용품
```

행은 포함되지 않습니다.

## 변경하지 않는 것

- Windows `ko-KR` OCR 엔진
- current official Korean full-item catalog
- exact-first matcher
- fuzzy confidence threshold
- top1/top2 margin
- fail-closed 정책
- Item ID 이후 JunhyunHelper data bridge
- scan-time network 금지
- Tarkov memory/DLL/packet 접근 금지
- Scanner 탭 UI
- Mini Scanner UI/직접 drag

즉 OCR을 잘못된 위치에 수행하던 문제를 고친 것이며, 인식률을 올리기 위해 matcher를 느슨하게 만들지 않았습니다.

## 회귀 테스트

추가/수정한 detector 테스트:

- current-detail outer frame 탐지
- title ROI가 breadcrumb/category 행 전에 끝나는지 검증
- 외곽 상세창 내부에 더 강한 사각형이 있어도 outer frame이 선택되는지 검증
- uniform frame fail-closed
- Display Test에서 축소된 상세창 탐지

현재 자동 테스트 수는 **244개**입니다.

## release gate

- [x] Windows Release build
- [x] 244 automated tests
- [x] detector strong-inner-frame regression
- [x] title ROI excludes category row
- [x] win-x64 self-contained publish
- [x] actual candidate EXE Product UI / Scanner / Map / Factory / MiniMap smoke
- [x] graceful shutdown
- [ ] v1.1.2 ProductVersion/FIRST_RUN identity final CI
- [ ] exact main release SHA fixed
- [ ] Draft ZIP + SHA256SUMS verification
- [ ] Draft-downloaded EXE smoke
- [ ] public/latest transition
- [ ] public re-download validation
- [ ] public-downloaded EXE smoke
- [ ] temporary release workflow cleanup
- [ ] final SHA/hash/run record

## 실제 Tarkov 후속 검증

v1.1.2 공개 후 같은 화면을 우선 다시 검사합니다.

확인 순서:

1. Scanner/Test 모드에서 geometry candidate가 외곽 상세창 크기로 잡히는지
2. 최근 인식 기록의 OCR 문자열이 `Ophthalmoscope 검안경` 제목을 읽는지
3. current catalog match가 성공하는지
4. 다른 종류 아이템 상세창에서도 제목 행만 읽는지
5. detector 세분화로 인한 CPU 증가가 체감되지 않는지

문제가 남으면 `%LocalAppData%/JunhyunHelper/logs/scanner.log`의 `geometry-candidate`, `ocr-result`, `match-result`를 기준으로 다음 병목을 분리합니다.

## 최종 공개 기록

릴리즈 완료 후 기록합니다.

```text
release source SHA: PENDING
release verification run: PENDING
automated tests: 244 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.2-win-x64.zip
bytes: PENDING
SHA-256: PENDING
ProductVersion: PENDING
Draft downloaded EXE smoke: PENDING
public downloaded EXE smoke: PENDING
```
