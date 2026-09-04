# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 상태는 **v1.17.3 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

공식 프로젝트 기억은 대화가 아니라 저장소의 문서·코드·테스트·GitHub 상태입니다.

- `docs/PROJECT_STATE.json` — 현재 사실값의 canonical source
- `docs/ACTIVE_WORK.md` — 진행 중 작업 체크포인트
- `docs/CURRENT_STATE.md` — 현재 상태 요약
- `docs/STATE.md` — 상세 구현/검증 상태
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` / `docs/DECISION_*` — 주요 결정

## 현재 공개 릴리즈

```text
version/tag: v1.17.3
exact product source/tag target:
8ec677b1552f9deed55f98931c1df317e9bc4a4b
validated PR head:
230a5284f58f9d5eb8954c6042164bc5635fd35c
merge PR: #294 — MERGED
PR CI / Shutdown / Docs:
33846545486 / 33846545485 / 33846545484 — SUCCESS
exact-main CI / Shutdown / Docs:
33846852935 / 33846852933 / 33846852922 — SUCCESS
Release workflow:
33847077606 — SUCCESS
release id: 382534812
published UTC: 2026-09-04T07:04:53Z
503 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 543938413
bytes: 80,560,157
SHA-256:
1384f2d42b843617ed61f90d4b2b0c5aa46bc616fd54e808cafabef2eb24f1f7

SHA256SUMS.txt
asset id: 543938412
bytes: 86
asset SHA-256:
4944f6e04b6ae191272db805dd8b60c8ef82fd6d7c0e4f4629e53d41755f5b0a
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9926904439
archive bytes: 241,611,421
archive SHA-256:
ce1946f12f8da5de755ac91696f2f1ed1b137bf76da5a32b198c36c0228e12a3
```

GitHub latest release `v1.17.3` directly targets exact product source `8ec677b1552f9deed55f98931c1df317e9bc4a4b`, is neither draft nor prerelease, and was published only after the Release workflow re-downloaded the exact-main artifact and verified ProductVersion, FIRST_RUN and package checksum identity.

Release evidence:

- `docs/.release-v1.17.3-status.json`
- `docs/RELEASE_NOTES_V1.17.3.md`
- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`

## v1.17.3 — Stability, Optimization and UI Finishing

이번 PATCH는 새 사용자 기능을 추가하지 않고 현재 기능 집합을 더 안정적이고 효율적으로 유지하기 위한 전제품 유지보수입니다.

주요 변경:

- Quest/Hideout/Items의 반복 content lookup 인덱스화와 하나의 authoritative profile snapshot 기반 workspace 갱신
- Scanner catalog/content/requirement lookup 재사용 및 반복 전체 순회 제거
- 공용 이미지 cache의 동일 path single-flight와 weak decoded-image 재사용
- Map Quest marker의 120ms polling 제거 및 ScaleTransform 변경 이벤트 기반 갱신
- 수동/자동/Map/최초복구 content update의 공용 operation gate 직렬화
- MainWindow 및 updater의 종료 cancellation 강화
- mutation 실패와 Hideout rollback 취소 시 authoritative UI 복구
- 공유 Button keyboard focus 표현 보강
- 주요 WPF page clipping/scrolling/virtualization 재검토

Scanner recognition thresholds/pacing/matcher safety, pinned Map donor revision, supported schema/read compatibility와 사용자 데이터 의미는 유지됩니다.

## Farming Guide

Farming Guide는 v1.17.1에서 사용자 결정에 따라 완전히 제거되었고 현재도 제거 상태입니다.

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

v1.17.3은 위 자동 검증을 완료했습니다. 실제 사용자 PC/Tarkov 실플레이 검증은 별도 `PENDING` evidence이며 공개 릴리즈의 완료 상태나 identity를 변경하지 않습니다.
