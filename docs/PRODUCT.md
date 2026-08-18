# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 **무엇을 만들고 왜 만드는지**를 정의하는 공식 제품 요구사항입니다. 현재 사용자가 명시적으로 확정한 제품 의도가 과거 구현보다 우선합니다.

## 1. 제품 정의

`CONFIRMED`

**준현 헬퍼**는 Escape from Tarkov의 최신 게임 데이터를 온라인 원천에서 받아 canonical Game Content와 로컬 DB로 검증·재구축하고, 이를 User Progress와 결합해 플레이에 필요한 진행도·아이템·탄약·지도 정보를 제공하는 Windows x64 데스크톱 헬퍼입니다.

저장소: `Propeex/JunhyunHelper`

핵심 원칙:

- 일반 Game Content 변화는 importer가 이해하는 범위에서 자동 흡수
- 의미/schema가 검증 불가능하게 변하면 fail-closed
- 실패 candidate가 마지막 정상 Game Content를 덮어쓰지 않음
- User Progress와 Game Content는 분리
- runtime GPT/AI 의존성 없음
- 현재 코드가 존재한다는 이유만으로 공식 제품 요구사항으로 추정하지 않음

## 2. 지원 플랫폼 / 배포

`CONFIRMED / IMPLEMENTED`

- Windows x64
- .NET 10 WPF
- self-contained single-file executable
- portable ZIP
- 별도 .NET Runtime 설치 불필요
- 관리자 권한 불필요
- 설치 프로그램 없음
- 현재 코드 서명 없음

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

## 3. Game Content update

`CONFIRMED / IMPLEMENTED`

```text
온라인 데이터
→ 다운로드
→ 외부 형식 / 필수 의미 검증
→ canonical model 변환
→ candidate DB
→ 관계 / read-back 검증
→ active Game Content 교체
→ image prefetch
→ User Progress와 결합
→ 파생 결과 계산
```

원칙:

- 실패 candidate는 폐기
- 기존 active content 보존
- Game Content update가 `user.db`를 삭제/덮어쓰지 않음
- 파생 결과를 별도 권위 데이터로 저장하지 않음
- 개별 이미지 실패는 Game Content update 전체 실패가 아님

현재 Content schema:

```text
latest: v7
readable: v3, v4, v5, v6, v7
```

- v3: Wiki Ballistics membership / effectiveness 분리
- v4: Quest geometry
- v5: availability metadata / opaque conditions
- v6: recoverable special-trader access 분리, source prerequisite state 보존
- v7: structured `globalVariable` requirement (`variableId`, operator, value)

## 4. Program Update

`CONFIRMED / IMPLEMENTED / PUBLIC VERIFIED v0.1.14`

사용자가 2026-08-18 확정한 제품 요구사항:

1. 프로그램 실행 시 최신 버전을 조회한다.
2. 현재 버전보다 최신 버전이 있으면 사용자에게 업데이트 동의 여부를 묻는다.
3. 사용자가 동의하면 업데이트를 진행하고 완료 후 새 버전으로 자동 재시작한다.

세부 제품 계약:

- latest public **stable** GitHub Release만 사용
- `vMAJOR.MINOR.PATCH` exact stable version만 허용
- 현재 버전보다 엄격히 높은 버전만 업데이트 대상으로 처리
- 새 버전이 없으면 아무 업데이트 UI도 표시하지 않음
- user consent는 Yes/No, 기본 선택은 No
- No 선택 시 현재 버전을 계속 사용하고 다음 실행 때 다시 확인
- GitHub/network check 실패는 일반 프로그램 실행을 막지 않음
- 동의 후 exact Windows ZIP + `SHA256SUMS.txt` 다운로드
- 공개 SHA-256과 다운로드 ZIP을 검증
- package traversal / symlink / duplicate / unexpected root / PDB 거부
- 검증 완료 전 현재 프로그램 파일 변경 금지
- 실행 중 EXE 교체는 임시 self-copy updater mode가 수행
- program-owned files만 transaction 교체
- 교체 실패 시 rollback 시도
- 성공 시 새 버전 자동 재실행
- 사용자 데이터는 업데이트하지 않음

Program-owned update target:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

상세: `docs/PROGRAM_UPDATE.md`

Bootstrap:

- v0.1.13에는 updater 코드가 없음
- **v0.1.13 → v0.1.14는 한 번 수동 ZIP 교체 필요**
- v0.1.14 이후 후속 정식 릴리즈는 프로그램 내 업데이트 가능

## 5. User Progress / Profile

`CONFIRMED / IMPLEMENTED`

한 GameMode당 독립 profile 하나를 사용합니다.

지원 GameMode:

- regular
- pve
- pvp-season

저장 사실:

- player level / faction / edition / prestige
- trader LL / 필요한 standing
- completed Quest
- required explicit permanent failed Quest
- exact profile-variable 값이 관측된 경우 `ProfileVariables`
- recoverable special-trader access sparse fact
- Hideout levels
- FIR / non-FIR Inventory
- Quest / Hideout consumption ledger

`user.db` SQLite schema: **v1**

프로그램 update / Game Content update / profile deletion은 서로 다른 작업입니다.

## 6. Quest

`CONFIRMED / IMPLEMENTED`

제품 상태:

- 진행 중
- 확인 필요
- 잠김
- 사용 불가
- 완료

별도 `수주 가능` 상태를 만들지 않습니다. EFT에서 받을 수 있는 Quest는 Helper에서 이미 수락한 것으로 간주합니다.

Prerequisite 원칙:

- 서로 다른 `taskRequirements` = AND
- 한 requirement의 `status[]` = OR
- source의 `complete` / `active` / `failed` 의미 보존
- source보다 강한 조건을 compatibility가 임의로 만들지 않음
- 증명할 수 없는 availability는 `확인 필요`

특수 trader:

- BTR Driver: 누락 gate는 `A Helping Hand = Active`
- Ref: source gate 보존 + GameMode별 검증된 missing unlock Complete만 보강
- Lightkeeper: ordinary prerequisite와 recoverable access 분리

Profile-variable:

1. exact current 값이 있으면 권위값
2. exact 값이 없고 현재 감사 구조가 완전히 일치하면 제한된 compatibility
3. 구조 drift / 증명 부족이면 fail-closed `확인 필요`

현재 감사된 dialogue gate는 exact-ID 12건에만 compatibility를 적용합니다. 새/변경 dialogue는 추측하지 않습니다.

실제 completion timestamp가 필요한 availability delay는 timestamp가 없으면 `확인 필요`입니다.

## 7. Quest Item / Consumption

`CONFIRMED / IMPLEMENTED`

- mandatory fixed submit material은 Quest completion과 함께 ledger 기반 자동 소비 가능
- flexible hand-in은 후보들을 group으로 유지
- 실제 어떤 후보를 냈는지 프로그램이 임의 추측하지 않음
- rollback 시 exact consumed ledger를 복구하거나 ledger를 보존해 중복 소비를 방지
- malformed empty candidate set / `Count <= 0`은 active canonical DB 적용 전 차단

## 8. Hideout

`CONFIRMED / IMPLEMENTED`

- station 현재 level 저장
- 현재 이후 미래 level requirements 포함
- fixed material 자동 소비/rollback ledger 지원
- Hideout item requirement `Count <= 0`은 fatal validation

## 9. Needed Items / Inventory

`CONFIRMED / IMPLEMENTED`

목표는 **앞으로 실제 필요할 수 있는 아이템을 보수적으로 보호**하는 것입니다.

- future Quest requirements 포함
- future Hideout requirements 포함
- unresolved future Quest는 `IndeterminatePotential`로 보호
- flexible hand-in candidate도 보호
- exact cleanup safety를 증명할 수 없는 item을 낙관적으로 정리 가능 처리하지 않음
- Quest/Hideout/profile prerequisite 구조가 바뀌면 full recalculation
- 단순 Inventory 수량 변경은 planning basis를 재사용

Inventory는 FIR / 일반 수량을 별도로 관리합니다.

## 10. Items

`CONFIRMED / IMPLEMENTED`

- Item category / 용도 / 필요 상태 filter
- Inventory와 Needed Items 결합
- Quest / Hideout / Ammo로 cross-navigation
- Item Wiki navigation
- flexible candidate group 표시
- `필요 / 전체 / 충분` 상태 필터

## 11. Ammo

`CONFIRMED / IMPLEMENTED`

- read-only comparison
- name / caliber 검색
- exact caliber / exact Ammo navigation
- raw Ammo stats와 Wiki Ballistics fact 분리
- Wiki membership과 Armor Class 1~6 effectiveness 분리
- 자체 effectiveness heuristic 금지
- caliber favorites는 shortcut menu

Favorite persistence:

```text
%LocalAppData%/JunhyunHelper/ammo-favorites.json
%LocalAppData%/JunhyunHelper/ammo-favorites.json.bak
```

## 12. Images

`CONFIRMED / IMPLEMENTED`

권위 데이터는 canonical image URL이며 local image cache는 presentation-only입니다.

```text
canonical URL
→ bytes download
→ SkiaSharp decode
→ validation
→ PNG normalize
→ image-cache
→ WPF
```

개별 image 실패는 nonfatal입니다.

## 13. Preference persistence

`CONFIRMED / IMPLEMENTED`

Map 설정과 Ammo favorites는 v0.1.13부터:

- same-directory temporary write
- flush-to-disk
- atomic replacement
- last-known-good `.bak`
- corrupt primary → backup fallback
- corrupt primary가 good backup을 덮어쓰지 않도록 보호
- 저장 실패를 전역 WPF fatal로 확대하지 않음

## 14. Map / MiniMap

`CONFIRMED / IMPLEMENTED / WINDOWS USER VALIDATED`

Map은 pinned donor-derived 독립 subsystem이며 Quest만 JunhyunHelper current content/profile과 연결합니다.

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

핵심 제품 계약:

- Current Quest sidebar
- Quest별 A/B/C/D marker identity
- 일반 marker / PMC·Scav·Transit extracts
- manual floor
- global floor/zoom/MiniMap size hotkeys
- screenshot 기반 Map 전환 / player position / 가능한 경우 heading
- floor는 visibility filter가 아니라 presentation relation
- enabled 타층 marker 유지
- current / above / below relation 표현
- Main Map floor 변경 시 zoom + map-space viewport center 보존
- MiniMap floor 변경 시 exact live Scale + Translate X/Y 보존
- shared map key 동기화
- MiniMap click-through
- opacity / temporary transparency / marker scale

Current Quest sidebar layout:

```text
30px checkbox | 34px A/B/C/D | * Quest text
```

실제 rendered title X-axis와 expanded handle 위치를 release smoke에서 검증합니다.

제품 설정:

```text
%LocalAppData%/JunhyunHelper/map-product-settings.json
%LocalAppData%/JunhyunHelper/map-product-settings.json.bak
```

안정적인 donor Map path는 concrete regression/performance 이유 없이 wholesale cleanup/refactor하지 않습니다.

## 15. Scanner

`PRODUCT OPEN / PLACEHOLDER TAB VISIBLE`

상단 `스캐너` 탭은 제품 UI에 유지합니다.

- 현재 표시: **`준비 중`**
- 실제 scanning/recognition/import 기능 없음
- 별도 사용자 요구사항 확정 전 임의 구현 금지
- maintenance/refactor에서 임의 숨김/삭제 금지

## 16. UI / 검증

- 공식 제품명: **준현 헬퍼**
- 실행 파일: **`준현 헬퍼.exe`**
- dark WPF desktop UI
- refresh-driven automatic list scrolling 금지

주요 UI 계약은 source inspection/build 성공만으로 완료 처리하지 않습니다. 실제 published WPF 앱의 `Measure/Arrange` 및 runtime smoke를 검사합니다.

현재 release gate:

- Flexible candidate actual row stretch
- icon/name left lane, FIR/general right lane
- Ammo favorite exact single `☆`/`★`
- Ammo expanded=`▼`, collapsed=`▲`
- Map Quest title X deviation `<= 0.75px`
- expanded Map sidebar handle right gap `<= 6px`
- Main Map / Factory / MiniMap smoke
- graceful close / process exit

## 17. 현재 공개 릴리즈

**v0.1.14 PUBLIC RELEASE / VERIFIED — Windows x64**

```text
release tag: v0.1.14
release baseline / tag SHA: bb0611e9263c24018825a87a58aba2c5474b6cc4
ProductVersion: 0.1.14+bb0611e9263c24018825a87a58aba2c5474b6cc4
Content schema: v7
Readable schemas: v3~v7
user.db schema: v1
feature CI: 32115435656 — SUCCESS
release PR CI: 32115953069 — SUCCESS
public verification workflow: 32116726491 — SUCCESS
automated tests: 232 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v0.1.14-win-x64.zip
size: 74,086,942 bytes
SHA-256: 9b3aaff8ba2182b146ea6b1ec463efd8dc8b1c5532a8d4db6cf716938536ae02
v0.1.13 → v0.1.14 mandatory data update: none
```

상세 구현/상태는 `docs/STATE.md`, program update 계약은 `docs/PROGRAM_UPDATE.md`, 공개 검증은 `docs/RELEASE_0.1.14.md`를 기준으로 합니다.

## 18. 현재 비범위 / fail-closed 범위

- EFT 1.0 Story Chapters: ordinary task source 밖, 현재 미지원
- PvE Skier LL2 task-pool drift: exact fact가 없으면 해당 pool fail-closed
- Scanner actual feature: 사용자 별도 요구 전 미구현
- code signing / installer: 현재 필수 아님
- stable donor Map wholesale refactor: 구체적 회귀/성능 근거 없으면 하지 않음
