# 준현 헬퍼 v1.3.4 릴리즈 검증

상태: **PUBLIC RELEASE / VERIFIED**

기준일: 2026-08-23

## 릴리즈 목적

v1.3.3 공개 후 실제 Tarkov Scanner 사용에서 재현된 recognition/diagnostics 결함을 하나의 PATCH로 수정했습니다.

- embedded `「` 계열 OCR glyph loss를 current-catalog unique one-unknown-glyph recovery로 보강
- title glyph가 magnifier로 승격되는 경로를 fixed search-icon lane + normalized template로 차단
- close/X를 red blob + X shape template로 강화
- full `HEADER_FRAME_LOCKED` 후보만 OCR identity path에 유지
- locked header에서 detail-window top/left/right bounds 재정렬
- 사용자 저장 PNG에 detail/title/magnifier/close 진단 rectangle 합성

## 변경하지 않은 제품 의미

- current official Korean item catalog가 identity source of truth
- normal confidence/top1/top2 margin
- bounded unique one-edit recovery 조건
- highest trader = valid non-flea RUB max
- flea average = positive `avg24hPrice`
- current needed = `NeededItems[itemId].RequiredTotal`
- scan-time network 없음
- game memory / injection / packet interception 없음

## Schema

```text
Desktop Version: 1.3.4
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.3.3 → v1.3.4 mandatory Game Content update: none
v1.3.3 → v1.3.4 user.db migration: none
```

## 최종 릴리즈 식별자

```text
exact release source: a78ddbc649747f1320236556f17e6b908304674a
public tag source: a78ddbc649747f1320236556f17e6b908304674a
final PR CI: 32636665202 — SUCCESS
release run: 32636927134 — SUCCESS
independent public verifier: 32637159066 — SUCCESS
automated tests: 267 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.3.4-win-x64.zip
bytes: 80,319,654
SHA-256: 8c442fec81a0b993a9a6b080e59b656668a7a73d8fadd8434595545b08c82e8e
ProductVersion: 1.3.4+a78ddbc649747f1320236556f17e6b908304674a
public/latest: VERIFIED
exact public tag source: VERIFIED
Draft re-download: VERIFIED
Draft-downloaded EXE smoke: SUCCESS
public re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

## 검증한 릴리즈 gate

1. final PR head Windows Release build — **SUCCESS**
2. 267 tests / 0 failed / 0 skipped — **SUCCESS**
3. win-x64 self-contained publish/package audit — **SUCCESS**
4. actual packaged EXE Product UI/Scanner/Mini Scanner/Main Map/Factory/MiniMap smoke — **SUCCESS**
5. exact source SHA와 ProductVersion 일치 — **VERIFIED**
6. Draft release asset re-download + SHA-256/size/layout/ProductVersion/FIRST_RUN 검증 — **VERIFIED**
7. Draft-downloaded EXE smoke + graceful shutdown — **SUCCESS**
8. public/latest 전환 — **VERIFIED**
9. exact public tag source — **VERIFIED**
10. public ZIP independent re-download + SHA256SUMS/size/layout/ProductVersion/FIRST_RUN 검증 — **VERIFIED**
11. public-downloaded EXE smoke + graceful shutdown — **SUCCESS**
12. one-shot release/public-verifier workflows cleanup — **VERIFIED**

기계 판독 가능한 최종 증거는 `docs/.release-v1.3.4-status.json`에 보존합니다.

상세 기술 결정:

- `docs/DECISION_SCANNER_V1.3.4_LIVE_HARDENING_2026-08-23.md`
- `docs/SCANNER_V1.3.4_LIVE_HARDENING.md`
