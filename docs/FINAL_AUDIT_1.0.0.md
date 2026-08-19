# FINAL AUDIT — v1.0.0 정식 안정판

상태: **`PUBLIC VERIFIED / COMPLETE`**

기준일: 2026-08-19

## 0. 최종 결과

v0.1.14의 사용자-visible 기능을 유지하면서 내부 하드닝, release reproducibility, 실제 Windows runtime 검증, 개발자 문서화를 완료하고 `v1.0.0`을 public latest stable로 릴리즈했습니다.

```text
Exact release source: 3147ad1b48c3d30df529d95b148c5c444a77d649
Release workflow run: 32219746319
Release workflow head: 312ef59a0f50bf3df43c9ebbc79e8a965d35d688
Automated tests: 232 passed / 0 failed / 0 skipped
Public ZIP: Junhyun-Helper-v1.0.0-win-x64.zip
ZIP bytes: 74,088,334
SHA-256: 0e92787409add9dd9e1138277c3588586a04266b05ca56d7cf7fb6f79c88094c
Draft: false
Prerelease: false
Latest stable: true
Public-downloaded executable smoke: passed
Removed v0.x Releases: 15
Remaining v0.x Releases: 0
```

Scanner는 기존 제품 계약대로 visible `준비 중` placeholder를 유지합니다.

---

# 1. 감사 원칙

허용:

- 사용되지 않는 first-party 내부 surface 제거
- 반복 I/O/계산 제거
- 예외/복구 경계 강화
- build/release reproducibility 강화
- version/package identity drift 차단
- 실제 release gate가 검출한 기존 제품 계약 위반의 최소 수정
- 구현 추적성/개발자 문서 강화

금지:

- 확정 기능 삭제/축소
- Quest/Hideout/Items 의미 임의 변경
- Scanner 실제 구현
- Map donor broad refactor
- unknown game fact 추측
- UI redesign
- smoke threshold를 낮춰 회귀를 숨기는 행위

---

# 2. Core 감사

검토:

- Profile canonical model
- Quest availability / failure / future reachability
- task pool / profile-variable compatibility
- Hideout level / requirement
- Needed Items / flexible requirements / cleanup
- Ammo / reference / content aggregate

결론:

- deterministic domain 계산에서 v1.0.0 blocker가 발견되지 않았습니다.
- `QuestAvailabilityEvaluator`는 missing dependency, cycle, unsupported fact, exact profile variable, special trader access를 fail-closed/Indeterminate 방식으로 처리합니다.
- `QuestFutureReachabilityEvaluator`는 current availability와 future item protection을 분리합니다.
- flexible hand-in은 실제 제출 item을 추측하지 않습니다.

조치:

### obsolete Hideout cleanup compatibility 제거

과거 설계의 잔재였던:

- `CleanupProtectionKind.UnenteredHideoutLevel`
- `UnenteredHideoutStationIds`

를 제거했습니다.

현재 제품 규칙은 **Hideout station progress가 없으면 Lv.0**입니다. 따라서 이 값들은 정상 경로에서 의미가 없었고 제거 후 사용자-visible 계산 결과는 동일합니다.

---

# 3. Application 감사

검토:

- Profile mutation
- Quest complete/fail/undo
- Hideout level mutation
- fixed inventory consumption/restore ledger
- Items workspace/cache

결론:

- authoritative mutation은 Application service에 집중되어 있습니다.
- Quest/Hideout fixed consumption ledger가 중복 차감을 막습니다.
- flexible consumption은 자동 추정하지 않습니다.
- inventory-only mutation은 reusable planning basis를 사용합니다.
- 추가 구조 변경은 위험 대비 실익이 낮아 하지 않았습니다.

---

# 4. Persistence / Infrastructure 감사

## UserProfileStore

기존에는 store의 read/write/delete 경로마다 `CREATE TABLE IF NOT EXISTS profiles` 확인이 다시 수행됐습니다.

수정:

- store instance당 schema initialization 한 번
- concurrent first access는 `SemaphoreSlim` gate
- 성공한 뒤에만 initialized flag
- failure/cancellation 시 retry 가능

효과:

- 반복 SQLite schema I/O 제거
- user.db schema v1 유지
- persisted semantics 변경 없음

## Game Content

다음 안전 계약 유지 확인:

```text
online source
→ canonical build
→ validation
→ candidate DB
→ SQLite integrity/read-back
→ active/previous transaction
→ failed activation rollback
```

Content schema는 v7, readable은 v3-v7이며 v1.0.0에서 schema bump가 필요하지 않았습니다.

## preference / image cache

- atomic JSON + `.bak` recovery
- same-directory temp
- image byte/dimension bounds
- image download concurrency bound
- corrupt cache recovery

계약을 확인했고 추가 변경은 하지 않았습니다.

## Program update

- strict stable version parsing
- trusted GitHub release URL scope
- SHA-256
- ZIP traversal/symlink/duplicate/PDB/unexpected root 차단
- staging validation
- transaction replace + rollback
- LocalAppData user data 분리

을 확인했습니다.

---

# 5. Desktop 감사

## HTTP User-Agent drift 제거

shared online-data `HttpClient`가 과거 `JunhyunHelper/0.1`을 hardcode하고 있었습니다.

수정:

- Desktop assembly version의 major/minor에서 User-Agent 파생
- release version을 별도 상수로 중복 관리하지 않음

## Scanner

- visible `스캐너` tab 유지
- 내용 `준비 중`
- 기능 구현/숨김/삭제 없음

## MainWindow / pages

Profile, Quest, Hideout, Items, Ammo page의 domain truth가 UI event handler에 중복 구현되지 않는지 확인했습니다. MainWindow는 Application service 결과를 화면에 orchestration하는 역할을 유지합니다.

---

# 6. Map donor reproducibility 감사

기존 `.gitmodules`는 과거 작업 fork를 fetch origin으로 사용했습니다. clean GitHub Actions checkout에서 해당 원격이 더 이상 재현되지 않았습니다.

확인:

- 현재 제품 pin: `d933792b6042a51cea38dc44b686a096fe30de67`
- 동일 exact Git object가 public upstream `SIGDrone/Tarkov-Helper`에 존재

조치:

- `.gitmodules` fetch origin만 public upstream으로 변경
- gitlink SHA 유지
- donor source identity 변경 없음

즉, Map source update가 아니라 dependency location 재현성 hardening입니다.

---

# 7. exact-release smoke가 발견한 Map late-suppression race

## 7.1 첫 release attempt

첫 v1.0.0 release-only attempt는 public Release를 만들기 전에 중단됐습니다.

통과:

- donor checkout
- Release build
- 232 automated tests
- single-file publish/package audit

실패:

- actual published EXE Factory Main Map late-state smoke

관찰된 타층 standard marker 예:

```text
Visibility = Visible
Opacity = 0.50
Relation = Above
```

JunhyunHelper 제품 계약은 타층 marker를 숨기지 않고 약 75% opacity + above/below relation으로 유지하는 것입니다.

## 7.2 원인

pinned donor `MapPage.SharedFloor.cs`에 legacy current-floor-only filter가 남아 있었습니다.

```text
ScheduleSharedMarkerFilter()
→ 200 ms timer
→ 최대 12 ticks (~2.4 s)
→ current floor가 아닌 Visible element를
   _sharedFloorHiddenMarkers에 기록
→ Visibility = Collapsed
```

기존 first-party `LegacyStandardMarkerFloorPresentationBridge`의 bounded settle보다 donor filter window가 길었고, map/position/render 변화가 filter window를 다시 예약할 수 있어 최종 state race가 남았습니다.

## 7.3 수정 원칙

- donor revision/pin 변경 없음
- donor source broad rewrite 없음
- smoke 기준 완화 없음
- category/faction visibility 재계산 없음
- permanent full-tree polling 없음

## 7.4 구현

새 first-party partial:

`Map/MapPage.JunhyunCrossFloorMarkerPolicy.cs`

기존 bridge:

`Map/LegacyStandardMarkerFloorPresentationBridge.cs`

동작:

```text
donor filter tick
→ donor가 floor 때문에 숨긴 element를 _sharedFloorHiddenMarkers에 기록
→ current tick 종료 직후 JunhyunHelper callback
→ 해당 set에 있는 element만 Visible 복구
→ set clear
→ existing Junhyun floor presentation Apply()
→ 약 75% opacity + floor relation 재적용
```

핵심 invariant:

- `_sharedFloorHiddenMarkers`는 donor가 floor 때문에 직접 `Visible → Collapsed`한 element의 권위 목록으로 사용
- category/faction/user filter 때문에 이미 hidden인 marker는 복구 대상 아님

Lifecycle:

- donor의 bounded timer에 callback만 부착
- donor timer가 page `Unloaded`에서 null이 된 뒤 `Loaded`에서 재생성되면 callback 재부착
- Dispose 시 callback/Loaded handler 제거 + 남은 floor-only suppression 복구

상세: `MAP_RUNTIME_COMPATIBILITY.md`

## 7.5 수정 후 검증

PR CI에서:

- Release build PASS
- 232 tests PASS
- package audit PASS
- actual published EXE Product UI PASS
- Main Map PASS
- Factory late-state PASS
- MiniMap PASS
- graceful shutdown PASS

smoke는 donor filter window보다 긴 약 3.2초 final-state settle을 유지했습니다.

---

# 8. Packaging / CI 감사

상시 CI gate:

1. pinned Map donor clean checkout
2. Release build
3. full automated tests
4. win-x64 self-contained single-file publish
5. csproj Version 확인
6. ProductVersion version-boundary 확인
7. FIRST_RUN first line exact identity
8. root layout 확인
9. PDB 없음
10. nested archive 없음
11. forbidden legacy dependency 없음
12. actual EXE rendered Product UI smoke
13. Main Map / Factory / MiniMap smoke
14. normal close / process termination
15. runtime portable root pollution 없음

v1.0.0 release-only gate는 여기에 Draft/public asset 재다운로드 검증과 public-downloaded executable smoke를 추가했습니다.

---

# 9. 최종 v1.0.0 release 검증

최종 exact source:

```text
3147ad1b48c3d30df529d95b148c5c444a77d649
```

최종 release workflow:

```text
Run ID: 32219746319
Head SHA: 312ef59a0f50bf3df43c9ebbc79e8a965d35d688
Conclusion: success
```

최종 public asset:

```text
Junhyun-Helper-v1.0.0-win-x64.zip
74,088,334 bytes
SHA-256 0e92787409add9dd9e1138277c3588586a04266b05ca56d7cf7fb6f79c88094c
```

검증:

- release-generated hash와 Draft download hash 일치
- Draft package identity/root 검증 PASS
- public/latest 전환 PASS
- public ZIP 재다운로드 SHA-256 일치
- public ProductVersion 1.0.0 identity gate PASS
- public package root identity PASS
- public-downloaded EXE Product UI + Main Map + Factory + MiniMap + graceful shutdown PASS
- existing v0.x Releases 15개 삭제 PASS
- v0.x remaining 0
- latest stable remains v1.0.0

---

# 10. 개발자 문서 감사

v1.0.0 기준 공식 문서 체계를 강화했습니다.

- `DEVELOPER_REFERENCE.md`
  - layer/dependency
  - authority/data flow
  - subsystem input/output
  - first-party file responsibility catalog
  - change-impact tracing
- `VERSIONING.md`
  - 사용자 확정 버전 정책
- `MAP_RUNTIME_COMPATIBILITY.md`
  - donor legacy runtime filter와 product compatibility 경계
- `RELEASE_1.0.0.md`
  - public release record
- 이 문서
  - 전체 hardening audit와 실패/수정/재검증 기록
- `STATE.md`
  - canonical current state
- `DECISIONS.md`
  - DEC-047~DEC-050 active decisions

새 세션은 `AGENTS.md`의 복구 순서에 따라 이 문서들을 사용합니다.

---

# 11. 의도적으로 남긴 비기능/한계

다음은 “미완성 정리”가 아니라 현재 제품 범위상 의도적으로 유지한 상태입니다.

- Scanner 실제 기능 미구현 — visible `준비 중` placeholder가 정상 상태
- EFT Story Chapters 추측 지원 없음
- unsupported Quest conditions optimistic 해석 없음
- flexible hand-in 실제 소비 Item 자동 추측 없음
- installer/code signing 추가 없음
- Content schema/user.db schema 불필요한 bump 없음
- pinned Map donor broad refactor 없음

---

# 12. 최종 판정

v1.0.0 release gate에서 발견된 실제 Map race까지 수정하고 동일 gate를 처음부터 다시 통과했습니다.

현재 v1.0.0은:

- 사용자-visible 기능 보존
- Core/Application correctness gate 유지
- persistence/network/package drift 하드닝
- Map donor clean reproducibility 확보
- actual Windows runtime late-state 검증 통과
- Draft/public 공급망 검증 통과
- public-downloaded executable smoke 통과
- 기존 0.x Releases 정리 완료
- 개발자용 구현/결정/변경 영향 문서화 완료

상태를 **정식 안정판 `PUBLIC VERIFIED`**로 확정합니다.
