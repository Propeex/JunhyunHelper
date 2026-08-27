# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 **무엇을 만들고 왜 만드는지**를 정의하는 공식 제품 요구사항이다. 사용자의 최신 확정 의도가 과거 구현보다 우선하며, 현재 코드가 존재한다는 이유만으로 그 동작을 제품 요구사항으로 추정하지 않는다.

기준일: 2026-08-28 KST  
상태: **v1.7.15 PUBLIC STABLE / VERIFIED / PRODUCT COMPLETE / MAINTENANCE MODE**

정확한 공개 release SHA, CI, asset hash는 `docs/STATE.md`와 `docs/RELEASE_1.7.15.md`가 권위다.

## 1. 제품 정의

**준현 헬퍼**는 Escape from Tarkov의 현재 검증 가능한 Game Content와 사용자 진행 상태를 결합해 Quest, Hideout, Needed Items, Inventory, Items, Ammo, Map/MiniMap, Scanner/Mini Scanner를 제공하는 Windows x64 데스크톱 헬퍼다.

제품 목표:

- 플레이 중 필요한 진행/아이템 정보를 빠르게 확인
- 사용자가 알고 있는 진행 상태를 정확하게 저장
- Tarkov 데이터가 바뀌어도 검증 가능한 범위에서 안전하게 갱신
- 알 수 없는 상태를 추측하지 않고 fail closed
- 게임 프로세스를 변조하거나 내부 메모리/패킷을 읽지 않는 외부 보조 프로그램 유지
- 일상 사용 UI와 개발/진단 UI를 구분
- 장시간 실행해도 사용자 데이터와 디스크 사용량을 안정적으로 관리
- Scanner 실패를 user-reviewed Ground Truth로 재현·교정할 수 있게 함

핵심 원칙:

- User Progress와 Game Content 분리
- authoritative fact와 derived presentation 분리
- 일반 Game Content 변화는 importer가 이해하는 범위에서 자동 흡수
- 의미/schema가 검증 불가능하게 변하면 fail closed
- failed candidate가 last-known-good content를 덮어쓰지 않음
- Runtime GPT/AI 의존성 없음
- 기존 `Propeex/Tarkov-Helper`는 공식 요구사항의 권위가 아님

## 2. 플랫폼 / 배포

- Windows x64
- .NET 10 WPF
- self-contained single-file executable
- portable ZIP
- 별도 .NET Runtime 불필요
- 일반 사용에 관리자 권한 불필요
- installer 없음
- 현재 code signing 없음

User-facing package contract:

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/
```

ZIP/folder 이름은 version과 분리한다. Version identity는 EXE ProductVersion, Git tag, GitHub Release metadata에 둔다.

Mutable user data는 `%LocalAppData%/JunhyunHelper`에 저장한다. Portable executable 옆에 mutable profile/log/settings data를 만들지 않는다.

현재 public stable/latest는 **v1.7.15**다. 공개 product source는 `4bf5e3a567d3ce9563657bbb3b90bec0871c06b4`이며 공개 후 immutable release로 취급한다.

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

- candidate 완성/검증 전 active overwrite 금지
- failed candidate 폐기, 기존 healthy active content 유지
- `user.db` 삭제/덮어쓰기 금지
- derived result를 별도 authoritative fact처럼 저장하지 않음
- 개별 image 실패는 update 전체 fatal이 아님
- collection schema drift를 이해할 수 없으면 fail closed
- 기존 healthy snapshot이 있으면 핵심 coverage가 baseline의 50% 미만으로 급감하는 suspicious partial payload를 차단
- Wiki Ballistics enrichment는 fail-soft

현재 compatibility:

```text
Content schema: v7
Readable: v3, v4, v5, v6, v7
```

## 4. Game Data Update

상단 데이터 업데이트는 일반 Game Content와 current GameMode Scanner full-item/market catalog를 하나의 제품 흐름에서 갱신한다.

```text
remote Game Content
→ validate/build/activate general content
→ Scanner full-item + market catalog refresh
→ combined result/status
```

Scanner refresh만 실패하면 healthy general Game Content를 rollback하지 않는다. 기존 healthy same-mode Scanner cache가 있으면 유지한다.

Game Content update progress는 전용 progress overlay를 사용한다. 메인 header의 version 영역에 transient update 문구를 표시하지 않는다.

## 5. Program Update

일반 실행 시 `Propeex/JunhyunHelper` latest public stable GitHub Release를 확인한다.

- current보다 strictly newer stable `vMAJOR.MINOR.PATCH`만 대상
- 사용자 동의형
- exact user-facing release asset + checksum 검증
- archive/package-root 검증 전 현재 program files 변경 금지
- program-owned files만 transaction 교체
- 실패 시 rollback/기존 실행 복구 시도
- `%LocalAppData%/JunhyunHelper` user data는 update 대상 아님
- 이미 공개된 stable release는 immutable

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

Profile edit은 MainWindow shared in-app overlay에서 표시한다. Overlay는 표시/닫기 lifetime만 소유하고 기존 validation/save semantics를 변경하지 않는다.

## 7. Quest

사용자 상태:

- 진행 중
- 확인 필요
- 잠김
- 사용 불가
- 완료

Availability 원칙:

- 서로 다른 `taskRequirements` = AND
- 한 requirement의 accepted `status[]` = OR
- 받을 수 있는 Quest는 Helper에서 이미 수락한 것으로 간주
- source보다 강한 prerequisite를 임의 생성하지 않음
- 증명할 수 없는 availability = `확인 필요`
- exact ProfileVariable fact가 있으면 권위값으로 사용
- audited compatibility는 구조가 정확히 맞을 때만 사용
- source drift / unsupported requirement는 fail closed

## 8. Quest Item / Consumption

- mandatory fixed submit material은 Quest completion과 함께 ledger 기반 자동 소비 가능
- flexible hand-in은 candidate group으로 유지
- 실제 소비 candidate를 자동 추측하지 않음
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

Scanner `필요 개수` authority:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
```

Scanner searched-needed source authority:

```text
ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

Scanner가 Quest/Hideout requirement를 별도로 재계산하지 않는다.

## 11. Items

- category / 필요 상태 filter
- 퀘스트용/은신처용 용도 selector는 제품 surface에 두지 않고 필요한 아이템을 하나의 기준으로 표시
- Inventory + Needed Items 결합
- Quest / Hideout / Ammo cross-navigation
- Item Wiki navigation
- flexible candidate group 표시
- current content/profile 기반 presentation
- 검색창 clear는 입력창 오른쪽 내부 `×` affordance 사용

### Cleanup attention — v1.7.15

- main header의 version/status 영역은 version만 표시한다.
- `정리 필요` 문자열을 header에 표시하지 않는다.
- `ItemsWorkspace.Plan.CleanupItems.Count > 0`이면 Items 탭 우측 상단에 작은 orange dot을 표시한다.
- cleanup 대상이 없어지면 indicator도 사라진다.
- indicator는 count/text를 추가하지 않는 compact attention signal이다.

## 12. Ammo

- read-only comparison
- name / caliber 검색
- 표시 열 control 유지
- 상세정보는 새 실행 세션에서 기본 접힘
- exact caliber / Ammo navigation
- raw Ammo stats와 Wiki Ballistics fact 분리
- membership과 Armor Class effectiveness 분리
- 자체 effectiveness heuristic 금지
- caliber favorites
- 검색창 clear는 입력창 오른쪽 내부 `×` affordance 사용

### Caliber / Favorites selector — v1.7.15

- visible `즐겨찾기 선택`은 standard dropdown을 사용한다.
- favorites persistence와 caliber filtering 의미는 기존 authority를 유지한다.
- caliber dropdown과 Favorites dropdown은 같은 icon+label presentation을 사용한다.
- 각 caliber icon 후보는 해당 `RawCaliber`에 실제 속한 `AmmoRow.Icon`만 사용한다.
- 특정 ammo 하나를 caliber의 영구 대표 icon으로 고정하지 않는다.
- 두 dropdown은 같은 caliber별 animation state를 공유한다.
- cadence는 1.4초이며 두 dropdown이 모두 닫히면 animation timer를 중지한다.
- icon은 기존 Ammo `ImageCacheService`/row icon load 결과를 재사용하고 별도 network/source authority를 만들지 않는다.

## 13. Map / MiniMap

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

제품 계약:

- Current Quest sidebar / marker identity
- general marker / PMC·Scav·Transit extracts
- 지도 마커 선택은 기본 접힘이며 `지도 마커` launcher 자체로 열고 닫음
- launcher는 JunhyunHelper 일반 Button chrome 사용
- collapsed marker panel은 빈 min-width/padding/background/border를 남기지 않음
- Map/MiniMap Settings는 MainWindow shared in-app overlay에 표시
- 경로(trail) 표시와 `경로 지우기`는 제품 surface에서 제거
- Map 단축키 안내 설명 문구는 제품 surface에서 제거
- manual floor / hotkeys
- screenshot 기반 Map/player tracking
- floor = presentation relation, visibility filter 아님
- enabled cross-floor marker 유지
- Main Map floor change 시 zoom + map-space center 보존
- MiniMap floor change 시 exact Scale + Translate 보존
- MiniMap click-through
- MiniMap first-open 전에 현재 Main Map 선택 동기화
- MiniMap width/height user preference 저장·복원

### Marker selector — v1.7.15

- expanded panel의 내부 checkbox list도 current Map viewport의 실제 available height를 사용한다.
- content가 available height에 들어오면 vertical scrollbar를 표시하지 않는다.
- 실제 content가 넘칠 때만 scrolling을 허용한다.
- existing launcher re-click toggle을 유지한다.
- panel outside click으로 dismiss할 수 있다.
- outside dismiss는 marker enable/check state를 변경하지 않는다.
- dismiss click은 가능한 한 원래 Map/control interaction을 소비하지 않는다.

Map은 독립 subsystem이고 Quest만 current JunhyunHelper content/profile과 bridge한다. Donor source 자체를 제품 요구사항에 맞추기 위해 broad-edit하지 않고 JunhyunHelper first-party customization boundary에서 제품 delta를 적용한다.

## 14. Scanner / Mini Scanner

Scanner는 Tarkov 화면 픽셀을 Item ID로 변환해 기존 JunhyunHelper data에 연결하는 입력 subsystem이다.

Canonical technical contract는 `docs/SCANNER.md`다.

### Recognition pipeline

```text
Tarkov window pixels
→ capture
→ detail rectangle proposals
→ red close-X + magnifier + inspect-header semantic validation
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

Scanner safety contract:

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous max candidates = 8
one-shot max candidates = 12
continuous observation target = 200 ms
```

- false positive보다 miss 선호
- geometry/structural score는 Item identity가 아님
- environment normalization은 Item identity가 아님
- current official Korean full-item catalog가 identity authority
- ambiguity / low confidence → no Item ID
- scan-time network 금지
- icon-only identity 금지
- game memory read / DLL injection / packet interception 금지
- cross-frame OCR/visual identity cache 금지
- Item ID 확정 전 price/needed/slot/source/previous-frame metadata identity evidence 사용 금지
- reviewed evidence 없이 threshold/candidate/matcher/visual acceptance 완화 금지

### Capture / one-shot / hotkeys

Continuous real Scanner는 EscapeFromTarkov client-area를 대상으로 한다. Display Test는 동일 recognition pipeline을 적용하며 real continuous mode와 상호 배타적이다.

Default hotkeys:

```text
1회 인게임: Ctrl+Shift+F10
1회 테스트: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

Windows modifier는 지원하지 않는다. Compatible binding 중 required modifier 수가 많은 더 구체적인 binding을 우선한다.

### Full Item catalog / item search

Scanner identity catalog는 Needed subset이 아니라 current GameMode 공식 전체 Item catalog다. 실제 scan/search 중에는 local/memory data만 사용한다.

Item search presentation:

- cached icon
- official name
- Tarkov Wiki navigation
- flea 24h average
- best trusted non-flea trader price + trader name
- `NeededItems[itemId].RemainingTotal`
- current needed item이면 `NeededItems[itemId].Sources` Quest/Hideout source list

### Scanner display settings schema v6

Mini Scanner fixed identity header:

- item icon
- official item name

사용자가 표시 여부/순서를 지정할 수 있는 추가 정보:

- trader sell price
- flea average
- trader price/slot
- flea price/slot
- current needed

Scanner Settings가 Mini Scanner display 설정과 global Scanner hotkey 편집을 함께 소유한다.

### Ground Truth / correction

Automatic diagnostic Case는 정답이 아니다. User-reviewed Case만 Ground Truth다. 정상 monitoring은 durable automatic correction Case를 만들지 않는다.

### Performance / retention

Same active scan cycle의 exact-identical current-pixel evidence만 reuse할 수 있다. Cross-frame identity cache는 금지한다. Reviewed Ground Truth는 자동 삭제하지 않는다. Runtime logs는 bounded rotation한다.

### Scanner UI

Normal Scanner surface:

- Scanner ON/OFF
- 설정
- 고급
- 현재 결과 교정
- item search
- recognition log

일반 surface에 catalog recovery/regression/export 같은 developer action을 펼쳐 놓지 않는다.

## 15. Shared user-facing overlay contract

주요 settings/editor surface:

- Profile Edit
- Scanner Settings
- Scanner Advanced
- Map / MiniMap Settings

공통 interaction:

```text
launcher
→ MainWindow shared overlay owner
→ same launcher / backdrop / common X → dismiss
```

Child editor의 validation/save semantics를 MainWindow가 재구현하지 않는다.

## 16. Images / preference persistence

Image cache는 remote bytes를 검증·정규화한 뒤 LocalAppData에 저장한다. Scanner scan/search path는 local cached icon만 사용한다.

Map/Ammo/Scanner preference와 MiniMap window size는 user mutable data이며 Program Update가 덮어쓰지 않는다.

## 17. UI 품질

- MainWindow minimum width는 실제 2-pane/header 요구를 만족하는 1180
- normal vs settings vs advanced hierarchy 유지
- user-facing editor/settings는 shared overlay interaction을 우선
- 주요 검색창 clear affordance는 입력창 내부 오른쪽에 통일
- main header identity lane은 version-only
- cleanup attention은 Items tab orange dot
- clipping/scroll/status wording 회귀를 Product UI smoke와 deterministic contract로 검출
- standard WPF 설명 ToolTip은 표시하지 않음
- 지도 marker detail 같은 기능성 custom Popup/information surface는 유지
- 검증된 Map/MiniMap을 제품 요구 없이 재설계하지 않음

## 18. Release quality gate

Runtime release candidate는 최소 다음을 통과해야 한다.

- Desktop Release build
- full deterministic tests
- Windows x64 self-contained single-file publish
- ProductVersion / FIRST_RUN identity audit
- Product UI / Scanner / Mini Scanner smoke
- Main Map / Factory / MiniMap smoke
- graceful shutdown
- clean portable root / forbidden dependency audit
- `Junhyun-Helper.zip` 생성
- package SHA-256 + checksum verification
- exact main source CI
- Release workflow exact artifact verification
- exact public tag/source
- public stable/latest publication
- public asset metadata/digest/tag-ref readback

v1.7.15는 이 gate를 통과했다. exact proof는 `docs/RELEASE_1.7.15.md`를 사용한다.

## 19. 현재 개발 방향

현재 제품과 Scanner는 **PRODUCT COMPLETE / MAINTENANCE MODE**다. 새 기능을 계속 추가하는 것이 기본 방향이 아니다.

```text
real usage / Tarkov change / reviewed Ground Truth
→ failure-stage classification
→ affected layer only modification
→ deterministic regression
→ full Windows release gate
→ PATCH 판단
```

Ground Truth evidence 없이 Scanner threshold/candidate cap/matcher/visual policy를 완화하지 않는다.

현재 public stable의 제품 결정 상세는 `docs/DECISION_V1.7.15_UI_REFINEMENTS.md`, 공개 릴리즈 증거는 `docs/RELEASE_1.7.15.md`를 권위 기록으로 사용한다. 이전 버전 결정은 historical foundation으로 유지한다.
