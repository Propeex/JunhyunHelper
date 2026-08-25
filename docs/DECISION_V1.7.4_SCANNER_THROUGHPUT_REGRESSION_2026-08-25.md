# Decision — v1.7.4 Scanner throughput regression

기준일: 2026-08-25
상태: **IMPLEMENTED / RELEASE VERIFICATION PENDING**

## 사용자 관찰

v1.7.3 정식 릴리즈 이후 실제 Tarkov에서 Item identity 정확도는 양호하지만 Item 인식 완료까지의 체감 시간이 크게 느려졌다.

## 조사 결과

### 로그 누적

Scanner runtime log는 다음과 같이 bounded되어 있다.

```text
scanner.log max = 2 MiB
rotation = scanner.log + scanner.log.1
retention = 7 days
recent user activities = 60
```

따라서 오래된 로그가 무한히 쌓이면서 매 scan 시간이 선형적으로 증가하는 구조는 아니다. 다만 diagnostic write는 동기 file append이므로 scan 빈도가 높아지면 부수적인 I/O 부담은 증가할 수 있다.

### v1.7.3 cadence 회귀

v1.7.3은 continuous observation target을 350 ms에서 200 ms로 줄였다. 기존 loop는 플랫폼 `PeriodicTimer`를 사용한다.

scan cycle 자체가 200 ms보다 오래 걸리면 다음 timer tick이 이미 pending 상태가 될 수 있고, loop가 이를 즉시 소비하면서 다음 capture/detection cycle을 거의 쉬지 않고 시작할 수 있다.

실사용에서는 이 상태가 CPU/capture pressure를 높이고 OCR/semantic work 및 UI scheduling과 경쟁하여, 더 자주 보려고 한 변경이 실제 Item recognition throughput을 오히려 낮출 수 있다.

## 결정

200 ms 관측 목표는 유지하되 timer backlog는 재생하지 않는다.

```text
cycle < 200 ms
→ 200 ms start-to-start cadence를 맞추도록 남은 시간 대기

cycle >= 200 ms
→ 밀린 tick 즉시 실행 금지
→ 최소 25 ms cooperative yield
→ 다음 fresh observation
```

이 pacing은 `ScannerObservationPacingPolicy`의 deterministic Core policy로 분리하고 단위 테스트한다.

## 유지하는 v1.7.3 개선

- adaptive semantic retry `250 / 500 / 800 / 1200 ms`
- direct BGRA Windows OCR transport
- current title pixels 기반 identity 판단
- 기존 full detector와 semantic header gate

## 변경하지 않는 인식 계약

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
```

다음도 변경하지 않는다.

- candidate ranking/selection semantics
- normal/deep OCR acceptance semantics
- catalog matcher confidence/ambiguity rule
- Tarkov-font visual corroboration/recovery acceptance
- false positive보다 miss 우선
- cross-frame OCR identity cache 금지
- scan-time network / game memory / DLL injection / packet interception 금지

## 검증 gate

릴리즈 전 반드시 다음을 모두 통과한다.

- Windows Desktop Release build
- full automated test suite
- Windows x64 self-contained publish
- Product UI / Scanner / Mini Scanner / Map / Factory / MiniMap smoke
- release ZIP structure/hash verification
- merged exact `main` CI 재검증
- public v1.7.4 release asset readback

실제 Tarkov latency 개선 여부의 최종 판정은 사용자의 v1.7.4 실사용으로 확인한다. synthetic CI smoke 성공만으로 실제 게임 latency 개선을 과장하지 않는다.
