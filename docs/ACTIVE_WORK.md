# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-09-01 KST**

## Goal

준현 헬퍼 v1.15.0에서 파밍 가이드를 레이드 중 실시간 의사결정 도우미로 확장한다.

## Base / Working branch

- Base main: `72cd197458f929bc0048b2c808c426f3444d0aa8`
- Working branch: `feature/v1.15.0-farming-guide-raid-session`
- PR: not created yet

## Confirmed scope

- 파밍 가이드 탭에 `레이드 시작 / 레이드 종료` 전환 버튼을 추가한다.
- 레이드 시작 시 현재 선택된 프리셋/작업 상태를 기준으로 독립적인 raid-session snapshot을 생성한다.
- 레이드 종료 시 raid-session 변경을 폐기하고 시작 시점의 프리셋/작업 상태로 복귀한다.
- 레이드 중 사용자가 파밍 가이드 UI에서 아이템·장비를 임의 변경하면 이후 판단은 즉시 새 상태를 기준으로 한다.
- 칸/아이템/장비 위에 커서를 두고 `F`를 누르면 잠금/해제한다. 빈 칸 잠금은 자동 배치 로직이 사용할 수 없는 예약 공간이다.
- 미니 스캐너 설정에 파밍 가이드 표시 옵션을 추가한다.
- 스캔된 아이템에 대해 파밍 가이드가 지시를 만들고 `수락 [단축키]`를 표시한다.
- 사용자가 수락 단축키를 눌러야 파밍 가이드 상태에 결과를 commit한다. 적용 후 `수락 완료` 피드백을 표시한다.
- 미확정 지시는 동시에 하나만 유지하고, 인벤토리/장비/잠금 상태가 바뀌면 stale 지시는 무효화한다.
- 파밍 가이드 검색 결과 아이템 위에서 `T`를 누르면 실제 스캔과 동일한 입력 경로로 테스트 스캔을 발생시킨다.
- 정책/배치/세션/UI/스캐너 어댑터를 분리해 추후 파밍 판단 로직을 교체하기 쉽게 유지한다.

## Completed

- v1.14.1 public stable / ACTIVE_WORK NONE 상태 복구.
- 기존 Farming Guide state, drag/drop, nested storage, scanner display settings/hotkey/mini overlay 구조 확인.
- v1.15.0 작업 브랜치 생성.

## Current step

Core raid-session / lock / pending-instruction 계약과 Desktop 통합 지점을 구현한다.

## Remaining

- Core raid session / lock / recommendation models and deterministic tests
- Farming Guide raid button / F lock / T simulated scan UI
- scanner display settings + mini overlay farming-guide instruction presentation
- configurable accept hotkey and stale-instruction guard
- integration/published-runtime smoke as appropriate
- PRODUCT / DECISIONS / ARCHITECTURE / DEVELOPER_REFERENCE / PROJECT_STATE documentation update
- PR / CI / exact-main verification
- v1.15.0 release and public asset verification
