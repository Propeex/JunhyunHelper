# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **NONE**  
Updated: **2026-08-31 KST**

현재 진행 중인 개발 작업은 없습니다.

## Last completed batch

`v1.12.1` PATCH 유지보수 배치를 완료했습니다.

```text
public stable: v1.12.1
exact product release source/tag target:
07a808f187e59f1b2b4b62ca6a947ccbed9baeaa
PR: #239 — MERGED
validated feature head: 7e418c7d32c945260b471d19ac43c411f15bef1b
PR exact-head CI: 33350561623 — SUCCESS
PR exact-head Shutdown Race CI: 33350561588 — SUCCESS
PR exact-head Documentation Consistency: 33350561628 — SUCCESS
exact-main CI: 33350742745 — SUCCESS
exact-main Shutdown Race CI: 33350742733 — SUCCESS
exact-main Documentation Consistency: 33350742720 — SUCCESS
Release workflow: 33350893047 — SUCCESS
release id: 379473487
483 passed / 0 failed / 0 skipped
```

완료된 제품 변경:

- 김태영 PC 진단 시작 확인 문구를 정확히 `혹시 김태영 본인?`으로 변경
- `예` 후 진단 실행 동안 indeterminate progress bar 표시
- 정상 완료 문구를 `진단 완료.` / `파일을 hyune4784@naver.com 으로 보내주세요.` 두 문장으로 고정
- 완료 안내 종료 후 기본 브라우저에서 `https://mail.naver.com/v2/new` 자동 열기
- ZIP 자동 업로드 / 웹메일 자동 첨부 / 자동 이메일 발송은 하지 않음
- 사용자 노트북의 실제 v1.12.0 diagnostic ZIP을 검토해 exporter가 정상적으로 evidence를 생성함을 확인
  - ZIP CRC 정상
  - expected evidence 11개 모두 존재
  - `probe-errors.txt = none`
  - display capture/stats와 nested Scanner support bundle 정상
  - 당시 Tarkov 미실행으로 Tarkov dual-capture evidence는 없음
- v1.12.1 공개 tag/release/assets/checksum/latest stable 검증 완료

공개 릴리즈와 상세 근거:

- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
- `docs/RELEASE_1.12.1.md`
- `docs/RELEASE_NOTES_V1.12.1.md`
- `docs/.release-v1.12.1-status.json`
- `docs/DECISION_V1.12.0_KIM_TAEYOUNG_PC_DIAGNOSTIC.md`

사용자의 실제 PC/Tarkov 환경에서 v1.12.1 최종 실사용 확인과 김태영 실제 PC diagnostic ZIP의 수집·분석은 자동화 release verification과 별개이며 `PENDING`입니다. 새 사용자 요구사항, 실제 회귀, 또는 Tarkov 변화가 확인되면 `main`의 현재 stable 상태에서 새 `ACTIVE` 작업을 시작합니다.
