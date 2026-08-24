# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 릴리즈 상태

현재 public stable / latest는 **v1.5.0**입니다.

```text
version: v1.5.0 PUBLIC RELEASE / VERIFIED
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
public re-download / checksum / package layout: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

공식 검증 기록:

- `docs/RELEASE_1.5.0.md`
- `docs/.release-v1.5.0-status.json`
- `docs/RELEASE_NOTES_V1.5.0.md`

현재 schema:

```text
Content schema: v7
Readable content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v5
Scanner catalog cache: v1/v2 readable, v2 written
```

## 주요 기능

- GameMode별 Profile
- Quest availability / prerequisite / special trader / profile-variable
- Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / consumption ledger
- Items / cross-navigation
- Ammo / favorites
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Scanner + Mini Scanner
- Scanner Ground Truth 교정 / diagnostics export / regression
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## v1.5.0 주요 변경

v1.5.0은 Scanner 연구 기능만 늘리는 버전이 아니라 현재 프로그램을 장시간 실제 플레이에서 쓰기 위한 **Product Finishing Pass**입니다.

- Scanner 최고 상점가/상인, flea 평균가, slots, price-per-slot, 필요한 수량 mapped-data 경로 보강
- 일반 Game Data update와 Scanner item/market catalog 갱신 통합
- 최신 Quest task-pool live data 감사 및 GameMode-aware fail-closed compatibility
- 사용자 OCR exact 문자열 치환 설정
- detector candidate 기반 Ground Truth 교정 + manual rectangle / `없음` fallback
- Scanner stage latency telemetry
- 같은 scan-cycle의 exact-identical OCR bitmap만 재사용하는 정확도 보존 최적화
- continuous trusted-result 안정화
- reviewed Ground Truth 보호 + automatic diagnostics/log bounded retention
- Scanner 일반 화면 / 설정 / 고급·진단 UI 분리
- Mini Scanner 우클릭 `현재 결과 교정`
- 전체 UI consistency audit 및 MainWindow 최소 폭 보정

상세: `docs/RELEASE_NOTES_V1.5.0.md`

## Scanner

Production Scanner는 게임 화면 픽셀만 사용합니다.

```text
Tarkov window pixels
→ capture
→ detail rectangle proposals
→ red close-X + magnifier + neutral header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ Windows ko-KR OCR
→ optional user OCR substitution
→ current-catalog sanitation / normalization
→ conservative official-catalog matching / bounded recovery
→ Item ID or fail closed
→ local mapped presentation
→ Mini Scanner
```

### 핵심 안전 계약

- false positive보다 miss 선호
- rectangle geometry는 proposal이며 identity proof가 아님
- `HEADER_FRAME_LOCKED >= 0.68`
- magnifier + red close-X 필수
- structural floor `0.34`
- continuous max 8 candidates
- one-shot max 12 candidates
- current official Korean Tarkov item catalog가 identity authority
- production OCR field는 item-name 하나
- price / slots / needed는 Item ID 이후 local mapped data
- scan-time network 없음
- game memory read / DLL injection / packet interception 없음
- 제품 기본값에 automatic global r/0/한글 forced substitution table 없음

## Scanner 사용 흐름

일반 Scanner 화면은 실제 플레이 동선에 필요한 기능을 우선합니다.

- Scanner ON/OFF
- `1회 스캔`
- `현재 결과 교정`
- runtime status
- 최근 인식 기록

`설정`에는 전역 단축키, OCR 치환, Mini Scanner 표시 설정이 있습니다.

`고급 / 진단`에는 Display Test, 인식 이미지, regression, Ground Truth export/manage, Scanner catalog 강제 최신화, 로그 삭제를 둡니다.

Mini Scanner에서는 우클릭 → `현재 결과 교정`으로 방금 본 결과를 즉시 교정할 수 있습니다.

기본 전역 단축키:

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

## OCR 문자 치환

Scanner settings schema v5부터 사용자 소유 exact 문자열 치환을 지원합니다.

```text
raw OCR
→ enabled user substitutions (single pass)
→ catalog sanitation / normalization
→ matching
```

- 기본 규칙은 비어 있음
- 규칙 추가 / 삭제 / ON·OFF / 초기화
- raw OCR forensic evidence 별도 보존
- 재귀/연쇄 치환 없음
- 사용자가 만든 규칙은 product-wide 자동 치환표가 아님

## Scanner 표시 데이터

Item ID 확정 후 아래는 OCR이 아니라 local trusted data에서 조회/계산합니다.

- 최고 상점가 = flea 제외 유효 판매처의 RUB 환산 가격 최댓값
- 최고가 상인명
- 플리마켓 평균가 = positive `avg24hPrice`
- slots = positive `width × height`
- 상인 가격/슬롯
- flea 가격/슬롯
- 필요한 개수 = `NeededItems[itemId].RequiredTotal`

Inventory를 차감한 부족량은 Scanner의 `필요 개수` 의미가 아닙니다. Market/dimension 일부가 없으면 해당 표시 필드만 비우고 건강한 Item ID를 폐기하지 않습니다.

## Ground Truth / 교정

교정은 detector candidate 선택이 기본입니다.

1. detail rectangle
2. close-X
3. magnifier
4. item-name ROI
5. 정답 item/text
6. 저장

정답 후보가 없으면 manual rectangle 지정이 가능하며, detector가 semantic object를 만들지 못한 경우 `없음`을 기록할 수 있습니다.

사용자-reviewed Case만 Ground Truth로 취급합니다. 자동 diagnostic Case는 정답이 아닙니다.

기본 저장 위치:

```text
%LocalAppData%\JunhyunHelper\scanner\diagnostics
```

Reviewed Ground Truth는 자동 retention 대상이 아닙니다.

## Scanner 성능 / 장시간 실행

v1.5.0은 threshold 완화가 아니라 stage latency를 계측합니다.

```text
capture
rectangle proposal
semantic header
OCR normal/deep
visual recovery
catalog matching
presentation
end-to-end
```

같은 active scan cycle에서 픽셀 단위로 완전히 동일한 OCR bitmap만 재사용합니다. Frame 간 OCR cache는 사용하지 않습니다.

Automatic unreviewed diagnostic samples는 30일 / 300건 / 512 MiB 상한과 최근 2시간 보호창으로 관리합니다. Scanner/startup logs도 bounded rotation합니다.

## Quest `확인 필요`

`확인 필요`를 UI에서 억지로 숨기지 않습니다. 최신 source에서 안전하게 판정할 수 있는 조건만 evaluator에 반영하고, 실제로 알 수 없는 조건은 fail closed합니다.

2026-08-24 live audit는 `regular`, `pve`, `pvp-season`을 대상으로 수행했습니다.

상세: `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md`

## Program Update

```text
latest public stable 확인
→ strictly newer면 사용자 동의
→ exact Windows ZIP + SHA256SUMS
→ checksum/package 검증
→ program-owned files transaction 교체
→ 새 버전 재시작
```

사용자 데이터는 `%LocalAppData%/JunhyunHelper`에 분리되어 있으며 프로그램 업데이트가 덮어쓰지 않습니다.

## 배포 형태

Windows x64 portable / .NET 10 self-contained single-file.

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

별도 .NET Runtime 설치나 관리자 권한은 필요하지 않으며 현재 code signing은 하지 않습니다.

## 개발 원칙

- 사용자 의도 / 제품 요구사항 / 현재 구현을 구분
- 기존 프로토타입 동작을 공식 요구사항으로 추정하지 않음
- 중요한 결정과 상태는 GitHub 문서에 즉시 기록
- Scanner는 실제 reviewed Ground Truth 기반으로 개선
- 기존 정상 Ground Truth의 `REGRESSION=0`을 우선
- 추가 evidence 없이 generic matcher/header threshold 또는 candidate cap 완화 금지
- 국소 수정 반복보다 전체 시스템 일관성을 우선하되 단순 변경에 불필요한 전면 리팩터링은 하지 않음

## 개발 문서

- `docs/STATE.md` — canonical 현재 상태
- `docs/CURRENT_STATE.md` — 짧은 상태 인덱스
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/ARCHITECTURE.md` — 전체 아키텍처
- `docs/DEVELOPER_REFERENCE.md` — 구현/참조 지도
- `docs/DECISIONS.md` — 장기 결정 인덱스
- `docs/SCANNER.md` — Scanner 제품/기술 기준선
- `docs/SCANNER_GROUND_TRUTH.md` — Ground Truth 계약
- `docs/SCANNER_TEST_PLAN.md` — Scanner 검증 gate
- `docs/DECISION_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md` — v1.5.0 승인 범위
- `docs/STATUS_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md` — v1.5.0 최종 상태
- `docs/RELEASE_1.5.0.md` — v1.5.0 공개 검증
- `docs/RELEASE_NOTES_V1.5.0.md` — v1.5.0 사용자 변경점
