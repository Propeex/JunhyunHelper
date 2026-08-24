# Decision — v1.5.0 Product Finishing Pass

기준일: 2026-08-24
상태: **APPROVED / IMPLEMENTED / PUBLIC RELEASE VERIFIED**

## 목적

v1.5.0은 Scanner 정확도 연구만 계속하는 릴리즈가 아니라, 현재 준현 헬퍼를 실제 Tarkov 플레이에서 장시간 안정적으로 사용할 수 있는 제품으로 마감하는 MINOR 릴리즈다.

기존 기능을 축소하지 않는다. 일반 사용자 화면은 단순화하고 개발·진단 기능은 고급 영역으로 이동한다. 현재 코드가 존재한다는 이유만으로 그 동작을 올바른 설계라고 간주하지 않으며, 제품 요구사항과 검증된 evidence를 우선한다.

이 결정은 구현과 공개 검증까지 완료되었다.

공개 기준선:

```text
v1.5.0
exact source/tag: 6de738959740d12e6ccb81b65e50006e463eb699
release run: 32691423654 — SUCCESS
public verifier: 32691641614 — SUCCESS
```

## 승인 범위와 최종 결정

### 1. Scanner market/mapped presentation 신뢰성

Item ID가 올바르게 확정됐는데 최고 상점가/칸당 가격 등이 비는 문제는 기능 추가가 아니라 제품 버그로 취급한다.

Item ID 확정 후 local trusted data에서 다음을 일관되게 조회/계산한다.

- 최고 non-flea trader RUB-equivalent sell price
- 최고가 상인명
- flea positive `avg24hPrice`
- positive `width × height` slots
- trader/flea price-per-slot
- `NeededItems[itemId].RequiredTotal`

Market/dimension 일부 누락은 해당 presentation field만 비우며 healthy Item ID를 identity failure로 승격하지 않는다.

### 2. Quest `확인 필요` 재감사

`확인 필요` 증가를 UI 노이즈로 덮지 않는다.

Latest Tarkov data에서 unresolved/unsupported availability를 원인별로 감사하고, 안전하게 해석 가능한 조건만 evaluator에 구현한다. 실제로 알 수 없는 조건은 fail closed한다.

v1.5.0 live audit 대상:

- regular
- pve
- pvp-season

Task-pool compatibility는 GameMode와 audited requirement shape가 일치할 때만 synthetic value를 허용한다.

Reference: `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md`.

### 3. Game Data update와 Scanner catalog refresh 통합

사용자가 별도 Scanner catalog 갱신 절차를 이해하거나 반복할 필요가 없게 한다.

상단 Game Data update가 일반 content와 Scanner item/market catalog refresh를 함께 orchestration한다.

Scanner refresh만 실패하면 건강한 일반 content를 rollback하지 않고 기존 healthy Scanner cache를 유지한다. Scanner 전용 `아이템 목록 최신화`는 고급/복구 surface에 남긴다.

### 4. 사용자 OCR 문자 치환

Scanner 설정에 사용자 정의 exact 문자열 치환 규칙을 제공한다.

계약:

- 기본 규칙은 비어 있음
- raw OCR은 forensic evidence로 항상 별도 보존
- user substitution은 raw OCR 이후 catalog sanitation/matching 이전에 한 번만 적용
- ordered single-pass
- recursive/cyclic reprocessing 금지
- 규칙 추가/삭제/ON·OFF/초기화 지원
- raw / user-substituted / normalized / matched 결과 구분
- 사용자 규칙은 product-wide automatic global substitution table이 아님

### 5. Candidate 기반 Ground Truth 교정

교정 기본 UX를 수동 rectangle drawing 중심에서 detector evidence 선택 중심으로 전환한다.

기본 순서:

1. detail rectangle candidate
2. red close-X candidate
3. magnifier candidate
4. item-name ROI candidate
5. correct item/text
6. save

후보에 정답이 없으면 manual rectangle 지정 fallback을 유지한다. Semantic object가 실제로 탐지되지 않았음을 기록하기 위해 `없음` 선택도 보존한다.

Candidate ID/rank/score/geometry와 정답을 함께 저장한다.

### 6. Scanner 일반 UI / 설정 / 고급·진단 분리

일상 Scanner 화면은 핵심 흐름에 집중한다.

- Scanner ON/OFF
- 1회 스캔
- 현재 결과 교정
- runtime status
- recent recognition history

설정:

- global hotkeys
- OCR substitutions
- Mini Scanner display options

고급 / 진단:

- Display Test
- recognition image
- regression
- Ground Truth export/manage
- Scanner catalog recovery/forced refresh
- log clear
- diagnostic storage information

기능은 삭제하지 않고 surface complexity를 낮춘다.

### 7. 빠른 교정 접근성

Mini Scanner에서 잘못 읽힌 직후 `현재 결과 교정`에 접근할 수 있게 한다.

우클릭 context menu가 latest recognition debug snapshot을 correction flow에 전달한다.

### 8. Continuous Scanner 결과 안정화

같은 상세창을 보고 있는 동안 harmless dark-background/GPU pixel variation이나 일시적인 OCR 흔들림으로 trusted result가 불필요하게 깜빡이지 않게 한다.

Title-ink shape identity는 continuity evidence일 뿐 Item identity proof가 아니다.

다른 title/geometry/identity evidence가 나타나면 stale trusted result를 즉시 폐기한다.

### 9. Scanner latency telemetry 및 정확도 보존 최적화

Threshold를 낮추는 방식이 아니라 stage latency를 먼저 계측한다.

측정 대상:

- capture
- rectangle proposal
- semantic header validation
- OCR normal/deep
- visual recovery
- catalog matching/recovery
- presentation
- end-to-end

첫 최적화는 같은 active scan cycle 안의 exact-identical OCR bitmap에 한해 WinRT OCR output을 재사용한다. Frame 간 OCR cache는 허용하지 않는다.

### 10. 장시간 실행 / diagnostics retention

사용자-reviewed Ground Truth는 자동 삭제하지 않는다.

자동 삭제 가능 대상:

```text
retention == automatic_sample
AND review_status == unreviewed
```

기본 bound:

- 30 days
- 300 automatic cases
- 512 MiB automatic diagnostic data
- recent 2-hour safety window

Corrupt/unknown metadata는 fail closed하여 보존한다. Scanner/startup logs도 bounded rotation한다.

### 11. 전체 UI consistency audit

Main / Quest / Hideout / Items / Ammo / Map / Scanner / settings/dialog을 다시 점검한다.

새 기능을 무분별하게 추가하지 않는다. 일상 사용자가 개발/진단 개념을 몰라도 핵심 기능을 사용할 수 있게 한다.

실제 구조적 문제로 확인된 MainWindow minimum width는 900에서 1180으로 교정한다. 검증된 Map/MiniMap subsystem은 불필요하게 재설계하지 않는다.

### 12. Release / verification

v1.5.0 공개 조건:

- Release build
- full automated tests
- Windows x64 publish
- package identity audit
- Product UI / Map / Scanner smoke
- graceful shutdown
- exact source tag
- draft asset redownload verification
- stable/latest publication
- fresh runner anonymous public ZIP/SHA256SUMS redownload
- public hash/size/layout/ProductVersion/FIRST_RUN verification
- public-downloaded EXE smoke
- durable machine-readable release status
- one-shot workflow cleanup

모든 조건을 통과했다.

## 변경하지 않는 Scanner 핵심 계약

v1.5.0 Product Finishing Pass는 다음 안전 기준을 완화하지 않는다.

- false positive보다 miss 선호
- rectangle geometry는 proposal이며 identity proof가 아님
- semantic anchors가 detail identity를 확립
- `HEADER_FRAME_LOCKED >= 0.68`
- magnifier + red close-X 필수
- structural floor `0.34`
- continuous max 8 candidates
- one-shot max 12 candidates
- current official Korean Tarkov item catalog가 identity authority
- production OCR field는 `item_name` 하나
- price/flea/slots/needed는 Item ID 이후 mapped data
- scan-time network 금지
- game memory read 금지
- DLL injection 금지
- packet interception 금지
- product default automatic global r/0/Korean forced substitution 금지

Threshold/candidate caps는 새로운 reviewed Ground Truth evidence 없이 변경하지 않는다.

## 버전 결정

사용자에게 노출되는 새 설정/교정 UX 및 제품 동작이 포함되므로 SemVer MINOR로 분류한다.

최종 릴리즈: **v1.5.0**

## 완료 증거

- final PR #172 release-candidate CI: `32688080850` — SUCCESS
- 296 tests / 0 failed / 0 skipped
- exact public source/tag: `6de738959740d12e6ccb81b65e50006e463eb699`
- release workflow: `32691423654` — SUCCESS
- independent public verifier: `32691641614` — SUCCESS
- durable release record: `docs/.release-v1.5.0-status.json`
- human-readable verification: `docs/RELEASE_1.5.0.md`

이 결정의 구현 단계는 종료되었다. 이후 변경은 v1.5.0 public baseline을 기준으로 별도 요구사항/결정으로 관리한다.
