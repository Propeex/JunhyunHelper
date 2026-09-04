# 준현 헬퍼 v1.17.1

## 변경 사항

- 파밍 가이드를 제품에서 완전히 제거했습니다.
- 메인 파밍 가이드 탭, loadout/inventory editor, raid-session advisor, global packing/repacking, 잠금/무게/수량 입력과 전용 저장 상태를 제거했습니다.
- Scanner의 파밍 가이드 표시 항목, 수락 단축키, Mini Scanner 지시/수량 입력 브리지와 simulated Farming Guide scan path를 제거했습니다.
- 파밍 가이드 전용 Core/Desktop/Infrastructure 코드와 Game Content 메타데이터 import 계약, 전용 테스트/스모크를 제거했습니다.
- 기존 `farming-guide.json`은 더 이상 읽거나 쓰지 않지만 자동 삭제하지 않습니다.
- Quest, Hideout, Items, Ammo, Map/MiniMap 및 Scanner의 독립 기능은 유지합니다.

## 검증 목표

- deterministic tests
- Windows Release build
- win-x64 self-contained publish
- actual published EXE Product UI / Map / Scanner smoke
- graceful shutdown / Shutdown Race
- package/checksum validation
- PR / exact-main / release identity verification
