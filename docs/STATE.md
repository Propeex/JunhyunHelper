# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 Phase

**v0.1.0 RELEASE READY — Windows x64 portable**

기준일: **2026-08-10**

현재 작업:

```text
PR #73 — Release hardening for v0.1.0
최종 제품/패키징 감사 및 CI 통과
→ main 병합이 다음 단계
```

---

# 1. 제품 전체 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / Windows 실사용 검증 완료 |
| Scanner | 요구사항 미확정 / v0.1.0 UI 비노출 |

사용자는 PR #72 Windows 빌드에서 최근 Map/MiniMap 피드백을 포함한 주요 사용자 기능이 정상 동작한다고 확인했습니다.

Scanner는 요구사항을 임의 구현하지 않으며 v0.1.0 public UI에 노출하지 않습니다.

---

# 2. 제품 핵심 원칙

온라인 Game Content:

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

- 일반 Tarkov 데이터 내용 변경은 importer가 이해하는 한 자동 흡수
- 비호환 schema/의미 변화는 update 실패
- 실패한 candidate는 마지막 정상 active content를 덮어쓰지 않음
- Game Content와 User Progress 분리
- update가 `user.db`를 삭제/덮어쓰지 않음
- runtime GPT/AI 의존성 없음
- 현재 Content snapshot schema: **v4**

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

프로그램 ZIP을 교체해도 위 사용자 데이터는 프로그램 폴더와 분리되어 유지됩니다.

---

# 4. Core 제품 상태

## Profile

- GameMode별 profile
- level / faction / edition / prestige / Trader / Fence
- 기존 profile 수정은 창 close 시 저장
- 새 profile은 명시적 생성 흐름

## Quest

- Current / Locked / Unavailable / Completed
- prerequisite / item requirement / stable-ID navigation
- residual Indeterminate → user-facing Current fallback
- 완료/완료 취소 / 필요한 permanent failure
- 고정 제출 Item 자동 소비 ledger + rollback 복원 선택
- v4 online Quest geometry

## Hideout

- 현재 level / 다음 upgrade material
- 미래 upgrade 전체 → Needed Items
- upgrade material 자동 소비 ledger + rollback 복원 선택

## Needed Items / Inventory

- 미래 Quest + 미래 Hideout 필요량
- FIR(인레이드) / 일반 필요·보유량
- flexible hand-in 그룹
- 안전하게 증명 가능한 초과분만 cleanup
- Item 종류 / 용도 / 필요 상태 filter
- rapid mutation coalescing + workspace reuse

## Ammo

- json.tarkov.dev raw stats
- healthy Wiki Ballistics membership + Armor Class 1~6 effectiveness
- caliber / 공급 경로 / unlock Quest / favorite shortcut
- dark inactive-selection / vertical grid

---

# 5. Map + MiniMap 기준

## source

```text
exact baseline:
9371c4769d8da8acb9df864a2c88f83ecdd42818

product source:
Propeex/Tarkov-Helper
branch: junhyun-map-product-v2

JunhyunHelper pinned submodule revision:
d933792b6042a51cea38dc44b686a096fe30de67

submodule:
vendor/Tarkov-Helper
```

Map/MiniMap은 사용자가 검증한 특정 기준선을 채택한 예외적 subsystem이며 기존 Tarkov-Helper 전체를 제품 사양으로 승계한 것이 아닙니다.

## 독립성

```text
Map subsystem = 독립
└─ Quest만 JunhyunHelper current profile/content와 연결
```

## 현재 동작

- Current Quest sidebar + A/B/C... marker identity
- Main Map / MiniMap Quest marker 동기화
- 일반 marker / PMC·Scav·Transit extract
- manual floor dropdown
- Main Map + MiniMap floor hotkey
- Main Map + MiniMap zoom hotkey
- 게임 foreground에서도 product hotkey 허용
- screenshot Map 전환 / player X-Z / 가능한 경우 heading
- MiniMap 우측 상단 고정
- MiniMap mouse drag/resize 금지
- MiniMap size hotkey
- hover hide / N초 temporary hide
- MiniMap normal opacity 10%~100%
- MiniMap non-player marker scale 25%~150%
- player marker size 별도
- other-floor opacity 0%, auto-floor OFF

Map 제품 설정 권위 저장소:

```text
%LocalAppData%/JunhyunHelper/map-product-settings.json
```

상세: `docs/MAP_PRODUCT_REQUIREMENTS.md`

---

# 6. 최근 Map 이력

```text
PR #68 — settings / input / lifecycle / performance
PR #69 — MiniMap zoom / floor hotkey / resize policy
PR #70 — MiniMap legacy hook conflict / real MiniMap smoke
PR #71 — Main Map floor render serialization + MiniMap opacity
PR #72 — MiniMap marker-size control
```

PR #72에서 사용자 Windows 실사용 검증을 통과했습니다.

---

# 7. PR #73 — v0.1.0 Release Hardening

최종 감사에서 발견하고 수정한 항목:

1. `libSkiaSharp.pdb` 약 89MB가 publish에 포함됨 → 모든 PDB 제거 + CI 재유입 차단
2. old Tarkov-Helper `UpdateService`가 legacy repository update.xml을 가리킴 → 컴파일 제외
3. AutoUpdater / WebView2 / GraphX / QuikGraph 불필요 dependency → 제거
4. GitHub Artifact가 ZIP 안에 ZIP을 만듦 → publish directory 직접 업로드
5. Scanner가 기능 없이 `준비 중` 탭으로 노출됨 → v0.1.0 UI에서 숨김
6. Map fallback 문구가 구현 전 상태를 표현 → `불러오는 중`으로 교정
7. 공식 README/PRODUCT/ARCHITECTURE/STATE/AGENTS/DEVELOPMENT/Map 요구사항/배포 안내가 과거 상태 → 현재 기준으로 정리
8. 임시 draft PR #12 / 완료된 discovery issue #1 → 정리
9. old `GlobalKeyboardHookService`에 제품과 무관한 숨은 입력 동작 존재:
   - `S → S+D → D → O` secret command
   - Ctrl+L legacy settings shortcut
   - legacy direct overlay hotkey dispatch
   - `%LocalAppData%/TarkovHelper/keyboard_hook.log` 입력/foreground log
   - 광범위한 process-name substring 허용
   → vendor hook을 compile 제외하고 JunhyunHelper-owned compatibility hook으로 교체
   → original Map direct NumPad0~5 floor-selection 계약만 유지
   → product hotkey는 기존 JunhyunHelper dispatcher가 계속 담당
10. NuGet dependency vulnerability audit가 명시적 release gate가 아니었음 → direct+transitive audit 활성화, NU1901~NU1904를 release-blocking

핵심 사용자 데이터 schema와 검증된 Quest/Hideout/Items/Ammo/Map 동작 의미는 변경하지 않습니다.

---

# 8. 최종 Release Gate 결과

PR #73 final audited code head:

```text
6ff9b2cc4a9a7236feed2ef10d0b5122232f2f70
```

CI:

```text
run: 31354467053
Desktop Release build: SUCCESS
automated tests: SUCCESS
Windows x64 self-contained publish: SUCCESS
NuGet direct/transitive vulnerability audit: SUCCESS
no PDB / forbidden legacy dependencies: SUCCESS
real Map + MiniMap smoke: SUCCESS
normal Main Window close + process exit: SUCCESS
one-layer Artifact upload: SUCCESS
```

Audited artifact:

```text
artifact id: 9050171150
size: 80,076,569 bytes
sha256: 5050a45b01797ba91dabd714700a476e524f2c52d271e3d889afd56dc691c50a
entries: 318
nested ZIP: none
PDB: none
AutoUpdater/WebView2/GraphX/QuikGraph: none
legacy hidden keyboard command/log markers in JunhyunHelper.dll: none
```

Release gate:

```text
[x] Desktop Release build
[x] automated tests
[x] Windows x64 self-contained publish
[x] direct/transitive NuGet vulnerability audit
[x] publish 안에 *.pdb 없음
[x] AutoUpdater/WebView2/GraphX/QuikGraph 없음
[x] hidden legacy keyboard behavior/logging 제거
[x] real Map + MiniMap smoke
[x] normal Main Window close + process exit
[x] direct one-layer Artifact
```

**현재 확인된 기능/패키징 blocker는 없습니다. v0.1.0 release-ready로 판정합니다.**

---

# 9. 릴리즈 형태

```text
Windows x64
portable ZIP
self-contained .NET 10
installer 없음
관리자 권한 불필요
```

코드 서명은 아직 구성하지 않아 SmartScreen 경고가 표시될 수 있습니다.

JunhyunHelper application auto-updater는 v0.1.0 범위가 아닙니다. Game Content 업데이트는 상단 `데이터 업데이트`가 담당합니다.

---

# 10. 의도적으로 남긴 비차단 범위

## Scanner

`PRODUCT OPEN` — 요구사항 확정 전. v0.1.0 UI 숨김.

## Map bundle update

Map artwork/config/general-marker bundle은 현재 pinned bundle을 배포합니다. 향후 updater는 같은 upstream revision의 artwork/config/general-marker DB를 한 원자적 bundle로 갱신해야 합니다.

## Code signing / installer / app updater

첫 portable v0.1.0의 기능 blocker로 보지 않습니다. 배포 규모가 커질 때 별도 제품/배포 설계 대상으로 둡니다.

## WinForms runtime

old Map source의 hidden color-dialog 코드가 compile-time dependency를 유지하므로 self-contained package에 WinForms runtime이 남습니다. 이 기능은 제품 UI에서 접근할 수 없습니다. 이를 제거하려면 exact Map source에 더 큰 수술이 필요하며 현재는 패키지 절감보다 회귀 위험이 커서 v0.1.0 blocker로 보지 않습니다.

## Legacy Map compile warnings

exact-source transplant의 일부 nullable/fire-and-forget warning만 `WarningsNotAsErrors`로 분리합니다. JunhyunHelper 자체 warning-as-error 정책은 유지합니다.

## User DB backup

`user.db` 저장은 SQLite 단일 statement/transaction semantics와 검증 경계를 사용하며 프로그램 update와 분리되어 있습니다. 별도 자동 백업 UX는 v0.1.0 필수 범위로 확정하지 않았습니다. 향후 장기 사용 편의 기능으로 검토할 수 있습니다.

## Repository license / third-party notices

저장소 자체 라이선스 선택은 제품 소유자의 배포 정책 결정이 필요한 영역이므로 개발자가 임의로 라이선스를 지정하지 않습니다. 공개 배포 범위를 확대할 경우 프로젝트 license와 third-party notice 정책을 명시적으로 정리합니다.
