# DEPLOYMENT — 준현 헬퍼 배포 원칙

기준일: 2026-08-27
상태: **v1.7.13 PUBLIC STABLE / VERIFIED / MAINTENANCE MODE**

## 1. 공개 형태

준현 헬퍼는 Windows x64에서 바로 실행 가능한 **portable / self-contained single-file product**로 공개한다.

현재 public stable:

```text
version: v1.7.13
exact product release source/tag target: 16198c462a6be58d77dbe2dc27aa57eabfc7b9fd
main CI: 33051890329 — SUCCESS
Release workflow: 33052109161 — SUCCESS
400 passed / 0 failed / 0 skipped
release id: 377652938
asset: Junhyun-Helper.zip
asset id: 531953179
bytes: 80,486,670
SHA-256: d1cfcf1f606985485584f0e085e8821e0f62156a980f259a90144fd134a7eeb6
checksum asset id: 531953171
published UTC: 2026-08-27T08:00:58Z
```

GitHub `/releases/latest` 및 `refs/tags/v1.7.13` readback에서 release target/tag ref가 exact product source와 일치하고, public ZIP digest가 exact main-CI package SHA-256과 일치함을 확인했다.

상세:

- `docs/RELEASE_1.7.13.md`
- `docs/.release-v1.7.13-status.json`
- `docs/RELEASE_NOTES_V1.7.13.md`

후속 documentation-only main commit은 v1.7.13 product release source가 아니다.

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

## 3. Stable public package contract

GitHub Release asset 이름은 version과 분리한다.

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/...
```

Canonical release assets:

```text
Junhyun-Helper.zip
SHA256SUMS.txt
```

Version identity는 filename에 두지 않고 다음이 일치해야 한다.

- Desktop project Version
- EXE ProductVersion
- FIRST_RUN 첫 줄
- Git tag
- GitHub Release metadata

Public ZIP 금지 항목:

- product folder 바깥 entry
- root runtime DLL clutter
- PDB
- nested ZIP/7z/rar
- unused legacy AutoUpdater/WebView2/GraphX/QuikGraph dependency
- runtime Logs directory beside executable
- permanent `Updater.exe`

Map/MiniMap이 path로 직접 읽는 pinned donor assets만 `Assets/`에 external product files로 유지한다.

## 4. 사용자 데이터

프로그램 폴더가 아니라 다음 경로를 사용한다.

```text
%LocalAppData%/JunhyunHelper
```

대표 경로:

```text
user.db
content/
image-cache/
map-product-settings.json(.bak)
minimap-window-state.json
ammo settings/favorites persistence
scanner-settings.json(.bak)
scanner/catalog/
scanner/fonts/
scanner/diagnostics/
logs/
updates/pending/
```

Program Update / release 교체가 이 사용자 데이터를 덮어쓰지 않는다. User-reviewed Scanner Ground Truth도 program-owned package가 아니다.

## 5. CI release-candidate gate

`.github/workflows/ci.yml`의 Windows gate는 최소 다음을 검증한다.

1. pinned Map donor exact checkout
2. Desktop Release build
3. full automated tests — failed/skipped 0
4. win-x64 self-contained single-file publish
5. ProductVersion ↔ project Version ↔ FIRST_RUN identity
6. publish-root / PDB / nested archive / forbidden dependency audit
7. actual published EXE startup
8. rendered Product UI assertions
9. Scanner / Mini Scanner product smoke
10. Main Map / Factory / MiniMap smoke
11. graceful Main Window close / process termination
12. clean portable root
13. stable package construction
14. `Junhyun-Helper.zip` checksum/layout verification
15. verified artifact upload

PR workflow artifact는 정식 공개 배포물이 아니다.

## 6. Stable Release workflow

`.github/workflows/release.yml`은 **main push CI가 성공한 경우에만** 실행된다.

```text
main push
→ CI success
→ Release workflow_run
→ exact CI head SHA checkout
→ exact CI artifact download
→ ProductVersion/FIRST_RUN revalidation
→ Junhyun-Helper.zip SHA256SUMS revalidation
→ draft release create/resume
→ required assets verification
→ stable publish/latest
→ final release metadata readback
```

핵심:

- Release workflow가 새 binary를 독립적으로 다시 빌드하지 않는다.
- 성공한 main CI가 검증한 exact artifact를 내려받아 게시한다.
- `RELEASE_SHA`는 해당 successful main CI의 `head_sha`다.
- draft release의 기존 asset은 expected size가 다르면 오염으로 보고 overwrite하지 않는다.
- required stable assets는 `Junhyun-Helper.zip`, `SHA256SUMS.txt`다.

## 7. Published stable immutability

이미 public stable로 게시된 같은 `vMAJOR.MINOR.PATCH`는 immutable historical release로 취급한다.

후속 documentation-only main commit은 같은 assembly version으로 다른 ProductVersion metadata bytes를 만들 수 있으므로, 이미 공개된 stable asset을 교체해서는 안 된다.

현재 Release workflow 정책:

```text
existing release is public/non-draft
→ required stable assets 존재 확인
→ existing tag/assets 유지
→ success exit
```

즉 docs-only commit의 산출물로 과거 stable ZIP을 mutate하지 않는다.

## 8. Public readback / release proof

Stable 게시 뒤 최소 확인:

- `/releases/latest` = target version
- draft=false
- prerelease=false
- exact release target = chosen product source
- tag ref object = exact product source
- `Junhyun-Helper.zip` present
- `SHA256SUMS.txt` present
- public ZIP size/digest = exact main-CI verified package size/hash

가능한 도구 환경에서는 public asset을 별도 anonymous client로 다시 내려받아 byte/layout/EXE smoke까지 검증할 수 있다. 해당 binary redownload가 수행되지 않은 세션에서는 완료했다고 기록하지 않는다. GitHub asset metadata/digest/tag-ref readback과 main-CI/release-workflow verification은 수행한 범위 그대로 기록한다.

## 9. Program Update 연계

일반 실행 updater는 latest public stable을 source of truth로 사용한다.

Current updater가 인정하는 package는 canonical `Junhyun-Helper.zip`뿐이다. 과거 전환기 versioned package는 current release/update contract가 아니다.

상세 계약은 `docs/PROGRAM_UPDATE.md`가 권위다.

## 10. 호환성

현재 schema/version 사실값은 이 배포 문서에 중복 저장하지 않는다. `docs/PROJECT_STATE.json`이 canonical source이며, 상세 read/write compatibility는 `docs/DEVELOPER_REFERENCE.md`와 subsystem 문서를 사용한다.

배포/업데이트는 기존 Profile / Quest / Inventory / Hideout / Map/MiniMap/Ammo/Scanner mutable user state를 보존해야 하며, package 교체가 `%LocalAppData%/JunhyunHelper`의 사용자 소유 데이터를 초기화해서는 안 된다.

## 11. 릴리즈 이후 운영

새 runtime change는 기존 public stable을 수정하지 않고 다음 PATCH/MINOR version으로 진행한다.

```text
evidence / approved product requirement
→ branch + PR
→ deterministic regression
→ full Windows CI/publish/product smoke/package
→ main merge
→ exact main CI
→ Release workflow
→ public release readback
→ durable release record
→ canonical docs sync
```

문서만 현재 사실에 맞추는 변경은 product release source와 명시적으로 구분한다.
