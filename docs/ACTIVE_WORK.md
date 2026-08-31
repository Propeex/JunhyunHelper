# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **NONE**  
Updated: **2026-08-31 KST**

현재 진행 중인 개발 작업은 없습니다.

## Last completed work

**v1.13.0 Farming Guide — raid-start Loadout / Inventory Editor**

```text
public stable: v1.13.0
exact product release source/tag target:
103ade0c5d54ffb59a6844330d19a930899c12fb
feature branch: feature/v1.13.0-farming-guide-loadout-editor-2026-08-31
original Draft PR: #240 — CLOSED / NOT MERGED
replacement PR: #241 — MERGED
validated feature head:
30424d0cc401a62b415dd772c52e5de4f6c931ee
exact-main CI: 33358877907 — SUCCESS
exact-main Shutdown Race CI: 33358877912 — SUCCESS
exact-main Documentation Consistency: 33358877946 — SUCCESS
release workflow: 33359054856 — SUCCESS
release id: 379519928
494 passed / 0 failed / 0 skipped
```

Draft PR #240은 제품/코드 문제가 아니라 connected GitHub draft→ready GraphQL mutation의 schema mismatch 때문에 ready 전환이 불가능해 닫았다. 동일 source branch와 검증 완료 HEAD로 non-draft PR #241을 만들고 exact-head CI를 다시 통과시킨 뒤 merge했다.

## Completed product scope

- Scanner 오른쪽 `파밍 가이드` first-class section
- raid-start equipment / storage / search-summary editor
- current Tarkov `width × height` item footprint
- drag-and-drop + `R` 90도 회전
- grid snap / bounds / overlap / contiguous-space / current filter 검증
- Pocket / Rig / Backpack / Secure Container / Special Slot
- current validated Tarkov storage grid / slot / attachment / armor plate / conflict structure 사용
- attachment / armor plate configuration
- full raid-start preset save/load
- fixed melee / PMC dogtag와 per-profile preset 분리
- total weight / storage cell summary
- populated carrier destructive replacement fail-closed
- old persisted preset의 impossible placement current-content sanitization
- Farming Guide state `%LocalAppData%/JunhyunHelper/farming-guide.json` schema v1
- Content write schema v9 / readable v3~v9

v1.13.0에는 loot 가치 판단, pickup/discard/replace 추천, Scanner 실시간 추천 연동, 실제 raid inventory 좌표의 지속적인 1:1 sync를 포함하지 않는다.

## Release verification

Exact product source `103ade0c5d54ffb59a6844330d19a930899c12fb`은 다음을 통과했다.

- 494 deterministic tests
- Windows Release build / XAML compile
- Windows x64 self-contained publish
- actual published EXE Product UI / Farming Guide / Map smoke
- graceful shutdown
- active-async Shutdown Race
- clean portable root / package audit
- ZIP/checksum equality
- exact-main CI / Documentation Consistency
- verified automatic Release workflow
- public `v1.13.0` tag / latest release / assets / digest readback

Public package:

```text
Junhyun-Helper.zip
80,613,758 bytes
SHA-256:
cbd8bafbf31ae65ecc659b15fc90a17408b87ecacdd9545c7b78de81c1835326
```

## Canonical records

- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
- `docs/PRODUCT.md`
- `docs/DECISIONS.md`
- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`
- `docs/ARCHITECTURE.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`
- `docs/RELEASE_1.13.0.md`
- `docs/.release-v1.13.0-status.json`
- `docs/RELEASE_NOTES_V1.13.0.md`

## External real-world evidence still pending

이 항목들은 v1.13.0 release 완료 조건과 별개이며 새 evidence가 들어오면 후속 유지보수 작업으로 시작한다.

- 사용자의 실제 PC/Tarkov v1.13.0 최종 실사용 확인
- 김태영 실제 PC diagnostic ZIP 수집/분석

## Next start condition

다음 중 하나가 생기면 현재 v1.13.0 public stable을 기준으로 새 `ACTIVE_WORK`를 연다.

- 사용자의 새 제품 요구사항
- 실사용 오류/회귀 보고
- Tarkov source/semantics 변화
- reviewed Scanner Ground Truth에 따른 개선 작업
- 김태영 실제 PC diagnostic evidence 분석

후속 documentation-only main commit은 v1.13.0 product release source가 아니다. v1.13.0 historical product identity는 `103ade0c5d54ffb59a6844330d19a930899c12fb`에 고정한다.
