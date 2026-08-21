# DEPLOYMENT — 준현 헬퍼 배포 원칙

기준일: 2026-08-21

## 1. 공개 형태

준현 헬퍼는 Windows x64에서 바로 실행 가능한 **portable / self-contained ZIP**으로 공개합니다.

현재 public stable:

```text
v1.1.0 PUBLIC RELEASE / VERIFIED
release id: 374188781
exact release source / target SHA: ac24f7717e81cf6fa32cb2e0ade63949ed87ade5
asset: Junhyun-Helper-v1.1.0-win-x64.zip
bytes: 80,235,043
SHA-256: 8e7f452701f866c84e753c1c34951af64f4415947e9f56c56634e2b584d9e1ce
ProductVersion: 1.1.0+ac24f7717e81cf6fa32cb2e0ade63949ed87ade5
public downloaded EXE smoke: SUCCESS
```

상세: `docs/RELEASE_1.1.0.md`

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

정식 release가 끝나면 release-only / dispatcher workflow를 저장소에서 제거하고 상시 workflow는 원칙적으로 `ci.yml`만 유지합니다.

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
→ 필요 시 Draft-downloaded EXE smoke
→ public/latest 전환
→ Public assets 재다운로드
→ checksum/hash/size/ProductVersion/FIRST_RUN/latest 검증
→ public downloaded EXE smoke
→ release record finalization
→ one-shot workflow 제거
```

릴리즈 workflow는 exact release commit을 checkout하고 Map donor gitlink도 exact pin인지 검증합니다.

## 8. v1.1.0 Scanner 릴리즈 결과

v1.1.0은 v1.0.0에 새 사용자 Scanner 기능을 추가하는 MINOR release입니다.

완료된 공개 차단 조건:

- 상시 CI 전체 성공
- 243 automated tests
- self-contained package 및 dependency audit
- packaged EXE Scanner controls + 기존 Product UI/Map smoke 성공
- Draft ZIP/checksum/package/ProductVersion/FIRST_RUN 검증 성공
- Draft-downloaded EXE smoke 성공
- public/latest 전환 성공
- public ZIP hash/size/ProductVersion 재검증 성공
- public downloaded EXE smoke 성공

Public verification run `32452416929`은 위 release gate를 모두 성공한 뒤 마지막 PR 코멘트 기록만 integration 권한 403으로 실패했습니다. 제품/배포 검증과 무관한 bookkeeping 실패이므로 public v1.1.0 검증 상태에는 영향이 없습니다.

### 의도적으로 release blocker가 아닌 것

사용자가 2026-08-21 확정한 정책에 따라 **최신 Tarkov Borderless 실제 인게임 E2E는 v1.1.0 release blocker가 아닙니다.**

공개 상태:

```text
Scanner implementation: IMPLEMENTED
Windows build/package: VERIFIED
offline screenshot/OCR path: VERIFIED
latest live Tarkov Borderless E2E: PENDING
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
