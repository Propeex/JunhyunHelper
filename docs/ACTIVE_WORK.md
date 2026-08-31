# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **NONE**  
Updated: **2026-08-31 KST**

현재 진행 중인 개발 작업은 없습니다.

## Last completed work

**v1.13.1 Farming Guide — 실사용 UI / drag-drop 회귀 수정**

```text
public stable: v1.13.1
exact product release source/tag target:
302f83e88cc65b5fae9b86b5cae294b2586c85a0
fix branch: fix/v1.13.1-farming-guide-ui-regressions-2026-08-31
PR: #243 — MERGED
validated PR head:
314ce0501c0f680aacb13d2b3c61b20487c4eb15
PR exact-head CI: 33364597514 — SUCCESS
PR exact-head Shutdown Race CI: 33364597501 — SUCCESS
PR exact-head Documentation Consistency: 33364597497 — SUCCESS
exact-main CI: 33364865109 — SUCCESS
exact-main Shutdown Race CI: 33364865123 — SUCCESS
exact-main Documentation Consistency: 33364865134 — SUCCESS
release workflow: 33365070880 — SUCCESS
release id: 379553485
494 passed / 0 failed / 0 skipped
```

### Completed product scope

- Farming Guide 장비 영역을 텍스트 목록형에서 아이콘 중심의 Tarkov 인벤토리 유사 slot board로 재구성.
- equipped item, carrier, storage grid placement, drag ghost에 실제 item icon 적용.
- `R` 회전 시 비정사각형 icon도 실제 rotated footprint에 맞게 layout.
- WPF mouse capture 중 equipment / Rig / Backpack / Secure Container drop target을 놓치던 판정 보강.
- geometry fallback에서 ScrollViewer/clip ancestor visible bounds를 검증해 화면 밖 target 선택 차단.
- mouse-up actual coordinate에서 drop probe 재계산.
- valid/invalid 초록·빨강 target highlight cleanup.
- preset save icon / search input clipping 수정.
- v1.13.0 placement / compatibility / persistence / preset / fixed melee·dogtag / carrier safety 계약 보존.

### Release verification

Exact product source `302f83e88cc65b5fae9b86b5cae294b2586c85a0`은 다음을 통과했습니다.

- 494 deterministic tests
- Windows Release build / XAML compile
- Windows x64 self-contained publish
- actual published EXE Product UI / Farming Guide / Map smoke
- graceful shutdown + clean portable root
- active-async Shutdown Race
- package / forbidden dependency / checksum audit
- exact-main Documentation Consistency
- automatic verified Release workflow
- public `v1.13.1` tag / release / latest / asset digest readback

Public package:

```text
Junhyun-Helper.zip
80,614,695 bytes
SHA-256:
d81b6bbcdb02712cb27a549e62cfb8c0d48a8c83f95d7798922474a56e99a737
```

## Canonical records

- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
- `docs/PRODUCT.md`
- `docs/RELEASE_1.13.1.md`
- `docs/RELEASE_NOTES_V1.13.1.md`
- `docs/.release-v1.13.1-status.json`
- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

## External real-world evidence still pending

자동화 release verification과 별개로 다음은 후속 실사용 evidence입니다.

- 사용자의 실제 PC/Tarkov에서 v1.13.1 최종 실사용 확인
- 김태영 실제 PC diagnostic ZIP 수집/분석

새 사용자 요구사항, 실사용 회귀, Tarkov 변화, reviewed Scanner Ground Truth 또는 실제 diagnostic evidence가 들어오면 v1.13.1 public stable에서 새 `ACTIVE_WORK`를 엽니다.

후속 documentation-only commit은 v1.13.1 product release source가 아닙니다. historical product identity는 `302f83e88cc65b5fae9b86b5cae294b2586c85a0`에 고정합니다.
