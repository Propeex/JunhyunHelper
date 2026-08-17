# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 공개 상태

**v0.1.11 PUBLIC RELEASE / VERIFIED — Windows x64**

```text
release tag: v0.1.11
release baseline: 88a732c70380b4c764634eff6fd01a16eb849b14
Desktop ProductVersion: 0.1.11+88a732c70380b4c764634eff6fd01a16eb849b14
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
feature PR: #92
feature PR CI: 32014857527 — SUCCESS
feature main CI: 32015175679 — SUCCESS
release candidate PR: #93
release candidate PR CI: 32015691464 — SUCCESS
release baseline main CI: 32015968523 — SUCCESS
release workflow: 32018616694 — SUCCESS
automated tests: 210 passed / 0 failed / 0 skipped
public asset: Junhyun-Helper-v0.1.11-win-x64.zip
public asset size: 74,063,248 bytes
public SHA-256: 1293cc20c09240c4bdafd6fb45ecb5d0bc37857e12e58f60e31dff620e01b426
public release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.11
```

공개 ZIP은 Release 생성 뒤 다시 다운로드해 SHA-256을 재검증했습니다. Release는 `draft=false`, `prerelease=false`이며 target commit은 정확히 release baseline과 일치합니다.

상세: `docs/RELEASE_0.1.11.md`

v0.1.11은 v0.1.10의 Quest `확인 필요` 수정은 그대로 유지하면서, v0.1.10에서 runtime visual-tree 후처리로 구현되어 실제 화면에 안정적으로 반영되지 않던 Items / Ammo / Map UI를 **원본 XAML / 원본 UI 생성 코드에서 직접 수정한 교정 릴리즈**입니다.

---

## Quest prerequisite / availability 기준

### 일반 prerequisite

- 서로 다른 `taskRequirements` 항목은 AND
- 한 requirement의 `status[]`는 OR
- `complete` = 해당 Quest 완료
- `active` = 해당 Quest가 진행 상태에 도달
- `failed` = 해당 Quest 실패
- 별도 `수주 가능` 상태를 만들지 않음
- `DEC-010` 유지: 게임에서 받을 수 있는 Quest는 Helper에서 즉시 수락한 것으로 간주
- source가 직접 제공한 prerequisite 상태는 compatibility overlay가 더 강한 상태로 덮어쓰지 않음

### BTR Driver / Ref / Lightkeeper

- BTR Driver는 `A Helping Hand = Active` 의미를 보존하고 누락된 후속 Quest에만 Active gate를 보강
- Ref는 source gate를 보존하고 누락된 후속 Quest에만 GameMode별 검증된 Complete gate를 보강
- Lightkeeper는 ordinary monotonic prerequisite와 recoverable access state를 분리
- 최초 접근은 Getting Acquainted 결과에서 추론하고, 실제 접근 상실/복구가 필요한 특수 상황에서만 sparse user fact 사용
- recoverable 접근 상실은 영구 `Unavailable`이 아니라 `Locked`
- 상세: `docs/QUEST_PREREQUISITE_SEMANTICS.md`, `DEC-043`

### EFT profile-variable / trader task-pool gate

`globalVariable` requirement는 `variableId / operator / value`를 canonical Content에 구조적으로 보존합니다.

우선순위:

1. exact current profile variable 값이 존재하면 항상 그 값이 권위값
2. exact 값이 없고 current EFT 1.1 audited task-pool 구조가 완전히 일치하면 current-version compatibility
3. 어느 쪽도 증명할 수 없으면 `확인 필요(Indeterminate)`

2026-08-17 live 감사 기준:

- `globalVariable` Quest: 162개
- unique trader-local task-pool variable: 27개
- audited LL2~LL4 Quest: 114개
- LL1 pool Quest: 48개

LL2~LL4 compatibility는 **정확한 variable ID / trader / pool Quest count / threshold set / direct same-trader LL seed count**가 모두 감사값과 일치할 때만 동작합니다.

- 현재 trader LL이 pool 단계보다 낮으면 해당 future pool current value는 0으로 확정
- trader LL이 단계에 도달했으면 완료한 direct LL seed Quest + 같은 pool의 완료 Quest로 current value 재구성
- exact profile variable 값이 synthetic compatibility 값보다 항상 우선
- synthetic 값은 Quest 화면 runtime profile copy에만 존재하며 `user.db`에 저장하지 않음
- source 구조가 바뀌면 compatibility는 자동 중단되고 `Indeterminate`로 fail closed

v0.1.10부터 LL1은 **증명 가능한 초기 상태만** 추가 처리합니다.

- audited current-version 구조가 그대로일 것
- 해당 trader의 현재 loyalty가 LL1일 것
- 그 trader에 대해 Helper가 알고 있는 completed Quest가 0개일 것
- 위 조건을 모두 만족하면 해당 LL1 pool current counter=0으로 확정
- completed Quest가 하나라도 있거나 trader가 LL2 이상이면 exact 값 없이 LL1 counter를 추측하지 않음

이 규칙은 generic server write rule을 발명한 것이 아닙니다. 새 variable ID나 진행된 LL1 값을 Quest 이름/ObjectId/유사성으로 역산하지 않습니다.

상세: `docs/QUEST_TASK_POOL_AUDIT_2026-08-17.md`, `docs/DECISION_TASK_POOL_RUNTIME_COMPATIBILITY_2026-08-17.md`, `DEC-044`

### Dialogue availability compatibility

2026-08-17 live feed의 `dialogue` Quest 12건은 regular / pve / pvp-season에서 동일하며 전수 감사했습니다.

- 정확히 검증된 12개 Quest ID에만 compatibility 적용
- 실제 시작 Quest 3개는 opaque dialogue gate 제거
- 나머지 9개는 검증된 prerequisite와 minimum level 복원
- Introduction은 Gunsmith - MP-133 `Active` 의미 보존
- upstream이 향후 ordinary `taskRequirements`를 제공하면 source rule이 자동 우선
- 새로운/변경된 dialogue Quest는 추측하지 않고 `확인 필요`
- 기존 content snapshot에도 read-time 적용하므로 데이터 DB 삭제/강제 재다운로드 불필요
- 상세: `docs/DIALOGUE_GATE_AUDIT_2026-08-17.md`

### 실패 / 불명확 availability

- 다른 Quest 완료로 확정되는 sibling failure는 자동 추론
- 프로그램이 알 수 없는 비재시작형 영구 실패만 사용자 입력
- 재시작 가능한 raid failure는 영구 저장하지 않음
- 검증되지 않은 새 dialogue는 `확인 필요`
- LL1 task-pool은 pristine LL1 zero 또는 exact current 값으로 증명되지 않으면 `확인 필요`
- 실제 완료 시각 기반 availability delay는 completion timestamp가 없으면 `확인 필요`
- upstream 자체가 의심스러운 데이터는 근거 없이 임의 보정하지 않음

profile fact 적용 전 raw unresolved ceiling:

```text
LL1 task-pool globalVariable: 48 Quest
availability delay: 13 Quest
structural union: 61 Quest
```

pristine LL1 rule은 사용자 profile fact로 이 중 일부를 `Locked`로 확정합니다. 진행된 profile은 exact 값이 없으면 일부 LL1 항목이 계속 남을 수 있습니다.

---

## Needed Items / cleanup 안전성

Quest 화면의 availability compatibility와 future item cleanup의 보수성은 분리합니다.

- `FutureNeededItemsPlanner`의 future reachability는 missing profile-variable fact를 계속 `IndeterminatePotential`로 보호
- unresolved future Quest의 Item도 Needed Items에 포함
- flexible hand-in alternative candidate도 cleanup protection 유지
- Quest 완료/실패처럼 prerequisite/필요 Item이 바뀌는 사건은 full recalculation 유지

따라서 Quest UI의 false `확인 필요`를 줄이기 위해 실제 필요한 미래 Item을 잘못 `정리 가능`으로 완화하지 않습니다.

---

## Inventory mutation 성능 기준

v0.1.9부터 inventory 수량 변경은 수량과 무관한 planning 구조를 매번 다시 만들지 않습니다.

`FutureNeededItemsBasis`에 다음을 캐시/재사용합니다.

- Quest future reachability
- fixed future Quest/Hideout item requirements
- flexible alternative requirements
- cleanup protections
- unentered Hideout station state

수량 변경에서는 Needed quantity / cleanup·surplus / flexible-owned progress / 변경된 Items row 표시만 다시 계산합니다. 이미 decode된 Item icon도 재사용하고 전체 icon load pipeline을 매번 취소/재시작하지 않습니다.

Quest 완료/실패, Hideout level, profile prerequisite fact 변경은 정확성을 위해 planning basis를 full rebuild합니다.

---

## Content / User Progress 호환성

```text
Current Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1 unchanged
v0.1.10 → v0.1.11 mandatory data update: none
```

다음 정상 `데이터 업데이트`가 성공하면 v7 snapshot으로 저장합니다.

기존 `%LocalAppData%/JunhyunHelper/user.db`의 Profile / Quest 완료·실패 / Inventory / Hideout 진행은 유지됩니다. exact profile-variable 값과 special trader access override는 optional user facts로 저장됩니다.

`GameContentValidator`는 prerequisite missing/self/duplicate/cycle/empty status 및 잘못된 special-trader gate를 candidate activation 전에 차단합니다.

---

## Map / MiniMap 기준

Map subsystem은 독립이고 Quest만 JunhyunHelper current profile/content와 연결합니다. pinned legacy Map revision은 `d933792b6042a51cea38dc44b686a096fe30de67`입니다.

- floor는 marker visibility filter가 아니라 presentation relation
- enabled 타층 일반 marker는 same-type/near-XZ라도 각각 유지
- current/above/below compact ring + known off-floor opacity
- semantic duplicate extract 정규화 유지
- Main Map floor 변경은 live zoom + map-space viewport center 보존
- MiniMap floor 변경은 exact live Scale + Translate X/Y 보존
- Main Map selector와 shared `MapTrackerService.CurrentMapKey`를 양방향 동기화
- Interchange 사용자 표시 명칭은 `인터체인지`
- 제품용 marker setting은 `%LocalAppData%/JunhyunHelper/map-product-settings.json`을 권위값으로 사용
- `퀘스트 마커 표시`도 persisted product value를 권위값으로 하며 late legacy initialization이 덮지 못하도록 재적용
- MiniMap hover 투명화는 heavy map synchronization과 분리한 lightweight 16ms Input-priority 감지 사용
- v0.1.11에서 current Quest sidebar는 생성 시점부터 `30px checkbox | 34px A·B·C marker | * quest text` 구조를 사용
- runtime `LegacyMapQuestSidebarPolishBridge`는 제거됨

상세: `docs/MINIMAP_FLOOR_FRAME_2026-08-17.md`, `docs/USABILITY_STABILITY_PASS_2026-08-17.md`, `docs/FEEDBACK_FIXES_2026-08-17.md`, `docs/RELEASE_0.1.11.md`

v0.1.11 공개 baseline publish 실행본으로 startup + Main Map + Factory + MiniMap + 정상 종료를 재검증했습니다.

---

## UI 현재 상태

### Items / flexible hand-in

v0.1.11에서 `FlexibleCandidateTemplate` 자체가 최종 레이아웃을 소유합니다.

- 68px row rhythm
- icon frame 44px
- 아이콘 clipping 없음
- icon + 이름/분류 좌측 정렬
- 인레이드 / 일반 보유량 우측 고정 lane
- 한 줄 이름 + ellipsis
- runtime visual-tree rewrite 제거

### Ammo

- 검색창은 header 가장 왼쪽
- 검색은 name/caliber로 가능하고 결과 클릭 시 exact caliber table + exact Ammo row 선택
- 검색 popup 표시는 image + name
- 중복 `구경`, `즐겨찾기` label 제거
- caliber selector 160px fixed
- 즐겨찾기 toggle은 `☆ / ★`, 38px fixed
- favorites selector 170px fixed
- 하단 detail toggle과 detail host는 원본 XAML에 직접 존재
- runtime 코드는 expansion state만 제어

### 검색 clear

Quest / Hideout / Items / Ammo 검색창 우측에 `×` 버튼을 제공합니다.

- 빈 검색어에서는 숨김
- 클릭 시 전체 삭제
- 기존 TextChanged filtering 사용
- 삭제 후 검색창 focus 유지

---

## 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 / 핵심 trader LL 저장 지원 |
| Quest | 구현 완료 / `확인 필요` 분리 / special trader + exact profile-variable + audited LL2~LL4 + pristine LL1 zero + audited dialogue gate 지원 |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 / unresolved future Quest item 보호 / inventory-only planning cache 및 icon refresh 최적화 |
| Ammo | 구현 완료 / 검색·정확 선택 / canonical compact header / centered detail toggle / star-only favorite |
| Map + MiniMap | 구현 완료 / exact MiniMap floor-frame / Quest marker setting 영속화 / map-key 동기화 / canonical Quest sidebar layout / 빠른 hover transparency |
| Scanner | `준비 중` placeholder / 실제 기능 PRODUCT OPEN |

## 비차단 후속 범위

- Scanner 실제 기능 설계/구현
- Map artwork/config/general-marker atomic bundle updater
- pinned Map renderer deeper refactor는 concrete regression/performance value가 있을 때만 수행
- code signing / installer / application updater
- user.db backup/restore UX
- repository license / third-party notice 정책

## 저장소 상태

- 공개 릴리즈: **v0.1.11**
- release baseline: `88a732c70380b4c764634eff6fd01a16eb849b14`
- 공개 release workflow run: `32018616694` — SUCCESS
- 임시 `.github/workflows/release-v0.1.11.yml`은 공개 검증 후 제거함
- 상시 workflow는 `.github/workflows/ci.yml`만 유지
