# 준현 헬퍼 v1.17.4

## 변경 사항

- Mini Scanner의 `필요 아이템 개수` 표시를 FIR 필요량과 그 외 현재 부족량으로 분리했습니다.
- 표시 형식은 항상 `<FIR 필요량>(인레이드) + <그 외 필요량>개`입니다.
- 어느 한쪽이 0이어도 생략하지 않습니다.
  - FIR 3 / 그 외 4 → `3(인레이드) + 4개`
  - FIR 0 / 그 외 4 → `0(인레이드) + 4개`
  - FIR 4 / 그 외 0 → `4(인레이드) + 0개`
- 이 값은 기존 Items planner의 `RemainingTotal` 및 `RemainingFir`에서 파생합니다.
- Quest/Hideout 필요량 계산, FIR 의미, inventory accounting, Scanner recognition, catalog, persistence, Mini Scanner 정보 순서/레이아웃은 변경하지 않았습니다.

## 기능 후보 검증

Functional candidate head:

`e2477ffd8df3adbc1b9742c35a500944e0d1595f`

Passed:

- CI `33938858432`
- Shutdown Race `33938858490`
- Documentation Consistency `33938858443`
- **504 passed / 0 failed / 0 skipped**
- Windows Release build
- win-x64 self-contained publish
- actual published EXE Product UI / full Map/Factory/MiniMap / Scanner smoke
- graceful shutdown
- release package/checksum validation
