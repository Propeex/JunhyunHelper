# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **ACTIVE**  
Updated: **2026-08-31 KST**

## Goal

v1.12.1 PATCH 유지보수 배치로 기존 `김태영 PC 진단`의 UX를 단순화하고 실제 사용자 노트북 진단 ZIP 결과를 검증한다.

## Base / working state

```text
base main: 33f52bdbe3b05a42544271557859eb3ef7de010c
public stable: v1.12.0
exact v1.12.0 product source: b2fcec460df256c581e87b53c6293dc4d2177b9c
working branch: fix/v1.12.1-kim-diagnostic-ux-2026-08-31
target version: v1.12.1
PR: #239
```

## Confirmed scope

- 헤더의 김태영 진단 아이콘 클릭 확인 문구는 정확히 `혹시 김태영 본인?`만 표시한다.
- `예`를 누른 뒤 진단 실행 중임을 확인할 수 있도록 별도 indeterminate progress bar를 표시한다.
- 정상 완료 메시지는 정확히 다음 두 문장만 표시한다.
  - `진단 완료.`
  - `파일을 hyune4784@naver.com 으로 보내주세요.`
- 완료 안내를 닫은 뒤 기본 브라우저에서 `https://mail.naver.com/v2/new` 네이버 메일 쓰기 페이지를 자동으로 연다.
- 진단 ZIP은 계속 바탕화면에 로컬 생성하며 자동 업로드, 웹메일 자동 첨부, 자동 발송은 하지 않는다.
- 브라우저 compose launch 실패는 성공 메시지 계약을 바꾸지 않고 내부 diagnostic log에만 남긴다.

## Completed

- 사용자 노트북에서 생성된 `JunhyunHelper-KimTaeyoung-Diagnostic-20260831-110826.zip` 검토
  - expected top-level evidence 11개 모두 생성
  - `probe-errors.txt = none`
  - display screenshot + luminance stats 정상
  - nested Scanner support ZIP 정상
  - Scanner/catalog snapshot 정상
  - 진단 당시 Tarkov 미실행으로 `captures/tarkov.txt = EscapeFromTarkov window not found.`; Tarkov dual-capture 비교는 이번 샘플에서 수행되지 않음
  - allowlist 대상 관련 process가 없어 `relevant-processes.txt`가 헤더만 있는 것은 정상
- 확인/완료 문구 고정 구현
- indeterminate progress overlay 구현
- 완료 후 네이버 메일 쓰기 페이지 기본 브라우저 launch 구현
- v1.12.1 Desktop/FIRST_RUN version 정렬
- regression source contract 추가
- release notes / decision 문서 갱신
- Ready PR #239 생성

## Current step

- PR #239 exact-head CI / Shutdown Race / Documentation Consistency 검증
- 첫 Documentation Consistency run `33350469114`는 ACTIVE_WORK required heading `## Confirmed scope` 누락만 검출했다. 제품/runtime 실패가 아니며 이 checkpoint에서 heading을 정정했다.

## Remaining

- 최신 exact-head PR gates 성공 확인
- main merge / exact-main CI / Shutdown Race / Documentation Consistency 확인
- automatic v1.12.1 release/tag/assets/checksum 검증
- PROJECT_STATE / CURRENT_STATE / STATE / README / release evidence 최종 정렬
- ACTIVE_WORK 완료 처리
