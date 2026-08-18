# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

기준일: 2026-08-18

## 1. 제품 목적

**준현 헬퍼**는 Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 프로그램입니다.

핵심 구조:

```text
온라인 Tarkov 데이터
→ 외부 형식/필수 의미 검증
→ canonical model 변환
→ candidate DB
→ 관계/read-back 검증
→ active Game Content 교체
→ User Progress와 결합
→ Quest / Hideout / Needed Items / Ammo / Map 표시
```

- runtime GPT/AI 의존성 없음
- Game Content update와 프로그램 update는 별도 subsystem
- 기존 `Propeex/Tarkov-Helper`는 공식 요구사항이 아니며 Map/MiniMap의 검증된 donor source로만 제한 사용

## 2. 현재 공개 상태

**v0.1.14 PUBLIC RELEASE / VERIFIED — Windows x64**

```text
release tag: v0.1.14
release baseline: bb0611e9263c24018825a87a58aba2c5474b6cc4
public tag SHA: bb0611e9263c24018825a87a58aba2c5474b6cc4
Desktop ProductVersion: 0.1.14+bb0611e9263c24018825a87a58aba2c5474b6cc4
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
feature PR: #100
feature CI: 32115435656 — SUCCESS
release PR: #101
release PR CI: 32115953069 — SUCCESS
public verification PR: #102
public verification workflow: 32116726491 — SUCCESS
automated tests: 232 passed / 0 failed / 0 skipped
public asset: Junhyun-Helper-v0.1.14-win-x64.zip
public asset size: 74,086,942 bytes
public SHA-256: 9b3aaff8ba2182b146ea6b1ec463efd8dc8b1c5532a8d4db6cf716938536ae02
public release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.14
v0.1.13 → v0.1.14 mandatory data update: none
```

Public Release는:

- `draft=false`
- `prerelease=false`
- release target = exact release baseline
- public tag SHA = exact release baseline
- public ZIP / `SHA256SUMS.txt` 재다운로드 검증 성공
- public EXE rendered Product UI / Main Map / Factory / MiniMap / graceful shutdown smoke 성공

상세: `docs/RELEASE_0.1.14.md`

## 3. v0.1.14 프로그램 업데이트 계약

사용자가 확정한 제품 동작:

1. 프로그램 일반 실행 시 최신 정식 버전을 조회한다.
2. 현재 버전보다 최신 버전이 있으면 사용자에게 업데이트 동의 여부를 묻는다.
3. 사용자가 동의하면 업데이트하고 새 버전으로 자동 재시작한다.

현재 구현:

```text
MainWindow 표시
→ latest stable GitHub Release 비동기 조회
→ latest <= current: 아무 UI 없음
→ latest > current: Yes/No 동의창
→ Yes: ZIP + SHA256SUMS 다운로드
→ SHA-256 + package contract 검증
→ 임시 self-copy updater 실행
→ 원래 프로세스 종료
→ program-owned files transaction 교체
→ 새 EXE 재실행
```

업데이트 대상:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

업데이트 비대상:

```text
%LocalAppData%/JunhyunHelper/user.db
content/
image-cache/
map-product-settings.json(.bak)
ammo-favorites.json(.bak)
logs/
```

실패 정책:

- latest 조회 실패 → 앱 정상 사용
- 사용자 No → 앱 정상 사용, 다음 실행 때 다시 확인
- download/checksum/package 검증 실패 → 현재 프로그램 파일 미변경
- updater runner 시작 실패 → 현재 프로그램 파일 미변경
- 교체 중 실패 → previous owned files rollback 시도, 기존 EXE 재실행 시도
- 오류 기록 → `%LocalAppData%/JunhyunHelper/logs/startup.log`

보안/무결성:

- stable `vMAJOR.MINOR.PATCH`만 자동 업데이트 대상
- exact Windows ZIP과 `SHA256SUMS.txt` 요구
- GitHub Release asset URL scope 확인
- SHA-256 검증
- path traversal / symlink / duplicate entry / unexpected root / PDB 거부
- 검증 완료 전 기존 프로그램 파일 변경 금지

상시 `Updater.exe`는 배포하지 않습니다. 현재 single-file EXE의 임시 복사본을 updater mode로 실행합니다.

**Bootstrap:** v0.1.13에는 updater 코드가 없으므로 v0.1.13 → v0.1.14는 한 번 수동 ZIP 교체가 필요합니다. v0.1.14 이후부터 후속 정식 릴리즈의 프로그램 내 업데이트가 가능합니다.

상세: `docs/PROGRAM_UPDATE.md`

## 4. 공개 릴리즈 계약

v0.1.14부터 program updater가 latest public Release를 신뢰하므로 미검증 release를 latest로 노출하지 않습니다.

```text
exact release baseline 고정
→ Release build / 전체 tests / publish / actual EXE smoke
→ ZIP + SHA256SUMS 생성
→ Draft GitHub Release 생성
→ Draft assets 재다운로드 / hash / package 검증
→ 성공한 경우에만 public/latest 전환
→ Public assets 재다운로드 / hash / ProductVersion / package 검증
→ 독립 public executable smoke
```

상시 저장소에는 원칙적으로 `.github/workflows/ci.yml`만 남기고 release/verification workflow는 릴리즈 완료 후 제거합니다.

## 5. Content / User Progress

### Content

현재 schema: **v7**

읽기 지원: **v3~v7**

- v3: Wiki Ballistics membership과 effectiveness 분리
- v4: Quest geometry
- v5: availability metadata / opaque conditions
- v6: recoverable special-trader access와 ordinary prerequisite 분리
- v7: structured `globalVariable` requirement (`variableId`, operator, value)

### User Progress

한 GameMode당 독립 profile 하나를 사용합니다.

저장 사실:

- level / faction / edition / prestige
- trader LL / 필요한 standing
- `CompletedQuestIds`
- required explicit `FailedQuestIds`
- optional exact `ProfileVariables`
- sparse recoverable special-trader access fact
- Hideout levels
- FIR / non-FIR Inventory
- Quest / Hideout consumption ledgers

`user.db` SQLite schema는 **v1**이며 optional JSON field 확장을 사용합니다.

## 6. Quest availability 정확도

기본 원칙:

- 서로 다른 `taskRequirements`는 AND
- 한 requirement 내부 `status[]`는 OR
- source의 `complete` / `active` / `failed` 의미 보존
- 별도 `수주 가능` 상태를 만들지 않음
- 받을 수 있는 Quest는 Helper에서 이미 수락한 것으로 간주
- 증명할 수 없는 availability는 `확인 필요(Indeterminate)`

특수 규칙:

- BTR Driver: 누락 gate는 `A Helping Hand = Active`, Complete로 강화하지 않음
- Ref: source gate 보존 + GameMode별 검증된 unlock Complete만 누락 보강
- Lightkeeper: ordinary prerequisite와 recoverable access를 분리, access loss는 recoverable `Locked`
- audited `dialogue` 12건만 exact-ID compatibility
- 새/변경 dialogue는 추측하지 않음
- 실제 completion timestamp가 필요한 availability delay는 timestamp가 없으면 `확인 필요`

### profile-variable / trader task-pool

판정 우선순위:

1. exact current profile-variable 값이 있으면 권위값
2. exact 값이 없고 current audited structure가 완전히 일치하면 제한된 current-version compatibility
3. 증명할 수 없으면 `확인 필요`

PvE Skier LL2의 audited task-pool drift는 구조 불일치로 해당 pool만 fail-closed합니다. 임의 보정하지 않습니다.

## 7. Needed Items / Inventory 안전성

- 미래에 진행 가능한 Quest와 미래 Hideout level 재료 포함
- unresolved future Quest는 `IndeterminatePotential`로 Item 보호
- flexible hand-in 후보는 group으로 보존하고 실제 소비 후보를 임의 선택하지 않음
- fixed completion material은 ledger를 사용해 명시적 진행 조작 시 자동 소비
- rollback 시 exact consumed ledger 복구 가능
- cleanup은 future requirement를 증명할 수 있을 때만 허용
- profile/Quest/Hideout 구조 변경 시 full recalculation
- 단순 Inventory 수량 변경은 planning basis를 재사용

## 8. Ammo / preference persistence

Ammo:

- read-only 비교
- name / caliber 검색
- exact caliber / exact Ammo navigation
- Wiki membership과 Armor effectiveness 별도 fact
- caliber favorites는 shortcut menu

Preference:

```text
%LocalAppData%/JunhyunHelper/ammo-favorites.json
%LocalAppData%/JunhyunHelper/ammo-favorites.json.bak
%LocalAppData%/JunhyunHelper/map-product-settings.json
%LocalAppData%/JunhyunHelper/map-product-settings.json.bak
```

v0.1.13부터:

- same-directory temp write
- flush-to-disk
- atomic replacement
- last-known-good `.bak`
- corrupt primary → good backup fallback
- corrupt primary가 good backup을 오염시키지 않음
- presentation preference save failure nonfatal

## 9. Map / MiniMap

Pinned donor-derived product revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

경계:

- Map subsystem은 독립
- JunhyunHelper Core와 Quest만 연결
- Hideout / Item / Ammo runtime과 결합하지 않음

제품 계약:

- floor는 marker visibility filter가 아니라 presentation relation
- enabled 타층 marker 유지
- current / above / below relation 표시
- semantic duplicate만 정규화
- Main Map floor 변경 시 live zoom + map-space viewport center 보존
- MiniMap floor 변경 시 exact live Scale + Translate X/Y 보존
- Main Map selector와 shared map key 동기화
- current Quest sidebar lane: `30px checkbox | 34px A/B/C/D | * Quest text`
- 실제 title X-axis와 expanded handle 위치를 rendered release smoke에서 검증
- Map slider save는 약 250ms coalesce 후 dispose 시 pending flush
- product hotkey / NumPad floor async failure와 keyboard hook failure를 전역 fatal로 확대하지 않음

안정적인 donor Map path는 concrete regression/performance 근거 없이 wholesale cleanup/refactor하지 않습니다.

## 10. Scanner

**현재 제품 계약: 상단 `스캐너` 탭은 visible, 내용은 `준비 중` placeholder.**

- 실제 scanning / recognition / import 기능 없음
- 기능을 구현된 것처럼 가장하지 않음
- 별도 사용자 요구사항 확정 전 임의 구현 금지
- maintenance/refactor에서 임의 숨김/삭제 금지

`DEC-045`가 과거 Scanner 숨김 결정을 대체합니다.

## 11. Rendered UI / release gate

소스 inspection이나 build 성공만으로 주요 UI 변경을 완료 처리하지 않습니다.

실제 publish된 WPF 앱에서 최소 다음을 검사합니다.

- Flexible hand-in candidate가 실제 row width로 stretch
- icon/name 좌측 lane / FIR-general 우측 lane
- Ammo favorite 실제 Content가 단일 `☆`/`★`
- Ammo detail expanded=`▼`, collapsed=`▲`
- 서로 다른 Map Quest title 시작 X 편차 `<= 0.75px`
- expanded Map sidebar handle right gap `<= 6px`
- Main Map / Factory / MiniMap runtime smoke
- graceful Main Window close / process exit

v0.1.14 public executable도 동일 gate를 통과했습니다.

## 12. 현재 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / fail-closed availability |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 / future protection / ledger |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / rendered gate |
| Game Content Update | 구현 완료 / candidate validation / last-known-good |
| Program Update | 구현 완료 / v0.1.14 public verified |
| Scanner | `준비 중` placeholder |

## 13. 현재 알려진 비차단 범위

- EFT 1.0 Story Chapters는 ordinary `json.tarkov.dev/tasks` progression source 밖이며 현재 제품 범위에 포함되지 않음
- PvE Skier LL2 task-pool drift는 exact fact가 없으면 해당 pool fail-closed
- Map donor/bridge maintenance debt는 안정성이 유지되는 동안 임의 refactor하지 않음
- code signing / installer는 현재 필수 범위 아님
- multi-DPI 확대 검증, user.db backup/restore UX 등은 별도 제품 요구가 있을 때 다룸

## 14. 새 작업을 시작할 때

`AGENTS.md` 순서를 따릅니다.

1. `README.md`
2. `docs/STATE.md`
3. `docs/PRODUCT.md`
4. `docs/DECISIONS.md`
5. `docs/ARCHITECTURE.md`
6. `docs/DEVELOPMENT.md`
7. `docs/REFERENCE_POLICY.md`
8. 관련 코드 / tests / issues / PR

현재 코드가 존재한다는 이유만으로 그 동작을 공식 제품 요구사항으로 추정하지 않습니다. 사용자 확정 요구사항과 공식 문서가 우선합니다.
