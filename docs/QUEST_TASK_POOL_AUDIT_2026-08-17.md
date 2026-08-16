# EFT 1.1 trader task-pool prerequisite audit — 2026-08-17

## Status

**AUDIT COMPLETE / PRODUCT POLICY CONFIRMED — DEC-044**

This document records evidence gathered after v0.1.6 when many quests appeared as `확인 필요` because their live `otherRequirements` contained `globalVariable`. It intentionally does **not** authorize guessing unknown profile-variable values. DEC-044 now defines the product boundary: exact read-side conditions are supported, while unobserved values and undocumented server write rules remain conservative.

## Confirmed implementation policy — 2026-08-17

The user confirmed that JunhyunHelper should implement every exact part of this mechanism and leave only the genuinely unknowable part conservative. DEC-044 therefore adopts the following boundary:

- preserve and evaluate the exact read-side `globalVariable` condition,
- persist exact EFT profile `Variables` values when a safe observation/import source provides them,
- never invent a missing variable value from trader LL or completed-task counts,
- keep only the unobserved current value / undocumented server write rule as `확인 필요`,
- fail closed if a future patch changes the supported `>= integer` shape.

This supersedes the pre-decision option analysis below where it conflicts with DEC-044.

## Executive conclusion

The live `globalVariable` requirements are not 162 unrelated opaque mechanics.

For each of regular / PvE / PvP Season, the current live `json.tarkov.dev` task feed contains:

- 162 `globalVariable` requirement usages,
- affecting 162 quests,
- but only **27 unique profile-variable IDs**,
- all 27 use only `>=` comparisons with small integer thresholds from 1 through 5,
- every variable is used by quests from exactly one of the seven classic progression traders,
- the split is Prapor 4, Therapist 4, Skier 4, Peacekeeper 4, Mechanic 4, Jaeger 4, Ragman 3.

This structure aligns very strongly with Battlestate Games' announced EFT 1.1.0.0 side-task rework: most side tasks are no longer strict chains, task sets are tied to trader loyalty levels, and additional sets become available as progression advances.

## Why current JunhyunHelper behavior is too coarse

JunhyunHelper v0.1.6 stores only the unsupported requirement type name (`globalVariable`) in `QuestDefinition.UnsupportedAvailabilityRequirementTypes`. The live feed actually preserves three pieces of information that are currently discarded:

- `variableId`
- `compareMethod`
- `value`

Regardless of the final task-pool implementation, future content schema should preserve this payload instead of reducing it to the type name.

## Live structural findings

### The global variable is the real gate

For all 162 global-variable-gated quests in the regular-mode audit:

- no ordinary `taskRequirements` are present,
- no `traderRequirements` are present,
- the global-variable condition is therefore not redundant with an existing supported prerequisite.

Ignoring it or treating it as automatically true would incorrectly expose quests.

### Per-trader staged pools

Within each trader, the variable IDs form 3–4 distinct staged groups. Thresholds divide the quests into small batches. Representative examples:

#### Prapor

Pool 1 (`6a20540cf1b67a977cc5a088`):

- >=1: Bad Rep Evidence, Shootout Picnic
- >=3: Background Check, Belka and Strelka, Luxurious Life
- >=5: BP Depot, Shaking Up the Teller, Test Drive - Part 2

Pool 2 (`6a2688488bba18e0b0187a04`):

- >=3: Anesthesia, Postman Pat - Part 1, Properties All Around
- >=5: Delivery From the Past, Kings of the Rooftops, Test Drive - Part 3

Pool 3 (`6a32651a811905ed0cac0973`):

- >=1: Documents, Special Comms, Test Drive - Part 5
- >=3: No Offence, Reconnaissance, Test Drive - Part 1

Pool 4 (`6a326525789ae12ecb0b2807`):

- >=1: Best Job in the World, Intimidator, Test Drive - Part 6
- >=2: Escort and one newly introduced task

#### Mechanic

The four variables similarly progress from low/medium Gunsmith and Farming tasks to high-tier tasks. The fourth pool contains A Shooter Born in Heaven.

#### Therapist

The four variables progress from Shortage / Operation Aquarius-era tasks through progressively later groups; the fourth pool currently gates Crisis at >=1.

Equivalent staged patterns exist for Skier, Peacekeeper, Ragman and Jaeger.

## Loyalty-level seed batches

The same live feed also contains direct, supported trader-loyalty gates that form plausible initial batches at LL2–LL4, plus root/dialogue-gated LL1 entries.

Examples:

- Mechanic LL2: Signal - Part 2, Broadcast - Part 1, Watching You, Black Swan
- Mechanic LL3: Gunsmith - Part 6/7, Surplus Goods, Back Door
- Mechanic LL4: Psycho Sniper, Calibration, Gunsmith - Part 14 and other high-tier entries
- Jaeger LL2: five direct LL2 tasks
- Jaeger LL3: four direct LL3 tasks
- Jaeger LL4: three direct LL4 tasks
- Peacekeeper LL2: four direct LL2 tasks
- Ragman LL2: four direct LL2 tasks

When the per-trader variables are ordered in their observed creation/ID order, the staged pools line up naturally with LL1 → LL4 (Ragman currently exposes only three staged pools). For every trader, the first threshold of the corresponding pool is reachable from the size of the initial LL batch; later thresholds are then reachable by completing quests from earlier threshold batches.

Example shape:

```text
reach trader LL
  -> initial LL batch is available
  -> complete N tasks
  -> profile variable reaches threshold N
  -> next 2–4 tasks become available
  -> complete more tasks
  -> next threshold batch becomes available
```

This is a strong model of the observed data and the announced 1.1 progression design.

## What is proven vs inferred

### Proven from current public data

- 162 usages collapse to 27 unique IDs.
- IDs are trader-local.
- comparisons are only `>=` small integers.
- the global condition is the sole non-level gate on those quests.
- direct trader-LL seed batches coexist with the staged global-variable batches.
- all three game modes currently exhibit the same 162 / 27 structure.
- public `json.tarkov.dev` endpoints expose these IDs only inside task requirements; no public variable-definition table was found in `tasks`, `traders`, `globals`, `areas` or `achievements`.

### Strong inference, not publicly authoritative

- the 27 IDs are EFT 1.1 per-trader/per-LL side-task progression counters,
- completing an eligible task in the associated LL pool increments the counter,
- ordered per-trader variables correspond to LL1, LL2, LL3, LL4 (Ragman has no fourth staged pool in the current data),
- the counter value can therefore probably be reconstructed from completed quests in the associated pool.

The structural fit is very strong, but the exact **write rule** is not present in the public task feed.

## Public-data boundary

The current `the-hideout/tarkov-data-manager` obtains raw quest data from an authenticated Fence endpoint. The public transformed feed exposes the read-side requirement (`variableId >= value`) but not an authoritative rule stating which task completion writes/increments which profile variable.

A current public definition mapping such as:

```text
variable X = Prapor LL2 side-task completion count
quest Y completion increments variable X
```

was not found.

The EFT client model confirms that these conditions read per-profile integer variables, but the exact EFT 1.1 backend update rule is server-side data/logic that is not exposed by the public endpoints audited here.

Therefore a fully generic future-proof converter cannot currently derive the write rule from the public API alone without inference.

## Product options to discuss

### Option A — audited current-version task-pool model (recommended if exact current behavior is prioritized)

- preserve raw global-variable payload,
- recognize the audited 27 current pool IDs,
- map them to trader + LL pool semantics,
- reconstruct pool progress from completed quests,
- validate the exact expected structure on every content update,
- if IDs/shape change, fail closed to `확인 필요` rather than guessing,
- optionally use EFT TaskStarted/TaskFinished logs as reality-sync evidence.

This can eliminate most/all current 162 false `확인 필요` entries while remaining conservative on future patches, but the mapping is a compatibility rule for current EFT data rather than a purely generic transformation.

### Option B — strict public-data-only semantics

- preserve raw payload,
- do not infer the hidden counter,
- use EFT task logs to override quests actually observed as started/completed,
- retain `확인 필요` for unobserved future availability.

This is maximally defensible but leaves many future quests indeterminate.

### Option C — heuristic generic inference

Infer trader + LL pools solely from variable ordering and task-batch structure on every update.

This is not recommended as the sole rule because the public schema does not promise that variable ObjectId/order encodes loyalty level. It could silently produce wrong availability after a content change.

## Recommended architecture regardless of product choice

1. Add a structured canonical availability requirement model instead of storing only unsupported type names.
2. Preserve raw `globalVariable` ID/operator/value.
3. Add a resolver layer that may convert known raw conditions into semantic conditions.
4. Keep unknown/new conditions fail-closed as `Indeterminate`.
5. Add content-update validation for recognized pool structure and mapping drift.
6. Later use EFT logs (`TaskStarted`, `TaskFailed`, `TaskFinished`) as observed facts, not as a substitute for the static progression model.

## Audit execution

The audit used temporary GitHub Actions workflows on `agent/global-variable-audit-2026-08-17` to fetch the current live regular / PvE / PvP Season feeds, compare requirement structures, trace variable IDs across public endpoints, and compare trader LL seed batches against staged pools. The temporary workflow was removed after the audit. No product code or release was changed by the audit itself.
