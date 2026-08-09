# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다. 세부 설계와 과거 원인은 링크된 상세 문서를 참조합니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 실사용 피드백 반복 개선**

상태: `MAP IMPLEMENTED / UPDATE-RESILIENT MAP PIPELINE MERGED / RE3MR GROUND ZERO WINDOWS VALIDATION`

Map 1차 기능은 구현되어 Windows 실사용 검증 중입니다. 사용자가 실제 화면에서 확인한 결과:

- floor selector의 record dump 문제는 해결됨
- Tarkov.dev/Shebuka schematic SVG 및 Official Wiki background는 좌표 기능에는 사용할 수 있으나 실전 지도 가독성이 부족함
- 목표 presentation은 기존 Tarkov Helper처럼 **도로, 건물 구조, 구역, 지명이 빠르게 읽히는 상세 지도**임
- 좌표 데이터와 지도 artwork는 같은 source에서 가져올 필요가 없음
- 무엇보다 **지도와 좌표 모두 패치마다 사람이 다시 맞추지 않고 온라인 source를 재다운로드해 같은 변환/검증 규칙으로 갱신 가능해야 함**

최근 Map checkpoint:

```text
PR #43 — Map 자동 업데이트 + SVG 라이선스 정책
PR #44 — Map 탭 / Quest marker / 위치 추적 / MiniMap
PR #45 — 대량 loot marker renderer 안정화
PR #46 — Map lazy-load / startup diagnostics / self-contained package
PR #47 — 초기 marker CheckBox NRE 수정
PR #48 — missing map-cache self-heal / 지도별 부분 복구 / SVG source fallback
PR #50 — Windows FileShare.None 다운로드 검증 실패 수정
PR #53 — floor Name 표시 + high-contrast readable SVG presentation
PR #55 — 선택된 floor가 record 전체를 표시하던 전역 ComboBox template 문제 최종 수정
PR #56 — Official Wiki background 자동 affine calibration
PR #57 — 기존 사용자 map-cache 자동 migration
PR #58 — Map 좌표/artwork update-resilient refresh 정책 + artwork provider 경계
PR #59 — RE3MR Ground Zero 상세 artwork provider + revision registration
```

PR #59 최종 검증:

```text
CI: 31303356682
Release Desktop build: success
full automated tests: success
Windows x64 self-contained publish: success
ZIP creation/upload: success
```

핵심 Map 상세 문서:

- `docs/MAP_PRODUCT_DESIGN.md`
- `docs/MAP_IMPLEMENTATION.md`
- `docs/MAP_UPDATE_PIPELINE.md`
- `docs/MAP_SOURCE_DECISION_2026-08-09.md`
- `docs/MAP_RE3MR_PROVIDER.md`
- `docs/MAP_DATA_SOURCE_ANALYSIS.md`
- `docs/MAP_VISIBILITY_ANALYSIS.md`
- `docs/MAP_ASSET_RECOVERY.md`
- `docs/MAP_STARTUP_RECOVERY.md`

---

## 최우선 제품 원칙

준현 헬퍼는 패치마다 GPT가 새 게임 데이터를 다시 해석해 수작업으로 넣는 프로그램이 아닙니다.

```text
온라인 Tarkov 데이터
→ 다운로드
→ 외부 형식 검증
→ canonical model 변환
→ candidate DB / presentation candidate
→ 검증
→ active 교체
→ User Progress와 결합
→ 파생 결과 계산
→ Desktop 표시
```

원칙:

- 일반적인 데이터 내용 변화는 같은 importer/변환 규칙으로 자동 재구축
- 외부 **형식/의미 자체가 바뀐 경우에만** importer 개발 변경
- runtime AI/GPT 없음
- 의미를 모르는 외부 데이터는 추측하지 않음
- Game Content와 `user.db` 분리
- update 실패가 기존 정상 Game Content/User Progress를 손상시키지 않음
- Map gameplay facts / coordinate / presentation artwork를 분리
- 새 지도 artwork는 다운로드 성공만으로 적용하지 않고 좌표 정합 검증 필수
- 잘못 정렬된 최신 지도보다 이전 정상 지도 유지가 우선

---

## 기술 / 저장 / 배포

- .NET 10 / C# / WPF
- SQLite
- Core / Infrastructure / Application / Desktop
- SkiaSharp image decode / image revision registration
- SharpVectors WPF SVG rendering

기본 root:

```text
%LocalAppData%/JunhyunHelper
```

주요 저장:

```text
user.db
content/<game-mode>/content.db
content/<game-mode>/content.candidate.db
content/<game-mode>/content.previous.db
image-cache/
map-cache/active/
map-cache/candidate/
map-cache/previous/
map-cache/update-state.json
map-cache/refresh.requested
map-settings.json
map-markers.json
map-bulk-marker-settings.json
ammo-favorites.json
logs/startup.log
```

Content schema는 **v4**이며 dynamic Map marker + Quest objective world geometry를 포함합니다. 이전 snapshot은 온라인 source에서 자동 재구축하고 `user.db`는 유지합니다.

Windows 테스트 전달본은 self-contained folder ZIP입니다. ZIP 전체를 새 폴더에 풀고 `JunhyunHelper.exe`를 실행하며 별도 .NET 설치는 필요 없습니다.

---

# 기존 Core 제품 상태

## Profile

- 한 GameMode당 profile 하나
- 새 프로필 / 수정 / 삭제
- level, faction, edition, prestige, trader 상태
- Fence reputation 별도 진행값

## Quest

- 진행 중 / 잠김 / 사용 불가 / 완료
- Quest Item → Item
- prerequisite Quest → Quest
- Wiki 이동
- Map Quest list/marker → Quest
- 고정 제출 요구 완료 시 tracked Inventory 자동 차감
- 완료 취소는 실제 소비 ledger 기반 복원 선택
- 실제 사용 Item을 알 수 없는 유동 제출 후보는 임의 자동 차감하지 않음

## Hideout

- 미입력 Lv.0
- 단계별 next-upgrade material
- material ↔ Item/facility 이동
- upgrade 고정 재료 자동 차감
- rollback은 ledger 기반 정확한 복원 선택

## Needed Items / Item

- 필요 · 인레이드 / 일반
- 보유 · 인레이드 / 일반
- 검색 / Item 종류 / 용도 / 필요 상태 필터

## Ammo

- raw 성능: `json.tarkov.dev`
- Wiki Ballistics 비교 정보
- favorite shortcut popup

---

# Map

## 사용자 기능

- Map dropdown
- floor selector
- wheel zoom / drag pan / 보기 초기화
- 선택 Map의 현재 진행 중 Quest 목록
- marker checkbox
- attribution
- user marker
- screenshot 현재 위치/방향
- 이동 경로
- game log Map 자동 전환
- always-on-top MiniMap

Marker 범위:

- PMC / Scav / shared extract
- transit
- PMC / Scav spawn
- sniper Scav
- boss / special AI
- hazard / artillery
- lock / switch
- stationary weapon
- BTR stop
- loot container / loose loot
- Quest
- user marker
- current player position
- optional trail

Loot marker는 기본 OFF이며 대량 marker는 `DrawingContext` 기반 bulk layer로 렌더링합니다.

## Quest 위치

Task의 `possibleLocations` / `zones`를 canonical world geometry로 변환합니다.
정확한 위치가 없는 Quest는 가짜 좌표를 만들지 않고 목록에만 표시합니다.

## Screenshot 위치

EFT screenshot 파일명의 X/Y/Z + quaternion을 파싱하며 OCR은 사용하지 않습니다.

```text
PrintScreen
→ FileSystemWatcher
→ filename 좌표/방향 parse
→ world position
→ Map transform
→ player marker + heading
```

---

## Map source architecture

### Canonical gameplay / coordinate

온라인 Tarkov data와 Tarkov.dev layout metadata를 사용합니다.

```text
online gameplay/map data
+ layout transform / rotation / bounds / floor metadata
→ canonical world X/Y/Z
→ normalized Map surface
```

Quest/extract/player/user marker는 이 canonical surface를 사용합니다.

### Presentation artwork

좌표 source와 독립 provider입니다.

현재 우선순위:

```text
1. RE3MR detailed artwork — 현재 Ground Zero만 검증 구현
2. Official Wiki artwork — machine-readable marker 기반 affine calibration
3. Tarkov.dev/Shebuka calibrated schematic SVG
4. refresh 전체 실패 시 previous active asset
```

기존 Tarkov Helper의 보기 좋은 SVG는 과거 Tarkov Market 계열 artwork를 수동 migration한 자산으로 확인됐습니다. 목표 UX reference로만 사용하며 신규 JunhyunHelper의 자동 source로 복제하지 않습니다.

---

## Map update-resilient pipeline — PR #58

Map source refresh 조건:

- active Map 없음/손상
- Game Content의 Map/marker fingerprint 변경
- Data Update 성공
- Map ingestion pipeline version 변경
- 마지막 성공 refresh 후 24시간 경과
- 사용자의 지도 자산 수동 refresh

현재 pipeline version:

```text
map-online-sources-v4-re3mr
```

따라서 새 artwork importer가 도입되면 기존 사용자의 cache도 자동 재구축합니다.

`update-state.json`은 active/candidate 밖에 보존하며 asset directory swap과 독립입니다.

---

## RE3MR Ground Zero — PR #59

목표는 상세 지도를 넣는 것과 업데이트 대응을 동시에 만족하는 것입니다.

```text
RE3MR page
→ page version
→ current image URL
→ image SHA256
→ visual extraction anchor 검증
→ 현재 canonical extraction marker 이름 매칭
→ artwork coordinate → canonical surface affine calibration
→ residual/max error 검증
→ candidate
→ active
```

Artwork 이미지 revision이 바뀌면 이전 validated image와 새 image를 자동 registration합니다.
현재는 안전성을 위해 global scale + translation만 허용합니다.

새 revision을 신뢰할 수 없으면:

```text
previous validated RE3MR 유지
→ 없으면 Official Wiki
→ Wiki 실패 시 calibrated schematic SVG
```

Ground Zero visual anchor:

- Emercom Checkpoint
- Scav Checkpoint (Co-Op)
- Mira Ave
- Police Cordon V-Ex
- Nakatani Basement Stairs

중요: 이 anchor는 artwork상의 시각 기준점이며 world X/Z를 하드코딩한 것이 아닙니다. world coordinate는 매 refresh마다 현재 canonical Map marker에서 다시 읽습니다.

현재 **Windows 실제 화면 검증 전**입니다. 자동 테스트는 통과했지만 실제 RE3MR source 다운로드, 화면 crop/scale, 실제 marker 정합은 사용자 Windows 검증이 다음 gate입니다.

---

## Scanner

탭과 placeholder만 있습니다. 실제 기능은 별도 제품 요구사항 확정 전까지 구현하지 않습니다. Map artwork의 anti-cheat 라이선스와 충돌하는 radar/ESP 성격 기능은 넣지 않습니다.

---

## 현재 다음 작업

1. 최신 Windows 빌드에서 Ground Zero가 실제 RE3MR 상세 artwork로 표시되는지 확인
2. 도로/건물/지명 가독성이 목표 UX에 맞는지 확인
3. Ground Zero extract/Quest marker의 실제 배경 위치 정합 확인
4. screenshot current-position marker 정합 확인
5. RE3MR source refresh 실패/변경 시 previous/fallback 동작 확인
6. Ground Zero 검증 성공 후 RE3MR provider를 다른 single-plane Map으로 확대
7. multi-floor Map은 floor별 상세 artwork/calibration 별도 구현
8. MiniMap에서 상세 artwork 가독성/성능 확인
