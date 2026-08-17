# 준현 헬퍼 v0.1.9 공개 검증 기록

날짜: 2026-08-17

## 공개 상태

**PUBLIC RELEASE / VERIFIED — Windows x64**

```text
release tag: v0.1.9
release baseline: 95d3bb139fb9c5f5b7a6e353ea560768c03d20f4
Desktop ProductVersion: 0.1.9+95d3bb139fb9c5f5b7a6e353ea560768c03d20f4
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
feature PR: #88
feature PR CI: 32002897379 — SUCCESS
feature main CI: 32003122340 — SUCCESS
release candidate PR: #89
release candidate PR CI: 32003361260 — SUCCESS
release baseline main CI: 32003570258 — SUCCESS
release workflow: 32003799898 — SUCCESS
automated tests: 208 passed / 0 failed / 0 skipped
public asset: Junhyun-Helper-v0.1.9-win-x64.zip
public asset size: 74,065,677 bytes
public SHA-256: c9a12ba52e2774c9a127c9f9d8740918bf8837df26e821bc11fcd793ae521952
public release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.9
```

Release는 draft/prerelease가 아닌 정식 공개 상태이며 target commit은 정확히 release baseline과 일치한다. 공개 ZIP과 `SHA256SUMS.txt`를 업로드한 뒤 GitHub Release에서 ZIP을 다시 다운로드해 local package SHA-256과 일치하는 것을 확인했다.

## 사용자 피드백 반영 범위

v0.1.8 실사용에서 전달된 10개 피드백을 대상으로 한다.

### 1. Quest `확인 필요` 과다

- 2026-08-17 current live feed의 `globalVariable` 162 Quest / 27 trader-local task-pool 변수를 다시 감사했다.
- exact profile variable 값이 존재하면 항상 exact 값을 사용한다.
- exact audited 구조가 유지되는 LL2~LL4 task-pool 114 Quest는 저장된 trader LL과 완료 Quest로 runtime current value를 재구성한다.
- 현재 trader LL보다 미래 단계인 pool은 아직 시작되지 않은 현재 값 0으로 확정한다.
- LL1 task-pool 48 Quest는 public feed가 initial seed/write rule을 제공하지 않으므로 exact profile variable 값이 없으면 계속 `확인 필요`로 남긴다.
- availability delay 13 Quest도 실제 completion timestamp를 모르면 계속 `확인 필요`다.
- exact variable ID / trader / pool Quest count / threshold set / direct seed count 중 하나라도 current audit와 달라지면 compatibility는 자동으로 중단되고 원래 `Indeterminate`로 fail closed한다.
- synthetic current value는 runtime Quest 표시용 profile copy에만 존재하며 `user.db`에 저장하지 않는다.
- `FutureNeededItemsPlanner`는 기존 conservative reachability를 유지해 `IndeterminatePotential` 아이템 보호를 약화시키지 않는다.

현재 raw 구조 기준 unresolved ceiling은 v0.1.8의 175에서 61(LL1 48 + delay 13)로 감소한다. 실제 특정 사용자 UI의 `확인 필요` 개수는 완료/Locked/Unavailable 상태와 exact profile variable 값 때문에 이 숫자와 다를 수 있다.

근거: `docs/QUEST_TASK_POOL_AUDIT_2026-08-17.md`, `docs/FEEDBACK_FIXES_2026-08-17.md`

### 2. 아이템 수량 변경 성능

v0.1.8은 Quest page rebuild만 제거했지만 inventory mutation마다 여전히 Quest future reachability, future Quest requirements, Hideout future requirements, cleanup protection, Items row/icon pipeline을 광범위하게 다시 계산/재시작했다.

v0.1.9에서는:

- `FutureNeededItemsBasis`로 inventory-independent planning을 분리/재사용한다.
- 수량 변경 시 Needed/Cleanup/Flexible-owned 등 inventory-dependent 값만 다시 계산한다.
- 이미 decode된 Item icon을 보존한다.
- inventory mutation마다 전체 icon pipeline을 취소/재시작하지 않는다.
- Quest 완료/실패, Hideout level, profile prerequisite fact처럼 실제 planning basis가 바뀌는 경우에는 정확한 full rebuild를 유지한다.

### 3. 유동 제출 Item 행

일반 Item list를 제품 기준으로 사용한다.

- row 68px
- icon lane 52px
- icon 44px
- quantity lane 118px
- single-line ellipsis
- fixed padding / vertical alignment

virtualization/layout 재생성 뒤 일부 row가 보정을 놓치지 않도록 Flexible ItemsControl 범위의 layout 변경을 batch 처리한다.

### 4. Ammo 검색

- 헤더 가장 왼쪽에 검색창 배치
- 이름/구경으로 검색 가능
- 결과 UI는 `탄약 이미지 + 탄약 이름`만 표시
- 구경은 결과 텍스트에서 제거
- 결과 선택 시 exact caliber table + exact Ammo row 이동 유지

### 5. Ammo 하단 상세 접기

v0.1.8 구현이 실제 XAML hierarchy와 다른 detail-host 조건을 사용해 Expander가 생성되지 않던 오류를 수정했다.

- row 4 outer detail Border 자체를 Expander로 감싼다.
- collapsed 상태에서 detail row를 Auto/MinHeight=0으로 줄이고 splitter도 숨긴다.
- expanded 상태에서 기존 detail 영역과 splitter를 복원한다.

### 6. Ammo 즐겨찾기

텍스트 문구를 제거한다.

- 미즐겨찾기: `☆`
- 즐겨찾기: `★`
- 설명은 tooltip으로 제공한다.

### 7. 지도 `퀘스트 마커 표시` 저장

`map-product-settings.json`의 `ChkShowQuestMarkers` 저장값을 권위값으로 사용한다. legacy Map startup의 늦은 기본값 할당이 사용자 저장값을 다시 덮지 못하도록 Loaded 및 초기 안정화 구간에 persisted product value를 재적용한다.

### 8. Map Quest sidebar 정렬

v0.1.8의 exact RGB background 기반 row detection을 제거하고 실제 `Border -> Grid -> CheckBox + Button` 구조로 Quest row를 판별한다.

- row 68px
- checkbox lane 30px
- marker badge lane 34px
- marker badge 28px
- text star lane
- single-line ellipsis

### 9. MiniMap hover 투명화 반응

hover 감지가 Quest/general marker/extract 동기화도 수행하는 80ms product timer에 묶여 있던 구조를 분리했다.

- dedicated lightweight 16ms Input-priority hover timer
- cursor inside / temporary hide 상태만 확인
- 무거운 Map render/sync 주기는 기존대로 유지

### 10. 검색창 × 버튼

Quest / Hideout / Items / Ammo 검색창 우측에 일괄 삭제 버튼을 추가한다.

- 검색어가 비어 있으면 숨김
- 클릭 시 전체 삭제
- 기존 TextChanged filter logic 사용
- 삭제 후 검색창 focus 유지

## 검증

Release workflow `32003799898`에서 exact baseline `95d3bb139fb9c5f5b7a6e353ea560768c03d20f4`를 detached checkout했다.

검증 결과:

- Windows Release build: SUCCESS
- automated tests: 208 passed / 0 failed / 0 skipped
- Windows x64 self-contained single-file publish: SUCCESS
- ProductVersion: `0.1.9+95d3bb139fb9c5f5b7a6e353ea560768c03d20f4`
- FIRST_RUN v0.1.9: SUCCESS
- Content schema v7 안내: SUCCESS
- user.db schema v1 안내: SUCCESS
- release root cleanliness / forbidden legacy dependency 검사: SUCCESS
- startup + Main Map + Factory + MiniMap runtime smoke: SUCCESS
- normal Main Window close / process exit: SUCCESS
- public GitHub Release target verification: SUCCESS
- public ZIP re-download: SUCCESS
- public SHA-256 comparison: SUCCESS

## 호환성

```text
v0.1.8 → v0.1.9 mandatory data update: none
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1 unchanged
```

기존 `%LocalAppData%/JunhyunHelper/user.db`의 Profile / Quest / Inventory / Hideout 진행과 `%LocalAppData%/JunhyunHelper/map-product-settings.json` 설정은 유지된다.

## 공개 파일

```text
Junhyun-Helper-v0.1.9-win-x64.zip
74,065,677 bytes
SHA-256 c9a12ba52e2774c9a127c9f9d8740918bf8837df26e821bc11fcd793ae521952
```

공개 주소: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.9
