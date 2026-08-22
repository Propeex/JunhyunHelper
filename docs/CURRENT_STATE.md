# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-22

상태: **`v1.2.1 PUBLIC RELEASE / VERIFIED — Scanner stability and accuracy hardening`**

## 현재 공개 기준선

```text
version: v1.2.1
release source: 8c0de649f18d7caa4f5669a06511c15e784dfd29
final PR CI: 32540688111 — SUCCESS
exact-source release run: 32542259521 — SUCCESS
automated tests: 255 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.2.1-win-x64.zip
bytes: 80,306,749
SHA-256: 48a8b54fcdc3346a092ef3da2744f2d4ca7e27d99da5b52e3ebee7b55fa0affa
ProductVersion: 1.2.1+8c0de649f18d7caa4f5669a06511c15e784dfd29
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact public tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Desktop Version: 1.2.1
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v3
Scanner cache schema: v1/v2 readable, v2 written
v1.2.0 → v1.2.1 mandatory Game Content update: none
v1.2.0 → v1.2.1 user.db migration: none
```

## Scanner 현재 계약

```text
Tarkov / Display pixels
→ detail structural candidates
→ red close + magnifier + title-field anchors
→ magnifier-free title ROI
→ Windows ko-KR OCR + current-catalog character validation
→ conservative official-name matching
   OR conservative current-catalog Tarkov-font visual recovery
→ confidence + top1/top2 margin
→ Item ID
→ local presentation data
→ Mini Scanner
```

핵심 원칙:

- false positive보다 miss 선호
- confidence/margin을 편의상 완화하지 않음
- current official Korean item catalog가 identity 권위
- scan-time network 없음
- game memory / DLL injection / packet interception 없음
- icon 하나만으로 Item identity 확정 금지
- 현재 필요한 수량 = `NeededItems[].RequiredTotal`

## v1.2.1 핵심 변경

- `resources.assets` title-font discovery bounded streaming scan
- Tarkov source manifest + actual font-binary generation hash
- generation-aware bounded visual template caches
- Mini Scanner inventory/stash OCR single active probe + latest-request coalescing + stale-result rejection
- one-shot/profile/GameMode lifecycle serialization + latest-mode restore rule
- shutdown-safe font-aware OCR active-operation lifetime
- PrintWindow sparse validation의 redundant full-frame managed copy 제거
- title-anchor diagnostic score에 실제 detector evidence 보존
- recognition confidence/top1-top2 margin 완화 없음
- v1.2.0의 `인식 이미지`, `1회 고정밀 스캔`, title-anchor/Tarkov-font recovery 기능 유지

상세: `docs/RELEASE_1.2.1.md`, `docs/SCANNER.md`, `docs/SCANNER_TEST_PLAN.md`.

## Mini Scanner / data baseline

- matched item 정보만 overlay에 표시
- Topmost + no-activate
- 전체 카드 drag hit surface + Arrow cursor
- 실제 mode에서는 Tarkov foreground/inventory context를 보수적으로 확인
- inventory/stash OCR probe는 동시에 최대 1개, stale epoch 결과 폐기
- canonical item 전체 icon prefetch
- raw `traderPrices` / derived `sellFor` 지원
- title OCR과 inventory-context OCR은 serialized WinRT OCR boundary 공유
- 가격/크기 field 누락은 해당 표시만 비우고 identity catalog health와 분리

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout | 구현 완료 |
| Needed Items / Inventory / Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / steady-state smoke 유지 |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / v1.2.1 public package verified |
| Scanner + Mini Scanner | **v1.2.1 public verified / live Tarkov calibration 및 evidence-based follow-up ongoing** |

실제 Tarkov에서 발견되는 문제는 `scanner.log`와 `인식 이미지`를 근거로 capture → candidate → anchors/ROI → OCR/visual matcher → catalog → presentation → inventory gate → overlay → performance 단계로 분리해 후속 PATCH에서 수정합니다.
