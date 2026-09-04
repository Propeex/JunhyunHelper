# 준현 헬퍼 v1.17.2

## 변경 사항

- 새 사용자 기능이나 성능 최적화 없이 현재 제품의 불필요한 구현 잔재를 제거했습니다.
- MainWindow/Profile/Ammo/Quest/Hideout/Items/Scanner/Mini Scanner에서 숨은 구형 UI, runtime handler rebinding, proxy control, 도달 불가능한 경로와 중복 lifecycle을 정리했습니다.
- Ammo와 Scanner의 현재 UI 소유권을 XAML 또는 명시적 page lifecycle로 정리하고, published smoke는 실제 제품 경로를 검증만 하도록 유지했습니다.
- Scanner의 제거된 독립 설정/디버그/핫키 창과 Mini Scanner의 사용되지 않는 preview/position-edit 설정 경로를 제거하되 현재 OCR/진단/드래그 위치 저장 계약은 유지했습니다.
- updater/package의 오래된 전환기 compatibility fallback을 제거하고 현재 stable package/root 계약만 유지했습니다.
- release/schema 상태의 canonical source를 `docs/PROJECT_STATE.json`으로 정리하고 Documentation Consistency를 강화했습니다.
- 정리 과정에서 발견한 Items 정리 필요 표시 갱신 회귀를 수정했습니다.
- 제거된 과거 구조를 다시 요구하던 회귀 테스트를 현재 canonical 구조 검증으로 갱신했습니다.
- Quest, Hideout, Items, Ammo, Map/MiniMap, Scanner 인식/검색/교정/진단과 pinned Map donor 계약은 유지합니다.

## 검증 목표

- deterministic tests 488개 전체 통과
- Windows Release build
- win-x64 self-contained publish
- actual published EXE Product UI / Map / Scanner smoke
- graceful shutdown / Shutdown Race
- stable package/checksum validation
- PR / exact-main / public release identity verification
