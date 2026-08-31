# 준현 헬퍼 v1.12.0

v1.12.0은 Quest 진행 후 `확인 필요`가 대량으로 증가하던 선행조건 회귀를 수정하고, 은신처 검색창 UI를 정렬하며, 특정 PC의 비정상 화면 캡처/Scanner 환경을 자동으로 수집하는 **김태영 PC 진단**을 추가하는 MINOR 릴리즈입니다.

## Quest — 진행 후 `확인 필요` 폭증 수정

사용자 실사용 캡처에서는 새 프로필에서 `확인 필요 0`이었다가 일부 Quest/Trader 진행 후 `확인 필요 49`가 표시됐습니다.

재감사 결과 current EFT 1.1 dataset의 LL1 staged task-pool Quest가 정확히 48개이며, 기존 runtime compatibility가 현재 LL1에서 첫 Quest를 완료한 뒤 hidden pool counter를 알 수 없다는 이유로 해당 pool을 fail-closed 처리한 상태를 Trader LL이 올라간 뒤에도 유지하는 것이 주원인이었습니다.

v1.12.0은 audited task-pool 구조에 한해 다음 의미를 적용합니다.

- exact ProfileVariable 값이 있으면 계속 최우선입니다.
- current trader LL이 아직 pool stage보다 낮으면 해당 stage는 이전처럼 잠긴 값으로 평가합니다.
- 현재 stage에서는 기존의 보수적 reconstruction/fail-closed 규칙을 유지합니다.
- **현재 trader LL이 pool stage보다 이미 높으면, EFT 1.1의 next Trader LL alternate unlock 의미에 따라 그 과거 stage의 threshold는 current Quest availability에서 충족된 것으로 평가합니다.**
- 이 값은 숨은 서버 counter의 exact 값을 저장하거나 추정하는 것이 아니라 runtime-only availability floor입니다.
- 구조 drift가 생기면 다시 fail-closed합니다.
- Future Needed Items / cleanup 안전성에는 이 낙관적 current-UI compatibility를 전파하지 않습니다.

결정적 테스트는 LL1→LL2와 LL2→LL3 past-stage unlock, exact value precedence, current LL1 conservative behavior, structural drift fail-closed를 고정합니다.

## 은신처 검색창 `×`

은신처 검색창만 검색어 clear `×`가 다른 검색창과 다른 높이에 보이던 문제를 수정했습니다.

공통 clear affordance는 parent Grid의 sibling overlay인데, 기존에는 TextBox의 오른쪽 margin만 반영했습니다. 은신처 검색창은 row spacing을 TextBox 자신의 bottom margin으로 가지고 있어 `×`가 실제 입력창 사각형보다 아래쪽에 정렬됐습니다.

v1.12.0은 공통 behavior가 TextBox의 전체 외부 margin을 반영해 실제 입력창 bounds에 맞춰 `×`를 정렬합니다.

## 김태영 PC 진단

메인 헤더 좌측 프로필 이미지를 클릭하면 전용 진단을 시작할 수 있습니다.

```text
프로필 이미지 클릭
→ “김태영 본인이 맞습니까?”
→ 예
→ 로컬 진단
→ 바탕화면 ZIP 생성
→ hyune4784@naver.com 으로 전송 요청 안내
```

진단은 자동 전송하지 않습니다.

진단 ZIP은 Scanner/화면 캡처에 영향을 줄 수 있는 다음 evidence를 폭넓게 수집합니다.

- Windows/runtime/display 구성, resolution/bpp/DPI
- GPU/driver/monitor 상태
- dxdiag의 HDR support / display color space / color primaries / luminance / current mode
- Discord/OBS/NVIDIA/AMD/RTSS/Game Bar 등 allowlist capture/overlay process의 존재/버전
- Scanner settings/runtime/catalog 상태
- 기존 Scanner support/performance/log bundle
- 각 display의 screen copy와 RGB/휘도/clipping 통계
- Tarkov가 실행 중이면 exact client screen-copy와 PrintWindow 비교 + 동일 통계

진단 목적과 무관한 다음 정보는 수집하지 않습니다.

- Windows 사용자 이름
- 컴퓨터 이름
- IP/MAC
- 네트워크 목록
- token/password/credential
- 환경변수 전체 dump
- 임의의 전체 process 목록
- application install path

단, 화면 캡처 PNG에는 진단 실행 당시 실제 화면에 표시되는 내용이 포함될 수 있으므로 실행 전 확인창과 ZIP README에서 이를 알립니다.

한 optional probe의 실패는 전체 진단을 중단하지 않고 `probe-errors.txt`에 기록합니다.

## 회귀 검증

Release 후보는 다음을 통과해야 합니다.

- Desktop Release build
- deterministic test suite
- Quest past-stage task-pool regression tests
- search clear / diagnostic source-contract tests
- Windows x64 self-contained single-file publish
- actual published EXE Product UI / Map / Factory / MiniMap / Scanner smoke
- graceful shutdown / Shutdown Race CI
- Documentation Consistency
- package/root cleanliness 및 SHA-256 manifest audit

## 호환성

- user DB schema: unchanged
- Game Content DB schema: unchanged
- Scanner display settings schema: unchanged
- Scanner catalog schema: unchanged
- Map donor pin: unchanged
- Scanner recognition threshold/matcher/candidate-cap semantics: unchanged

김태영 실제 PC에서 생성된 ZIP은 public release 자동화와 별도의 실사용 evidence입니다. 해당 evidence를 받은 뒤 PC 환경 문제인지 Scanner 호환성 문제인지 먼저 분리하고, 한 PC의 샘플만으로 Scanner global recognition threshold를 완화하지 않습니다.
