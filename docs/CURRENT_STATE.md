# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-23

상태: **`v1.2.2 PUBLIC RELEASE / VERIFIED — Scanner catalog mode-transition hardening`**

## 현재 공개 기준선

```text
version: v1.2.2
release source: e3925cbc55215c7de0502c9b6b1ff1428d2f272b
final PR CI: 32590303579 — SUCCESS
exact-source release run: 32590701086 — SUCCESS
independent public finalizer: 32607942093 — SUCCESS
automated tests: 256 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.2.2-win-x64.zip
bytes: 80,302,910
SHA-256: 125d4a5b0e6db64f6772cc63c112f13cbcdac2fb7bc9ce501313ca2fc3645d7c
ProductVersion: 1.2.2+e3925cbc55215c7de0502c9b6b1ff1428d2f272b
public/latest: VERIFIED
exact public tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Desktop Version: 1.2.2
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v3
Scanner cache schema: v1/v2 readable, v2 written
v1.2.1 → v1.2.2 mandatory Game Content update: none
v1.2.1 → v1.2.2 user.db migration: none
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

## v1.2.2 핵심 변경

- Scanner catalog disk cache load와 network refresh를 같은 operation gate로 직렬화
- 다른 GameMode refresh의 in-memory clear도 gate 안에서 수행
- 이전 모드의 오래된 in-flight refresh가 새 profile/GameMode cache state를 뒤늦게 덮어쓰는 race 제거
- cache load가 Scanner catalog lifetime cancellation을 사용하도록 보강
- 실제 경합 순서를 재현하는 deterministic concurrency regression test 추가
- 자동 테스트 255 → 256
- OCR/detector/visual confidence/top1-top2 margin 변경 없음
- 최고 상점가/플리 평균가/RequiredTotal 의미 변경 없음

## v1.2.1 기준선 유지

- `resources.assets` title-font discovery bounded streaming scan
- Tarkov source manifest + actual font-binary generation hash
- generation-aware bounded visual template caches
- Mini Scanner inventory/stash OCR single active probe + latest-request coalescing + stale-result rejection
- one-shot/profile/GameMode lifecycle serialization + latest-mode restore rule
- shutdown-safe font-aware OCR active-operation lifetime
- PrintWindow sparse validation의 redundant full-frame managed copy 제거
- title-anchor diagnostic score에 실제 detector evidence 보존
- v1.2.0의 `인식 이미지`, `1회 고정밀 스캔`, title-anchor/Tarkov-font recovery 기능 유지

상세: `docs/RELEASE_1.2.2.md`, `docs/SCANNER.md`, `docs/SCANNER_TEST_PLAN.md`.

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
| Program Update | 구현 완료 / v1.2.2 public package verified |
| Scanner + Mini Scanner | **v1.2.2 public verified / live Tarkov calibration 및 evidence-based follow-up ongoing** |

실제 Tarkov에서 발견되는 문제는 `scanner.log`와 `인식 이미지`를 근거로 capture → candidate → anchors/ROI → OCR/visual matcher → catalog → presentation → inventory gate → overlay → performance 단계로 분리해 후속 PATCH에서 수정합니다.

코드/자동 검증만으로 확정 가능한 deterministic defect도 threshold 추측 없이 후속 PATCH 대상으로 다룹니다.
