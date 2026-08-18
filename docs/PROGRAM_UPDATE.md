# PROGRAM UPDATE — 제품 업데이트 계약

## 확정 요구사항

2026-08-18 사용자와 다음 동작을 제품 요구사항으로 확정했습니다.

1. 준현 헬퍼는 일반 실행 시 최신 **정식** 프로그램 버전을 조회한다.
2. 현재 버전보다 최신 정식 버전이 있으면 사용자에게 업데이트 동의 여부를 묻는다.
3. 사용자가 동의하면 업데이트를 진행하고 완료 후 새 버전으로 자동 재시작한다.

세부 구현과 실패 처리 정책은 JunhyunHelper가 소유합니다.

이 결정은 `DEC-034`의 “program auto-update는 v0.1.0 범위가 아니다”라는 과거 범위 문장과 `DEC-035`의 “application auto-updater는 v0.1.0 blocker가 아니다”라는 초기 릴리즈 범위 문장을 현재 제품 상태에서 supersede합니다. old `Tarkov-Helper`의 UpdateService를 되살리는 것이 아니라 JunhyunHelper 자체 업데이트 시스템을 구현합니다.

## 최신 버전 판정

- source of truth: `Propeex/JunhyunHelper` GitHub의 latest public Release
- 대상: `draft=false`, `prerelease=false`인 stable release
- 버전 형식: `vMAJOR.MINOR.PATCH`
- 현재 실행 버전보다 엄격히 높은 버전만 업데이트 대상으로 취급
- 예상 Windows package: `Junhyun-Helper-v<version>-win-x64.zip`
- checksum asset: `SHA256SUMS.txt`
- release/package shape가 계약과 다르면 자동 추측하지 않고 업데이트를 중단

## 실행 시 동작

업데이트 조회는 MainWindow가 표시된 뒤 startup flow에서 한 번 실행합니다.

- 최신 버전과 같거나 더 높으면 아무 UI도 띄우지 않음
- GitHub/네트워크 조회 실패는 진단 로그에 남기고 일반 프로그램 사용을 계속함
- 새 버전이 있으면 현재 버전 실행 중 한 번만 동의창 표시
- 사용자가 거절하면 현재 실행은 그대로 계속하며 다음 프로그램 실행 때 다시 확인

## 동의 후 준비 단계

사용자가 동의한 뒤에도 기존 프로그램 파일을 즉시 수정하지 않습니다.

1. `%LocalAppData%\JunhyunHelper\updates\pending` 아래 임시 작업 디렉터리 생성
2. `SHA256SUMS.txt` 다운로드
3. exact Windows ZIP 다운로드
4. ZIP SHA-256이 공개 checksum과 일치하는지 검증
5. ZIP path traversal / symlink / duplicate / 예상 밖 root / PDB를 차단
6. package root가 정확히 `준현 헬퍼.exe`, `FIRST_RUN_KO.txt`, `Assets/` 계약을 만족하는지 확인
7. 전부 성공한 뒤에만 파일 교체 단계로 진입

다운로드나 검증이 실패하면 현재 프로그램 파일은 변경하지 않고 MainWindow를 다시 사용할 수 있게 합니다.

## 파일 교체와 재시작

실행 중인 Windows EXE를 자기 자신이 직접 교체하지 않습니다.

- 현재 `준현 헬퍼.exe`를 `%TEMP%\JunhyunHelper\updater\<guid>`로 복사
- 복사본을 updater mode로 실행
- 원래 준현 헬퍼 정상 종료를 기다림
- program-owned files만 transaction 형태로 교체
  - `준현 헬퍼.exe`
  - `FIRST_RUN_KO.txt`
  - `Assets/`
- 새 파일을 먼저 같은 target volume에 준비한 후 기존 파일을 temporary previous 경로로 이동
- 교체 중 실패하면 가능한 범위에서 previous 파일을 원위치로 rollback
- 성공하면 새 `준현 헬퍼.exe` 자동 실행
- 실패하면 기존 실행 파일이 복구되어 있으면 기존 버전을 다시 실행

상시 `Updater.exe`는 공개 ZIP에 포함하지 않습니다.

## 사용자 데이터 경계

프로그램 업데이트는 다음 `%LocalAppData%\JunhyunHelper` 사용자/게임 데이터의 의미를 변경하거나 삭제하지 않습니다.

- `user.db`
- `content/`
- `image-cache/`
- `map-product-settings.json` 및 `.bak`
- `ammo-favorites.json` 및 `.bak`
- `logs/`

`updates/pending/`만 프로그램 업데이트의 임시 staging 용도로 추가됩니다.

프로그램 업데이트와 Game Content 업데이트는 서로 다른 subsystem입니다.

## 실패 정책

- update check 실패 → 앱 정상 사용
- 사용자 거절 → 앱 정상 사용
- download/checksum/package validation 실패 → 현재 프로그램 파일 미변경, 앱 정상 사용
- updater runner 시작 실패 → 현재 프로그램 파일 미변경, 앱 정상 사용
- 파일 교체 실패 → transaction rollback 시도, 기존 EXE가 있으면 자동 재시작
- 모든 실패는 `%LocalAppData%\JunhyunHelper\logs\startup.log`에 진단 기록

업데이트 실패를 일반 WPF global fatal로 확대하지 않는 것이 원칙입니다.

## 릴리즈 계약 영향

v0.1.14부터 자동 업데이트가 정상 작동하려면 모든 정식 릴리즈가 다음 계약을 유지해야 합니다.

- stable GitHub Release
- exact semantic tag `vMAJOR.MINOR.PATCH`
- exact Windows ZIP 이름
- `SHA256SUMS.txt`
- portable package root 계약 유지
- public asset checksum 재검증

코드 서명/installer는 이 기능과 별개의 제품 범위이며 현재 자동 업데이트의 필수 조건이 아닙니다.

## 검증 요구

최소 자동 검증:

- stable version parsing
- exact release asset selection
- exact checksum selection
- ZIP traversal 거부
- portable root validation
- owned file replacement
- old Assets 전체 교체
- unrelated target file 보존

릴리즈 검증에서는 기존 Windows x64 self-contained publish와 실제 EXE smoke를 계속 통과해야 합니다.
