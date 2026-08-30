# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-30 KST**

## Goal

v1.11.0 실사용 직후 확인된 UI/사용성 회귀 3건을 수정하고, 직전 v1.11.0 변경 범위를 포함해 전체 제품 유지보수 점검을 수행한 뒤 **v1.11.1** PATCH 릴리즈로 검증·배포한다.

## Base / branch

```text
base main: cbb7ed9c998bde8ece0eceaf4197c6811e2da5ce
public stable: v1.11.0
working branch: fix/v1.11.1-scanner-search-correction-feedback-2026-08-30
PR: opening next
target version: v1.11.1
latest checkpoint: 0e385f028aeef0e6c7498513b6cb06e198a8d54c
```

`docs/VERSIONING.md` 기준으로 이번 범위는 새 제품 능력 추가가 아니라 기존 Scanner/검색/교정 저장 UX의 수정·보완이므로 PATCH 릴리즈로 분류한다.

## Confirmed scope

1. Scanner/Mini Scanner의 기존 탄약 pickup 판단(`주워야 함`)을 Scanner 설정에서 표시/숨김 및 정보 순서 대상으로 노출해 실제 테스트 가능하게 한다.
2. Items 탭과 Hideout 탭 검색창에 다른 검색 UI와 동일한 검색어 지우기 `X` 동작을 추가한다.
3. `교정 데이터 추가` 전역 단축키로 Saved Case 저장 성공 시 Mini Scanner에서 `저장 완료` 피드백을 보여 사용자가 성공 여부를 즉시 확인할 수 있게 한다.
4. 직전 v1.11.0 변경과 현재 주요 제품 기능을 함께 점검해 명확한 결함/회귀가 있으면 범위에 비례해 수정하고 회귀 검증을 강화한다. 근거 없는 대규모 리팩터링은 하지 않는다.

## Root cause / implementation

- **탄약 판단 설정 누락**: v1.11.0은 `AmmoPickupText`를 Mini Scanner 정보 순서 밖에서 무조건 마지막에 추가했다. 따라서 settings schema/visibility/order에 필드가 없었다. v1.11.1은 display settings schema v9에 `ammo_pickup`을 추가하고 기존 v8 사용자에게 현재 visible 동작을 기본 보존하면서 표시/숨김 및 순서 저장 대상으로 승격한다.
- **Items/Hideout 검색 지우기 누락**: 두 검색창만 plain TextBox였고 Scanner 검색의 `×` clear UX를 공유하지 않았다. 공통 `SearchClearButtonInstaller`가 기존 TextBox/Search TextChanged 계약을 보존한 채 동일 clear/focus 동작을 추가한다.
- **교정 저장 성공 피드백 부재**: hotkey 저장 성공은 Scanner status event에만 게시되어 게임 중 Mini Scanner만 보는 사용자가 성공 여부를 즉시 확인하기 어려웠다. 저장 성공 뒤 Mini Scanner에 정확히 `저장 완료`를 2초간 표시하며 현재 아이템 snapshot은 교체하지 않는다. Mini Scanner가 닫혀 있어도 status-only 카드로 잠시 표시한다.

## Completed

- v1.11.0 public stable 및 canonical project memory 복구
- v1.11.1 maintenance branch 생성
- 사용자 요구사항/목표 버전 공식 체크포인트 기록
- Scanner display settings schema v9 + ammo pickup visibility/order 구현
- Scanner 설정 화면 `탄약 줍기 판단` 항목 연결
- Mini Scanner ammo 판단이 settings visibility/order를 실제 렌더에 적용하도록 수정
- Items/Hideout 공통 검색 `×` clear/focus 동작 구현
- correction hotkey save-success → Mini Scanner `저장 완료` transient feedback 구현
- 세 사용자 요구사항 source-level regression contracts 추가

## Current step

- PR/Windows CI로 Release build/test/published EXE 경로 검증
- v1.11.0 직전 변경과 현재 주요 제품 영역 regression/maintenance audit

## Remaining

- CI에서 드러나는 컴파일/runtime/contract 오류 수정
- audit 결과 중 근거 있는 추가 결함만 최소 수정
- v1.11.1 version / schema / release documentation 정리
- final PR CI green
- main merge + exact-main 검증
- v1.11.1 tag/release/assets public readback
- 공식 상태 문서 갱신 후 ACTIVE_WORK NONE
