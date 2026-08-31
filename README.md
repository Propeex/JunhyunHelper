# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.13.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

v1.13.1은 v1.13.0에서 추가된 **파밍 가이드 Loadout / Inventory Editor**의 실사용 UI/drag-drop 회귀를 수정한 PATCH 릴리즈입니다. 새 제품 기능을 추가하지 않고 기존 제품 의미와 계약을 보존하면서, 사용자가 의도한 아이콘 중심 Tarkov 인벤토리형 UI와 실제 drag/drop 동작을 복구했습니다.

공식 프로젝트 기억은 대화가 아니라 저장소 문서와 코드입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태와 유지 계약
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 설계·제품 결정

## 현재 공개 릴리즈

```text
version: v1.13.1
Desktop target version: 1.13.1
exact product release source/tag target:
302f83e88cc65b5fae9b86b5cae294b2586c85a0
PR: #243 — MERGED
validated PR head: 314ce0501c0f680aacb13d2b3c61b20487c4eb15
PR exact-head CI: 33364597514 — SUCCESS
PR exact-head Shutdown Race CI: 33364597501 — SUCCESS
PR exact-head Documentation Consistency: 33364597497 — SUCCESS
exact-main CI: 33364865109 — SUCCESS
exact-main Shutdown Race CI: 33364865123 — SUCCESS
exact-main Documentation Consistency: 33364865134 — SUCCESS
Release workflow: 33365070880 — SUCCESS
release id: 379553485
494 passed / 0 failed / 0 skipped
published UTC: 2026-08-31T06:39:45Z
```

Public package:

```text
Junhyun-Helper.zip
asset id: 537579591
bytes: 80,614,695
SHA-256:
d81b6bbcdb02712cb27a549e62cfb8c0d48a8c83f95d7798922474a56e99a737
```

Checksum asset:

```text
SHA256SUMS.txt
asset id: 537579593
bytes: 86
asset SHA-256:
14c38f75b70a27d3d6d0ec956404e363dd7d134a6111da3a4b11538a97864e8c
```

Exact-main CI artifact:

```text
JunhyunHelper-win-x64
artifact id: 9747973218
archive bytes: 241,778,025
archive SHA-256:
58b38558b33095ddb20ec2e3cdd1ebeea7abb4e9c9c4614ce5d8747927b8e3f6
```

GitHub `/releases/latest`, release target, `refs/tags/v1.13.1`, exact-main product source가 모두 `302f83e88cc65b5fae9b86b5cae294b2586c85a0`에 일치합니다. 공개 release는 `draft=false`, `prerelease=false`입니다.

공식 v1.13.1 공개 기록:

- `docs/RELEASE_1.13.1.md`
- `docs/RELEASE_NOTES_V1.13.1.md`
- `docs/.release-v1.13.1-status.json`
- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

후속 documentation-only commit은 v1.13.1 제품 릴리즈 소스가 아닙니다. product source/tag/assets는 위 exact source에 고정된 historical identity입니다.

## v1.13.1 — 파밍 가이드 UI / drag-drop 보완

v1.13.1에서 v1.13.0의 제품 의미를 유지하면서 다음 실사용 회귀를 바로잡았습니다.

- 장비 영역을 텍스트 목록형에서 **아이콘 중심의 Tarkov 인벤토리 유사 슬롯 UI**로 재구성
- 장착 장비, Rig / Backpack / Secure Container, storage grid 배치 item을 실제 item icon으로 표시
- drag ghost를 item text가 아닌 실제 item icon으로 표시
- `R` 회전 시 비정사각형 item icon도 회전된 footprint에 맞게 표시
- WPF mouse capture 중 장비/carrier target을 놓치던 drag/drop 판정 보강
- ScrollViewer로 잘린 offscreen target을 geometry fallback이 잘못 선택하지 않도록 visible bounds 검증
- mouse-up 실제 좌표에서 drop 판정을 재계산
- valid/invalid 초록·빨강 hover border가 pointer 이동 후 남지 않도록 cleanup
- 프리셋 저장 아이콘과 검색창 입력 텍스트 clipping 수정

## 파밍 가이드 제품 의미

Scanner 오른쪽의 `파밍 가이드`는 실제 레이드 중 inventory grid를 계속 추적하는 기능이 아니라, **레이드 출발 장비와 수납 상태를 구성하는 Loadout / Inventory Editor**입니다.

주요 동작:

- 헤드셋, 헬멧, 얼굴/안경, 방탄복/아머드 리그, 무기, 권총 등 출발 장비 구성
- Pocket / Rig / Backpack / Secure Container / Special Slot 표현
- 검색 결과 item을 실제 Tarkov `width × height` 크기로 drag
- drag 중 `R` 키로 90도 회전
- grid snap / bounds / overlap / contiguous-space / current filter 검증
- current Tarkov data의 storage grid / equipment slot / attachment slot / armor plate slot / conflict 사용
- attachment와 교체형 방탄판 설정
- 전체 raid-start 상태 preset 저장/복원
- 근접무기와 PMC 인식표는 preset과 분리된 fixed setting
- 총 무게와 사용/전체 storage cell 요약
- 내용물이 든 carrier의 destructive replacement 방지
- Tarkov 변화로 오래된 preset이 불가능해지면 invalid placement를 fail closed

현재 파밍 가이드에는 다음을 포함하지 않습니다.

- loot 가치 판단
- 획득/폐기/교체 추천
- Scanner 실시간 추천 연동
- 실제 raid inventory 좌표의 지속적인 1:1 동기화

Farming Guide 사용자 상태는 `%LocalAppData%/JunhyunHelper/farming-guide.json` schema v1에 저장됩니다.

## 설치 / 실행

배포 형태는 Windows x64 portable ZIP입니다.

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/
```

- Windows x64
- .NET 10 WPF
- self-contained executable
- 별도 .NET Runtime 설치 불필요
- installer 없음
- 일반 사용에 관리자 권한 불필요

사용자 데이터는 `%LocalAppData%/JunhyunHelper` 아래에 저장됩니다.

## 주요 기능

- GameMode별 Profile / User Progress
- Quest / Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / consumption ledger / cleanup
- Items / cross-navigation
- Ammo / favorites / 현재 프로필 기반 pickup 판단
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Scanner + Mini Scanner
- Scanner Ground Truth / diagnostics / Saved Case / regression dataset
- Scanner 아이템 정보 DB / Favorites / Recents
- Farming Guide raid-start Loadout / Inventory Editor
- opt-in PC capture/Scanner 지원 진단
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## 주요 안전·유지 계약

- Scanner는 external screen pixels + OCR만 사용하며 game memory read, injection, hook, kernel/driver 접근, input automation, network manipulation, anti-cheat bypass를 사용하지 않습니다.
- false positive보다 miss를 선호하며 actual Tarkov evidence 없이 OCR/matcher/candidate acceptance를 임의 완화하지 않습니다.
- Game Content update는 candidate → validation → active/LKG 전환의 fail-closed 계약을 유지합니다.
- Quest exact ProfileVariable은 runtime compatibility보다 항상 우선합니다.
- Future Needed Items / cleanup은 current Quest UI compatibility와 분리해 보수적으로 계산합니다.
- Hideout FIR은 source `attributes.foundInRaid` 의미를 보존합니다.
- Ammo pickup은 same-caliber penetration과 현재 profile에서 증명된 direct purchase 상태를 기준으로 합니다.
- Farming Guide는 current validated Tarkov item structure를 사용하고 불가능한 persisted placement는 fail closed합니다.
- 내용물이 든 carrier를 묵시적으로 교체해 contents를 유실시키지 않습니다.
- Map/MiniMap donor는 pinned revision `d933792b6042a51cea38dc44b686a096fe30de67`입니다.
- user-visible WPF 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE smoke까지 검증합니다.

## Schema / compatibility

```text
Desktop target version: 1.13.1
Content schema write: v9
Readable Content schemas: v3~v9
user.db schema: v1
Farming Guide state schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

v1.13.0 → v1.13.1에는 mandatory user data migration이 없습니다.

## 검증

v1.13.1 exact product source `302f83e88cc65b5fae9b86b5cae294b2586c85a0`은 494 deterministic tests, Windows Release build, Windows x64 self-contained publish, actual published EXE Product UI / Farming Guide / Map smoke, graceful shutdown, active-async Shutdown Race, package/checksum audit, exact-main Documentation Consistency, artifact upload, verified Release workflow, public tag/release/assets/latest-stable readback을 통과했습니다.

사용자의 실제 PC/Tarkov v1.13.1 최종 실사용 확인과 김태영 실제 PC diagnostic ZIP의 수집·분석은 자동화 검증과 별개이며 현재 `PENDING`입니다.
