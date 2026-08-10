# 준현 헬퍼 v0.1.0

Release date: **2026-08-10**

Status: **RELEASED**

## 배포 대상

- Windows x64
- portable ZIP
- self-contained .NET 10
- single-file application executable
- 실행 파일: `준현 헬퍼.exe`

배포 루트:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

`Assets/`는 Map/MiniMap이 파일 경로로 직접 읽는 검증된 artwork/config/general-marker bundle입니다. .NET/WPF/SQLite/SkiaSharp 런타임과 관리 DLL은 single-file 실행 파일에 묶습니다.

## v0.1.0 기능

- GameMode별 Profile 관리
- Quest 진행/잠김/사용 불가/완료 판정
- Quest 선행 조건 및 제출 Item
- Quest/Hideout 고정 재료 자동 차감 ledger + rollback
- Hideout 현재 레벨/미래 upgrade material
- 미래 Quest + Hideout 기반 Needed Items
- 인레이드/일반 Inventory와 안전한 cleanup 계산
- flexible hand-in 그룹
- Item 종류/용도/필요 상태 filter 및 cross-navigation
- Ammo 성능/수급처/Armor Class 1~6 비교와 caliber favorites
- 온라인 Game Content 안전 업데이트 + image cache
- Map + MiniMap
  - Current Quest sidebar / A·B·C Quest marker
  - 일반 marker / PMC·Scav·Transit 탈출구
  - Main Map + MiniMap floor/zoom hotkey
  - MiniMap 크기 hotkey / 기본 투명도 / 일시 투명 / marker scale
  - screenshot 기반 Map 전환 / player tracking
- 상단 Scanner 탭
  - v0.1.0에서는 `준비 중` placeholder만 제공
  - 실제 Scanner 기능은 후속 요구사항 확정 후 구현

## 최종 검증

기준 코드:

```text
PR #74 final head: 47f3ec4cabf70879465b216bc42fecea23e514da
PR #74 merge:      e282fffebcb1004ddab0b028b6db5ad0d88db279
CI run:            31356282143
```

검증 결과:

```text
Desktop Release build                 SUCCESS
Automated tests                       163 passed / 0 failed
Windows x64 self-contained publish    SUCCESS
Korean executable                     준현 헬퍼.exe
Map + MiniMap real startup smoke      SUCCESS
Normal Main Window close/process exit SUCCESS
Release root DLL                      0
PDB                                   0
Nested ZIP                            0
Runtime Logs folder beside EXE        0
Legacy forbidden dependencies         0
```

## 최종 배포물

GitHub Actions artifact:

```text
artifact id: 9050775673
size: 73,973,345 bytes
SHA-256: 6db752972b3b52d9e6239c746bb910904a91d364c2410062f4c1635ac61efcaa
entries: 32
```

ZIP 내부 확인:

```text
root entries: 준현 헬퍼.exe / FIRST_RUN_KO.txt / Assets
DLL: 0
PDB: 0
nested ZIP: 0
Logs: 0
```

## 사용자 데이터

사용자 진행/설정/로그는 프로그램 폴더와 분리해 다음 아래에 저장합니다.

```text
%LocalAppData%/JunhyunHelper
```

프로그램 ZIP 교체가 `user.db`와 기존 User Progress를 삭제하거나 덮어쓰지 않습니다.

## v0.1.0 비차단 후속 범위

- Scanner 실제 기능 설계/구현
- Map artwork/config/general-marker atomic bundle updater
- code signing
- installer
- application auto-updater
- user.db 자동 backup/restore UX
- 본격 공개 재배포 시 repository license / third-party notice 정책 확정

위 항목은 v0.1.0의 기능/패키징 blocker로 판정하지 않았습니다.
