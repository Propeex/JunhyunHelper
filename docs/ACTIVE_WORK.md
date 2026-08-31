# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-31 KST**

## Goal

사용자 실사용 요구사항을 반영한 **v1.12.0** 개발 배치를 진행한다.

- Quest 선행조건/상태 계산 회귀 수정: 새 프로필에서는 `확인 필요` 0개인데 일부 퀘스트 진행 후 정상 퀘스트가 대량으로 `확인 필요`로 늘어나는 현상을 재현·원인 분석·수정한다.
- Hideout 검색창의 clear `×` 위치를 다른 탭 검색창과 일관되게 맞춘다.
- 메인 좌측 상단 프로필 이미지 클릭으로 실행하는 `김태영 PC 진단` 지원 기능을 추가한다.

## Base / working state

```text
base main: b97556bfe162bd6d6507500eb1633adf4607efb6
public stable: v1.11.4
working branch: feature/v1.12.0-quest-diagnostics-search-ui-2026-08-31
PR: not created yet
```

## Confirmed scope

### Quest

- 사용자 증상 자체를 높은 우선순위 회귀 증거로 취급한다.
- 퀘스트 몇 개를 진행한 뒤 `확인 필요`가 수십 개로 증가하는 이유를 prerequisite/status 계산과 데이터 의미를 기준으로 추적한다.
- 기존 정상 상태 의미를 바꾸거나 fail-open으로 숨기지 않고 root cause를 수정한다.

### Hideout search clear button

- 첨부 캡처 기준 Hideout 검색창의 `×`만 다른 주요 검색창과 위치가 다르다.
- 공통 검색 UI 계약에 맞춰 정렬한다.

### 김태영 PC 진단

사용자 확정 흐름:

1. 메인 헤더 좌측 프로필 이미지 클릭.
2. 팝업에서 김태영 본인인지 확인.
3. `예`를 누르면 진단 시작.
4. Scanner/capture 결과에 영향을 줄 수 있는 환경·디스플레이·그래픽·캡처·앱/Scanner 상태를 필요한 범위에서 폭넓게 수집한다.
5. 진단 결과와 필요한 증거를 ZIP으로 묶어 바탕화면에 생성한다.
6. 완료 후 `hyune4784@naver.com`으로 ZIP을 보내 달라는 메시지를 표시하고 종료한다.

개발자는 세부 진단 항목, 안전한 개인정보 제외 기준, 파일 구조, 오류 격리, 수집 구현 방식을 결정한다.

## Current step

- 공식 문서/현재 v1.11.4 상태 복구 완료.
- 새 v1.12.0 작업 브랜치 생성 완료.
- 관련 Quest prerequisite 계산, 검색창 템플릿, 메인 헤더 프로필 이미지, Scanner/capture/diagnostic subsystem 코드와 테스트를 조사한다.

## Remaining

- Quest root cause 규명 및 결정적 회귀 테스트 작성
- Hideout clear button 정렬 수정 및 UI 계약 테스트
- 김태영 PC 진단 설계/구현/테스트
- 제품/설계/개발자 문서 갱신
- Desktop version v1.12.0 정렬
- PR 생성 및 CI
- Windows published EXE 실제 UI/runtime smoke
- main 병합 / exact-main CI
- v1.12.0 공개 릴리즈 및 asset/tag/checksum 검증
