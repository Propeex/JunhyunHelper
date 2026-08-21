# DEPLOYMENT — 준현 헬퍼 배포 원칙

기준일: 2026-08-21

## 1. 공개 형태

준현 헬퍼는 Windows x64에서 바로 실행 가능한 **portable / self-contained ZIP**으로 공개합니다.

현재 public stable:

```text
v1.0.0 PUBLIC VERIFIED
release source: 3147ad1b48c3d30df529d95b148c5c444a77d649
release workflow: 32219746319 — SUCCESS
asset: Junhyun-Helper-v1.0.0-win-x64.zip
bytes: 74,088,334
SHA-256: 0e92787409add9dd9e1138277c3588586a04266b05ca56d7cf7fb6f79c88094c
```

다음 release candidate:

```text
v1.1.0 — Scanner
```

## 2. 배포 특성

- Target: Windows x64
- .NET 10 WPF
- self-contained
- single-file executable
- portable ZIP
- entry point: `준현 헬퍼.exe`
- 별도 .NET Runtime 설치 불필요
- 관리자 권한 불필요
- installer 없음
- registry registration 없음
- 현재 code signing 없음
- user-consent program updater 포함

## 3. 공개 ZIP root 계약

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

금지:

- root runtime DLL clutter
- PDB
- nested ZIP/7z/rar
- unused legacy AutoUpdater/WebView2/GraphX/QuikGraph
- runtime Logs directory beside executable
- permanent `Updater.exe`

Map/MiniMap이 path로 직접 읽는 pinned donor assets만 `Assets/`에 외부 유지합니다.

## 4. 사용자 데이터

프로그램 폴더가 아니라:

```text
%LocalAppData%/JunhyunHelper
```

주요 경로:

```text
user.db
content/
image-cache/
map-product-settings.json(.bak)
ammo-favorites.json(.bak)
scanner-settings.json(.bak)
scanner/catalog/
updates/pending/
logs/startup.log
logs/scanner.log(.1)
```

Program update는 이 사용자 데이터를 교체하지 않습니다.

## 5. Program Update

일반 실행:

```text
latest stable GitHub Release
→ latest > current이면 사용자 Yes/No
→ Yes
→ exact Windows ZIP + SHA256SUMS
→ SHA-256/package validation
→ temporary self-copy updater
→ original app shutdown
→ program-owned files transaction replace
→ new app restart
```

업데이트 대상:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

검증 전 기존 program file을 수정하지 않습니다. 교체 실패 시 previous files rollback과 기존 EXE 재실행을 시도합니다.

## 6. 상시 CI 계약

`.github/workflows/ci.yml`은 최소 다음을 검증합니다.

1. Release Desktop build
2. 전체 automated tests
3. NuGet vulnerability audit
4. win-x64 self-contained single-file publish
5. ProductVersion ↔ project Version ↔ FIRST_RUN identity
6. package root / PDB / nested archive / forbidden dependency
7. actual published EXE launch
8. rendered Product UI assertions
9. Scanner safe-default real/test toggle assertions
10. Main Map / Factory / MiniMap smoke
11. graceful Main Window close / process exit
12. workflow artifact upload

PR artifact는 정식 공개 배포물이 아닙니다.

## 7. Public Release — Draft first

Program updater가 latest public stable을 신뢰하므로 검증되지 않은 release를 latest로 잠시도 노출하지 않습니다.

정식 순서:

```text
release candidate PR final CI
→ main merge
→ exact release SHA
→ release workflow independently build/tests/publish/smoke
→ ZIP + SHA256SUMS
→ Draft GitHub Release
→ Draft assets GitHub에서 재다운로드
→ checksum/package root/ProductVersion/FIRST_RUN 검증
→ public/latest 전환
→ Public assets 재다운로드
→ checksum/ProductVersion/FIRST_RUN/latest 검증
→ public downloaded EXE smoke
→ release record finalization
→ one-shot workflow 제거
```

릴리즈 workflow는 exact release commit을 checkout하고 Map donor gitlink도 exact pin인지 검증합니다.

## 8. v1.1.0 Scanner 릴리즈 계약

v1.1.0은 v1.0.0에 새 사용자 Scanner 기능을 추가하는 MINOR release입니다.

공개 차단 조건:

- 상시 CI 전체 성공
- release workflow의 독립 build/tests/publish 성공
- published EXE Scanner controls + 기존 Product UI/Map smoke 성공
- Draft ZIP/checksum/package 검증 성공
- public ZIP/checksum/package 검증 성공
- public downloaded EXE smoke 성공

### 의도적으로 release blocker가 아닌 것

사용자가 2026-08-21 확정한 정책에 따라 **최신 Tarkov Borderless 실제 인게임 E2E는 v1.1.0 release blocker가 아닙니다.**

공개 release notes와 공식 상태에는 다음을 명확하게 유지합니다.

```text
Scanner implementation: implemented
Windows build/package: verified
offline screenshot/OCR path: previously verified
latest live Tarkov Borderless E2E: pending
```

공개 후 실제 게임 검증은 다음 로그를 사용합니다.

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
```

실제 capture/detector/OCR 문제가 발견되면 버전 정책상 후속 PATCH release로 보정합니다.

## 9. Scanner diagnostics privacy/packaging

Scanner log는 LocalAppData에만 생성합니다.

기록 가능:

- capture/runtime state
- detail candidate bounds/signature
- title OCR text
- matcher confidence/result
- error metadata

저장 금지:

- screenshot
- raw pixel buffer

로그 파일은 공개 ZIP에 포함하지 않습니다.

## 10. 호환성

v1.1.0:

```text
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
v1.0.0 → v1.1.0 mandatory Game Content update: none
v1.0.0 → v1.1.0 user.db migration: none
```

기존 Profile / Quest / Inventory / Hideout / Map preferences / Ammo favorites는 유지합니다.

Scanner는 새로 별도 settings/catalog cache를 생성합니다.

## 11. Code signing / installer

현재 공개 빌드는 code signing하지 않습니다. Windows SmartScreen 경고가 표시될 수 있습니다.

Code signing / installer는 현재 release 필수 조건이 아닙니다.
