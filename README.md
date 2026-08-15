# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 현재 상태

**v0.1.5 PUBLIC RELEASE — Windows x64**

현재 공개 버전은 **v0.1.5**입니다.

**현재 공개 다운로드:** https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.5

v0.1.5는 v0.1.4 실사용에서 확인된 두 Map 회귀를 수정하는 패치입니다.

- Main Map의 다른 층 일반 marker가 잠깐 보인 뒤 깜박이며 사라지는 문제
- 층 변경 시 MiniMap의 현재 지도 중심이 초기/이전 위치로 돌아가는 문제

> **v0.1.4 → v0.1.5:** 필수 `데이터 업데이트`가 없습니다. Content schema v5와 `user.db`는 변경하지 않습니다.

## 주요 기능

- GameMode별 Profile 관리
- Quest 진행/잠김/사용 불가/완료 판정과 선행 Quest 연결
- Quest 제출 Item / 자동 소비·rollback ledger
- Hideout 레벨 / 미래 업그레이드 재료
- 미래 Quest + Hideout 기준 Needed Items
- FIR / 일반 Inventory와 안전한 cleanup 계산
- flexible hand-in 그룹 + `필요 / 전체 / 충분` 상태 필터
- Item 종류/용도/필요 상태 필터, cross-navigation, Item Wiki
- Ammo 성능/수급처/Armor Class 1~6 비교와 caliber favorites
- 온라인 Game Content 안전 업데이트와 image cache
- Map + MiniMap
  - 현재 Quest sidebar / A·B·C marker identity
  - 일반 marker / PMC·Scav·Transit 탈출구
  - floor / zoom / MiniMap 크기 hotkey
  - floor 변경 시 Main Map + MiniMap zoom/지도 중심 위치 보존
  - 타층 marker 유지 + 현재층 초록 / 위층 빨강 / 아래층 파랑 compact ring
  - MiniMap opacity / temporary hide / marker scale
  - screenshot 기반 Map 전환 / player tracking
- 상단 `스캐너` 탭 — 현재 `준비 중` placeholder 유지

## v0.1.5 Map 회귀 패치

### 타층 일반 marker

v0.1.4에서는 같은 종류의 서로 다른 층 marker가 비슷한 X/Z에 있으면 대표 하나만 남기는 정리 로직이 있었습니다. legacy marker가 비동기로 추가된 뒤 이 로직이 뒤늦게 실행되면서 다른 층 marker가 `보임 → 깜박임 → 사라짐` 상태가 될 수 있었습니다.

v0.1.5에서는 서로 다른 floor라는 이유만으로 일반 marker를 숨기지 않습니다. category가 켜져 있으면 각 marker를 유지하고 current/above/below floor relation만 표현합니다. Factory `Gate 3`처럼 source상 실제 같은 물리 탈출구로 확인되는 semantic duplicate 정규화는 유지합니다.

### MiniMap floor viewport

MiniMap의 PlayerTracking 현재 중심은 live `MapTranslate`에 갱신되지만 persisted offset은 이전 값일 수 있습니다. 기존 floor renderer가 SVG 교체 뒤 stale offset을 다시 적용하여 중심이 초기/이전 위치로 점프할 수 있었습니다.

v0.1.5에서는 floor up/down과 NumPad 직접 층 선택 모두 변경 직전의 live zoom + map-space 중심을 저장하고 floor render 뒤 복원합니다. 층을 바꿔도 Main Map과 MiniMap에서 보고 있던 위치가 유지되어야 합니다.

공개 릴리즈 검증:

```text
release baseline: 2ff504c24661b6e37ec40e685dd344ce5581350f
branch CI: 31863894702 — SUCCESS
main CI: 31864041783 — SUCCESS
release workflow: 31864223946 — SUCCESS
177 tests passed / 0 failed
Windows x64 self-contained single-file publish: SUCCESS
Main Map off-floor async-settle smoke: SUCCESS
Factory Gate 3 / Office Window smoke: SUCCESS
MiniMap floor viewport preservation smoke: SUCCESS
normal Main Window close / process exit: SUCCESS
asset: Junhyun-Helper-v0.1.5-win-x64.zip
SHA-256: 565bf0ad01ac9ec8385e99b26aa692e0962550a0c975a889e4b56ad33a6a41f7
release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.5
```

## v0.1.4 Main Map / Quest 정확도 패치

- 다른 층 marker/extract를 숨기지 않고 floor 관계를 compact ring으로 표시
  - 현재층: 초록
  - 위층: 빨강
  - 아래층: 파랑
  - 알려진 타층: 약 75% opacity
- Factory `Gate 3`의 동일 물리 PMC/Scav 탈출구 중복 visual 정규화
- `Office Window` 같은 Scav marker 본체 의미와 floor ring 의미 분리
- 프로그램만으로 정확히 판정할 수 없는 Quest를 `진행 중`으로 가장하지 않고 `확인 필요`로 분리
- `확인 필요`는 정확한 Current count와 Map Current Quest sidebar에서 제외
- Future Needed Items의 보수 보호 유지

공개 릴리즈:

```text
release baseline: 68038c6aac43e91f9ba8e810918eed389c753dea
asset: Junhyun-Helper-v0.1.4-win-x64.zip
SHA-256: 0238d059f3c714c826c2a962b30e5361b6e3e16c247d2657993a612aed8d8ef9
```

## v0.1.3 Map/MiniMap 핫픽스

v0.1.2 실사용에서 확인된 지도 탭 지연과 타층 marker 표시 회귀를 수정했습니다.

- Main Map 표준 marker 전체를 200ms마다 순회하던 영구 UI polling 제거
- marker tree/map/floor가 실제로 바뀔 때만 one-shot debounce로 floor presentation 갱신
- 신뢰 가능한 Quest height가 없는 경우 `main`으로 추측하지 않고 floor unknown으로 유지
- MiniMap의 중복 off-floor renderer/timer 제거, 기존 canonical marker/extract 경로로 통합
- Quest/Raider scale 갱신의 별도 polling을 event/signature 기반으로 전환
- MiniMap Raider floor/zoom/marker-scale/container reload 갱신 보강
- legacy extract refresh가 컨테이너를 비운 뒤 타층 extract가 사라진 채 남는 경우 복구
- floor-hotkey zoom + map-space viewport-center 보존 유지

## v0.1.2 사용성 패치

- 층 단축키로 floor를 바꿔도 기존 zoom과 보고 있던 지도 중심 위치를 유지
- 다른 층 marker를 숨기지 않고 약 50% 투명도 + 위층 `↑` / 아래층 `↓`로 구분
- Main Map / MiniMap의 Quest·일반 marker·탈출구·Raider 층 표현 통일
- 유동 제출 상태 dropdown `필요 / 전체 / 충분`; 모두 모은 그룹은 기본 `필요` 목록에서 자동 제외
- Item 상세에 canonical Wiki URL 기반 `위키` 버튼 추가

## v0.1.1 Quest 정확도 패치

2026-08-15 최신 live 데이터를 기준으로 Quest prerequisite/availability를 다시 감사했습니다.

- `taskRequirements`의 `active / complete / failed` 상태 모델 재검증
- Lightkeeper / BTR Driver / Ref 상인 접근 이후 후속 Quest가 너무 일찍 열릴 수 있던 공백 수정
- `globalVariable` / `dialogue`처럼 현재 User Progress만으로 확정할 수 없는 조건은 추측하지 않고 `확인 필요`로 분리
- 각 GameMode의 시간 지연 Quest 13개에 대한 min/max delay metadata 보존
- 실제 게임 완료 시각을 알 수 없으므로 잘못된 가짜 countdown은 생성하지 않음
- Content snapshot schema v5

실제 current online source 전체 검증:

```text
regular:    517 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
pve:        513 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
pvp-season: 490 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
validation errors: 0
importer warnings: 0
```

## 실행

1. GitHub Release에서 최신 `Junhyun-Helper-vX.Y.Z-win-x64.zip`을 다운로드합니다.
2. 원하는 폴더에 압축을 풉니다.
3. **`준현 헬퍼.exe`**를 실행합니다.

배포 루트:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

Windows x64 portable / self-contained single-file 빌드이므로 별도 .NET 설치나 관리자 권한은 필요하지 않습니다. 코드 서명은 아직 적용하지 않아 Windows SmartScreen 경고가 표시될 수 있습니다.

사용자 데이터와 로그는 프로그램 폴더가 아니라 `%LocalAppData%/JunhyunHelper`에 저장됩니다.

## 데이터 원칙

```text
online source
→ download
→ 외부 형식/필수 의미 검증
→ canonical 변환
→ candidate DB
→ 관계/read-back 검증
→ active 교체
→ User Progress와 결합
```

실패 candidate가 마지막 정상 Game Content를 덮어쓰지 않으며 Game Content update가 `user.db`를 삭제하거나 덮어쓰지 않습니다. Runtime GPT/AI 의존성은 없습니다.

## 개발 문서

- [`docs/STATE.md`](docs/STATE.md) — 현재 프로젝트 상태
- [`docs/PRODUCT.md`](docs/PRODUCT.md) — 공식 제품 요구사항
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — 장기 설계 결정
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 기술 구조
- [`docs/MAP_PRODUCT_REQUIREMENTS.md`](docs/MAP_PRODUCT_REQUIREMENTS.md) — Map/MiniMap 제품 기준
- [`docs/RELEASE_0.1.5.md`](docs/RELEASE_0.1.5.md) — v0.1.5 Map 회귀 패치 기록
- [`docs/FEEDBACK_2026-08-15_OFF_FLOOR_MARKER_FLICKER.md`](docs/FEEDBACK_2026-08-15_OFF_FLOOR_MARKER_FLICKER.md) — 타층 일반 marker 소실 회귀
- [`docs/FEEDBACK_2026-08-15_MINIMAP_FLOOR_CENTER_RESET.md`](docs/FEEDBACK_2026-08-15_MINIMAP_FLOOR_CENTER_RESET.md) — MiniMap floor viewport 회귀
- [`docs/QUEST_PREREQUISITE_AUDIT_2026-08-15.md`](docs/QUEST_PREREQUISITE_AUDIT_2026-08-15.md) — 최신 Quest 선행/해금 조건 감사
- [`docs/RELEASE_0.1.4.md`](docs/RELEASE_0.1.4.md) — v0.1.4 릴리즈 기록
- [`docs/RELEASE_0.1.3.md`](docs/RELEASE_0.1.3.md) — v0.1.3 핫픽스 릴리즈 기록
- [`docs/RELEASE_0.1.2.md`](docs/RELEASE_0.1.2.md) — v0.1.2 릴리즈 기록
- [`docs/RELEASE_0.1.1.md`](docs/RELEASE_0.1.1.md) — v0.1.1 릴리즈 기록
