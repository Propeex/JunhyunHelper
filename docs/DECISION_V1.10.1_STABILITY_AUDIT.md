# v1.10.1 안정성 감사 결정

기준일: 2026-08-29 KST

상태: **IMPLEMENTED / PUBLIC VERIFIED v1.10.1**

## 목적

v1.10.0 공개 제품을 새 기능 추가 없이 다시 감사하고, 실제 안정성 이득이 확인되는 부분만 최소 수정한다. 기존 제품 의미와 사용자 경험, Scanner 인식 계약, Game Content fail-closed 계약, Map/MiniMap 의미는 보존한다.

## 감사 범위

- Desktop/WPF 초기화·종료 수명주기
- 사용자 진행 저장 경계와 busy serialization
- Scanner background/runtime dispose 경계
- Program Update 실패·복구 경로
- local JSON/SQLite persistence
- CI/package/release immutable gate
- 사용되지 않는 일회성 repository maintenance artifact
- canonical project documentation drift

## 확인 결과

### 유지 — 변경 근거 없음

- `UserProfileStore`는 UI mutation 경계에서 `SetBusy(true)`로 사용자 입력을 차단하고 DB 성공 뒤 메모리 캐시를 갱신한다. 현재 실사용 증거 없이 별도 전역 저장 락을 추가하지 않는다.
- `AtomicJsonFileStore`는 same-directory temp + write-through + flush-to-disk + readable backup promotion을 사용하므로 현재 계약을 유지한다.
- Program Update는 기존 stable release checksum/immutable 정책과 실패 시 기존 버전 보존·복구 재시작 계약을 유지한다.
- Scanner runtime/context monitor의 current serialization/disposal 계약은 실제 실패 증거 없이 변경하지 않는다.
- Scanner OCR threshold, matcher, candidate cap, visual recovery acceptance, catalog identity authority는 변경하지 않는다.
- Game Content schema/LKG/completeness/fail-closed와 Map/Factory/MiniMap semantics는 변경하지 않는다.

### 수정 — WPF header lifecycle ownership

`MainWindow.HeaderStatusPolish.cs`가 메인 헤더의 제품 UI 보강을 위해 정적 `EventManager.RegisterClassHandler(... Loaded ...)`에 의존하고 있었다. 이는 이전 장기 유지보수 감사에서 제거한 incidental Loaded ownership과 같은 종류의 숨은 초기화 결합이다.

v1.10.1은 다음으로 변경했다.

- `MainWindow.OnInitialized`에서 `ScheduleHeaderStatusPolish()`를 명시적으로 호출한다.
- 실제 visual-tree 변경은 기존처럼 `DispatcherPriority.Loaded`에서 한 번 수행한다.
- 정적 class-level Loaded handler와 registration sentinel을 제거한다.
- `DependencyPropertyDescriptor.AddValueChanged`로 등록한 header status watcher는 `MainWindow.OnClosed`에서 `RemoveValueChanged`로 명시 해제한다.

사용자-visible 의미는 유지한다.

- 헤더에는 버전 정보만 표시
- 정리 가능한 아이템이 있으면 아이템 탭 우측 상단의 작은 오렌지 점 표시
- 기존 내부 status/progress 및 dedicated update overlay 의미 유지

### 제거 — obsolete v1.2.1 finalization helper

`.github/scripts/finalize-v121.py`는 v1.2.1 공개 당시 문서에 고정된 SHA/run/asset 값을 기록하기 위한 일회성 스크립트였다. 현재 CI, Release workflow, source build, test, package, current docs 어느 실행 경로에서도 사용되지 않아 제거했다.

역사적 v1.2.1 릴리즈 증거는 해당 release/docs에 보존한다.

### 정리 — packaged release notes

`packaging/FIRST_RUN_KO.txt`에 누적되던 장기간의 historical changelog를 패키지에 계속 중복 보관하지 않는다. v1.10.1부터 설치/실행 안내, 현재 maintenance 변경, 직전 핵심 변경만 남긴다. 전체 역사와 immutable release evidence의 권위는 GitHub Releases와 `docs/`다.

## 회귀 방지

`DesktopStartupWiringContractTests`를 확대하여 다음을 고정한다.

- header initialization은 `MainWindow.ProductLifecycle`에서 명시적으로 소유
- header source에 `RegisterClassHandler`가 다시 들어오지 않음
- header `DependencyPropertyDescriptor` subscription이 종료 시 해제됨
- 기존 page infrastructure explicit ownership 계약 유지

## 공개 검증

```text
exact product release source/tag target:
c444a1e26793e15c075875159f6605d8a99cf7f9
PR CI: 33253141127 — SUCCESS
exact-main CI: 33253293015 — SUCCESS
Release workflow: 33253438908 — SUCCESS
439 passed / 0 failed / 0 skipped
release id: 378982127
public ZIP bytes: 80,540,164
public ZIP SHA-256:
c37c00a5e5ecdc431d6b26775d73682cabf17e4310533065c88e2d58d8f14922
```

PR와 exact-main에서 Release build, self-contained publish, actual published EXE Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke, graceful shutdown, clean portable root, package audit가 모두 통과했다. `/releases/latest`와 `refs/tags/v1.10.1` readback에서 stable release와 tag가 exact product source를 가리키는 것도 확인했다.

## 버전 판단

기존 사용자 기능의 의미를 바꾸지 않는 안정성/유지보수 변경이므로 PATCH release **v1.10.1**로 공개했다.

사용자 실제 PC/Tarkov 환경에서의 실사용 검증은 자동 검증과 별도로 아직 확인 전이다.
