# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-01 KST**  
상태: **v1.14.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.14.0
exact product release source/tag target:
9ff23b9f50dd84b84ec93cea31b079d7eff70fe1
PR: #251 — MERGED
superseded Draft PR: #250 — CLOSED UNMERGED
validated PR head: c5ee50ba60f2bc7db461328608ec591f4320ccca
PR exact-head CI: 33453431628 — SUCCESS
PR exact-head Shutdown Race CI: 33453431625 — SUCCESS
PR exact-head Documentation Consistency: 33453431595 — SUCCESS
exact-main CI: 33453784868 — SUCCESS
exact-main Shutdown Race CI: 33453784901 — SUCCESS
exact-main Documentation Consistency: 33453784893 — SUCCESS
release workflow: 33454002732 — SUCCESS
release id: 380133403
published UTC: 2026-09-01T00:15:44Z
527 passed / 0 failed / 0 skipped
```

Public assets:

```text
Junhyun-Helper.zip
asset id: 538692301
bytes: 80,633,458
SHA-256:
87728ce9e34a30a9b1eb735fe92b1a4a39f172f3b9cf536dfd12d88c8c35667b

SHA256SUMS.txt
asset id: 538692300
bytes: 86
asset SHA-256:
06ae3473f7fe87d62b0d05dac0d16640a55e30e8a8fd83e4770f962a8fc5dfe3
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9780762947
archive bytes: 241,830,878
archive SHA-256:
1898028e10ef336b2dce35add94d2e1cf83b5c58c27c98649691fe11bdbe8632
```

GitHub `/releases/latest`, `refs/tags/v1.14.0`, release target, exact-main source가 모두 `9ff23b9f50dd84b84ec93cea31b079d7eff70fe1`에 일치한다. Release는 `draft=false`, `prerelease=false`이다.

## v1.14.0 핵심 변경 — Farming Guide assembly / validated layouts

- PMC dogtag equipment surface를 제거하고 legacy persisted value는 backward-compatible하게 읽는다.
- `FarmingGuideAssemblyPolicy`가 deep attachment/armor tree mutation, compatible candidates, slot filter, allowed plate, assembly-wide conflict, required-slot recursion, persisted-tree sanitization의 Core authority다.
- installed attachment의 하위 slot으로 재귀 navigation할 수 있다.
- empty slot 클릭은 in-page compatible-item icon picker를 열며 single-click으로 즉시 장착한다. 별도 Windows/OS configuration dialog는 사용하지 않는다.
- inline picker와 search drag/drop은 동일 compatibility policy를 사용한다.
- exact imported default-preset membership일 때만 composed preset image를 사용하고 arbitrary build는 deterministic assembly-aware fallback을 사용한다.
- storage legality는 current validated Game Content grid/filter mechanics가 권위다.
- product-owned exact multi-grid coordinates는 current grid count/width/height signature가 정확히 일치할 때만 적용한다.
- unknown/stale visual metadata는 finite compact layout으로 fail-safe fallback하며 authentic layout으로 주장하지 않는다.
- importer는 `GridLayoutName` / `RigLayoutName` 계열 identity를 `StorageLayoutName`으로 보존한다.
- Content snapshot write schema는 v10, readable compatibility는 v3~v10이다.
- Farming Guide user state schema는 v1을 유지한다.

Canonical decision / architecture:

- `docs/DECISION_V1.14.0_FARMING_GUIDE_ASSEMBLY_AND_AUTHENTIC_LAYOUTS.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

## Farming Guide 유지 계약

Farming Guide는 Scanner 오른쪽의 raid-start Loadout / Inventory Editor다.

- current Tarkov `width × height` footprint
- drag 중 `R` 90도 회전
- bounded grid snap
- bounds / overlap / contiguous-space / current filter 검증
- current validated Game Content의 storage grids, equipment/attachment/armor slots, conflicts 사용
- nested storage parent-instance 관계 보존
- recursive attachment / armor tree editing
- inline compatible-item picker + existing drag/drop
- 전체 raid-start preset save/load/delete
- melee user-level fixed setting
- profile-aware standard/expanded pockets
- 총 무게 / 사용·전체 storage cell 표시
- filled carrier destructive replacement fail closed
- impossible persisted state current-content/profile sanitization

Loot 가치 판단, pickup/discard/replace 추천, Scanner 실시간 recommendation, 실제 raid inventory 좌표의 지속적인 1:1 동기화는 현재 범위가 아니다.

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
Desktop version: 1.14.0
Content schema write: v10
Readable Content schemas: v3~v10
user.db schema: v1
Farming Guide state schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

Farming Guide 상태는 `%LocalAppData%/JunhyunHelper/farming-guide.json`에 Game Content와 분리해 저장한다. v1.14.0에는 mandatory user-state migration이 없다.

## 검증 상태

Exact product source `9ff23b9f50dd84b84ec93cea31b079d7eff70fe1`은 다음을 통과했다.

- 527 deterministic tests
- Windows Release build / XAML compile
- Windows x64 self-contained publish
- ProductVersion `1.14.0+9ff23b9f50dd84b84ec93cea31b079d7eff70fe1`
- actual published EXE Product UI / Farming Guide / Map smoke
- recursive assembly / inline picker / exact storage-layout render-drop target smoke
- graceful shutdown + clean portable root
- active-async Shutdown Race
- package root / forbidden dependency audit
- release package + checksum audit
- exact-main Documentation Consistency
- exact-main artifact upload/digest verification
- automatic verified Release workflow
- public tag/release/assets/latest-stable readback

## 남은 외부 실사용 확인

자동화 release verification과 별개로 다음은 `PENDING`이다.

- 사용자의 실제 PC/Tarkov에서 v1.14.0 최종 실사용 확인
- 김태영 실제 PC에서 diagnostic ZIP 수집/분석

공개 증거:

- `docs/RELEASE_1.14.0.md`
- `docs/.release-v1.14.0-status.json`
- `docs/RELEASE_NOTES_V1.14.0.md`
- `docs/DECISION_V1.14.0_FARMING_GUIDE_ASSEMBLY_AND_AUTHENTIC_LAYOUTS.md`

후속 documentation-only commit은 v1.14.0 product release source가 아니다. historical identity는 `9ff23b9f50dd84b84ec93cea31b079d7eff70fe1`에 고정한다.
