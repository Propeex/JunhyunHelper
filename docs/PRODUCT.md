# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 **무엇을 만들고 왜 만드는지**를 정의하는 공식 제품 요구사항이다. 사용자의 최신 확정 의도가 과거 구현보다 우선하며, 현재 코드가 존재한다는 이유만으로 그 동작을 제품 요구사항으로 추정하지 않는다.

기준일: 2026-08-25
상태: **v1.7.0 PUBLIC RELEASE / VERIFIED**

## 1. 제품 정의

**준현 헬퍼**는 Escape from Tarkov의 공식 게임 데이터와 사용자 진행 상태를 결합해 Quest, Hideout, Needed Items, Inventory, Items, Ammo, Map/MiniMap, Scanner/Mini Scanner를 제공하는 Windows x64 데스크톱 헬퍼다.

제품 목표:

- 플레이 중 필요한 진행/아이템 정보를 빠르게 확인
- 사용자가 이미 알고 있는 진행 상태를 정확히 저장
- 공식 Tarkov 데이터가 바뀌어도 검증 가능한 범위에서 안전하게 갱신
- 알 수 없는 상태를 추측하지 않고 fail closed
- 게임 프로세스를 변조하거나 내부 데이터를 읽지 않는 외부 보조 프로그램 유지
- 일상 사용 UI와 개발/진단 UI를 구분
- 장시간 실행해도 사용자 데이터와 디스크 사용량을 안정적으로 관리
- Scanner 실패를 실제 reviewed Ground Truth로 재현·교정할 수 있게 함

핵심 원칙:

- User Progress와 Game Content 분리
- 일반 Game Content 변화는 importer가 이해하는 범위에서 자동 흡수
- 의미/schema가 검증 불가능하게 변하면 fail closed
- failed candidate가 last-known-good content를 덮어쓰지 않음
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

v1.6.0부터 user-facing package contract:

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/
```

ZIP/folder 이름은 version과 분리한다. GitHub Release asset은 filename normalization 제약 때문에 `Junhyun-Helper.zip` ASCII 이름을 사용하고, 압축 내부 제품 폴더는 `준현 헬퍼/`를 유지한다. Version identity는 EXE ProductVersion, Git tag, GitHub Release metadata에 둔다.

Mutable user data는 `%LocalAppData%/JunhyunHelper`에 저장한다.

현재 public stable/latest는 v1.7.0이다. 제품 source의 권위는 tag `v1.7.0`의 exact SHA `56e12342e3490fd0defa5f327a03d20d4f32b3a6`이며, main의 후속 문서/housekeeping commit은 release source가 아니다.

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

상단 데이터 업데이트가 일반 Game Content와 current GameMode Scanner item/market catalog를 하나의 제품 흐름에서 갱신한다.

```text
remote Game Content
→ validate/build/activate general content
→ Scanner full-item + market catalog refresh
→ combined result/status
```

Scanner refresh만 실패하면 healthy general Game Content를 rollback하지 않는다. 기존 healthy Scanner cache가 있으면 유지한다.

v1.6.0 일반 Scanner surface에는 별도 catalog force-refresh를 필수 사용자 작업으로 노출하지 않는다.

## 5. Program Update

일반 실행 시 `Propeex/JunhyunHelper` latest public stable GitHub Release를 확인한다.

- current보다 strictly newer stable `vMAJOR.MINOR.PATCH`만 대상
- 사용자 동의형
- exact user-facing release asset + checksum 검증
- archive/package-root 검증 전 현재 파일 변경 금지
- program-owned files만 transaction 교체
- 실패 시 rollback/기존 실행 복구 시도
- LocalAppData user data는 update 대상 아님
- 정식 release는 exact-source build/test/publish/smoke + public redownload verification을 통과해야 함

## 6. User Progress / Profile

GameMode별 독립 profile:

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

`user.db` schema는 v1이다.

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
- source drift / unsupported requirement는 fail closed

2026-08-24 live audit 대상: `regular`, `pve`, `pvp-season`.

상세: `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md`.

## 8. Quest Item / Consumption

- mandatory fixed submit material은 Quest completion과 함께 ledger 기반 자동 소비 가능
- flexible hand-in은 candidate group으로 유지
- 실제 소비 candidate 자동 추정 금지
- rollback은 consumed ledger로 복구하고 중복 소비 방지
- malformed empty candidate / non-positive requirement는 active content 전에 차단

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
- Inventory FIR / non-FIR 분리

Scanner `필요 개수`는 Inventory 차감 shortage가 아니라 `NeededItems[itemId].RequiredTotal`이다.

## 11. Items

- category / 용도 / 필요 상태 filter
- Inventory + Needed Items 결합
- Quest / Hideout / Ammo cross-navigation
- Item Wiki navigation
- flexible candidate group 표시
- current content/profile 기반 presentation

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

Map은 독립 subsystem이고 Quest만 current JunhyunHelper content/profile과 bridge한다. 검증된 Map/MiniMap은 기능 요구 없이 불필요하게 재설계하지 않는다.

## 14. Scanner / Mini Scanner

Scanner는 Tarkov 화면 픽셀을 Item ID로 변환해 기존 JunhyunHelper data에 연결하는 입력 subsystem이다.

Canonical technical contract: `docs/SCANNER.md`.

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
→ Scanner Page / Mini Scanner
→ optional correction / Ground Truth
```

### 14.2 Scanner safety contract

- false positive보다 miss 선호
- geometry/structural score는 proposal evidence이며 Item identity가 아님
- `HEADER_FRAME_LOCKED >= 0.68`
- valid magnifier + red close-X 필수
- structural floor `0.34`
- continuous max 8 candidates
- one-shot max 12 candidates
- current official Korean full-item catalog가 identity authority
- exact-first conservative matcher
- generic confidence/top1-top2 margin을 evidence 없이 완화하지 않음
- ambiguity / low confidence → no Item ID
- scan-time network 금지
- icon-only identity 금지
- game memory read / DLL injection / packet interception 금지
- production OCR field는 item-name 하나
- automatic product-wide forced OCR substitution table 금지
- cross-frame OCR cache 금지

### 14.3 Capture / one-shot

Continuous real Scanner는 EscapeFromTarkov Borderless client-area를 감지한다. `PrintWindow` 우선, invalid/empty이면 exact client screen rectangle fallback.

Display Test는 동일 recognition pipeline을 적용하며 real continuous mode와 상호 배타적이다.

One-shot 기능은 유지하지만 v1.6.0 일반 Scanner page에는 별도 one-shot 버튼을 두지 않는다.

```text
1회 인게임: Ctrl+Shift+F10
1회 테스트: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

### 14.4 Full Item catalog / item search

Scanner identity catalog는 Needed subset이 아니라 current GameMode 공식 전체 Item catalog다.

실제 scan/search 중에는 local/memory data만 사용한다.

v1.6.0 item search result:

- cached icon
- official name

선택 presentation:

- icon/name
- Tarkov Wiki
- flea 24h average
- best non-flea trader price + trader name where trusted
- `NeededItems[itemId].RequiredTotal`

### 14.5 Scanner display settings schema v6

v6 fixed Mini Scanner identity header:

- item icon
- official item name

사용자가 표시 여부와 순서를 지정:

- trader sell price
- flea average
- trader price/slot
- flea price/slot
- current needed

v5 이하 설정은 자동 migration하고 enabled state/hotkeys/visibility/position/font size/user OCR substitutions를 가능한 한 보존한다.

### 14.6 User OCR substitutions

User-owned exact substitution engine은 유지한다.

```text
raw OCR
→ enabled user substitutions (single ordered pass)
→ catalog sanitation / normalization
→ matching
```

- default empty
- raw OCR evidence 보존
- recursive/cyclic reprocessing 금지
- user rule은 product-wide automatic table이 아님

### 14.7 Mapped presentation

Item ID 확정 뒤 local trusted data:

- official item name
- local cached icon
- highest non-flea trader RUB-equivalent sell price
- best trader name
- flea positive `avg24hPrice`
- positive `width × height` slots
- trader/flea price per slot
- `NeededItems[itemId].RequiredTotal`

Market/dimension 일부 오류는 affected field만 fail closed하고 healthy Item ID를 버리지 않는다.

### 14.8 Ground Truth / correction — v1.6.0

교정 image는 viewport에 auto-fit하되 saved ROI는 original pixel coordinate를 사용한다.

Candidate-first fields:

1. detail rectangle
2. close-X
3. magnifier
4. item-name ROI
5. correct item/text

기본 선택 UX는 image 위 candidate box 직접 클릭이다.

- candidate가 정답을 포함하지 않음 → manual rectangle
- 실제 semantic object 없음 → explicit `없음`

Saved Case는 correction dataset manager에서 다시 열 수 있다. `case.json`, `full.png`, `candidate_selection.json`을 복원해 same Case ID로 reviewed Ground Truth를 수정한다.

Automatic diagnostic Case는 정답이 아니다. User-reviewed Case만 Ground Truth다.

### 14.9 Performance / stability / retention

Stage telemetry:

- capture
- rectangle proposal
- semantic header
- OCR normal/deep
- visual recovery
- catalog matching
- presentation
- end-to-end

Same active scan cycle의 exact-identical OCR bitmap만 reuse. Cross-frame OCR cache 없음.

Title continuity signature는 trusted detail continuity evidence이지 Item identity proof가 아니다.

Reviewed Ground Truth는 자동 삭제하지 않는다.

Automatic unreviewed diagnostic only:

- max 30 days
- max 300 cases
- max 512 MiB
- recent 2h protection

Unknown/corrupt metadata는 preserve fail closed. Logs는 bounded rotation.

### 14.10 Scanner UI — v1.6.0

일반 surface:

- Scanner ON/OFF
- 설정
- 고급
- item search
- recognition log

`설정`은 global hotkey + Mini Scanner display/order를 우선한다.

`고급`은 Display Test + current result correction + correction data management를 우선한다.

일반 surface에 catalog recovery/regression/export/log-delete 같은 developer action을 펼쳐 놓지 않는다.

## 15. Images / preference persistence

Image cache는 remote bytes를 검증·정규화한 뒤 LocalAppData에 저장한다. Scanner scan/search path는 local cached icon만 사용한다.

Map/Ammo/Scanner preference는 사용자 mutable data이며 Program Update가 덮어쓰지 않는다.

## 16. UI 품질

- MainWindow minimum width는 실제 2-pane/header 요구를 만족하는 1180
- normal vs settings vs advanced hierarchy를 명확히 유지
- clipping/scroll/status wording 회귀를 Product UI smoke로 검출
- 검증된 Map/MiniMap을 제품 요구 없이 재설계하지 않음

## 17. Release quality gate

v1.6.0 release candidate는 최소 다음을 통과해야 한다.

- Desktop Release build
- full automated tests
- Windows x64 self-contained single-file publish
- ProductVersion / FIRST_RUN identity audit
- Product UI / Scanner / Mini Scanner smoke
- Main Map / Factory / MiniMap smoke
- graceful shutdown
- clean portable root
- `Junhyun-Helper.zip` 생성
- archive top-level `준현 헬퍼/` + required file verification
- exact public tag/source
- public stable/latest publication
- independent anonymous public redownload/hash/layout verification
- public-downloaded EXE Product UI/Map/Scanner smoke

## 18. 현재 개발 방향

v1.6.0 공개 검증 후에는 새 Scanner 기능을 계속 추가하는 것이 기본 방향이 아니다.

```text
real Tarkov usage
→ reviewed Ground Truth accumulation
→ failure-stage classification
→ affected stage only modification
→ full reviewed replay
→ REGRESSION=0
→ PATCH 판단
```

Ground Truth evidence 없이 threshold/candidate cap을 완화하지 않는다.
