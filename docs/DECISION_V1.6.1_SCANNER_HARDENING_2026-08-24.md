# DECISION — v1.6.1 Scanner hardening

상태: `VERIFIED / READY FOR MERGE & RELEASE`

기준일: 2026-08-24

## 1. 사용자 문제

v1.6.0 실사용에서 다음 결함이 확인되었다.

1. 일반 게임 데이터 업데이트는 성공하지만 Scanner catalog 최신화가 실패하여 기존 정상 Scanner cache를 유지했다는 modal이 반복될 수 있다.
2. `Scanner 고급` 고정 높이 dialog에서 `교정 데이터 관리` 버튼이 잘릴 수 있다.
3. Scanner 인식 로그는 용량 회전만 있고 시간 기반 자동 만료가 없어 오래된 로그가 남을 수 있다.
4. 위 결함을 고치면서 현재 기능의 안정성·정확성·성능 완성도를 높인다. 단, 실제 reviewed Ground Truth가 없는 recognition threshold/candidate-budget 변경은 하지 않는다.

`docs/VERSIONING.md`에 따라 새 사용자 기능이 아니라 기존 기능의 버그 수정/안정성/성능/UX 보완이므로 목표 버전은 **v1.6.1 PATCH**다.

## 2. Scanner catalog update resilience

Scanner identity authority와 atomic cache safety는 유지한다.

필수 데이터와 보조 presentation 데이터를 실패 경계에서 분리한다.

필수:

- current GameMode `items`
- current GameMode `items_ko`

보조/fail-soft:

- `items_en`
- trader display-name endpoints

정책:

- 필수 request는 bounded retry와 request-local timeout을 사용한다.
- 보조 request timeout/HTTP/JSON failure가 healthy Korean identity catalog refresh를 실패시키지 않는다.
- caller cancellation / application shutdown은 즉시 존중한다.
- candidate catalog는 기존과 동일하게 최소 health 검증, atomic write, read-back 검증을 통과한 뒤에만 active memory를 교체한다.
- refresh가 최종 실패해도 same-mode healthy existing catalog가 있으면 그대로 유지한다.
- 다른 GameMode identity를 fallback으로 사용하지 않는다.
- scan-time network 금지는 그대로 유지한다. 이 network work는 명시적 data-update/catalog-sync 단계에서만 실행한다.

## 3. Safe fallback UX

일반 Game Content 성공 + Scanner refresh 실패 + same-mode healthy Scanner cache 존재 상태는 제품 사용을 막는 fatal error가 아니다.

따라서:

- healthy fallback은 blocking MessageBox를 띄우지 않는다.
- 사용자에게는 main status로 `Scanner 기존 정상 캐시 유지` 상태를 남긴다.
- 실패 outcome은 diagnostics에 보존한다.
- usable Scanner cache 자체가 없는 경우에는 기존 warning modal을 유지한다.

즉 실패를 숨기지 않되, 정상적으로 복구 가능한 상태를 매 업데이트마다 사용자 작업을 막는 modal로 취급하지 않는다.

## 4. Scanner 고급 dialog layout

고정 window height와 `*` row 조합을 제거한다.

- compact content는 `Auto` row로 측정한다.
- dialog 높이는 실제 content 기준으로 결정한다.
- Windows DPI/font/chrome 차이에서도 세 기능 버튼과 닫기 버튼이 서로 clip/overlap하지 않아야 한다.
- 창은 계속 resize 불가의 단순 고급 dialog로 유지한다.

## 5. Scanner log retention

Scanner recognition log는 reviewed Ground Truth와 다른 ephemeral diagnostic/user-activity 데이터다.

정책:

- Scanner log 보존기간: **7일**
- 기존 size bound도 유지한다.
- startup/first hydration에서 오래된 log line을 best-effort로 자동 정리한다.
- UI activity history도 같은 7일 범위만 복원한다.
- malformed/legacy log line은 Ground Truth가 아니므로 자동 retention 정리에서 제거할 수 있다.
- `ScannerDiagnosticDataset`의 reviewed Ground Truth는 이 정책의 대상이 아니다.
- automatic unreviewed diagnostic Case의 기존 30일/300건/512 MiB retention도 별도로 유지한다.

## 6. Recognition safety — 변경 금지

이번 PATCH에서는 다음을 변경하지 않는다.

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
```

또한 다음 계약을 유지한다.

- false positive보다 miss 선호
- red close-X + magnifier semantic evidence 필수
- official current Korean catalog가 identity authority
- production OCR field = item-name only
- automatic global OCR forced substitution 금지
- cross-frame OCR cache 금지
- game memory read / DLL injection / packet interception 금지

실제 Tarkov reviewed Ground Truth가 없는 상태에서 recognition threshold를 낮추는 것을 정확도 개선으로 간주하지 않는다.

## 7. 이번 PATCH의 품질 개선 범위

정확성:

- 최신 Korean Scanner identity/market catalog가 정상적으로 갱신될 확률을 높인다.
- optional localization/presentation 장애를 identity failure로 오분류하지 않는다.
- existing atomic validation/read-back/fail-closed contract를 유지한다.

안정성:

- request별 timeout/retry로 transient network failure를 흡수한다.
- mode/cancellation/cache replacement ordering을 유지한다.
- UI layout이 DPI 차이에 의존해 잘리지 않도록 한다.
- 로그 디스크 수명주기를 명시적으로 제한한다.

성능:

- retry는 data-update 시에만 bounded하게 수행하고 scan-time hot path에는 추가 work를 넣지 않는다.
- recognition threshold를 성능 목적으로 완화하지 않는다.
- Scanner item search 결과 행은 full mapped presentation을 반복 계산하지 않고 필요한 icon/name/wiki 데이터만 조회한다.
- telemetry evidence 없는 broad Scanner refactor는 하지 않는다.

## 8. 검증 gate

최소 검증:

- required catalog endpoint transient failure 후 retry recovery
- optional English/trader endpoint HTTP/timeout/invalid JSON fail-soft
- mandatory refresh 최종 실패 시 same-mode healthy cache preservation
- different-mode cache leakage 없음
- Scanner mapped market/dimension tests 유지
- Scanner advanced dialog rendered smoke에서 clipping 없음
- 7일 Scanner log retention + size rotation + Ground Truth separation
- full automated tests 0 failed / 0 skipped
- Windows x64 publish
- Product UI / Scanner / Mini Scanner smoke
- Main Map / Factory / MiniMap smoke
- graceful shutdown / clean portable root

최종 검증에서 회귀가 발견되면 v1.6.1 release를 진행하지 않는다.

## 9. Release-candidate 검증 기록

2026-08-24 PR #176의 release-candidate CI run `#1803`에서 위 gate를 전부 통과했다.

- Release build: 성공, error 0
- Automated tests: **319 passed / 0 failed / 0 skipped**
- Windows x64 self-contained single-file publish: 성공
- Published identity: `v1.6.1`, ProductVersion `1.6.1+40feb605033d3ea67c1db3bd6d0f1354b35ff28f`
- Startup + rendered Product UI smoke: 성공
- Scanner Advanced 실제 렌더링 clipping/overlap smoke: 성공
- Main Map / Factory / MiniMap smoke: 성공
- graceful shutdown + portable root cleanliness smoke: 성공
- stable package: `Junhyun-Helper.zip`
- package size: `80,429,131 bytes`
- package SHA-256: `95e7c7b7c3ae53dc21950cebdb7351901400d360f98863c691c62ef9d9c07b65`
- CI artifact upload: 성공 (`JunhyunHelper-win-x64`, artifact ID `9516185946`)

현재 상태는 구현 완료 및 release-candidate 검증 완료다. PR 최종 문서 커밋이 동일 gate를 다시 통과한 뒤 `main` 병합 및 v1.6.1 정식 릴리즈를 진행한다.
