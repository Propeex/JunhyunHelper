# 준현 헬퍼 v1.7.4

## Scanner 처리량 회귀 수정

v1.7.3에서 continuous Scanner의 관측 목표를 350 ms에서 200 ms로 줄였지만, 플랫폼 `PeriodicTimer`는 scan cycle이 목표 시간을 초과하면 밀린 tick을 다음 loop에서 즉시 소비할 수 있다. 실제 Tarkov 환경에서 capture/detection이 200 ms budget을 넘는 경우 거의 쉬지 않는 capture loop가 형성되어 CPU/화면 캡처 압력을 높이고, 결과적으로 OCR/semantic identification까지 도달하는 체감 시간이 오히려 길어질 수 있었다.

v1.7.4는 200 ms 목표 자체를 폐기하지 않고 **non-backlogging pacing**으로 교체한다.

- cycle이 200 ms보다 빨리 끝나면 남은 시간만 기다려 기존 200 ms start-to-start 목표를 유지한다.
- cycle이 200 ms를 이미 초과했으면 밀린 tick을 즉시 재생하지 않고 최소 25 ms를 양보한 뒤 다음 관측을 시작한다.
- 이 정책은 deterministic Core policy로 분리하고 회귀 테스트를 추가했다.

## 유지한 v1.7.3 개선

- 동일 안정 후보 semantic 실패 retry: `250 → 500 → 800 → 1200 ms`
- Windows OCR의 direct BGRA → `SoftwareBitmap` 전달
- 기존 full semantic gate / matcher / deep OCR / Tarkov-font visual recovery
- Mini Scanner sticky presentation contract

## 정확도 안전 계약 — 변경 없음

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
```

추가로 다음 원칙도 그대로 유지한다.

- false positive보다 miss 우선
- current official Korean Tarkov catalog가 Item identity authority
- cross-frame OCR 결과를 새 identity proof로 재사용하지 않음
- Item ID 확정 전 가격/필요 개수 등 mapped data를 identity evidence로 사용하지 않음
- scan-time network 없음
- game memory read / DLL injection / packet interception 없음

## 로그 관련 확인

Scanner runtime log는 2 MiB 단위로 회전하고, 7일 보존 정책과 최대 60개 사용자 activity feed를 사용한다. 따라서 로그가 무한히 누적되면서 scan 시간이 선형적으로 계속 증가하는 구조는 아니다. 사용자는 성능 문제 해결을 위해 Scanner 로그나 reviewed Ground Truth를 삭제할 필요가 없다.

## 사용자 데이터

프로필, 진행도, Scanner 설정, 교정 데이터와 reviewed Ground Truth는 기존 위치와 형식을 유지한다. v1.7.4 업데이트는 해당 사용자 데이터를 초기화하거나 덮어쓰지 않는다.
