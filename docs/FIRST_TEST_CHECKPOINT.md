# FIRST TEST CHECKPOINT — 2026-08-08

## 상태

**첫 실사용 테스트 가능 상태.**

제품 핵심 흐름 구현, 대형 패치 회귀 검증, Windows x64 self-contained publish와 ZIP artifact 생성까지 완료했습니다.

현재 다음 제품 단계는 새 기능 추가가 아니라 **사용자가 실제 프로그램을 실행해서 사용성/의도 불일치/실행 환경 문제를 확인하는 것**입니다.

## 테스트 빌드

- Product version: `0.1.0`
- Target: Windows x64
- Runtime: .NET 10 self-contained
- Entry point: `JunhyunHelper.exe`
- Installer: 없음
- Code signing: 없음
- Package contents:
  - `JunhyunHelper.exe`
  - `FIRST_RUN_KO.txt`

2026-08-08 CI 검증 기준:

- Windows Server 2025
- .NET SDK 10.0.302
- Desktop Release build: 0 warnings / 0 errors
- Tests: 134 passed / 0 failed / 0 skipped
- self-contained publish: success
- ZIP creation: success
- artifact upload: success

회수한 첫 테스트 ZIP SHA-256:

```text
009ee5579d5154cb05b47db30e7efb2bd5ea12d6bc3efb24af7aa591529204e7
```

GitHub Actions artifact 기록 digest:

```text
sha256:2bc74da9376da42193d1532795675cd45c6b3f1193c4691dd190edb80a95baae
```

## 사용자 데이터 보존

실행 파일과 User Progress는 분리됩니다.

```text
%LocalAppData%\JunhyunHelper\user.db
```

새 테스트 ZIP을 다른 폴더에 풀거나 실행 파일을 교체해도 user.db를 자동 삭제하지 않습니다.

Game Content는 다음 경로 아래의 재생성 가능한 데이터입니다.

```text
%LocalAppData%\JunhyunHelper\content
```

## 첫 실사용에서 우선 확인할 것

1. 압축 해제 후 실행 가능 여부
2. Windows SmartScreen 경험
3. 최초 Game Content 다운로드
4. 프로필 생성/수정/전환
5. Quest 실제 사용 흐름
6. Hideout 레벨 입력 부담
7. Item FIR/Non-FIR 수량 입력 부담
8. `필요 / 충분 / 정리 필요 / 판단 보류`의 직관성
9. Ammo 표/필터 가독성
10. 종료 후 재실행했을 때 user.db 보존
11. 데이터 업데이트 후 기존 진행/Inventory 보존
12. 실제 사용자 PC의 DPI/화면 크기/레이아웃 문제

## 개발 우선순위

첫 실사용이 시작된 이후에는 사용자 피드백과 실제 오류를 우선합니다.

- blocking 실행 오류 → 즉시 수정
- 잘못된 게임 판정/데이터 손상 위험 → 즉시 수정
- 실제 사용에서 불편한 UX → 원인과 빈도를 확인해 수정
- 단순 장식/추측성 기능 → 실사용 검토 전 추가하지 않음

지도와 Scanner는 여전히 핵심 실사용 검토 이후의 후속 기능입니다.

## 다음 대화 복구 규칙

새 대화에서는 `docs/STATE.md`를 먼저 읽고, 현재 테스트/배포 상태가 필요하면 이 파일과 `docs/DEPLOYMENT.md`를 이어서 확인합니다.

이 체크포인트 이후의 기본 다음 작업은 **첫 실사용 피드백 처리**입니다.
