# DECISION — v1.7.9 Mini Scanner confirmed-item presentation

상태: **RELEASED / VERIFIED**

기준일: 2026-08-26

## 1. 사용자 실사용 결함

v1.7.8 실사용에서 Scanner 사용자 로그에는 아이템 인식 성공이 기록되지만 Mini Scanner 창이 열리지 않는 현상이 확인되었다.

증상:

```text
Scanner recognition success
→ Item ID confirmed
→ success activity/log written
→ MiniScannerOverlayService.Show(snapshot)
→ Mini Scanner hidden
```

## 2. Root cause

Hidden Mini Scanner initial show가 authoritative Scanner Item identity보다 약한 별도 inventory/stash top-band OCR에 의존했다.

기존 구조:

```text
Scanner semantic success
→ Item ID confirmed
→ presentation snapshot
→ Mini Scanner Show
→ auxiliary inventory/stash top-band OCR
   → enough anchors: show
   → insufficient anchors: remain hidden
```

이 auxiliary OCR은 Item identity proof가 아니며 raid UI 배치/가림/Windows OCR variation에 따라 실패할 수 있었다.

따라서 이미 stronger semantic evidence로 Item ID를 확정한 뒤 weaker auxiliary OCR이 presentation을 veto하는 authority inversion이 root cause였다.

## 3. 제품 동작 결정

확정된 Item presentation의 권위는 Scanner semantic success에 둔다.

현재 구조:

```text
Scanner semantic success
→ Item ID confirmed
→ presentation snapshot
→ Mini Scanner
   ├─ preview/display-test: show
   ├─ already visible: authoritative Item result로 즉시 update
   └─ hidden real Scanner:
        Tarkov foreground yes → show
        Tarkov foreground no  → fail closed / hidden
```

Auxiliary inventory-header OCR은 Mini Scanner 표시를 veto하지 않는다.

## 4. 안전성

Hidden real Scanner의 initial show에는 실제 Tarkov foreground guard를 유지한다.

- `EscapeFromTarkov` main window 존재
- foreground window가 Tarkov
- visible
- non-minimized

이 guard는 presentation safety용이며 Item identity proof가 아니다.

## 5. Sticky presentation 유지

```text
success A → show / miss budget reset
success B → immediate replace / miss budget reset
miss #1 → retain last good
miss #2 → retain last good
miss #3 → hide
```

Progress-only state는 miss로 세지 않는다.

## 6. 변경하지 않은 recognition 계약

v1.7.9는 presentation-only hotfix다.

변경하지 않은 기준:

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

OCR/matcher/visual acceptance도 완화하지 않았다.

## 7. 검증 / release proof

PR #190 final head:

```text
971c27a40566d01651cf14af0f519ceb68c3515a
```

PR CI:

```text
run: 32971624200
CI #1972
result: SUCCESS
380 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
Product UI / Scanner / Map / Factory / MiniMap / graceful shutdown smoke: SUCCESS
Mini Scanner confirmed-item initial visibility policy smoke: SUCCESS
```

Exact v1.7.9 product release source:

```text
bbb04e02385026eba6c77ba0a9d66bad9868cc92
```

Main CI:

```text
run: 32971976531
CI #1973
result: SUCCESS
```

Release workflow:

```text
run: 32972267012
Release #33
result: SUCCESS
```

Public release readback:

```text
release id: 377149426
tag: v1.7.9
target: bbb04e02385026eba6c77ba0a9d66bad9868cc92
asset id: 530823055
asset bytes: 80,468,715
asset SHA-256: bd9285f7d8f819a1cf7f161f72baaae1c32a68f5db2e6f9a305053bbf3852946
draft: false
prerelease: false
```

v1.7.10 이후에도 이 presentation authority contract는 유지한다.
