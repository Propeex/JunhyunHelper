# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **NONE**  
Updated: **2026-08-31 KST**

현재 진행 중인 개발 작업은 없습니다.

## Last completed batch

`v1.11.3` PATCH 유지보수 배치를 완료했습니다.

```text
public stable: v1.11.3
exact product source/tag target:
043abad38f4c3ebc9101463a162614ef67df7536
merged PR: #234
superseded draft PR: #233 — CLOSED / NOT MERGED
PR exact-head CI: 33319386444 — SUCCESS
PR exact-head Shutdown Race: 33319386465 — SUCCESS
PR exact-head Documentation Consistency: 33319386455 — SUCCESS
exact-main CI: 33319592093 — SUCCESS
exact-main Shutdown Race: 33319592115 — SUCCESS
exact-main Documentation Consistency: 33319592111 — SUCCESS
Release workflow: 33319769016 — SUCCESS
release id: 379321405
474 passed / 0 failed / 0 skipped
```

완료된 제품 수정:

- Items / Hideout 검색창의 canonical inline `×`가 실제 page lifecycle에서 안정적으로 연결되도록 수정했습니다.
- published smoke가 search clear UI를 스스로 만들어 회귀를 숨기던 false-positive 검증 경로를 제거했습니다.
- Map 지도 마커 패널이 큰 창의 가용 세로 공간을 사용하도록 수정해 하단 탈출구 항목 클리핑을 제거했습니다. 실제 overflow에서만 내부 scrollbar를 사용합니다.
- Scanner 교정 스크린샷에 마우스 휠 확대/축소와 스크롤/pan을 추가하면서 Ground Truth 및 직접 지정 좌표는 원본 pixel 좌표를 유지합니다.
- correction zoom 최초 runtime smoke에서 Auto scrollbar 때문에 fit scale이 달라지는 문제를 검출했고 stable arranged bounds 기준으로 수정했습니다.
- 사용자 diagnostics/calibration evidence에서 분석 완료 OCR/matcher frame이 이후 geometry-only frame에 덮여 `NOT_RUN`으로 저장되는 timing defect를 확인했습니다.
- 동일 non-empty title signature, 동일 capture mode, 3초 이내에서만 최근 analyzed semantics를 correction snapshot에 보존하도록 수정했습니다. live recognition 판정과 threshold는 완화하지 않았습니다.

공개 릴리즈와 상세 근거는 다음 문서를 기준으로 합니다.

- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
- `docs/RELEASE_1.11.3.md`
- `docs/RELEASE_NOTES_V1.11.3.md`
- `docs/.release-v1.11.3-status.json`

사용자의 실제 PC/Tarkov 환경에서 v1.11.3 최종 실사용 확인은 자동화 검증과 별개이며 `PENDING`입니다. 새 사용자 요구사항, 실제 회귀, 또는 Tarkov 변화가 확인되면 `main`의 현재 stable 상태에서 새 `ACTIVE` 작업을 시작합니다.
