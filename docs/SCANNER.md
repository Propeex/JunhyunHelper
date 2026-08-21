# Scanner — 제품/기술 계약

기준일: 2026-08-21

상태: **`v1.1.1 IMPLEMENTING / v1.1.0 PUBLIC VERIFIED / LIVE TARKOV E2E PENDING`**

이 문서는 준현 헬퍼 Scanner의 공식 제품·기술 계약입니다.

## 1. 목적

Scanner는 Tarkov의 게임 로직을 다시 계산하는 기능이 아니라 **게임 화면을 기존 JunhyunHelper Item ID와 진행 데이터에 연결하는 입력 bridge**입니다.

```text
화면 픽셀
→ 아이템 상세창 구조 감지
→ 제목 ROI
→ 한국어 OCR
→ 현재 공식 아이템 전체 카탈로그 매칭
→ Item ID
→ 기존 JunhyunHelper 데이터
→ Mini Scanner
```

오탐(false positive)은 미탐(false negative)보다 나쁩니다. 확신이 부족하면 아무 Item도 확정하지 않습니다.

## 2. 입력 모드

### 스캐너 — 실사용

```text
EscapeFromTarkov 프로세스
→ MainWindowHandle
→ GetClientRect + ClientToScreen
→ Borderless client-area
→ detail detector
→ title ROI
→ OCR
```

- 대상 창 `PrintWindow(PW_CLIENTONLY | PW_RENDERFULLCONTENT)`를 우선 시도합니다.
- DirectX presentation path에서 유효 픽셀을 얻지 못하면 정확한 Borderless client screen rectangle을 캡처합니다.
- 최소화/유효하지 않은 client-area에서는 인식하지 않습니다.
- 프로세스가 없으면 대기 상태로 남습니다.

### 테스트 — 전체 디스플레이

- 연결된 모든 디스플레이를 순회합니다.
- 실사용과 **동일한 detector → OCR → matcher → presentation**을 사용합니다.
- Tarkov 전체 스크린샷을 바탕화면 또는 이미지 뷰어에 띄워 게임 없이 파이프라인을 검증할 수 있습니다.
- 다양한 이미지 표시 크기를 고려해 실사용보다 넓은 geometry scale search를 사용합니다.
- session-only이며 재실행 시 OFF입니다.

### 관계

- 실사용/테스트는 상호 배타적입니다.
- 한 모드를 켜면 다른 모드는 꺼집니다.
- 둘 다 OFF면 capture/detector/OCR background loop가 없습니다.

## 3. 금지된 접근

Scanner는 다음을 사용하지 않습니다.

- 게임 메모리 읽기
- DLL injection
- packet interception
- game-process 내부 상태 읽기
- icon 이미지 기반 Item identity
- scan 순간 외부 HTTP/API 요청

화면 픽셀과 로컬/메모리 캐시만 사용합니다.

## 4. 전체 Item identity 카탈로그

Scanner identity catalog는 Needed Items subset과 별개이며 Tarkov 전체 Item을 포함합니다.

source:

```text
https://json.tarkov.dev/{gameMode}/items
https://json.tarkov.dev/{gameMode}/items_ko
```

mode:

```text
regular
pve
pvp-season
```

Scanner는 stale catalog를 준비 단계에서 갱신할 수 있고 실제 scan 시에는 network를 사용하지 않습니다.

사용자 UI의 수동 강제 갱신 이름은:

```text
아이템 목록 최신화
```

입니다. 정상 사용에서는 자동 stale refresh가 우선이며 이 버튼은 패치 직후/캐시 복구/강제 최신화 용도입니다.

## 5. OCR / Item ID 식별

현재 production OCR은 Windows `ko-KR` OCR입니다. OCR 계층은 replaceable interface로 유지합니다.

matcher 순서:

1. Unicode FormKC + invariant lowercase + alphanumeric normalization
2. 전체 OCR / 줄 / separator variant 생성
3. normalized exact equality 우선
4. exact가 없으면 global fuzzy comparison
5. 길이에 따른 높은 confidence threshold
6. top-1 / top-2 margin gate
7. 부족하면 fail-closed

기본 fuzzy threshold는 높은 수준을 유지하며 짧은 이름일수록 더 엄격합니다. substring 단축 매칭은 사용하지 않습니다.

현재 한국어 클라이언트가 실제 표시하는 **현재 공식 문자열**이 identity truth입니다. 과거 한글명 alias를 무제한 누적하지 않습니다.

## 6. 안정화 상태 머신

고비용 OCR은 매 tick 실행하지 않습니다.

```text
cheap geometry observe
→ 동일 geometry 2회 이상
→ 동일 title signature 2회 이상
→ OCR
→ matcher
```

- 같은 성공 title은 반복 OCR하지 않습니다.
- title이 바뀌면 이전 Item 표시를 즉시 제거하고 새 title을 안정화합니다.
- 실패 title은 짧은 cooldown을 둡니다.
- 상세창이 사라지면 Item 결과를 제거하고 대기 상태로 돌아갑니다.

## 7. Item ID 이후 데이터

Scanner는 Quest/Hideout/Inventory 계산을 복제하지 않습니다.

Item ID 확정 후 기존 JunhyunHelper 데이터 흐름에서 가져옵니다.

- official name
- local cached icon
- best non-flea trader sell price
- flea average price
- slots
- trader/flea price per slot
- current needed

`current needed`는 부족량이 아니라 현재 진행 기준 총 필요 수량입니다.

```text
ItemsWorkspace.Plan.NeededItems[].RequiredTotal
```

`RemainingTotal` 또는 현재 보유량 차감 결과를 Scanner 표시 의미로 사용하지 않습니다.

## 8. Scanner 탭 — v1.1.1 운용 UI

Scanner 탭은 설명서가 아니라 운용 화면입니다.

### 상단 bar

왼쪽:

```text
스캐너 ON/OFF
테스트 ON/OFF
```

오른쪽:

```text
아이템 목록 최신화
```

상단 Scanner 제목, 기능 설명문, 버튼 설명문, 카탈로그 설명문, Mini Scanner 설명문은 표시하지 않습니다.

### 표시 정보

상단 bar 아래에 체크박스로 둡니다.

- 아이템 이름
- 아이템 아이콘
- 상인 판매 가격
- 플리마켓 평균 가격
- 상인 가격 / 슬롯
- 플리 가격 / 슬롯
- 현재 필요한 수량

### 최근 인식 기록

화면 하단에 최근 Item 식별 시도를 사람이 읽을 수 있게 표시합니다.

각 기록은 다음을 포함합니다.

- 시각
- 스캐너/테스트 모드
- OCR로 읽은 문자열
- 가장 가까운 공식 Item 이름
- top-1 유사도
- top-1 / top-2 차이
- `식별 성공` / `식별 보류`
- exact/fuzzy/low-confidence 등 판단 이유의 한국어 요약

예:

```text
화면에서 ‘들격소총’을 읽었고 ‘돌격소총’과 94.4% 일치해 해당 아이템으로 판단했습니다.
```

사용자 기록은 bounded in-memory feed이며 개발자 `scanner.log` 원문을 그대로 노출하지 않습니다.

## 9. Foundation 개발 경로

Foundation의 Item ID → presentation preview 경로는 개발/회귀 진단에 유용하므로 내부 API는 유지할 수 있습니다.

하지만 일반 Scanner 탭에는 다음을 노출하지 않습니다.

- Foundation 검증 도구 제목
- Item ID 입력
- Item ID 미리보기
- 자동 미리보기
- 미리보기 숨기기

## 10. Mini Scanner

Mini Scanner는 MiniMap과 완전히 독립된 Window/service/settings/lifecycle입니다.

표시:

- 배경 패널/타이틀/버튼 없이 최소 정보만 표시
- Scanner ON 상태에서는 standby 또는 Item 결과 표시
- OFF에서는 숨김
- Topmost
- ShowActivated=false
- `WS_EX_NOACTIVATE`
- `WS_EX_TOOLWINDOW`

### v1.1.1 위치 정책

별도 `위치 편집` / `위치 초기화` UI를 사용하지 않습니다.

Mini Scanner는 보이는 동안 언제든 왼쪽 마우스로 직접 드래그할 수 있습니다. Drag가 끝나면 좌표를 기존 Scanner settings에 즉시 저장합니다. 음수 multi-monitor 좌표도 정상 값입니다.

항상 직접 이동하려면 Mini Scanner Window가 자기 영역의 마우스 hit-test를 받아야 하므로 v1.1.0의 `WS_EX_TRANSPARENT` click-through는 **Mini Scanner에 한해 제거**합니다. `NOACTIVATE`는 유지해 게임의 키보드 포커스를 빼앗지 않습니다.

## 11. Settings / cache

```text
%LocalAppData%/JunhyunHelper/scanner-settings.json
%LocalAppData%/JunhyunHelper/scanner-settings.json.bak
%LocalAppData%/JunhyunHelper/scanner/catalog/items-{mode}-ko.json
%LocalAppData%/JunhyunHelper/scanner/catalog/items-{mode}-ko.json.bak
```

설정은 same-directory temp + flush + atomic replacement + `.bak` last-known-good recovery를 사용합니다.

## 12. 개발자 진단 로그

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

기록 가능:

- mode/runtime state
- capture/detector 상태
- detail candidate bounds/signature
- OCR text
- matcher result/confidence/reason
- runtime error metadata

저장하지 않음:

- screenshot
- raw pixel buffer

약 2MB에서 회전하며 logging 실패가 Scanner 동작을 실패시키지 않습니다.

## 13. 검증 상태

이미 검증된 사전 실험:

- 한국어 text OCR
- detail-view image detector
- full Tarkov screenshot detail detector
- full screenshot → detail → title ROI → OCR

v1.1.0 public release에서 검증:

- Windows Release build
- 243 automated tests
- detector/catalog/matcher regression
- win-x64 self-contained publish
- ProductVersion/FIRST_RUN/package hygiene
- published EXE startup
- Scanner safe-default controls
- 기존 Product UI / Main Map / Factory / MiniMap smoke
- Draft/Public asset re-download 검증
- public-downloaded EXE smoke

v1.1.1에서는 위 gate 전체에 더해 다음을 검증합니다.

- `아이템 목록 최신화` 렌더링
- 최근 인식 기록 empty state
- 사용자용 OCR/candidate/confidence/decision 문장
- 사용자 UI에서 위치 편집/초기화 및 Foundation preview controls 부재
- Mini Scanner direct-drag 구현의 Windows build/publish 안정성

## 14. Live Tarkov 후속 검증

최신 Borderless Tarkov 실제 E2E는 사용자 결정에 따라 release blocker가 아니며 후속 로그 기반 검증입니다.

확인 대상:

- PrintWindow vs client-rectangle fallback
- current detail geometry calibration
- current Korean title OCR
- false positive / false negative
- Mini Scanner 직접 drag와 게임 입력 coexistence
- 장시간 CPU/memory/handle/OCR rate
- Alt+Tab/minimize/MiniMap coexistence

문제가 발견되면 `scanner.log`를 기준으로 PATCH 보정합니다.

## 15. 결정

- Scanner subsystem: DEC-050
- v1.1.0 production/live policy: DEC-051
- v1.1.1 운용 UI / 최근 인식 기록 / always-draggable Mini Scanner: `docs/SCANNER_UI_DECISION_2026-08-21.md` (DEC-052)
- version policy: DEC-048
