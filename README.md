# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 현재 공개 버전

**v0.1.14 PUBLIC RELEASE / VERIFIED — Windows x64**

**다운로드:** https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.14

v0.1.14는 사용자가 확정한 **프로그램 자체 업데이트**를 도입한 릴리즈입니다.

```text
프로그램 실행
→ 최신 정식 GitHub Release 확인
→ 새 버전이 있으면 사용자에게 업데이트 여부 질문
→ 동의 시 다운로드 + SHA-256/패키지 검증
→ 프로그램 파일 교체
→ 새 버전 자동 재시작
```

- 최신 버전이 없으면 별도 UI 없이 기존처럼 실행됩니다.
- 사용자가 업데이트를 거절하면 현재 버전을 그대로 사용하고 다음 실행 때 다시 확인합니다.
- GitHub/네트워크 조회 실패는 프로그램 실행을 막지 않습니다.
- 다운로드/검증 실패 시 현재 프로그램 파일을 변경하지 않습니다.
- 실제 교체 중 실패하면 기존 program-owned files로 rollback을 시도하고 기존 실행 파일을 다시 실행합니다.
- 프로그램 업데이트는 `%LocalAppData%/JunhyunHelper`의 `user.db`, Game Content, image cache, Map 설정, Ammo 즐겨찾기를 건드리지 않습니다.
- 상시 `Updater.exe`를 배포하지 않고 현재 실행 파일의 임시 self-copy를 updater mode로 사용합니다.
- Scanner는 기존대로 상단 `스캐너` 탭의 **`준비 중` placeholder**이며 실제 Scanner 기능은 추가하지 않았습니다.

### v0.1.13 사용자의 최초 전환

**v0.1.13에는 updater 코드가 없으므로 v0.1.13 → v0.1.14는 한 번 수동으로 ZIP을 받아 교체해야 합니다.**

v0.1.14를 한 번 실행한 이후부터는 후속 정식 릴리즈를 프로그램 안에서 업데이트할 수 있습니다.

## v0.1.14 공개 검증

```text
release baseline / tag SHA: bb0611e9263c24018825a87a58aba2c5474b6cc4
ProductVersion: 0.1.14+bb0611e9263c24018825a87a58aba2c5474b6cc4
feature PR: #100
feature CI: 32115435656 — SUCCESS
release PR: #101
release PR CI: 32115953069 — SUCCESS
public verification PR: #102
public verification workflow: 32116726491 — SUCCESS
automated tests: 232 passed / 0 failed / 0 skipped
Windows x64 self-contained single-file publish: SUCCESS
published/public rendered Product UI smoke: SUCCESS
Main Map / Factory / MiniMap runtime smoke: SUCCESS
normal Main Window close / process exit: SUCCESS
asset: Junhyun-Helper-v0.1.14-win-x64.zip
asset size: 74,086,942 bytes
SHA-256: 9b3aaff8ba2182b146ea6b1ec463efd8dc8b1c5532a8d4db6cf716938536ae02
public ZIP re-download + checksum/ProductVersion/package verification: SUCCESS
```

정식 릴리즈는 먼저 Draft 상태에서 업로드한 자산을 다시 내려받아 검증하고, 검증 성공 후에만 public/latest로 전환합니다. 공개 후에도 ZIP을 다시 내려받아 동일 검증을 반복합니다.

## 주요 기능

- GameMode별 Profile 관리
- Quest 진행/잠김/사용 불가/완료/확인 필요 판정과 선행조건 연결
- Quest 제출 Item / 자동 소비·rollback ledger
- Hideout 레벨 / 미래 업그레이드 재료
- 미래 Quest + Hideout 기준 Needed Items
- FIR / 일반 Inventory와 안전한 cleanup 계산
- flexible hand-in 후보 그룹과 보수적 Item 보호
- Item 종류/용도/필요 상태 필터, cross-navigation, Item Wiki
- Ammo 성능/수급처/Armor Class 1~6 비교와 caliber favorites
- 온라인 Game Content 안전 업데이트와 image cache
- Map + MiniMap
  - 현재 Quest sidebar / A·B·C·D marker identity
  - 일반 marker / PMC·Scav·Transit 탈출구
  - floor / zoom / MiniMap 크기 hotkey
  - 타층 marker 유지 + 현재층/위층/아래층 relation 표시
  - Main Map floor 변경 시 zoom + map-space viewport center 보존
  - MiniMap floor 변경 시 exact Scale + Translate frame 보존
  - screenshot 기반 Map 전환 / player tracking
- 상단 `스캐너` 탭 — 현재 `준비 중` placeholder
- 실행 시 사용자 동의형 프로그램 업데이트

## 데이터 / 호환성

```text
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1
v0.1.13 → v0.1.14 mandatory data update: none
```

기존 Profile / Quest / Inventory / Hideout / Map 설정 / Ammo 즐겨찾기는 그대로 유지됩니다.

Game Content update와 프로그램 업데이트는 별도 subsystem입니다.

```text
Game Content update
온라인 데이터 → 검증 → canonical 변환 → candidate DB → 관계/read-back 검증 → active 교체

Program update
GitHub stable Release → 사용자 동의 → ZIP/checksum 검증 → program-owned files 교체 → 재시작
```

Runtime GPT/AI 의존성은 없습니다.

## 실행 / 배포 형태

1. GitHub Release에서 `Junhyun-Helper-v0.1.14-win-x64.zip`을 다운로드합니다.
2. 원하는 폴더에 압축을 풉니다.
3. **`준현 헬퍼.exe`**를 실행합니다.

배포 루트:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

Windows x64 portable / self-contained single-file 빌드이며 별도 .NET 설치나 관리자 권한은 필요하지 않습니다. 코드 서명은 아직 적용하지 않아 Windows SmartScreen 경고가 표시될 수 있습니다.

사용자 데이터와 로그는 프로그램 폴더가 아니라 `%LocalAppData%/JunhyunHelper`에 저장됩니다.

## 정확도 / 안전성 원칙

- source가 제공하는 Quest prerequisite 의미를 보존합니다.
- 증명할 수 없는 availability를 임의로 해금하지 않고 `확인 필요`로 유지합니다.
- exact EFT profile-variable 값이 있으면 해당 값이 권위값입니다.
- current-version compatibility는 감사된 구조가 정확히 일치할 때만 사용하고 drift가 있으면 fail-closed 합니다.
- unresolved future Quest Item은 Needed Items에서 계속 보호합니다.
- flexible hand-in의 실제 소비 후보를 임의 추측하지 않습니다.
- 설정/즐겨찾기 JSON은 atomic replacement + `.bak` recovery를 사용합니다.
- 공개 릴리즈 ZIP은 `SHA256SUMS.txt`와 대조하여 검증합니다.

## 개발 문서

- [`docs/STATE.md`](docs/STATE.md) — canonical 현재 프로젝트 상태
- [`docs/CURRENT_STATE.md`](docs/CURRENT_STATE.md) — 짧은 현재 상태 인덱스
- [`docs/PRODUCT.md`](docs/PRODUCT.md) — 공식 제품 요구사항
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — 현재 유효한 장기 결정
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 기술 구조
- [`docs/PROGRAM_UPDATE.md`](docs/PROGRAM_UPDATE.md) — 프로그램 업데이트 제품/실패 계약
- [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md) — 배포 및 공개 검증 계약
- [`docs/RELEASE_0.1.14.md`](docs/RELEASE_0.1.14.md) — v0.1.14 공개 검증 기록
- [`docs/MAP_PRODUCT_REQUIREMENTS.md`](docs/MAP_PRODUCT_REQUIREMENTS.md) — Map/MiniMap 제품 기준
- [`docs/QUEST_PREREQUISITE_SEMANTICS.md`](docs/QUEST_PREREQUISITE_SEMANTICS.md) — Quest 선행조건 의미
- [`docs/FINAL_AUDIT_2026-08-18.md`](docs/FINAL_AUDIT_2026-08-18.md) — v0.1.13 hardening 전 전체 프로그램 감사
