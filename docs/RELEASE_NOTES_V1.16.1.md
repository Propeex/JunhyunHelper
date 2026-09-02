# 준현 헬퍼 v1.16.1

상태: **RELEASE CANDIDATE / VALIDATION IN PROGRESS**  
기준일: **2026-09-02 KST**

v1.16.1은 새 사용자 기능을 추가하지 않고, 공개 안정판 v1.16.0의 **저장 복구력·비동기 상태 일관성·릴리즈 검증 신뢰성**을 보강하는 PATCH 유지보수 릴리즈다.

## Farming Guide 저장 복구 보강

`farming-guide.json`은 원자적 쓰기와 백업 복구를 이미 사용하고 있었지만, JSON 문법 자체는 정상인 채 내부 필드가 `null`이거나 일부 구조만 손상된 경우에는 백업 복구 대상이 되지 않으면서 후속 정규화 과정에서 예외가 발생할 수 있었다.

v1.16.1에서는 다음을 보강했다.

- nullable profile/preset/snapshot/lock/fixed-equipment 컬렉션을 정상 기본값으로 복구한다.
- Item ID나 instance ID가 없는 구조적으로 사용할 수 없는 항목은 버리고, 정상 장비·프리셋·stored item은 최대한 보존한다.
- attachment / armor plate 하위 구조도 재귀적으로 정상화한다.
- 저장된 stack quantity는 최소 1개로 정규화한다.
- Strength 설정은 기존 제품 계약 범위로 정규화한다.
- legacy dogtag persistence 제거 계약은 그대로 유지한다.
- 부분 손상 문서를 로드한 뒤 다시 저장·재로드해도 정상 상태가 유지되는 결정적 회귀 테스트를 추가했다.

이 변경은 파밍 가이드의 판단 규칙이나 사용자 조작 방식을 바꾸지 않고, 기존 상태 파일이 비정상적으로 부분 손상되었을 때의 복구 경로만 강화한다.

## 프로필 전환 중 자동 콘텐츠 갱신 일관성

시작 시 구형 콘텐츠 schema를 자동 갱신하는 경로는 비동기 cache read/update를 수행한다. 이전 구현에서는 작업을 시작한 game mode는 보존했지만, 비동기 작업 사이에 사용자가 다른 프로필로 전환했을 때 완료 결과가 새 active profile에 적용될 수 있는 stale continuation 여지가 있었다.

v1.16.1에서는:

- 자동 schema migration 시작 시 `ProfileId + GameMode`를 함께 고정한다.
- 각 주요 비동기 경계 뒤에 현재 active profile이 여전히 동일한지 다시 확인한다.
- 다른 프로필로 바뀌었다면 이전 작업은 busy state나 content/workspace를 새 프로필에 적용하지 않는다.
- 이 identity guard를 유지하는 maintenance regression contract를 추가했다.

## 제품/UI 검토

이번 maintenance pass에서는 코드만 보는 방식이 아니라 기존 제품 검증 계약과 실제 published WPF smoke 범위를 함께 점검했다.

검토 범위에는 다음이 포함된다.

- MainWindow profile/data-update/lifecycle
- Farming Guide 저장·중첩 수납공간·Workbench·수량·무게
- Scanner runtime/coordinator/settings/UI-state persistence
- Map/MiniMap product settings와 window state
- atomic storage/content activation/image cache
- service ownership/disposal과 updater
- rendered Scanner / Ammo / Farming Guide / Quest / Map / overlay surface
- published EXE startup / Product UI / Map / graceful shutdown
- shutdown race와 release package/checksum

현재 저장소와 runtime smoke 증거에서 기존 제품 동작을 바꿀 정도의 추가 UI 결함은 재현되지 않았다. 특히 최소 메인 창 크기의 전체 containment를 더 명시적으로 측정하는 자동 검증은 향후 강화 여지가 있지만, 재현되지 않은 잘림을 추측으로 고치기 위해 창 크기나 레이아웃을 임의 변경하지 않았다.

## 검증 목표

릴리즈 전 다음을 모두 통과해야 한다.

```text
Release build
612 deterministic tests / 0 failed / 0 skipped
self-contained win-x64 publish
published EXE Product UI + Map/MiniMap + Farming Guide runtime smoke
graceful shutdown / Shutdown Race
package/checksum verification
PR CI / exact-main CI / Documentation Consistency
public v1.16.1 tag / release / assets verification
```

최종 exact product source, CI run, release/tag/asset 정보는 공개 릴리즈 검증 후 `docs/PROJECT_STATE.json`, `docs/STATE.md`, `docs/CURRENT_STATE.md`에 기록한다.
