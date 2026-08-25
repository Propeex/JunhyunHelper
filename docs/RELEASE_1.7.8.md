# 준현 헬퍼 v1.7.8 공개 릴리즈 기록

기준일: 2026-08-26 KST  
상태: **PUBLIC STABLE / VERIFIED**

## 공개 결과

```text
version: v1.7.8
release source: 3ba9d99c43ad143dbc8329e7d29b1d01da335b06
main CI run: 32888653630
release workflow run: 32888935292
release id: 376650517
asset: Junhyun-Helper.zip
asset id: 529666832
asset bytes: 80,469,671
asset SHA-256: 3716d2d3c6d3c9ce2f87c759aac74f6b56b483a09016339c0d8bb6d3bc67e730
checksum asset: SHA256SUMS.txt
checksum asset id: 529666831
published UTC: 2026-08-25T19:20:21Z
```

GitHub readback에서 다음을 확인했다.

- `v1.7.8` tag target = exact release source `3ba9d99c43ad143dbc8329e7d29b1d01da335b06`
- draft = false
- prerelease = false
- GitHub `releases/latest` = v1.7.8
- `Junhyun-Helper.zip`과 `SHA256SUMS.txt`가 모두 존재
- ZIP GitHub asset digest = `sha256:3716d2d3c6d3c9ce2f87c759aac74f6b56b483a09016339c0d8bb6d3bc67e730`

## 릴리즈 목적

v1.7.7 공개 후 실제 레이드에서 Scanner가 상세보기 창을 정상적으로 보고도 item-name OCR 단계에 진입하지 못하는 실사용 회귀를 수정했다.

사용자가 직접 검토한 8개 Ground Truth Case를 분석한 결과:

- 6건은 사용자 교정이 필요한 실패
- 2건은 프로그램 결과가 Ground Truth와 일치
- 실패 6건 모두 detail rectangle과 item-name ROI proposal은 정상
- 실제 화면에는 빨간 X와 돋보기가 모두 존재
- 프로그램은 `HEADER_CLOSE_NOT_LOCKED` / `TITLE_ANCHOR_INCOMPLETE`로 OCR 이전에 중단
- raw OCR은 empty

레이드 인벤토리의 중립색 수평선이 inspect header와 이어져 보이면서 기존 fallback이 실제 상세창보다 47~132px 왼쪽까지 header를 소유한 것이 root cause였다. 이 오차가 magnifier 예상 lane도 함께 이동시켜 semantic gate를 실패시켰다.

## 확정된 수정

기존 정상 Scanner 경로를 우선 유지한다.

```text
existing primary header lock
→ existing live Ground Truth recovery
→ v1.7.8 raid ownership recovery
→ existing contained-subpanel recovery
→ fail closed
```

raid recovery는 기존 경로가 실패하고 강한 `RED_X_CANDIDATE >= 0.90`인 경우에만 진입한다. coarse geometry는 header-left ownership proposal에만 사용하며 Item identity proof가 아니다.

다음 안전 기준은 그대로 유지한다.

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

red close-X, neutral header, magnifier, dark title field, title text evidence를 독립적으로 다시 검증하고 최종 semantic score 0.68을 통과한 경우에만 OCR로 진행한다. OCR variants, catalog matcher acceptance, visual recovery acceptance는 변경하지 않았다.

## Scanner UI

사용자 요청으로 일반 Scanner 화면의 primary actions를 다음 순서로 변경했다.

```text
스캐너 ON/OFF
설정
고급
현재 결과 교정
```

`현재 결과 교정`은 최신 exact in-memory Scanner frame을 기존 교정 창으로 연다. 고급 창에서는 중복 버튼을 제거하고 테스트 스캐너, 교정 데이터 관리, Scanner 성능 진단 자료 내보내기만 유지한다.

v1.7.7의 사용자 선택형 Ground Truth 저장, legacy automatic sample 안전 정리, 반복 실패 activity collapse, Scanner/Map 공통 hotkey 계약은 그대로 유지한다.

## 검증

PR #188 최종 HEAD:

```text
52fbeaf6d56cf01631325ba3d65a1f018e9eb138
PR CI run: 32886379050 — SUCCESS
```

검증 결과:

```text
Desktop Release build: SUCCESS
Tests: 380 passed / 0 failed / 0 skipped
Windows x64 self-contained publish: SUCCESS
Product UI / Scanner / Map / Factory / MiniMap smoke: SUCCESS
Graceful shutdown: SUCCESS
Release package verification: SUCCESS
Artifact upload: SUCCESS
```

병합 후 exact release source `3ba9d99c43ad143dbc8329e7d29b1d01da335b06`에서 main CI `32888653630`이 동일 gate를 다시 통과했다. 성공한 exact main CI artifact만 Release workflow `32888935292`가 받아 ProductVersion, `FIRST_RUN_KO.txt`, checksum/package identity를 검증한 뒤 v1.7.8을 공개했다.

공식 설계 결정은 `docs/DECISION_V1.7.8_RAID_HEADER_LOCK_2026-08-26.md`, 사용자 변경 설명은 `docs/RELEASE_NOTES_V1.7.8.md`, machine-readable 공개 상태는 `docs/.release-v1.7.8-status.json`을 기준으로 한다.
