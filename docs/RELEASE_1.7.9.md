# RELEASE — v1.7.9

기준일: 2026-08-26
상태: **PUBLIC STABLE / VERIFIED**

## 목적

v1.7.8 실사용에서 Scanner 로그에는 아이템 인식 성공이 기록되지만 Mini Scanner 창이 열리지 않는 presentation 회귀를 수정한 유지보수 PATCH다.

## Root cause

Scanner semantic pipeline은 Item ID를 정상 확정하고 성공 로그를 기록한 뒤 `MiniScannerOverlayService.Show(snapshot)`까지 호출했다.

그러나 hidden Mini Scanner의 initial show가 별도의 top-band inventory/stash OCR을 다시 수행했다. 이 auxiliary OCR이 `장비`, `건강상태`, `스킬`, `지도`, `종합정보` 계열 중 2개 이상을 인식하지 못하면 이미 확정된 Item 결과도 표시하지 않았다.

따라서 recognition failure가 아니라 presentation-only failure였다.

## 수정

- confirmed Item presentation에서 auxiliary inventory-header OCR veto 제거
- hidden real Scanner initial show는 Tarkov client foreground 여부만 fail-closed guard로 유지
- preview/display-test 동작 유지
- visible Mini Scanner는 authoritative Scanner Item success로 즉시 갱신
- v1.7.2 sticky presentation / 3회 실제 miss retention 유지
- Scanner recognition threshold/candidate cap/OCR/matcher/visual acceptance 변경 없음
- Product smoke에 confirmed-item initial visibility policy 추가

## Recognition 안전 불변식

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

- false positive보다 miss 선호
- stale/cross-frame identity proof 금지
- scan-time network 없음
- game memory read / DLL injection / packet interception / process hook 없음

## PR 검증

PR: #190

Final PR HEAD:

```text
971c27a40566d01651cf14af0f519ceb68c3515a
```

PR CI:

```text
run: 32971624200
result: SUCCESS
```

검증 결과:

- Desktop Release build SUCCESS
- 380 passed / 0 failed / 0 skipped
- Windows x64 self-contained publish SUCCESS
- Product UI / Scanner / Map / Factory / MiniMap smoke SUCCESS
- Mini Scanner confirmed-item initial visibility policy smoke SUCCESS
- graceful shutdown SUCCESS
- release package verification SUCCESS
- artifact upload SUCCESS

## Public release source

PR #190 병합 결과:

```text
bbb04e02385026eba6c77ba0a9d66bad9868cc92
```

이 커밋이 v1.7.9의 정확한 제품 릴리즈 소스/tag target이다.

Main CI:

```text
run: 32971976531
result: SUCCESS
```

Stable Release workflow:

```text
run: 32972267012
result: SUCCESS
```

## GitHub Release readback

```text
release id: 377149426
tag: v1.7.9
target_commitish: bbb04e02385026eba6c77ba0a9d66bad9868cc92
draft: false
prerelease: false
releases/latest: v1.7.9
published_at: 2026-08-26T13:07:15Z
```

## Public assets

### Junhyun-Helper.zip

```text
asset id: 530823055
bytes: 80,468,715
SHA-256: bd9285f7d8f819a1cf7f161f72baaae1c32a68f5db2e6f9a305053bbf3852946
```

### SHA256SUMS.txt

```text
asset id: 530823056
bytes: 86
```

## 최종 판정

v1.7.9는 **PUBLIC STABLE / VERIFIED**다.

현재 제품 상태는 계속 **PRODUCT COMPLETE / MAINTENANCE MODE**다.
