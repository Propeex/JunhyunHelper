# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **NONE**  
Updated: **2026-08-31 KST**

현재 진행 중인 개발 작업은 없습니다.

## Last completed batch

`v1.12.0` MINOR 배치를 완료했습니다.

```text
public stable: v1.12.0
exact product release source/tag target:
b2fcec460df256c581e87b53c6293dc4d2177b9c
final PR: #238 — MERGED
superseded draft PR: #237 — CLOSED / NOT MERGED
validated feature head: 5216ab410c8a4384aee7d9f1a69fbd30302ad0a8
feature-head CI: 33348681591 — SUCCESS
feature-head Shutdown Race CI: 33348681589 — SUCCESS
feature-head Documentation Consistency: 33348681555 — SUCCESS
exact-main CI: 33348916340 — SUCCESS
exact-main Shutdown Race CI: 33348916440 — SUCCESS
exact-main Documentation Consistency: 33348916365 — SUCCESS
Release workflow: 33349066686 — SUCCESS
release id: 379463868
482 passed / 0 failed
```

완료된 제품 변경:

- Trader LL 진행 뒤 과거 staged task-pool Quest 최대 48개가 `확인 필요`로 되돌아가던 current availability 회귀 수정
- exact ProfileVariable 우선 / current-stage 보수적 평가 / structural drift fail-closed 유지
- Future Needed Items / cleanup 안전 계산과 current Quest UI compatibility 분리 유지
- 은신처 검색창 clear `×` 수직 정렬 수정
- 메인 좌측 상단 프로필 이미지로 실행하는 opt-in `김태영 PC 진단` 추가
- 진단 ZIP은 바탕화면에만 생성하고 자동 전송하지 않으며 display/GPU/HDR/capture/Scanner evidence와 화면 비교를 수집
- 공개 v1.12.0 tag/release/assets/checksum 검증 완료

공개 릴리즈와 상세 근거:

- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
- `docs/RELEASE_1.12.0.md`
- `docs/RELEASE_NOTES_V1.12.0.md`
- `docs/.release-v1.12.0-status.json`
- `docs/DECISION_TASK_POOL_RUNTIME_COMPATIBILITY_2026-08-17.md`
- `docs/DECISION_V1.12.0_KIM_TAEYOUNG_PC_DIAGNOSTIC.md`

사용자의 실제 PC/Tarkov 환경에서 v1.12.0 최종 실사용 확인과 김태영 PC diagnostic ZIP의 실제 수집·분석은 자동화 release verification과 별개이며 `PENDING`입니다. 새 사용자 요구사항, 실제 회귀, 또는 Tarkov 변화가 확인되면 `main`의 현재 stable 상태에서 새 `ACTIVE` 작업을 시작합니다.
