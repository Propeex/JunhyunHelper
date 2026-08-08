# DEPLOYMENT — 준현 헬퍼 배포 원칙

## 현재 목표

첫 실사용 검토를 위해 Windows x64에서 바로 실행 가능한 self-contained ZIP 빌드를 제공합니다.

현재는 설치 프로그램, 자동 업데이트 클라이언트, 레지스트리 등록을 만들지 않습니다.

이유:

- 사용자가 먼저 실제 제품 흐름을 검토해야 함
- 배포 시스템을 제품 핵심보다 먼저 복잡하게 만들지 않음
- 실행 파일 교체와 사용자 데이터 보존을 명확히 분리

## 첫 테스트 빌드 형식

- Target: Windows x64
- .NET: self-contained
- Package: ZIP
- Entry point: `JunhyunHelper.exe`
- 별도 .NET Runtime 설치 요구 없음
- 관리자 권한 요구 없음
- 코드 서명 없음

WPF와 SQLite를 포함한 publish 결과 전체를 ZIP에 넣습니다. 단일 파일 publish 옵션을 사용하되 런타임이 추가 파일을 요구하는 경우를 막기 위해 publish 디렉터리 전체를 패키징합니다.

## 사용자 데이터 위치

실행 파일 위치와 무관하게 기본 데이터 루트는 다음입니다.

```text
%LocalAppData%\JunhyunHelper
```

### 보존 대상

```text
user.db
```

사용자 진행 사실의 기준입니다.

### 재생성 가능한 데이터

```text
content/
```

온라인 Game Content에서 다시 만들 수 있습니다.

## 버전 교체 원칙

새 ZIP을 다른 폴더에 풀거나 기존 실행 파일을 교체해도 `user.db`를 자동 삭제하지 않습니다.

프로그램 업데이트와 사용자 진행 초기화는 별개입니다.

- 프로그램 파일 교체 → user.db 유지
- Game Content 업데이트 → user.db 유지
- 프로필 삭제 → 선택 프로필 User Progress 삭제

향후 user.db schema 자체를 변경해야 할 때는 별도 migration + backup 정책을 먼저 설계하고 검증한 뒤 배포합니다. 현재 schema 변경을 자동 추측하거나 파괴적으로 재생성하지 않습니다.

## CI package 계약

main push의 정상 CI에서 다음을 순서대로 실행합니다.

1. Release Desktop build
2. 전체 테스트
3. `win-x64` self-contained publish
4. publish output 검증
5. `FIRST_RUN_KO.txt` 포함
6. `JunhyunHelper-win-x64.zip` 생성
7. GitHub Actions artifact 업로드

빌드 또는 테스트가 실패하면 package를 만들지 않습니다.

## 첫 실사용 전 확인 범위

자동 검증:

- Windows Release build
- 전체 단위/통합/회귀 테스트
- publish 성공
- `JunhyunHelper.exe` 생성 확인
- ZIP 생성 확인

사용자 PC 실사용에서 확인할 항목:

- 첫 실행
- SmartScreen/Windows 실행 경험
- 최초 온라인 content 다운로드
- 프로필 생성
- 앱 종료/재실행 후 user.db 보존
- 네트워크 실패 시 기존 content 사용/오류 안내
- 데이터 업데이트 후 User Progress 유지
- 화면 크기/DPI/가독성
- 실제 플레이 중 입력 부담

## 코드 서명

현재 첫 테스트 빌드는 서명하지 않습니다.

따라서 Windows SmartScreen 경고가 표시될 수 있습니다. 공개 배포 단계에서는 코드 서명 필요성과 배포 방식을 별도 결정합니다.
