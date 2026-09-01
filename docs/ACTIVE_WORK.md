# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **NONE**  
Updated: **2026-09-01 KST**

현재 복구할 진행 중 개발 작업은 없다.

## Recent completion — v1.14.0 Farming Guide assembly / validated storage layouts

v1.14.0은 구현·검증·병합·공개 릴리즈·공개 asset readback까지 완료됐다.

```text
public stable: v1.14.0
exact product release source/tag target:
9ff23b9f50dd84b84ec93cea31b079d7eff70fe1
PR: #251 — MERGED
superseded Draft PR: #250 — CLOSED UNMERGED
validated PR head:
c5ee50ba60f2bc7db461328608ec591f4320ccca
exact-main CI: 33453784868 — SUCCESS
exact-main Shutdown Race CI: 33453784901 — SUCCESS
exact-main Documentation Consistency: 33453784893 — SUCCESS
Release workflow: 33454002732 — SUCCESS
release id: 380133403
527 passed / 0 failed / 0 skipped
```

완료 범위:

- obsolete PMC dogtag equipment surface 제거 + legacy persistence readability 유지
- recursive weapon/helmet/armor assembly editing
- in-page icon-based compatible-item picker
- search drag/drop과 동일 Core compatibility authority 공유
- exact default-preset match 기반 composed image + arbitrary-build deterministic fallback
- `GridLayoutName` / `RigLayoutName` family identity import
- validated exact multi-grid visual placement + signature mismatch compact fallback
- Content snapshot v10 write / v3-v10 read
- Farming Guide state schema v1 유지
- Windows Release build / self-contained publish
- actual published EXE Product UI / Farming Guide / Map smoke
- graceful shutdown / active-async Shutdown Race
- package/checksum verification
- exact-main artifact digest verification
- public tag/release/assets/latest-stable verification

Public package:

```text
Junhyun-Helper.zip
asset id: 538692301
bytes: 80,633,458
SHA-256:
87728ce9e34a30a9b1eb735fe92b1a4a39f172f3b9cf536dfd12d88c8c35667b
```

공식 release evidence:

- `docs/RELEASE_1.14.0.md`
- `docs/.release-v1.14.0-status.json`
- `docs/RELEASE_NOTES_V1.14.0.md`
- `docs/DECISION_V1.14.0_FARMING_GUIDE_ASSEMBLY_AND_AUTHENTIC_LAYOUTS.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

후속 documentation-only commit은 v1.14.0 product source가 아니다. 공개 product identity는 `9ff23b9f50dd84b84ec93cea31b079d7eff70fe1`과 공개 v1.14.0 assets에 고정한다.

자동화 검증과 별개의 외부 확인 항목은 유지보수 evidence가 들어올 때 처리한다.

- 사용자의 실제 PC/Tarkov v1.14.0 실사용 확인: PENDING
- 김태영 실제 PC diagnostic ZIP 수집/분석: PENDING
