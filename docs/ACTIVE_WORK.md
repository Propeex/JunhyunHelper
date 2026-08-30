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
PR: #229 (ready; supersedes closed draft #228)
target version: v1.11.1
validated non-documentation RC commit: 805d5cdfa0b19791fe4d15b02d3cfddd645c3628
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
- **검증 공백**: v1.11.0 published EXE smoke는 Scanner 본 화면을 검사했지만 Scanner settings info rows는 실제 WPF control로 검사하지 않았다. v1.11.1은 settings row / Items·Hideout clear button / Mini Scanner save-feedback를 smoke 환경에서 직접 검증한다.
- **RC startup gate stale expectation**: schema v9 전환 뒤 기존 `ScannerPage.MiniScannerSmoke`가 schema `8`을 고정 비교해 published EXE startup과 Shutdown Race를 차단했다. 제품 설정 구현은 정상이었고 smoke contract만 오래된 상태였다. 해당 contract를 v9로 갱신하면서 ammo pickup의 실제 순서 이동/숨김까지 직접 검증하도록 강화했다.

## Completed

- v1.11.0 public stable 및 canonical project memory 복구
- v1.11.1 maintenance branch 생성
- closed draft PR #228을 동일 branch/head의 ready PR #229로 승계 (connected ready-for-review mutation의 GitHub GraphQL schema 호환 오류 우회; 제품 diff 변화 없음)
- Scanner display settings schema v9 + ammo pickup visibility/order 구현
- Scanner 설정 화면 `탄약 줍기 판단` 항목 연결
- Mini Scanner ammo 판단이 settings visibility/order를 실제 렌더에 적용하도록 수정
- Items/Hideout 공통 검색 `×` clear/focus 동작 구현
- correction hotkey save-success → Mini Scanner `저장 완료` transient feedback 구현
- 세 사용자 요구사항 source-level regression contracts 추가
- published EXE v1.11.1 usability smoke 추가
- stale v1.10 Scanner schema source contract와 startup product contract를 v9에 맞게 갱신
- startup product contract가 ammo pickup visibility/order까지 직접 검증하도록 강화
- Desktop target version 1.11.1 / Scanner display settings schema v9 / FIRST_RUN_KO / release notes RC 정리

## Maintenance audit

직전 v1.11.0과 주요 제품 계약을 재검토했다.

- ammo pickup evaluator: 사용자 확정 rank 예시, direct purchase band, LL, completed quest unlock, barter/craft 제외, equal-penetration tie, no-purchase boundary 유지
- Ammo Pack: authoritative `containsItems` 우선, empty relationship에서만 좁은 fallback, ambiguous/mixed relation fail-closed 유지
- Hideout FIR: nested `attributes.foundInRaid` 우선 및 canonical requirement 의미 유지
- Map/MiniMap: first-open map replay, extract late-load, marker/name presentation repair, empty-layer one-shot recovery 계약 유지
- correction hotkey: no-evidence exact status, evidence-only Saved Case, no automatic Ground Truth, duplicate explicit saves 유지
- Scanner OCR/matcher/candidate/recovery acceptance와 screen-pixels+OCR anti-cheat boundary 변경 없음
- Quest/Hideout/Items/Ammo/Map/MiniMap/Scanner 기존 smoke/lifecycle/package gate 유지

이번 사용자 보고 3건과 Scanner settings runtime-smoke 공백 외에 추가로 수정할 명확한 제품 결함은 확인되지 않았다. 추측성 리팩터링은 하지 않는다.

## Validation progress

Validated non-documentation RC: `805d5cdfa0b19791fe4d15b02d3cfddd645c3628`

- Documentation Consistency: PASS
- Windows Release build: PASS
- deterministic tests: **460 / 460 PASS**
- Windows x64 publish: PASS
- actual published EXE Startup + Product UI + Map + graceful shutdown smoke: PASS
  - Scanner settings `탄약 줍기 판단` runtime row 확인
  - Items / Hideout search `×` clear 동작 확인
  - Mini Scanner `저장 완료` transient status 렌더 확인
  - 기존 Map/MiniMap/Scanner 주요 product smoke 유지
- release package build/audit: PASS
- Windows x64 artifact upload: PASS
- Shutdown Race published candidate: PASS

중간 gate에서 발견된 stale schema-8 source/startup smoke 기대값은 v9로 갱신했고, 재검증에서 정상 통과했다.

## Current step

- ready PR #229의 final HEAD consistency/CI 확인
- main merge

## Remaining

- PR #229 main merge
- exact-main Windows CI + published EXE smoke
- automatic v1.11.1 release workflow 성공 확인
- v1.11.1 tag/release/assets/checksum public readback
- 공식 public-stable 상태 문서 finalization
- finalization docs CI/merge
- ACTIVE_WORK NONE
