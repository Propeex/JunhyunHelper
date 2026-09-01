# Decision — v1.15.1 Farming Guide Real-Play Corrections

Date: **2026-09-01 KST**  
Status: **CONFIRMED / IMPLEMENTED PENDING VERIFICATION**

This decision records product corrections discovered during the first real-play review of the v1.15.0 Farming Guide raid advisor. Where this document conflicts with `DECISION_V1.15.0_FARMING_GUIDE_RAID_ADVISOR.md`, this document supersedes the older behavior.

## 1. Recommendation text is action-only

Mini Scanner already displays the scanned item name, so Farming Guide must not repeat it.

User-confirmed wording:

- store: `[보관할 장소]에 보관`
- replace stored item: `[보관할 장소]의 [기존 아이템 명]과 교체`
- discard: `버리기`
- equip: `[장착할 장소]에 장착`
- replace equipped/attached item: `[장착할 장소]의 [기존 아이템 명]과 교체`
- accepted feedback: `반영 완료`

Manual inventory/lock changes may invalidate pending advice internally but do not show cancellation noise to the user.

## 2. A new scan rejects an unaccepted recommendation

The single-pending-transaction safety model remains, but the user is not forced to accept or explicitly cancel the previous item.

New contract:

`scan A → recommendation A → scan B without acceptance → discard pending A with no state mutation → calculate recommendation B against the unchanged current raid state`

Only explicit accept commits inventory/equipment state.

## 3. Equip and replace-equip are first-class recommendations

The raid advisor evaluates legal empty equipment targets in addition to storage targets. Equipment targets include:

- PMC equipment slots
- rig/backpack/secure-container carrier slots when legal
- recursive weapon/helmet attachment slots
- unlocked armor-plate slots

The advisor may also recommend replacing a lower-priority equipped/attached item when no empty placement is available. Accepted equip and replace-equip actions count toward raid-acquired Needed quantity exactly like accepted storage.

## 4. Lock ownership follows the locked target

Locks constrain automation, not direct user editing.

- item lock: protects that item from automated removal/replacement; moving the same item preserves its instance lock
- equipment/carrier lock: protects the currently equipped target from automated removal/replacement
- removing or replacing the locked target removes that target lock
- empty-cell lock: independent reserved-space constraint and remains until the user unlocks it; primary intended use is reload-magazine headroom

A carrier lock does **not** lock the carrier's internal inventory surface. A locked rig, backpack, or secure container may still receive automatically recommended loot. Likewise, item locking does not turn a nested storage grid into a globally unusable surface.

## 5. Special slots use Tarkov special-slot semantics

Special slots are not generic 1x1 inventory grids.

- eligibility comes from canonical Tarkov item classification (`specialSlot` type), not a hardcoded item list
- an eligible item occupies exactly one special slot regardless of ordinary inventory footprint
- an ineligible item may not be placed in a special slot
- rendering, collision, sanitizer, summary, manual drag/drop, and raid advisor use the same policy
- nested ordinary storage remains ordinary even when its parent item itself occupies a special slot

## 6. Lock interaction must stay lightweight and visually stable

Pressing F for a lock toggle must not rebuild the full Farming Guide page. The lock state and the affected visual are updated directly, while full rerenders reapply lock visuals from state.

A drag/drop rerender must not erase lock highlighting.

## 7. Simulated T scans are temporary presentation

The search-result `T` test scan uses the same Farming Guide decision path as a real scan, but the test Mini Scanner presentation has a bounded lifetime. A later real Scanner presentation invalidates the temporary-hide timer so the test lifecycle cannot hide a newer real scan.

## 8. Equipment editor presentation

- pistol/holster is displayed below eyewear
- inactive raid explanatory text is removed
- storage hint is `R: 회전 · F: 아이템/장비/빈 칸 잠금`
- raw attachment/armor slot identifiers are translated to understandable Korean user-facing labels

## 9. Assembly imagery remains source-authoritative

The program may show a changed weapon/helmet image only when current canonical content contains an authoritative composed/preset image whose exact contained-item signature matches the current assembly.

If no exact source-backed image exists, the program does not fabricate a visually misleading composite. It retains the base item image and installed-part indicators.

## Verification contract

Before v1.15.1 is closed:

- deterministic special-slot eligibility/footprint tests
- pending rejection/replacement and equip transaction tests
- Korean slot-label tests
- full deterministic test suite
- Release build and affected WPF/published-runtime smoke
- PR CI, Shutdown Race CI, Documentation Consistency
- exact-main verification after merge
- public v1.15.1 release/tag/assets and integrity verification
