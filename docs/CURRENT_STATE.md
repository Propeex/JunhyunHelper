# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계/이력은 `docs/STATE.md` 및 개별 문서를 참조합니다.

기준일: 2026-08-18

상태: **`v0.1.14 PUBLIC RELEASE / VERIFIED`**

## 현재 공개 제품

```text
release: v0.1.14
release baseline / tag SHA: bb0611e9263c24018825a87a58aba2c5474b6cc4
ProductVersion: 0.1.14+bb0611e9263c24018825a87a58aba2c5474b6cc4
feature PR: #100
feature CI: 32115435656 — SUCCESS
release PR: #101
release PR CI: 32115953069 — SUCCESS
public verification PR: #102
public verification workflow: 32116726491 — SUCCESS
tests at release: 232 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v0.1.14-win-x64.zip
size: 74,086,942 bytes
SHA-256: 9b3aaff8ba2182b146ea6b1ec463efd8dc8b1c5532a8d4db6cf716938536ae02
public release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.14
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
mandatory data update from v0.1.13: none
```

공개 Release는 `draft=false`, `prerelease=false`이고 public tag SHA와 release target은 exact release baseline과 일치합니다. 공개 ZIP과 `SHA256SUMS.txt`를 다시 다운로드해 checksum, package root, ProductVersion을 검증했고 실제 공개 EXE의 rendered UI / Main Map / Factory / MiniMap / 정상 종료 smoke까지 통과했습니다.

상세: `docs/RELEASE_0.1.14.md`

## v0.1.14 — 사용자 동의형 프로그램 업데이트

확정 제품 동작:

```text
일반 실행
→ latest public stable GitHub Release 조회
→ 현재 버전보다 최신이면 사용자에게 업데이트 여부 질문
→ 동의 시 ZIP + SHA256SUMS 다운로드
→ SHA-256 / package security contract 검증
→ 현재 프로그램 종료
→ 임시 self-copy updater가 program-owned files 교체
→ 새 버전 자동 재실행
```

실패 정책:

- update check 실패 → 앱 정상 사용
- 사용자 거절 → 앱 정상 사용
- download/checksum/package validation 실패 → 현재 프로그램 파일 미변경
- updater 시작 실패 → 현재 프로그램 파일 미변경
- 교체 실패 → previous program files rollback 시도 + 기존 EXE 재실행 시도
- 실패 진단 → `%LocalAppData%/JunhyunHelper/logs/startup.log`

업데이트 대상:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

업데이트 비대상:

```text
%LocalAppData%/JunhyunHelper/user.db
content/
image-cache/
map-product-settings.json(.bak)
ammo-favorites.json(.bak)
logs/
```

상시 `Updater.exe`는 배포하지 않습니다. 현재 EXE의 임시 복사본을 `%TEMP%/JunhyunHelper/updater/<guid>`에서 updater mode로 실행합니다.

**Bootstrap:** 공개 v0.1.13에는 updater 코드가 없으므로 v0.1.13 → v0.1.14는 한 번 수동 교체가 필요합니다. v0.1.14 이후 후속 정식 릴리즈는 프로그램 안에서 업데이트 가능합니다.

상세: `docs/PROGRAM_UPDATE.md`

## 제품 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 / GameMode별 독립 진행 |
| Quest | 구현 완료 / `확인 필요` 분리 / special trader + exact profile-variable + audited compatibility |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 / unresolved future Quest Item 보호 / consumption ledger |
| Ammo | 구현 완료 / caliber favorites / atomic preference recovery |
| Map + MiniMap | 구현 완료 / exact floor-frame / persisted settings recovery / rendered sidebar gate |
| Program Update | 구현 완료 / v0.1.14 public verified |
| Scanner | **`준비 중` placeholder** / 별도 사용자 요구 전 실제 기능 구현 금지 |

## Content / User Progress

```text
Content schema: v7
Readable: v3, v4, v5, v6, v7
user.db SQLite schema: v1
v0.1.13 → v0.1.14 mandatory data update: none
```

- exact profile-variable fact가 있으면 권위값으로 판정
- 증명 불가능한 Quest availability는 `확인 필요`
- unresolved future Quest Item은 `IndeterminatePotential`로 Needed Items 보호
- Game Content update는 `user.db`를 삭제하거나 덮어쓰지 않음
- program update와 Game Content update는 별도 subsystem

## Map / MiniMap 기준

Pinned donor-derived product revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

- floor는 visibility filter가 아니라 presentation relation
- enabled 타층 marker 유지
- Main Map floor 변경 시 zoom + map-space center 보존
- MiniMap floor 변경 시 exact Scale + Translate X/Y 보존
- current Quest sidebar: `30px checkbox | 34px A/B/C/D | * Quest text`
- rendered title X-axis/handle 위치를 release smoke가 실제 WPF layout으로 검증
- 안정적인 donor Map 경로는 구체적 regression/performance 이유 없이 wholesale refactor하지 않음

## 유지되는 v0.1.13 hardening

- Map/Ammo preference atomic replacement + `.bak` recovery
- corrupt primary → good backup fallback
- presentation preference save failure nonfatal
- Map slider save coalescing + dispose flush
- Map hotkey / NumPad direct floor async failure containment
- keyboard hook failure diagnostics
- canonical validator의 empty Quest item candidate / non-positive Quest·Hideout count 차단

## 현재 알려진 비차단 범위

- EFT 1.0 Story Chapters는 ordinary `json.tarkov.dev/tasks` progression source 밖이며 현재 미지원
- PvE Skier LL2 task-pool drift는 exact fact가 없으면 해당 pool만 fail-closed
- Map donor/bridge maintenance debt는 안정성이 유지되는 동안 임의 정리하지 않음
- code signing / installer는 현재 제품 필수 범위 아님
- Scanner는 사용자 별도 요구 전 `준비 중` 유지

## 검증 기준

상시 `.github/workflows/ci.yml`:

1. Release build
2. 전체 자동 테스트
3. win-x64 self-contained single-file publish
4. package/dependency hygiene
5. 실제 published EXE 실행
6. rendered Product UI assertions
7. Main Map / Factory / MiniMap smoke
8. graceful shutdown

v0.1.14 릴리즈부터 public Release는 **Draft asset 검증 → public 전환 → public asset 재다운로드 검증** 순서를 사용합니다.
