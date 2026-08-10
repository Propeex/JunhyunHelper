# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**v0.1.0 Release Hardening — 첫 Windows x64 배포 준비**

기준일: **2026-08-10**

현재 작업:

```text
PR #73 — Release hardening for v0.1.0
→ 기능 추가가 아니라 배포/의존성/미완성 surface/문서 정리
→ 전체 CI + 실제 Map/MiniMap smoke 통과 후 main 병합 예정
```

---

# 1. 제품 전체 상태

현재 사용자 기능 구현 상태:

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / Windows 실사용 검증 완료 |
| Scanner | 요구사항 미확정 / v0.1.0 UI 비노출 |

사용자는 PR #72 Windows 빌드에서 최근 Map/MiniMap 피드백 항목을 포함해 주요 기능이 정상 동작한다고 확인했습니다.

v0.1.0에서 Scanner를 임의 구현하거나 `준비 중` 탭으로 노출하지 않습니다. Scanner는 별도 제품 요구사항 확정 후 후속 범위에서 진행합니다.

---

# 2. 제품 핵심 원칙

온라인 Game Content는 프로그램이 직접 내려받고 다음 흐름으로 갱신합니다.

```text
온라인 source
→ 다운로드
→ 외부 형식/필수 의미 검증
→ canonical 변환
→ candidate DB
→ 관계/read-back 검증
→ active 교체
→ 이미지 cache 준비
→ User Progress와 결합
```

- 일반 Tarkov 데이터 내용 변경은 importer가 이해하는 한 자동 흡수합니다.
- 외부 schema/의미가 비호환으로 바뀌면 update를 실패시킵니다.
- 실패한 candidate가 마지막 정상 active content를 덮어쓰지 않습니다.
- Game Content와 User Progress는 분리합니다.
- update가 `user.db`를 삭제하거나 덮어쓰지 않습니다.
- runtime GPT/AI 의존성은 없습니다.

현재 Quest geometry를 포함한 Content snapshot schema는 **v4**입니다. 이전 snapshot은 온라인 source에서 재구축하며 User Progress migration과 결합하지 않습니다.

---

# 3. 사용자 데이터

기본 루트:

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
map-product-settings.json
ammo-favorites.json
```

프로그램 ZIP을 새 버전으로 교체해도 위 사용자 데이터는 프로그램 폴더와 분리되어 유지됩니다.

---

# 4. Core 제품 동작

## Profile

- GameMode별 profile
- level / faction / edition / prestige / Trader / Fence 상태
- 기존 profile 수정은 창을 닫을 때 현재 입력값 저장
- 새 profile 생성은 명시적 생성 흐름 유지

## Quest

- Current / Locked / Unavailable / Completed
- prerequisite / item requirement / stable-ID navigation
- residual Indeterminate는 사용자 workflow에서 Current fallback
- 완료/완료 취소와 필요한 permanent failure 조작
- 고정 제출 Item 자동 소비 ledger + rollback 복원 선택
- online `possibleLocations` / `zones` Quest geometry 저장

## Hideout

- 현재 level
- 다음 upgrade material
- 미래 upgrade 전체가 Needed Items에 반영
- upgrade material 자동 소비 ledger + rollback 복원 선택

## Needed Items / Inventory

- 미래 Quest + 미래 Hideout 기준 필요량
- FIR(인레이드) / 일반 필요량과 보유량
- flexible hand-in 그룹 계산
- 안전하게 증명 가능한 초과분만 cleanup
- Item 종류 / 용도 / 필요 상태 필터
- +/- 연속 입력 coalescing과 workspace reuse로 mutation refresh 최적화

## Ammo

- json.tarkov.dev raw stats
- 검증된 Wiki Ballistics membership + Armor Class 1~6 effectiveness
- caliber filter / 고정 sort / 공급 경로 / unlock Quest navigation
- caliber favorite shortcut menu
- dark inactive-selection 및 vertical grid line 적용

---

# 5. Map + MiniMap 기준

## 기준 source

JunhyunHelper의 Map은 PR #62에서 `Propeex/Tarkov-Helper` Map + MiniMap subsystem을 exact-source 기준으로 이식한 뒤 제품 요구사항에 맞게 제한/보완했습니다.

```text
exact baseline Tarkov-Helper revision:
9371c4769d8da8acb9df864a2c88f83ecdd42818

product source repository:
Propeex/Tarkov-Helper

product branch:
junhyun-map-product-v2

currently pinned revision:
d933792b6042a51cea38dc44b686a096fe30de67

JunhyunHelper submodule:
vendor/Tarkov-Helper
```

기존 `Propeex/Tarkov-Helper` main은 새 제품의 사양 기준으로 사용하지 않습니다.

## 독립성

확정 제품 원칙:

```text
Map subsystem = 독립
└─ Quest만 JunhyunHelper current profile/content와 연결
```

Map artwork/config/general marker/MiniMap/hotkey/screenshot tracking은 Hideout/Item/Ammo runtime과 결합하지 않습니다.

## 현재 사용자 동작

- 현재 선택 Map의 Current Quest sidebar
- Quest별 A/B/C... marker identity
- Main Map / MiniMap 동일 Quest marker identity
- 일반 marker / PMC·Scav·Transit extract
- 수동 floor dropdown
- Main Map + MiniMap floor up/down global hotkey
- Main Map + MiniMap zoom in/out global hotkey
- 게임(`EscapeFromTarkov`, `EscapeFromTarkov_BE`) foreground에서도 product hotkey 허용
- screenshot 기반 Map 전환 / player X-Z / 가능한 경우 heading
- MiniMap 우측 상단 고정
- MiniMap mouse drag/resize 금지
- MiniMap size increase/decrease hotkey
- MiniMap hover 0% hide
- MiniMap 설정형 N초 temporary hide
- MiniMap 기본 opacity 10%~100%
- MiniMap non-player marker scale 25%~150%
- player marker size는 별도 설정 유지
- 다른 floor opacity 0%, auto-floor OFF

Map 제품 설정의 권위 저장소:

```text
%LocalAppData%/JunhyunHelper/map-product-settings.json
```

상세 제품 기준: `docs/MAP_PRODUCT_REQUIREMENTS.md`

최근 사용자 피드백 기록:

- `docs/PRODUCT_FEEDBACK_2026-08-10_04.md`
- `docs/MAP_HOTKEY_FEEDBACK_2026-08-10_05.md`
- `docs/MAP_MINIMAP_HOTKEY_FIX_2026-08-10_06.md`
- `docs/MAP_FLOOR_HOTKEY_RENDER_FIX_2026-08-10_07.md`
- `docs/MAP_MINIMAP_MARKER_SIZE_FEEDBACK_2026-08-10_08.md`

---

# 6. 최근 완료된 Map 릴리즈 후보 이력

```text
PR #68 — settings / input / lifecycle / performance
PR #69 — MiniMap zoom / floor hotkey / resize policy
PR #70 — MiniMap legacy hook conflict / real MiniMap smoke
PR #71 — Main Map floor render serialization + MiniMap opacity control
PR #72 — MiniMap marker-size control
```

PR #72 최종 검증:

```text
Desktop Release build: success
automated tests: 163 passed / 0 failed
Windows x64 self-contained publish: success
real Map + MiniMap smoke: success
graceful Main Window close + process exit: success
```

---

# 7. v0.1.0 Release Hardening — PR #73

릴리즈 전 감사에서 확인한 비기능 문제:

1. publish에 `libSkiaSharp.pdb` 약 89MB가 포함됨
2. old Tarkov-Helper `UpdateService`가 컴파일되어 legacy `Tarkov-Helper/update.xml` 경로와 AutoUpdater/WebView2가 release dependency에 남아 있음
3. 사용하지 않는 GraphX/QuikGraph direct/transitive dependency가 남아 있음
4. GitHub Artifact가 ZIP 안에 ZIP을 넣는 구조였음
5. Scanner 요구사항이 미확정인데 상단 탭이 `준비 중`으로 노출됨
6. README / 공식 상태 문서 / 배포 안내문 일부가 Map 구현 이전 상태를 가리킴

PR #73 처리:

- legacy UpdateService 컴파일 제외
- AutoUpdater/WebView2 제거
- GraphX/QuikGraph 제거
- publish 후 모든 PDB 제거
- CI에서 PDB와 제거 대상 dependency가 다시 들어오면 실패
- GitHub Artifact에 publish directory를 직접 업로드하여 nested ZIP 제거
- Scanner tab release UI 비노출
- release 안내문/README/STATE 최신화

핵심 사용자 기능 로직과 사용자 데이터 schema는 변경하지 않습니다.

---

# 8. 릴리즈 형태

v0.1.0 목표 배포:

```text
Windows x64
portable ZIP
self-contained .NET 10
installer 없음
관리자 권한 불필요
```

현재 코드 서명은 구성하지 않았으므로 SmartScreen 경고가 표시될 수 있습니다.

애플리케이션 자체 자동업데이터는 v0.1.0 범위가 아닙니다. 특히 transplanted Tarkov-Helper의 old updater는 release에서 제거합니다. Game Content 업데이트는 제품 상단 `데이터 업데이트` 기능이 별도로 담당합니다.

---

# 9. 알려진 비차단 항목 / 후속 범위

## Scanner

`PRODUCT OPEN` — 요구사항 확정 전. v0.1.0에서는 숨김.

## Map bundle update

Quest/Hideout/Item/Ammo Game Content updater와 달리 Map artwork/config/general-marker bundle은 현재 검증된 pinned bundle을 배포물에 포함합니다.

향후 Map bundle updater를 만들 경우 artwork/config/general-marker DB를 **같은 upstream revision의 한 원자적 bundle**로 갱신해야 합니다. 서로 다른 revision을 혼합하지 않습니다.

## Code signing / installer

현재 없음. 첫 portable release의 기능 차단 요소는 아니지만 외부 배포 규모가 커지면 code signing과 설치/업데이트 UX를 별도 설계할 수 있습니다.

## Legacy Map compile warnings

exact-source Map transplant의 일부 nullable/fire-and-forget warning은 `WarningsNotAsErrors`로 명시적으로 분리되어 있습니다. JunhyunHelper 자체 코드의 일반 warning-as-error 정책은 유지합니다. 단순 경고 제거를 위해 upstream Map source 의미를 임의 변경하지 않습니다.

---

# 10. Release Gate

v0.1.0을 release-ready로 판정하려면 PR #73 최종 head에서 다음을 모두 만족해야 합니다.

```text
[ ] Desktop Release build
[ ] automated tests
[ ] Windows x64 self-contained publish
[ ] publish 안에 *.pdb 없음
[ ] AutoUpdater/WebView2/GraphX/QuikGraph 없음
[ ] real Map + MiniMap smoke
[ ] normal Main Window close + process exit
[ ] direct one-layer Artifact 생성
```

이 Gate를 통과하고 새 blocker가 없으면 **v0.1.0 release-ready**로 간주합니다.
