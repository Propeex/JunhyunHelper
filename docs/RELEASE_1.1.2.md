# RELEASE 1.1.2 — Scanner 제목 영역 회귀 수정

기준일: 2026-08-21

상태: **`PUBLIC RELEASE / VERIFIED`**

## 목적

v1.1.2는 v1.1.1 실제 사용자 검증에서 확인된 Scanner 상세창/제목 ROI 회귀를 수정한 PATCH release입니다.

DEC-048의 기존 기능 버그 수정 규칙에 따라 PATCH +1입니다.

## 사용자 재현

현재 한국어 Tarkov 상세창 예시:

```text
Ophthalmoscope 검안경
교환용 물품 > 의료용품
```

v1.1.1에서는 Scanner가 자주 `아이템 이름을 읽지 못해 식별을 보류했습니다.`를 표시했고, 일부 시도에서는 실제 아이템 제목이 아니라 바로 아래 분류 행을 OCR했습니다.

## 원인

카탈로그/matcher가 아니라 OCR보다 앞단의 **detail geometry + title ROI 통합 회귀**였습니다.

1. 통합 detector의 상세창 canonical height가 현재 관측 구조와 맞지 않았습니다.
2. 외곽 상세창보다 내부의 작은 고대비 사각형이 detector score에서 이길 수 있었습니다.
3. 잘못 선택된 영역에서 계산한 title ROI가 실제 분류 행 높이에 걸렸습니다.
4. 외곽 상세창을 잡더라도 기존 title ROI가 높아 제목 아래 분류 행까지 포함할 수 있었습니다.

사용자가 제공한 `Ophthalmoscope 검안경` 현재 클라이언트 이미지로 이 증상을 재현했습니다.

## 수정

### 상세창 구조

- 현재 관측 상세창 구조를 약 `676x522 @ 1920x1080 UI scale` 기준으로 보정
- 상/하/좌/우 외곽 테두리를 모두 요구
- 우상단 닫기 영역 신호를 함께 요구
- 구조 점수가 비슷하면 작은 내부 사각형보다 큰 외곽 프레임을 우선
- 엄격한 경계 조건에서도 프레임을 놓치지 않도록 위치 탐색 정밀도 개선
- border probe 반경으로 소수 px의 후보 오차를 흡수
- close-control 및 각 테두리를 순차 검사해 잘못된 후보를 early reject

### 제목 ROI

현재 `676x522` 상세창 기준 대략:

```text
x = 24
y = 1
width = 602
height = 25
```

영역만 OCR에 전달합니다.

사용자 제공 화면에 적용하면 `Ophthalmoscope 검안경` 제목 한 줄만 포함되고 `교환용 물품 > 의료용품` 행은 ROI 밖으로 제외됩니다.

## 변경하지 않은 것

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

즉 잘못된 위치를 OCR하던 문제를 고친 것이며 인식률을 올리기 위해 matcher를 느슨하게 만들지 않았습니다.

## 회귀 테스트

- current-detail outer frame 탐지
- title ROI가 breadcrumb/category 행 전에 끝나는지 검증
- 외곽 상세창 내부에 더 강한 사각형이 있어도 outer frame이 선택되는지 검증
- uniform frame fail-closed
- Display Test에서 축소된 상세창 탐지

현재 자동 테스트 수는 **244개**입니다.

## 검증 결과

- PR #116 최종 CI `#1187` / run `32461315093`: **SUCCESS**
- Windows Release build: **SUCCESS**
- automated tests: **244 passed / 0 failed / 0 skipped**
- win-x64 self-contained publish: **SUCCESS**
- candidate EXE Product UI / Scanner / Main Map / Factory / MiniMap smoke: **SUCCESS**
- graceful shutdown: **SUCCESS**
- exact release source build run `32462093818`: build/tests/publish/exact EXE smoke/ZIP 생성 **SUCCESS**
- Draft asset metadata/hash/size/ProductVersion/FIRST_RUN 검증: **SUCCESS**
- Draft-downloaded EXE smoke: **SUCCESS**
- public/latest transition: **SUCCESS**
- public tag target exact SHA 검증: **SUCCESS**
- public asset re-download/hash/size/ProductVersion/FIRST_RUN 검증: **SUCCESS**
- public-downloaded EXE smoke: **SUCCESS**
- final recovery/public verification run `32462693267`: **SUCCESS**

초기 release run은 Draft 생성 이후 검증용 git refspec 문자열 오류로 중단됐습니다. 제품 빌드나 배포 자산 문제는 아니었으며, 생성된 동일 Draft를 재빌드하지 않고 검증해 public으로 승격했습니다.

## 최종 공개 기록

```text
release: v1.1.2
release id: 374253005
release source / public tag target SHA: f19d0f6993693aba4eaa26a4bde203c5731f0aad
release build run: 32462093818
final Draft/Public verification run: 32462693267
automated tests: 244 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.2-win-x64.zip
bytes: 80,238,099
SHA-256: 8a9613b0b2b06a731a7c6d607f0ed8c9b2991dd73a4789a1058242bb181d87f9
published EXE bytes: 83,813,638
ProductVersion: 1.1.2+f19d0f6993693aba4eaa26a4bde203c5731f0aad
Draft downloaded EXE smoke: SUCCESS
public downloaded EXE smoke: SUCCESS
```

## 실제 Tarkov 후속 검증

v1.1.2 공개 후 사용자가 같은 `Ophthalmoscope 검안경` 화면을 우선 다시 검사합니다.

확인 순서:

1. geometry candidate가 외곽 상세창 크기로 잡히는지
2. 최근 인식 기록의 OCR 문자열이 실제 제목을 읽는지
3. current catalog match가 성공하는지
4. 다른 종류 아이템 상세창에서도 제목 행만 읽는지
5. detector 세분화로 인한 CPU 증가가 체감되지 않는지

문제가 남으면 `%LocalAppData%/JunhyunHelper/logs/scanner.log`의 `geometry-candidate`, `ocr-result`, `match-result`를 기준으로 detector → OCR → matcher 계층을 분리해 보정합니다.

상세 설계 결정: `docs/SCANNER_TITLE_ROI_DECISION_2026-08-21.md`.
