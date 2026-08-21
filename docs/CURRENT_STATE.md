# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-22

상태: **`v1.1.6 PUBLIC RELEASE / VERIFIED — Scanner catalog synchronization regression fix`**

## 현재 공개 기준선

```text
version: v1.1.6
release source: 8efee02e5966adb9b67b47847f95a12dfc357d0a
exact-source release run: 32500707112 — SUCCESS
automated tests: 250 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.6-win-x64.zip
bytes: 80,271,024
SHA-256: 986d0d2855381060267f63d2902317eabedc5d5738448fbd6c2b09e764c3477e
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Desktop Version: 1.1.6
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner cache schema: v1/v2 readable, v2 written
v1.1.5 → v1.1.6 mandatory Game Content update: none
v1.1.5 → v1.1.6 user.db migration: none
```

## Scanner 현재 계약

```text
Tarkov / Display pixels
→ detail structural candidates
→ title ROI
→ Windows ko-KR OCR
→ conservative current official Korean catalog matching
→ Item ID
→ local presentation data
→ Mini Scanner
```

핵심 원칙:

- false positive보다 miss 선호
- matcher confidence/margin을 편의상 완화하지 않음
- scan-time network 없음
- game memory / DLL injection / packet interception 없음
- item identity를 icon 하나로 확정하지 않음
- 현재 필요한 수량 = `NeededItems[].RequiredTotal`

## v1.1.5 이후 Mini Scanner / data baseline

- matched item 정보만 표시; runtime/status 문구는 overlay에 표시하지 않음
- Topmost + no-activate
- 전체 카드 drag hit surface + Arrow cursor
- 실제 모드에서는 Tarkov foreground/inventory context를 보수적으로 확인
- 전체 canonical item icon prefetch
- raw `traderPrices`와 derived `sellFor` 모두 지원
- title OCR과 inventory-context OCR은 직렬화된 OCR 경계를 공유

## v1.1.6 catalog fix

- Scanner identity catalog health = 4,000개 이상 유효 Item ID/공식 이름
- trader/flea 가격 coverage는 identity health와 분리
- 가격이 없으면 해당 표시 필드만 비움
- 4,000개 identity + trader price 0개도 Scanner 식별 가능
- 3,999개 identity는 계속 거부
- 수동 `아이템 목록 최신화`는 `catalog-sync` 진단을 `scanner.log`에 기록

상세: `docs/RELEASE_1.1.6.md`.

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout | 구현 완료 |
| Needed Items / Inventory / Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / 기존 검증 기준선 유지 |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / v1.1.6 public package verified |
| Scanner + Mini Scanner | **v1.1.6 public baseline / live Tarkov validation and follow-up fixes ongoing** |

실제 Tarkov에서 발견되는 문제는 기능별로 재현 조건과 로그를 분리해 후속 PATCH에서 수정합니다.
