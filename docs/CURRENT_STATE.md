# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-08-31 KST**  
상태: **v1.13.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.13.0
exact product release source/tag target:
103ade0c5d54ffb59a6844330d19a930899c12fb
PR: #241 — MERGED
validated feature head: 30424d0cc401a62b415dd772c52e5de4f6c931ee
PR exact-head CI: 33358670772 — SUCCESS
PR exact-head Shutdown Race CI: 33358670694 — SUCCESS
PR exact-head Documentation Consistency: 33358670722 — SUCCESS
exact-main CI: 33358877907 — SUCCESS
exact-main Shutdown Race CI: 33358877912 — SUCCESS
exact-main Documentation Consistency: 33358877946 — SUCCESS
release workflow: 33359054856 — SUCCESS
release id: 379519928
published UTC: 2026-08-31T05:01:47Z
494 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 537475557
bytes: 80,613,758
SHA-256:
cbd8bafbf31ae65ecc659b15fc90a17408b87ecacdd9545c7b78de81c1835326

SHA256SUMS.txt
asset id: 537475554
bytes: 86
asset SHA-256:
c3f174348668c0dfe9fc7b0ebcf5c1c2846b802b60a78f205833f6ffcb9f6a71
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9746074189
archive bytes: 241,774,204
archive SHA-256:
d1119a9931695016085e71bd84514f15c0bd5b051734deddce6dfb43053cf94e
```

GitHub `/releases/latest`, release target, `refs/tags/v1.13.0`, exact-main product source가 모두 `103ade0c5d54ffb59a6844330d19a930899c12fb`에 일치한다. 공개 release는 `draft=false`, `prerelease=false`이다.

## v1.13.0 핵심 변경 — 파밍 가이드

Scanner 오른쪽에 raid-start Loadout / Inventory Editor인 `파밍 가이드` 탭이 추가됐다.

- 착용 장비와 Pocket / Rig / Backpack / Secure Container / Special Slot 상태를 구성한다.
- 검색 결과 item을 current Tarkov `width × height` footprint로 drag한다.
- drag 중 `R`로 90도 회전한다.
- grid snap / bounds / overlap / contiguous-space / current filter를 검증한다.
- current validated Game Content의 storage grids, equipment/attachment/armor slots, conflicts를 사용한다.
- attachment와 교체형 armor plate를 별도 설정 UI에서 편집한다.
- 전체 출발 상태를 preset으로 저장/복원한다.
- 근접무기와 PMC 인식표는 per-profile preset과 분리된 fixed setting이다.
- 총 무게와 사용/전체 storage cell을 표시한다.
- 내용물이 든 carrier를 묵시적으로 교체해 contents를 유실시키지 않는다.
- 오래된 preset이 current Tarkov grid/filter와 충돌하면 impossible placement를 fail closed한다.

이번 v1.13.0에는 loot 가치 판단, 획득/폐기/교체 추천, Scanner 실시간 추천 연동, 실제 raid inventory grid 좌표의 지속적인 1:1 동기화를 포함하지 않는다.

Canonical product decision:

- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`

Architecture/maintenance contract:

- `docs/ARCHITECTURE_FARMING_GUIDE.md`

## 유지되는 주요 계약

- Scanner는 false positive보다 miss를 선호하며 reviewed actual Tarkov evidence 없이 recognition acceptance를 완화하지 않는다.
- Scanner는 external screen pixels + OCR만 사용한다. game memory read/injection/hook/kernel/input automation/network manipulation/anti-cheat bypass를 사용하지 않는다.
- Quest exact ProfileVariable은 runtime compatibility보다 우선하며 Future Needed Items / cleanup은 current Quest UI compatibility와 분리해 보수적으로 계산한다.
- Hideout FIR은 source `attributes.foundInRaid` 의미를 보존한다.
- Ammo pickup은 same-caliber penetration과 현재 profile에서 증명된 direct purchase 상태를 기준으로 한다.
- Game Content update는 candidate/LKG/completeness/fail-closed 계약을 유지한다.
- Map/MiniMap donor pin은 `d933792b6042a51cea38dc44b686a096fe30de67`이다.
- user-visible WPF 변경은 actual published EXE runtime evidence로 검증한다.
- 공개 stable release bytes/tag/source는 immutable historical identity로 취급한다.

## Schema / compatibility

```text
Desktop target version: 1.13.0
Content schema write: v9
Readable Content schemas: v3~v9
user.db schema: v1
Farming Guide state schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

Farming Guide 상태는 `%LocalAppData%/JunhyunHelper/farming-guide.json`에 Game Content와 분리해 저장한다. 기존 v1.12.x user.db/Scanner 설정에 대한 mandatory migration은 없다.

## 검증 상태

Exact product source `103ade0c5d54ffb59a6844330d19a930899c12fb`은 다음을 통과했다.

- 494 deterministic tests
- Windows Release build
- Windows x64 self-contained publish
- actual published EXE Product UI / Farming Guide / Map smoke
- graceful shutdown
- active-async Shutdown Race
- clean portable root / forbidden dependency audit
- release package + checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload
- automatic verified Release workflow
- public tag/release/assets/latest-stable readback

## 남은 외부 실사용 확인

자동화 release verification과 별개로 다음은 `PENDING`이다.

- 사용자의 실제 PC/Tarkov에서 v1.13.0 최종 실사용 확인
- 김태영 실제 PC에서 diagnostic ZIP 수집/분석

공개 증거:

- `docs/RELEASE_1.13.0.md`
- `docs/.release-v1.13.0-status.json`
- `docs/RELEASE_NOTES_V1.13.0.md`
- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`

후속 documentation-only commit은 v1.13.0 product release source가 아니다. historical identity는 `103ade0c5d54ffb59a6844330d19a930899c12fb`에 고정한다.
