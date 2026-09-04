# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 상태는 **v1.17.2 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.17.2
exact product source/tag target:
73f0386a45818408c2a68530b90de7946ecaf1d1
validated PR head:
121d060db102eed0f4af241ef5f37c51164c6a04
merge PR: #292 — MERGED
PR CI / Shutdown / Docs:
33840328932 / 33840328963 / 33840329237 — SUCCESS
exact-main CI / Shutdown / Docs:
33840553320 / 33840553329 / 33840553303 — SUCCESS
Release workflow:
33840780902 — SUCCESS
release id: 382500195
published UTC: 2026-09-04T05:31:31Z
488 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 543847934
bytes: 80,554,487
SHA-256:
a64d202046505273964b0735976d71e382624c68f16699c6844b193599b43971

SHA256SUMS.txt
asset id: 543847933
bytes: 86
asset SHA-256:
a105826dcc518a58412a521b221a2e7842ccfb716662418981005b4d276505a0
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9924825161
archive bytes: 241,595,886
archive SHA-256:
864f971ebe799df881ac4d69318ae331cd3c4c4e783013836bceaacb33232ba4
```

GitHub latest release `v1.17.2` directly targets exact product source `73f0386a45818408c2a68530b90de7946ecaf1d1`, is neither draft nor prerelease, and was published only after the Release workflow re-downloaded the exact-main artifact and verified ProductVersion, FIRST_RUN and package checksum identity.

Release evidence:

- `docs/.release-v1.17.2-status.json`
- `docs/RELEASE_NOTES_V1.17.2.md`
- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`

## v1.17.2 — Product Purity Cleanup

이번 PATCH는 새 기능 추가나 성능 최적화가 아닙니다.

현재 제품에서 실제 역할이 없다고 증명된 잔재만 제거했습니다.

주요 정리:

- 숨은 구형 UI와 runtime repair/rebinding 경로 제거
- MainWindow/Profile/Ammo/Quest/Hideout/Items/Scanner lifecycle 소유권 정리
- 구형 Scanner standalone debug/settings/hotkey UI 제거
- Mini Scanner의 사용되지 않는 preview/position-edit 잔재 제거
- updater/package의 전환기 compatibility fallback 제거
- 오래된 current-state 문서와 중복 canonical fact 정리
- 제거된 구조를 요구하던 회귀 테스트 갱신
- 감사 중 발견된 Items 정리 필요 표시 갱신 회귀 수정

Quest, Hideout, Items, Ammo, Map/MiniMap, Scanner 인식/검색/교정/Ground Truth/진단과 pinned Map donor 계약은 유지됩니다.

## Farming Guide

Farming Guide는 v1.17.1에서 사용자 결정에 따라 완전히 제거되었으며 v1.17.2에서도 제거 상태를 유지합니다.

기존 사용자 PC의 `%LocalAppData%/JunhyunHelper/farming-guide.json`은 더 이상 읽거나 쓰지 않으며 자동 삭제하지 않습니다.

## 검증 계약

중요한 제품 변경은 변경 성격에 따라 다음을 검증합니다.

- deterministic tests
- Windows Release build
- win-x64 self-contained publish
- 실제 published EXE Product UI / Map / Scanner runtime smoke
- graceful shutdown
- active-async Shutdown Race
- package / SHA256SUMS 검증
- PR 및 exact-main CI
- public tag / release / asset identity 및 digest 검증

v1.17.2는 위 자동 검증을 완료했습니다. 실제 사용자 PC/Tarkov 실플레이 검증은 별도 `PENDING` evidence이며 공개 릴리즈의 완료 상태나 identity를 변경하지 않습니다.
