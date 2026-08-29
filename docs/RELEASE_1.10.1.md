# 준현 헬퍼 v1.10.1 릴리즈 기록

기준일: 2026-08-29 KST

상태: **RELEASE CANDIDATE — public verification pending**

## 목적

v1.10.0 전체 유지보수 감사에서 확인된 WPF 초기화 수명주기 결합을 제거하고, 현재 실행 경로에서 사용되지 않는 일회성 저장소 잔재와 패키지 changelog 중복을 정리한다. 새 사용자 기능은 추가하지 않는다.

## 제품 변경

- 메인 헤더 보강 초기화를 static class-level `Loaded` handler에서 `MainWindow.OnInitialized` 소유의 explicit schedule로 이동.
- header `DependencyPropertyDescriptor` watcher를 MainWindow 종료 시 명시 해제.
- 헤더의 버전-only 표시와 아이템 정리 오렌지 점 의미는 그대로 유지.
- `.github/scripts/finalize-v121.py` 제거. v1.2.1 역사적 release evidence는 공식 docs/release에 유지.
- `FIRST_RUN_KO.txt`를 현재 설치 안내 + 최근 유지보수 변경 중심으로 정리. 과거 전체 changelog는 GitHub Releases/docs가 권위.

## 비변경 계약

- Scanner OCR threshold / matcher / candidate cap / visual recovery acceptance
- Scanner Item ID identity authority / presentation-only price join
- Game Content v8 / readable v3~v8 / LKG / completeness / fail-closed
- user.db schema v1
- Scanner display settings v7
- Scanner catalog cache v1~v4 readable / v4 written
- Map/MiniMap donor pin 및 marker/Factory floor/viewport semantics
- v1.10.0 MiniMap same-window reopen synchronization
- Scanner Favorites / Recents / canonical item-open boundary

## 검증 계획

```text
Desktop target version: 1.10.1
Release build: pending
Automated tests: pending
Published EXE runtime smoke: pending
PR CI: pending
Exact-main CI: pending
Release workflow: pending
Public release/tag/assets/checksum: pending
User real-PC/play environment: pending
```

공개 이후 exact source, CI/run IDs, test count, ProductVersion, asset bytes/SHA-256, tag target, release ID를 이 문서와 canonical state docs에 기록한다.
