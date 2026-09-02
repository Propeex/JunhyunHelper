# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-09-02 KST**  
상태: **v1.16.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.16.1
exact product source/tag target:
7fb148434d22fac823d57d88021f9615081c47cd
validated PR head: 7d7cf002aa4f1d61c891b340ff73c56781655d64
merge PR: #276
PR CI / Shutdown / Docs:
33589038565 / 33589038575 / 33589038576 — SUCCESS
exact-main CI / Shutdown / Docs:
33589274983 / 33589275133 / 33589275021 — SUCCESS
Release workflow: 33589497077 — SUCCESS
release id: 380969416
published UTC: 2026-09-02T04:06:31Z
612 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 540589667
bytes: 80,717,818
SHA-256: 8599645a2d0a38c6b74f4f79cab71120b26e378da254a98605610f1c7493b3c3

SHA256SUMS.txt
asset id: 540589668
bytes: 86
asset SHA-256: c78b0be06dbcf3f5239591d796f3b6a94299445e45157012ee122972cbfcaeee
```

Exact-main Actions artifact:

```text
JunhyunHelper-win-x64
artifact id: 9831224038
bytes: 242,086,160
SHA-256: 74435818344f94d6cd9d8fb918582dbdb3b047e789aa0f2f47c398facfbabd2a
```

`v1.16.1` is `draft=false`, `prerelease=false`, and its target is the exact product source above. The release workflow re-downloaded the exact-main Actions artifact with digest verification and compared the manifest hash against the actual `Junhyun-Helper.zip` hash before publication.

## v1.16.1 maintenance hardening

### Farming Guide state recovery

Syntactically valid but semantically partial/null `farming-guide.json` state is now normalized safely. Valid equipment, presets and stored items are salvaged where possible; unusable entries are discarded rather than causing later null failures. Nested attachment/armor-plate state, locks, stack quantity, Strength settings and fixed equipment are normalized within the existing schema and product contracts.

A deterministic regression loads a deliberately partial state document, verifies salvage/normalization, saves it, and reloads it successfully.

### Profile/content async consistency

Opportunistic startup content-schema refresh now captures the initiating `ProfileId + GameMode` and revalidates that identity after asynchronous boundaries. If the user has switched profiles, the old operation may not take over busy state or apply refreshed content/workspaces to the new profile.

A maintenance source-contract regression preserves these identity guards.

### Product/UI maintenance review

The pass reviewed MainWindow profile/update/lifecycle flows, Farming Guide persistence/nested storage/workbench/quantity/weight, Scanner runtime/settings/UI-state, Map/MiniMap settings/window state, atomic storage/content activation/image cache, updater/service disposal and existing rendered WPF runtime-smoke coverage.

No additional user-visible defect was reproduced strongly enough to justify changing confirmed layout or behavior. Existing published EXE smoke again passed Scanner/Ammo/Farming Guide/Quest/Map-related product surfaces, Map runtime evidence and graceful shutdown.

## Farming Guide current contract — v1.16.0 behavior retained

### Deterministic rulebook

Farming Guide no longer treats loot as a weighted score. The product contract is **제약 확인 → 중요도 비교 → 상황 대처 결정**. Illegal Tarkov states, locks, reserved cells, protected carrier-role migration and final-state weight constraints are hard gates before priority comparison.

### Priority and economics

- Only items whose requirement specifically needs **Found in Raid** receive special needed priority.
- Non-FIR needed items are ordinary economic loot for Farming Guide purposes.
- Economic value is **average Flea Market price**.
- If space requires destructive replacement, compare the incoming item's total Flea value against the total Flea value of the actual sacrificed set; do not use universal item-to-item ₽/slot ranking.
- Quantity-dependent items use their entered quantity for needed count, total value and weight.

### Equipment superiority

- body armor / helmet: armor class
- headset: hearing distance
- ordinary rig / backpack / secure container: storage capacity
- armored rig: armor class first, then storage capacity only when armor class is equal
- weapon / pistol: no automatic superiority replacement

Unknown durability, remaining uses and firearm assembly state are not inferred.

### Protected state and carrier replacement

Protected state consists only of **locked items and reserved cells**. Storage-bearing equipment replacement must preserve locked item instances and recreate equivalent connected reserved-cell shapes/capacity in the replacement carrier. If that protected state cannot legally migrate, replacement is forbidden.

### Stack quantity

Ammo/currency and other authoritative quantity-dependent items request quantity before Mini Scanner Farming Guide recommendation. Stored stack quantity is persisted in Farming Guide state schema v3, displayed on item cards and editable by double-click. New scan cancels stale pending quantity input.

### Weight

The Farming Guide footer exposes current modeled weight and the Strength-based carry limit. Strength level is persisted per profile. Final proposed state must satisfy the configured carry-weight rule; if the current manually reflected state is already above the limit, a recommendation may only preserve or reduce that weight until the state returns under the limit.

### MiniMap hotkey cleanup

Bare NumPad 0–5 no longer trigger direct floor selection. Existing configurable floor-up/floor-down hotkeys remain available. The donor-compatible hook lifecycle is preserved.

### Runtime stability fix retained from v1.16.0

The first v1.16 release candidate created a WPF `LayoutUpdated` feedback loop by repeatedly assigning unchanged weight/badge presentation values. This starved Dispatcher `ContextIdle`, preventing Map runtime smoke evidence from executing. Presentation refresh remains idempotent: UI properties change only when the rendered value actually differs. v1.16.1 PR and exact-main published-EXE smoke both pass.

## Schema

```text
Desktop: 1.16.1
Content write/read: v11 / v3-v11
user.db: v1
Farming Guide state: v3
Scanner display settings: v10
Scanner catalog write/read: v4 / v1-v4
```

## Canonical references

- `docs/.release-v1.16.1-status.json`
- `docs/RELEASE_NOTES_V1.16.1.md`
- `docs/DECISION_FARMING_GUIDE_RULEBOOK_V1_16.md`
- `docs/PROJECT_STATE.json`
- `docs/STATE.md`

## External validation still pending

Automated release validation is complete. Separate actual-PC/Tarkov real-play validation remains `PENDING`; it does not alter the verified public v1.16.1 release identity above.
