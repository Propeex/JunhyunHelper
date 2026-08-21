# DEPLOYMENT — 준현 헬퍼 배포 원칙

기준일: 2026-08-21

## 1. 공개 형태

준현 헬퍼는 Windows x64에서 바로 실행 가능한 **portable / self-contained ZIP**으로 공개합니다.

현재 public stable:

```text
v1.1.3 PUBLIC RELEASE / VERIFIED
exact release source SHA: 8803f899341859887281ad50135911f4625a64f3
release verification run: 32470606548
asset: Junhyun-Helper-v1.1.3-win-x64.zip
bytes: 80,251,960
SHA-256: 419f6288aa3202f10868f2fe6a4ccac40475753ce4ba8c8c2d9985396c4bf493
ProductVersion: 1.1.3+8803f899341859887281ad50135911f4625a64f3
Draft downloaded EXE smoke: SUCCESS
public downloaded EXE smoke: SUCCESS
```

상세: `docs/RELEASE_1.1.3.md`

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

정식 release가 끝나면 release-only / dispatcher / diagnostic workflow를 저장소에서 제거하고 상시 workflow는 원칙적으로 `ci.yml`만 유지합니다.

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
→ Draft-downloaded EXE smoke
→ public/latest 전환
→ public tag가 exact release SHA를 가리키는지 검증
→ Public assets 재다운로드
→ checksum/hash/size/ProductVersion/FIRST_RUN 검증
→ public downloaded EXE smoke
→ release record finalization
→ one-shot workflow 제거
```

릴리즈 workflow는 exact release commit을 checkout하고 Map donor gitlink도 exact pin인지 검증합니다.

Public tag 확인은 shell refspec 문자열 조합보다 GitHub API의 tag ref/object를 우선합니다. v1.1.3 릴리즈에서 PowerShell refspec 보간 문제가 실제로 발생했기 때문에 이 경계를 명시합니다.

## 8. v1.1.3 Scanner Lab v3.8 복원 릴리즈 결과

v1.1.3은 새 기능이 아니라 Scanner 인식 회귀를 복구한 PATCH입니다.

완료된 공개 차단 조건:

- Windows Release build
- **245 automated tests / 0 failed / 0 skipped**
- Scanner Lab v3.8 geometry/title ROI regression
- self-contained package 및 dependency audit
- exact packaged EXE Product UI + Scanner + Main Map + Factory + MiniMap smoke
- Draft ZIP/checksum/package/ProductVersion/FIRST_RUN 검증
- Draft-downloaded EXE smoke
- public/latest 전환
- GitHub API를 통한 exact public tag → source SHA 검증
- public ZIP hash/size/ProductVersion 재검증
- public downloaded EXE smoke
- audit artifact upload

Final release verification:

```text
run: 32470606548
job: 96736389584
source: 8803f899341859887281ad50135911f4625a64f3
asset: Junhyun-Helper-v1.1.3-win-x64.zip
bytes: 80,251,960
SHA-256: 419f6288aa3202f10868f2fe6a4ccac40475753ce4ba8c8c2d9985396c4bf493
EXE bytes: 83,826,070
ProductVersion: 1.1.3+8803f899341859887281ad50135911f4625a64f3
public downloaded EXE smoke: SUCCESS
```

릴리즈 자동화 중 v1/v2에서 발견된 오류는 제품이 아니라 one-shot workflow의 null 처리 / PowerShell git refspec 보간 문제였습니다. 실패 시 cleanup으로 불완전한 release/tag를 회수했고, v3에서 GitHub API tag 검증으로 전체 gate를 성공했습니다.

### 의도적으로 release blocker가 아닌 것

DEC-051에 따라 **최신 Tarkov Borderless 실제 인게임 E2E는 release blocker가 아닙니다.**

공개 후 실제 게임 검증은 다음 로그를 사용합니다.

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
```

v1.1.3은 Scanner Lab v3.8의 multi-candidate semantic validation 구조를 복원했으며, 실제 capture/candidate/OCR/semantic selection 문제가 남으면 후속 PATCH로 보정합니다.

## 9. Scanner diagnostics privacy/packaging

Scanner log는 LocalAppData에만 생성합니다.

기록 가능:

- capture/runtime state
- structural candidate bounds/score/reason
- candidate별 OCR pass
- matcher/resolver confidence/result
- semantic-selected candidate
- error metadata

저장 금지:

- screenshot
- raw pixel buffer

로그 파일은 공개 ZIP에 포함하지 않습니다.

## 10. 호환성

v1.1.3:

```text
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
v1.1.2 → v1.1.3 mandatory Game Content update: none
v1.1.2 → v1.1.3 user.db migration: none
```

기존 Profile / Quest / Inventory / Hideout / Scanner settings/catalog / Map preferences / Ammo favorites는 유지합니다.
