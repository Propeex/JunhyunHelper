# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-31 KST**

## Goal

사용자 실사용 요구사항을 반영한 **v1.12.0** 개발 배치를 진행한다.

- Quest 선행조건/상태 계산 회귀 수정: 새 프로필에서는 `확인 필요` 0개인데 일부 퀘스트 진행 후 정상 퀘스트가 대량으로 `확인 필요`로 늘어나는 현상을 수정한다.
- Hideout 검색창의 clear `×` 위치를 다른 탭 검색창과 일관되게 맞춘다.
- 메인 좌측 상단 프로필 이미지 클릭으로 실행하는 `김태영 PC 진단` 지원 기능을 추가한다.

## Base / working state

```text
base main: b97556bfe162bd6d6507500eb1633adf4607efb6
public stable: v1.11.4
working branch: feature/v1.12.0-quest-diagnostics-search-ui-2026-08-31
PR: #237 (draft)
latest verified feature head: 6d289265d40fbabd68906952075d07f5f3f30e96
PR CI: 33348464596 — SUCCESS
PR Shutdown Race CI: 33348464611 — SUCCESS
PR Documentation Consistency: 33348464606 — SUCCESS
```

## Confirmed scope and implementation

### Quest

사용자 실사용 캡처의 `확인 필요 49`와 current EFT 1.1 audited task-pool 구조를 대조했다.

- current source에는 LL1 task-pool Quest가 정확히 **48개** 존재한다.
- 기존 compatibility는 LL1 trader에서 첫 Quest를 완료한 순간 hidden pool variable의 exact write semantics를 알 수 없다는 이유로 해당 trader의 LL1 pool을 fail-closed로 돌렸다.
- trader LL이 올라간 이후에도 과거 LL1 pool을 계속 unknown으로 유지하여, 정상 진행 뒤 최대 48개가 한꺼번에 `확인 필요`로 노출되는 것이 사용자 증상의 주원인이다.
- EFT 1.1 side-task 제품 규칙은 같은 단계의 side-task 진행 외에 **다음 Trader Loyalty Level 도달 자체도 다음 그룹의 대체 unlock 조건**으로 정의한다.

수정 계약:

1. exact profile variable 값이 존재하면 계속 최우선 권위값이다.
2. audited pool stage보다 현재 trader LL이 낮으면 effective availability value=0으로 유지한다.
3. audited pool stage와 현재 trader LL이 같으면 기존 보수적 current-stage 계산을 유지한다.
4. **현재 trader LL이 audited pool stage보다 높으면 해당 과거 stage의 모든 threshold가 충족된 effective availability floor를 사용한다.** 이 값은 숨은 서버 counter의 exact 값이라고 주장하지 않으며 runtime Quest availability copy에만 사용한다.
5. 구조 drift 시 계속 fail-closed한다.
6. Future Needed Items / cleanup 안전 계산에는 이 current-UI compatibility를 낙관적으로 전파하지 않는다.

결정적 회귀 테스트에 LL1→LL2, LL2→LL3 과거 단계 unlock을 추가했다.

### Hideout search clear button

원인 확인:

- clear `×`는 TextBox 내부 template이 아니라 parent Grid에 올라가는 sibling overlay다.
- Hideout만 SearchBox 자체의 bottom margin으로 행 간격을 만들고 있었지만 overlay는 Right margin만 보정했다.
- 따라서 Hideout에서만 `×`가 TextBox 실제 사각형보다 아래로 치우쳤다.

수정:

- shared clear behavior가 TextBox의 Left/Top/Right/Bottom 외부 margin을 모두 반영하도록 변경했다.
- 다른 탭의 동일 behavior 계약은 그대로 유지한다.

### 김태영 PC 진단

사용자 확정 흐름:

1. 메인 헤더 좌측 프로필 이미지 클릭.
2. 팝업에서 김태영 본인인지 확인.
3. `예`를 누르면 진단 시작.
4. Scanner/capture 결과에 영향을 줄 수 있는 환경·디스플레이·그래픽·캡처·앱/Scanner 상태를 폭넓게 수집한다.
5. 진단 결과와 필요한 증거를 ZIP으로 묶어 바탕화면에 생성한다.
6. 완료 후 `hyune4784@naver.com`으로 ZIP을 보내 달라는 메시지를 표시하고 종료한다.

현재 구현:

- Windows/런타임/화면 구성/해상도/bpp/DPI
- GPU와 드라이버/모니터 상태 PowerShell-CIM probe
- dxdiag의 HDR support, color space, luminance, current mode, GPU/driver/display 관련 allowlist 필드
- Discord/OBS/NVIDIA/AMD/RTSS/Game Bar 등 Scanner/capture에 영향을 줄 수 있는 allowlist 프로세스 존재/버전
- Scanner display settings, runtime 상태, catalog 상태
- 기존 Scanner support/performance/log bundle 포함
- 각 디스플레이 화면 캡처 + RGB/휘도/clipping 통계
- Tarkov가 실행 중이면 client screen-copy와 PrintWindow capture 비교 + 통계
- 각 probe를 fail-soft로 격리하고 실패 목록을 ZIP에 기록
- 자동 업로드 없음; ZIP은 로컬 바탕화면에만 생성
- 사용자명, 컴퓨터명, IP/MAC, 네트워크 목록, secret/token, 임의 전체 프로세스 목록, 설치 경로는 수집하지 않음
- 화면 캡처가 포함될 수 있다는 점을 실행 확인창과 README에서 명시

## Completed

- Quest task-pool root cause 규명 및 past-stage availability 수정 구현
- LL1→LL2 / LL2→LL3 / exact-value / structural-drift 회귀 테스트 추가
- Hideout 검색창 clear `×` alignment 수정 및 source-contract test 추가
- 메인 헤더 이미지 entry point와 김태영 확인/완료 UX 구현
- 로컬 PC diagnostic ZIP exporter 구현
- v1.12.0 Desktop/FIRST_RUN 버전 정렬
- task-pool compatibility 결정 문서 정제
- 김태영 PC 진단 결정 문서와 v1.12.0 release notes 작성
- Draft PR #237 생성
- final feature head `6d289265d40fbabd68906952075d07f5f3f30e96`에서 Windows Release build/test/publish/Product UI+Map+graceful-shutdown/package 검증 성공
- final feature head에서 Shutdown Race CI와 Documentation Consistency 성공

## Current step

- PR #237을 ready로 전환하고 검증된 feature head를 main에 병합한다.
- 병합 후 exact-main CI / Shutdown Race / Documentation Consistency와 자동 stable release를 검증한다.

## Remaining

- PR ready / main 병합
- exact-main CI / Shutdown Race CI / Documentation Consistency 확인
- automatic v1.12.0 Release workflow 및 public tag/assets/checksum 검증
- PROJECT_STATE / CURRENT_STATE / STATE / README / PRODUCT / DECISIONS / DEVELOPER_REFERENCE 최종 release evidence 정렬
- ACTIVE_WORK 완료 처리
