# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계/이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-21

상태: **`v1.1.2 PUBLIC RELEASE / VERIFIED — Scanner 상세창·제목 ROI 회귀 수정`**

## 현재 공개 기준선

```text
release: v1.1.2
release id: 374253005
exact release source / public tag target SHA: f19d0f6993693aba4eaa26a4bde203c5731f0aad
asset: Junhyun-Helper-v1.1.2-win-x64.zip
bytes: 80,238,099
SHA-256: 8a9613b0b2b06a731a7c6d607f0ed8c9b2991dd73a4789a1058242bb181d87f9
ProductVersion: 1.1.2+f19d0f6993693aba4eaa26a4bde203c5731f0aad
automated tests: 244 passed / 0 failed / 0 skipped
final Draft/Public verification run: 32462693267
public downloaded EXE smoke: SUCCESS
```

v1.1.2는 v1.1.1 사용자 검증에서 발견된 Scanner detail geometry/title-ROI 통합 회귀를 수정한 PATCH입니다.

## Scanner 현재 상태

사용자 제공 현재 Tarkov 상세창에서 v1.1.1이 제목 대신 내부 영역 또는 `교환용 물품 > 의료용품` 분류 행을 OCR하는 문제를 재현했습니다.

원인은 catalog/matcher가 아니라 OCR 앞단의 detail-window geometry/title ROI였습니다.

v1.1.2에서:

- 현재 관측 상세창 구조를 약 `676x522 @ 1920x1080 UI scale` 기준으로 보정
- 상/하/좌/우 outer frame + 우상단 close-control을 구조 gate로 사용
- 신뢰도가 비슷한 작은 내부 사각형보다 큰 outer frame 우선
- strict frame gate를 유지한 채 위치 탐색 정밀도 개선
- 제목 OCR ROI를 상세창 최상단 한 줄로 축소
- 사용자 제공 `Ophthalmoscope 검안경` 화면 기준 category/breadcrumb 행을 ROI에서 제외
- candidate 검사 early reject로 추가 탐색 비용 완화
- matcher confidence/top1-top2 margin/fail-closed 정책은 변경하지 않음

상세: `docs/RELEASE_1.1.2.md`, `docs/SCANNER_TITLE_ROI_DECISION_2026-08-21.md`.

## Scanner 운용 UI

v1.1.1에서 확정한 운용 UI를 유지합니다.

```text
상단 bar
  왼쪽: 스캐너 / 테스트
  오른쪽: 아이템 목록 최신화
↓
표시 정보 checkboxes
↓
최근 인식 기록
```

- Foundation 검증 UI는 일반 Scanner 탭에서 숨김
- Mini Scanner 별도 위치 편집/초기화 control 없음
- Mini Scanner는 보이는 동안 직접 left-drag하고 위치 저장
- Topmost / ShowActivated=false / `WS_EX_NOACTIVATE` 유지
- 직접 drag 때문에 Mini Scanner 자기 영역은 mouse hit-test를 받음
- 기존 bounded `scanner.log(.1)` → `scanner.log`에서 최근 OCR/matcher 판정을 복원

## Scanner 핵심 파이프라인

```text
Tarkov/Display pixels
→ detail geometry detector
→ top title-row ROI
→ Windows ko-KR OCR
→ conservative full-catalog matcher
→ Item ID
→ existing JunhyunHelper data bridge
→ Mini Scanner
```

- real: `EscapeFromTarkov` Borderless client-area
- test: all connected displays
- real/test mutually exclusive
- no game memory / DLL injection / packet interception
- no icon identity
- no scan-time network
- current needed = `RequiredTotal`
- low confidence/ambiguity = no Item ID

## v1.1.2 검증

- PR #116 final CI `#1187` / run `32461315093`: SUCCESS
- exact release source build/tests/publish/smoke run `32462093818`
- 244 automated tests: SUCCESS
- strong-inner-rectangle detector regression: SUCCESS
- title ROI category-row exclusion regression: SUCCESS
- win-x64 self-contained package audit: SUCCESS
- exact published EXE Product UI / Scanner / Main Map / Factory / MiniMap smoke: SUCCESS
- Draft metadata/hash/size/ProductVersion/FIRST_RUN: SUCCESS
- Draft-downloaded EXE smoke: SUCCESS
- public/latest transition and public tag target: SUCCESS
- public re-download hash/size/ProductVersion/FIRST_RUN: SUCCESS
- public-downloaded EXE smoke: SUCCESS

## 실제 Tarkov 후속 검증

최신 Borderless Tarkov 실제 E2E는 계속 사용자 환경에서 검증합니다. 우선 v1.1.1에서 실패했던 같은 `Ophthalmoscope 검안경` 상세창을 v1.1.2로 재검사합니다.

문제가 남으면 다음 순서로 로그를 분리합니다.

```text
geometry-candidate
→ ocr-result
→ match-result
```

제목 ROI가 올바른데 OCR 문자열 자체가 약한 경우에만 Windows OCR 전처리/확대/대비를 별도 계층으로 개선합니다. matcher를 느슨하게 해서 OCR/ROI 결함을 숨기지 않습니다.

## 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / fail-closed availability |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 / future protection / ledger |
| Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / Windows user validated |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / public stable updater |
| Scanner | **v1.1.2 public verified / live Tarkov revalidation ongoing** |

## 데이터/호환성

```text
Desktop Version: 1.1.2
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
v1.1.1 → v1.1.2 mandatory Game Content update: none
v1.1.1 → v1.1.2 user.db migration: none
```

## 현재 비차단 범위

- EFT 1.0 Story Chapters ordinary task source 밖
- PvE Skier LL2 task-pool drift는 exact fact 없으면 fail-closed
- code signing / installer는 현재 필수 범위 아님
- Scanner 최신 live Tarkov E2E는 로그 기반 후속 검증
