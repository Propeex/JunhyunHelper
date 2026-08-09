# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**Phase 2B — 핵심 Desktop 흐름 구현 + 실사용 피드백 반복 개선**

상태: `LEGACY MAP + MINIMAP PORT IMPLEMENTED / AUTOMATED VALIDATION PASSED / WINDOWS USER VALIDATION NEXT`

---

## 현재 최우선 사용자 결정

사용자는 현재 JunhyunHelper의 RE3MR / Official Wiki / Shebuka 기반 Map presentation 결과가 실사용 기준에 맞지 않는다고 확정했습니다.

새 기준:

- **기존 `Propeex/Tarkov-Helper`의 지도 artwork를 기본 표시 지도에 사용**
- 기존 Tarkov Helper에서 그 artwork에 맞춰 사용하던 좌표 보정 방식도 함께 이해해서 이식
- **MiniMap 포함**
- 단순 asset copy가 아니라 좌표식, floor, 위치 추적, MiniMap lifecycle을 이해하고 필요한 문제를 수정해서 이식
- 지도 artwork와 gameplay/Quest 좌표 source는 달라도 됨
- 가장 중요한 원칙은 **지도와 좌표 모두 업데이트에 대응 가능해야 함**

상세 계약/구현 문서:

- `docs/LEGACY_MAP_PORT.md`

---

## 최근 Map checkpoint

```text
PR #43 — Map 자동 업데이트 + SVG 라이선스 정책
PR #44 — Map 탭 / Quest marker / 위치 추적 / MiniMap
PR #45 — 대량 loot marker renderer 안정화
PR #46 — Map lazy-load / startup diagnostics / self-contained package
PR #47 — 초기 marker CheckBox NRE 수정
PR #48 — missing map-cache self-heal / 지도별 부분 복구 / SVG source fallback
PR #50 — Windows FileShare.None 다운로드 검증 실패 수정
PR #53 — floor Name 표시 + high-contrast readable SVG presentation
PR #55 — selected floor record dump 최종 수정
PR #56–#60 — Wiki/RE3MR detailed artwork 실험 및 update-aware calibration
PR #61 — legacy Tarkov Helper Map + MiniMap 이식, atomic upstream update 구조
```

PR #61의 현재 구현 브랜치:

```text
agent/legacy-map-minimap-port
```

자동 검증 checkpoint:

```text
Desktop Release build: success
full automated tests: success
Windows x64 self-contained publish: success
ZIP creation/upload: success
```

Windows 실제 화면 검증 전이므로 PR #61은 사용자 visual validation 전까지 Map UX 최종 완료로 보지 않습니다.

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
- 외부 형식/의미 자체가 바뀐 경우에만 importer 개발 변경
- runtime AI/GPT 없음
- 의미를 모르는 외부 데이터는 추측하지 않음
- Game Content와 `user.db` 분리
- update 실패가 기존 정상 Game Content/User Progress를 손상시키지 않음
- Map gameplay facts / coordinate / presentation artwork를 분리
- 새 Map candidate가 실패하면 이전 정상 active를 유지

---

## 기술 / 저장 / 배포

- .NET 10 / C# / WPF
- SQLite
- Core / Infrastructure / Application / Desktop
- SharpVectors SVG rendering
- SkiaSharp image handling

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
minimap-settings.json
ammo-favorites.json
logs/startup.log
```

Content schema는 **v4**이며 dynamic Map marker + Quest objective world geometry를 포함합니다.

Windows 전달본은 self-contained folder ZIP입니다. ZIP 전체를 새 폴더에 풀고 `JunhyunHelper.exe`를 실행합니다.

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
- 제출/취소 inventory ledger 처리

## Hideout

- 시설 레벨 추적
- next-upgrade material
- material ↔ Item/facility 이동
- upgrade/rollback inventory ledger 처리

## Needed Items / Item

- 필요 · FIR / 일반
- 보유 · FIR / 일반
- 검색 / Item 종류 / 용도 / 필요 상태 필터

## Ammo

- raw 성능: `json.tarkov.dev`
- Wiki Ballistics 비교 정보
- favorite shortcut popup

---

# Map — 현재 구조

## 1. Canonical gameplay / Quest facts

현재 online Game Content pipeline을 유지합니다.

- extracts
- transit
- PMC / Scav spawn
- sniper Scav
- boss / special AI
- hazards
- lock / switch
- stationary weapon
- BTR
- loot container / loose loot
- Quest possibleLocations / zones

정확한 위치가 없는 Quest는 가짜 좌표를 만들지 않습니다.

## 2. 현재 floor spatial metadata

Tarkov.dev online layout metadata의 X/Z bounds + Y height를 사용합니다.

이 데이터는 실제 현재 위치의 자동 floor 판정에 사용됩니다.

## 3. Presentation artwork + calibration

기본 표시 경로는 legacy Tarkov Helper입니다.

```text
Propeex/Tarkov-Helper current main commit SHA resolve
→ 동일 SHA의 map_configs.json
→ 동일 SHA의 Assets/DB/Maps/*.svg
→ config/SVG validation
→ 현재 Tarkov.dev floor spatial extents와 결합
→ Map candidate
→ active
```

`map_configs.json`에서 사용하는 핵심 값:

- map key / aliases
- SVG filename
- source image width / height
- `playerMarkerTransform` 2D affine matrix
- floor SVG layer IDs

좌표식:

```text
surfaceX = a * worldX + b * worldZ + tx
surfaceY = c * worldX + d * worldZ + ty
```

Map surface 크기가 달라도 source image width/height 비율로 정규화합니다.

최신 legacy source를 받을 수 없거나 검증 실패하면 코드에 기록된 pinned known-good legacy bundle로 fallback합니다. 이미 active인 정상 candidate는 update 실패로 삭제하지 않습니다.

현재 Map pipeline version:

```text
legacy-tarkov-helper-map-minimap-v2-atomic-upstream
```

refresh 조건:

- usable active asset 없음
- Game Content Map/marker fingerprint 변경
- Data Update 성공
- pipeline version 변경
- 마지막 성공 확인 후 24시간 경과
- 사용자 수동 refresh

---

# Main Map 사용자 기능

- Map dropdown
- floor selector
- legacy Tarkov Helper SVG artwork
- wheel zoom / drag pan / reset
- 선택 Map의 current Quest 목록
- dynamic online marker checkbox
- user marker
- screenshot current position + heading
- trail
- raid-log Map auto switch
- MiniMap toggle

Loot marker는 기본 OFF이며 대량 marker는 `DrawingContext` bulk layer를 사용합니다.

---

# MiniMap

현재 legacy Tarkov Helper MiniMap 핵심 UX를 기준으로 재구현했습니다.

- separate borderless always-on-top WPF window
- legacy Map과 같은 artwork/floor
- Map과 marker/player/trail 상태 공유
- player tracking / fixed view
- player-centered tracking
- mouse wheel zoom
- middle-button pan
- opacity
- click-through
- `Ctrl+Shift+M` click-through 해제
- NumPad +/- zoom
- PageUp/PageDown floor 이동
- detected floor 복귀
- window position / size / settings persistence
- Map 탭을 벗어나도 MiniMap 유지

과거 legacy 회귀 원인은 `docs/LEGACY_MAP_PORT.md`에 기록되어 있으며 그대로 복제하지 않습니다.

---

# Scanner

탭과 placeholder만 있습니다. 실제 기능은 별도 제품 요구사항 확정 전까지 구현하지 않습니다.

---

## 현재 다음 작업

1. PR #61 최신 Windows x64 빌드에서 **기존 Tarkov Helper artwork가 실제로 표시되는지** 확인
2. Ground Zero를 시작으로 artwork crop/aspect/가독성 확인
3. extract / Quest / player marker 좌표 정합 확인
4. multi-floor 수동/자동 전환 확인
5. MiniMap player-follow / fixed view / zoom / pan 확인
6. MiniMap click-through + hotkey 확인
7. Map 탭 밖에서도 MiniMap 유지 확인
8. Windows 결과를 반영해 Map/MiniMap 2차 수정
9. visual/coordinate validation이 끝난 뒤 PR #61을 최종 Map 기준으로 확정
