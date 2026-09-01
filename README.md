# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 상태는 **v1.15.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.15.1
exact product source/tag target:
821def285e2b4964242b50981f6ba6245e996057
validated PR head: e78ca34c272ac40b8f7c6a4bfcefede59adb9d59
merge PR: #259 — MERGED
PR CI / Shutdown / Docs:
33476320371 / 33476320367 / 33476320491 — SUCCESS
exact-main CI / Shutdown / Docs:
33476586723 / 33476586808 / 33476586819 — SUCCESS
Release workflow: 33476812315 — SUCCESS
release id: 380252024
published UTC: 2026-09-01T06:15:51Z
558 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 539091025
bytes: 80,658,918
SHA-256:
80283d9dfc294d195d644ab12ac074b5d4698f4e500475d7435680ccb6e4fc0a

SHA256SUMS.txt
asset id: 539091026
bytes: 86
asset SHA-256:
906bde7d2c5a6e7234b3de1c21ba935c39522af84fe9f6fda352738457fb91d9
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9788440065
archive bytes: 241,908,886
archive SHA-256:
e865fb395dcca353788495bbfb84f860129b39bdc6e89b51780d99db481592b8
```

GitHub `/releases/latest`, release target and `refs/tags/v1.15.1` all resolve to `821def285e2b4964242b50981f6ba6245e996057`. The release is neither draft nor prerelease. Later documentation-only commits are not v1.15.1 product sources and may not replace these assets.

Release evidence:

- `docs/RELEASE_1.15.1.md`
- `docs/.release-v1.15.1-status.json`
- `docs/RELEASE_NOTES_V1.15.1.md`
- `docs/DECISION_V1.15.1_FARMING_GUIDE_REAL_PLAY_CORRECTIONS.md`

## v1.15.1 Farming Guide real-play corrections

`파밍 가이드`는 raid-start Loadout / Inventory Editor와 Scanner 기반 raid-session advisor를 함께 제공합니다. Tarkov 내부 inventory를 직접 읽거나 게임 입력을 자동화하지 않습니다.

현재 raid advisor 동작:

- `레이드 시작` 시 현재 장비·수납·잠금 상태를 독립적인 raid session으로 snapshot
- Scanner가 확인한 아이템을 현재 Needed Items 수량, 경제 가치, footprint, 현재 장비·수납·잠금 상태와 함께 평가
- 새 스캔이 들어오면 이전 미수락 지시는 상태에 반영하지 않고 폐기한 뒤 새 아이템을 현재 상태에서 다시 판단
- Mini Scanner에는 스캔 아이템 이름을 반복하지 않고 `보관`, `교체`, `버리기`, `장착` 행동만 짧게 표시
- 사용자가 설정한 `파밍 가이드 수락` 단축키를 눌러야 raid-session 상태에 반영하며 성공 피드백은 `반영 완료`
- 일반 장비 슬롯뿐 아니라 리그/가방/보안 컨테이너 장비 슬롯, 재귀 attachment 슬롯, 방탄판 슬롯까지 장착/교체 판단
- 특수 슬롯은 canonical `specialSlot` 분류만 허용하며 호환 아이템은 일반 크기와 관계없이 특수 슬롯 1칸을 사용
- 장비·아이템·carrier 잠금은 해당 대상 자체의 자동 제거/교체만 막고, 잠긴 리그/가방/보안 컨테이너 내부의 정상적인 자동 수납은 허용
- 잠긴 대상이 제거·교체되면 대상 잠금은 함께 사라지며 빈 칸 잠금은 독립적인 예약 공간으로 유지
- 검색 결과 아이템 hover + `T`는 실제 Scanner와 같은 recommendation path를 사용하되 테스트 표시가 자동 만료
- `레이드 종료` 시 레이드 중 변경을 폐기하고 시작 상태로 복귀

기존 편집 기능도 유지됩니다.

- 총기·헬멧·방어구 attachment tree 재귀 편집
- compatible-item picker와 search drag/drop의 공통 compatibility policy
- `ParentInstanceId` 기반 nested bag/rig storage
- Secure Container와 일반 case/container 구분
- product-owned exact multi-grid visual layout의 full signature guard
- occupied one-item slot silent overwrite 금지
- exact source-backed composed/preset image만 사용하며 근거 없는 조립 이미지 합성 금지
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
- Farming Guide recommendation은 session revision과 user lock/reserved state를 존중하고 명시적 수락 전에는 상태를 commit하지 않습니다.
- Map/MiniMap donor는 `SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67`에 고정되어 있습니다.
- user-visible WPF 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE smoke까지 검증합니다.

## Schema / compatibility

```text
Desktop version: 1.15.1
Content schema write: v10
Readable Content schemas: v3~v10
user.db schema: v1
Farming Guide state schema: v2 (reads v1-v2)
Scanner display settings schema: v10
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

v1.15.1은 Farming Guide state schema v2, Scanner display settings v10, Game Content v10을 유지하며 별도 사용자 데이터 migration을 요구하지 않습니다.

## 검증 상태

Exact product source `821def285e2b4964242b50981f6ba6245e996057`은 558 deterministic tests, Windows Release build, self-contained win-x64 publish, actual published EXE Product UI/Farming Guide/Scanner/Map smoke, graceful shutdown, active-async Shutdown Race, package/checksum audit, exact-main Documentation Consistency, exact-main artifact digest verification, automatic Release workflow, public tag/release/assets/latest-stable readback을 통과했습니다.

사용자의 추가 실제 PC/Tarkov 실사용 확인과 김태영 실제 PC diagnostic evidence 수집은 자동화 release verification과 별개이며 현재 `PENDING`입니다.
