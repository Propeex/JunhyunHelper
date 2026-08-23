# 준현 헬퍼 v1.3.4 릴리즈 검증

상태: **RELEASE CANDIDATE — PUBLIC RELEASE PENDING**

기준일: 2026-08-23

## 릴리즈 목적

v1.3.3 공개 후 실제 Tarkov Scanner 사용에서 재현된 recognition/diagnostics 결함을 하나의 PATCH로 수정합니다.

- embedded `「` 계열 OCR glyph loss를 current-catalog unique one-unknown-glyph recovery로 보강
- title glyph가 magnifier로 승격되는 경로를 fixed search-icon lane + normalized template로 차단
- close/X를 red blob + X shape template로 강화
- full `HEADER_FRAME_LOCKED` 후보만 OCR identity path에 유지
- locked header에서 detail-window top/left/right bounds 재정렬
- 사용자 저장 PNG에 detail/title/magnifier/close 진단 rectangle 합성

## 변경하지 않는 제품 의미

- current official Korean item catalog가 identity source of truth
- normal confidence/top1-top2 margin
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

## Pre-release Windows gate

```text
PR: #146
pre-identity CI: 32635992721 — SUCCESS
automated tests: 267 passed / 0 failed / 0 skipped
Release build: SUCCESS
win-x64 publish: SUCCESS
packaged EXE Product UI + Scanner + Mini Scanner + Main Map + Factory + MiniMap smoke: SUCCESS
diagnostic PNG overlay renderer smoke: SUCCESS
graceful shutdown: SUCCESS
```

## 최종 릴리즈 식별자

아래 값은 PR #146 final head CI와 main 병합 후 exact-source release controller가 완료된 뒤 확정합니다.

```text
exact release source: PENDING
public tag source: PENDING
final PR CI: PENDING
release run: PENDING
asset: Junhyun-Helper-v1.3.4-win-x64.zip
bytes: PENDING
SHA-256: PENDING
ProductVersion: 1.3.4+<exact release source SHA>
public/latest: PENDING
exact public tag source: PENDING
public re-download: PENDING
public-downloaded EXE smoke: PENDING
```

## Release blocker

다음이 모두 통과하기 전에는 v1.3.4를 public stable로 기록하지 않습니다.

1. final PR head Windows Release build
2. 267 tests / 0 failed / 0 skipped
3. win-x64 self-contained publish/package audit
4. actual packaged EXE Product UI/Scanner/Mini Scanner/Map smoke
5. exact source SHA와 ProductVersion 일치
6. Draft release asset re-download + SHA-256/size/layout/ProductVersion/FIRST_RUN 검증
7. Draft-downloaded EXE smoke
8. public/latest 전환
9. exact public tag source 검증
10. public ZIP 재다운로드 + SHA256SUMS/size/layout/ProductVersion/FIRST_RUN 검증
11. public-downloaded EXE smoke + graceful shutdown

상세 기술 결정: `docs/DECISION_SCANNER_V1.3.4_LIVE_HARDENING_2026-08-23.md`
