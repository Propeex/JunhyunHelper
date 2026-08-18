# DEPLOYMENT — 준현 헬퍼 배포 원칙

## 현재 배포 형태

준현 헬퍼는 Windows x64에서 바로 실행 가능한 **portable / self-contained ZIP**으로 공개 배포합니다.

현재 공개 기준선은 `v0.1.13`이며, `v0.1.14` release candidate부터 JunhyunHelper 자체 프로그램 업데이트 기능을 포함합니다.

- Target: Windows x64
- .NET: self-contained
- Package: ZIP
- Entry point: `준현 헬퍼.exe`
- 별도 .NET Runtime 설치 요구 없음
- 관리자 권한 요구 없음
- 설치 프로그램 없음
- 코드 서명 없음
- v0.1.14+: 사용자 동의형 program auto-update

portable 배포와 사용자 데이터 분리는 유지합니다. installer/registry 등록 없이 프로그램 소유 파일만 교체합니다.

## 공개 ZIP root 계약

공개 ZIP을 압축 해제했을 때 루트는 다음 구조만 허용합니다.

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

- 런타임/관리 DLL을 루트에 따로 두지 않음
- PDB를 공개 패키지에 포함하지 않음
- legacy updater/WebView2/GraphX 등 사용하지 않는 dependency를 포함하지 않음
- Map/MiniMap이 경로로 직접 읽는 검증된 파일만 `Assets/`에 유지
- 상시 `Updater.exe`를 공개 패키지에 포함하지 않음
- 로그나 사용자 데이터를 배포 ZIP에 포함하지 않음

## 프로그램 자동 업데이트 — v0.1.14+

확정 제품 계약은 `docs/PROGRAM_UPDATE.md`를 따릅니다.

일반 실행 시:

1. `Propeex/JunhyunHelper`의 latest public stable GitHub Release 조회
2. 현재 버전보다 최신 버전이 없으면 아무 UI 없이 일반 사용 계속
3. 최신 버전이 있으면 사용자에게 업데이트 여부 질문
4. 사용자가 거절하면 현재 버전 그대로 사용
5. 사용자가 동의하면 exact Windows ZIP과 `SHA256SUMS.txt` 다운로드
6. SHA-256과 package root/security contract 검증
7. 현재 EXE의 임시 self-copy를 updater mode로 실행
8. 원래 앱 종료 후 program-owned files를 transaction 형태로 교체
9. 성공하면 새 `준현 헬퍼.exe` 자동 재실행
10. 교체 실패 시 rollback을 시도하고 기존 EXE가 있으면 재실행

업데이트 조회/다운로드/검증 실패는 일반 프로그램 실행을 막지 않습니다.

자동 업데이트 대상은 stable `vMAJOR.MINOR.PATCH` release뿐이며 draft/prerelease 또는 예상 asset contract와 다른 release를 임의로 사용하지 않습니다.

### 최초 전환 주의

공개 v0.1.13에는 program updater 코드가 존재하지 않습니다. 따라서 기존 v0.1.13 사용자는 **v0.1.14를 한 번 수동으로 내려받아 교체해야 합니다.** v0.1.14 이후 실행본부터 후속 정식 버전의 자동 업데이트가 가능합니다.

## 사용자 데이터 위치

실행 파일 위치와 무관하게 데이터 루트는 다음입니다.

```text
%LocalAppData%\JunhyunHelper
```

### 사용자 진행 사실

```text
user.db
```

Profile / Quest / Inventory / Hideout 진행의 기준입니다.

### Game Content / cache

```text
content/
image-cache/
```

온라인 Game Content에서 재생성 가능한 데이터입니다.

### presentation preference

```text
map-product-settings.json
map-product-settings.json.bak
ammo-favorites.json
ammo-favorites.json.bak
```

Map 제품 설정과 Ammo 즐겨찾기는 프로그램 교체와 독립적으로 유지합니다.

### 프로그램 업데이트 staging

```text
updates/pending/
```

동의 후 받은 package/checksum 검증과 staging에만 사용합니다. 프로그램 업데이트 성공/실패 후 best-effort cleanup 대상입니다.

### 로그

```text
logs/
```

프로그램 실행 폴더가 아니라 LocalAppData 아래에 기록합니다. updater check/preparation/apply 실패도 `startup.log`에 진단 기록합니다.

## 버전 교체 원칙

프로그램 업데이트, Game Content 업데이트, 사용자 진행 초기화는 서로 다른 작업입니다.

- 프로그램 파일 교체 → `user.db` / content / cache / Map 설정 / Ammo 즐겨찾기 유지
- Game Content 업데이트 → `user.db` 유지
- 프로필 삭제 → 선택 프로필 User Progress만 삭제

v0.1.14 candidate 호환성:

```text
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
v0.1.13 → v0.1.14 mandatory data update: none
```

향후 `user.db` schema 자체를 변경해야 할 때는 별도 migration + backup 정책을 먼저 설계하고 검증한 뒤 배포합니다.

## 프로그램 업데이트 무결성 계약

정식 release updater가 소비하는 public Release는 다음을 반드시 제공합니다.

```text
Tag: vMAJOR.MINOR.PATCH
Junhyun-Helper-vMAJOR.MINOR.PATCH-win-x64.zip
SHA256SUMS.txt
```

클라이언트는:

- GitHub Release asset URL이 `Propeex/JunhyunHelper/releases/download/` 아래 HTTPS URL인지 확인
- exact ZIP 이름만 선택
- `SHA256SUMS.txt`의 exact ZIP entry를 선택
- 실제 ZIP SHA-256을 계산해 비교
- path traversal / symlink / duplicate entry / unexpected root / PDB 거부
- `준현 헬퍼.exe`, `FIRST_RUN_KO.txt`, non-empty `Assets/`를 요구

검증이 끝나기 전에는 기존 프로그램 파일을 수정하지 않습니다.

## 상시 CI 계약

`.github/workflows/ci.yml`은 PR 및 유지보수 후보에서 다음을 검증합니다.

1. Release Desktop build
2. 전체 자동 테스트
3. program updater parser/checksum/archive/replacement tests
4. `win-x64` self-contained single-file publish
5. publish root / dependency hygiene 검증
6. `FIRST_RUN_KO.txt` 포함
7. 실제 publish EXE 실행
8. rendered Product UI assertions
9. Main Map / Factory / MiniMap smoke
10. 정상 Main Window close / process exit
11. GitHub Actions artifact 업로드

빌드 또는 테스트, publish, 실제 실행 smoke 중 하나라도 실패하면 검증된 후보로 취급하지 않습니다.

Artifact 보존 정책:

- Pull Request 검증 빌드: 3일
- main 빌드: 14일

PR artifact는 검증용 후보입니다. 사용자에게 정식으로 전달하는 기준은 공개 Release 검증을 끝낸 exact release baseline입니다.

## 공개 Release 계약

정식 공개 릴리즈는 다음 원칙을 따릅니다.

1. release candidate PR에서 상시 CI 전체 통과
2. exact release baseline SHA 고정
3. 일회성 release workflow가 그 SHA를 직접 checkout
4. Release build / 전체 테스트 / publish / 실제 EXE smoke 재실행
5. 공개 ZIP과 `SHA256SUMS.txt` 생성
6. GitHub Release 생성
7. 공개된 ZIP을 GitHub에서 다시 다운로드
8. 파일 크기와 SHA-256 재계산 및 원본과 대조
9. public tag / release target이 exact baseline과 일치하는지 확인
10. ProductVersion / package root / FIRST_RUN contract 확인
11. 검증 결과를 `docs/RELEASE_<version>.md`, `STATE.md`, `CURRENT_STATE.md`에 기록
12. 일회성 release/verification workflow 제거

v0.1.14부터 이 release contract는 사람의 공개 검증뿐 아니라 **다음 버전의 program updater가 신뢰하는 입력 계약**이기도 합니다.

## 현재 공개 v0.1.13 검증 기준선

```text
release: v0.1.13
release baseline: f43190494ce91b3adf389e57a3a790fd45db8b20
release candidate CI: 32105275116 — SUCCESS
public verification workflow: 32111533861 — SUCCESS
automated tests: 217 passed / 0 failed / 0 skipped
public asset: Junhyun-Helper-v0.1.13-win-x64.zip
public size: 74,069,173 bytes
public SHA-256: 77a8e5d70bacfa8054fb3eafbe03a892456f17fc63c00776379e2730e55c4120
```

## 실사용 확인 범위

자동화로 완전히 대체하기 어려운 항목은 실제 Windows PC 사용 경험에서 계속 확인합니다.

- SmartScreen/Windows 실행 경험
- 실제 네트워크 환경의 program update 다운로드
- 실제 파일 시스템에서 update 종료→교체→재시작 경험
- 실제 네트워크 환경의 최초 Game Content 다운로드
- 장시간 사용 시 UI 반응성
- 다양한 화면 크기/DPI
- 실제 플레이 중 입력 부담

## 코드 서명

현재 공개 빌드는 코드 서명하지 않습니다. 따라서 Windows SmartScreen 경고가 표시될 수 있습니다.

코드 서명/installer는 program updater와 별개의 제품 범위이며 현재 updater 동작의 필수 요소가 아닙니다.
