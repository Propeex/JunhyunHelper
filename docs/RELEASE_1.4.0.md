# 준현 헬퍼 v1.4.0 릴리즈

상태: **RELEASE CANDIDATE**

기준일: 2026-08-23

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
Content schema: 기존 호환 유지
user.db: migration 없음
Scanner display settings: 기존 호환 유지
Scanner catalog cache: 기존 호환 유지
Scanner Ground Truth dataset: 신규 local diagnostics persistence
v1.3.5 → v1.4.0 mandatory Game Content update: none
```

Scanner Ground Truth는 `%LocalAppData%/JunhyunHelper/scanner/diagnostics`에 별도 저장되며 일반 scanner.log와 삭제 경계를 분리합니다.

## 릴리즈 gate

최종 공개 전 다음을 모두 확인합니다.

1. release-source Windows Release build
2. 268 tests / 0 failed / 0 skipped
3. win-x64 self-contained single-file publish/package audit
4. packaged EXE rendered Product UI + Map/Factory/MiniMap smoke
5. exact source SHA ↔ ProductVersion 일치
6. Draft release ZIP 재다운로드 + SHA-256/size/layout 검증
7. Draft-downloaded EXE smoke + graceful shutdown
8. public/latest 전환
9. exact public tag source 검증
10. public ZIP 재다운로드 + SHA256SUMS 검증
11. public-downloaded EXE smoke + graceful shutdown

최종 source SHA, release run, asset 크기와 SHA-256은 공개 검증 완료 후 이 문서와 기계 판독 status 문서에 기록합니다.

관련 공식 문서:

- `docs/SCANNER_GROUND_TRUTH.md`
- `docs/CURRENT_SCANNER_WORK.md`
- `docs/VERSIONING.md`
