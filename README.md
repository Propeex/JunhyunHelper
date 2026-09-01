# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 상태는 **v1.15.0 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.15.0
exact product source/tag target:
b974d56f32d073ce21a5de4171737670f83261f3
validated candidate head: 397c82b8911597128c5878e7974db6a7822888d8
candidate CI: 33466090956 — SUCCESS
candidate Shutdown Race: 33466090958 — SUCCESS
candidate Documentation Consistency: 33466090940 — SUCCESS
merge PR: #256 — MERGED
exact-main CI: 33467376556 — SUCCESS
exact-main Shutdown Race: 33467376508 — SUCCESS
exact-main Documentation Consistency: 33467376529 — SUCCESS
Release workflow: 33467575493 — SUCCESS
release id: 380200480
published UTC: 2026-09-01T03:49:49Z
540 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 538909239
bytes: 80,647,419
SHA-256:
95f62c7d795f1954c3fd3437b17d9e15db05f5ab113f95df97055d15061bc76a

SHA256SUMS.txt
asset id: 538909237
bytes: 86
asset SHA-256:
5b8101bf0e086952ee12d4070e678cd1e0b5406e0c32ae91b7bf2562e7ab2ecb
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9785383239
archive bytes: 241,875,746
archive SHA-256:
6ba4c5819119a230ee02e4f7c2cb093679527623e3ab9665b8ebc05dee5936ae
```

GitHub `/releases/latest`, release target and `refs/tags/v1.15.0` all resolve to `b974d56f32d073ce21a5de4171737670f83261f3`. The release is neither draft nor prerelease. Later documentation-only commits are not v1.15.0 product sources and may not replace these assets.

Release evidence:

- `docs/RELEASE_1.15.0.md`
- `docs/.release-v1.15.0-status.json`
- `docs/RELEASE_NOTES_V1.15.0.md`
- `docs/DECISION_V1.15.0_FARMING_GUIDE_RAID_ADVISOR.md`

## v1.15.0 Farming Guide raid advisor

`파밍 가이드`는 기존 raid-start Loadout / Inventory Editor에 **레이드 세션 기반 파밍 도우미**가 추가된 형태입니다. Tarkov 내부 inventory를 직접 읽거나 게임 입력을 자동화하지 않습니다.

현재 동작:

- `레이드 시작` 시 현재 장비·수납·잠금 상태를 독립적인 raid session으로 snapshot
- Scanner가 인식한 아이템을 현재 Needed Items 수량, 경제 가치, 차지하는 칸, 현재 수납/잠금 상태와 함께 평가
- Mini Scanner에 보관 위치, 교체 또는 버리기 형태의 지시 표시
- 사용자가 설정한 `파밍 가이드 수락` 단축키를 눌러야 raid-session 상태에 반영
- 수락 전 장비·수납·잠금이 바뀌면 stale 지시 자동 취소
- 아이템·장비·수납 영역·빈 칸 hover + `F`로 자동 판단에서 보호/예약
- 검색 결과 아이템 hover + `T`로 실제 Scanner와 같은 recommendation path 테스트
- `레이드 종료` 시 레이드 중 변경을 폐기하고 시작 상태로 복귀

v1.14.x에서 구축한 기존 편집 기능도 유지됩니다.

- 총기·헬멧·방어구 attachment tree 재귀 편집
- compatible-item picker와 search drag/drop의 공통 compatibility policy
- `ParentInstanceId` 기반 nested bag/rig storage
- Secure Container와 일반 case/container 구분
- product-owned exact multi-grid visual layout의 full signature guard
- occupied one-item slot silent overwrite 금지
- impossible persisted state fail closed

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
- Farming Guide loadout/inventory editor + raid-session advisor
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
- Farming Guide raid recommendation은 user lock/reserved state와 session revision을 존중하고 명시적 수락 전에는 상태를 commit하지 않습니다.
- Map/MiniMap donor는 `SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67`에 고정되어 있습니다.
- user-visible WPF 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE smoke까지 검증합니다.

## Schema / compatibility

```text
Desktop version: 1.15.0
Content schema write: v10
Readable Content schemas: v3~v10
user.db schema: v1
Farming Guide state schema: v2 (reads v1-v2)
Scanner display settings schema: v10
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

v1.15.0의 Farming Guide state v2와 Scanner display settings v10은 기존 저장 상태/설정을 읽어 migration합니다.

## 검증 상태

Exact product source `b974d56f32d073ce21a5de4171737670f83261f3`은 540 deterministic tests, Windows Release build, self-contained win-x64 publish, actual published EXE Product UI/Farming Guide/Map smoke, graceful shutdown, active-async Shutdown Race, package/checksum audit, exact-main Documentation Consistency, exact-main artifact digest verification, automatic Release workflow, public tag/release/assets/latest-stable readback을 통과했습니다.

사용자의 실제 PC/Tarkov 실사용 확인과 김태영 실제 PC diagnostic evidence 수집은 자동화 release verification과 별개이며 현재 `PENDING`입니다.
