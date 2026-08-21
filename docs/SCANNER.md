# Scanner — 제품/기술 계약

기준일: 2026-08-21

상태: **`v1.1.0 IMPLEMENTED / WINDOWS CI VERIFIED / LIVE TARKOV E2E PENDING`**

이 문서는 준현 헬퍼 Scanner의 공식 제품·기술 계약입니다.

## 1. 제품 목적

Scanner는 Tarkov 게임 로직을 새로 계산하는 기능이 아니라 **게임 화면과 기존 JunhyunHelper 데이터 사이의 안전한 입력 bridge**입니다.

```text
Tarkov 화면
→ 아이템 상세창 구조 감지
→ 제목 ROI 추출
→ 한국어 Windows OCR
→ 현재 공식 아이템 이름 매칭
→ Item ID 확정
→ 기존 JunhyunHelper 데이터 조회
→ Mini Scanner 표시
```

오탐(false positive)은 미탐(false negative)보다 나쁩니다. 확신이 부족하면 Item ID를 강제로 선택하지 않습니다.

## 2. v1.1.0 입력 모드

### Scanner ON — 실사용 모드

```text
EscapeFromTarkov 프로세스
→ MainWindowHandle
→ GetClientRect
→ ClientToScreen
→ Tarkov client-area
→ 실시간 상세창 감지
```

- 사용자 기준 기본 Tarkov 표시 설정: Borderless
- 우선 `PrintWindow(PW_CLIENTONLY | PW_RENDERFULLCONTENT)`로 대상 게임 창 자체 픽셀을 요청
- 현재 DirectX presentation path에서 유효한 픽셀이 반환되지 않으면 정확한 Borderless client rectangle의 화면 픽셀로 fallback
- 최소화되었거나 유효한 client-area가 없으면 인식하지 않음
- `EscapeFromTarkov` 프로세스가 없으면 Mini Scanner에 대기 상태 표시

### 테스트 ON — 전체 디스플레이 모드

- 연결된 모든 디스플레이를 순회해 화면 픽셀을 캡처
- 실사용 모드와 **동일한 detector → OCR → matcher → presentation** 파이프라인 사용
- Tarkov 전체 스크린샷을 바탕화면/이미지 뷰어에 표시해 게임 없이 회귀 확인 가능
- 다양한 화면 축소율을 고려해 실사용 모드보다 넓은 상세창 scale search 사용
- session-only: 프로그램 재실행 시 자동 OFF

### 모드 관계

- `스캐너 ON`과 `테스트 ON`은 상호 배타적
- 한 모드를 켜면 다른 모드는 자동 OFF
- 둘 다 OFF면 capture/detector/OCR background loop 없음

## 3. 금지된 접근

Scanner는 다음을 사용하지 않습니다.

- 게임 메모리 읽기
- DLL injection
- 패킷 가로채기
- game process 내부 상태/구조 읽기
- icon 이미지 기반 Item identity
- scan 순간 외부 API 요청

화면 픽셀과 로컬/메모리 캐시만 사용합니다.

## 4. 상세창 구조 detector

`ScannerDetailGeometryDetector`는 BGRA 픽셀에서 상세창의 외곽 frame/edge/tone/close-glyph 특징을 보수적으로 평가합니다.

- canonical 기준은 기존 전체 Tarkov 스크린샷 검증에서 얻은 상세창 비율
- 실사용 모드: 좁은 scale/center search
- 테스트 모드: 축소된 screenshot viewer까지 고려한 확장 scale/center search
- geometry score 통과는 Item 확정이 아니라 OCR 후보 생성 조건일 뿐임
- geometry 후보가 2회 연속 안정화되어야 제목 안정화 단계로 이동

단위 회귀 테스트:

- 합성 centered detail panel 감지
- uniform frame 거부
- display-test 축소 상세창 감지

## 5. 제목 ROI / OCR

상세창이 안정화되면 detector가 제목 영역만 잘라 `BitmapSource`로 전달합니다.

OCR:

- Windows `Windows.Media.Ocr.OcrEngine`
- `ko-KR`
- 원본 ROI 인식
- 허용 범위에서 최대 2배 확대 ROI를 추가 인식
- 두 결과를 matcher input variant로 전달

Windows 한국어 OCR 언어 팩/런타임 초기화가 실패하면 Scanner만 fail-closed하고 앱의 다른 기능은 계속 사용할 수 있습니다.

같은 title signature가 유지되면 OCR을 반복하지 않습니다.

## 6. 전체 아이템 identity catalog

Scanner identity catalog는 Needed Items subset이 아니라 **전체 Tarkov 아이템**을 대상으로 합니다.

source:

```text
https://json.tarkov.dev/{gameMode}/items
https://json.tarkov.dev/{gameMode}/items_ko
https://json.tarkov.dev/{gameMode}/items_en
```

mode:

```text
Regular    → regular
Pve        → pve
PvpSeason  → pvp-season
```

cache:

```text
%LocalAppData%/JunhyunHelper/scanner/catalog/items-{mode}-ko.json
%LocalAppData%/JunhyunHelper/scanner/catalog/items-{mode}-ko.json.bak
```

정상 최소 조건:

- schema/source/language/mode 일치
- 생성 시각 존재
- 최소 4,000 item
- 모든 item에 non-empty Item ID / official name

stale 기준은 현재 12시간입니다.

## 7. scan-time 네트워크 금지

```text
Scanner enable / 명시적 sync / profile 전환
→ cache 확인
→ 필요 시 pre-scan catalog sync
→ 실제 scanning
→ detector/OCR/matcher/presentation은 local/in-memory only
```

아이콘 cache가 없더라도 scan 순간 다운로드하지 않습니다.

## 8. matcher 계약

`ScannerItemMatcher`는 current official Korean-client name exact match를 최우선으로 합니다.

정규화:

- Unicode FormKC
- invariant lowercase
- 문자/숫자만 유지

exact 이후에만 fuzzy를 사용합니다.

fuzzy 기본 gate:

- confidence >= 0.90
- top1-top2 margin >= 0.05
- 짧은 이름은 더 높은 threshold/margin
- bigram overlap prefilter
- global Levenshtein similarity
- 동일 normalized official name이 여러 Item ID에 있으면 ambiguous fail

낮은 confidence에서 1위 후보를 강제 선택하지 않습니다.

## 9. Item ID → 기존 JunhyunHelper bridge

Item ID가 확정된 뒤 Scanner는 Quest/Hideout 필요량 로직을 재구현하지 않습니다.

현재 필요한 수량:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

보유량을 뺀 `RemainingTotal`은 Scanner의 `현재 필요한 수량` 의미가 아닙니다.

표시 가능 항목:

- 공식 아이템 이름
- local cached icon
- 최고 non-flea 상인 판매가
- flea 24h 평균가
- 상인 판매가 / 슬롯
- flea 평균가 / 슬롯
- 현재 필요한 수량

값이 없으면 만들어내지 않고 해당 line을 생략합니다.

## 10. Mini Scanner overlay

Mini Scanner는 MiniMap과 독립된 Window/service/lifecycle/settings를 사용합니다.

ON 직후에도 창을 표시하고 현재 상태를 보여줍니다.

예:

- Tarkov 게임 창 찾는 중
- 상세창 기다리는 중
- 상세창 위치 확인 중
- 아이템 제목 읽는 중
- 식별 불확실
- 확정된 아이템 정보

play mode:

- transparent / no chrome
- Topmost
- taskbar 미표시
- `ShowActivated=false`
- `WS_EX_TRANSPARENT`
- `WS_EX_NOACTIVATE`
- `WS_EX_TOOLWINDOW`

edit mode에서만 click-through/no-activate를 일시 해제하고 드래그 위치를 저장합니다.

## 11. 설정

```text
%LocalAppData%/JunhyunHelper/scanner-settings.json
%LocalAppData%/JunhyunHelper/scanner-settings.json.bak
```

설정:

- Scanner real-mode enabled
- item name/icon
- trader/flea price
- trader/flea price per slot
- current needed
- overlay nullable X/Y
- font size

테스트 모드는 영구 설정에 저장하지 않습니다.

## 12. runtime state machine

```text
Disabled / NoProfile / CatalogUnavailable / WaitingForVision
↓
WaitingForInspectWindow
↓
geometry candidate
↓
geometry stable >= 2 observations
↓
title signature stable >= 2 observations
↓
OCR once
↓
conservative match
↓
Item ID
↓
presentation snapshot
↓
ShowingItem
```

failed title은 짧은 cooldown 동안 OCR 반복을 억제합니다.

## 13. Scanner 진단 로그

경로:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

목적은 v1.1.0 이후 실제 Tarkov 환경에서 capture/detector/OCR/matcher 문제를 사용자와 함께 분석하는 것입니다.

기록:

- runtime mode/start/stop
- state transition
- 상세창 candidate bounds/signature
- 안정화된 title OCR 문자열
- matcher success/reason/Item ID/name/confidence/second score
- runtime error type/message

정책:

- 최대 약 2MB 후 이전 파일 `.1`로 회전
- 전체 screenshot 저장 금지
- raw pixel buffer 저장 금지
- log 실패는 Scanner 동작에 영향 없음

## 14. 검증 상태

v1.1.0 이전 실험에서 통과한 범위:

- 한국어 텍스트 OCR
- 상세보기 이미지 단독 감지
- 전체 Tarkov screenshot에서 상세창 구조 감지
- 전체 screenshot → 상세창 → 제목 ROI → OCR 경로

현재 v1.1.0 코드 자동 검증:

- Windows .NET 10 Release build
- Scanner geometry detector regression tests
- 기존 catalog/matcher/persistence tests
- win-x64 self-contained single-file publish
- published EXE startup/rendered Product UI smoke
- Scanner real/test toggle UI smoke
- Main Map / Factory / MiniMap smoke
- graceful shutdown

## 15. 의도적으로 후속으로 남기는 live Tarkov 검증

사용자 결정에 따라 **v1.1.0 공개 릴리즈의 차단 조건에서 실제 인게임 검증을 제외합니다.**

따라서 공개 시점에도 다음은 `LIVE UNVERIFIED`로 남습니다.

- 현재 Tarkov Borderless에서 PrintWindow가 실제 DirectX frame을 반환하는지 또는 screen-rectangle fallback이 사용되는지
- 실제 최신 client UI에서 geometry threshold/scale가 충분한지
- 실제 최신 한국어 아이템 제목 OCR 품질
- 장시간 레이드 CPU/memory/handle/OCR rate
- Alt+Tab/minimize/MiniMap coexistence
- 실제 false-positive/false-negative calibration

이 범위는 v1.1.0 공개 후 `scanner.log`와 사용자 인게임 검증을 통해 조정합니다.

**`LIVE UNVERIFIED`는 Scanner 코드가 placeholder라는 뜻이 아닙니다. v1.1.0에는 실제 capture/detector/OCR/matcher/Mini Scanner 기능이 구현되어 있으며, 최신 게임 환경 E2E만 후속 검증 대상입니다.**
