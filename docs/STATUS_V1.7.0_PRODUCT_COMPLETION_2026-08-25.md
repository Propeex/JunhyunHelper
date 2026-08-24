# STATUS — v1.7.0 Product Completion Hardening

기준일: 2026-08-25
상태: **PUBLIC RELEASE VERIFIED / COMPLETE**

## 공식 릴리즈 기준선

```text
version: v1.7.0
exact product source/tag: 56e12342e3490fd0defa5f327a03d20d4f32b3a6
ProductVersion: 1.7.0+56e12342e3490fd0defa5f327a03d20d4f32b3a6
stable asset: Junhyun-Helper.zip
stable bytes: 80,443,318
stable SHA-256: 1c640c80bf6113176b885a47e19478666e27dbf584f872d1a8396886334f3418
tests: 348 passed / 0 failed / 0 skipped
public proof run: 32745399476
```

공개 stable/latest, anonymous asset redownload, SHA-256, ZIP layout, exact ProductVersion/FIRST_RUN, public-downloaded rendered Product UI/Map smoke, graceful shutdown을 모두 검증했다.

## 완료된 hardening

- Scanner recognition log → exact Case/current-frame correction
- 기존 Ground Truth + Scanner log ZIP export 재사용
- Game Content update request timeout/retry, completeness/integrity, candidate read-back, atomic activation, transaction serialization
- Scanner market cache last-known-good / trader-Flea-slot coverage collapse protection
- Item ID 이후 metadata/market/needed 동일-ID join과 교차오염 regression coverage
- Scanner Advanced clipping 방지 / runtime log 7일 retention 유지
- project version / FIRST_RUN / release notes source identity CI guard
- exact-source + anonymous public release proof

## 다음 단계

제품 기능과 v1.7.0 release hardening은 완료되었다. Scanner는 **LIVE GROUND TRUTH MAINTENANCE** 단계로 돌아간다.

reviewed live evidence 없이 다음 값을 변경하지 않는다.

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
```

새 실사용 Case가 들어오면 실패 stage를 Ground Truth로 재현하고, 필요한 부분만 수정한 뒤 전체 reviewed replay에서 regression=0을 확인한다.
