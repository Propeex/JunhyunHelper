# 준현 헬퍼 v1.14.1

## Farming Guide exact storage-layout fail-closed 보강

v1.14.0에서 추가한 product-owned exact multi-grid layout은 current Tarkov storage mechanics와 구조가 정확히 일치할 때만 사용해야 한다.

v1.14.0 공개 이후 release-closure review에서 `FarmingGuideStorageVisualLayoutResolver`가 layout identity, grid count, positive dimensions, resulting non-overlap은 검증하지만 **각 grid index의 expected width/height signature 자체는 비교하지 않는 구현 누락**이 확인됐다.

따라서 Tarkov가 grid의 가로 또는 세로 크기만 변경하고 기존 exact 좌표가 우연히 서로 겹치지 않는 경우 stale visual coordinates가 계속 적용될 수 있었다.

v1.14.1은 이 회귀를 수정한다.

- product-owned exact profile에 grid별 expected width/height signature를 함께 보존한다.
- exact layout 적용 전 각 live grid index의 width/height가 expected signature와 정확히 일치하는지 검증한다.
- 단 하나의 dimension mismatch라도 exact layout을 거부한다.
- mismatch 시 storage legality/filter/item footprint는 current Game Content를 그대로 사용하며 presentation만 finite compact fallback을 사용한다.
- 기존 non-overlap 검증은 profile corruption에 대한 secondary defense로 유지한다.
- A18 / ANA Tactical M1 / 현재 product-owned exact profile의 정상 signature와 dimension-drift 거부를 deterministic regression으로 고정한다.

## 변경하지 않는 계약

- v1.14.0 recursive assembly / inline compatible-item picker
- weapon / helmet / armor attachment compatibility
- nested storage drag/drop 및 persistence
- Farming Guide user-state schema v1
- Game Content write schema v10 / readable v3-v10
- Scanner / Map / Quest / Hideout / Ammo 동작

## 릴리즈 검증

최종 공개 v1.14.1은 다음 gate를 통과한 exact-main artifact만 사용한다.

- deterministic tests
- Windows Release build
- self-contained win-x64 publish
- actual published EXE Product UI / Farming Guide / Map smoke
- exact storage layout / drop-target runtime smoke
- graceful shutdown
- Shutdown Race
- package/checksum verification
- Documentation Consistency
- public tag/source/assets/latest readback

공개 exact source, test count, CI run, release ID, asset bytes/hash는 release 완료 후 canonical 상태 문서에 기록한다.
