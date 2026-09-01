# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **NONE**  
Updated: **2026-09-01 KST**

현재 복구해야 할 진행 중 개발 작업은 없습니다.

## 최근 완료

**v1.14.1 Farming Guide exact storage-layout signature guard**

- v1.14.0 공개 후 release-closure review에서 exact visual-layout activation의 per-grid expected width/height 비교 누락을 발견했습니다.
- v1.14.0 public tag/source/assets는 immutable historical identity로 유지했습니다.
- v1.14.1에서 product-owned exact profile에 expected width/height를 저장하고 current live grid의 per-index dimensions가 정확히 일치할 때만 exact coordinates를 사용하도록 수정했습니다.
- dimension mismatch는 storage mechanics를 변경하지 않고 finite compact visual fallback으로 fail closed합니다.
- deterministic regression과 actual published-runtime A18 smoke fixture를 동일 verified signature로 정합화했습니다.
- PR #253 final exact head `42abdc7945c8f12a26553c6d0386cdadc6e41803`의 CI / Shutdown Race / Documentation Consistency가 성공했습니다.
- exact product source `add12c1b160f54e494d549978073f25e27cc4191`의 exact-main CI / Shutdown Race / Documentation Consistency가 성공했습니다.
- 529 passed / 0 failed / 0 skipped.
- Release workflow `33457066723`이 exact-main artifact `9781796510`을 사용해 v1.14.1을 공개했습니다.
- public release ID `380147230`, ZIP SHA-256 `b1216d9c661be909aee8c4a3f4eeb199b03eae46ba1f91799172bf8fd0074921`을 검증했습니다.
- `/releases/latest`, `refs/tags/v1.14.1`, release target이 exact product source에 일치함을 확인했습니다.

공식 증거:

- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
- `docs/RELEASE_1.14.1.md`
- `docs/.release-v1.14.1-status.json`
- `docs/DECISION_V1.14.1_STORAGE_LAYOUT_SIGNATURE_GUARD.md`

향후 새로운 작업은 현재 public stable v1.14.1과 위 canonical 상태에서 시작합니다.
