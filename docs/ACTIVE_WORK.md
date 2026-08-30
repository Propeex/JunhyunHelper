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
PR: not opened yet
target version: v1.11.1
```

`docs/VERSIONING.md` 기준으로 이번 범위는 새 제품 능력 추가가 아니라 기존 Scanner/검색/교정 저장 UX의 수정·보완이므로 PATCH 릴리즈로 분류한다.

## Confirmed scope

1. Scanner/Mini Scanner의 기존 탄약 pickup 판단(`주워야 함`)을 Scanner 설정에서 표시/숨김 및 정보 순서 대상으로 노출해 실제 테스트 가능하게 한다.
2. Items 탭과 Hideout 탭 검색창에 다른 검색 UI와 동일한 검색어 지우기 `X` 동작을 추가한다.
3. `교정 데이터 추가` 전역 단축키로 Saved Case 저장 성공 시 Mini Scanner에서 `저장 완료` 피드백을 보여 사용자가 성공 여부를 즉시 확인할 수 있게 한다.
4. 직전 v1.11.0 변경과 현재 주요 제품 기능을 함께 점검해 명확한 결함/회귀가 있으면 범위에 비례해 수정하고 회귀 검증을 강화한다. 근거 없는 대규모 리팩터링은 하지 않는다.

## Completed

- v1.11.0 public stable 및 canonical project memory 복구
- v1.11.1 maintenance branch 생성
- 사용자 요구사항/목표 버전 공식 체크포인트 기록

## Current step

- Scanner display settings 구조와 ammo pickup presentation 연결 누락 원인 조사
- Items/Hideout search UI와 다른 탭의 clear-button 패턴 비교
- correction hotkey save-success → Mini Scanner feedback 전달 경로 조사
- v1.11.0 변경 범위 중심 regression/maintenance audit

## Remaining

- root cause 확정 및 최소 구현
- 결정적 회귀 테스트/제품 smoke 보강
- 전체 Windows Release build/test/published EXE smoke
- PR/CI
- main merge + exact-main 검증
- v1.11.1 tag/release/assets public readback
- 공식 상태 문서 갱신 후 ACTIVE_WORK NONE
