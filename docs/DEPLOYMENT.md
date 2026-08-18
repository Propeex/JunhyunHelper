# DEPLOYMENT — 준현 헬퍼 배포 원칙

## 현재 공개 형태

준현 헬퍼는 Windows x64에서 바로 실행 가능한 **portable / self-contained ZIP**으로 공개 배포합니다.

현재 공개 기준선은 **v0.1.14 PUBLIC RELEASE / VERIFIED**입니다.

```text
release: v0.1.14
release baseline / tag SHA: bb0611e9263c24018825a87a58aba2c5474b6cc4
ProductVersion: 0.1.14+bb0611e9263c24018825a87a58aba2c5474b6cc4
asset: Junhyun-Helper-v0.1.14-win-x64.zip
size: 74,086,942 bytes
SHA-256: 9b3aaff8ba2182b146ea6b1ec463efd8dc8b1c5532a8d4db6cf716938536ae02
public verification: 32116726491 — SUCCESS
```

배포 특성:

- Target: Windows x64
- .NET: self-contained
- Package: ZIP
- Entry point: `준현 헬퍼.exe`
- 별도 .NET Runtime 설치 요구 없음
- 관리자 권한 요구 없음
- 설치 프로그램 없음
- 레지스트리 등록 없음
- 현재 코드 서명 없음
- v0.1.14+: 사용자 동의형 program update

## 공개 ZIP root 계약

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

허용 원칙:

- 런타임/관리 DLL을 root에 별도 노출하지 않음
- PDB 미포함
- nested ZIP 미포함
- legacy updater/WebView2/GraphX 등 사용하지 않는 dependency 미포함
- Map/MiniMap이 경로로 직접 읽는 검증된 자산만 `Assets/`에 유지
- 상시 `Updater.exe` 미포함
- logs / user data 미포함

## 사용자 데이터 위치

프로그램 폴더와 무관하게:

```text
%LocalAppData%\JunhyunHelper
```

### 권위 User Progress

```text
user.db
```

Profile / Quest / Inventory / Hideout / prerequisite facts / consumption ledgers의 기준입니다.

### Game Content / cache

```text
content/
image-cache/
```

온라인 원천에서 재생성 가능한 데이터입니다.

### Presentation preference

```text
map-product-settings.json
map-product-settings.json.bak
ammo-favorites.json
ammo-favorites.json.bak
```

### Program update staging

```text
updates/pending/
```

동의 후 받은 ZIP/checksum/staging에만 사용하며 성공/실패 후 best-effort cleanup 대상입니다.

### Diagnostics

```text
logs/startup.log
```

프로그램 실행 폴더 옆에 runtime log directory를 만들지 않습니다.

## Program Update — v0.1.14+

상세 계약: `docs/PROGRAM_UPDATE.md`

일반 실행:

```text
latest stable public GitHub Release 조회
→ latest <= current: no-op
→ latest > current: 사용자 동의창
→ No: 현재 버전 계속 사용
→ Yes: exact ZIP + SHA256SUMS download
→ SHA-256 / package validation
→ temp self-copy updater start
→ original app shutdown
→ program-owned files transaction replace
→ new app restart
```

### 대상 release

- repository: `Propeex/JunhyunHelper`
- stable public release
- tag: exact `vMAJOR.MINOR.PATCH`
- package: `Junhyun-Helper-vMAJOR.MINOR.PATCH-win-x64.zip`
- checksum: `SHA256SUMS.txt`
- draft/prerelease 제외

### Client validation

- GitHub Release asset HTTPS scope
- exact package name
- exact checksum entry
- actual SHA-256
- path traversal reject
- symlink reject
- duplicate entry reject
- unexpected root reject
- PDB reject
- required product files/Assets 확인

검증 완료 전 기존 프로그램 파일을 수정하지 않습니다.

### File replacement

실행 중인 EXE는 자기 자신을 직접 덮어쓰지 않습니다.

현재 `준현 헬퍼.exe`를 `%TEMP%\JunhyunHelper\updater\<guid>`로 복사해 updater mode로 실행합니다.

교체 대상:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

새 파일을 target volume에 먼저 준비하고 기존 파일을 temporary previous name으로 이동한 뒤 commit합니다. 중간 실패 시 previous 파일 rollback을 시도합니다.

성공하면 새 EXE를 자동 실행합니다. 실패 시 기존 EXE가 복구되어 있으면 기존 버전을 다시 실행합니다.

### Bootstrap

**공개 v0.1.13에는 updater 코드가 없으므로 v0.1.13 → v0.1.14는 한 번 수동 ZIP 교체가 필요합니다.**

v0.1.14 이후 후속 정식 버전부터 program update flow를 사용할 수 있습니다.

## 버전 교체와 사용자 데이터

Program update, Game Content update, profile deletion은 서로 다른 작업입니다.

- Program file replacement → `user.db` / content / cache / preferences 유지
- Game Content update → `user.db` 유지
- Profile delete → 선택 profile User Progress만 삭제

현재 호환성:

```text
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
v0.1.13 → v0.1.14 mandatory data update: none
```

향후 `user.db` schema가 바뀌면 migration + backup 정책을 먼저 설계/검증해야 합니다. 파괴적으로 재생성하지 않습니다.

## 상시 CI 계약

상시 저장소 workflow는 원칙적으로 `.github/workflows/ci.yml` 하나입니다.

검증:

1. Release Desktop build
2. 전체 automated tests
3. program-update parser/checksum/archive/replacement/rollback regression tests
4. win-x64 self-contained single-file publish
5. publish root / dependency hygiene
6. FIRST_RUN 포함
7. actual published EXE launch
8. rendered Product UI assertions
9. Main Map / Factory / MiniMap smoke
10. graceful Main Window close / process exit
11. workflow artifact upload

v0.1.14 release tests:

```text
232 passed / 0 failed / 0 skipped
```

PR artifact는 정식 배포물이 아닙니다. 사용자 배포 기준은 검증된 public Release입니다.

## Public Release 계약 — Draft first

v0.1.14부터 program updater가 latest public Release를 신뢰하므로 **검증되지 않은 release를 latest로 잠시도 노출하지 않습니다.**

정식 릴리즈 순서:

1. release candidate CI 전체 통과
2. exact product release baseline SHA 고정
3. one-shot release workflow가 exact SHA checkout
4. Release build / 전체 tests / publish / actual EXE smoke 재실행
5. ZIP + `SHA256SUMS.txt` 생성
6. **Draft Release** 생성
7. Draft ZIP/checksum을 GitHub에서 다시 다운로드
8. hash / size / package root / ProductVersion 검증
9. 검증 성공 시에만 `draft=false`, latest로 공개
10. Public ZIP/checksum을 다시 다운로드
11. public hash / size / ProductVersion / package 검증
12. 필요 시 별도 one-shot public EXE runtime verification
13. `RELEASE_<version>.md`, `STATE.md`, `CURRENT_STATE.md`에 결과 기록
14. one-shot release/verification workflow 제거

## v0.1.14 공개 검증 기준선

```text
feature PR #100 CI: 32115435656 — SUCCESS
release PR #101 CI: 32115953069 — SUCCESS
public verification PR #102 workflow: 32116726491 — SUCCESS
release baseline/tag SHA: bb0611e9263c24018825a87a58aba2c5474b6cc4
automated tests: 232 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v0.1.14-win-x64.zip
size: 74,086,942 bytes
SHA-256: 9b3aaff8ba2182b146ea6b1ec463efd8dc8b1c5532a8d4db6cf716938536ae02
ProductVersion: 0.1.14+bb0611e9263c24018825a87a58aba2c5474b6cc4
public rendered UI + Main Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown: SUCCESS
```

상세: `docs/RELEASE_0.1.14.md`

## 코드 서명

현재 공개 빌드는 코드 서명하지 않습니다. Windows SmartScreen 경고가 표시될 수 있습니다.

Code signing / installer는 program updater와 별도 제품 범위이며 현재 updater 기능의 필수 조건이 아닙니다.
