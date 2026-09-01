# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 상태는 **v1.14.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.14.1
exact product source/tag target:
add12c1b160f54e494d549978073f25e27cc4191
PR: #253 — MERGED
validated PR head: 42abdc7945c8f12a26553c6d0386cdadc6e41803
PR CI: 33456589868 — SUCCESS
PR Shutdown Race: 33456589884 — SUCCESS
PR Documentation Consistency: 33456589878 — SUCCESS
exact-main CI: 33456851817 — SUCCESS
exact-main Shutdown Race: 33456851818 — SUCCESS
exact-main Documentation Consistency: 33456851901 — SUCCESS
Release workflow: 33457066723 — SUCCESS
release id: 380147230
published UTC: 2026-09-01T01:01:22Z
529 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 538731592
bytes: 80,630,913
SHA-256:
b1216d9c661be909aee8c4a3f4eeb199b03eae46ba1f91799172bf8fd0074921

SHA256SUMS.txt
asset id: 538731593
bytes: 86
asset SHA-256:
a3817550bf8d8ed0813606ddc4ae511d3f989b473cedea8c1e137e9209b7944a
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9781796510
archive bytes: 241,822,850
archive SHA-256:
c55c6da388c078c9cf011b5db35b2797424daa8d59cdd7a7c9ed232acfd97031
```

GitHub `/releases/latest`, release target and `refs/tags/v1.14.1` all resolve to `add12c1b160f54e494d549978073f25e27cc4191`. The release is neither draft nor prerelease. Later documentation-only commits are not v1.14.1 product sources and may not replace these assets.

Release evidence:

- `docs/RELEASE_1.14.1.md`
- `docs/.release-v1.14.1-status.json`
- `docs/RELEASE_NOTES_V1.14.1.md`
- `docs/DECISION_V1.14.1_STORAGE_LAYOUT_SIGNATURE_GUARD.md`

## v1.14.x Farming Guide

`파밍 가이드`는 Scanner 오른쪽의 **raid-start Loadout / Inventory Editor**입니다. 실제 raid inventory를 실시간 mirror하지 않습니다.

v1.14.0에서 추가된 현재 기능:

- 총기·헬멧·방어구 attachment tree를 하위 부품 슬롯까지 재귀 편집
- 빈 attachment/armor slot 클릭 시 같은 화면의 compatible-item picker로 즉시 장착
- search drag/drop과 picker가 동일 Core compatibility/conflict policy 사용
- authoritative default preset 구성과 정확히 일치할 때 composed image 사용
- arbitrary assembly는 deterministic assembly-aware fallback으로 표시
- 검증된 일부 multi-grid carrier에 product-owned exact visual layout 적용
- Content snapshot write schema v10에서 assembly/layout identity 보존

v1.14.1 교정:

- exact storage layout은 layout identity와 grid count만이 아니라 **각 grid index의 width/height까지 검증된 signature와 정확히 일치할 때만** 사용
- width/height drift가 non-overlap이어도 stale exact coordinates를 거부
- mismatch/unknown은 storage mechanics를 변경하지 않고 finite compact visual fallback 사용

v1.13.3부터 유지되는 interaction:

- Secure Container와 일반 case/container를 구분
- `ParentInstanceId` 기반 nested bag/rig storage
- 실제 storage grid / weapon mod / attachment / armor plate drag/drop
- occupied one-item slot silent overwrite 금지
- nested cycle/orphan/impossible placement fail closed
- assembled weapon preset records를 Farming Guide 검색에서 제외

현재 Farming Guide에는 loot 가치 판단, 획득/폐기/교체 추천, Scanner 실시간 recommendation, 실제 raid inventory 좌표의 지속적인 1:1 동기화를 포함하지 않습니다.

## 설치 / 실행

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/
```

- Windows x64
- .NET 10 / WPF
- self-contained single-file executable
- portable ZIP / installer 없음
- 일반 사용에 관리자 권한 불필요
- mutable user data는 `%LocalAppData%/JunhyunHelper`에 저장

## 주요 기능

- GameMode별 Profile / User Progress
- Quest / Hideout / Needed Items / Inventory / cleanup
- Items / Ammo / cross-navigation / profile-aware pickup
- Game Content 안전 업데이트 / Last Known Good
- Map + MiniMap
- Scanner + Mini Scanner / Ground Truth / diagnostics / Saved Case
- Farming Guide raid-start Loadout / Inventory Editor
- opt-in PC capture/Scanner 지원 진단
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## 주요 안전·유지 계약

- Scanner는 external screen pixels + OCR만 사용하며 memory read, injection, hook, kernel/driver access, input automation, network manipulation, anti-cheat bypass를 사용하지 않습니다.
- false positive보다 miss를 선호하고 reviewed actual Tarkov evidence 없이 recognition acceptance를 완화하지 않습니다.
- Game Content는 candidate → validation → active/LKG의 fail-closed lifecycle을 유지합니다.
- Quest exact ProfileVariable은 compatibility inference보다 우선합니다.
- Future Needed Items / cleanup은 current Quest UI compatibility와 분리해 보수적으로 계산합니다.
- Hideout FIR은 source 의미를 보존합니다.
- Ammo pickup은 same-caliber penetration과 현재 profile에서 증명된 direct purchase 상태를 기준으로 합니다.
- Farming Guide storage legality는 current validated Game Content가 권위이며 visual layout metadata가 mechanics를 바꿀 수 없습니다.
- Map/MiniMap donor는 `SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67`에 고정되어 있습니다.
- user-visible WPF 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE smoke까지 검증합니다.

## Schema / compatibility

```text
Desktop version: 1.14.1
Content schema write: v10
Readable Content schemas: v3~v10
user.db schema: v1
Farming Guide state schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

v1.14.1에는 mandatory user-data migration이 없습니다.

## 검증 상태

Exact product source `add12c1b160f54e494d549978073f25e27cc4191`은 529 deterministic tests, Windows Release build, self-contained win-x64 publish, actual published EXE Product UI/Farming Guide/Map smoke, exact storage-layout/drop-target smoke, graceful shutdown, active-async Shutdown Race, package/checksum audit, exact-main Documentation Consistency, exact-main artifact digest verification, automatic Release workflow, public tag/release/assets/latest-stable readback을 통과했습니다.

사용자의 실제 PC/Tarkov 실사용 확인과 김태영 실제 PC diagnostic evidence 수집은 자동화 release verification과 별개이며 현재 `PENDING`입니다.
