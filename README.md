# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 릴리즈 상태

현재 공개 stable/latest는 **v1.5.0**이며, 저장소의 현재 작업은 **v1.6.0 release candidate**입니다.

v1.6.0은 최종 CI와 공개 릴리즈 검증이 끝나기 전까지 public stable로 간주하지 않습니다.

현재 v1.6.0 release candidate 기준:

```text
Desktop target version: 1.6.0
Content schema: v7
Readable content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1/v2 readable, v2 written
automated test suite: 296 tests
stable user ZIP name: 준현 헬퍼.zip
stable extracted folder: 준현 헬퍼/
```

마지막 공개 검증 기준선 v1.5.0:

```text
exact release source/tag: 6de738959740d12e6ccb81b65e50006e463eb699
asset: Junhyun-Helper-v1.5.0-win-x64.zip
SHA-256: 6ad657653123ff35d8b6fe3d7f9877858992e9327697077492cf29f7c900e5e9
296 passed / 0 failed / 0 skipped
public/latest: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

v1.6.0 공식 작업 기록:

- `docs/DECISION_V1.6.0_SCANNER_PRODUCT_WORKFLOW_2026-08-24.md`
- `docs/STATUS_V1.6.0_SCANNER_PRODUCT_WORKFLOW_2026-08-24.md`
- `docs/RELEASE_NOTES_V1.6.0.md`

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
- Scanner Ground Truth 교정 / diagnostics / regression dataset
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## v1.6.0 주요 변경

v1.6.0은 Scanner의 identity threshold를 낮추는 버전이 아니라 **실사용·검색·Mini Scanner·교정 작업 흐름을 정리하는 MINOR 릴리즈**입니다.

- Scanner 일반 화면을 `스캐너 ON/OFF / 설정 / 고급` 중심으로 단순화
- 하단을 `아이템 검색 / Scanner 로그` 2분할로 구성
- 현재 local full-item catalog 기반 아이템 검색 추가
- 검색 결과 icon/name, 선택 후 Wiki/flea/best trader/current needed 표시
- Mini Scanner icon/name fixed identity header
- Mini Scanner 다섯 정보의 표시 여부와 순서 저장
- 가능한 경우 최고가 상인 이름 + 판매가 표시
- Scanner settings schema v6 migration
- 큰 교정 이미지를 화면 안에 자동 축소하되 원본 pixel 좌표 보존
- 상세창/X/돋보기/item-name ROI candidate를 이미지 위에서 직접 클릭 선택
- manual rectangle / explicit `없음` fallback 유지
- 저장된 Scanner Case 재열기·재교정
- 배포 ZIP/폴더 이름을 버전과 분리

상세: `docs/RELEASE_NOTES_V1.6.0.md`

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
→ optional visual corroboration/recovery
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
→ optional correction / Ground Truth
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
- cross-frame OCR cache 없음

## Scanner 사용 흐름 — v1.6.0

일반 Scanner 화면 상단:

- `스캐너 ON/OFF`
- `설정`
- `고급`

하단:

- 왼쪽 `아이템 검색`
- 오른쪽 최근 Scanner 인식 로그

기존 one-shot 기능은 삭제하지 않았습니다. 기본 전역 단축키:

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

`설정`에서는 전역 단축키와 Mini Scanner 정보 표시/순서를 관리합니다.

`고급`에서는 Display Test와 현재 결과 교정, 교정 데이터 관리 같은 실사용 진단 작업을 다룹니다.

## Scanner 아이템 검색

검색은 현재 내려받은 local/memory full-item catalog를 사용합니다.

검색 순간 network request를 만들지 않습니다.

선택한 아이템에서 확인할 수 있는 핵심 정보:

- icon
- official item name
- Tarkov Wiki
- flea positive `avg24hPrice`
- 최고 non-flea trader RUB 가격 + 가능한 경우 trader name
- `NeededItems[itemId].RequiredTotal`

Inventory를 차감한 부족량은 Scanner의 필요 개수 의미가 아닙니다.

## Mini Scanner — schema v6

항상 표시:

- 아이템 icon
- official item name

사용자가 표시 여부와 순서를 지정:

- 상인 판매가
- 플리 평균가
- 상인 가격/칸
- 플리 가격/칸
- 필요 개수

기존 v5 이하 설정은 자동 migration되며 hotkey/visibility/position/font size/user OCR substitutions를 가능한 한 보존합니다.

## OCR 사용자 치환

사용자 소유 exact OCR substitution engine은 유지됩니다.

```text
raw OCR
→ enabled user substitutions (single ordered pass)
→ catalog sanitation / normalization
→ matching
```

- 기본 규칙 empty
- raw OCR forensic evidence 별도 보존
- recursive/chained reprocessing 없음
- user rule은 product-wide automatic substitution table이 아님

v1.6.0의 일반 설정 UI는 hotkey와 Mini Scanner 표시 흐름을 우선하지만 기존 사용자 substitution 데이터는 schema migration에서 보존합니다.

## Scanner 표시 데이터

Item ID 확정 후 아래 데이터는 OCR이 아니라 local trusted data에서 조회/계산합니다.

- 최고 non-flea trader 판매가
- 최고가 trader name
- flea positive `avg24hPrice`
- positive `width × height` slots
- trader price/slot
- flea price/slot
- required total = `NeededItems[itemId].RequiredTotal`

Market/dimension 일부가 없으면 affected field만 비우고 healthy Item identity를 폐기하지 않습니다.

## Ground Truth / 교정

v1.6.0 교정 화면은 큰 원본 image를 viewport에 맞게 축소해 보여 주되 **저장 좌표는 항상 원본 pixel coordinate**를 사용합니다.

Candidate-first fields:

1. detail rectangle
2. close-X
3. magnifier
4. item-name ROI
5. correct item/text

후보 box는 이미지 위에서 직접 클릭합니다.

- 정답 candidate 없음 → manual rectangle
- 실제 semantic object 없음 → explicit `없음`

저장된 Case는 교정 데이터 관리에서 다시 열어 기존 Ground Truth와 candidate selection을 수정할 수 있습니다.

사용자-reviewed Case만 Ground Truth로 취급합니다. 자동 diagnostic Case는 정답이 아닙니다.

기본 저장 위치:

```text
%LocalAppData%\JunhyunHelper\scanner\diagnostics
```

Reviewed Ground Truth는 자동 retention 대상이 아닙니다.

## Scanner 성능 / 장시간 실행

Stage latency telemetry:

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
→ exact Windows release asset + checksum
→ checksum/package 검증
→ program-owned files transaction 교체
→ 새 버전 재시작
```

사용자 데이터는 `%LocalAppData%/JunhyunHelper`에 분리되어 있으며 프로그램 업데이트가 덮어쓰지 않습니다.

## 배포 형태 — v1.6.0부터

Windows x64 portable / .NET 10 self-contained single-file.

정식 user ZIP contract:

```text
준현 헬퍼.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/
```

ZIP과 압축 해제 폴더 이름에는 버전 번호를 넣지 않습니다.
버전은 EXE ProductVersion, Git tag, GitHub Release metadata에서 관리합니다.

별도 .NET Runtime 설치나 관리자 권한은 필요하지 않으며 현재 code signing은 하지 않습니다.

## 개발 원칙

- 사용자 의도 / 제품 요구사항 / 현재 구현을 구분
- 기존 프로토타입 동작을 공식 요구사항으로 추정하지 않음
- 중요한 결정과 상태는 GitHub 문서에 즉시 기록
- Scanner는 실제 reviewed Ground Truth 기반으로 개선
- 기존 정상 Ground Truth의 `REGRESSION=0`을 우선
- 추가 evidence 없이 matcher/header threshold 또는 candidate cap 완화 금지
- 국소 수정 반복보다 전체 시스템 일관성을 우선하되 단순 변경에 불필요한 전면 리팩터링은 하지 않음

## 개발 문서

- `docs/STATE.md` — canonical 현재 상태
- `docs/CURRENT_STATE.md` — 짧은 상태 인덱스
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/ARCHITECTURE.md` — 전체 아키텍처
- `docs/DEVELOPER_REFERENCE.md` — 구현/참조 지도
- `docs/SCANNER.md` — Scanner canonical 전문 계약
- `docs/SCANNER_GROUND_TRUTH.md` — Ground Truth dataset 계약
- `docs/SCANNER_TEST_PLAN.md` — Scanner release/regression gate
- `docs/CURRENT_SCANNER_WORK.md` — 현재 Scanner 작업 단계
