# 준현 헬퍼 v1.17.4

## 변경 사항

- Mini Scanner의 `필요 아이템 개수` 표시를 FIR 필요량과 그 외 현재 부족량으로 분리했습니다.
- 표시 형식은 항상 `<FIR 필요량>(인레이드) + <그 외 필요량>개`입니다.
- 어느 한쪽이 0이어도 생략하지 않습니다.
  - `3(인레이드) + 4개`
  - `0(인레이드) + 4개`
  - `4(인레이드) + 0개`
- 기존 Items planner의 `RemainingTotal` / `RemainingFir`에서 표시값을 파생합니다.
- Quest/Hideout requirement calculation, FIR semantics, inventory accounting, Scanner recognition/catalog/persistence, Mini Scanner layout/order는 변경하지 않았습니다.

## 검증 완료

- final PR head: `5ba3c504e4da8b8758b685715498437d3a7862b2`
- exact product source: `2297a27332069e18ade56c53931002f7a4728338`
- 504 / 504 deterministic tests
- PR CI / Shutdown / Docs: `33939249250` / `33939249290` / `33939249230`
- exact-main CI / Shutdown / Docs: `33939474734` / `33939474738` / `33939474753`
- Release workflow: `33939616674`
- published EXE Product UI / full Map/Factory/MiniMap / Scanner smoke
- graceful shutdown / Shutdown Race
- package/checksum and public release asset digest verification
