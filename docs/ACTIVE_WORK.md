# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **NONE**  
Updated: **2026-08-31 KST**

현재 진행 중인 개발 작업은 없다.

## Last completed work

**v1.13.2 Farming Guide — 장비·수납·프리셋·내부 정보 실사용 보완**

```text
public stable: v1.13.2
exact product release source/tag target:
207cb948affc091c4ad67f18d7e4e4382b2f8125
PR: #245 — MERGED
validated PR head:
ef4522880218b5e5ec8d8c0a8a3211e0f0c51020
PR exact-head CI: 33373322410 — SUCCESS
PR exact-head Shutdown Race CI: 33373322440 — SUCCESS
PR exact-head Documentation Consistency: 33373322395 — SUCCESS
exact-main CI: 33373612303 — SUCCESS
exact-main Shutdown Race CI: 33373612281 — SUCCESS
exact-main Documentation Consistency: 33373612283 — SUCCESS
release workflow: 33373940475 — SUCCESS
release id: 379612102
504 passed / 0 failed / 0 skipped
```

### Completed product scope

- pistol / revolver / handgun을 Holster 전용으로 판정하고 Primary Weapon 1/2에서 제외
- body armor / rig / backpack / secure container compatibility 보강
- active profile edition / Old Patterns 기반 standard·expanded pocket geometry
- `Rig → Pockets + Special Slots → Backpack → Secure Container` 수납 순서
- Pockets 좌측 / Special Slots 우측 배치
- equipped/search-result double-click internal structure inspect
- preset delete + current working loadout 보존
- preset-name dialog DPI/theme clipping 수정
- melee / PMC dogtag fixed lifecycle 유지 + `고정` 표시 제거
- current content/profile geometry 기준 persisted-state sanitization

### Public release evidence

```text
Junhyun-Helper.zip
asset id: 537701878
bytes: 80,617,300
SHA-256:
659071659531259a61d0996e277bf9643ee9fc4cfa8a0a437b4686994bd38bed

SHA256SUMS.txt
asset id: 537701880
bytes: 86
asset SHA-256:
0ebdc1240c721bf0192b703c77cfd944665f870edb7d79444dfd6181a2a43a19
```

GitHub `/releases/latest`, `refs/tags/v1.13.2`, release target가 모두 exact product source `207cb948affc091c4ad67f18d7e4e4382b2f8125`에 일치한다.

## Canonical records

- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
- `docs/PRODUCT.md`
- `docs/RELEASE_1.13.2.md`
- `docs/RELEASE_NOTES_V1.13.2.md`
- `docs/.release-v1.13.2-status.json`
- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

## External real-world evidence still pending

자동화 release verification과 별개로 다음은 후속 실사용 evidence다.

- 사용자의 실제 PC/Tarkov에서 v1.13.2 최종 실사용 확인
- 김태영 실제 PC diagnostic ZIP 수집/분석

후속 documentation-only commit은 v1.13.2 product release source가 아니다. historical product identity는 `207cb948affc091c4ad67f18d7e4e4382b2f8125`에 고정한다.
