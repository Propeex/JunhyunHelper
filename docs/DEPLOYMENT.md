# DEPLOYMENT — 준현 헬퍼 배포 원칙

기준일: 2026-08-23

## 1. 공개 형태

준현 헬퍼는 Windows x64에서 바로 실행 가능한 **portable / self-contained ZIP**으로 공개합니다.

현재 public stable:

```text
v1.3.2 PUBLIC RELEASE / VERIFIED
release source SHA: 922797a99ea221fdc4984dd6ed05df552149d6e4
final PR CI: 32619142034 — SUCCESS
exact-source release run: 32621021058 — SUCCESS
automated tests: 263 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.3.2-win-x64.zip
bytes: 80,311,752
SHA-256: 6e3a7af2de50dfd14f1c49ccb39753177a0bce5b22993bb8bb94ffde93086767
ProductVersion: 1.3.2+922797a99ea221fdc4984dd6ed05df552149d6e4
public/latest: VERIFIED
exact public tag source: VERIFIED
public downloaded EXE smoke: SUCCESS
```

상세: `docs/RELEASE_1.3.2.md`, `docs/.release-v1.3.2-status.json`.

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
- user-consent Program Update 포함

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

프로그램 폴더가 아니라 다음 경로를 사용합니다.

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
scanner/fonts/
updates/pending/
logs/startup.log
logs/scanner.log(.1)
```

Program Update는 이 사용자 데이터를 교체하지 않습니다.

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
10. Scanner product regressions
11. Main Map / Factory / MiniMap smoke
12. graceful Main Window close / process exit
13. workflow artifact upload

PR artifact는 정식 공개 배포물이 아닙니다.

정식 release가 끝나면 release-only / finalizer / diagnostic workflow를 저장소에서 제거하고, 상시 workflow는 원칙적으로 `ci.yml`만 유지합니다. 검증 결과를 담은 `RELEASE_x.y.z.md`와 `.release-x.y.z-status.json`은 공식 이력 증거로 보존합니다.

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
→ public-downloaded EXE smoke
→ independent public finalizer
→ release record finalization
→ one-shot workflow 제거
```

릴리즈 workflow는 exact release commit을 checkout하고 Map donor gitlink도 exact pin인지 검증합니다.

Public tag 확인은 shell refspec 문자열 조합보다 GitHub API의 tag ref/object 또는 exact commit compare를 우선합니다.

## 8. v1.3.2 release verification

v1.3.2는 실제 Tarkov/DisplayTest live evidence를 반영한 Scanner 정확성 PATCH입니다.

완료된 공개 차단 조건:

- Windows Release build
- **263 automated tests / 0 failed / 0 skipped**
- current-catalog punctuation/symbol sanitation regressions
- bounded unique one-edit safety regressions
- live-scale sparse-glyph magnifier packaged-EXE regression
- self-contained package 및 dependency audit
- exact packaged EXE Product UI + Scanner + Mini Scanner + Main Map + Factory + MiniMap smoke
- Draft ZIP/checksum/package/ProductVersion/FIRST_RUN 검증
- Draft-downloaded EXE smoke
- public/latest 전환
- exact public tag → source SHA 검증
- public ZIP hash/size/ProductVersion/FIRST_RUN 재검증
- public-downloaded EXE smoke
- independent public finalizer로 public/latest/tag/assets/package/EXE 재검증

Final release verification:

```text
final PR CI: 32619142034
release run: 32621021058
source: 922797a99ea221fdc4984dd6ed05df552149d6e4
asset: Junhyun-Helper-v1.3.2-win-x64.zip
bytes: 80,311,752
SHA-256: 6e3a7af2de50dfd14f1c49ccb39753177a0bce5b22993bb8bb94ffde93086767
ProductVersion: 1.3.2+922797a99ea221fdc4984dd6ed05df552149d6e4
public downloaded EXE smoke: SUCCESS
```

상세 검증 증거는 `docs/RELEASE_1.3.2.md`와 `docs/.release-v1.3.2-status.json`에 있습니다.

### 의도적으로 release blocker가 아닌 것

**최신 Tarkov Borderless의 모든 환경 조합에 대한 실제 인게임 E2E calibration은 release blocker가 아닙니다.**

공개 후 실제 게임 검증은 다음 자료를 사용합니다.

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
Scanner 탭 → 인식 이미지
사용자가 저장한 recognition 원본 PNG
```

실제 capture/candidate/anchor/ROI/OCR/catalog/visual/presentation 문제가 관측되면 후속 PATCH로 보정합니다. live evidence 없이 recognition confidence/margin을 완화하지 않습니다.

## 9. Scanner diagnostics privacy / packaging

Scanner log는 LocalAppData에만 생성합니다.

기록 가능:

- capture/runtime state
- structural candidate bounds/score/reason
- title-anchor/ROI evidence
- candidate별 OCR pass
- semantic/visual resolver confidence/result
- selected candidate
- error metadata

자동 저장 금지:

- screenshot
- raw pixel buffer

`인식 이미지`는 process memory의 최신 diagnostic frame을 사용합니다. 사용자가 명시적으로 저장할 때만 원본 PNG를 export합니다. 로그/진단 이미지는 공개 ZIP에 포함하지 않습니다.

## 10. 호환성

v1.3.2:

```text
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache: v1/v2 readable, v2 written
v1.3.1 → v1.3.2 mandatory Game Content update: none
v1.3.1 → v1.3.2 user.db migration: none
```

기존 Profile / Quest / Inventory / Hideout / Scanner settings/catalog / Map preferences / Ammo favorites는 유지합니다.
