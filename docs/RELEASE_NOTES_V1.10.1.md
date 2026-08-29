# 준현 헬퍼 v1.10.1

## 목적

v1.10.1은 v1.10.0 공개 제품을 안정성 중심으로 다시 감사하여, 사용자 기능의 의미를 바꾸지 않고 WPF 제품 수명주기 소유권과 저장소 유지보수 잔재를 정리하는 PATCH 릴리즈다.

새 사용자 기능은 추가하지 않는다. Scanner 인식 기준, Game Content 안전 정책, Map/MiniMap 의미도 변경하지 않는다.

## MainWindow 헤더 수명주기 안정화

메인 헤더의 현재 제품 계약은 다음과 같다.

- 상단 상태 영역에는 버전 정보만 표시한다.
- 정리 가능한 아이템이 있으면 아이템 탭 우측 상단에 작은 오렌지 점을 표시한다.
- 게임 데이터 업데이트 진행 상황은 전용 progress overlay에서 표시한다.

기존 구현은 이 UI 보강을 활성화하기 위해 `EventManager.RegisterClassHandler(... Loaded ...)` 정적 클래스 핸들러에 의존했다. 실제 표시 결과는 맞았지만, 제품 창 초기화가 routed `Loaded` 발생 방식과 타입 수준의 숨은 등록에 연결되는 구조였다.

v1.10.1은 사용자-visible 결과를 유지하면서 lifecycle ownership을 명시화한다.

- `MainWindow.OnInitialized`가 header polish 초기화를 소유한다.
- visual tree 변경이 필요한 실제 적용은 `DispatcherPriority.Loaded`에서 한 번 수행한다.
- 정적 class-level `Loaded` handler와 registration sentinel은 제거한다.
- `DependencyPropertyDescriptor.AddValueChanged`로 등록한 상태 감시는 `MainWindow.OnClosed`에서 `RemoveValueChanged`로 명시 해제한다.

따라서 헤더 기능 자체를 바꾸는 것이 아니라, 초기화와 종료 책임을 제품 창 수명주기에 대칭적으로 귀속한다.

## 저장소 및 패키지 정리

### v1.2.1 일회성 finalization helper 제거

`.github/scripts/finalize-v121.py`는 v1.2.1 공개 당시 특정 SHA, CI run, asset 값을 문서에 반영하기 위한 일회성 스크립트였다.

현재 CI, Release workflow, build, test, package, current documentation update 경로에서는 사용되지 않는다. v1.2.1의 역사적 릴리즈 증거는 기존 GitHub Release와 공식 문서에 이미 보존되어 있으므로 실행 잔재만 제거한다.

### FIRST_RUN_KO.txt 정리

배포 패키지의 `FIRST_RUN_KO.txt`에 오래된 버전별 변경 내역을 계속 누적하지 않는다.

v1.10.1부터는 다음만 유지한다.

- 설치/실행 안내
- 현재 릴리즈의 핵심 변경
- 직전 핵심 변경
- 전체 변경 이력과 검증 기록을 확인할 공식 GitHub/docs 위치

역사적 변경 내역과 immutable release evidence의 권위는 GitHub Releases와 `docs/`다.

## 감사 후 유지한 영역

전체 점검 결과 다음 영역은 현재 방어 계약이 충분하고, 실제 오류 증거 없이 추가 변경할 경우 회귀 위험이 더 크다고 판단해 유지한다.

- 사용자 진행 저장 경계와 UI busy serialization
- atomic JSON 저장의 same-directory temp / flush / backup promotion
- Program Update checksum / immutable stable / 실패 복구 정책
- Scanner runtime/context monitor 직렬화 및 disposal 구조
- Scanner OCR threshold
- matcher / candidate cap
- visual corroboration / recovery acceptance
- Scanner canonical Item ID identity policy
- Game Content v8 / v3~v8 read compatibility
- Game Content LKG / relationship completeness / fail-closed
- Scanner Favorites / Recents / canonical item-open boundary
- Map marker / Factory floor / viewport 의미
- v1.10.0 MiniMap same-window reopen synchronization
- pinned Map/MiniMap donor commit

## Schema / compatibility

```text
Desktop target version: 1.10.1
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v7
Scanner catalog cache: v1~v4 readable, v4 written
```

v1.10.0 → v1.10.1에서 Game Content 또는 user.db 강제 마이그레이션은 없다.

## 공개 검증

```text
exact product release source/tag target:
c444a1e26793e15c075875159f6605d8a99cf7f9
PR CI: 33253141127 — SUCCESS
exact-main CI: 33253293015 — SUCCESS
Release workflow: 33253438908 — SUCCESS
439 passed / 0 failed / 0 skipped
release id: 378982127
public asset: Junhyun-Helper.zip
bytes: 80,540,164
SHA-256: c37c00a5e5ecdc431d6b26775d73682cabf17e4310533065c88e2d58d8f14922
```

PR CI와 exact-main CI에서 Release build, Windows x64 self-contained publish, actual published EXE Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke, graceful shutdown, clean portable root와 release package audit가 모두 성공했다.

GitHub `/releases/latest`에서 v1.10.1이 `draft=false`, `prerelease=false`, latest stable임을 확인했고 release target은 exact product release source와 일치한다. `refs/tags/v1.10.1`도 같은 source commit을 직접 가리킨다.

사용자의 실제 PC/Tarkov 플레이 환경 실사용은 자동 검증과 별도로 아직 확인 전이다.
