# ACTIVE WORK — 현재 진행 중 작업 체크포인트

Status: **NONE**  
Updated: **2026-09-01 KST**

현재 진행 중인 구현 작업은 없습니다.

## Last completed work

**v1.15.3 Farming Guide storage and scan simulation PATCH**

Public stable:

```text
version/tag: v1.15.3
exact product source/tag target:
c35204da66eb0af454b50550c830b071a0897835
merge PR: #265
validated PR head: db82512e6e723f2d85ed0ddf3f3c7c9b0e3a70af
PR CI / Shutdown / Docs:
33487099126 / 33487099119 / 33487099201 — SUCCESS
exact-main CI / Shutdown / Docs:
33487466031 / 33487466005 / 33487465946 — SUCCESS
Release workflow: 33487795730 — SUCCESS
release id: 380333729
published UTC: 2026-09-01T08:35:55Z
563 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 539249489
bytes: 80,659,355
SHA-256: a22a426de32aa20a4c158018d98a6eec96b39d460d367d33d9d970d7e2581d99

SHA256SUMS.txt
asset id: 539249490
bytes: 86
asset SHA-256: 286e27a9db1394d1a4487c5b26598f08998bb03e07e21fa116dc4fca5844fdde
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9792459273
bytes: 241,909,375
SHA-256: c0aba02d6a465734c841b044776dfcf087bab9b29141b23c71ffb5a0a65c6cb2
```

Completed scope:

- 일반 stored item의 기본 테두리를 neutral로 복원하고 명시적 `F` lock에만 accent/yellow 표시
- current validated Game Content의 실제 `StorageGrids`를 가진 모든 stored container로 nested-storage surface 일반화
- source-backed allowed/excluded item/category filter를 manual placement, sanitizer, raid planning에 공통 적용
- Secure Container 안 specialized container 및 container-in-container 재귀 상태 유지
- compatible positive-allow-list nested grid를 general root empty storage보다 우선하는 raid placement
- 검색 결과 hover + `T` simulated scan의 Search TextBox focus 회귀 수정
- Scanner capture가 비활성/미초기화 상태여도 verified same-mode local catalog를 on-demand 사용
- v1.15.2 complete-equipment boundary 유지
- Release build, 563 deterministic tests, self-contained Windows x64 publish, published EXE runtime smoke, graceful shutdown, Shutdown Race, package/checksum, exact-main CI/docs, public tag/release/assets 검증 완료

Canonical evidence:

- `docs/PROJECT_STATE.json`
- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
- `docs/PRODUCT.md`
- `docs/DECISIONS.md`
- `docs/DECISION_V1.15.3_SPECIALIZED_NESTED_STORAGE.md`
- `docs/RELEASE_1.15.3.md`
- `docs/.release-v1.15.3-status.json`
- `docs/RELEASE_NOTES_V1.15.3.md`

## External evidence not treated as active development

Automated v1.15.3 release validation is complete. 다음 실환경 증거는 별도 유지보수 요청이 있을 때 다시 시작합니다.

- 사용자 실제 Tarkov 플레이에서 v1.15.3 Farming Guide 시각/동작 검증
- 김태영 actual-PC diagnostic ZIP collection/analysis

새 작업은 `AGENTS.md` → `docs/PROJECT_STATE.json` → 이 파일 순으로 복구하고, 현재 public stable v1.15.3에서 시작합니다.
