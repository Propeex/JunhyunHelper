# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 제품 목적

준현 헬퍼는 Escape from Tarkov 플레이를 지원하는 Windows 데스크톱 프로그램입니다.

핵심 구조:

- 온라인 Tarkov 데이터를 프로그램이 직접 다운로드
- 외부 형식을 검증하고 canonical 데이터로 변환
- candidate DB를 만든 뒤 관계/read-back 검증이 끝난 경우에만 active 데이터 교체
- Quest / Hideout 데이터를 통해 Needed Items 계산
- Ammo 정보 제공
- Map / MiniMap 제공
- runtime GPT/AI 의존성 없음

기존 `Propeex/Tarkov-Helper`는 공식 요구사항이 아니라 Map/MiniMap의 검증된 코드·자산을 참고/재사용하기 위한 prototype입니다.

---

## 현재 공개 상태

**v0.1.12 PUBLIC RELEASE / VERIFIED — Windows x64**

```text
release tag: v0.1.12
release baseline: cfacee6cfa893932d74d6a71725b6c711282981e
Desktop ProductVersion: 0.1.12+cfacee6cfa893932d74d6a71725b6c711282981e
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
feature correction PR: #94
feature correction PR CI: 32022249988 — SUCCESS
feature correction main CI: 32022514487 — SUCCESS
release candidate PR: #95
release candidate PR CI: 32025523609 — SUCCESS
release baseline main CI: 32025837427 — SUCCESS
release workflow: 32026123215 — SUCCESS
automated tests: 210 passed / 0 failed / 0 skipped
public asset: Junhyun-Helper-v0.1.12-win-x64.zip
public asset size: 74,067,018 bytes
public SHA-256: bc91f17f94c6554d09da3fed6db6ebb679c6e1d57ff7017d4a624e8dcd8eae89
public release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.12
```

공개 ZIP은 Release 생성 뒤 다시 다운로드하여 크기와 SHA-256을 재검증했습니다.

Release는:

- `draft=false`
- `prerelease=false`
- target commit = exact release baseline

상세: `docs/RELEASE_0.1.12.md`

---

## v0.1.12의 핵심 의미

v0.1.11까지 여러 차례 UI 수정이 소스상으로는 반영됐지만 사용자 실제 화면에서 그대로인 문제가 반복됐습니다.

v0.1.12부터 해당 UI 계약은 **소스 문자열/빌드 성공만으로 완료 처리하지 않습니다.**

실제 publish된 Windows 실행 파일에서 WPF `Measure` / `Arrange` 결과를 검사합니다.

현재 rendered UI gate:

- Flexible candidate Grid가 실제 row 폭으로 확장
- icon/name 좌측 축 확인
- FIR/general 우측 축 확인
- Ammo favorite 실제 Content가 단일 `☆`/`★`
- Ammo detail actual state가 expanded=`▼`, collapsed=`▲`
- marker/check 조합이 다른 Map Quest 3개의 title 시작 X 편차 `<= 0.75px`
- expanded Map Quest sidebar handle right gap `<= 6px`

같은 published-app smoke에서 Main Map / Factory / MiniMap / 정상 종료도 검증합니다.

릴리즈 실행에서 명시적으로:

```text
PUBLISHED_RENDERED_UI_MAP_SMOKE=true
PUBLIC_RELEASE_VERIFIED=true
```

를 확인했습니다.

상세: `docs/RENDERED_UI_ALIGNMENT_FIX_2026-08-17.md`, `docs/RELEASE_0.1.12.md`

---

## Quest prerequisite / availability 기준

### 일반 prerequisite

- 서로 다른 `taskRequirements` 항목은 AND
- 한 requirement의 `status[]`는 OR
- `complete` = 해당 Quest 완료
- `active` = 해당 Quest가 진행 상태에 도달
- `failed` = 해당 Quest 실패
- 별도 `수주 가능` 상태를 만들지 않음
- `DEC-010`: EFT에서 받을 수 있는 Quest는 Helper에서 즉시 수락한 것으로 간주
- source가 직접 제공한 prerequisite 상태는 compatibility overlay가 더 강한 상태로 덮어쓰지 않음

### BTR Driver / Ref / Lightkeeper

- BTR Driver는 `A Helping Hand = Active` 의미를 보존하고 누락된 후속 Quest만 보강
- Ref는 source gate를 보존하고 누락된 후속 Quest만 GameMode별 검증된 Complete gate로 보강
- Lightkeeper는 ordinary prerequisite와 recoverable access state를 분리
- recoverable 접근 상실은 영구 `Unavailable`이 아니라 `Locked`

상세: `docs/QUEST_PREREQUISITE_SEMANTICS.md`

### EFT profile-variable / trader task-pool

`globalVariable` requirement는 `variableId / operator / value`를 canonical Content에 구조적으로 보존합니다.

판정 우선순위:

1. exact current profile variable 값이 있으면 그 값이 권위값
2. exact 값이 없고 current EFT 1.1 audited task-pool 구조가 완전히 일치하면 current-version compatibility
3. 어느 쪽도 증명할 수 없으면 `확인 필요(Indeterminate)`

2026-08-17 감사 기준:

```text
globalVariable Quest: 162
unique trader-local task-pool variable: 27
audited LL2~LL4 Quest: 114
LL1 pool Quest: 48
```

LL2~LL4 compatibility는 정확한 variable ID / trader / pool Quest count / threshold set / direct same-trader LL seed count가 모두 감사값과 일치할 때만 적용합니다.

LL1은 다음 pristine 초기 상태만 counter 0으로 확정합니다.

- audited 구조 일치
- 현재 trader LL1
- 해당 trader 완료 Quest 0개

진행된 LL1 값을 exact fact 없이 추측하지 않습니다.

상세: `docs/QUEST_TASK_POOL_AUDIT_2026-08-17.md`, `docs/DECISION_TASK_POOL_RUNTIME_COMPATIBILITY_2026-08-17.md`

### Dialogue availability

2026-08-17 live feed의 `dialogue` Quest 12건은 전수 감사했습니다.

- 정확히 검증된 12개 Quest ID에만 compatibility 적용
- 시작 Quest 3개는 opaque dialogue gate 제거
- 나머지 9개는 검증된 prerequisite/minimum level 복원
- Introduction은 Gunsmith - MP-133 `Active` 의미 유지
- upstream이 ordinary `taskRequirements`를 제공하면 source 우선
- 새로운/변경된 dialogue는 추측하지 않고 `확인 필요`

상세: `docs/DIALOGUE_GATE_AUDIT_2026-08-17.md`

### 불명확 availability

- 재시작 가능한 raid failure는 영구 저장하지 않음
- 검증되지 않은 새 dialogue는 `확인 필요`
- 진행된 LL1 task-pool은 exact current 값이 없으면 `확인 필요`
- 실제 완료 시각 기반 availability delay는 completion timestamp가 없으면 `확인 필요`
- upstream 자체가 의심스럽더라도 근거 없이 임의 보정하지 않음

profile fact 적용 전 raw unresolved ceiling:

```text
LL1 task-pool globalVariable: 48 Quest
availability delay: 13 Quest
structural union: 61 Quest
```

이 값은 source-level ceiling이며 사용자가 UI에서 반드시 61개를 본다는 뜻이 아닙니다. completed / unavailable / locked 등 profile state precedence로 실제 UI 수는 달라집니다.

---

## Needed Items / cleanup 안전성

Quest 화면의 current availability 판정과 future cleanup 안전성은 분리합니다.

- missing future profile-variable fact는 `IndeterminatePotential`로 보호
- unresolved future Quest의 Item도 Needed Items에 포함
- flexible hand-in alternative candidate도 cleanup protection 유지
- Quest 완료/실패처럼 prerequisite/필요 Item이 바뀌는 사건은 full recalculation

따라서 `확인 필요`를 줄이기 위해 실제 필요한 미래 Item을 잘못 `정리 가능`으로 완화하지 않습니다.

---

## Inventory mutation 성능 기준

`FutureNeededItemsBasis`에 수량과 무관한 planning 구조를 캐시/재사용합니다.

- Quest future reachability
- fixed future Quest/Hideout requirements
- flexible alternatives
- cleanup protections
- unentered Hideout station state

Inventory 수량 변경에서는 inventory-dependent 계산만 갱신하고 이미 decode된 Item icon을 재사용합니다.

Quest 완료/실패, Hideout level, profile prerequisite fact 변경은 정확성을 위해 planning basis를 full rebuild합니다.

---

## Content / User Progress 호환성

```text
Current Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1 unchanged
v0.1.11 → v0.1.12 mandatory data update: none
```

기존 `%LocalAppData%/JunhyunHelper/user.db`의 Profile / Quest 완료·실패 / Inventory / Hideout 진행은 유지됩니다.

Map 제품 설정은 `%LocalAppData%/JunhyunHelper/map-product-settings.json`에 유지됩니다.

Game Content update는 user.db를 삭제하거나 덮어쓰지 않습니다.

---

## Map / MiniMap 기준

pinned legacy Map revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

기준:

- floor는 marker visibility filter가 아니라 presentation relation
- enabled 타층 일반 marker 유지
- current/above/below relation 표시
- semantic duplicate extract 정규화
- Main Map floor 변경 시 live zoom + map-space viewport center 보존
- MiniMap floor 변경 시 exact live Scale + Translate X/Y 보존
- Main Map selector와 shared `MapTrackerService.CurrentMapKey` 양방향 동기화
- Interchange 표시 명칭은 `인터체인지`
- `퀘스트 마커 표시` 포함 제품 설정은 persisted product value가 권위값
- MiniMap hover transparency는 lightweight Input-priority 감지

### Current Quest sidebar — v0.1.12

구조:

```text
30px checkbox | 34px A/B/C/D marker | * Quest text
```

- Quest text를 전역 centered Button ContentPresenter에서 분리
- marker/check 유무와 무관하게 실제 title 시작 X축 유지
- 펼친 sidebar handle은 패널 오른쪽 바깥 경계, 즉 지도와 패널 사이에 위치
- 실제 rendered X좌표를 release smoke가 검증

---

## UI 현재 상태

### Items / flexible hand-in

- 68px row rhythm
- 44px icon frame
- candidate 전용 stretch Button template
- 실제 row width 사용
- icon + 이름/분류 좌측
- 인레이드 / 일반 보유량 우측 고정 lane
- 한 줄 이름 + ellipsis

### Ammo

- 검색창은 header 가장 왼쪽
- name/caliber 검색 및 exact caliber/exact Ammo 선택
- favorite toggle은 runtime refresh 이후에도 `☆ / ★`만 표시
- favorite button 38px
- detail handle 42px
- expanded=`▼`
- collapsed=`▲`
- actual rendered state를 release smoke가 검증

### 검색 clear

Quest / Hideout / Items / Ammo 검색창 우측 `×` 버튼:

- 빈 검색어에서는 숨김
- 클릭 시 전체 삭제
- 기존 TextChanged filtering 사용
- 삭제 후 focus 유지

---

## 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 / 핵심 trader LL 저장 지원 |
| Quest | 구현 완료 / `확인 필요` 분리 / special trader + exact profile-variable + audited task-pool + audited dialogue 지원 |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 / unresolved future Quest item 보호 / mutation cache |
| Ammo | 구현 완료 / v0.1.12 rendered UI gate 적용 |
| Map + MiniMap | 구현 완료 / exact floor-frame / persisted settings / map-key sync / v0.1.12 rendered sidebar gate |
| Scanner | `준비 중` placeholder / 실제 기능 PRODUCT OPEN |

---

## 비차단 후속 범위

- 사용자 v0.1.12 실사용 피드백
- Scanner 실제 기능 설계/구현
- Map artwork/config/general-marker atomic bundle updater
- pinned Map renderer deeper refactor는 concrete regression/performance value가 있을 때만 수행
- code signing / installer / application updater
- user.db backup/restore UX
- repository license / third-party notice 정책

---

## 저장소 상태

- 공개 릴리즈: **v0.1.12**
- release baseline: `cfacee6cfa893932d74d6a71725b6c711282981e`
- release workflow run: `32026123215` — SUCCESS
- public ZIP re-download + size/hash verification — SUCCESS
- 임시 `.github/workflows/release-v0.1.12.yml`은 공개 검증 후 제거
- 상시 workflow는 `.github/workflows/ci.yml`만 유지

관련 문서:

- `docs/CURRENT_STATE.md`
- `docs/RELEASE_0.1.12.md`
- `docs/RENDERED_UI_ALIGNMENT_FIX_2026-08-17.md`
- `docs/PRODUCT.md`
- `docs/ARCHITECTURE.md`
- `docs/DECISIONS.md`
