# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 공개 안정판은 **v1.17.0**이며, **v1.17.1에서 Farming Guide를 완전히 제거하는 PATCH 작업이 진행 중**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.17.0
exact product source/tag target:
8b0e1f8f46fa3822f4cff05b7be3223d40ad7435
validated PR head: a01d61cd9957db94a7475734c1e8df66ce71f53d
merge PR: #288 — MERGED
PR CI / Shutdown / Docs:
33746966753 / 33746966804 / 33746966771 — SUCCESS
exact-main CI / Shutdown / Docs:
33748900315 / 33748900348 / 33748900377 — SUCCESS
Release workflow: 33749193376 — SUCCESS
release id: 381959220
published UTC: 2026-09-03T11:21:35Z
649 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 542663027
bytes: 80,766,362
SHA-256:
6ecc3a61d0b492f6b475e18f309e55790776911e5496fc704d12ffd611c629cb

SHA256SUMS.txt
asset id: 542663026
bytes: 86
asset SHA-256:
7a2fb4f7ebcb333eafd8cad6f9acbf532549118e608776786666014a24875bdf
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9890816795
archive bytes: 242,234,759
archive SHA-256:
d9115f24968804fc5b4e65fa7bbaaf008f4af516e044f3b00e0ee6b4525a15dd
```

GitHub release `v1.17.0` targets exact product source `8b0e1f8f46fa3822f4cff05b7be3223d40ad7435`, is neither draft nor prerelease, and was published only after the Release workflow re-downloaded the exact-main artifact, verified ProductVersion/FIRST_RUN identity, and matched the actual release ZIP hash against `SHA256SUMS.txt`. Later documentation-only commits are not v1.17.0 product sources and must not replace these stable assets.

Release evidence:

- `docs/.release-v1.17.0-status.json`
- `docs/RELEASE_NOTES_V1.17.0.md`
- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`


## v1.17.1 Farming Guide removal

사용자 결정에 따라 Farming Guide는 제품에서 완전히 제거됩니다.

제거 범위:

- 메인 Farming Guide 탭/페이지
- loadout/inventory editor, preset, lock/reserved-cell/weight/quantity UI
- raid-session advisor와 loot/global packing/repacking 판단
- Scanner → Farming Guide bridge와 simulated scan
- Mini Scanner Farming Guide 지시 항목
- Farming Guide 수락 단축키/Scanner 설정
- Farming Guide 전용 persistence/service/domain policy
- Farming Guide 전용 Game Content metadata/import와 테스트/스모크

Quest, Hideout, Items/Needed Items, Ammo, Map/MiniMap, Scanner 인식/검색/교정/진단은 독립 기능으로 유지됩니다.

기존 사용자 PC에 남아 있는 `%LocalAppData%/JunhyunHelper/farming-guide.json`은 더 이상 읽거나 쓰지 않습니다. 프로그램이 자동 삭제하지는 않습니다.

현재 제품 결정 authority는 `docs/DECISION_V1.17.1_REMOVE_FARMING_GUIDE.md`입니다. 이전 Farming Guide 결정 문서는 역사 기록일 뿐 현재 제품 동작을 정의하지 않습니다.

## 검증 계약

중요한 제품 변경은 가능한 범위에서 다음을 통과해야 합니다.

- deterministic tests
- Release build
- Windows x64 self-contained publish
- 실제 published EXE Product UI / Map / Scanner runtime smoke
- graceful shutdown
- Shutdown Race CI
- package / SHA256SUMS 검증
- PR 및 exact-main CI
- 공개 tag / release / asset identity 및 digest 검증

현재 v1.17.0은 위 자동 검증을 완료했습니다. 실제 사용자 PC/Tarkov 실플레이 검증은 별도 `PENDING` 상태이며 공개 릴리즈 identity나 완료된 개발 상태를 변경하지 않습니다.
