# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 **무엇을 만들고 왜 만드는지**를 정의하는 공식 제품 요구사항이다. 사용자의 최신 확정 의도가 과거 구현보다 우선하며, 현재 코드가 존재한다는 이유만으로 그 동작을 제품 요구사항으로 추정하지 않는다.

기준일: 2026-08-24
상태: **v1.5.0 PUBLIC RELEASE / VERIFIED**

## 1. 제품 정의

**준현 헬퍼**는 Escape from Tarkov의 공식 게임 데이터와 사용자 진행 상태를 결합해 Quest, Hideout, Needed Items, Inventory, Items, Ammo, Map/MiniMap, Scanner/Mini Scanner를 제공하는 Windows x64 데스크톱 헬퍼다.

제품 목표:

- 플레이 중 필요한 진행/아이템 정보를 빠르게 확인
- 사용자가 이미 알고 있는 진행 상태를 정확히 저장
- 공식 Tarkov 데이터가 바뀌어도 검증 가능한 범위에서 안전하게 갱신
- 알 수 없는 상태를 추측하지 않고 명시적으로 fail closed
- 게임 프로세스를 변조하거나 내부 데이터를 읽지 않는 외부 보조 프로그램 유지
- 일상 사용 UI와 개발/진단 UI를 구분
- 장시간 실행해도 사용자 데이터와 디스크 사용량을 안정적으로 관리

핵심 원칙:

- User Progress와 Game Content 분리
- 일반 Game Content 변화는 importer가 이해하는 범위에서 자동 흡수
- 의미/schema가 검증 불가능하게 변하면 fail closed
- 실패 candidate가 last-known-good content를 덮어쓰지 않음
- authoritative fact와 derived presentation을 구분
- runtime GPT/AI 의존성 없음
- 기존 `Propeex/Tarkov-Helper`는 공식 요구사항의 권위가 아님

## 2. 플랫폼 / 배포

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

Mutable 사용자 데이터는 program package 옆이 아니라 `%LocalAppData%/JunhyunHelper`에 저장한다.

현재 공개 기준선:

```text
v1.5.0 PUBLIC RELEASE / VERIFIED
exact source/tag: 6de738959740d12e6ccb81b65e50006e463eb699
296 tests / 0 failed / 0 skipped
release run: 32691423654 — SUCCESS
public verifier: 32691641614 — SUCCESS
```

상세: `docs/RELEASE_1.5.0.md`, `docs/.release-v1.5.0-status.json`.

## 3. Game Content

Game Content는 remote Tarkov source를 JunhyunHelper canonical model로 변환한 읽기 기준선이다.

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

원칙:

- failed candidate 폐기
- 기존 healthy active content 유지
- `user.db` 삭제/덮어쓰기 금지
- derived result를 별도 authoritative fact처럼 저장하지 않음
- 개별 image 실패는 update 전체 fatal이 아님

현재 content compatibility:

```text
Content schema: v7
Readable: v3, v4, v5, v6, v7
```

## 4. Game Data Update

사용자가 상단 데이터 업데이트를 실행하면 일반 Game Content와 현재 GameMode의 Scanner item/market catalog를 하나의 제품 흐름에서 갱신한다.

```text
remote Game Content
→ validate/build/activate general content
→ Scanner full-item + market catalog refresh
→ combined result/status
```

Scanner refresh만 실패하면 건강한 일반 Game Content를 rollback하지 않는다. 기존 healthy Scanner cache가 있으면 유지한다.

Scanner 화면의 별도 `아이템 목록 최신화`는 일반 사용 절차가 아니라 고급/복구 기능이다.

## 5. Program Update

일반 실행 시 `Propeex/JunhyunHelper` latest public stable GitHub Release를 확인한다.

- current보다 strictly newer stable `vMAJOR.MINOR.PATCH`만 대상
- 사용자 동의형
- exact Windows ZIP + `SHA256SUMS.txt`
- checksum/archive/package-root 검증 전 현재 파일 변경 금지
- temporary self-copy updater로 program-owned files transaction 교체
- 실패 시 rollback/기존 실행 복구 시도
- 사용자 데이터는 update 대상 아님
- 정식 release는 exact-source build/test/publish/smoke와 public re-download verification을 통과해야 함

## 6. User Progress / Profile

GameMode별 독립 profile을 사용한다.

- regular
- pve
- pvp-season

저장 사실:

- level / faction / edition / prestige
- trader LL / standing
- completed Quest / explicit permanent failed Quest
- exact observed ProfileVariables
- recoverable special-trader access
- Hideout levels
- FIR / non-FIR Inventory
- Quest / Hideout consumption ledgers

`user.db` SQLite schema는 v1이다.

## 7. Quest

사용자 상태:

- 진행 중
- 확인 필요
- 잠김
- 사용 불가
- 완료

Availability 원칙:

- 서로 다른 `taskRequirements` = AND
- 한 requirement의 `status[]` = OR
- 받을 수 있는 Quest는 Helper에서 이미 수락한 것으로 간주
- source보다 강한 prerequisite 임의 생성 금지
- 증명할 수 없는 availability = `확인 필요`
- exact ProfileVariable fact가 있으면 권위값으로 사용
- audited compatibility는 구조가 정확히 맞을 때만 사용
- source drift 또는 미지원 requirement는 fail closed

2026-08-24에는 `regular`, `pve`, `pvp-season` 최신 task-pool live data를 감사했다. GameMode와 audited structure가 맞을 때만 task-pool synthetic compatibility를 허용한다.

상세: `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md`.

## 8. Quest Item / Consumption

- mandatory fixed submit material은 Quest completion과 함께 ledger 기반 자동 소비 가능
- flexible hand-in은 후보 group으로 유지
- 실제 소비 후보 자동 추정 금지
- rollback은 consumed ledger로 복구하고 중복 소비 방지
- malformed empty candidate / non-positive requirement는 active content 적용 전에 차단

## 9. Hideout

- station current level 저장
- 미래 upgrade requirement 포함
- fixed material 소비/rollback ledger
- 미입력 station은 Lv.0
- Needed Items 계산과 연결

## 10. Needed Items / Inventory

앞으로 실제 필요할 수 있는 Item을 보수적으로 보호한다.

- future Quest 포함
- future Hideout 포함
- unresolved future Quest = `IndeterminatePotential`
- flexible candidate 보호
- cleanup safety를 증명할 수 없으면 정리 가능 처리 금지
- Inventory FIR / non-FIR 수량 분리

Scanner의 `필요 개수`는 Inventory 차감 shortage가 아니라 `NeededItems[itemId].RequiredTotal`이다.

## 11. Items

- category / 용도 / 필요 상태 filter
- Inventory + Needed Items 결합
- Quest / Hideout / Ammo cross-navigation
- Item Wiki navigation
- flexible candidate group 표시
- 현재 content/profile를 읽어 presentation을 구성

## 12. Ammo

- read-only comparison
- name / caliber 검색
- exact caliber / Ammo navigation
- raw Ammo stats와 Wiki Ballistics fact 분리
- membership과 Armor Class effectiveness 분리
- 자체 effectiveness heuristic 금지
- caliber favorites

## 13. Map / MiniMap

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

제품 계약:

- Current Quest sidebar / marker identity
- general marker / PMC·Scav·Transit extracts
- manual floor / hotkeys
- screenshot 기반 Map/player tracking
- floor = presentation relation, visibility filter 아님
- enabled cross-floor marker 유지
- Main Map floor change 시 zoom + map-space center 보존
- MiniMap floor change 시 exact Scale + Translate 보존
- MiniMap click-through

Map은 독립 subsystem이고 Quest만 current JunhyunHelper content/profile과 bridge한다.

검증된 Map/MiniMap subsystem은 기능 요구 없이 불필요하게 재설계하지 않는다.

## 14. Scanner / Mini Scanner

Scanner는 Tarkov 화면 픽셀을 Item ID로 변환해 기존 JunhyunHelper data에 연결하는 입력 subsystem이다.

상세 technical contract는 `docs/SCANNER.md`가 권위 문서다.

### 14.1 Recognition pipeline

```text
Tarkov window pixels
→ capture
→ detail rectangle proposals
→ red close-X + magnifier + neutral inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ Windows ko-KR OCR
→ optional user OCR substitution
→ current-catalog sanitation / normalization
→ conservative official-catalog matching / bounded recovery
→ optional Tarkov-font visual corroboration/recovery
→ Item ID or fail closed
→ local mapped presentation
→ Mini Scanner
```

### 14.2 Scanner 안전 계약

- false positive보다 miss 선호
- geometry/structural score는 proposal evidence이며 Item identity가 아님
- `HEADER_FRAME_LOCKED >= 0.68`
- valid magnifier + red close-X 필수
- structural floor `0.34`
- continuous max 8 candidates
- one-shot max 12 candidates
- current official Korean full-item catalog를 identity authority로 사용
- exact-first conservative matcher
- fuzzy confidence/top1-top2 margin을 evidence 없이 완화하지 않음
- ambiguity / low confidence → no Item ID
- scan-time network 금지
- icon identity 금지
- game memory read / DLL injection / packet interception 금지
- production OCR field는 item-name 하나
- 자동 product-wide forced OCR substitution table 금지

### 14.3 Capture / one-shot

Continuous real Scanner는 EscapeFromTarkov Borderless client-area를 감지한다.

`PrintWindow`를 우선 사용하고 invalid/empty이면 exact client screen rectangle으로 fallback한다.

Display Test는 연결된 display에 동일 recognition pipeline을 적용하며 actual Scanner continuous mode와 상호 배타적이다.

일반 UI의 `1회 스캔`은 continuous state를 영구 변경하지 않고 TarkovWindow를 한 번 정밀 분석한다.

기본 global hotkeys:

```text
1회 인게임: Ctrl+Shift+F10
1회 테스트: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

### 14.4 Full Item catalog

Scanner identity catalog는 Needed Items subset이 아니라 현재 GameMode의 공식 전체 Item catalog다.

준비/업데이트 단계에서 remote source를 사용하되 실제 scan 중에는 local/memory data만 사용한다.

Identity health와 market coverage를 분리한다.

### 14.5 User OCR substitution — schema v5

사용자는 자신의 환경에서 반복 확인한 OCR 오류에 exact 문자열 치환을 등록할 수 있다.

```text
raw OCR
→ enabled user substitutions (single pass)
→ catalog sanitation / normalization
→ matching
```

- 기본 list 비어 있음
- add/delete/ON·OFF/reset
- raw OCR forensic evidence 보존
- raw / substituted / normalized / matched 결과 구분
- recursive/cyclic reprocessing 없음
- user rule은 product-wide automatic substitution table이 아님

### 14.6 Mapped presentation

Item ID 확정 뒤 local trusted data에서:

- official item name
- local cached icon
- 최고 non-flea trader RUB-equivalent sell price
- 최고가 상인명
- flea positive `avg24hPrice`
- positive `width × height` slots
- trader/flea price per slot
- `NeededItems[itemId].RequiredTotal`

을 연결한다.

Market/dimension 일부 오류는 해당 field만 fail closed하고 healthy Item ID를 버리지 않는다.

### 14.7 Ground Truth / correction

교정 기본 UX는 detector candidate 선택이다.

1. detail rectangle
2. close-X
3. magnifier
4. item-name ROI
5. correct item/text
6. save

후보에 정답이 없으면 manual rectangle을 사용할 수 있고, detector가 semantic object를 생성하지 못했다면 `없음`을 기록할 수 있다.

Candidate ID/rank/score/geometry를 reviewed Ground Truth와 함께 저장한다.

자동 diagnostic Case는 정답이 아니다. 사용자-reviewed Case만 Ground Truth다.

### 14.8 Performance / stability

Stage telemetry:

- capture
- rectangle proposal
- semantic header
- OCR normal/deep
- visual recovery
- catalog matching
- presentation
- end-to-end

같은 active scan cycle에서 pixel까지 완전히 동일한 OCR bitmap만 결과 재사용을 허용한다. Frame 간 OCR cache는 없다.

이미 semantic validation을 통과한 같은 detail의 title-ink shape continuity가 유지되면 harmless background pixel variation 때문에 결과가 즉시 깜빡이지 않게 한다. 이 continuity signature는 Item identity proof가 아니다.

### 14.9 Retention

사용자-reviewed Ground Truth는 자동 삭제하지 않는다.

Automatic unreviewed diagnostic Case만:

- 최대 30일
- 최대 300건
- 최대 512 MiB
- 최근 2시간 보호

정책으로 관리한다. Unknown/corrupt metadata는 fail closed하여 보존한다. Scanner/startup log도 bounded rotation한다.

### 14.10 Scanner UI

일반 surface:

- Scanner ON/OFF
- `1회 스캔`
- `현재 결과 교정`
- runtime status
- recent recognition history

`설정`:

- global hotkeys
- OCR substitutions
- Mini Scanner 표시 설정

`고급 / 진단`:

- Display Test
- 인식 이미지
- regression
- Ground Truth export/manage
- Scanner catalog 복구/강제 최신화
- 로그 삭제
- diagnostic storage 정보

Mini Scanner는 우클릭 `현재 결과 교정`으로 최신 recognition Case를 바로 correction flow에 전달한다.

## 15. Images / Preference Persistence

Images:

```text
canonical URL → bytes → SkiaSharp decode → validation → PNG cache → WPF
```

개별 image 실패는 nonfatal이다.

Presentation JSON preferences는 same-directory temp + flush-to-disk + atomic replacement + last-known-good `.bak` recovery를 사용한다. Scanner 설정도 같은 원칙을 따른다.

## 16. UI 제품 원칙

공식 제품명: **준현 헬퍼**

실행 파일: **`준현 헬퍼.exe`**

- 일상 사용자가 개발/진단 개념을 몰라도 핵심 기능 사용 가능
- 기능 삭제보다 surface hierarchy를 정리
- 실제 clipping/scroll/status hierarchy 문제를 우선 수정
- 새 기능을 이유 없이 추가하지 않음
- 검증된 subsystem을 요구사항 없이 재설계하지 않음

v1.5.0 whole-product UI audit는 Main / Quest / Hideout / Items / Ammo / Map / Scanner / 주요 settings/dialog를 점검했다.

실제 header/Items 2-pane 구조와 맞지 않던 MainWindow MinWidth 900은 1180으로 교정했다.

주요 UI 변경은 build 성공만으로 완료 처리하지 않는다. Published WPF app의 rendered Product UI/Map/Scanner smoke를 release gate에 포함한다.

## 17. 사용자 데이터 보호

Program Update와 content update는 다음 mutable 사용자 데이터를 덮어쓰지 않는다.

```text
%LocalAppData%/JunhyunHelper/user.db
%LocalAppData%/JunhyunHelper/content/
%LocalAppData%/JunhyunHelper/image-cache/
Map/Ammo/Scanner preferences
Scanner logs
Scanner diagnostics / reviewed Ground Truth
```

Scanner 일반 로그 삭제와 Ground Truth dataset 삭제는 독립 동작이다.

## 18. Release 품질 게이트

정식 public release는 다음을 통과해야 한다.

- exact source identity
- Release build
- full automated tests
- Windows x64 self-contained single-file publish
- dependency/package-root audit
- exact ProductVersion/FIRST_RUN
- rendered Product UI smoke
- Scanner/Mini Scanner smoke
- Main Map/Factory/MiniMap smoke
- graceful shutdown / clean portable root
- exact source tag
- draft asset re-download + checksum/package/EXE smoke
- public stable/latest publication
- fresh independent public asset re-download
- public SHA256SUMS/hash/size/layout/ProductVersion/FIRST_RUN verification
- public-downloaded EXE smoke
- durable release status record
- one-shot release/verifier workflow cleanup

v1.5.0은 이 gate를 모두 통과했다.

## 19. 현재 비범위 / fail-closed 범위

- EFT 1.0 Story Chapters 등 ordinary task source 밖 데이터를 임의 추측하지 않음
- 최신 Quest source에 새 requirement가 생겨 evaluator가 증명할 수 없으면 `확인 필요`
- code signing / installer
- game memory / injection / packet interception 기반 Scanner
- automatic product-wide OCR forced-substitution table
- evidence 없는 Scanner threshold/candidate-cap 완화

## 20. 현재 개발 방향

v1.5.0은 공식 제품 기준선이다.

추가 개발은 기능을 무분별하게 늘리기보다 실제 사용자 문제와 reviewed Ground Truth를 근거로 진행한다.

Scanner 문제는 다음 stage로 분리한다.

```text
capture
→ proposal
→ semantic header
→ title ROI
→ raw OCR
→ user substitution
→ catalog matching / visual recovery
→ Item ID
→ mapped presentation
→ overlay
```

수정 후에는 전체 reviewed dataset replay에서 기존 정상 Case의 `REGRESSION=0`을 우선한다.
