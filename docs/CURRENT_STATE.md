# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-22

상태: **`v1.2.0 PUBLIC RELEASE / VERIFIED — Scanner title recognition overhaul`**

## 현재 공개 기준선

```text
version: v1.2.0
release source: a7601f8498e8d75e832962fb9dd60f4112d28dc6
exact-source release run: 32514322439 — SUCCESS
automated tests: 255 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.2.0-win-x64.zip
bytes: 80,298,514
SHA-256: ab5e9ef35b300268d16a1c5eece86cd8c6e57c91c83364caf4b7d02cde1d27d1
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Desktop Version: 1.2.0
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v3
Scanner cache schema: v1/v2 readable, v2 written
v1.1.6 → v1.2.0 mandatory Game Content update: none
v1.1.6 → v1.2.0 user.db migration: none
```

## Scanner 현재 계약

```text
Tarkov / Display pixels
→ detail structural candidates
→ red close + magnifier + title-field anchors
→ magnifier-free title ROI
→ Windows ko-KR OCR + current-catalog character validation
→ conservative official-name matching
   OR conservative full-catalog Tarkov-font visual recovery
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

## v1.2.0 핵심 변경

- red X / magnifier / title field 기반 title ROI 보정
- magnifier anchor 발견 시 OCR 영역에서 돋보기 픽셀 제외
- current official Korean item-name catalog 기반 허용 문자 정책
- Korean title에 대한 Han ideograph OCR invalid 처리
- empty/corrupt OCR에 대한 full-catalog Tarkov-font visual recovery
- `인식 이미지` in-memory diagnostic view
- `1회 고정밀 스캔`
- 기본 global hotkey `Ctrl+Shift+F10`, 변경/비활성화 가능
- continuous loop와 one-shot capture/OCR/presentation state 직렬화
- Scanner display settings schema v3

상세: `docs/RELEASE_1.2.0.md`, `docs/SCANNER.md`, `docs/SCANNER_TEST_PLAN.md`.

## Mini Scanner / data baseline

- matched item 정보만 표시; runtime/status 문구는 overlay에 표시하지 않음
- Topmost + no-activate
- 전체 카드 drag hit surface + Arrow cursor
- 실제 모드에서는 Tarkov foreground/inventory context를 보수적으로 확인
- 전체 canonical item icon prefetch
- raw `traderPrices`와 derived `sellFor` 모두 지원
- title OCR과 inventory-context OCR은 직렬화된 OCR 경계를 공유
- market field 누락은 해당 표시만 비우며 identity catalog health와 분리

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout | 구현 완료 |
| Needed Items / Inventory / Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / 기존 검증 기준선 유지 |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / v1.2.0 public package verified |
| Scanner + Mini Scanner | **v1.2.0 public baseline / live Tarkov validation and follow-up fixes ongoing** |

실제 Tarkov에서 발견되는 문제는 `scanner.log`와 `인식 이미지`를 근거로 capture → candidate → anchors/ROI → OCR/visual matcher → catalog → presentation → overlay 단계로 분리해 후속 PATCH에서 수정합니다.
