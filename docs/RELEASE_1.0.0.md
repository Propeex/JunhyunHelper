# RELEASE 1.0.0 — 정식 안정판

상태: `RELEASE CANDIDATE`

날짜: 2026-08-19

## 목적

v0.1.14까지 구현·검증된 준현 헬퍼를 새로운 기능 추가 없이 정리하고, 내부 하드닝·개발자 문서·배포 검증을 완료하여 첫 정식 안정판 `v1.0.0`으로 승격합니다.

## 제품 범위

v1.0.0은 v0.1.14의 사용자 기능을 그대로 유지합니다.

- Profile
- Quest
- Hideout
- Items / Inventory / Needed Items / Cleanup
- Ammo
- Map / MiniMap
- Game Content update
- 사용자 동의형 Program update
- Scanner `준비 중` placeholder

Scanner 실제 기능은 v1.0.0에 추가하지 않습니다.

## 내부 하드닝

- 현재 제품 규칙에서 사용되지 않는 과거 Hideout cleanup compatibility API 제거
- `user.db` schema initialization을 store instance당 한 번으로 제한
- shared online-data HTTP User-Agent를 assembly version에서 파생해 버전 drift 제거
- CI에서 project version ↔ published ProductVersion ↔ FIRST_RUN version identity 확인
- release tree의 nested archive 오염 차단
- v1.0.0 개발자 reference와 version policy 공식화

## 데이터 호환성

- Content schema: v7
- readable Content schema: v3-v7
- user.db schema: v1
- v0.1.14 → v1.0.0 필수 Game Content refresh: 없음
- v0.1.14 → v1.0.0 user data migration: 없음

기존 `%LocalAppData%/JunhyunHelper`의 profile, quest progress, inventory, hideout progress, Map settings, Ammo favorites는 유지됩니다.

## 배포

- Windows x64
- .NET 10 self-contained
- portable
- single-file executable
- root contract:
  - `준현 헬퍼.exe`
  - `FIRST_RUN_KO.txt`
  - `Assets/`
- installer 없음
- 관리자 권한 불필요
- code signing 없음

## 정식 릴리즈 gate

다음을 모두 통과해야 public v1.0.0으로 인정합니다.

1. Release build
2. full automated tests
3. win-x64 self-contained single-file publish
4. ProductVersion = 1.0.0
5. FIRST_RUN = v1.0.0
6. package root/dependency/PDB/nested archive audit
7. 실제 publish EXE 실행
8. rendered Product UI smoke
9. Main Map smoke
10. Factory smoke
11. MiniMap smoke
12. 정상 MainWindow close 및 process 종료
13. draft release package SHA-256/package identity 재검증
14. public 전환 후 public asset 재다운로드 SHA-256/ProductVersion/package 재검증
15. latest stable release가 v1.0.0인지 확인
16. 기존 모든 v0.x GitHub Release 제거

## 이후 버전 규칙

공식 규칙은 `VERSIONING.md`를 따릅니다.

- 새 기능 추가 → MINOR +1, PATCH=0
- 기존 기능 수정/보완/버그 수정/성능·안정성 개선 → PATCH +1

예:

- `1.0.0` + Scanner 실제 기능 → `1.1.0`
- `1.0.0` + Quest 수정 → `1.0.1`
- `1.0.1` + Scanner 실제 기능 → `1.1.0`

## 릴리즈 후 기록할 값

아래 값은 public verification이 끝난 뒤 확정 기록합니다.

- release baseline commit SHA
- CI run
- automated test count
- ZIP byte size
- ZIP SHA-256
- public release URL/tag 상태
- 삭제된 v0.x release 목록
