# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-24

상태: **`v1.5.0 PUBLIC RELEASE / VERIFIED`**

## 현재 공개 기준선

```text
public stable/latest: v1.5.0
exact release source/tag: 6de738959740d12e6ccb81b65e50006e463eb699
asset: Junhyun-Helper-v1.5.0-win-x64.zip
bytes: 80,422,292
SHA-256: 6ad657653123ff35d8b6fe3d7f9877858992e9327697077492cf29f7c900e5e9
ProductVersion: 1.5.0+6de738959740d12e6ccb81b65e50006e463eb699
automated tests: 296 passed / 0 failed / 0 skipped
release run: 32691423654 — SUCCESS
independent public verifier: 32691641614 — SUCCESS
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download: VERIFIED
SHA256SUMS: VERIFIED
package layout: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

공식 공개 검증:

- `docs/RELEASE_1.5.0.md`
- `docs/.release-v1.5.0-status.json`
- `docs/RELEASE_NOTES_V1.5.0.md`

## Schema / compatibility

```text
Desktop Version: 1.5.0
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v5
Scanner catalog cache: v1/v2 readable, v2 written
Scanner Ground Truth: local diagnostics persistence
```

v1.5.0은 기존 사용자 데이터 저장 위치를 변경하지 않는다. Program Update는 `%LocalAppData%/JunhyunHelper`의 user.db, content/image cache, Map/Ammo/Scanner 설정, Scanner logs/diagnostics를 교체하지 않는다.

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
| Program Update | 구현 완료 / v1.5.0 public package verified |
| Scanner + Mini Scanner | **v1.5.0 public verified / Ground Truth 기반 개선 지속** |

## v1.5.0 핵심 변경

- Scanner 최고 상점가/상인, flea avg24hPrice, slots, price-per-slot, RequiredTotal mapped-data 경로 보강
- 일반 Game Data update와 Scanner item/market catalog refresh 통합
- Quest `확인 필요` 최신 live-data 감사 및 GameMode-aware fail-closed task-pool compatibility
- 사용자 OCR 문자열 치환 설정 추가; raw OCR forensic evidence 보존
- candidate 기반 Ground Truth 교정 + manual rectangle / `없음` fallback
- Scanner stage latency telemetry
- 같은 scan-cycle 안의 exact-identical OCR bitmap만 재사용하는 보수적 최적화
- continuous trusted-result 안정화
- reviewed Ground Truth 보호 + automatic diagnostic/log bounded retention
- Scanner normal/settings/advanced UI 분리
- Mini Scanner 우클릭 `현재 결과 교정`
- 전체 UI consistency audit 및 MainWindow 최소 폭 1180 보정

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
→ Item ID or fail closed
→ local mapped presentation
→ Mini Scanner
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
- 자동 global r/0/한글 강제 substitution table 없음

## OCR substitution

Scanner settings schema v5부터 사용자 소유 exact 문자열 치환을 지원한다.

```text
raw OCR
→ user substitutions (single pass)
→ catalog sanitation / normalization
→ matching
```

기본 규칙은 비어 있고, raw OCR은 별도로 보존한다. 재귀/연쇄 치환은 수행하지 않는다.

## Ground Truth / correction

교정의 기본 경로는 detector candidate 선택이다.

1. detail rectangle
2. close-X
3. magnifier
4. item-name ROI
5. 정답 item/text
6. 저장

후보가 없으면 manual rectangle을 사용하며 semantic object가 실제로 없으면 `없음`으로 기록할 수 있다. Reviewed Ground Truth는 자동 retention 대상이 아니다.

## Runtime stability / retention

- 같은 검증 대상의 title-ink identity가 유지되면 일시적 pixel/OCR 흔들림으로 trusted result를 즉시 깜빡이지 않음
- 명확한 다른 title/identity evidence에서는 stale result 즉시 해제
- automatic unreviewed diagnostics: 30일 / 300건 / 512 MiB 상한
- 최근 2시간 automatic case 보호
- Scanner/startup logs bounded rotation

## 검증 상태

v1.5.0에서 확인 완료:

- final Release build
- 296 tests / 0 failed / 0 skipped
- Windows x64 self-contained single-file publish
- exact ProductVersion / FIRST_RUN identity
- Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke
- graceful shutdown
- draft asset re-download/hash/identity/EXE smoke
- public/latest 전환
- exact tag source verification
- 독립 anonymous public ZIP + SHA256SUMS 재다운로드
- public hash/size/layout/ProductVersion/FIRST_RUN 검증
- public-downloaded EXE smoke + graceful shutdown
- one-shot release/public verifier workflow cleanup

## 현재 개발 방향

새 기능을 무분별하게 추가하는 단계가 아니다. v1.5.0을 공식 제품 기준선으로 유지하면서 실제 Tarkov 사용의 reviewed Ground Truth를 축적하고, 실패가 생기면 capture → proposal → semantic header → ROI → raw OCR → user substitution → catalog matching/visual recovery → mapped presentation → overlay 단계 중 원인을 특정해 필요한 단계만 수정한다.

추가 Ground Truth 없이 generic confidence/margin/header threshold나 candidate cap을 완화하지 않는다.
