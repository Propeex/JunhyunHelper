# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 상태

**PUBLIC RELEASE: v0.1.5 / DEVELOPMENT MAIN: Quest prerequisite semantics correction complete**

현재 사용자에게 공개된 릴리즈는 **v0.1.5**이며 그대로 유지합니다. 이번 Quest prerequisite 작업은 사용자가 릴리즈가 아니라 설계/정확도 점검으로 시작했고, 이후 구현을 승인했으므로 **개발 `main`에만 반영했으며 새 Release/Tag/Asset은 만들지 않았습니다.**

### 공개 릴리즈 v0.1.5

```text
release baseline: 2ff504c24661b6e37ec40e685dd344ce5581350f
Desktop ProductVersion: 0.1.5
released Content schema: v5
released user.db SQLite schema: v1
release workflow: 31864223946 — SUCCESS
public asset: Junhyun-Helper-v0.1.5-win-x64.zip
public SHA-256: 565bf0ad01ac9ec8385e99b26aa692e0962550a0c975a889e4b56ad33a6a41f7
release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.5
```

v0.1.5 공개 변경의 상세 기준은 다음 문서에 있습니다.

- `docs/RELEASE_0.1.5.md`
- `docs/FEEDBACK_2026-08-15_OFF_FLOOR_MARKER_FLICKER.md`
- `docs/FEEDBACK_2026-08-15_MINIMAP_FLOOR_CENTER_RESET.md`
- `docs/MAP_PRODUCT_REQUIREMENTS.md`
- `docs/DECISIONS.md` DEC-041 / DEC-042

---

## Development main — Quest prerequisite semantics

2026-08-15 후속 감사에서 일반 prerequisite 계산 구조는 유지하고, 특수 상인 접근 보강의 잘못된 의미만 분리/수정했습니다.

상세 canonical 문서:

- `docs/QUEST_PREREQUISITE_AUDIT_2026-08-15.md`
- `docs/QUEST_PREREQUISITE_SEMANTICS.md`
- `docs/QUEST_FAILURE_ANALYSIS.md`
- `docs/CONTENT_STORAGE.md`

### 확정된 일반 Quest prerequisite 규칙

- 서로 다른 `taskRequirements`는 AND
- 한 requirement 안의 `status[]`는 OR
- `complete` = 완료
- `active` = 진행 상태에 도달
- `failed` = 실패
- `DEC-010` 유지: 별도 `수주 가능` 상태를 만들지 않고, 게임에서 받을 수 있는 Quest는 Helper에서 즉시 수락한 것으로 간주
- 따라서 active 시점에 이미 열렸을 후속 Quest는 선행 Quest 완료 때문에 다시 잠기지 않음
- level / faction / prestige / trader standing / trader loyalty는 각자의 독립 AND gate
- `globalVariable`, `dialogue`, 실제 게임 완료 시각이 필요한 delay처럼 증명할 수 없는 조건은 `확인 필요(Indeterminate)` 유지

### BTR Driver 수정

기존 compatibility overlay는 모든 BTR Driver Quest에 `A Helping Hand = Complete`를 강제했고, raw source가 이미 `Active`를 제공하는 `Shipping Delay - Part 2`조차 `Complete`로 덮어썼습니다.

현재 development main:

- upstream이 직접 prerequisite를 제공하면 **절대 덮어쓰지 않음**
- BTR Driver 후속 Quest에서 gate가 빠진 경우에만 `A Helping Hand = Active`를 추가
- `DEC-010` 자동 수락 모델과 결합하여 A Helping Hand가 열리는 시점부터 BTR 접근 가능
- A Helping Hand 완료 뒤 이미 열린 BTR Quest를 다시 잠그지 않음

### Ref

- source가 직접 제공한 gate는 보존
- 누락된 Ref 후속 Quest에만 GameMode별 검증된 unlock Quest `Complete` gate를 추가
- 현재 mode에 unlock Quest가 없으면 dangling prerequisite를 만들지 않음

### Lightkeeper

Lightkeeper는 최초 해금 후 DSP transmitter 상태에 따라 접근을 잃고 Make Amends 계열로 복구할 수 있으므로, `Getting Acquainted = Complete`를 모든 후속 Quest에 영구 ordinary prerequisite로 두지 않습니다.

현재 development main:

- `QuestSpecialTraderAccessRequirement`로 ordinary prerequisite와 분리
- 최초 접근은 Getting Acquainted 완료에서 자동 추론
- 최초 unlock이 아직 종결되지 않았을 때 수동 접근 동기화로 진행을 우회할 수 없음
- Getting Acquainted가 완료 또는 실제 영구 실패로 종결된 뒤에만 실제 게임 접근 상실/복구를 sparse user fact로 기록 가능
- `GameProfileSnapshot.SpecialTraderAccessOverrides`에 trader id별 bool 저장
- key가 없으면 자동 추론
- 실제 접근 상실은 `false`, 실제 접근 복구는 `true`
- 평상시 관리 설정이 아니며 해당 상황에서만 Quest 상세 UI에 contextual `접근 상실 기록` / `접근 복구 기록` action 노출
- recoverable 접근 상실은 영구 불가능이 아니므로 `Unavailable`이 아니라 `Locked`

### 실패 분기

기존 실패 설계를 유지합니다.

- 다른 Quest 완료로 확정되는 sibling failure는 자동 추론
- 프로그램이 알 수 없는 비재시작형 영구 실패만 사용자 입력
- 재시작 가능한 raid failure는 영구 저장하지 않음
- Getting Acquainted 영구 실패 → Make Amends 진입 → 실제 접근 복구 후 Lightkeeper access override 동기화 가능

### Content schema / 저장 호환성

Development main의 최신 Content schema는 **v6**입니다.

```text
Current Content schema: v6
Readable Content schemas: v3, v4, v5, v6
user.db SQLite schema: v1 unchanged
```

v3~v5 content snapshot은 네트워크 업데이트 없이 읽는 시점에 메모리에서 legacy special-trader overlay를 정규화합니다.

- 과거 BTR 강제 Complete → Active compatibility gate
- 과거 Lightkeeper Getting Acquainted ordinary Complete gate → recoverable special access gate
- Ref Complete 의미 유지

다음 정상 `데이터 업데이트`가 성공하면 v6 snapshot으로 새로 저장합니다.

`user.db`는 table/schema migration 없이 optional JSON property로 special trader override를 저장하므로 기존 DB를 파괴하지 않습니다.

### Data validation hardening

`GameContentValidator`가 다음 Quest graph 오류를 candidate activation 전에 fatal로 차단합니다.

- 빈 prerequisite status
- self prerequisite
- 동일 prerequisite target 중복
- missing prerequisite Quest
- dependency cycle
- special trader access의 빈 status
- missing/mismatched special trader
- self unlock Quest
- missing unlock Quest
- ordinary prerequisite와 special access가 같은 unlock Quest를 중복 평가

이 검사는 현재 live source가 깨졌다는 뜻이 아니라, 향후 Tarkov 패치 데이터 변경에서 조용한 오판을 막기 위한 update-resilience gate입니다.

### Development 검증

제품 코드 기준 검증 commit:

```text
code validation baseline: 0d04edbf4ca8869fc109314d307a49dc17b3acdf
CI: 31871828046 — SUCCESS
automated tests: 190 passed / 0 failed / 0 skipped
Desktop Release build: SUCCESS
Windows x64 self-contained single-file publish candidate: SUCCESS
startup + full Map/Factory/MiniMap runtime smoke: SUCCESS
graceful shutdown + clean portable root smoke: SUCCESS
```

CI publish artifact는 개발 검증용일 뿐 public Release가 아닙니다.

---

## Quest / Game Content 기준

Quest를 포함한 Game Content 분류는 `데이터 업데이트` 시 프로그램 importer가 수행합니다. Runtime GPT/AI 의존성은 없습니다.

현재 development main 정확도 기준:

- 일반 `taskRequirements active / complete / failed` 의미 보존
- source prerequisite 우선 / compatibility overlay는 누락만 보강
- BTR Active semantics
- Ref mode-aware Complete semantics
- Lightkeeper recoverable special access 분리
- `globalVariable` / `dialogue` unresolved condition은 `확인 필요`
- `availableDelaySecondsMin/Max` canonical 보존, 가짜 countdown 없음
- Content schema v6 / v3~v6 readable

2026-08-15 감사 당시 live product importer/validator 규모:

```text
regular:    517 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
pve:        513 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
pvp-season: 490 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
```

이번 변경은 그 live 감사에서 발견한 compatibility-overlay 의미 오류를 수정하며, Battery Change의 의심스러운 upstream failure 데이터는 근거 없이 임의 보정하지 않습니다.

## 업그레이드 정책

### 공개 v0.1.5 기준

- **v0.1.4 → v0.1.5:** 필수 `데이터 업데이트` 없음
- `%LocalAppData%/JunhyunHelper/user.db`의 Profile / Quest 완료 / Inventory / Hideout 진행 유지

### 현재 development main 기준

- 과거 v3~v5 `content.db`는 앱 시작 시 읽을 수 있으며 special-trader semantics를 메모리에서 정규화하므로 이 수정만을 위해 즉시 데이터 업데이트를 강제하지 않음
- 새 데이터 업데이트 성공 시 v6 content 저장
- user.db SQLite schema v1 유지

## 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / `확인 필요` 분리 / prerequisite semantics 보강 완료 |
| Hideout | 구현 완료 / current live validation 통과 |
| Needed Items / Inventory | 구현 완료 / flexible status + Item Wiki |
| Ammo | 구현 완료 / current live validation 통과 |
| Map + MiniMap | 구현 완료 / v0.1.5 off-floor marker + MiniMap floor viewport regression patch 공개 완료 |
| Scanner | `준비 중` placeholder 탭 유지 / 실제 기능 PRODUCT OPEN |

## Map 기준

Map subsystem은 독립이고 Quest만 JunhyunHelper current profile/content와 연결합니다. pinned submodule revision은 `d933792b6042a51cea38dc44b686a096fe30de67`입니다.

v0.1.5 Map 기준은 계속 유지됩니다.

- floor는 visibility filter가 아니라 presentation relation
- enabled 타층 일반 marker는 same-type/near-XZ라도 각각 유지
- current/above/below compact ring + known off-floor opacity
- semantic duplicate extract 정규화 유지
- Main Map / MiniMap floor 변경 시 live zoom + map-space viewport center 보존

## 비차단 후속 범위

- Scanner 실제 기능 설계/구현
- Map artwork/config/general-marker atomic bundle updater
- deeper pinned Map renderer refactor only when concrete regression/performance value justifies the risk
- code signing / installer / application updater
- user.db backup/restore UX
- repository license / third-party notice 정책

## 현재 릴리즈 정책

이번 Quest prerequisite 수정은 development main에 반영되어 있지만 **새 public release는 생성하지 않았습니다.** 공개 버전은 계속 v0.1.5입니다. 다음 release는 별도 제품 변경 묶음이나 사용자가 릴리즈를 원할 때 진행합니다.
