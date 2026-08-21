# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 **무엇을 만들고 왜 만드는지**를 정의하는 공식 제품 요구사항입니다. 현재 사용자가 명시적으로 확정한 제품 의도가 과거 구현보다 우선합니다.

기준일: 2026-08-21

## 1. 제품 정의

`CONFIRMED`

**준현 헬퍼**는 Escape from Tarkov의 게임 데이터와 사용자 진행 상태를 결합해 Quest, Hideout, Needed Items, Inventory, Ammo, Map/MiniMap 및 Scanner 정보를 제공하는 Windows x64 데스크톱 헬퍼입니다.

핵심 원칙:

- 일반 Game Content 변화는 importer가 이해하는 범위에서 자동 흡수
- 의미/schema가 검증 불가능하게 변하면 fail-closed
- 실패 candidate가 마지막 정상 Game Content를 덮어쓰지 않음
- User Progress와 Game Content 분리
- runtime GPT/AI 의존성 없음
- 현재 코드가 존재한다는 이유만으로 제품 요구사항으로 추정하지 않음

## 2. 플랫폼 / 배포

`CONFIRMED / IMPLEMENTED`

- Windows x64
- .NET 10 WPF
- self-contained single-file executable
- portable ZIP
- 별도 .NET Runtime 설치 불필요
- 관리자 권한 불필요
- installer 없음
- 현재 code signing 없음

공개 ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

사용자 데이터 root:

```text
%LocalAppData%/JunhyunHelper
```

## 3. Game Content Update

`CONFIRMED / IMPLEMENTED`

```text
online source
→ external-format / required-semantics validation
→ canonical model
→ candidate DB
→ relation/read-back validation
→ active replacement
→ image prefetch
→ User Progress 결합
```

- failed candidate는 폐기하고 기존 active content 유지
- `user.db`를 삭제/덮어쓰지 않음
- 파생 결과를 별도 권위 데이터로 저장하지 않음
- 개별 image 실패는 update 전체 실패가 아님

```text
Content schema: v7
Readable: v3, v4, v5, v6, v7
```

## 4. Program Update

`CONFIRMED / IMPLEMENTED / PUBLIC VERIFIED`

일반 실행 시 latest public stable GitHub Release를 확인합니다.

- 현재 버전보다 strictly newer stable `vMAJOR.MINOR.PATCH`만 대상
- 새 버전이 있으면 사용자 Yes/No 동의
- Yes 후 exact Windows ZIP + `SHA256SUMS.txt` 다운로드
- SHA-256 / archive security / package root 검증
- 검증 전 기존 program files 변경 금지
- temporary self-copy updater로 program-owned files transaction 교체
- 성공 시 새 버전 자동 재실행
- 실패 시 rollback / 기존 프로그램 재실행 시도
- 사용자 데이터는 업데이트 대상이 아님

상세: `docs/PROGRAM_UPDATE.md`

## 5. User Progress / Profile

`CONFIRMED / IMPLEMENTED`

GameMode별 독립 profile을 사용합니다.

지원 mode:

- regular
- pve
- pvp-season

저장 사실:

- level / faction / edition / prestige
- trader LL / standing facts
- completed Quest
- explicit permanent failed Quest
- exact observed `ProfileVariables`
- recoverable special-trader access
- Hideout levels
- FIR / non-FIR Inventory
- Quest / Hideout consumption ledger

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
- source보다 강한 prerequisite를 임의 생성하지 않음
- 증명할 수 없는 availability = `확인 필요`
- exact profile-variable current 값이 있으면 권위값
- 제한된 audited compatibility가 drift하면 fail-closed

BTR Driver / Ref / Lightkeeper 특수 상인 규칙은 전문 문서와 canonical 구현을 따릅니다.

## 7. Quest Item / Consumption

`CONFIRMED / IMPLEMENTED`

- mandatory fixed submit material은 completion과 함께 ledger 기반 자동 소비 가능
- flexible hand-in은 후보 group으로 유지
- 실제 소비 후보 자동 추정 금지
- rollback 시 consumed ledger로 복구/중복 소비 방지
- malformed empty candidate / non-positive requirement는 active DB 적용 전 차단

## 8. Hideout

`CONFIRMED / IMPLEMENTED`

- station current level 저장
- 미래 upgrade requirement 포함
- fixed material 소비/rollback ledger
- 미입력 station은 Lv.0 의미

## 9. Needed Items / Inventory

`CONFIRMED / IMPLEMENTED`

목표는 앞으로 실제 필요할 수 있는 Item을 보수적으로 보호하는 것입니다.

- future Quest 포함
- future Hideout 포함
- unresolved future Quest = `IndeterminatePotential` 보호
- flexible candidate 보호
- cleanup safety를 증명할 수 없으면 낙관적으로 정리 가능 처리 금지
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

- current Quest sidebar / A·B·C·D marker identity
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

`CONFIRMED / IMPLEMENTED / PUBLIC VERIFIED v1.1.0 / LIVE TARKOV E2E PENDING`

Scanner는 Tarkov 화면을 Item ID로 변환해 기존 JunhyunHelper 데이터에 연결하는 입력 subsystem입니다.

### 실사용 Scanner

사용자 동작:

```text
스캐너 OFF
→ 버튼 클릭
→ 스캐너 ON
→ Mini Scanner 즉시 표시
→ EscapeFromTarkov Borderless 게임 창 탐색
→ 상세창 자동 감지
→ 제목 OCR
→ Item ID 확정
→ Mini Scanner 정보 표시
```

실사용 capture:

- `EscapeFromTarkov` window handle
- `GetClientRect` + `ClientToScreen`
- target `PrintWindow` 우선
- 필요 시 exact Borderless client screen rectangle fallback

### 테스트 Scanner

사용자 동작:

```text
테스트 OFF
→ 버튼 클릭
→ 테스트 ON
→ 모든 연결 디스플레이 실시간 감지
```

실사용과 동일 detector/OCR/matcher/presentation pipeline을 사용합니다. Tarkov 전체 screenshot을 바탕화면/이미지 뷰어에 띄워 게임 없이 확인할 수 있습니다.

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
- icon 기반 identity 금지
- 과거 이름 alias 누적 금지
- scan-time network 금지

### 표시

Item ID 뒤에는 기존 JunhyunHelper data flow를 사용합니다.

- official item name
- local cached icon
- trader sell price
- flea average price
- price/slot
- current needed

`current needed` 의미:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

### Mini Scanner

- MiniMap과 독립
- Topmost
- click-through / no-activate in play mode
- position edit mode only drag
- ON 상태에서는 standby 또는 item 결과를 표시
- OFF에서 숨김

### 금지

- game memory read
- DLL injection
- packet interception
- process-internal game data read
- scan-time HTTP

상세: `docs/SCANNER.md`

## 14. Scanner 릴리즈/검증 정책

v1.1.0에서 완료된 공개 차단 조건:

- Windows Release build
- 243 automated tests
- detector/catalog/matcher/persistence regression
- self-contained publish
- actual packaged EXE/rendered UI smoke
- Scanner real/test safe-default controls
- existing Main Map / Factory / MiniMap smoke
- Draft asset checksum/package/ProductVersion/FIRST_RUN verification
- Draft-downloaded EXE smoke
- public asset re-download hash/size/ProductVersion verification
- public downloaded EXE smoke

**최신 Tarkov Borderless live E2E는 사용자 결정에 따라 v1.1.0 공개 차단 조건이 아니며 현재 PENDING입니다.**

공개 후 `%LocalAppData%/JunhyunHelper/logs/scanner.log`를 이용해 실제 capture/detector/OCR을 함께 검증하고 필요한 보정을 PATCH로 배포합니다.

현재 정확한 상태 표기:

```text
IMPLEMENTED
WINDOWS RELEASE/PACKAGE VERIFIED
OFFLINE SCREENSHOT/OCR EXPERIMENTS VERIFIED
LATEST LIVE TARKOV E2E PENDING
```

## 15. Images / Preference Persistence

Images:

```text
canonical URL → bytes → SkiaSharp decode → validation → PNG cache → WPF
```

개별 image 실패는 nonfatal입니다.

Presentation JSON preferences는:

- same-directory temp write
- flush-to-disk
- atomic replacement
- last-known-good `.bak`
- corrupt primary → backup recovery
- save failure nonfatal

Scanner 설정도 동일한 안전 원칙을 사용합니다.

## 16. UI / 제품 검증

공식 제품명: **준현 헬퍼**

실행 파일: **`준현 헬퍼.exe`**

주요 UI 변경은 source/build 성공만으로 완료 처리하지 않습니다. 실제 published WPF app smoke에서 rendered contract를 검증합니다.

v1.1.0 smoke에는 기존 Items/Ammo/Map gate에 더해 Scanner의 `스캐너 OFF` / `테스트 OFF` safe-default controls가 포함되며 public-downloaded EXE에서도 재검증했습니다.

## 17. 현재 버전

현재 공개 stable:

```text
v1.1.0 PUBLIC RELEASE / VERIFIED
release id: 374188781
exact release source / target SHA: ac24f7717e81cf6fa32cb2e0ade63949ed87ade5
asset: Junhyun-Helper-v1.1.0-win-x64.zip
bytes: 80,235,043
SHA-256: 8e7f452701f866c84e753c1c34951af64f4415947e9f56c56634e2b584d9e1ce
ProductVersion: 1.1.0+ac24f7717e81cf6fa32cb2e0ade63949ed87ade5
public downloaded EXE smoke: SUCCESS
```

버전 규칙 상세: `docs/VERSIONING.md`, 공개 검증 상세: `docs/RELEASE_1.1.0.md`.

## 18. 현재 비범위 / fail-closed 범위

- EFT 1.0 Story Chapters: ordinary task source 밖
- PvE Skier LL2 task-pool drift: exact fact 없으면 해당 pool fail-closed
- code signing / installer
- Scanner live Tarkov E2E는 공개 v1.1.0에서 로그 기반 검증/튜닝 범위
