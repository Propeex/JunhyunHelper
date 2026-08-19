# FINAL AUDIT — v1.0.0 정식 안정판

상태: `RELEASE CANDIDATE AUDIT`

기준일: 2026-08-19

기준선:

- v0.1.14 public verified baseline
- main baseline commit: `d44aaa744fa5c6a14f864316ce127899b8c80433`
- v0.1.14 release code commit: `bb0611e9263c24018825a87a58aba2c5474b6cc4`
- v0.1.14 자동 테스트: 232 passed
- v0.1.14 actual Windows publish smoke: rendered Product UI + Main Map + Factory + MiniMap + graceful shutdown passed

목적은 새로운 기능을 추가하는 것이 아니라 **동일한 사용자 동작을 유지한 채 0.x 개발판에서 1.0.0 정식 안정판으로 승격하기 위한 최종 내부 감사**입니다.

---

# 1. 감사 원칙

이번 감사에서 허용한 변경:

- 사용되지 않는 first-party 내부 코드/API 제거
- 반복 I/O/계산 제거
- 예외/복구 경계 강화
- 버전/배포 identity 일치 강화
- 테스트/CI gate 강화
- 문서 추적성 강화
- 실제 release gate가 검출한 기존 제품 계약 위반의 최소 범위 수정

이번 감사에서 금지한 변경:

- 확정 기능 삭제/축소
- Quest/Hideout/Items 의미 변경
- 새로운 Scanner 구현
- Map donor의 광범위 정리
- unknown game fact에 대한 새로운 추측
- UI redesign

---

# 2. Core 감사

검토 영역:

- Profile canonical model
- Quest availability/failure/future reachability
- task pool/profile variable compatibility
- Hideout level/requirements
- Needed Items/flexible requirements/cleanup
- Ammo/reference/content aggregate

결론:

- 현재 deterministic domain 계산에서 v1.0.0을 막는 correctness defect를 발견하지 못했습니다.
- `QuestAvailabilityEvaluator`는 dependency cycle, missing dependency, unsupported fact, exact profile variable, special trader access를 fail-closed/Indeterminate 방식으로 처리합니다.
- `QuestFutureReachabilityEvaluator`는 현재 availability와 미래 필요를 의도적으로 분리하며, 불확실한 Quest item을 보수적으로 보호합니다.
- fixed/flexible item requirement 분리는 현재 제품 계약과 일치합니다.

발견 및 조치:

### 과거 Hideout “미입력 보호” compatibility surface 제거

기존 `FutureNeededItemsPlanner`에는 다음이 남아 있었습니다.

- `CleanupProtectionKind.UnenteredHideoutLevel`
- `UnenteredHideoutStationIds`

현재 제품 규칙은 **Hideout station progress가 없으면 Lv.0**이므로 정상 경로에서 이 값들은 영구히 사용되지 않았습니다. 기능이 아니라 과거 설계의 잔재였으므로 제거했습니다.

사용자-visible 결과는 동일합니다.

- missing station → Lv.0
- 그 station의 모든 future level 재료 → Needed Items에 포함
- 실제 fixed requirement보다 초과인 수량 → cleanup 계산 가능

Map/Quest/Inventory 계약에는 영향이 없습니다.

---

# 3. Application 감사

검토:

- `ProfileApplicationService`
- `QuestApplicationService`
- `HideoutApplicationService`
- `ItemsApplicationService`
- `FixedInventoryConsumptionPolicy`

결론:

- authoritative mutation은 Application service에 집중되어 있습니다.
- Quest complete/undo와 Hideout upgrade/rollback의 consumption ledger가 중복 차감을 막습니다.
- flexible hand-in은 실제 제출 item을 추측하지 않습니다.
- Items inventory-only mutation은 reusable planning basis를 사용합니다.
- immutable snapshot reference cache는 동일 snapshot 재평가를 줄이며 제품 의미를 캐시에 의존시키지 않습니다.

추가 구조 변경은 위험 대비 효과가 낮아 하지 않았습니다.

---

# 4. Infrastructure 감사

## UserProfileStore

발견:

`CREATE TABLE IF NOT EXISTS profiles`가 profile store의 매 read/write/delete 경로에서 다시 실행되고 있었습니다. 기능상 문제는 아니지만 같은 process에서 schema가 이미 준비된 이후에도 별도 SQLite connection/statement I/O가 발생합니다.

조치:

- process/store instance당 schema initialization을 한 번만 수행
- `SemaphoreSlim`으로 concurrent first access 직렬화
- 성공한 경우에만 initialized flag 설정
- cancellation/failure는 retry 가능 상태 유지

효과:

- 반복 profile/inventory/quest mutation의 불필요한 SQLite schema I/O 제거
- DB schema와 저장 semantics는 변경 없음

## Content storage/update

검토 결과:

- candidate-first
- canonical validation
- SQLite integrity/read-back
- active/previous transaction
- failed activation rollback

계약이 유지되고 있습니다. 추가 변경 없음.

## Atomic preference storage

same-directory temp + durable write + previous readable backup 유지 구조가 적절합니다. 추가 변경 없음.

## Image cache

- byte limit
- dimension limit
- decode/PNG normalization
- concurrent download limit
- corrupt cache recovery

모두 적절합니다. 추가 변경 없음.

## Program update

release parsing, URL trust, SHA-256, archive traversal/symlink/duplicate/PDB 차단, staging validation, durable copy, previous-file rollback을 검토했습니다. v1.0.0 blocker를 찾지 못했습니다.

---

# 5. Desktop 감사

검토:

- `App` startup/fatal diagnostics/update apply mode
- `MainWindow` profile/content/workspace lifecycle
- Profile/Quest/Hideout/Items/Ammo page 경계
- image/favorite services
- program update consent UX
- Map product bridge/lifecycle/smoke

발견 및 조치:

### HTTP User-Agent의 과거 버전 하드코딩 제거

`DesktopServices` shared HTTP client가 `JunhyunHelper/0.1`을 고정해서 사용하고 있었습니다. 기능에는 영향이 없지만 1.x 이후에도 네트워크 요청이 0.1로 식별되는 maintenance drift였습니다.

조치:

- Desktop assembly version의 `major.minor`에서 User-Agent를 파생
- 버전 정보를 별도 상수로 복제하지 않음

### Scanner

Scanner는 기존 결정대로 visible `준비 중` placeholder를 유지합니다. 구현/숨김/삭제하지 않았습니다.

### Map — donor current-floor-only late suppression 제거

초기 v1.0.0 hardening에서는 pinned donor source를 변경하지 않고 first-party bridge와 smoke contract만 확인했습니다. 이후 exact release baseline을 다시 publish해 실행한 release-only smoke에서 Factory 전환 후 타층 standard marker가 최종적으로 `Opacity=0.50`에 남는 release-blocking 상태를 검출했습니다.

진단 결과:

- JunhyunHelper 제품 계약은 **floor = presentation only**이며 타층 marker를 숨기지 않고 약 75% opacity + 위/아래 방향 표시를 사용합니다.
- first-party `LegacyStandardMarkerFloorPresentationBridge`는 이 계약대로 75% presentation을 적용하고 있었습니다.
- 그러나 pinned donor `MapPage.SharedFloor.cs`에는 과거 current-floor-only filter가 남아 있었습니다.
- donor는 map/floor/position/render 변화 때 `ScheduleSharedMarkerFilter()`를 예약하고 200ms 간격으로 최대 12회 marker container를 다시 검사합니다.
- 타층 element가 그 시점에 Visible이면 donor의 `_sharedFloorHiddenMarkers`에 기록한 뒤 `Collapsed`로 바꿉니다.
- 따라서 first-party 90ms bounded settle이 끝난 뒤에도 donor가 늦게 visibility를 다시 덮어쓸 수 있으며 position update가 해당 window를 다시 시작할 수도 있었습니다.
- 이 race는 일반 PR smoke에서는 재현되지 않았지만 exact release workflow의 실제 Windows EXE smoke에서 재현되어 public Release 생성 전에 차단되었습니다.

수정 원칙:

- donor revision/pin 자체는 변경하지 않습니다.
- smoke threshold를 낮추지 않습니다.
- category/faction visibility를 다시 추론하지 않습니다.
- donor가 **floor 때문에 Visible → Collapsed로 직접 변경한 element만** `_sharedFloorHiddenMarkers`를 권위 목록으로 사용해 각 donor filter tick 직후 복구합니다.
- 복구 직후 기존 JunhyunHelper floor presentation bridge를 다시 호출해 standard marker의 75% opacity/층 관계 표시를 최종 상태로 고정합니다.
- donor의 bounded timer에 post-filter callback만 부착하므로 새로운 영구 full-tree polling을 추가하지 않습니다.

구현:

- `Map/MapPage.JunhyunCrossFloorMarkerPolicy.cs`
- `Map/LegacyStandardMarkerFloorPresentationBridge.cs`

이 변경은 Map 기능 추가가 아니라 이미 확정된 타층 marker 표시 계약의 race-condition 수정입니다.

---

# 6. Packaging / CI 감사

기존 CI가 이미 다음을 검증했습니다.

- Release build
- full unit/integration tests
- win-x64 self-contained single-file publish
- `준현 헬퍼.exe`, `FIRST_RUN_KO.txt`, `Assets/` root contract
- no root DLL
- no PDB
- forbidden legacy dependencies 없음
- actual EXE rendered UI smoke
- Main Map / Factory / MiniMap smoke
- normal close/process exit
- runtime portable root pollution 없음

v1.0.0에서 추가한 gate:

- Desktop csproj `<Version>`을 release identity source로 읽음
- published EXE ProductVersion과 project version 일치 확인
- `FIRST_RUN_KO.txt` header version 일치 확인
- release tree 내부 nested `.zip/.7z/.rar` 차단
- smoke 단계 이름/설명을 실제 Product UI + Map 검증 범위와 일치시킴

release-only exact-baseline smoke는 실제로 PR CI에서 간헐적으로 보이지 않았던 donor Map late-suppression race를 검출했고 public Release 생성 전에 작업을 중단시켰습니다. 따라서 v1.0.0의 실제 release gate는 단순 중복 검증이 아니라 독립적인 최종 안정성 검증으로 유지합니다.

목적은 “코드는 1.0.0인데 안내문/실행 파일/패키지는 다른 버전” 또는 “일반 CI에서는 지나갔지만 실제 release package의 late runtime state가 제품 계약을 위반”하는 상황을 차단하는 것입니다.

---

# 7. 문서 감사

v1.0.0에서 추가/강화:

- `docs/DEVELOPER_REFERENCE.md`
  - layer/dependency
  - authority/data flow
  - startup/mutation/update flow
  - subsystem inputs/outputs
  - first-party file responsibility catalog
  - 변경 영향 추적법
  - 실패 경계/성능 구조
- `docs/VERSIONING.md`
  - 사용자 확정 버전 규칙
- `docs/RELEASE_1.0.0.md`
  - v1.0.0 release contract
- 이 문서
  - 실제 최종 감사 기록

릴리즈 후 `STATE.md`와 `DECISIONS.md`를 public verification 상태로 갱신합니다.

---

# 8. 의도적으로 손대지 않은 영역

다음은 “미처 정리하지 못한 것”이 아니라 **정식판 안정성을 위해 의도적으로 그대로 둔 것**입니다.

- pinned Map donor의 broad refactor — 단, release smoke가 검출한 구체적인 legacy visibility race는 first-party compatibility layer에서 최소 수정
- Scanner 실제 구현
- Story Chapters 추측 지원
- unsupported Quest condition optimistic support
- flexible hand-in 자동 소비 추측
- installer/code signing 추가
- content schema bump
- user.db schema bump

---

# 9. 릴리즈 전 남은 gate

이 문서를 처음 작성한 시점에서 남은 작업:

1. v1.0.0 branch 전체 CI 통과
2. PR 최종 diff 검토
3. main 병합
4. exact release baseline SHA 확정
5. v1.0.0 Windows package 생성
6. draft Release asset checksum/ProductVersion/root/smoke 검증
7. public/latest 전환
8. public asset 재다운로드 검증
9. 기존 모든 `v0.*` GitHub Release 제거
10. public v1.0.0이 latest이고 assets가 유효한지 최종 확인
11. release-only workflow 정리
12. `STATE.md`, `DECISIONS.md`, 이 audit 문서에 최종 SHA/hash/test 결과 기록

release-only smoke에서 blocker가 발견되면 public Release를 만들지 않고 수정 후 **새 exact baseline으로 전체 gate를 처음부터 다시 실행**합니다. 이 원칙에 따라 첫 v1.0.0 release attempt는 public state를 변경하지 않은 채 중단되었고 Map compatibility fix 후 재검증합니다.

이 단계 중 하나라도 실패하면 v1.0.0 완료로 기록하지 않습니다.
