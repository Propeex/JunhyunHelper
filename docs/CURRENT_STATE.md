# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계/이력은 `docs/STATE.md`와 전문 문서를 참조합니다.

기준일: 2026-08-21

상태: **`v1.1.4 RELEASE CANDIDATE — Scanner hardening / data reliability / log clear`**

## 공개 기준선

현재 public stable은 v1.1.3이며 v1.1.4 public release 검증을 진행합니다.

```text
Desktop target version: 1.1.4
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
v1.1.3 → v1.1.4 mandatory Game Content update: none
v1.1.3 → v1.1.4 user.db migration: none
automated tests: 247
```

## v1.1.4 Scanner 변경

- Scanner Lab v3.8 multi-candidate semantic validation 유지
- 연속 frame의 동일 quantized geometry signature가 있을 때만 2-hit 안정화 누적
- verified detail은 OCR 반복 없이 1초 간격 presentation snapshot refresh
- `현재 필요한 수량`은 최신 `ItemsWorkspace.Plan.NeededItems[].RequiredTotal` 재연결
- Scanner local icon process-memory decode cache
- 최고 상점가 = fleaMarket 제외 `sellFor.priceRUB` 최댓값 회귀 고정
- 플리 평균가 = `avg24hPrice` 회귀 고정
- invalid market/dimension은 field 단위 fail-closed
- 최근 인식 기록 우측 상단 `로그 삭제`
- 로그 삭제 = activity + `scanner.log` + `scanner.log.1`
- published EXE smoke에서 로그 생성/버튼 삭제까지 실제 검증

## Scanner 핵심 계약

```text
pixels
→ RED-X + rectangle/edge candidates
→ IoU dedup
→ 최대 8 candidates
→ adaptive ko-KR OCR
→ current official Korean full-item catalog semantic validation
→ Item ID
→ existing JunhyunHelper data
→ Mini Scanner
```

- false positive보다 miss 선호
- matcher confidence/margin 완화 금지
- scan-time network 없음
- game memory / DLL injection / packet interception / icon identity 없음
- current needed = `RequiredTotal`

## 검증

최종 release gate:

- Windows Release build
- 247 tests
- Scanner Lab v3.8 geometry/title ROI regressions
- market-field regressions
- self-contained single-file publish
- actual EXE rendered UI + Scanner log clear + Map/Factory/MiniMap smoke
- Draft/public asset checksum/ProductVersion 검증
- exact public tag 검증
- public-downloaded EXE smoke

최종 release source/run/hash는 `docs/RELEASE_1.1.4.md`에 기록합니다.

## 실제 Tarkov 후속 검증

최신 Tarkov Borderless E2E는 기존 정책대로 public release blocker가 아니며 사용자 환경에서 계속 검증합니다. 문제는 `%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)`를 기준으로 후속 PATCH에서 분리·보정합니다.

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout | 구현 완료 |
| Needed Items / Inventory / Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / Windows user validated |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 |
| Scanner + Mini Scanner | **v1.1.4 release candidate / live Tarkov validation ongoing** |
