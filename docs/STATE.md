# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 상태

**v0.1.6 PUBLIC RELEASE — Quest prerequisite semantics correction / Windows x64**

현재 공개 릴리즈는 **v0.1.6**입니다.

```text
release baseline: 0e4683409b62fd326c5605f1485be896e2216836
Desktop ProductVersion: 0.1.6
Content schema: v6
Readable Content schemas: v3, v4, v5, v6
user.db SQLite schema: v1
candidate CI: 31872459229 — SUCCESS
release workflow: 31872620863 — SUCCESS
public asset: Junhyun-Helper-v0.1.6-win-x64.zip
public SHA-256: be642e076d265944282ff3edd3a91323e57ced702e839b3111a0779884fd0111
release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.6
```

공개 ZIP과 `SHA256SUMS.txt`는 릴리즈 후 다시 다운로드해 SHA-256을 재검증했습니다. Release는 draft/prerelease가 아닌 정식 공개 상태입니다.

상세:

- `docs/RELEASE_0.1.6.md`
- `docs/QUEST_PREREQUISITE_SEMANTICS.md`
- `docs/QUEST_PREREQUISITE_AUDIT_2026-08-15.md`
- `docs/QUEST_FAILURE_ANALYSIS.md`
- `docs/DECISIONS.md` DEC-043

---

## v0.1.6 — Quest prerequisite 기준

### 일반 prerequisite

- 서로 다른 `taskRequirements` 항목은 AND
- 한 requirement의 `status[]`는 OR
- `complete` = 해당 Quest 완료
- `active` = 해당 Quest가 진행 상태에 도달
- `failed` = 해당 Quest 실패
- 별도 `수주 가능` 상태를 만들지 않음
- `DEC-010` 유지: 게임에서 받을 수 있는 Quest는 Helper에서 즉시 수락한 것으로 간주
- source가 직접 제공한 prerequisite 상태는 compatibility overlay가 덮어쓰거나 더 강한 상태로 바꾸지 않음

### BTR Driver

- `A Helping Hand = Active` 의미를 보존
- source에 직접 gate가 있으면 그대로 사용
- BTR Driver 후속 Quest에서 gate가 빠진 경우에만 `A Helping Hand = Active` 보강
- A Helping Hand 완료 뒤 이미 열린 BTR Quest를 다시 잠그지 않음

### Ref

- source가 직접 제공한 gate는 보존
- 누락된 Ref 후속 Quest에만 GameMode별 검증된 unlock Quest `Complete` gate 보강
- 현재 GameMode에 unlock Quest가 없으면 dangling prerequisite를 만들지 않음

### Lightkeeper

Lightkeeper는 최초 접근 후 DSP transmitter 상태 변화로 접근을 잃고 Make Amends 계열로 복구할 수 있으므로 ordinary monotonic prerequisite와 분리합니다.

- `QuestSpecialTraderAccessRequirement` 사용
- 최초 접근은 Getting Acquainted 완료에서 자동 추론
- 최초 unlock이 아직 종결되지 않았을 때 수동 접근 동기화로 우회 불가
- Getting Acquainted가 완료 또는 실제 영구 실패로 종결된 뒤에만 실제 접근 상실/복구를 sparse user fact로 저장 가능
- `GameProfileSnapshot.SpecialTraderAccessOverrides`에 trader id별 bool 저장
- key 없음 = 자동 추론
- `false` = 실제 접근 상실
- `true` = 실제 접근 복구
- 평상시 별도 설정으로 관리하지 않음
- 해당 특수 상황에서만 Quest 상세 UI에 `접근 상실 기록` / `접근 복구 기록` action 노출
- recoverable 접근 상실은 영구 불가능이 아니므로 `Unavailable`이 아니라 `Locked`

### 실패 / 불명확 availability

기존 보수적 정확도 정책을 유지합니다.

- 다른 Quest 완료로 확정되는 sibling failure는 자동 추론
- 프로그램이 알 수 없는 비재시작형 영구 실패만 사용자 입력
- 재시작 가능한 raid failure는 영구 저장하지 않음
- `globalVariable` / `dialogue`처럼 프로그램이 증명할 수 없는 availability는 `확인 필요(Indeterminate)` 유지
- 실제 게임 완료 시각이 필요한 delay에는 가짜 countdown을 만들지 않음
- Battery Change처럼 upstream 자체가 의심스러운 데이터는 근거 없이 임의 보정하지 않음

---

## Content / User Progress 호환성

```text
Current Content schema: v6
Readable Content schemas: v3, v4, v5, v6
user.db SQLite schema: v1 unchanged
v0.1.5 → v0.1.6 mandatory data update: none
```

v3~v5 `content.db`는 네트워크 업데이트 없이도 읽을 수 있습니다. 읽는 시점에 legacy special-trader semantics를 메모리에서 정규화합니다.

- 과거 BTR 강제 Complete → Active compatibility gate
- 과거 Lightkeeper Getting Acquainted ordinary Complete gate → recoverable special access gate
- Ref Complete 의미 유지

다음 정상 `데이터 업데이트`가 성공하면 v6 snapshot으로 저장합니다.

`user.db`는 SQLite schema v1을 유지합니다. special trader override는 optional JSON property로 저장하므로 기존 Profile / Quest 완료·실패 / Inventory / Hideout 진행을 파괴하는 migration이 없습니다.

## 데이터 검증

`GameContentValidator`가 다음 Quest graph 오류를 candidate activation 전에 fatal로 차단합니다.

- 빈 prerequisite status
- self prerequisite
- 동일 prerequisite target 중복
- missing prerequisite Quest
- dependency cycle
- special trader access 빈 status
- missing/mismatched special trader
- self unlock Quest
- missing unlock Quest
- ordinary prerequisite와 special access가 같은 unlock Quest를 중복 평가

이 검사는 현재 live source가 깨졌다는 의미가 아니라 향후 Tarkov 패치 데이터 변경에서 조용한 오판을 막기 위한 update-resilience gate입니다.

---

## v0.1.6 공개 검증

```text
release baseline: 0e4683409b62fd326c5605f1485be896e2216836
candidate CI: 31872459229 — SUCCESS
release workflow: 31872620863 — SUCCESS
Desktop ProductVersion: 0.1.6+0e4683409b62fd326c5605f1485be896e2216836
automated tests: 190 passed / 0 failed / 0 skipped
Windows x64 self-contained single-file publish: SUCCESS
startup + Main Map + Factory + MiniMap runtime smoke: SUCCESS
graceful shutdown + clean portable root: SUCCESS
public ZIP re-download + SHA-256 verification: SUCCESS
public SHA-256: be642e076d265944282ff3edd3a91323e57ced702e839b3111a0779884fd0111
```

임시 `.github/workflows/release-v0.1.6.yml`은 공개 검증 후 제거했습니다. 상시 workflow는 `ci.yml`만 유지합니다.

---

## 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / `확인 필요` 분리 / v0.1.6 prerequisite semantics 공개 완료 |
| Hideout | 구현 완료 / current live validation 통과 |
| Needed Items / Inventory | 구현 완료 / flexible status + Item Wiki |
| Ammo | 구현 완료 / current live validation 통과 |
| Map + MiniMap | 구현 완료 / v0.1.5 안정화 기준 유지, v0.1.6 회귀 smoke 통과 |
| Scanner | `준비 중` placeholder 탭 유지 / 실제 기능 PRODUCT OPEN |

## Map 기준

Map subsystem은 독립이고 Quest만 JunhyunHelper current profile/content와 연결합니다. pinned submodule revision은 `d933792b6042a51cea38dc44b686a096fe30de67`입니다.

v0.1.5에서 확정한 Map 기준은 v0.1.6에서도 그대로 유지됩니다.

- floor는 marker visibility filter가 아니라 presentation relation
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
