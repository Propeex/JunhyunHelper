# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 상태는 **v1.17.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.17.1
exact product source/tag target:
4ad1f76ed7c2469e60d0822b229fe03f83c75816
validated PR head:
edd6fa6f5a2edc9d52be84bf1625266d5ad6abec
merge PR: #290 — MERGED
PR CI / Shutdown / Docs:
33826796756 / 33826796665 / 33826796667 — SUCCESS
exact-main CI / Shutdown / Docs:
33827008615 / 33827008595 / 33827008638 — SUCCESS
Release workflow:
33827205735 — SUCCESS
release id: 382428841
published UTC: 2026-09-04T01:49:57Z
485 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 543627042
bytes: 80,573,737
SHA-256:
fad73f3987c04cae73c5a473ccbce6c3a70ff8ca22da04a95a942e66ebea3b6c

SHA256SUMS.txt
asset id: 543627044
bytes: 86
asset SHA-256:
d665b07efa2d3e402937701f903d1eb5da8001feab0b54bcb2a9d8a93e46f9b1
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9920376580
archive bytes: 241,651,630
archive SHA-256:
94cb4670b2889c42efaeaa50874b8bb0a186c3849f09a814184b82609bb2ad22
```

GitHub latest release `v1.17.1` targets exact product source `4ad1f76ed7c2469e60d0822b229fe03f83c75816`, is neither draft nor prerelease, and was published only after the Release workflow re-downloaded the exact-main artifact and independently verified ProductVersion, FIRST_RUN and package checksum identity.

Release evidence:

- `docs/.release-v1.17.1-status.json`
- `docs/RELEASE_NOTES_V1.17.1.md`
- `docs/DECISION_V1.17.1_REMOVE_FARMING_GUIDE.md`
- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`

## v1.17.1 — Farming Guide removed

사용자 결정에 따라 Farming Guide는 제품에서 **완전히 제거**되었습니다. 숨김 또는 비활성화 상태로 남긴 것이 아닙니다.

제거 범위:

- 메인 Farming Guide 탭/페이지
- loadout/inventory editor와 preset
- raid-session farming advisor
- loot/global packing/repacking 판단
- lock/reserved-cell/weight/quantity 흐름
- Scanner → Farming Guide bridge와 simulated scan
- Mini Scanner Farming Guide 지시/수량 입력
- Farming Guide 수락 단축키와 Scanner 설정
- Farming Guide 전용 persistence/service/domain policy
- Farming Guide 전용 Game Content metadata/import와 테스트/스모크

Quest, Hideout, Items/Needed Items, Ammo, Map/MiniMap, Scanner 인식/검색/교정/Ground Truth/진단 기능은 유지됩니다.

기존 사용자 PC의 `%LocalAppData%/JunhyunHelper/farming-guide.json`은 더 이상 읽거나 쓰지 않습니다. 불필요한 사용자 파일 파괴를 피하기 위해 프로그램이 자동 삭제하지는 않습니다.

Current authority: `docs/DECISION_V1.17.1_REMOVE_FARMING_GUIDE.md`.

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

v1.17.1은 위 자동 검증을 완료했습니다. 실제 사용자 PC/Tarkov 실플레이 검증은 별도 `PENDING` evidence이며 공개 릴리즈의 완료 상태나 identity를 변경하지 않습니다.
