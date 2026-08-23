# 준현 헬퍼 v1.4.0 릴리즈

상태: **PUBLIC RELEASE / VERIFIED**

기준일: 2026-08-23

## 공개 검증 결과

```text
version: v1.4.0 PUBLIC RELEASE / VERIFIED
release source: 1b7f565adec9dfa2546fb959c813310707aabd32
public tag source: 1b7f565adec9dfa2546fb959c813310707aabd32
feature PR #149 final CI: 32643727571 — SUCCESS
release-prep PR #150 final CI: 32644579509 — SUCCESS
automated tests: 268 passed / 0 failed / 0 skipped
release run: 32644951640 — SUCCESS
independent public verifier: 32645536757 — SUCCESS
asset: Junhyun-Helper-v1.4.0-win-x64.zip
bytes: 80,374,018
SHA-256: ef3676bbc7fb07fd45f4e9291e6fd4ef8a4a686a0f584cb1ddfdb6569376645f
ProductVersion: 1.4.0+1b7f565adec9dfa2546fb959c813310707aabd32
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download: VERIFIED
public SHA256SUMS: VERIFIED
public package layout: VERIFIED
public-downloaded EXE smoke: SUCCESS
one-shot release/verifier workflows: CLEANED UP
```

기계 판독 가능한 최종 증거:

- `docs/.release-v1.4.0-status.json`

## 릴리즈 목적

v1.4.0은 Scanner를 단순 인식 기능에서 **실사용 교정 → Ground Truth 축적 → 개발 분석 → 전체 회귀 검증**이 가능한 폐쇄형 Tarkov 인식 시스템으로 확장하는 MINOR 릴리즈입니다.

사용자가 새롭게 할 수 있는 일:

- 최신 Scanner 판정을 `맞음`으로 검증
- 상세보기 창 ROI / 아이템명 ROI를 직접 드래그 교정
- 정답 아이템명을 직접 입력
- 저장된 교정/진단 Case와 용량 확인
- 특정 Case만 삭제하거나 전체 dataset 삭제
- 개발 분석용 교정/진단 ZIP 생성
- 검증된 과거 원본을 현재 Scanner 전체 파이프라인으로 다시 실행해 회귀 테스트

## Ground Truth / diagnostics

각 Scanner diagnostic frame에 Case ID를 부여하고 `scanner.log`, 사용자 교정, dataset artifact를 같은 ID로 연결합니다.

보존 evidence:

- `full.png`
- `detail_window.png`
- `annotated.png`
- detected/corrected item-name ROI
- OCR preprocessing 재현 이미지
- `case.json`
- raw OCR / matcher text
- program Item ID / official name
- confidence / second score / margin
- 상위 matcher 후보 rank / Item ID / 공식명 / score
- pipeline stage
- 사용자 Ground Truth 오류 라벨
- 현재 mapped presentation values

자동 보존 사례는 `unreviewed`이며 사용자가 확인/교정한 사례만 Ground Truth로 취급합니다. 사용자 검증 Case는 늦게 끝난 background 자동 저장이 덮어쓰지 못합니다.

## 자동 분석

Dataset summary는 다음을 자동 집계합니다.

- 사용자 검증 최종 정확도
- Ground Truth 오류 유형
- observed pipeline stage
- detail-window ROI correction delta
- item-name ROI correction delta
- OCR ↔ Ground Truth 반복 문자 치환/삽입/누락 패턴

## Full-pipeline regression

`회귀 테스트`는 검증된 Case의 `full.png`를 현재 production 경로로 다시 처리합니다.

```text
full.png
→ detail geometry candidates
→ inspect header lock
→ title ROI
→ primary/deep OCR
→ Tarkov font corroboration/recovery
→ current official catalog matcher
→ final Item ID
```

각 Case 결과:

- `STILL_CORRECT`
- `SOLVED`
- `STILL_FAILING`
- `REGRESSION`
- `ERROR`

기존 정상 Ground Truth가 새 알고리즘에서 실패하면 평균 정확도가 올라도 `REGRESSION`으로 취급합니다.

## 현재 Scanner 데이터 의미

현재 화면 OCR 필드는 `item_name` 하나입니다.

- 최고 상점가
- 플리마켓 평균가
- 슬롯 / 슬롯당 가격
- 현재 필요한 개수

위 값은 Item ID 확정 이후 준현 헬퍼 로컬 데이터에서 조회/계산되는 `mapped_data`입니다. 존재하지 않는 숫자 OCR 필드를 인위적으로 추가하지 않았습니다.

## 변경하지 않은 인식 기준

Ground Truth 없이 다음 acceptance 기준을 임의 완화하지 않았습니다.

- detail structural floor
- strict `HEADER_FRAME_LOCKED`
- header anchor score 0.68
- matcher confidence / top1-top2 margin
- current official Korean item catalog authority

이번 릴리즈의 목적은 추측 기반 튜닝이 아니라 **정확한 실사용 evidence를 수집하고 같은 evidence로 이후 변경을 검증할 기반을 제품화하는 것**입니다.

## Schema / 데이터 호환성

```text
Desktop Version: 1.4.0
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache schema: v1/v2 readable, v2 written
Scanner Ground Truth dataset: 신규 local diagnostics persistence
v1.3.5 → v1.4.0 mandatory Game Content update: none
v1.3.5 → v1.4.0 user.db migration: none
```

Scanner Ground Truth는 `%LocalAppData%/JunhyunHelper/scanner/diagnostics`에 별도 저장되며 일반 scanner.log와 삭제 경계를 분리합니다.

## 검증된 릴리즈 gate

완료:

1. release-source Windows Release build — SUCCESS
2. 268 tests / 0 failed / 0 skipped — VERIFIED
3. win-x64 self-contained single-file publish/package audit — VERIFIED
4. packaged EXE rendered Product UI + Map/Factory/MiniMap smoke — SUCCESS
5. exact source SHA ↔ ProductVersion — VERIFIED
6. Draft release ZIP 재다운로드 + SHA-256/size/layout 검증 — release workflow SUCCESS
7. Draft-downloaded EXE smoke + graceful shutdown — release workflow SUCCESS
8. public/latest 전환 — VERIFIED
9. exact public tag source — VERIFIED
10. public ZIP 재다운로드 + SHA256SUMS 검증 — VERIFIED
11. public-downloaded EXE smoke + graceful shutdown — SUCCESS
12. 독립 public verifier — SUCCESS
13. 완료된 one-shot release/verifier workflow 제거 — VERIFIED

관련 공식 문서:

- `docs/SCANNER_GROUND_TRUTH.md`
- `docs/CURRENT_SCANNER_WORK.md`
- `docs/VERSIONING.md`
- `docs/.release-v1.4.0-status.json`
