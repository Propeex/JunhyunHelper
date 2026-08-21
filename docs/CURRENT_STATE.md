# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계/이력은 `docs/STATE.md`와 전문 문서를 참조합니다.

기준일: 2026-08-21

상태: **`v1.1.4 PUBLIC RELEASE / VERIFIED — Scanner hardening / data reliability / log clear`**

## 현재 공개 기준선

```text
version: v1.1.4
release source: 833ac66c522632a695d106bd7ca9b1d6bfc030dc
PR final CI: 32475893012 — SUCCESS
exact-source Draft-first release run: 32476391800
public verification run: 32476952938 — SUCCESS
automated tests: 247 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.4-win-x64.zip
bytes: 80,253,044
SHA-256: 6d7a4646032c91a66d66ceac0d78b197dd112e78fa9c7a6e99d7092febc2cb54
ProductVersion: 1.1.4+833ac66c522632a695d106bd7ca9b1d6bfc030dc
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Desktop Version: 1.1.4
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
v1.1.3 → v1.1.4 mandatory Game Content update: none
v1.1.3 → v1.1.4 user.db migration: none
```

## v1.1.4 Scanner 변경

- Scanner Lab v3.8 multi-candidate semantic validation 유지
- 연속 frame의 동일 quantized geometry signature가 있을 때만 2-hit 안정화 누적
- verified detail은 OCR 반복 없이 1초 간격 presentation snapshot refresh
- `현재 필요한 수량`은 최신 `ItemsWorkspace.Plan.NeededItems[].RequiredTotal` 재연결
- Scanner local icon process-memory decode cache
- 최고 상점가 = fleaMarket 제외 `sellFor.priceRUB` 최댓값 회귀 고정
- 플리 평균가 = `avg24hPrice` 회귀 고정
- 4,000개 전체 카탈로그 fixture의 market/dimension 투영 검사
- invalid market/dimension은 field 단위 fail-closed
- 최근 인식 기록 우측 상단 `로그 삭제`
- 로그 삭제 = activity + `scanner.log` + `scanner.log.1`
- published EXE smoke에서 로그/activity 생성, rendered button 클릭, 두 로그 삭제까지 실제 검증

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

## 공개 릴리즈 검증

완료:

- Windows Release build
- 247/247 automated tests
- Scanner Lab v3.8 geometry/title ROI regressions
- Scanner market-field 전체 fixture regressions
- self-contained single-file publish
- actual EXE rendered Product UI / Scanner log clear / Map / Factory / MiniMap smoke
- Draft ZIP 재다운로드 checksum/package/ProductVersion 검증
- Draft-downloaded EXE smoke
- public/latest 전환
- tag `v1.1.4` = source `833ac66c522632a695d106bd7ca9b1d6bfc030dc` identical 검증
- public ZIP 재다운로드 checksum/root/ProductVersion/FIRST_RUN 검증
- public-downloaded EXE smoke + 정상 종료

첫 release workflow `32476391800`은 public 전환 직후 태그 재조회 refspec 문자열 버그로 마지막 자동 단계만 실패했습니다. 제품/패키지 gate는 그 전에 통과했고, 독립 public verification run `32476952938`에서 누락된 public 검증을 모두 다시 수행해 최종 승인했습니다.

## 실제 Tarkov 후속 검증

최신 Tarkov Borderless E2E는 기존 정책대로 public release blocker가 아니며 사용자 환경에서 계속 수행합니다. 실제 게임에서 발견되는 문제는 `%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)`와 최근 인식 기록을 기준으로 capture → candidate → OCR → matcher → presentation 단계를 분리해 후속 PATCH에서 보정합니다.

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout | 구현 완료 |
| Needed Items / Inventory / Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / Windows user validated |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / v1.1.4 public package verified |
| Scanner + Mini Scanner | **v1.1.4 public verified / live Tarkov validation ongoing** |
