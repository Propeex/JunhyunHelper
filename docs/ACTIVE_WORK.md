# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **NONE**  
Updated: **2026-08-31 KST**

현재 진행 중인 개발 작업은 없습니다.

## Last completed batch

`v1.11.4` PATCH 유지보수 배치를 완료했습니다.

```text
public stable: v1.11.4
exact product release source/tag target:
f9d3497004241ea80193e5a0d242e7219cf04f2a
merged PR: #236
superseded draft PR: #235 — CLOSED / NOT MERGED
final feature head: 84b56e81171543e289ed417d822c40c9d607d4d3
PR exact-head CI: 33345630940 — SUCCESS
PR exact-head Shutdown Race: 33345630896 — SUCCESS
PR exact-head Documentation Consistency: 33345630871 — SUCCESS
exact-main CI: 33345851673 — SUCCESS
exact-main Shutdown Race: 33345851704 — SUCCESS
exact-main Documentation Consistency: 33345851658 — SUCCESS
Release workflow: 33346020525 — SUCCESS
release id: 379449740
478 passed / 0 failed / 0 skipped
```

완료된 제품 수정:

- Main Map에서 지도를 바꾼 직후 MiniMap을 처음 생성해도 stale map을 첫 프레임에 표시하지 않도록 selection synchronization 순서를 수정했습니다.
- MiniMap PMC / Scav / Transit extract marker 경로를 actual rendered marker 기준으로 검증했습니다.
- donor async marker refresh 취소 타이밍으로 standard marker layer가 비는 경우, 이미 로드된 데이터에서 해당 레이어만 직접 복구하도록 보강했습니다.
- Player Marker Size 변경을 player marker scale에만 격리해 Name Size / MiniMap Marker Size 등 unrelated presentation을 건드리지 않도록 했습니다.
- Mini Scanner 우클릭 `현재 결과 교정` context menu를 제거하면서 좌클릭 드래그, topmost, 결과 표시, 교정 데이터 단축키 계약은 유지했습니다.
- release identity 문서 형식을 바로잡고 final PR head, exact-main, published EXE smoke, package checksum, 공개 release/tag/assets까지 검증했습니다.

공개 릴리즈와 상세 근거는 다음 문서를 기준으로 합니다.

- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
- `docs/RELEASE_1.11.4.md`
- `docs/RELEASE_NOTES_V1.11.4.md`
- `docs/.release-v1.11.4-status.json`

사용자의 실제 PC/Tarkov 환경에서 v1.11.4 최종 실사용 확인은 자동화 검증과 별개이며 `PENDING`입니다. 새 사용자 요구사항, 실제 회귀, 또는 Tarkov 변화가 확인되면 `main`의 현재 stable 상태에서 새 `ACTIVE` 작업을 시작합니다.
