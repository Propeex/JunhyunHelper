# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계/이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-21

상태: **`v1.1.3 PUBLIC RELEASE / VERIFIED — Scanner Lab v3.8 recognition restored`**

## 현재 공개 기준선

```text
version: v1.1.3
release source: 8803f899341859887281ad50135911f4625a64f3
release verification run: 32470606548
automated tests: 245 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.3-win-x64.zip
bytes: 80,251,960
SHA-256: 419f6288aa3202f10868f2fe6a4ccac40475753ce4ba8c8c2d9985396c4bf493
ProductVersion: 1.1.3+8803f899341859887281ad50135911f4625a64f3
Draft downloaded EXE smoke: SUCCESS
public downloaded EXE smoke: SUCCESS
```

```text
Desktop Version: 1.1.3
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
v1.1.2 → v1.1.3 mandatory Game Content update: none
v1.1.2 → v1.1.3 user.db migration: none
```

## Scanner recognition — Scanner Lab v3.8 복원

사용자가 보존하고 있던 `TarkovHelper-ScannerLab-v3.8` 원본을 다시 확보해 실제로 성공했던 recognition 구조를 현재 JunhyunHelper Scanner 경계에 복원했습니다.

```text
capture
→ RED-X connected-component candidates
+
→ rectangle-structure fallback candidates
→ IoU deduplication
→ 최대 8개 structural candidates
→ title ROI
→ adaptive 4x / 6x / 8x Windows ko-KR OCR
→ current official full-item catalog resolver
→ 필요 시 상위 3개 candidate deep OCR
   - original
   - high-contrast grayscale
   - binary
   - inverse binary
→ official item name으로 안전하게 resolve된 candidate만 inspect window로 확정
→ Item ID
→ existing JunhyunHelper data bridge
→ Mini Scanner
```

핵심 원칙:

- structural score는 후보 순위일 뿐 최종 사실 판정이 아님
- 하나의 geometry rectangle을 즉시 상세창으로 확정하지 않음
- current official Korean full-item catalog를 semantic validator로 사용
- OCR line 개별 후보 + 인접 두 line 결합 후보 검사
- matcher confidence/top1-top2 margin은 완화하지 않음
- historical Scanner Lab alias는 production에 추가하지 않음
- low confidence / ambiguity는 계속 fail-closed
- scan-time network 없음
- game memory / DLL injection / packet interception / icon identity 없음

상세 reference: `docs/SCANNER_LAB_3_8_REFERENCE.md`

## 검증

- Windows Release build: SUCCESS
- automated tests: **245 passed / 0 failed / 0 skipped**
- Scanner Lab v3.8 geometry regression: SUCCESS
- win-x64 self-contained single-file publish: SUCCESS
- exact package EXE Product UI / Scanner / Main Map / Factory / MiniMap smoke: SUCCESS
- Draft asset re-download/checksum/ProductVersion validation: SUCCESS
- Draft-downloaded EXE smoke: SUCCESS
- public/latest exact tag verification: SUCCESS
- public asset re-download/checksum/ProductVersion validation: SUCCESS
- public-downloaded EXE smoke: SUCCESS
- graceful shutdown / clean portable root: SUCCESS

회귀 기준에는 다음이 포함됩니다.

- cropped `Ophthalmoscope 검안경` 구조에서 v3.8 outer inspect + title ROI 재현
- full `Water 0.6L 물병` screenshot 구조에서 중앙 inspect + title ROI 재현
- 강한 내부 rectangle이 있어도 RED-X 외곽 candidate가 사라지지 않음
- RED-X가 없어도 rectangle fallback 유지
- uniform frame fail-closed

## Scanner UI / Mini Scanner

```text
상단 bar
  왼쪽: 스캐너 / 테스트
  오른쪽: 아이템 목록 최신화
↓
표시 정보 checkboxes
↓
최근 인식 기록
```

- Foundation 검증 controls는 일반 Scanner 탭에서 비노출
- Mini Scanner는 보이는 동안 직접 drag
- drag 종료 시 위치 저장
- Topmost / no-activate 유지
- 최근 인식 기록은 OCR text / candidate / confidence / 최종 판단을 사용자 문장으로 표시
- 개발자 로그: `%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)`
- screenshot/raw pixels는 로그에 저장하지 않음

v1.1.3 로그는 구조 후보와 후보별 semantic OCR pass를 기록해 실제 Tarkov 테스트에서 detector/OCR/matcher 병목을 분리할 수 있습니다.

## 실제 Tarkov 후속 검증

최신 Tarkov Borderless E2E는 DEC-051에 따라 공개 후 사용자 환경에서 계속 수행합니다.

우선 확인:

1. `Ophthalmoscope 검안경` 상세창이 Scanner Lab v3.8 수준으로 다시 감지되는지
2. 제목 OCR이 실제 제목을 읽는지
3. 공식 카탈로그와 semantic candidate validation이 성공하는지
4. 다른 상세창에서도 인식률이 복구됐는지
5. 오탐/미탐과 장시간 CPU 영향
6. Mini Scanner / MiniMap / Alt+Tab 공존

문제가 있으면 `scanner.log`의 candidate/ocr/match/selected 기록을 기준으로 후속 PATCH를 진행합니다.

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
| Program Update | 구현 완료 |
| Scanner | **v1.1.3 public verified / Scanner Lab v3.8 recognition restored / live Tarkov revalidation ongoing** |

## 다음 작업

현재 공개 v1.1.3을 실제 Tarkov Borderless 환경에서 검증합니다. 인식 문제가 남으면 Scanner Lab v3.8 구조를 유지한 채 `scanner.log`로 capture → candidate → OCR → semantic resolver 계층을 분리해 수정합니다.
