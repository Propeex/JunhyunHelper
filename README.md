# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **PRODUCT COMPLETE / PUBLIC STABLE / MAINTENANCE MODE**입니다.

2026-08-26 제품 사용자는 기존 제품 요구사항과 Scanner 실사용 검증을 기준으로 준현 헬퍼가 완성 상태에 도달했다고 최종 확정했습니다. 마지막 집중 개발 영역이었던 Scanner도 v1.7.6에서 실사용 성능 문제가 해결되어 기능 개발 단계에서 유지보수 단계로 전환했습니다.

새로운 실제 회귀, Tarkov/Windows/.NET 호환성 변화, 또는 사용자가 명시적으로 결정한 새 제품 요구사항이 없는 한 선제적 기능 추가나 구조 변경을 시작하지 않습니다.

상세 결정:

- `docs/DECISION_PRODUCT_COMPLETE_2026-08-26.md`
- `docs/CURRENT_SCANNER_WORK.md`
- `docs/STATE.md`

## 릴리즈 상태

현재 공개 stable/latest는 **v1.7.8**입니다.

```text
Desktop target version: 1.7.8
Content schema: v7
Readable content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v6
Scanner catalog cache: v1~v3 readable, v3 written
automated test suite: 380 tests
stable user ZIP name: Junhyun-Helper.zip
stable extracted folder: 준현 헬퍼/
```

공개 검증 기준선:

```text
exact release source/tag target: 3ba9d99c43ad143dbc8329e7d29b1d01da335b06
release CI run: 32888653630
release workflow run: 32888935292
release id: 376650517
stable asset: Junhyun-Helper.zip
stable bytes: 80,469,671
stable SHA-256: 3716d2d3c6d3c9ce2f87c759aac74f6b56b483a09016339c0d8bb6d3bc67e730
public stable/latest: VERIFIED
published: 2026-08-26 KST
```

v1.7.8 공식 작업 기록:

- `docs/RELEASE_NOTES_V1.7.8.md`
- `docs/RELEASE_1.7.8.md`
- `docs/DECISION_V1.7.8_RAID_HEADER_LOCK_2026-08-26.md`
- `docs/.release-v1.7.8-status.json`
- `docs/DECISION_SCANNER_STORAGE_AND_HOTKEYS_2026-08-26.md`
- `docs/DECISION_PRODUCT_COMPLETE_2026-08-26.md`

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

## Scanner 성능 기준선

v1.7.6은 일부 실제 데스크톱에서 Scanner 인식이 5~13초까지 지연되던 문제의 root cause를 실측 자료로 확인하고 해결한 stable release입니다.

문제는 Windows OCR 자체가 아니라 동일 current-frame visual proof가 여러 구조 후보에서 반복 계산되며 latency가 증폭되는 것이었습니다. v1.7.6은 동일 Scanner cycle의 exact current-pixel visual evidence를 안전하게 재사용하고 optional Tarkov font source 탐색의 hot-path 반복도 억제합니다.

문제 PC 재검증:

```text
Display Test — 하프 마스크
10,840.877 ms → 70.603 ms
약 99.35% 감소

Display Test — USB 보안 플래시 드라이브
12,686.278 ms → 1,354.775 ms
약 89.32% 감소
```

실제 Tarkov 성공 12건의 `ReadingTitle → ShowingItem`:

```text
minimum: 38.07 ms
median:  63.92 ms
maximum: 1.05 s
mean:    211.47 ms
```

실사용 평가에서도 충분한 반응성을 확인했으며 Scanner 성능 알고리즘은 완료 상태로 취급합니다. 새로운 runtime evidence 없이 threshold, candidate cap, OCR variant 또는 visual acceptance를 성능 목적으로 변경하지 않습니다.

v1.7.7은 이 인식 알고리즘을 변경하지 않고, 실사용에서 확인된 Scanner 교정 데이터 폭증·반복 로그·단축키 설정 불일치만 수정한 유지보수 PATCH입니다.

v1.7.8은 사용자 reviewed 레이드 Case에서 확인된 inspect-header 수평 소유권 오류를 수정합니다. 주변 인벤토리 수평선이 상세창 header와 이어져도 강한 detail proposal의 실제 왼쪽 경계를 기준으로 red close-X, magnifier, neutral header, dark title field와 text evidence를 다시 검증하며 기존 `HEADER_FRAME_LOCKED >= 0.68` 기준은 완화하지 않습니다.

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
- game memory read / DLL injection / packet interception / process hook 없음
- 제품 기본값에 automatic global r/0/한글 forced substitution table 없음
- cross-frame OCR/visual identity cache 없음

## Scanner 사용 흐름

일반 Scanner 화면 상단:

- `스캐너 ON/OFF`
- `설정`
- `고급`
- `현재 결과 교정`

하단:

- 왼쪽 `아이템 검색`
- 오른쪽 최근 Scanner 인식 로그

기본 전역 단축키:

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

Scanner와 configurable Map 단축키는 **일반 키 하나 + 선택적 Ctrl/Alt/Shift 조합**을 공통 계약으로 사용합니다. 따라서 `F10`, `K`, `Ctrl+K`, `Alt+F10`, `Ctrl+Shift+K` 같은 형태를 사용할 수 있습니다. Windows 키 조합은 지원하지 않습니다.

Map의 bare `NumPad0~5`는 기존 직접 층 선택에 예약되며, `Ctrl+NumPad1`처럼 modifier가 붙은 NumPad 조합은 일반 Map 단축키로 사용할 수 있습니다.

`설정`에서는 전역 단축키와 Mini Scanner 정보 표시/순서를 관리합니다.

`현재 결과 교정`은 메모리에 보존된 최신 exact Scanner frame을 바로 교정 창으로 엽니다.

`고급`에서는 Display Test, 교정 데이터 관리, Scanner 성능 진단 자료 export를 다룹니다.

## Scanner 아이템 검색

검색은 현재 내려받은 local/memory full-item catalog를 사용하며 검색 순간 network request를 만들지 않습니다.

선택한 아이템에서 확인할 수 있는 핵심 정보:

- icon
- official item name
- Tarkov Wiki
- flea positive `avg24hPrice`
- 최고 non-flea trader RUB 가격 + 가능한 경우 trader name
- `NeededItems[itemId].RequiredTotal`

Inventory를 차감한 부족량은 Scanner의 필요 개수 의미가 아닙니다.

## Mini Scanner

항상 표시:

- 아이템 icon
- official item name

사용자가 표시 여부와 순서를 지정:

- 상인 판매가
- 플리 평균가
- 상인 가격/칸
- 플리 가격/칸
- 필요 개수

기존 설정은 schema migration을 통해 hotkey/visibility/position/font size/user OCR substitutions를 가능한 한 보존합니다.

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

교정 화면은 큰 원본 image를 viewport에 맞게 축소해 보여 주되 **저장 좌표는 항상 원본 pixel coordinate**를 사용합니다.

Candidate-first fields:

1. detail rectangle
2. close-X
3. magnifier
4. item-name ROI
5. correct item/text

후보 box는 이미지 위에서 직접 선택할 수 있습니다.

- 정답 candidate 없음 → manual rectangle
- 실제 semantic object 없음 → explicit `없음`

저장된 Case는 교정 데이터 관리에서 다시 열어 기존 Ground Truth와 candidate selection을 수정할 수 있습니다.

**v1.7.7부터 정상 연속 Scanner는 실패 프레임을 durable Case로 자동 저장하지 않습니다.** 최신 exact diagnostic frame은 현재 교정을 위해 메모리에만 유지하며, 사용자가 명시적으로 교정 저장한 Case만 장기 Ground Truth가 됩니다.

기본 저장 위치:

```text
%LocalAppData%\JunhyunHelper\scanner\diagnostics
```

이전 버전에서 생성된 legacy Case는 `retention=automatic_sample`과 `review_status=unreviewed`를 모두 증명할 수 있고 최근 쓰기 중이 아님을 확인한 경우에만 background cleanup합니다. 삭제 직전 상태를 다시 확인하며 reviewed/manual/corrupt/unknown Case는 자동 삭제하지 않습니다.

사용자-reviewed Ground Truth는 자동 retention 대상이 아닙니다.

## Scanner 로그 / 장시간 실행

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

v1.7.6은 같은 active latency cycle에서 동일한 title bitmap dimensions + exact current-pixel SHA-256 + OCR text 조합의 visual corroboration 결과만 재사용합니다. Cycle이 바뀌면 폐기하며 frame 간 identity cache로 사용하지 않습니다.

Continuous observation은 non-backlogging pacing을 사용합니다. 작업 시간이 target interval을 초과해도 missed tick을 몰아서 재생하지 않고 cooperative yield를 둡니다.

v1.7.7에서는 동일 실패를 사용자 activity feed에서 30초 동안 collapse해 필요한 기록의 가시성을 유지합니다. 지원 분석용 `scanner.log`는 기존의 작은 bounded rotation/retention을 유지하며 Ground Truth lifetime과 분리됩니다.

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

## 배포 형태

Windows x64 portable / .NET 10 self-contained single-file.

정식 user ZIP contract:

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/
```

ZIP과 압축 해제 폴더 이름에는 버전 번호를 넣지 않습니다. 버전은 EXE ProductVersion, Git tag, GitHub Release metadata에서 관리합니다.

별도 .NET Runtime 설치나 관리자 권한은 필요하지 않으며 현재 code signing은 하지 않습니다.

## 유지보수 원칙

- 사용자 의도 / 제품 요구사항 / 현재 구현을 구분
- 기존 프로토타입 동작을 공식 요구사항으로 추정하지 않음
- 중요한 결정과 상태는 GitHub 문서에 즉시 기록
- 실사용 defect/regression은 exact evidence를 확보하고 영향받은 계층만 수정
- Scanner는 실제 reviewed Ground Truth 기반으로 개선
- 기존 정상 Ground Truth의 `REGRESSION=0`을 우선
- 추가 evidence 없이 matcher/header threshold 또는 candidate cap 완화 금지
- 코드 미관만을 위한 위험한 대규모 refactor 금지
- Tarkov 데이터/UI 및 Windows/.NET 호환성 변화는 필요할 때 대응
- 새 기능은 사용자가 새로운 제품 요구사항으로 결정했을 때 시작

## 개발 문서

- `docs/STATE.md` — canonical 현재 상태
- `docs/CURRENT_STATE.md` — 짧은 상태 인덱스
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/ARCHITECTURE.md` — 전체 아키텍처
- `docs/DEVELOPER_REFERENCE.md` — 구현/참조 지도
- `docs/SCANNER.md` — Scanner canonical 전문 계약
- `docs/SCANNER_GROUND_TRUTH.md` — Ground Truth dataset 계약
- `docs/SCANNER_TEST_PLAN.md` — Scanner release/regression gate
- `docs/CURRENT_SCANNER_WORK.md` — Scanner 유지보수 기준
- `docs/DECISION_V1.7.8_RAID_HEADER_LOCK_2026-08-26.md` — v1.7.8 레이드 header ownership 및 교정 UI 결정
- `docs/DECISION_SCANNER_STORAGE_AND_HOTKEYS_2026-08-26.md` — v1.7.7 저장/단축키 결정
- `docs/DECISION_PRODUCT_COMPLETE_2026-08-26.md` — 제품 완성 및 유지보수 전환 결정
