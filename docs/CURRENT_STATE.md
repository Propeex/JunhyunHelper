# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계/이력은 `docs/STATE.md`와 전문 문서를 참조합니다.

기준일: 2026-08-21

상태: **`v1.1.5 PUBLIC RELEASE / VERIFIED — Scanner + Mini Scanner reliability / Tarkov title-font recovery`**

## 현재 공개 기준선

```text
version: v1.1.5
release source / public tag: 3541bab6536ff91a00f394c4f7b03d5cbf112746
PR final candidate CI: 32493986403 — SUCCESS
initial exact-source release run: 32494487841 — build/test/package/Draft creation passed; Draft tag-order automation defect after creation
Draft resume/public verification run: 32495042444 — SUCCESS
independent public verification run: 32495225958 — SUCCESS
automated tests: 249 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.5-win-x64.zip
bytes: 80,269,429
SHA-256: dc31177ae1bd4d152453a010dffe6cbb1e6c1d2a4a7e2eb82fb7444fa99c0748
ProductVersion: 1.1.5+3541bab6536ff91a00f394c4f7b03d5cbf112746
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
independent public-downloaded EXE smoke: SUCCESS
release: https://github.com/Propeex/JunhyunHelper/releases/tag/v1.1.5
```

```text
Desktop Version: 1.1.5
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
v1.1.4 → v1.1.5 mandatory Game Content update: none for schema/user data
v1.1.4 → v1.1.5 user.db migration: none
Scanner display settings: schema v2 one-time presentation-default normalization
```

## v1.1.5 Scanner / Mini Scanner 변경

- Mini Scanner는 **식별된 Item 정보만** 표시; 대기/OCR/오류/진단 상태 문구는 overlay에 표시하지 않음
- WPF `Topmost` + native `HWND_TOPMOST`, `WS_EX_NOACTIVATE`, `WS_EX_TOOLWINDOW`
- 전체 카드가 drag hitbox이고 cursor는 강제 Arrow
- 실사용 overlay는 foreground Tarkov의 한국어 inventory/stash UI anchor를 2개 이상 확인할 때만 표시; 불확실하면 숨김
- title/context WinRT OCR은 하나의 serialized boundary 사용
- raw `traderPrices` + derived `sellFor` 모두 지원; flea 제외 best trader price
- market coverage가 비정상적으로 비어 있는 catalog는 정상 cache를 덮지 못함
- 기존 설치에서 icon/trader/trader-per-slot 표시 기본값을 schema v2로 1회 정상화
- Game Content update 시 전체 canonical Item icon prefetch
- 기존 Scanner Lab v3.8 구조와 current official Korean catalog identity contract 유지

## 상세창 제목 폰트 기반 복구

```text
normal ko-KR OCR
→ current official Korean catalog semantic gate
→ 필요 시 existing Deep OCR
→ 여전히 실패한 경우에만:
   official-name shortlist
   → Tarkov Bender primary + Noto Sans CJK KR Hangul fallback 렌더링
   → 실제 title ROI glyph shape 비교
   → semantic + visual + top1/top2 margin 모두 통과 시 복구
→ Item ID
```

중요:

- 기존 OCR 성공 결과는 font verifier가 변경/거부하지 않음
- current official Korean catalog가 계속 Item identity 권위
- font shape는 보조 증거일 뿐 독립 identity source가 아님
- Bender 바이너리는 공개 ZIP에 포함하지 않음
- 실행 중 사용자의 `EscapeFromTarkov_Data/resources.assets`를 read-only로 확인해 필요한 Bender/Noto SFNT만 `%LocalAppData%/JunhyunHelper/scanner/fonts`에 cache
- game asset 탐색/추출/검증 실패 시 기존 OCR-only 경로로 자동 fallback
- game directory는 수정하지 않음

## Scanner 핵심 계약

```text
pixels
→ RED-X + rectangle/edge candidates
→ IoU dedup
→ 최대 8 candidates
→ adaptive ko-KR OCR
→ current official Korean full-item catalog semantic validation
→ optional conservative font-aware recovery after failed deep OCR
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
- **249/249 automated tests**
- Scanner Lab v3.8 geometry/title ROI regressions
- raw `traderPrices` / market-health regressions
- self-contained single-file publish
- actual EXE Product UI / Mini Scanner / title-font parser / Scanner / Map / Factory / MiniMap smoke
- graceful shutdown / clean portable root
- Draft ZIP 재다운로드 checksum/root/ProductVersion/FIRST_RUN 검증
- Draft-downloaded EXE smoke
- public/latest 전환
- tag `v1.1.5` = exact source `3541bab...`
- public ZIP 재다운로드 checksum/root/ProductVersion/FIRST_RUN 검증
- public-downloaded EXE smoke
- **독립 public verification run `32495225958` 전체 성공**

## 실제 Tarkov 후속 검증

최신 Tarkov Borderless의 inventory/stash anchor OCR과 실제 설치의 `resources.assets` 폰트 추출은 환경 의존 empirical validation입니다. 자동 릴리즈 gate는 parser/fallback/product behavior를 검증했지만 CI runner에는 Tarkov 설치 자체가 없습니다.

문제 발생 시 `%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)`의 `inventory-context`, `title-font-*`, candidate/OCR/match 기록으로 단계별 진단하며, 인식 확신 기준을 약화하지 않고 PATCH로 보정합니다.

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout | 구현 완료 |
| Needed Items / Inventory / Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / Windows user validated |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / v1.1.5 public package independently verified |
| Scanner + Mini Scanner | **v1.1.5 public verified / live Tarkov environment validation ongoing** |
