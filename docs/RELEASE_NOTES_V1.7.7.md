# 준현 헬퍼 v1.7.7

## Scanner 교정 데이터 폭증 및 단축키 설정 일관성 수정

v1.7.7은 v1.7.6 공개 이후 실사용에서 확인된 Scanner 유지보수 결함을 수정하는 PATCH 릴리즈입니다.

이번 변경은 Scanner의 인식 알고리즘이나 정확도 기준을 변경하지 않습니다. 수정 범위는 **진단/교정 데이터 저장 정책, 사용자 로그 가시성, Scanner·Map 단축키 입력 계약**입니다.

## 확인된 문제

사용자가 전달한 Scanner 진단 자료를 분석한 결과, 포함된 51개 Case는 모두 `UNREVIEWED / automatic_sample`이었습니다. 연속 Scanner가 상세보기 창이 없거나 인식에 실패한 프레임까지 PNG를 포함한 durable diagnostic Case로 자동 저장할 수 있었고, 같은 실패가 주기적으로 반복되면서 교정 데이터가 7GB 이상 증가할 수 있었습니다.

이는 사용자가 직접 검토해 만든 Ground Truth와 성격이 다른 런타임 관측 자료가 같은 durable dataset에 과도하게 축적된 설계 결함입니다.

또한 Scanner 화면의 인식 로그에는 동일 실패가 반복 표시되어 필요한 결과를 찾기 어려웠습니다.

단축키 설정도 일관되지 않았습니다. Scanner는 Ctrl/Alt/Shift 중 하나 이상을 강제했지만 Map은 반대로 modifier 조합을 지원하지 않았습니다.

## 수정 내용

### 1. 교정/Ground Truth 저장은 사용자 선택형으로 변경

- 정상 연속 Scanner는 실패 프레임을 durable diagnostic Case로 자동 저장하지 않습니다.
- 상세보기 창 미탐지, header lock 실패, OCR/matcher 실패만으로 교정 데이터가 생성되지 않습니다.
- 최신 exact diagnostic frame은 현재 사용자 교정을 위해 메모리에만 유지합니다.
- 사용자가 교정 창에서 명시적으로 저장한 reviewed Case만 durable Ground Truth가 됩니다.

따라서 Scanner를 장시간 켜 두어도 정상 감시 자체가 이미지 기반 교정 dataset을 계속 증가시키지 않습니다.

### 2. 기존 자동 Case 안전 정리

이전 버전에서 이미 생성된 자료는 다음 조건을 모두 증명할 수 있을 때만 background maintenance에서 정리합니다.

- `retention = automatic_sample`
- `review_status = unreviewed`
- 최근 쓰기 중인 Case가 아님
- 삭제 직전 재확인에서도 동일한 자동/미검토 상태임

다음 자료는 자동 삭제하지 않습니다.

- 사용자가 검토한 Ground Truth
- 수동 저장 자료
- metadata가 손상되거나 소유/검토 상태를 확신할 수 없는 Case
- 정리 도중 상태가 변경된 Case

즉 기존 7GB 문제의 원인이 된 legacy 자동 자료를 제거하면서, 사용자 교정 결과는 fail-closed 방식으로 보호합니다.

### 3. Scanner 사용자 로그 중복 억제

- 동일한 실패는 사용자 활동 목록에서 30초 동안 반복 표시하지 않습니다.
- 성공 결과와 의미가 다른 실패는 그대로 표시합니다.
- 지원 진단용 `scanner.log`는 기존과 같이 작은 회전 파일로 유지되어 원인 분석 자료를 잃지 않습니다.

### 4. Scanner·Map 단축키 규칙 통일

두 기능 모두 다음 형식을 지원합니다.

```text
일반 키 단독
Ctrl + 키
Alt + 키
Shift + 키
Ctrl / Alt / Shift의 임의 조합 + 키
```

Windows 키 조합은 지원하지 않습니다.

기존 Map 단축키 설정은 modifier 없는 기존 binding으로 자동 호환됩니다.

Map의 `NumPad 0~5` 단독 입력은 기존 직접 층 선택 기능을 유지합니다. 대신 `Ctrl+NumPad1`, `Alt+NumPad2`처럼 modifier가 붙은 NumPad 입력은 일반 Map 단축키로 지정할 수 있습니다.

## Scanner 인식 안전 계약

이번 PATCH에서 다음 값과 의미는 변경하지 않습니다.

- structural floor `0.34`
- `HEADER_FRAME_LOCKED` floor `0.68`
- continuous candidate cap `8`
- one-shot candidate cap `12`
- 기존 deep OCR candidate limit
- catalog matcher acceptance
- targeted/full-catalog visual acceptance
- 200ms continuous observation target
- false positive보다 miss 우선
- stale Item ID를 새 identity proof로 사용하지 않음
- cross-frame OCR/visual identity cache 금지
- Item ID 확정 전 가격/필요 개수를 identity proof로 사용하지 않음
- scan-time network 없음
- game memory reading, DLL injection, packet interception, Tarkov process hook 없음

v1.7.6에서 해결한 Scanner 성능 경로도 변경하지 않습니다.

## 릴리즈 검증

v1.7.7은 최종 source HEAD에서 다음 gate를 모두 통과한 경우에만 공개 stable로 병합·배포합니다.

- Windows Desktop Release build
- 전체 자동 테스트
- Windows x64 self-contained single-file publish
- Product UI / Scanner / Map / Factory / MiniMap smoke
- 정상 종료 및 portable-root 검증
- release package 및 SHA-256 manifest 검증
- 공개 release source/tag/asset identity readback
