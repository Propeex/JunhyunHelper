# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **NONE**  
Updated: **2026-08-30 KST**

v1.11.0 Scanner / Ammo / Hideout FIR / Map·MiniMap 유지보수 배치는 구현, 회귀 검증, Windows published EXE smoke, main 병합, exact-main CI, 공개 tag/release/assets 검증까지 완료됐다.

공개 stable:

```text
version: v1.11.0
exact product release source/tag target:
e0a8dd8acc86f8c5675efd0b24cb3006c19ccb1d
PR: #226 — MERGED
exact-main CI: 33299138580 — SUCCESS
exact-main Shutdown Race CI: 33299138567 — SUCCESS
release workflow: 33299258838 — SUCCESS
release id: 379210317
457 passed / 0 failed / 0 skipped
```

공식 현재 상태는 다음 문서에서 복구한다.

- `docs/PROJECT_STATE.json`
- `README.md`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
- `docs/RELEASE_1.11.0.md`
- `docs/RELEASE_NOTES_V1.11.0.md`
- `docs/.release-v1.11.0-status.json`

현재 남은 제품 개발 작업은 없다. 다음 작업은 사용자가 새 요구사항을 전달하거나 실제 실사용 회귀/Tarkov 변화가 확인될 때 현재 stable 상태에서 시작한다.

사용자의 실제 PC/Tarkov 플레이 환경 v1.11.0 실사용 검증은 자동 release verification과 별개이며 현재 `PENDING`이다.
