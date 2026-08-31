# Farming Guide — architecture / maintenance contract

Date: **2026-08-31 KST**  
Initial target: **v1.13.0**

Canonical product decision: `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`.

## 1. Purpose

Farming Guide의 첫 제품 slice는 사용자가 레이드 출발 상태를 직접 구성하는 local Loadout / Inventory Editor다.

```text
validated Tarkov Game Content
+ user-selected equipment/carriers/items
+ per-profile working state/presets
+ user-level fixed melee/dogtag
→ Farming Guide editor
→ future packing/value/recommendation input
```

현재 구현은 Tarkov 프로세스 내부 inventory를 읽거나 실시간 좌표를 동기화하지 않는다.

## 2. Ownership by project

### Core

`src/JunhyunHelper.Core/FarmingGuide/`

- `FarmingGuideItemLayout.cs`
  - canonical Tarkov storage grid / attachment / armor / conflict structure
- `FarmingGuideStateModels.cs`
  - equipment, carrier, stored-item, preset snapshot state
- `FarmingGuidePlacementEngine.cs`
  - footprint, rotation, bounds, overlap, first-fit calculations
- `FarmingGuideCompatibility.cs`
  - equipment/carrier/filter/conflict compatibility
- `FarmingGuideLoadoutPolicy.cs`
  - carrier replacement safety
  - persisted snapshot sanitization against current Game Content

Core는 WPF/HTTP/SQLite에 의존하지 않는다.

### Infrastructure

`src/JunhyunHelper.Infrastructure/TarkovJson/Items/TarkovItemImporter.cs`

현재 validated item source에서 다음 optional 구조를 canonical `GameItem.FarmingGuideData`로 import한다.

- `width`, `height`, `weight`
- item property type
- storage grids and filters
- attachment slots and filters
- armor slots / allowed plates
- conflicting items / slots
- blocks-headphones
- armored-rig structure

`src/JunhyunHelper.Infrastructure/Storage/ContentSnapshotStore.cs`

- v1.13.0 target write schema: **v9**
- readable: **v3~v9**
- v9는 optional Farming Guide item layout을 보존한다.
- 구형 LKG snapshot은 계속 읽을 수 있다.

`src/JunhyunHelper.Infrastructure/Storage/FarmingGuidePresetStore.cs`

- path: `%LocalAppData%/JunhyunHelper/farming-guide.json`
- schema: **v1**
- atomic JSON + backup semantics
- Game Content와 독립된 mutable user state

### Desktop

`src/JunhyunHelper.Desktop/FarmingGuide/`

- `FarmingGuidePage.xaml(.cs)` — editor composition/search/working-state ownership
- `FarmingGuidePage.Rendering.cs` — equipment/storage grid rendering
- `FarmingGuidePage.Drag.cs` — drag session, rotation, target probe, snap/drop/delete
- `FarmingGuidePage.Interaction.cs` — item configuration launch
- `FarmingGuideItemConfigurationWindow.cs` — attachment/armor plate editor
- `FarmingGuidePresetNameWindow.cs` — preset naming

`MainWindow.FarmingGuide.cs`와 MainWindow section lifecycle이 profile/content/busy/navigation boundary를 소유한다.

## 3. Persistence model

```text
farming-guide.json
├─ SchemaVersion = 1
├─ Profiles[profileId]
│  ├─ WorkingSnapshot
│  ├─ SelectedPresetName
│  └─ Presets[]
└─ FixedEquipment
   ├─ Melee
   └─ Dogtag
```

Profile snapshot:

```text
Equipment slot → FarmingGuideItemState
Rig / Backpack / SecureContainer → FarmingGuideItemState
StoredItems[]
  ├─ InstanceId
  ├─ Item tree
  ├─ Storage kind
  ├─ GridIndex
  ├─ X / Y
  └─ Rotated
```

`FarmingGuideItemState`는 attachment 및 armor plate child state를 재귀적으로 보존한다.

## 4. Placement contract

`FarmingGuidePlacementEngine`의 grid 좌표가 authoritative editor coordinate다.

- item footprint는 최소 1×1로 normalize한다.
- rotation은 width/height swap이다.
- bounds 밖은 불가다.
- axis-aligned cell rectangle overlap은 불가다.
- 남은 cell 총량만으로 배치 가능을 판단하지 않는다.
- 실제 연속 공간이 있어야 한다.
- UI drop은 grid 근처에서 bounded snap을 허용한다.

현재 editor 좌표는 Tarkov 실제 raid inventory 좌표와 지속적으로 1:1 동기화하지 않는다.

## 5. Carrier safety contract

Carrier는 Rig / Backpack / Secure Container다.

- populated carrier를 일반 stored item처럼 이동하지 않는다.
- populated target carrier를 다른 carrier로 묵시적으로 덮어써 내부 item을 삭제하지 않는다.
- 같은 현재 carrier를 자기 slot으로 되돌리는 동작은 허용한다.
- 사용자가 carrier 자체를 명백한 빈 영역으로 끌어내 삭제하는 explicit destructive action은 carrier와 그 storage contents를 함께 제거한다.

이 경계는 `FarmingGuideLoadoutPolicy.CanReplaceCarrier`와 Desktop drag probe 양쪽에 반영한다.

## 6. Persisted-state sanitization

Tarkov item/grid/filter 구조는 업데이트로 바뀔 수 있으므로 저장된 preset/working state를 무조건 신뢰하지 않는다.

Load 시 current catalog 기준으로 다음을 검증한다.

- item이 아직 존재하는가
- equipment/carrier type이 현재 slot과 호환되는가
- target storage/grid가 현재 존재하는가
- grid filter가 item을 허용하는가
- footprint가 current bounds 안에 있는가
- 이미 승인한 placement와 overlap하지 않는가
- duplicate/empty instance ID가 아닌가

증명할 수 없는 stored placement는 fail closed로 제외한다. 오래된 impossible coordinate를 화면에 그대로 복원하거나 새 working state에 재저장하지 않는다.

## 7. Equipment compatibility

Interactive editor는 최소 다음 관계를 유지한다.

- equipment slot type compatibility
- body armor ↔ armored rig mutual exclusion
- headphones ↔ blocking helmet mutual exclusion
- current conflicting item metadata
- carrier kind compatibility
- grid/slot allow/exclude filters

Attachment/armor plate 선택은 current item layout의 slot/filter/allowed-plate data를 사용한다.

## 8. Summary semantics

현재 summary:

- total weight: equipment/carriers/fixed items/stored items와 nested attachments/plates의 canonical `WeightKg` 합
- storage usage: stored item footprint cell 합 / current rendered storage-grid cell 총량
- farming value: v1.13.0에서는 정책 미구현이므로 `—`

Storage cell summary는 참고값이며 특정 대형 item의 수납 가능성은 packing/연속 공간 계산으로 별도 판단한다.

## 9. Safety boundary

Farming Guide는 외부 helper 경계를 유지한다.

사용하지 않음:

- game memory read
- code/DLL injection
- process/game hook
- kernel/driver access
- input automation
- game network manipulation
- anti-cheat bypass

## 10. Verification

결정적 테스트:

- `tests/JunhyunHelper.Tests/Core/FarmingGuidePlacementEngineTests.cs`
- `tests/JunhyunHelper.Tests/Core/FarmingGuideLoadoutPolicyTests.cs`
- `tests/JunhyunHelper.Tests/Infrastructure/FarmingGuidePresetStoreTests.cs`
- `tests/JunhyunHelper.Tests/TarkovJson/TarkovItemImporterTests.cs`
- `tests/JunhyunHelper.Tests/Storage/ContentSnapshotStoreTests.cs`
- `tests/JunhyunHelper.Tests/Maintenance/FarmingGuideDesktopSectionContractTests.cs`

User-visible completion gate:

```text
Release build
→ deterministic tests
→ self-contained Windows x64 publish
→ actual published EXE starts
→ Product UI smoke activates/renders Farming Guide
→ existing Map/Product UI smoke remains healthy
→ graceful shutdown
→ Shutdown Race
→ release package audit
```

Future recommendation/value work must not be folded into placement/persistence code by inference. New recommendation semantics require their own confirmed product decision and tests.
