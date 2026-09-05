# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 상태는 **v1.17.4 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.17.4
exact product source/tag target:
2297a27332069e18ade56c53931002f7a4728338
validated PR head:
5ba3c504e4da8b8758b685715498437d3a7862b2
merge PR: #295 — MERGED
PR CI / Shutdown / Docs:
33939249250 / 33939249290 / 33939249230 — SUCCESS
exact-main CI / Shutdown / Docs:
33939474734 / 33939474738 / 33939474753 — SUCCESS
Release workflow:
33939616674 — SUCCESS
release id: 383108819
published UTC: 2026-09-05T02:38:16Z
504 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 545248484
bytes: 80,559,673
SHA-256:
bc174bfe1e58aee46fe8af4aeb3d9f680ac2320b09c8fab70f112914e1f076aa
```

## v1.17.4 — Mini Scanner FIR 필요량 분리 표시

Mini Scanner의 기존 `필요 아이템 개수` 정보는 더 이상 FIR 필요량과 그 외 현재 필요량을 하나의 합계로만 표시하지 않습니다.

표시 형식:

```text
<FIR 필요량>(인레이드) + <그 외 현재 필요량>개
```

예:

- `3(인레이드) + 4개`
- `0(인레이드) + 4개`
- `4(인레이드) + 0개`

Items planner의 기존 `RemainingTotal` / `RemainingFir`가 authority이며, Quest/Hideout 필요량 계산·FIR 의미·inventory accounting·Scanner recognition·catalog·persistence·Mini Scanner 정보 순서와 레이아웃은 변경하지 않았습니다.

Farming Guide는 v1.17.1에서 제거된 상태를 계속 유지합니다.

## 검증

v1.17.4는 504 deterministic tests, Windows Release build, win-x64 publish, 실제 published EXE Product UI / Map / Scanner smoke, graceful shutdown, Shutdown Race, package/checksum, exact-main CI와 public release/tag/asset identity 검증을 완료했습니다.
