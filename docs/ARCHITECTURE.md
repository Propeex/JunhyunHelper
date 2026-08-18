# ARCHITECTURE — 기술 설계

이 문서는 준현 헬퍼의 현재 구현 구조와 장기적으로 지켜야 할 기술 경계를 기록합니다.

## 현재 상태

`CONFIRMED — v0.1.14 PUBLIC VERIFIED`

기술 스택:

- .NET 10
- C#
- WPF Desktop (`net10.0-windows`)
- SQLite (`Microsoft.Data.Sqlite`)
- SkiaSharp — 외부 image decode / PNG normalize
- SharpVectors — SVG Map rendering
- Windows x64 portable / self-contained single-file
- 별도 backend 없음
- runtime AI/GPT 없음

## 1. 솔루션 경계

```text
JunhyunHelper.Core
JunhyunHelper.Infrastructure
JunhyunHelper.Application
JunhyunHelper.Desktop
```

### Core

제품의 권위 도메인 의미를 소유합니다.

- canonical Game Content model
- User Progress model
- Quest availability state
- Needed Items / Inventory planning domain
- trader / hideout / ammo 의미

Core는 WPF, HTTP, SQLite 같은 presentation/infrastructure 세부사항을 알지 않습니다.

### Infrastructure

외부 세계와 persistence를 소유합니다.

- online source download
- canonical conversion 지원
- content DB / SQLite
- image cache
- atomic JSON preference persistence
- GitHub public Release 조회 / update package download / checksum 검증
- program-owned file replacement transaction

### Application

Core와 Infrastructure를 조합하는 product orchestration 계층입니다.

- profile / current content 결합
- Quest / Hideout / Needed Items 계산 orchestration
- user mutation flow

### Desktop

WPF 제품 UI와 Windows runtime integration을 소유합니다.

- MainWindow / pages / controls
- rendered product UI
- Map/MiniMap host integration
- global hotkey/product input
- startup program-update consent flow
- updater apply-mode entrypoint / restart

## 2. 데이터 흐름

### Game Content

```text
online source
→ download
→ source shape / required semantic validation
→ canonical conversion
→ candidate DB write
→ relationship / read-back validation
→ active content replacement
→ image prefetch
→ User Progress와 결합
```

Candidate가 실패하면 마지막 정상 active content를 유지합니다.

### User Progress

```text
Desktop user action
→ Application orchestration
→ Core fact mutation
→ user.db persistence
→ derived state recomputation
→ UI refresh
```

권위 User Progress는 `%LocalAppData%/JunhyunHelper/user.db`에 저장됩니다.

### Derived state

Needed Items, Quest current state, cleanup safety 같은 값은 authoritative persistent fact가 아니라 Content + User Progress에서 계산하는 파생값입니다.

## 3. Content / storage schema

Current Content schema: **v7**

Readable schemas: **v3~v7**

- v3 — Wiki Ballistics membership / effectiveness 분리
- v4 — Quest possibleLocations / zones geometry
- v5 — availability metadata / opaque conditions
- v6 — recoverable special-trader access와 ordinary prerequisite 분리
- v7 — structured globalVariable requirement

User DB:

```text
%LocalAppData%/JunhyunHelper/user.db
SQLite schema v1
```

Optional JSON fields로 exact profile variables, sparse special-trader access, consumption ledger 같은 fact를 확장합니다.

## 4. Preference persistence

Map settings와 Ammo favorites는 작은 JSON preference이며 Game Content/User DB와 별도입니다.

```text
map-product-settings.json
map-product-settings.json.bak
ammo-favorites.json
ammo-favorites.json.bak
```

`AtomicJsonFileStore` 계약:

```text
serialize
→ same-directory temporary file
→ flush to disk
→ previous valid primary를 last-known-good backup으로 보존
→ atomic replacement
```

Load:

```text
primary valid → primary
primary invalid + backup valid → backup
둘 다 unusable → default / caller policy
```

손상 primary를 다시 저장할 때 정상 backup을 손상본으로 덮어쓰지 않습니다.

Presentation preference I/O failure는 앱 전체 fatal로 확대하지 않습니다.

## 5. Quest architecture

Quest availability는 source 의미를 우선합니다.

```text
canonical prerequisite
+ exact User Progress facts
+ 검증된 제한적 compatibility
→ Complete / Current / Indeterminate / Locked / Unavailable
```

Desktop 표시:

```text
Complete      → 완료
Current       → 진행 중
Indeterminate → 확인 필요
Locked        → 잠김
Unavailable   → 사용 불가
```

별도 Accept state/button은 없습니다. EFT에서 받을 수 있는 Quest는 Helper에서 이미 수락한 것으로 간주합니다.

### profile-variable

Canonical v7은 `variableId / operator / required value`를 보존합니다.

판정 우선순위:

1. exact profile current value
2. exact structural proof가 있는 current-version compatibility
3. Indeterminate

Compatibility는 structure drift 시 fail-closed합니다.

### special trader

- BTR Driver — source Active 의미 보존
- Ref — source gate 보존 + missing GameMode unlock 보강
- Lightkeeper — ordinary prerequisites와 recoverable access 분리

## 6. Needed Items / consumption

```text
Content + Profile facts
→ future Quest reachability
→ future Hideout requirements
→ fixed requirements
→ flexible candidate groups
→ cleanup protections
→ inventory-dependent sufficiency
```

수량과 무관한 planning 구조는 `FutureNeededItemsBasis`로 재사용합니다.

- Inventory 수량 변경 → inventory-dependent 부분만 재계산
- Quest/Hideout/profile prerequisite 변경 → full basis rebuild

Fixed completion material은 ledger 기반으로 자동 소비/rollback할 수 있습니다. Flexible candidate는 실제 소비 item을 추측하지 않습니다.

## 7. Image architecture

```text
canonical image URL
→ HTTP bytes
→ SkiaSharp decode
→ validation
→ normalized PNG
→ image-cache
→ WPF BitmapSource
```

Canonical URL이 권위값이며 local image cache는 재생성 가능한 presentation cache입니다.

개별 image 실패는 nonfatal입니다.

## 8. Map / MiniMap boundary

Map subsystem은 pinned `Propeex/Tarkov-Helper` donor revision에서 이식한 독립 subsystem입니다.

Pinned revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

경계:

```text
JunhyunHelper Core/Application
        │
        │ current Quest + Quest geometry
        ▼
Map product bridge
        │
        ▼
pinned donor-derived Map/MiniMap subsystem
```

Map은 Hideout/Item/Ammo runtime과 직접 결합하지 않습니다.

Product-owned bridge가 책임지는 것:

- current Quest projection
- persisted product settings
- map/floor selection synchronization
- product hotkeys
- error containment
- rendered release smoke hooks

Donor renderer 내부의 안정적인 경로는 구체적 regression/performance 이유 없이 wholesale refactor하지 않습니다.

### Floor transform

Floor는 visibility filter가 아니라 relation입니다.

- 타층 marker 유지
- current/above/below presentation
- Main Map floor change → zoom + map-space viewport center 보존
- MiniMap floor change → exact live Scale + Translate X/Y 보존

### Quest sidebar

```text
30px checkbox | 34px marker identity | * Quest title
```

Source inspection이 아니라 실제 WPF rendered X coordinate를 release smoke에서 검증합니다.

## 9. Program Update architecture — v0.1.14

Program Update는 Game Content update와 독립된 subsystem입니다.

### 9.1 Check path

```text
MainWindow visible
→ ProgramUpdateCoordinator.CheckAtStartupAsync
→ GitHubProgramUpdateClient.GetLatestReleaseAsync
→ latest public stable release parse
→ latest > current ? consent UI : no-op
```

조회 timeout은 8초입니다. 조회 실패는 startup fatal이 아니며 diagnostic만 남깁니다.

### 9.2 Download / verify path

```text
user consent
→ %LocalAppData%/JunhyunHelper/updates/pending/<version-guid>
→ SHA256SUMS download
→ exact win-x64 ZIP streaming download
→ SHA-256 verify
→ ZIP security/package validation
→ staging/
```

Validation:

- stable semantic version
- exact expected asset names
- HTTPS GitHub Release asset scope
- exact checksum line
- path traversal reject
- symlink reject
- duplicate reject
- unexpected root reject
- PDB reject
- non-empty `준현 헬퍼.exe`
- non-empty `FIRST_RUN_KO.txt`
- non-empty `Assets/`

검증 이전에는 현재 product files를 수정하지 않습니다.

### 9.3 Apply path

Windows는 실행 중인 EXE를 안전하게 자기 자신으로 교체할 수 없으므로 current single-file EXE를 TEMP에 복사해 updater mode로 실행합니다.

```text
current 준현 헬퍼.exe
→ copy to %TEMP%/JunhyunHelper/updater/<guid>/준현 헬퍼 업데이트.exe
→ updater mode start
→ parent exit wait
→ new files same target volume에 prepare
→ existing owned files previous temp name으로 move
→ new owned files commit
→ success: previous cleanup + new app restart
→ failure: rollback + old app restart attempt
```

Program-owned replacement boundary:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

`user.db`, content/cache/preferences/logs는 app directory 밖이므로 교체 transaction에 포함되지 않습니다.

상시 Updater.exe dependency는 없습니다.

## 10. Runtime failure containment

Presentation/support subsystem의 실패가 전역 WPF fatal로 번지지 않도록 경계를 둡니다.

Nonfatal examples:

- Map/Ammo preference save failure
- product hotkey async failure
- direct floor async failure
- keyboard hook install failure
- image download/decode failure
- program update check/download/validation failure

Fatal examples:

- 필수 startup construction failure
- canonical active candidate에 적용할 수 없는 structural corruption

Fatal/nonfatal 모두 가능한 범위에서 `%LocalAppData%/JunhyunHelper/logs/startup.log` 또는 subsystem diagnostic에 기록합니다.

## 11. Release architecture

상시 CI `.github/workflows/ci.yml`:

1. Release Desktop build
2. 전체 tests
3. win-x64 self-contained single-file publish
4. root/dependency hygiene
5. actual published EXE launch
6. rendered Product UI assertions
7. Main Map / Factory / MiniMap smoke
8. graceful close / process exit
9. artifact upload

Program updater가 latest public Release를 신뢰하므로 public release pipeline은 v0.1.14부터 Draft-first입니다.

```text
exact baseline
→ build/test/publish/smoke
→ package + SHA256SUMS
→ draft release
→ draft asset re-download verification
→ publish latest
→ public asset re-download verification
→ independent public executable verification
```

Release/verification workflow는 one-shot이며 완료 후 저장소에서 제거합니다.

## 12. Scanner boundary

Scanner는 현재 실제 subsystem이 아닙니다.

```text
Desktop tab: visible
content: 준비 중
runtime scanner implementation: none
```

사용자 별도 요구사항이 확정되기 전에는 Core/Infrastructure/Application에 Scanner architecture를 임의 추가하지 않습니다.

## 13. 현재 공개 baseline

```text
release: v0.1.14
baseline: bb0611e9263c24018825a87a58aba2c5474b6cc4
ProductVersion: 0.1.14+bb0611e9263c24018825a87a58aba2c5474b6cc4
Content schema: v7
user.db schema: v1
232 tests passed
public verification workflow: 32116726491 — SUCCESS
```

공개 검증 상세는 `docs/RELEASE_0.1.14.md`, program update 상세는 `docs/PROGRAM_UPDATE.md`를 기준으로 합니다.
