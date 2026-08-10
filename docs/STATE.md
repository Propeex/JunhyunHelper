# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

## 현재 상태

**v0.1.0 RELEASE READY — `준현 헬퍼` Windows x64 single-file portable**

기준일: **2026-08-10**

최신 사용자 요청 반영:

```text
PR #74 — Restore Scanner tab and ship clean 준현 헬퍼 executable: MERGED
merge: e282fffebcb1004ddab0b028b6db5ad0d88db279
final PR head: 47f3ec4cabf70879465b216bc42fecea23e514da
final PR CI: 31356282143 — SUCCESS
final artifact: 9050775673
artifact sha256: 6db752972b3b52d9e6239c746bb910904a91d364c2410062f4c1635ac61efcaa
```

현재 확인된 기능/패키징 blocker는 없습니다.

---

# 1. 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / Windows 실사용 검증 완료 |
| Scanner | 요구사항 미확정 / **`준비 중` 탭은 사용자 요청으로 표시 유지** |

Scanner는 실제 기능을 임의 구현하지 않습니다. 상단 탭은 항상 보이되 현재 화면에는 `준비 중`임을 명확히 표시합니다.

---

# 2. 제품 이름 / Windows 실행 파일

공식 사용자 표시 이름:

```text
준현 헬퍼
```

Windows 실행 파일:

```text
준현 헬퍼.exe
```

C# namespace / 저장소 내부 식별자는 기존 `JunhyunHelper`를 유지합니다. AssemblyName만 사용자 표시 이름과 일치시켜 소스 namespace를 불필요하게 변경하지 않습니다.

EXE 이름 변경 때문에 Map 전역 hotkey의 foreground allowlist도 함께 갱신했습니다. Tarkov 게임과 `준현 헬퍼`가 활성 창일 때 기존 제품 단축키 계약을 유지합니다.

---

# 3. v0.1.0 배포 구조

Windows x64 배포는 **self-contained single-file portable**입니다.

사용자가 압축을 풀면 루트는 다음만 보이는 것을 기준으로 합니다.

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

정책:

- .NET / WPF / SQLite / SkiaSharp managed/native runtime → `준현 헬퍼.exe` bundle
- `PublishTrimmed=false` 유지
- native runtime은 .NET single-file host가 관리
- Map이 파일 경로로 직접 읽는 artwork/config/general-marker asset만 `Assets/`에 외부 유지
- root DLL: **0개**
- PDB: **0개**
- nested ZIP: **없음**
- AutoUpdater/WebView2/GraphX/QuikGraph: **없음**

DLL을 임의의 `lib/` 폴더로 옮기는 방식은 사용하지 않습니다. .NET/native loader와 Map 회귀 위험이 있기 때문에 검증된 single-file bundle 방식으로 해결했습니다.

---

# 4. 사용자 데이터 / 로그

기본 사용자 데이터 루트:

```text
%LocalAppData%/JunhyunHelper
├─ user.db
├─ content/<game-mode>/...
├─ image-cache/
├─ map-product-settings.json
├─ ammo-favorites.json
└─ Logs/
```

PR #74에서 transplanted Map logger의 과거 `실행폴더/Logs` 정책을 제거했습니다. 이제 프로그램을 실행해도 EXE 옆에 `Logs` 폴더가 생기지 않습니다.

프로그램 ZIP을 교체해도 User Progress와 위 로컬 데이터는 프로그램 폴더와 분리되어 유지됩니다.

---

# 5. 핵심 데이터 원칙

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
- 실패 candidate가 마지막 정상 active content를 덮어쓰지 않음
- Game Content와 User Progress 분리
- Game Content update가 `user.db`를 삭제/덮어쓰지 않음
- runtime GPT/AI 의존성 없음
- 현재 Content snapshot schema: **v4**

---

# 6. Core 기능 기준

## Profile

- GameMode별 독립 profile
- level / faction / edition / prestige / Trader / Fence
- 기존 profile 수정은 창 close 시 저장

## Quest

- Current / Locked / Unavailable / Completed
- prerequisite / item requirement / stable-ID navigation
- residual Indeterminate → user-facing Current fallback
- 고정 제출 Item 자동 소비 ledger + rollback 복원 선택
- v4 online Quest `possibleLocations` / `zones` geometry

## Hideout

- 현재 level / 다음 upgrade material
- 미래 upgrade 전체 → Needed Items
- upgrade material 자동 소비 ledger + rollback 복원 선택

## Needed Items / Inventory

- 미래 Quest + 미래 Hideout 기준 필요량
- 인레이드 / 일반 필요량과 보유량
- flexible hand-in 그룹
- 안전하게 증명 가능한 초과분만 cleanup

## Ammo

- json.tarkov.dev raw stats
- healthy Wiki Ballistics membership + Armor Class 1~6 effectiveness
- caliber / 공급 경로 / unlock Quest / favorite shortcut

---

# 7. Map + MiniMap 기준

Map/MiniMap은 사용자가 검증한 특정 `Propeex/Tarkov-Helper` 기준선을 명시적으로 채택한 예외적 subsystem입니다.

```text
exact baseline:
9371c4769d8da8acb9df864a2c88f83ecdd42818

product source:
Propeex/Tarkov-Helper
branch: junhyun-map-product-v2

pinned submodule revision:
d933792b6042a51cea38dc44b686a096fe30de67

submodule:
vendor/Tarkov-Helper
```

아키텍처 원칙:

```text
Map subsystem = 독립
└─ Quest만 JunhyunHelper current profile/content와 연결
```

현재 제품 동작:

- Current Quest sidebar + A/B/C... marker identity
- Main Map / MiniMap Quest marker 동기화
- 일반 marker / PMC·Scav·Transit extract
- manual floor dropdown
- Main Map + MiniMap floor up/down hotkey
- Main Map + MiniMap zoom in/out hotkey
- Tarkov 게임 foreground에서도 product hotkey 허용
- screenshot 기반 Map 전환 / player X-Z / 가능한 경우 heading
- MiniMap 우측 상단 고정
- MiniMap mouse drag/resize 금지
- MiniMap size hotkey
- hover hide / N초 temporary hide
- MiniMap normal opacity 10%~100%
- MiniMap non-player marker scale 25%~150%
- player marker size 별도
- other-floor opacity 0%, auto-floor OFF

Map 제품 설정:

```text
%LocalAppData%/JunhyunHelper/map-product-settings.json
```

상세: `docs/MAP_PRODUCT_REQUIREMENTS.md`

---

# 8. 최근 릴리즈 하드닝

## PR #73

- old Tarkov-Helper updater 제거
- AutoUpdater/WebView2/GraphX/QuikGraph 제거
- PDB 제거
- nested ZIP 제거
- 숨은 legacy keyboard command/logging 제거
- NuGet direct/transitive vulnerability audit를 release gate로 적용

## PR #74

- 사용자 요청으로 Scanner `준비 중` 탭 복구
- 프로그램/EXE 이름을 `준현 헬퍼`로 통일
- single-file publish 도입
- 배포 root DLL 0개
- Map path-addressed asset만 `Assets/` 유지
- runtime Map log를 `%LocalAppData%/JunhyunHelper/Logs`로 이동
- 실제 `준현 헬퍼.exe`로 Map/MiniMap smoke 검증

---

# 9. PR #74 최종 Release Gate

```text
final head: 47f3ec4cabf70879465b216bc42fecea23e514da
CI run: 31356282143
merge: e282fffebcb1004ddab0b028b6db5ad0d88db279
```

검증:

```text
[x] Desktop Release build
[x] automated tests — 163 passed / 0 failed
[x] Windows x64 self-contained single-file publish
[x] Korean executable `준현 헬퍼.exe`
[x] real Map + MiniMap startup smoke
[x] normal Main Window close + process exit
[x] 실행 후 EXE 옆 `Logs` 폴더 없음
[x] release root DLL 0개
[x] PDB 0개
[x] nested ZIP 없음
[x] legacy forbidden dependency 없음
```

최종 PR artifact:

```text
artifact id: 9050775673
size: 73,973,345 bytes
sha256: 6db752972b3b52d9e6239c746bb910904a91d364c2410062f4c1635ac61efcaa
```

**현재 v0.1.0은 release-ready입니다.**

---

# 10. 의도적으로 남긴 비차단 범위 / 다음 작업

## Scanner

제품 surface는 유지하지만 실제 기능은 `PRODUCT OPEN`입니다. 다음 Scanner 작업은 기능 의미/입력/출력/검증 기준을 사용자와 확정한 뒤 구현합니다.

## Map bundle updater

Map artwork/config/general-marker bundle은 현재 pinned bundle을 배포합니다. 향후 updater는 같은 upstream revision의 artwork/config/general-marker DB를 한 원자적 bundle로 갱신해야 합니다.

## Code signing / installer / app updater

현재 v0.1.0 blocker가 아닙니다. 배포 규모가 커질 때 별도 설계합니다.

## User DB backup

`user.db`는 Game Content/update 파일과 분리되어 있습니다. 자동 백업 UX는 후속 편의 기능 후보입니다.

## Repository license / third-party notices

불특정 다수에게 본격 공개·재배포할 경우 프로젝트 license와 third-party notice 정책을 명시적으로 정리합니다.
