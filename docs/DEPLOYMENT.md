# DEPLOYMENT — 준현 헬퍼 배포 원칙

## 현재 배포 형태

준현 헬퍼는 Windows x64에서 바로 실행 가능한 **portable / self-contained ZIP**으로 공개 배포합니다.

현재 공개 기준선은 `v0.1.13`입니다.

- Target: Windows x64
- .NET: self-contained
- Package: ZIP
- Entry point: `준현 헬퍼.exe`
- 별도 .NET Runtime 설치 요구 없음
- 관리자 권한 요구 없음
- 설치 프로그램 없음
- 자동 업데이트 클라이언트 없음
- 코드 서명 없음

현재는 설치 프로그램, 레지스트리 등록, 자동 업데이트 클라이언트를 추가하지 않습니다. 프로그램 파일과 사용자 데이터를 분리하고, 검증된 portable ZIP을 교체하는 방식이 현재 공식 배포 계약입니다.

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
- 로그나 사용자 데이터 폴더를 배포 ZIP에 포함하지 않음

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

Map 제품 설정과 Ammo 즐겨찾기는 프로그램 교체와 독립적으로 유지합니다. v0.1.13부터 직전 정상 JSON을 `.bak` 복구본으로 보존합니다.

### 로그

```text
logs/
```

프로그램 실행 폴더가 아니라 LocalAppData 아래에 기록합니다.

## 버전 교체 원칙

새 ZIP을 다른 폴더에 풀거나 기존 프로그램 파일을 교체해도 사용자 데이터를 자동 삭제하지 않습니다.

프로그램 업데이트, Game Content 업데이트, 사용자 진행 초기화는 서로 다른 작업입니다.

- 프로그램 파일 교체 → user.db / Map 설정 / Ammo 즐겨찾기 유지
- Game Content 업데이트 → user.db 유지
- 프로필 삭제 → 선택 프로필 User Progress만 삭제

현재 호환성:

```text
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
v0.1.12 → v0.1.13 mandatory data update: none
```

향후 user.db schema 자체를 변경해야 할 때는 별도 migration + backup 정책을 먼저 설계하고 검증한 뒤 배포합니다. schema 변경을 자동 추측하거나 파괴적으로 재생성하지 않습니다.

## 상시 CI 계약

`.github/workflows/ci.yml`은 PR 및 유지보수 후보에서 다음을 검증합니다.

1. Release Desktop build
2. 전체 자동 테스트
3. `win-x64` self-contained single-file publish
4. publish root / dependency hygiene 검증
5. `FIRST_RUN_KO.txt` 포함
6. 실제 publish EXE 실행
7. rendered Product UI assertions
8. Main Map / Factory / MiniMap smoke
9. 정상 Main Window close / process exit
10. GitHub Actions artifact 업로드

빌드 또는 테스트, publish, 실제 실행 smoke 중 하나라도 실패하면 검증된 후보로 취급하지 않습니다.

Artifact 보존 정책:

- Pull Request 검증 빌드: 3일
- main 빌드: 14일

PR artifact는 검증용 후보입니다. 사용자에게 정식으로 전달하는 기준은 별도의 공개 Release 검증을 끝낸 exact release baseline입니다.

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
10. 검증 결과를 `docs/RELEASE_<version>.md`, `STATE.md`, `CURRENT_STATE.md`에 기록
11. 일회성 release/verification workflow 제거

따라서 상시 저장소에는 원칙적으로 `.github/workflows/ci.yml`만 남깁니다.

### v0.1.13 검증 기준선

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

자동화로 검증하기 어려운 항목은 실제 Windows PC 사용 경험에서 계속 확인합니다.

- SmartScreen/Windows 실행 경험
- 실제 네트워크 환경의 최초 content 다운로드
- 장시간 사용 시 UI 반응성
- 다양한 화면 크기/DPI
- 실제 플레이 중 입력 부담

이 항목의 피드백은 기존 기능 보완 근거로 사용하며, 확인되지 않은 새 기능을 자동으로 추가하지 않습니다.

## 코드 서명

현재 공개 빌드는 코드 서명하지 않습니다.

따라서 Windows SmartScreen 경고가 표시될 수 있습니다. 코드 서명/installer/updater는 별도의 제품 요구가 확정되기 전까지 현재 배포 계약의 필수 요소가 아닙니다.
