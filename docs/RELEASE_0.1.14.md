# RELEASE 0.1.14 — 사용자 동의형 프로그램 업데이트

## 상태

**PUBLIC RELEASE / VERIFIED**

```text
release: v0.1.14
release baseline: bb0611e9263c24018825a87a58aba2c5474b6cc4
public tag SHA: bb0611e9263c24018825a87a58aba2c5474b6cc4
ProductVersion: 0.1.14+bb0611e9263c24018825a87a58aba2c5474b6cc4
asset: Junhyun-Helper-v0.1.14-win-x64.zip
asset size: 74,086,942 bytes
SHA-256: 9b3aaff8ba2182b146ea6b1ec463efd8dc8b1c5532a8d4db6cf716938536ae02
public release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.14
```

Release는 `draft=false`, `prerelease=false`이며 release target과 public tag가 exact release baseline과 일치합니다.

## 목적

사용자가 확정한 프로그램 동작:

1. 프로그램 실행 시 최신 정식 버전을 조회한다.
2. 현재 버전보다 최신 버전이 있으면 사용자에게 업데이트 동의 여부를 묻는다.
3. 사용자가 동의하면 업데이트를 진행하고 완료 후 새 버전으로 자동 재시작한다.

세부 안전성/실패 정책은 `docs/PROGRAM_UPDATE.md`를 따릅니다.

## 구현

### 실행 시 확인

- source of truth: `Propeex/JunhyunHelper` latest public stable GitHub Release
- stable `vMAJOR.MINOR.PATCH`만 허용
- 현재 실행 버전보다 엄격히 높은 버전만 대상
- latest check timeout 8초
- check 실패는 `%LocalAppData%/JunhyunHelper/logs/startup.log`에 기록하고 일반 사용 계속
- 새 버전이 있으면 Yes/No 동의창 표시, 기본 선택은 No
- 거절 시 현재 실행을 계속하고 다음 실행 때 다시 확인

### 동의 후 검증

업데이트 package는 `%LocalAppData%/JunhyunHelper/updates/pending`에서 먼저 준비합니다.

- exact ZIP: `Junhyun-Helper-v<version>-win-x64.zip`
- checksum: `SHA256SUMS.txt`
- public SHA-256과 실제 ZIP 비교
- GitHub Release asset URL scope 검증
- ZIP path traversal 거부
- symbolic link 거부
- duplicate entry 거부
- 예상 밖 root 거부
- PDB 거부
- `준현 헬퍼.exe`, `FIRST_RUN_KO.txt`, non-empty `Assets/` 필수

검증이 끝나기 전에는 현재 프로그램 파일을 변경하지 않습니다.

### 교체 / 재시작

실행 중인 EXE가 자기 자신을 직접 교체하지 않습니다.

- 현재 `준현 헬퍼.exe`를 `%TEMP%/JunhyunHelper/updater/<guid>`에 임시 복사
- 복사본을 updater mode로 실행
- 원래 프로그램 종료를 최대 30초 대기
- program-owned files를 same-volume transaction 형태로 교체
- 교체 실패 시 previous 파일 rollback 시도
- 성공 시 새 `준현 헬퍼.exe` 자동 실행
- 실패 시 기존 EXE가 복구되어 있으면 기존 버전 자동 재실행
- 임시 updater runner는 다음 정상 실행에서 best-effort cleanup

업데이트 대상:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

사용자 데이터는 업데이트 대상이 아닙니다.

## 최초 전환

**v0.1.13에는 program updater 코드가 없으므로 v0.1.13 → v0.1.14는 한 번 수동 ZIP 교체가 필요합니다.**

v0.1.14 이후 실행본부터 후속 정식 릴리즈를 프로그램 안에서 업데이트할 수 있습니다.

## 데이터 / 제품 호환성

```text
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
v0.1.13 → v0.1.14 mandatory data update: none
```

유지되는 사용자 데이터:

- Profile
- Quest 완료/실패
- Inventory
- Hideout 진행
- `ProfileVariables` / special trader access facts
- Quest/Hideout consumption ledgers
- Map 제품 설정 및 `.bak`
- Ammo 즐겨찾기 및 `.bak`
- Game Content / image cache

Scanner는 상단 `스캐너` 탭의 `준비 중` placeholder를 그대로 유지하며 실제 Scanner 기능은 추가하지 않았습니다.

## 기능 PR 검증

PR #100 final head:

```text
feature PR: #100
feature head: a84eff05a157205ca2319c90272031197623258d
feature CI: 32115435656 — SUCCESS
automated tests: 232 passed / 0 failed / 0 skipped
Release build: SUCCESS
Windows x64 self-contained single-file publish: SUCCESS
published rendered Product UI assertions: SUCCESS
Main Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown: SUCCESS
```

PR #100은 squash merge되었고 exact 제품 release baseline은 다음입니다.

```text
bb0611e9263c24018825a87a58aba2c5474b6cc4
```

## 릴리즈 PR 검증

PR #101은 제품 코드를 변경하지 않고 one-shot release workflow만 추가했습니다.

```text
release PR: #101
release workflow PR head: 44e14cbf1c70914c293658c510b0fbc8e4edce68
release PR CI: 32115953069 — SUCCESS
```

릴리즈 workflow는 exact baseline `bb0611...`을 직접 checkout하여 build/test/publish/smoke를 다시 수행했습니다.

v0.1.14부터 updater가 latest public Release를 직접 신뢰하기 때문에 공개 순서를 다음처럼 강화했습니다.

```text
exact baseline build/test/publish
→ ZIP + SHA256SUMS 생성
→ Draft Release 생성
→ Draft asset 재다운로드/hash/package 검증
→ 검증 성공 후 public/latest 전환
→ Public asset 재다운로드/hash/package 검증
```

미검증 release가 잠시라도 latest stable로 노출되지 않도록 Draft-first를 사용합니다.

## 독립 공개 검증

공개 후 별도 one-shot PR #102가 **GitHub에 실제 공개된 v0.1.14 자산을 다시 다운로드**하여 검증했습니다.

```text
public verification PR: #102
public verification workflow: 32116726491 — SUCCESS
PUBLIC_TAG_SHA=bb0611e9263c24018825a87a58aba2c5474b6cc4
PUBLIC_SIZE=74086942
PUBLIC_SHA256=9b3aaff8ba2182b146ea6b1ec463efd8dc8b1c5532a8d4db6cf716938536ae02
PUBLIC_PRODUCT_VERSION=0.1.14+bb0611e9263c24018825a87a58aba2c5474b6cc4
PUBLIC_RELEASE_VERIFIED=true
```

검증 내용:

- release `draft=false`
- release `prerelease=false`
- release target exact baseline
- tag SHA exact baseline
- exact public ZIP 존재
- `SHA256SUMS.txt` 존재
- public ZIP 재다운로드
- checksum 일치
- root가 `준현 헬퍼.exe`, `FIRST_RUN_KO.txt`, `Assets/` 계약과 일치
- PDB 없음
- ProductVersion exact v0.1.14 baseline
- FIRST_RUN의 updater / no-migration / Scanner 계약 확인
- **실제 public EXE rendered Product UI + Main Map + Factory + MiniMap smoke 성공**
- 정상 Main Window close / process exit 성공

따라서 v0.1.14는 공개된 실제 배포 자산까지 검증 완료된 상태입니다.
