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
```

## User-confirmed product behavior

- 헤더의 김태영 진단 아이콘 클릭 확인 문구는 정확히 `혹시 김태영 본인?`만 표시한다.
- `예`를 누른 뒤 진단 실행 중임을 확인할 수 있도록 별도 loading/progress bar를 표시한다.
- 정상 완료 메시지는 정확히 다음 두 문장만 표시한다.
  - `진단 완료.`
  - `파일을 hyune4784@naver.com 으로 보내주세요.`
- 완료 후 기본 브라우저에서 `https://mail.naver.com/v2/new` 네이버 메일 쓰기 페이지를 자동으로 연다.
- 진단 ZIP은 계속 바탕화면에 로컬 생성하며 자동 업로드/자동 발송하지 않는다.

## Uploaded diagnostic evidence reviewed

사용자 노트북에서 생성된 `JunhyunHelper-KimTaeyoung-Diagnostic-20260831-110826.zip`을 확인했다.

- expected top-level evidence 11개가 모두 생성됨
- `probe-errors.txt = none`
- display screenshot + luminance stats 생성 정상
- nested Scanner support ZIP 포함 정상
- Scanner/catalog snapshot 포함 정상
- 진단 당시 Tarkov가 실행 중이지 않아 `captures/tarkov.txt`는 `EscapeFromTarkov window not found.`였으며 Tarkov dual-capture 비교는 이번 샘플에서 수행되지 않음
- 관련 allowlist process가 진단 시점에 없어 `relevant-processes.txt`가 비어 있는 것은 정상

## Current step

- v1.12.1 version/UX 구현 및 regression contract 추가
- PR CI에서 actual published EXE product smoke까지 검증

## Remaining

- implementation + tests
- PR / exact-head CI / Shutdown Race / Documentation Consistency
- main merge / exact-main verification
- v1.12.1 release/tag/assets verification
- release state documentation 정리 후 ACTIVE_WORK 종료
