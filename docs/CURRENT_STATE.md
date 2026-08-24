# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-24

상태: **`v1.6.0 PUBLIC RELEASE / VERIFIED — LIVE GROUND TRUTH MAINTENANCE`**

## 공개 stable과 현재 source

현재 공개 stable/latest는 **v1.6.0**이다.

```text
public stable/latest: v1.6.0
exact release source/tag: e18c108380572913552030aa677bba06ebf49355
stable asset: Junhyun-Helper.zip
stable bytes: 80,425,013
stable SHA-256: f9384ff49d522afb5976efe291ff932d66063dcfeee64b0aed7a5daa691a12c5
v1.5 bridge: Junhyun-Helper-v1.6.0-win-x64.zip
bridge bytes: 80,424,089
bridge SHA-256: 3f05b20ccbd7463fb590889042b1b706290a88e0568cd00c3b2fa23cf966dfc8
299 passed / 0 failed / 0 skipped
public verification run: 32710012954 — SUCCESS
public-downloaded EXE smoke: SUCCESS
```

`main`의 후속 문서/housekeeping commit은 release source가 아니다. v1.6.0 제품 source는 tag `v1.6.0`의 exact SHA로 고정한다.

## Schema / compatibility

```text
Desktop target version: 1.6.0
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1/v2 readable, v2 written
Scanner Ground Truth: local diagnostics persistence
```

사용자 mutable data는 계속 `%LocalAppData%/JunhyunHelper`에 둔다. Program Update는 user.db, content/image cache, Map/Ammo/Scanner 설정, Scanner logs/diagnostics/Ground Truth를 덮어쓰지 않는다.

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / latest task-pool live audit 반영 |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 |
| Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / steady-state smoke 유지 |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / v1.6 stable ZIP contract 반영 |
| Scanner + Mini Scanner | **v1.6.0 공개 검증 완료 / live Ground Truth maintenance** |

## v1.6.0 핵심 변경

- Scanner 일반 화면을 `스캐너 ON/OFF / 설정 / 고급` 중심으로 단순화
- 하단을 local item search / Scanner recognition log 2분할로 구성
- full-item catalog 검색, local cached icon/name, Wiki/flea/best trader/current needed presentation
- Mini Scanner icon/name fixed header
- Mini Scanner 다섯 정보 표시 여부 및 순서 저장
- Scanner display settings schema v6 migration
- 교정 image auto-fit + original pixel coordinate preservation
- candidate box 직접 클릭 선택
- manual rectangle / explicit `없음` fallback 유지
- saved Case reopen / Ground Truth re-edit
- stable user package `Junhyun-Helper.zip` → `준현 헬퍼/`
- CI에서 실제 stable ZIP 생성 및 내부 경로 검증

공식 문서:

- `docs/DECISION_V1.6.0_SCANNER_PRODUCT_WORKFLOW_2026-08-24.md`
- `docs/STATUS_V1.6.0_SCANNER_PRODUCT_WORKFLOW_2026-08-24.md`
- `docs/RELEASE_NOTES_V1.6.0.md`
- `docs/RELEASE_1.6.0.md`
- `docs/SCANNER.md`
- `docs/CURRENT_SCANNER_WORK.md`

## Scanner 안전 기준선

```text
Tarkov window pixels
→ detail rectangle proposals
→ red close-X + magnifier + neutral header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ Windows ko-KR OCR
→ optional user substitution
→ catalog sanitation / normalization
→ conservative official-catalog matching / bounded recovery
→ optional visual recovery
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
```

불변 계약:

- false positive보다 miss 선호
- geometry는 proposal이며 identity proof가 아님
- `HEADER_FRAME_LOCKED >= 0.68`
- magnifier + red close-X 필수
- structural floor `0.34`
- continuous max 8 / one-shot max 12 candidates
- current official Korean Tarkov item catalog가 identity authority
- production OCR field는 item-name 하나
- price / slots / needed는 Item ID 이후 local mapped data
- scan-time network 없음
- game memory read / DLL injection / packet interception 없음
- automatic global forced substitution table 없음
- cross-frame OCR cache 없음

## Scanner UI / hotkeys

일반 surface:

- Scanner ON/OFF
- 설정
- 고급
- item search
- recognition log

기존 one-shot 기능은 hotkey로 유지한다.

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

## Mini Scanner schema v6

항상 표시:

- icon
- official item name

사용자 표시/순서 설정:

- trader sell price
- flea average price
- trader price/slot
- flea price/slot
- current needed

v5 이하 설정은 자동 migration하고 기존 hotkey/visibility/position/font size/OCR substitution data를 가능한 한 보존한다.

## Ground Truth / correction

교정 image는 viewport 안에 auto-fit하지만 저장 ROI는 원본 pixel coordinate를 유지한다.

Candidate-first fields:

1. detail rectangle
2. close-X
3. magnifier
4. item-name ROI
5. correct item/text

기본 선택 UX는 image 위 candidate box 직접 클릭이다.

- 정답 candidate 없음 → manual rectangle
- 실제 object 없음 → `없음`
- saved Case → 다시 열기 / same Case ID로 재교정

Reviewed Ground Truth는 자동 retention 대상이 아니다.

## Runtime stability / retention

- same-cycle exact-identical OCR bitmap만 reuse
- cross-frame OCR cache 없음
- title continuity signature는 Item identity proof가 아님
- automatic unreviewed diagnostics: 30일 / 300건 / 512 MiB
- recent 2시간 protection
- Scanner/startup logs bounded rotation

## 검증 현황

v1.6 UI/기능 중간 gate CI `32700507526`:

- Desktop build SUCCESS
- 296 / 296 tests SUCCESS
- Windows x64 publish SUCCESS
- Product UI / Scanner / Mini Scanner smoke SUCCESS
- Main Map / Factory / MiniMap smoke SUCCESS
- graceful shutdown SUCCESS
- artifact upload SUCCESS

이 성공 이후 1.6.0 version identity, FIRST_RUN, stable release ZIP CI gate, final docs를 추가했으므로 최신 HEAD에서 최종 CI를 다시 통과해야 한다.

## 현재 남은 작업

1. 최신 v1.6.0 HEAD final CI
2. PR #174 merge
3. main push CI
4. exact release source/tag 고정
5. public stable/latest v1.6.0 release
6. `Junhyun-Helper.zip` public redownload/hash/layout verification
7. public-downloaded EXE ProductVersion / Product UI / Map / Scanner smoke
8. final release status 기록

공개 검증 후에는 새 기능 확장보다 live reviewed Ground Truth maintenance가 다시 기본 개발 단계가 된다.
