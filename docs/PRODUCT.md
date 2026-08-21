# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 **무엇을 만들고 왜 만드는지**를 정의하는 공식 제품 요구사항입니다. 사용자 확정 의도가 과거 구현보다 우선합니다.

기준일: 2026-08-21

## 1. 제품 정의

`CONFIRMED`

**준현 헬퍼**는 Escape from Tarkov의 게임 데이터와 사용자 진행 상태를 결합해 Quest, Hideout, Needed Items, Inventory, Items, Ammo, Map/MiniMap 및 Scanner 정보를 제공하는 Windows x64 데스크톱 헬퍼입니다.

핵심 원칙:

- 일반 Game Content 변화는 importer가 이해하는 범위에서 자동 흡수
- 의미/schema가 검증 불가능하게 변하면 fail-closed
- 실패 candidate가 last-known-good content를 덮어쓰지 않음
- User Progress와 Game Content 분리
- runtime GPT/AI 의존성 없음
- 현재 코드가 존재한다는 이유만으로 제품 요구사항으로 추정하지 않음

## 2. 플랫폼 / 배포

`CONFIRMED / IMPLEMENTED`

- Windows x64
- .NET 10 WPF
- self-contained single-file executable
- portable ZIP
- 별도 .NET Runtime 불필요
- 관리자 권한 불필요
- installer 없음
- 현재 code signing 없음

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

사용자 데이터:

```text
%LocalAppData%/JunhyunHelper
```

## 3. Game Content Update

`CONFIRMED / IMPLEMENTED`

```text
online source
→ external-format/required-semantics validation
→ canonical model
→ candidate DB
→ relation/read-back validation
→ active replacement
→ image prefetch
→ User Progress 결합
```

- failed candidate 폐기, 기존 active 유지
- `user.db` 삭제/덮어쓰기 금지
- 파생 결과를 별도 권위 데이터로 저장하지 않음
- 개별 image 실패는 update 전체 실패가 아님

```text
Content schema: v7
Readable: v3, v4, v5, v6, v7
```

## 4. Program Update

`CONFIRMED / IMPLEMENTED / PUBLIC VERIFIED`

일반 실행 시 `Propeex/JunhyunHelper` latest public stable GitHub Release를 확인합니다.

- current보다 strictly newer stable `vMAJOR.MINOR.PATCH`만 대상
- 사용자 동의형
- exact Windows ZIP + `SHA256SUMS.txt`
- checksum/archive/package-root 검증 전 현재 파일 변경 금지
- temporary self-copy updater로 program-owned files transaction 교체
- 실패 시 rollback/기존 실행 복구 시도
- 사용자 데이터는 update 대상 아님
- 정식 release는 Draft asset 검증 후 public/latest 전환

## 5. User Progress / Profile

`CONFIRMED / IMPLEMENTED`

GameMode별 독립 profile:

- regular
- pve
- pvp-season

저장 사실:

- level / faction / edition / prestige
- trader LL / standing
- completed Quest
- explicit permanent failed Quest
- exact observed ProfileVariables
- recoverable special-trader access
- Hideout levels
- FIR / non-FIR Inventory
- Quest / Hideout consumption ledgers

```text
user.db SQLite schema: v1
```

## 6. Quest

`CONFIRMED / IMPLEMENTED`

제품 상태:

- 진행 중
- 확인 필요
- 잠김
- 사용 불가
- 완료

원칙:

- 서로 다른 `taskRequirements` = AND
- 한 requirement의 `status[]` = OR
- 받을 수 있는 Quest는 Helper에서 이미 수락한 것으로 간주
- source보다 강한 prerequisite 임의 생성 금지
- 증명할 수 없는 availability = `확인 필요`
- exact profile-variable 값이 있으면 권위값
- audited compatibility가 drift하면 fail-closed

## 7. Quest Item / Consumption

`CONFIRMED / IMPLEMENTED`

- mandatory fixed submit material은 completion과 함께 ledger 기반 자동 소비 가능
- flexible hand-in은 후보 group으로 유지
- 실제 소비 후보 자동 추정 금지
- rollback은 consumed ledger로 복구/중복 소비 방지
- malformed empty candidate/non-positive requirement는 active DB 적용 전 차단

## 8. Hideout

`CONFIRMED / IMPLEMENTED`

- station current level 저장
- 미래 upgrade requirement 포함
- fixed material 소비/rollback ledger
- 미입력 station = Lv.0

## 9. Needed Items / Inventory

`CONFIRMED / IMPLEMENTED`

앞으로 실제 필요할 수 있는 Item을 보수적으로 보호합니다.

- future Quest 포함
- future Hideout 포함
- unresolved future Quest = `IndeterminatePotential`
- flexible candidate 보호
- cleanup safety를 증명할 수 없으면 정리 가능 처리 금지
- Inventory FIR / 일반 수량 분리

## 10. Items

`CONFIRMED / IMPLEMENTED`

- category / 용도 / 필요 상태 filter
- Inventory + Needed Items 결합
- Quest / Hideout / Ammo cross-navigation
- Item Wiki navigation
- flexible candidate group 표시

## 11. Ammo

`CONFIRMED / IMPLEMENTED`

- read-only comparison
- name / caliber 검색
- exact caliber / Ammo navigation
- raw Ammo stats와 Wiki Ballistics fact 분리
- membership과 Armor Class effectiveness 분리
- 자체 effectiveness heuristic 금지
- caliber favorites

## 12. Map / MiniMap

`CONFIRMED / IMPLEMENTED / WINDOWS USER VALIDATED`

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

- Current Quest sidebar / marker identity
- general marker / PMC·Scav·Transit extracts
- manual floor / hotkeys
- screenshot 기반 Map/player tracking
- floor = presentation relation, visibility filter 아님
- enabled cross-floor marker 유지
- Main Map floor change 시 zoom + map-space center 보존
- MiniMap floor change 시 exact Scale + Translate 보존
- MiniMap click-through

Map은 독립 subsystem이며 Quest만 current JunhyunHelper content/profile과 bridge합니다.

## 13. Scanner / Mini Scanner

`CONFIRMED / IMPLEMENTED / v1.1.1 USABILITY UPDATE / LIVE TARKOV E2E PENDING`

Scanner는 Tarkov 화면을 Item ID로 변환해 기존 JunhyunHelper 데이터에 연결하는 입력 subsystem입니다.

### 실사용 Scanner

```text
스캐너 ON
→ EscapeFromTarkov Borderless client-area
→ detail detector
→ title ROI
→ Windows ko-KR OCR
→ current full-item conservative matcher
→ Item ID
→ existing JunhyunHelper data
→ Mini Scanner
```

capture:

- `EscapeFromTarkov` window handle
- `GetClientRect` + `ClientToScreen`
- target `PrintWindow` 우선
- 유효 frame이 없으면 exact Borderless client screen rectangle fallback

### 테스트 Scanner

```text
테스트 ON
→ 모든 연결 디스플레이 실시간 capture
→ 동일 detector/OCR/matcher/presentation
```

Tarkov 전체 screenshot을 바탕화면/이미지 뷰어에 띄워 게임 없이 확인할 수 있습니다.

real/test는 상호 배타적이며 test는 session-only입니다.

### 식별

```text
screen pixels
→ detail geometry detector
→ stable title ROI
→ Windows ko-KR OCR
→ full current item catalog
→ exact-first conservative match
→ Item ID
```

- low-confidence/ambiguous → no Item ID
- icon identity 금지
- 과거 이름 alias 무제한 누적 금지
- scan-time network 금지

### 표시 데이터

Item ID 뒤에는 기존 JunhyunHelper data flow를 사용합니다.

- official item name
- local cached icon
- trader sell price
- flea average price
- price/slot
- current needed

`current needed`:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

### Scanner 탭 — v1.1.1

상단 bar:

- 왼쪽 `스캐너`, `테스트`
- 오른쪽 `아이템 목록 최신화`

bar 아래:

- 표시 정보 checkboxes
- 최근 인식 기록

상시 설명문을 제거합니다. Foundation preview 개발 도구와 Mini Scanner 별도 위치 편집/초기화 controls도 사용자 화면에 노출하지 않습니다.

최근 인식 기록은 각 실제 OCR/matcher 시도에 대해 OCR 문자열, nearest official Item, 유사도, top1/top2 margin, 성공/보류, 판단 이유를 사용자 문장으로 보여줍니다. 기존 bounded `scanner.log(.1)`에서 최근 판정을 복원해 앱 재실행 뒤에도 볼 수 있습니다.

Foundation Item ID → presentation 내부 API는 개발 진단용으로 유지할 수 있습니다.

### Mini Scanner — v1.1.1

- MiniMap과 독립
- Topmost
- ShowActivated=false / `WS_EX_NOACTIVATE`
- ON 상태에서 standby 또는 Item 결과
- OFF에서 숨김
- 별도 position edit/reset mode 없음
- visible 상태에서 언제든 직접 left-drag
- drag 완료 위치 atomic settings 저장
- negative monitor 좌표 허용

always-drag 요구 때문에 Mini Scanner 자기 영역의 `WS_EX_TRANSPARENT` click-through는 제거합니다. Mini Scanner 영역은 mouse hit-test를 받지만 게임 keyboard focus를 가져가지 않습니다.

### Scanner 금지

- game memory read
- DLL injection
- packet interception
- process-internal game data read
- scan-time HTTP

상세: `docs/SCANNER.md`.

## 14. Scanner 릴리즈 / 검증 정책

v1.1.1 release blocker:

- Windows Release build
- full automated tests
- detector/catalog/matcher regression
- self-contained publish
- ProductVersion/FIRST_RUN identity
- rendered Scanner top bar + `아이템 목록 최신화`
- recent-recognition empty/readable-decision smoke
- removed Foundation/position controls absent
- actual packaged EXE Product UI smoke
- existing Main Map / Factory / MiniMap smoke
- Draft/Public checksum/package verification
- public-downloaded EXE smoke

**최신 Tarkov Borderless live E2E는 사용자 결정에 따라 release blocker가 아닙니다.**

공개 후 `%LocalAppData%/JunhyunHelper/logs/scanner.log`와 최근 인식 기록을 이용해 capture/detector/OCR/input coexistence를 검증하고 필요한 보정을 PATCH로 배포합니다.

## 15. Images / Preference Persistence

Images:

```text
canonical URL → bytes → SkiaSharp decode → validation → PNG cache → WPF
```

개별 image 실패는 nonfatal입니다.

Presentation JSON preferences:

- same-directory temp
- flush-to-disk
- atomic replacement
- last-known-good `.bak`
- corrupt primary → backup recovery
- save failure nonfatal

Scanner 설정도 같은 원칙을 사용합니다.

## 16. UI 검증

공식 제품명: **준현 헬퍼**

실행 파일: **`준현 헬퍼.exe`**

주요 UI 변경은 build 성공만으로 완료 처리하지 않습니다. 실제 published WPF app smoke에서 rendered contract를 검증합니다.

v1.1.1 Scanner smoke에는:

- `스캐너 OFF`
- `테스트 OFF`
- `아이템 목록 최신화`
- 최근 인식 기록 empty state
- 사용자용 OCR/candidate/confidence 문장
- removed developer/position controls 부재

를 포함합니다.

## 17. 현재 버전

현재 public stable:

```text
v1.1.0 — Scanner first public release
```

현재 release candidate:

```text
v1.1.1 — Scanner UI/usability refinement
```

버전 규칙: `docs/VERSIONING.md`.

## 18. 현재 비범위 / fail-closed 범위

- EFT 1.0 Story Chapters: ordinary task source 밖
- PvE Skier LL2 task-pool drift: exact fact 없으면 해당 pool fail-closed
- code signing / installer
- Scanner latest live Tarkov E2E는 공개 후 검증/튜닝 범위
