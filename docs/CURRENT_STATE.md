# CURRENT STATE

> 기계 판독 가능한 현재 사실값은 `docs/PROJECT_STATE.json`, 진행 중 작업은 `docs/ACTIVE_WORK.md`가 기준입니다.

기준일: **2026-09-05 KST**  
상태: **v1.17.4 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.17.4
exact product source/tag target:
2297a27332069e18ade56c53931002f7a4728338
validated PR head:
5ba3c504e4da8b8758b685715498437d3a7862b2
merge PR: #295
PR CI / Shutdown / Docs:
33939249250 / 33939249290 / 33939249230 — SUCCESS
exact-main CI / Shutdown / Docs:
33939474734 / 33939474738 / 33939474753 — SUCCESS
Release workflow:
33939616674 — SUCCESS
release id: 383108819
504 passed / 0 failed / 0 skipped
```

## v1.17.4 change

Mini Scanner `필요 아이템 개수`는 현재 부족량을 다음과 같이 분리 표시합니다.

`RemainingFir(인레이드) + (RemainingTotal - RemainingFir)개`

0인 component도 생략하지 않습니다.

이번 PATCH는 표시 변경만 수행했습니다. Items planner, Quest/Hideout requirement 의미, FIR accounting, Scanner recognition/catalog/persistence, Mini Scanner layout/order는 유지됩니다.

## 검증

- Windows Release build
- 504 / 504 deterministic tests
- win-x64 self-contained publish
- ProductVersion `1.17.4+2297a27332069e18ade56c53931002f7a4728338`
- actual Product UI / full Map/Factory/MiniMap / Scanner runtime smoke
- graceful shutdown / Shutdown Race
- clean portable root
- package/checksum
- Documentation Consistency
- public tag/release/assets digest readback

실제 사용자 PC/Tarkov 실플레이 검증은 별도 `PENDING` evidence입니다.
