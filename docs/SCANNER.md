# Scanner — 제품 계약과 Foundation 아키텍처

기준일: 2026-08-21

> 이 문서는 준현 헬퍼 Scanner의 공식 제품/기술 계약입니다. 실제 Tarkov 창 캡처·최신 상세창 detector·한국어 OCR 입력부는 실게임 검증 전까지 의도적으로 unavailable 상태로 유지합니다.

## 1. 현재 상태

Scanner는 더 이상 요구사항 미정 placeholder가 아닙니다.

현재 단계는 **Foundation 구현 단계**입니다.

완성 대상으로 취급하는 범위:

- Scanner 설정과 안전한 persistence
- Scanner 전용 전체 아이템 identity/market catalog
- 보수적 OCR matcher
- Item ID → 기존 JunhyunHelper 데이터 bridge
- `현재 필요한 수량 = FutureNeededItemsPlan.NeededItems[].RequiredTotal`
- scan-time local-only icon lookup
- 독립 Mini Scanner overlay
- Scanner runtime/state machine
- detector/OCR abstraction
- Scanner 설정/상태/미리보기 Page
- 앱 종료 시 Scanner resource cleanup

아직 정식 기능으로 취급하지 않는 범위:

- 실제 Tarkov 프로세스/창 선택
- Tarkov 창 전용 캡처
- 최신 상세창 구조 detector
- 최신 한국어 Tarkov title OCR
- 실제 레이드 장시간 E2E 검증

따라서 현재 기본 detector/OCR 구현은 `Unavailable*`이며, 검증되지 않은 자동 인식 루프를 실행하지 않습니다.

## 2. 제품 목적

Scanner는 게임 로직을 새로 계산하는 시스템이 아닙니다.

```text
Tarkov 화면
→ 아이템 상세창 감지
→ 제목 OCR
→ 현재 공식 한국어 아이템명 매칭
→ Item ID 확정
→ 기존 JunhyunHelper 데이터 조회
→ Mini Scanner 표시
```

즉 Scanner의 책임은 **게임 화면과 기존 JunhyunHelper 데이터 사이의 안전한 입력 bridge**입니다.

## 3. 우선순위와 실패 정책

우선순위:

1. 작동성
2. 신뢰성
3. 안정성
4. 성능
5. 편의
6. 시각 장식

오탐(false positive)은 미탐(false negative)보다 나쁩니다.

따라서 확신이 부족하면 아무것도 표시하지 않습니다.

금지:

- 낮은 confidence에서 1위 후보 강제 선택
- 과거 이름 alias 누적
- 짧은 이름의 substring만으로 강제 일치
- 전체 필요한 아이템 subset만 identity catalog로 사용
- icon 이미지 기반 식별
- scan 시점 네트워크 요청

## 4. 지원 언어와 이름의 진실 원천

Scanner는 **한국어 Tarkov 클라이언트 전용**입니다.

정답 문자열은 현재 한국어 클라이언트에 실제 표시되는 공식 아이템 이름입니다.

- 한국어로 번역된 이름 → 해당 한국어 문자열
- 번역되지 않아 영어가 표시되는 이름 → 해당 영어 문자열
- 한국어+영어 혼합 → 그 혼합 문자열 자체

Scanner가 임의로 번역하지 않습니다.

과거 인터넷 캡처의 이름은 detector/ROI/OCR 실험에는 사용할 수 있지만 정식 identity alias로 추가하지 않습니다.

## 5. 전체 아이템 카탈로그

### 책임

Scanner identity catalog는 모든 Tarkov 아이템을 화면 이름으로 식별하기 위한 별도 데이터입니다.

기존 `GameContentCatalog`나 Needed Items가 Scanner 전체 identity catalog라고 가정하지 않습니다.

연결 key는 Tarkov Item ID입니다.

### 소스

현재 source:

```text
https://json.tarkov.dev/{gameMode}/items
https://json.tarkov.dev/{gameMode}/items_ko
https://json.tarkov.dev/{gameMode}/items_en   # per-key fallback
```

지원 mode는 현재 앱 profile과 동일합니다.

```text
Regular    → regular
Pve        → pve
PvpSeason  → pvp-season
```

별도 Scanner game mode 설정을 만들지 않습니다.

### cache

경로:

```text
%LocalAppData%/JunhyunHelper/scanner/catalog/items-{mode}-ko.json
%LocalAppData%/JunhyunHelper/scanner/catalog/items-{mode}-ko.json.bak
```

`AtomicJsonFileStore`를 사용합니다.

정상 catalog 최소 조건:

- schema/source/language/mode 일치
- 생성 시각 존재
- 최소 4,000개 item
- 모든 item에 non-empty Item ID / official name

현재 sync stale 기준은 12시간입니다. 사용자는 Scanner Page에서 명시적으로 다시 동기화할 수 있습니다.

다른 mode의 cache가 없거나 손상되면 이전 mode identity를 재사용하지 않고 requested mode를 빈 catalog 상태로 둡니다.

## 6. scan-time 네트워크 금지

정상 흐름:

```text
Scanner enable / 명시적 sync / profile 전환
→ local cache 확인
→ 필요 시 pre-scan catalog sync
→ gameplay
→ detector/OCR/matcher/presentation은 local/in-memory only
```

실제 상세창을 읽는 순간에는 외부 API를 호출하지 않습니다.

## 7. matcher 계약

`ScannerItemMatcher`는 공식 이름 exact match를 최우선으로 합니다.

정규화:

- Unicode FormKC
- invariant lowercase
- 문자/숫자만 유지

OCR text variant:

- 전체 OCR 문자열
- CR/LF/`|` 단위 분리 문자열

exact match 이후에만 fuzzy를 수행합니다.

현재 fuzzy 기본 gate:

- confidence >= 0.90
- 1위-2위 margin >= 0.05
- 짧은 공식 이름은 더 엄격한 threshold/margin
- bigram overlap으로 후보를 먼저 제한
- global Levenshtein similarity 사용

동일 normalized official name이 여러 Item ID에 존재하면 ambiguous로 실패합니다.

구버전 예시:

```text
OCR: Water 0.6L 물병
현재 공식: 물병 Bottle of water (0.6L)
```

이런 경우 비슷하더라도 confidence가 부족하면 실패해야 합니다.

## 8. Item ID → 기존 데이터 bridge

Scanner가 Item ID를 확정한 뒤에는 Quest/Hideout 필요량 로직을 새로 계산하지 않습니다.

현재 필요한 수량:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

사용하지 않는 값:

```text
RemainingTotal   # 보유량을 뺀 부족량
```

보유량은 Scanner 결과에 표시하지 않습니다.

표시 가능한 값:

- 공식 아이템 이름
- local cached icon
- 최고 non-flea 상인 판매가
- flea 24h 평균가
- 상인 판매가 / 슬롯
- flea 평균가 / 슬롯
- 현재 필요한 수량

가격 또는 slot 정보가 없으면 값을 만들어내지 않고 해당 line을 생략합니다.

## 9. icon 정책

icon은 식별에 사용하지 않습니다.

Scanner는 기존 JunhyunHelper image-cache 파일 naming contract를 읽기 전용으로 재사용합니다.

scan 시점에 icon이 cache에 없으면:

```text
icon 생략
Scanner 나머지 정보 정상 표시
```

icon 하나 때문에 HTTP 요청을 만들지 않습니다.

## 10. Mini Scanner overlay

Mini Scanner는 MiniMap과 독립된 Window/service/lifecycle/settings를 가집니다.

play mode:

- transparent
- no title/chrome
- no background panel
- Topmost
- `ShowActivated=false`
- `WS_EX_TRANSPARENT`
- `WS_EX_NOACTIVATE`
- `WS_EX_TOOLWINDOW`
- 게임 입력/포커스 방해 금지

edit mode:

- click-through/no-activate를 일시 해제
- 드래그 가능
- 종료 시 위치 저장

위치는 nullable X/Y로 저장합니다. 좌측/상단 보조 모니터의 음수 좌표도 정상 값입니다. `-1` 같은 sentinel을 사용하지 않습니다.

Window 조작은 detector/OCR background thread에서 호출되더라도 WPF UI Dispatcher로 marshal합니다.

## 11. Scanner 설정

경로:

```text
%LocalAppData%/JunhyunHelper/scanner-settings.json
%LocalAppData%/JunhyunHelper/scanner-settings.json.bak
```

`user.db` schema migration을 하지 않습니다.

설정:

- Scanner enabled
- item name
- icon
- trader price
- flea average price
- trader price/slot
- flea price/slot
- current needed
- nullable overlay X/Y
- presentation font size

Scanner OFF일 때 detector/OCR background loop를 실행하지 않습니다.

## 12. runtime state machine

목표 흐름:

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
current JunhyunHelper presentation snapshot
↓
ShowingItem
```

같은 title signature가 유지되면 OCR을 반복하지 않습니다.

다른 title signature가 나타나면 이전 item overlay를 **즉시 숨긴 뒤** 새 title이 안정화될 때까지 기다립니다.

상세창 miss가 연속 2회 발생하면 overlay를 숨깁니다.

같은 실패 title은 짧은 cooldown 동안 OCR 반복을 억제합니다.

기본 관찰 interval은 350ms입니다. 실제 live detector 성능 검증 후 조정할 수 있습니다.

## 13. profile/context lifecycle

Scanner는 현재 활성 JunhyunHelper profile의 GameMode와 current ItemsWorkspace를 사용합니다.

별도 Scanner profile/mode 상태를 만들지 않습니다.

Scanner enabled 상태에서는 경량 context monitor가 profile/mode 변화를 감지합니다.

profile/mode 변경 시:

1. 기존 overlay/result 즉시 무효화
2. 새 mode catalog 준비
3. 필요 시 pre-scan sync
4. runtime 재개

Scanner disabled 상태에서는 monitor를 실행하지 않습니다.

## 14. 현재 vision boundary

인터페이스:

```text
IScannerInspectDetector
IScannerOcrEngine
```

Foundation 기본 구현:

```text
UnavailableScannerInspectDetector
UnavailableScannerOcrEngine
```

이 상태에서는 자동 scan loop를 시작하지 않습니다.

향후 live 구현은 다음 제약을 지켜야 합니다.

- 게임 메모리 읽기 금지
- DLL injection 금지
- 패킷 가로채기 금지
- game process 내부 데이터 접근 금지
- 가능하면 Tarkov 게임 창 자체만 capture
- own MiniMap/Mini Scanner가 capture에 섞이지 않도록 검증

## 15. Scanner Page

최소 user-facing 기능:

- Mini Scanner ON/OFF
- 표시 정보 checkboxes
- runtime/catalog 상태
- full catalog sync
- 위치 편집
- 위치 초기화

Foundation 검증용 접힌 도구:

```text
Item ID
→ catalog identity/price
→ current RequiredTotal
→ local icon
→ Mini Scanner render
```

이 도구는 live Scanner가 안정화된 최종 사용자 릴리스에서 숨기거나 제거할 수 있습니다.

## 16. 실게임 검증 Gate

### Gate A — Tarkov window capture

- actual Tarkov window 찾기
- window-only capture
- borderless/fullscreen
- DPI/resolution
- own overlay exclusion

### Gate B — current inspect detector

- stash/inventory/trader/raid
- 여러 위치/크기/배경/해상도
- no-inspect negative samples
- false-positive audit

### Gate C — Korean OCR

- current Korean client official displayed name
- OCR noise distribution
- title ROI robustness

### Gate D — Item ID

- exact/fuzzy confidence
- top1/top2 margin
- ambiguous/short names
- fail-closed audit

### Gate E — End-to-End

```text
actual Tarkov
→ inspect
→ OCR
→ Item ID
→ prices / RequiredTotal / local icon
→ Mini Scanner
```

### Gate F — long-run stability

- CPU
- memory
- GDI/User/handle leak
- OCR repeat rate
- MiniMap coexistence
- keyboard/mouse focus
- Alt+Tab
- minimize/restore
- Scanner OFF
- app shutdown cleanup

## 17. 릴리스 상태

현재 public stable baseline은 **v1.0.0**입니다.

Scanner는 새 사용자 기능이므로 live validation 후 정식 릴리스할 경우 버전 정책상 목표는 **v1.1.0**입니다.

Foundation 코드가 build/test를 통과하더라도 실제 Tarkov Gate A~F가 끝나기 전에는 Scanner를 정식 안정 기능으로 선언하거나 v1.1.0을 공개하지 않습니다.
