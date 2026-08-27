# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 **무엇을 만들고 왜 만드는지**를 정의하는 공식 제품 요구사항이다. 사용자의 최신 확정 의도가 과거 구현보다 우선하며, 현재 코드가 존재한다는 이유만으로 그 동작을 제품 요구사항으로 추정하지 않는다.

기준일: 2026-08-27
상태: **v1.7.13 PUBLIC STABLE / VERIFIED / PRODUCT COMPLETE / MAINTENANCE MODE**

## 1. 제품 정의

**준현 헬퍼**는 Escape from Tarkov의 공식 게임 데이터와 사용자 진행 상태를 결합해 Quest, Hideout, Needed Items, Inventory, Items, Ammo, Map/MiniMap, Scanner/Mini Scanner를 제공하는 Windows x64 데스크톱 헬퍼다.

제품 목표:

- 플레이 중 필요한 진행/아이템 정보를 빠르게 확인
- 사용자가 이미 알고 있는 진행 상태를 정확하게 저장
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

현재 public stable/latest는 v1.7.13이다. 제품 source의 권위는 tag `v1.7.13`의 exact SHA `16198c462a6be58d77dbe2dc27aa57eabfc7b9fd`이며, main의 후속 documentation/housekeeping commit은 release source가 아니다. 정확한 현재 CI/release/asset proof는 `docs/STATE.md`와 `docs/RELEASE_1.7.13.md`를 사용한다.

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

일반 Scanner surface에는 별도 catalog force-refresh를 필수 사용자 작업으로 노출하지 않는다.

## 5. Program Update

일반 실행 시 `Propeex/JunhyunHelper` latest public stable GitHub Release를 확인한다.

- current보다 strictly newer stable `vMAJOR.MINOR.PATCH`만 대상
- 사용자 동의형
- exact user-facing release asset + checksum 검증
- archive/package-root 검증 전 현재 파일 변경 금지
- program-owned files만 transaction 교체
- 실패 시 rollback/기존 실행 복구 시도
- LocalAppData user data는 update 대상 아님
- 정식 release는 exact-source build/test/publish/smoke + public release verification을 통과해야 함

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

프로필 편집은 사용자-facing editing surface이며 v1.7.13부터 가능한 경우 MainWindow 내부 공통 overlay host에서 표시한다. Overlay는 표시/닫기 ownership만 담당하고 기존 validation/save semantics를 바꾸지 않는다.

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

Scanner `필요 개수`는 Item ID 확정 뒤 canonical `NeededItems[itemId].RemainingTotal`을 표시한다. 이 값은 현재 Inventory와 FIR 조건을 반영한 실제 남은 필요량이다. `RequiredTotal`은 전체 요구량이며 Scanner 사용자 표시값이 아니다.

## 11. Items

- category / 필요 상태 filter
- 퀘스트용/은신처용 용도 selector는 제품 surface에 두지 않고 필요한 아이템을 하나의 기준으로 표시
- Inventory + Needed Items 결합
- Quest / Hideout / Ammo cross-navigation
- Item Wiki navigation
- flexible candidate group 표시
- current content/profile 기반 presentation

## 12. Ammo

- read-only comparison
- name / caliber 검색
- 상단 조작은 `구경 → 즐겨찾기 토글 → 즐겨찾기 선택 → 검색` 순서로 좌측 정렬
- 표시 열 control은 우측 정렬
- 상세정보는 새 실행 세션에서 기본 접힘
- 표 위 중복 요약 문구를 두지 않음
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
- 지도 마커 선택은 기본 접힘이며 `지도 마커` launcher 자체로 열고 닫음
- 펼친 지도 마커 선택은 필요한 선택지를 한 번에 표시
- 설정 surface는 같은 설정 launcher 재클릭으로 닫을 수 있음
- 경로(trail) 표시와 `경로 지우기`는 제품 surface에서 제거
- Map 단축키 안내 설명 문구는 제품 surface에서 제거
- manual floor / hotkeys
- screenshot 기반 Map/player tracking
- floor = presentation relation, visibility filter 아님
- enabled cross-floor marker 유지
- Main Map floor change 시 zoom + map-space center 보존
- MiniMap floor change 시 exact Scale + Translate 보존
- MiniMap click-through
- MiniMap first-open 전에 현재 Main Map 선택을 shared `MapTrackerService`에 동기화
- MiniMap width/height를 `%LocalAppData%/JunhyunHelper/minimap-window-state.json`에 저장·복원하고 안전 범위로 clamp

Configurable Map hotkey:

- primary key는 일치해야 함
- 등록된 Ctrl/Alt/Shift는 모두 눌려 있어야 함
- 등록하지 않은 Ctrl/Alt/Shift가 추가로 눌린 것은 허용
- 같은 primary key에 여러 compatible binding이 있으면 required modifier 수가 많은 더 구체적인 binding 우선
- 동률은 기존 기능 우선순위/안정적 등록 순서
- Windows modifier 미지원
- bare NumPad0~5 direct floor selection 유지

Map은 독립 subsystem이고 Quest만 current JunhyunHelper content/profile과 bridge한다. 검증된 Map/MiniMap은 기능 요구 없이 불필요하게 재설계하지 않는다. v1.7.13 UI 단순화는 donor source 자체를 수정하지 않고 JunhyunHelper first-party customization boundary에서 적용한다.

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
→ serialized Windows ko-KR OCR
→ optional user OCR substitution
→ conditional environment-aware title normalization when needed
→ current-catalog sanitation / normalization
→ conservative official-catalog matching / bounded recovery
→ optional Tarkov-font/current-pixel visual corroboration/recovery
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
→ optional correction / Ground Truth
```

정상 normal OCR success path는 환경 정규화 분석이나 추가 OCR을 수행하지 않는다. Normal OCR miss 또는 기존 bounded deep OCR 단계에서만 title ROI luminance profile을 분석하고 lifted/washed/low-contrast 입력으로 판단될 때 auxiliary normalized OCR evidence를 추가한다. 정규화는 identity proof가 아니다.

### 14.2 Scanner safety contract

- false positive보다 miss 선호
- geometry/structural score는 proposal evidence이며 Item identity가 아님
- environment normalization은 Item identity가 아님
- `HEADER_FRAME_LOCKED >= 0.68`
- valid magnifier + red close-X 필수
- structural floor `0.34`
- continuous max 8 candidates
- one-shot max 12 candidates
- continuous observation target `200 ms`
- current official Korean full-item catalog가 identity authority
- exact-first conservative matcher
- generic confidence/top1-top2 margin을 evidence 없이 완화하지 않음
- ambiguity / low confidence → no Item ID
- scan-time network 금지
- icon-only identity 금지
- game memory read / DLL injection / packet interception 금지
- production OCR field는 item-name 하나
- automatic product-wide forced OCR substitution table 금지
- cross-frame OCR/visual identity cache 금지
- Item ID 확정 전 price/needed/slot metadata를 identity evidence로 사용하지 않음

### 14.3 Capture / one-shot

Continuous real Scanner는 EscapeFromTarkov Borderless client-area를 감지한다. `PrintWindow` 우선, invalid/empty이면 exact client screen rectangle fallback.

Display Test는 동일 recognition pipeline을 적용하며 real continuous mode와 상호 배타적이다.

One-shot 기능은 유지하지만 일반 Scanner page에는 별도 one-shot 실행 버튼을 두지 않는다.

```text
1회 인게임: Ctrl+Shift+F10
1회 테스트: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

Configurable Scanner hotkey는 Map과 동일한 modifier compatibility 계약을 따른다. 등록 modifier는 모두 필요하고 추가 Ctrl/Alt/Shift는 허용하며, 같은 primary key에서 여러 compatible binding이 있으면 더 구체적인 binding이 우선한다. Windows modifier는 지원하지 않는다.

### 14.4 Full Item catalog / item search

Scanner identity catalog는 Needed subset이 아니라 current GameMode 공식 전체 Item catalog다.

실제 scan/search 중에는 local/memory data만 사용한다.

item search result:

- cached icon
- official name

선택 presentation:

- icon/name
- Tarkov Wiki
- flea 24h average
- best non-flea trader price + trader name where trusted
- `NeededItems[itemId].RemainingTotal`
- 현재 needed item이면 기존 `NeededItems[itemId].Sources`의 Quest/Hideout source list
- source 선택 시 해당 Quest/Hideout 화면으로 navigation

Scanner 검색은 source/필요량을 자체 재계산하지 않고 기존 `ItemsWorkspace.Plan.NeededItems` 결과를 같은 Item ID로 join한다.

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

아이콘/공식 이름은 fixed identity header이므로 별도 `항상 표시` 안내 row를 제품 설정에 두지 않는다.

표시 설정은 변경 즉시 기존 atomic settings store에 저장하고 별도 저장/취소 버튼을 요구하지 않는다. Scanner global hotkey 편집은 display 설정에서 분리해 기본 Scanner surface에서 접근한다.

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
- `NeededItems[itemId].RemainingTotal`

Market/dimension 일부 오류는 affected field만 fail closed하고 healthy Item ID를 버리지 않는다.

### 14.8 Ground Truth / correction

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

정상 monitoring은 durable automatic correction Case를 만들지 않는다. latest exact frame은 current correction용 메모리 상태로만 유지한다.

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

Same active scan cycle의 exact-identical OCR/current-pixel bitmap evidence만 reuse. Cross-frame identity cache 없음.

Title continuity signature는 trusted detail continuity evidence이지 Item identity proof가 아니다.

Reviewed Ground Truth는 자동 삭제하지 않는다.

Legacy/automatic unreviewed diagnostic cleanup은 retention/state/recent-write safety를 증명할 때만 수행하며 unknown/corrupt metadata는 preserve fail closed한다. Logs는 bounded rotation한다.

### 14.10 Scanner UI

일반 surface:

- Scanner ON/OFF
- 표시 설정
- 고급
- configurable hotkey 설정
- 현재 결과 교정
- item search
- recognition log

`표시 설정`은 Mini Scanner display/order와 OCR substitution 같은 display-owned preference를 다루며 변경 즉시 저장한다.

Global hotkey 편집은 display 설정에서 분리해 normal Scanner surface에서 접근한다.

`현재 결과 교정`은 최신 exact in-memory frame을 대상으로 하며 우측 조작 영역에 둔다.

`고급`은 Display Test + correction data management + support diagnostics를 우선한다.

일반 surface에 catalog recovery/regression/export 같은 developer action을 펼쳐 놓지 않는다.

Scanner 설정/편집 surface는 가능한 경우 MainWindow 내부 공통 overlay interaction을 사용하고 X/backdrop/동일 launcher 재클릭으로 닫는다.

## 15. Images / preference persistence

Image cache는 remote bytes를 검증·정규화한 뒤 LocalAppData에 저장한다. Scanner scan/search path는 local cached icon만 사용한다.

Map/Ammo/Scanner preference와 MiniMap window size는 사용자 mutable data이며 Program Update가 덮어쓰지 않는다.

## 16. UI 품질

- MainWindow minimum width는 실제 2-pane/header 요구를 만족하는 1180
- normal vs settings vs advanced hierarchy를 명확히 유지
- 사용자-facing 편집/설정 surface는 가능한 경우 MainWindow 내부 공통 overlay host를 사용하고 backdrop/동일 launcher 재클릭 close semantics를 일관되게 유지
- clipping/scroll/status wording 회귀를 Product UI smoke로 검출
- standard WPF 설명 ToolTip은 표시하지 않음
- 지도 marker detail 같은 기능성 custom Popup/information surface는 유지
- 검증된 Map/MiniMap을 제품 요구 없이 재설계하지 않음

## 17. Release quality gate

Release candidate는 최소 다음을 통과해야 한다.

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
- public asset metadata/digest/tag-ref readback

가능한 검증 환경에서는 independent anonymous public redownload/hash/layout와 public-downloaded EXE smoke를 추가한다. 자동화 도구가 binary redownload를 제공하지 않는 경우 수행하지 않은 검증을 완료했다고 기록하지 않는다.

v1.7.13은 exact main CI artifact, Release workflow 검증, public GitHub asset digest와 tag-ref readback의 일치를 검증했다.

## 18. 현재 개발 방향

현재 제품과 Scanner는 **PRODUCT COMPLETE / MAINTENANCE MODE**다. 새 기능을 계속 추가하는 것이 기본 방향이 아니다.

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

현재 public stable의 제품 결정 상세는 `docs/DECISION_V1.7.13_UI_SIMPLIFICATION.md`, 공개 릴리즈 증거는 `docs/RELEASE_1.7.13.md`를 권위 기록으로 사용한다. v1.7.12 장기 유지보수 결정은 historical foundation으로 유지한다.
