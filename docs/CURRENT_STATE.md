# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계/이력은 `docs/STATE.md` 및 개별 문서를 참조합니다.

기준일: 2026-08-18

상태: `v0.1.12 PUBLIC RELEASE / VERIFIED / POST-RELEASE FINAL AUDIT COMPLETE / MAINTENANCE HARDENING`

## 현재 공개 제품

```text
release: v0.1.12
release baseline: cfacee6cfa893932d74d6a71725b6c711282981e
ProductVersion: 0.1.12+cfacee6cfa893932d74d6a71725b6c711282981e
release candidate PR: #95
release candidate CI: 32025523609 — SUCCESS
release baseline main CI: 32025837427 — SUCCESS
release workflow: 32026123215 — SUCCESS
tests at release: 210 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v0.1.12-win-x64.zip
size: 74,067,018 bytes
SHA-256: bc91f17f94c6554d09da3fed6db6ebb679c6e1d57ff7017d4a624e8dcd8eae89
public release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.12
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
mandatory data update from v0.1.11: none
```

공개 ZIP은 Release 생성 뒤 다시 다운로드하여 크기와 SHA-256을 재검증했습니다. Release는 `draft=false`, `prerelease=false`이며 target commit은 정확히 release baseline과 일치합니다.

상세: `docs/RELEASE_0.1.12.md`

## 2026-08-18 최종 감사

기능 추가를 잠시 중단하기 전 전체 maintenance audit를 수행했습니다.

상세 기록: `docs/FINAL_AUDIT_2026-08-18.md`

최종 판정:

- **v0.1.12를 현재 안정 기준선으로 유지 가능**
- 지원하는 ordinary task/hideout/item source 범위에서 blocking correctness bug 발견 없음
- Quest → mandatory submit Item → Future Needed Items → cleanup pipeline current live 검증 통과
- current live Regular / PvE / PvP Season canonical build 모두 valid, fatal validation 0
- mandatory `giveItem` objective와 derived Quest item requirement 누락/중복 0
- current live Quest/Hideout item reference 누락 0, non-positive requirement 0
- release baseline automated tests 재통과

### current live 데이터 요약

```text
Regular
  quests 517 / objectives 1457 / quest item requirements 307
  mandatory submit 307 / missing-derived 0 / malformed 0
  items 5312 / hideout stations 26 / hideout item requirements 317

PvE
  quests 513 / objectives 1428 / quest item requirements 291
  mandatory submit 291 / missing-derived 0 / malformed 0
  items 5312 / hideout stations 26 / hideout item requirements 317

PvP Season
  quests 490 / objectives 1392 / quest item requirements 286
  mandatory submit 286 / missing-derived 0 / malformed 0
  items 5312 / hideout stations 26 / hideout item requirements 317
```

모든 mode에서 현재 residual unsupported availability는 실제 completion timing을 알아야 하는 `availabilityDelay` 13건뿐이며, 검증된 dialogue compatibility 이후 residual `dialogue`는 0입니다.

### PvE task-pool drift

current audited trader task-pool 구조:

```text
Regular:   27 / 27 valid
PvE:       26 / 27 valid
PvPSeason: 27 / 27 valid
```

PvE Skier LL2 variable `6a5a111de1f417ac80a163e5`만 기존 감사 구조와 달라졌습니다.

- pool count/threshold는 동일
- direct LL2 seed가 3→4로 증가
- 추가 candidate: `Easy Money - Part 1 [PVE ZONE]`

현재 compatibility는 이 drift를 감지하면 해당 pool만 추측하지 않고 fail-closed 합니다. 따라서 exact variable이 없을 경우 일부 관련 Quest가 `확인 필요`로 남을 수 있으나, 잘못된 해금 판정은 하지 않습니다. Future Needed Items도 IndeterminatePotential을 계속 보호합니다.

증명 없이 seed count를 변경하지 않습니다.

## 2026-08-18 maintenance hardening

새 기능을 추가하지 않고 기존 제품의 failure containment와 persistence를 보강합니다.

- Map 제품 설정과 Ammo 즐겨찾기 JSON은 같은 디렉터리의 temporary file에서 원자적으로 교체
- 직전 정상 preference는 `.bak` recovery copy로 유지
- primary JSON이 손상되어도 정상 backup으로 읽기 복구
- 손상된 primary를 다음 저장 때 정상 backup에 덮어쓰지 않음
- Map/Ammo presentation preference 저장 실패는 앱 전체 종료로 확대하지 않고 진단 로그로 격리
- Map slider 연속 변경은 250ms 단위로 묶어 저장하고 종료 시 pending 값을 flush
- Map hotkey 및 NumPad 직접 층 선택 비동기 실패는 전역 dispatcher fatal로 확대하지 않음
- product/direct-floor keyboard hook 설치 실패는 `%LocalAppData%/JunhyunHelper/logs/startup.log`에 기록
- canonical final validator에서 empty Quest accepted-item set과 Quest/Hideout `Count <= 0`을 fatal로 차단
- Scanner는 실제 기능을 추가하지 않고 `준비 중` placeholder 탭을 유지하며 DEC-045가 과거 숨김 결정을 대체

이 변경은 `user.db` schema, Content schema, Quest/Hideout/Needed Items 계산 의미, 현재 승인된 UI를 변경하지 않습니다.

## 가장 중요한 알려진 coverage gap

**EFT 1.0 Story Chapters는 현재 준현 헬퍼의 ordinary `json.tarkov.dev/tasks` 기반 progression model에 포함되지 않습니다.**

따라서 현재 Quest/Needed Items는 가져온 task feed 범위에서는 정합성이 높지만, Story Chapter 전용 hand-in/해금까지 포함한 'EFT 전체 progression'을 완전히 대표한다고 주장하지 않습니다.

향후 개발 재개 시 가장 높은 정확도 개선 항목은 Story Chapters를 canonical progression/Needed Items에 안전하게 연결하는 것입니다.

## v0.1.12 핵심 수정

### Flexible hand-in

- 전역 Button template의 centered ContentPresenter 때문에 내부 Grid가 행 전체 폭을 사용하지 못하던 근본 원인 수정
- candidate 전용 stretch template 적용
- 실제 렌더 구조: `52px icon | * name/category | 108px in-raid | 96px normal`
- icon/name은 좌측 축, in-raid/normal은 우측 축 유지

### Ammo

- runtime data/filter refresh 이후에도 favorite Button은 `☆` / `★`만 표시
- detail handle은 42px 화살표 전용
- expanded=`▼`, collapsed=`▲`

### Map current Quest sidebar

- Quest title을 전역 centered Button ContentPresenter에서 분리
- `30px checkbox | 34px A/B/C/D | * Quest text` 고정 lane
- marker/check 상태와 관계없이 Quest title 시작 X축 유지
- expanded sidebar handle은 panel의 오른쪽 바깥 경계, 즉 지도와 panel 사이에 위치

## UI 완료 판정 기준

v0.1.12부터 위 UI 계약은 source inspection이나 build 성공만으로 완료 처리하지 않습니다.

실제 publish된 Windows 앱에서 `MainWindow.ProductUiLayoutSmoke`가 WPF Measure/Arrange 결과를 검사합니다.

- Flexible candidate가 실제 row 폭으로 확장
- icon/name 좌측 축과 in-raid/normal 우측 축
- Ammo favorite 실제 Content가 단일 `☆`/`★`
- Ammo detail expanded=`▼`, collapsed=`▲`
- 서로 다른 Map Quest 행의 title 시작 X 편차 `<= 0.75px`
- expanded Map sidebar handle right gap `<= 6px`

동일 실행에서 Main Map / Factory / MiniMap / 정상 종료 smoke도 수행합니다.

릴리즈 실행 로그에서 `PUBLISHED_RENDERED_UI_MAP_SMOKE=true`가 확인됐습니다.

## 제품 기능 상태

- Profile: 구현 완료
- Quest: 구현 완료 / conservative unknown 유지 / audited compatibility 적용
- Hideout: 구현 완료
- Needed Items / Inventory: 구현 완료 / unresolved future item 보호 / inventory mutation cache
- Ammo: 구현 완료 / v0.1.12 rendered alignment gate 적용
- Map + MiniMap: 구현 완료 / exact floor-frame / current Quest sidebar rendered alignment gate 적용
- Scanner: `준비 중` placeholder / 실제 기능 PRODUCT OPEN
- runtime GPT/AI 의존성 없음
- 온라인 Tarkov 데이터는 프로그램 importer가 다운로드 → 검증 → canonical DB 재구축

## 기술부채 / 다음 maintenance 후보

우선순위 순서:

1. Story Chapters coverage 설계/통합
2. PvE Skier LL2 새 PVE ZONE seed의 실제 counter semantics 확인
3. `user.db` backup/export/restore UX
4. multi-DPI visual regression
5. Map의 disconnected V1 adapter/sidebar path 정리 — **Map을 다시 손볼 때만**
6. stale unentered-hideout cleanup API 정리
7. code signing / installer / updater

현재 승인된 v0.1.12 UI, conservative Indeterminate/IndeterminatePotential 정책, task-pool fail-closed, FIR cleanup 계산은 단순 정리 목적으로 변경하지 않습니다.
