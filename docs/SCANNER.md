# Scanner — 제품/기술 계약

기준일: 2026-08-21

상태: **`v1.1.3 PUBLIC RELEASE / VERIFIED / SCANNER LAB v3.8 RECOGNITION RESTORED / LIVE TARKOV REVALIDATION ONGOING`**

이 문서는 준현 헬퍼 Scanner의 공식 제품·기술 계약입니다.

## 1. 목적

Scanner는 Tarkov의 게임 로직을 다시 계산하는 기능이 아니라 **게임 화면을 기존 JunhyunHelper Item ID와 진행 데이터에 연결하는 입력 bridge**입니다.

```text
화면 픽셀
→ 상세창 structural candidates
→ candidate title ROI
→ 한국어 OCR
→ 현재 공식 아이템 전체 카탈로그 semantic validation
→ Item ID
→ 기존 JunhyunHelper 데이터
→ Mini Scanner
```

오탐(false positive)은 미탐(false negative)보다 나쁩니다. 구조적으로 그럴듯한 창을 발견했다는 이유만으로 Item을 확정하지 않으며, OCR과 현재 공식 카탈로그까지 안전하게 통과해야 합니다.

## 2. 입력 모드

### 스캐너 — 실사용

```text
EscapeFromTarkov 프로세스
→ MainWindowHandle
→ GetClientRect + ClientToScreen
→ Borderless client-area
→ candidate detector
→ OCR / semantic validation
```

- 대상 창 `PrintWindow(PW_CLIENTONLY | PW_RENDERFULLCONTENT)`를 우선 시도합니다.
- 유효한 게임 픽셀이 나오지 않으면 정확한 Borderless client screen rectangle을 캡처합니다.
- 최소화/유효하지 않은 client-area에서는 인식하지 않습니다.
- 프로세스가 없으면 대기 상태로 남습니다.

### 테스트 — 전체 디스플레이

- 연결된 모든 디스플레이를 순회합니다.
- 실사용과 **동일한 candidate detector → OCR → semantic catalog validation → presentation**을 사용합니다.
- Tarkov 전체 스크린샷을 바탕화면/이미지 뷰어에 띄워 게임 없이 파이프라인을 검증할 수 있습니다.
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

사용자 UI의 수동 강제 갱신 이름은 `아이템 목록 최신화`입니다. 정상 사용에서는 자동 stale refresh가 우선이며 이 버튼은 패치 직후/캐시 복구/강제 최신화 용도입니다.

## 5. Scanner Lab v3.8 recognition architecture

v1.1.3부터 화면 인식의 기준 구조는 사용자가 보존하고 있던 실제 Scanner Lab v3.8 원본입니다.

상세 reference: `docs/SCANNER_LAB_3_8_REFERENCE.md`

### 5.1 Structural candidate generation

```text
capture
→ RED-X connected-component candidates
+
→ edge/rectangle fallback candidates
→ IoU deduplication
→ 최대 8개 candidates
```

#### RED-X path

- 우상단 dark-red close-control connected component를 찾습니다.
- broad color 조건과 component geometry로 후보를 만듭니다.
- close-control을 anchor로 right border와 아래 방향 window extent를 추정합니다.
- 약 `1.30` 중심의 inspect-window aspect와 border continuity를 이용해 outer rectangle을 구성합니다.

#### rectangle fallback

RED-X가 손상/누락되거나 표시 조건이 다를 수 있으므로 edge projection 기반 rectangle 후보를 별도로 생성합니다.

- vertical/horizontal edge line 후보
- inspect-like aspect
- border continuity
- interior darkness
- optional red-X proximity

Structural floor는 `0.34`입니다.

**Structural score는 후보 순위이지 최종 상세창 판정이 아닙니다.**

### 5.2 Candidate title ROI

v3.8에서 검증된 title ROI 공식을 사용합니다.

```text
titleX = window.Left + window.Width * 0.032
titleY = window.Top - 1
titleWidth = window.Width * 0.64
titleHeight = max(12, window.Height * 0.052)
```

약 `674x514` 상세창에서는 약 `431x27` title ROI가 됩니다.

사용자가 제공한 v3.8 자료의 대표 기준:

- cropped `Ophthalmoscope 검안경`: outer inspect 약 `3,3,672,514`
- full `Water 0.6L 물병` screenshot: inspect 약 `622,282,674,514`

### 5.3 Adaptive OCR

현재 production OCR은 Windows `ko-KR`이며 replaceable interface를 유지합니다.

candidate title 높이에 따라 확대합니다.

- `<=14px`: 8x
- `<=20px`: 6x
- 그 외: 4x

1차 OCR에서 official item resolution이 모두 실패하면 상위 3개 candidate를 deep OCR합니다.

1. enlarged original
2. high-contrast grayscale
3. binary white-on-black
4. inverse black-on-white

OCR engine이 한 Item 이름을 여러 line으로 나눌 수 있으므로 개별 line과 인접 두 line 결합 candidate를 모두 matcher에 전달합니다.

### 5.4 Semantic candidate validation

최대 8개 structural candidate를 OCR하고 **현재 공식 전체 아이템 카탈로그와 대조**합니다.

- geometry alone → final inspect 아님
- OCR/candidate name → current official catalog match 필요
- exact/fuzzy confidence/margin gate 통과 필요
- semantic confidence와 structural score를 함께 후보 순위에 사용
- 공식 Item으로 resolve되지 않은 candidate는 실제 inspect로 확정하지 않음
- 이미 성공한 inspect의 title signature가 동일하면 OCR 반복을 생략

이 구조가 v3.8과 현재 JunhyunHelper 통합 사이에서 가장 중요한 계약입니다.

## 6. Item matcher

matcher 순서:

1. Unicode FormKC + invariant lowercase + alphanumeric normalization
2. 전체 OCR / 줄 / separator variant 생성
3. normalized exact equality 우선
4. exact가 없으면 global fuzzy comparison
5. 길이에 따른 높은 confidence threshold
6. top-1 / top-2 margin gate
7. 부족하면 fail-closed

짧은 이름일수록 더 엄격합니다. substring shortcut은 사용하지 않습니다.

현재 한국어 클라이언트가 실제 표시하는 **현재 공식 문자열**이 identity truth입니다. 과거 Scanner Lab 테스트 alias나 과거 번역을 production matcher에 추가해 억지로 성공시키지 않습니다.

## 7. 런타임 안정화 / 반복 방지

- 구조 후보가 최소 2회 관측된 뒤 semantic OCR을 시도합니다.
- candidate search는 최대 8개, deep OCR은 상위 3개입니다.
- 동일하게 검증된 title signature는 반복 OCR하지 않습니다.
- Item/title이 바뀌면 이전 Item 표시를 제거하고 다시 semantic validation합니다.
- 실패 후에는 짧은 retry interval을 둡니다.
- 상세창 후보가 사라지면 Item 결과를 제거하고 대기 상태로 돌아갑니다.

## 8. Item ID 이후 데이터

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

## 9. Scanner 탭

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

- 아이템 이름
- 아이템 아이콘
- 상인 판매 가격
- 플리마켓 평균 가격
- 상인 가격 / 슬롯
- 플리 가격 / 슬롯
- 현재 필요한 수량

### 최근 인식 기록

화면 하단에서 OCR/matcher 판정을 사용자 문장으로 보여줍니다.

- 시각
- 스캐너/테스트 모드
- OCR 문자열
- 가장 가까운 공식 Item
- top-1 similarity
- top-1/top-2 margin
- 식별 성공/보류
- exact/fuzzy/low-confidence 등 판단 이유

사용자 기록은 개발자 `scanner.log` 원문을 그대로 노출하지 않습니다.

## 10. Foundation 개발 경로

Foundation의 Item ID → presentation preview 내부 API는 개발/회귀 진단에 유지할 수 있습니다.

일반 Scanner 탭에는 Foundation 검증 제목, Item ID 입력, preview controls를 노출하지 않습니다.

## 11. Mini Scanner

Mini Scanner는 MiniMap과 독립된 Window/service/settings/lifecycle입니다.

- 배경 패널/타이틀/버튼 없이 최소 정보
- Scanner ON 상태에서는 standby 또는 Item 결과
- OFF에서는 숨김
- Topmost
- ShowActivated=false
- `WS_EX_NOACTIVATE`
- `WS_EX_TOOLWINDOW`
- 별도 위치 편집/초기화 mode 없음
- visible 상태에서 언제든 left-drag
- drag 종료 위치 즉시 저장
- negative multi-monitor 좌표 허용

직접 이동을 위해 Mini Scanner 자기 영역은 mouse hit-test를 받지만 no-activate를 유지하므로 게임의 keyboard focus를 가져가지 않습니다.

## 12. Settings / cache

```text
%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/catalog/items-{mode}-ko.json(.bak)
```

same-directory temp + flush + atomic replacement + `.bak` last-known-good recovery를 사용합니다.

## 13. 개발자 진단 로그

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

v1.1.3에서 기록 가능한 주요 정보:

- mode/runtime state
- capture/detector 상태
- structural candidate count/top bounds/score/reason
- candidate별 OCR pass (`ORIGINAL` / deep preprocessing)
- candidate별 official match/reason/confidence
- semantic-selected candidate / bounds
- runtime error metadata

저장하지 않음:

- screenshot
- raw pixel buffer

약 2MB에서 회전하며 logging 실패가 Scanner 동작을 실패시키지 않습니다.

## 14. 검증 상태

Scanner Lab v3.8 복원 제품 코드 validation CI `#1222` / run `32466187224`:

- Windows Release build: SUCCESS
- **245 automated tests / 0 failed / 0 skipped**
- cropped Ophthalmoscope-shape v3.8 geometry regression: SUCCESS
- full Water-screenshot-shape v3.8 geometry regression: SUCCESS
- inner rectangle coexistence: SUCCESS
- no-RED-X rectangle fallback: SUCCESS
- uniform frame fail-closed: SUCCESS
- win-x64 self-contained single-file publish: SUCCESS
- actual candidate EXE Product UI / Scanner / Main Map / Factory / MiniMap smoke: SUCCESS
- graceful shutdown / clean portable root: SUCCESS

최종 v1.1.3 public release verification:

```text
release source: 8803f899341859887281ad50135911f4625a64f3
release run: 32470606548
245 passed / 0 failed / 0 skipped
ZIP bytes: 80,251,960
ZIP SHA-256: 419f6288aa3202f10868f2fe6a4ccac40475753ce4ba8c8c2d9985396c4bf493
ProductVersion: 1.1.3+8803f899341859887281ad50135911f4625a64f3
Draft download/package verification: SUCCESS
Draft-downloaded EXE smoke: SUCCESS
public/latest exact tag verification: SUCCESS
public download/package verification: SUCCESS
public-downloaded EXE smoke: SUCCESS
```

## 15. Live Tarkov 후속 검증

최신 Borderless Tarkov 실제 E2E는 DEC-051에 따라 release blocker가 아니며 후속 로그 기반 검증입니다.

v1.1.3 우선 확인:

- `Ophthalmoscope 검안경` 실제 화면 감지 복구 여부
- current title OCR
- candidate semantic validation
- 다른 Item detail window 인식률
- false positive / false negative
- 장시간 CPU/memory/handle/OCR rate
- Mini Scanner 직접 drag와 게임 입력 coexistence
- Alt+Tab/minimize/MiniMap coexistence

문제가 발견되면 candidate/ocr/match/selected 로그를 기준으로 PATCH 보정합니다.

## 16. 결정 / reference

- Scanner subsystem: DEC-050
- production/live policy: DEC-051
- 운용 UI / 최근 인식 기록 / always-draggable Mini Scanner: DEC-052
- Scanner Lab v3.8 recognition architecture: DEC-053
- Scanner Lab v3.8 reference: `docs/SCANNER_LAB_3_8_REFERENCE.md`
- v1.1.3 release record: `docs/RELEASE_1.1.3.md`
- version policy: DEC-048
