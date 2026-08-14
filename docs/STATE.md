# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 상태

**v0.1.2 USABILITY RELEASE CANDIDATE — 2026-08-15**

현재 공개 버전은 **v0.1.1**입니다. PR #77에서 층 전환/타층 marker/유동 제출/Item Wiki 피드백을 반영했고, Windows runtime regression smoke까지 통과했습니다. 최종 문서/버전 반영 후 PR-head CI를 다시 통과시키고 v0.1.2로 공개합니다.

## v0.1.2 변경

- Main Map floor up/down hotkey가 zoom과 viewport 중앙의 map-space 위치를 보존
- NumPad 0~5 직접 floor 선택도 같은 viewport-safe floor renderer 사용
- 다른 층 marker를 숨기지 않고 약 50% opacity로 유지
- `Floor.Order` 기준 위층 `↑` / 아래층 `↓` badge
- Main Map / MiniMap의 Quest, 일반 marker, extract, Raider floor 표현 통일
- 유동 제출 상태 dropdown `필요 / 전체 / 충분`
- 유동 제출 기본 `필요`; 모든 objective 충족 group은 `필요`에서 자동 제외
- Item 상세 canonical Wiki URL `위키` 버튼
- 타층 badge 재사용 및 MiniMap 타층 extract signature cache로 불필요한 반복 UI 생성 감소

상세: `docs/USABILITY_REQUIREMENTS_2026-08-15.md`, `docs/MAP_PRODUCT_REQUIREMENTS.md`

## 검증

기능 head 검증 run `31827542036`: SUCCESS

```text
Desktop Release build: SUCCESS
automated tests: 176 passed / 0 failed
Windows x64 self-contained single-file publish: SUCCESS
Main Map multi-floor SVG switch: SUCCESS
other-floor ↑/↓ + opacity visual smoke: SUCCESS
floor-hotkey zoom + map-space viewport-center preservation: SUCCESS
MiniMap window / zoom / floor / marker-scale smoke: SUCCESS
normal Main Window close / process exit: SUCCESS
```

v0.1.2 version metadata와 공식 문서를 반영한 최종 PR-head CI를 release gate로 사용합니다.

## v0.1.1 Quest 정확도 기준 유지

- current `taskRequirements`의 `active / complete / failed` 상태 모델
- Lightkeeper / BTR Driver / Ref 상인 접근 gate 보강
- `globalVariable` / `dialogue` unresolved condition은 `판정 문제`에 보존
- `availableDelaySecondsMin/Max` canonical 보존, 가짜 countdown 없음
- Content snapshot schema **v5**
- `user.db` schema/progress 변경 없음

2026-08-15 live product importer/validator 기준:

```text
regular:    517 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
pve:        513 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
pvp-season: 490 quests / 5312 items / 16 traders / 17 maps / 26 hideout / 200 ammo
validation errors: 0
importer warnings: 0
```

## 업그레이드 정책

- **v0.1.1 → v0.1.2:** 필수 `데이터 업데이트` 없음. Content schema는 그대로 v5이며 이번 패치는 UI/Map 사용성 변경입니다.
- **v0.1.0 → v0.1.2:** v0.1.1에서 도입된 최신 Quest 판정을 적용하려면 설치 후 `데이터 업데이트`를 한 번 실행합니다.
- `%LocalAppData%/JunhyunHelper/user.db`의 Profile / Quest 완료 / Inventory / Hideout 진행은 유지됩니다.

## 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / v0.1.1 live prerequisite audit 유지 |
| Hideout | 구현 완료 / current live validation 통과 |
| Needed Items / Inventory | 구현 완료 / v0.1.2 유동 제출 상태 filter + Item Wiki |
| Ammo | 구현 완료 / current live validation 통과 |
| Map + MiniMap | 구현 완료 / v0.1.2 floor viewport + other-floor marker semantics |
| Scanner | `준비 중` placeholder 탭 유지 / 실제 기능 PRODUCT OPEN |

## 핵심 데이터 원칙

```text
online source
→ download
→ external shape/semantic validation
→ canonical transform
→ candidate DB
→ relationship/read-back validation
→ active swap
→ User Progress와 결합
```

Quest를 포함한 Game Content 분류는 프로그램 importer가 수행하며 runtime GPT/AI 의존성은 없습니다. 실패 candidate는 last-known-good active content를 덮지 않고 Game Content update는 `user.db`를 삭제/덮어쓰지 않습니다.

## Map 기준

Map subsystem은 독립이고 Quest만 JunhyunHelper current profile/content와 연결합니다. pinned submodule revision은 `d933792b6042a51cea38dc44b686a096fe30de67`입니다.

## 현재 공개 릴리즈

```text
v0.1.1
https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.1
asset: Junhyun-Helper-v0.1.1-win-x64.zip
SHA-256: 91394101c5011b833c2810d8857fe2e9fd59b9f42f8710b90a899fe8169f0b54
```

## 비차단 후속 범위

- Scanner 실제 기능 설계/구현
- Map artwork/config/general-marker atomic bundle updater
- deeper pinned Map renderer refactor only when regression risk is justified
- code signing / installer / application updater
- user.db backup/restore UX
- repository license / third-party notice 정책
