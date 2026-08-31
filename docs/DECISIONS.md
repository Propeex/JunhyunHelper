# DECISIONS — 현재 유효한 장기 결정 인덱스

이 문서는 준현 헬퍼의 **현재 유효한 장기 결정과 supersession 관계를 빠르게 복구하기 위한 active index**다. 현재 사실값은 `docs/PROJECT_STATE.json`, 현재 제품 상태와 release evidence는 `docs/CURRENT_STATE.md` / `docs/STATE.md`가 권위다.

기준일: **2026-08-31 KST**  
현재 공개 제품: **v1.13.3 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

과거 결정 원문과 당시 release-specific 사실은 historical evidence다. 현재 제품 의미와 충돌하면 최신 confirmed decision, canonical current-state 문서, 실제 코드/테스트가 우선한다.

## 1. 장기 기본 결정

DEC-001~DEC-029 원문은 `docs/DECISIONS_HISTORY_THROUGH_2026-08-09.md`에 보존한다. 이후 numbered/standalone 결정도 각 전문 문서에 보존한다.

현재도 유지되는 핵심 원칙:

- 새로 확정된 사용자 제품 요구사항이 현재 구현보다 우선한다. 기존 `Propeex/Tarkov-Helper` 프로토타입은 제품 사양 권위가 아니다.
- GitHub 저장소의 공식 문서, 현재 코드, 테스트, CI/release 상태가 프로젝트 기억의 기준이다.
- 사용자는 제품 판단에 집중하고 구현/Git/PR/CI/배포는 개발자가 책임진다.
- 새 사용자 기능은 MINOR, 기존 기능의 수정·보완은 PATCH를 기본으로 한다.
- user-visible WPF 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE runtime smoke까지 검증한다.
- 장기 async/lifecycle 종료 경계는 active async work 중 정상 Main Window close를 포함해 회귀 검증한다.
- 공개 stable release의 tag/source/assets는 immutable historical identity로 취급하며 후속 documentation-only commit을 제품 source로 재정의하지 않는다.

## 2. Farming Guide current authority

현재 authority:

- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`
- `docs/DECISION_V1.13.3_FARMING_GUIDE_LIVE_ITEM_INTERACTION.md`
- `docs/ARCHITECTURE_FARMING_GUIDE.md`

상태: **CONFIRMED / PUBLIC VERIFIED v1.13.3**.

핵심 제품 계약:

- Scanner 오른쪽에 `파밍 가이드` first-class section을 둔다.
- 제품 의미는 raid-start Loadout / Inventory Editor이며 실시간 인게임 inventory mirror가 아니다.
- actual Tarkov item width/height, carrier grids, filters, equipment/attachment/armor slots, conflicts를 current validated Game Content에서 사용한다.
- drag/drop은 `R` 회전, bounded grid snap, bounds/overlap/contiguous-space/current filter 검증과 valid-invalid feedback을 제공한다.
- 근접무기와 PMC 인식표는 per-profile preset과 분리된 user-level fixed setting이다.
- profile edition / Old Patterns state에 따라 standard/expanded pocket geometry를 사용하고 UI와 persisted-state sanitization이 같은 resolved geometry를 소비한다.
- contents가 있는 carrier를 다른 carrier로 묵시적으로 교체해 contents를 유실시키지 않는다.
- persisted state가 current Tarkov structure와 충돌하면 impossible placement를 fail closed한다.

v1.13.3 supersession:

- 별도 generic `장비 정보/장비 설정` Window와 read-only storage preview 중심 UX를 폐기한다.
- double-click은 가운데 in-page workbench에서 실제 actionable structure를 연다.
- stored bag/rig는 실제 내부 grid를 직접 drag/drop한다.
- weapon/helmet/armor는 current attachment/mod/replaceable armor plate slot을 one-item drop target으로 직접 조작한다.
- occupied one-item slot을 묵시적으로 overwrite하지 않는다.
- nested storage는 `ParentInstanceId`로 parent container instance를 보존한다.
- nested sanitize는 root → accepted parent tree 순으로 진행하고 orphan/duplicate/self-cycle/unresolved cycle/invalid grid/filter/bounds/overlap을 fail closed한다.
- nested container 이동은 descendants를 유지하고 destructive delete/replacement는 subtree를 함께 제거한다.
- current Secure Container fallback은 generic case/container와 구분해 Medicine Case 같은 일반 storage case를 오장착하지 않는다.
- Farming Guide 검색에서는 upstream `ItemPropertiesPreset` / `preset` assembled weapon record만 제외하고 canonical base weapon의 actual mod slots를 사용한다.
- 열린 workbench owner item 이동 시작 시 workbench를 닫아 stale write-back을 방지한다.

현재 product identity:

```text
v1.13.3
exact source/tag target:
9a0064d81dca4c2cffcb01c55742d46298d235de
release id: 379676479
513 passed / 0 failed / 0 skipped
```

## 3. Scanner current authority

현재 사용자-facing 필요 수량/출처 authority:

```text
needed quantity = ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
needed source   = ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

Scanner identity proof에는 price/needed/source/relationship metadata를 사용하지 않는다. OCR threshold, matcher, candidate cap, visual corroboration/recovery acceptance는 reviewed actual Tarkov evidence 없이 완화하지 않는다.

Scanner는 external screen pixels + OCR만 사용한다. 다음은 사용하지 않는다.

- game process memory read
- code/DLL injection
- game/process hook
- kernel/driver 접근
- input automation
- game network manipulation
- anti-cheat bypass

Ground Truth는 explicit user-reviewed save만 authoritative하다. correction hotkey는 evidence-only Saved Case를 저장하며 Ground Truth를 자동 생성·추측하지 않는다.

관련 결정:

- `docs/DECISION_SCANNER_STORAGE_AND_HOTKEYS_2026-08-26.md`
- `docs/DECISION_SCANNER_CROSS_ENVIRONMENT_2026-08-26.md`
- `docs/DECISION_V1.8.0_SCANNER_ITEM_DATABASE.md`
- `docs/DECISION_V1.8.1_ITEM_RELATIONSHIP_COMPLETENESS.md`
- `docs/DECISION_V1.8.2_RUNTIME_LIVE_REGRESSIONS.md`
- `docs/DECISION_V1.8.4_AMMO_SCANNER_ITEM_DETAIL.md`
- `docs/DECISION_V1.9.0_SCANNER_FAVORITES_RECENTS_AND_UI_FIXES.md`

## 4. Quest staged task-pool compatibility

현재 authority:

- `docs/DECISION_TASK_POOL_RUNTIME_COMPATIBILITY_2026-08-17.md`

유지 계약:

- exact ProfileVariable 값이 있으면 항상 최우선 권위값이다.
- audited current-version staged pool과 구조가 일치할 때만 제한적 compatibility를 허용한다.
- 현재 trader LL이 audited stage보다 낮으면 잠금 의미를 유지한다.
- current stage에서는 기존 보수적 reconstruction/fail-closed를 유지한다.
- 현재 trader LL이 audited stage보다 높으면 과거 stage threshold가 충족됐다는 runtime-only effective floor를 사용할 수 있다.
- 이 floor는 숨은 server counter의 exact fact로 저장하거나 주장하지 않는다.
- structural drift는 `Indeterminate / 확인 필요`로 fail closed한다.
- Future Needed Items / cleanup에는 current Quest UI compatibility를 낙관적으로 전파하지 않는다.

## 5. 김태영 PC 진단

현재 authority:

- `docs/DECISION_V1.12.0_KIM_TAEYOUNG_PC_DIAGNOSTIC.md`

상태: **CONFIRMED / PUBLIC VERIFIED**.

정상 성공 UX:

```text
프로필 이미지 클릭
→ “혹시 김태영 본인?”
→ 예
→ indeterminate progress bar
→ Desktop diagnostic ZIP 생성
→ “진단 완료.\n파일을 hyune4784@naver.com 으로 보내주세요.”
→ 기본 브라우저에서 네이버 메일 쓰기 열기
```

- ZIP은 자동 업로드하지 않는다.
- 웹메일 DOM/UI를 자동 조작하지 않는다.
- 파일을 자동 첨부하거나 이메일을 자동 발송하지 않는다.
- 민감하거나 불필요한 시스템 식별 정보는 진단 수집에서 제외한다.
- optional probe는 fail-soft이며 핵심 ZIP 작성 실패만 전체 실패로 처리한다.
- 김태영 PC 원인 판정은 김태영 실제 PC evidence로 수행한다.

## 6. Map / MiniMap

Map/MiniMap donor pin:

```text
SIGDrone/Tarkov-Helper@d933792b6042a51cea38dc44b686a096fe30de67
```

JunhyunHelper first-party bridge가 제품 의미와 lifecycle/presentation ownership을 가진다.

현재 유지되는 핵심 계약:

- Main Map selection은 fresh/reused MiniMap에 동기화된다.
- player heading은 position과 동일한 map별 affine transform 좌표계를 사용한다.
- PMC / Scav / Transit extract filter와 실제 rendered marker를 검증한다.
- loaded marker data는 있는데 standard layer만 비는 bounded empty-layer race는 another refresh race 없이 직접 복구한다.
- Player Marker Size 변경은 unrelated presentation을 재초기화하지 않는다.
- Mini Scanner 우클릭 correction context menu는 제거 상태를 유지한다.

## 7. Hideout / Ammo / Game Content

Hideout FIR:

- source `attributes.foundInRaid` 의미를 canonical requirement에 보존한다.
- FIR requirement에는 non-FIR inventory가 충당되지 않는다.

Ammo:

- pickup 판단은 same-caliber penetration과 현재 profile에서 증명된 direct purchase 상태를 기준으로 한다.
- flea/barter/craft/higher trader LL/unproven quest unlock은 현재 직접 구매 가능으로 취급하지 않는다.
- Ammo Pack은 authoritative `containsItems` 관계를 우선한다.

Game Content:

- candidate download/build → schema/completeness/integrity validation → validated active 승격
- Last Known Good 보존
- 검증 실패 시 기존 정상 데이터 유지
- current source 의미가 불명확하거나 structure drift가 있으면 fail closed
- Farming Guide item structure는 Content v9에 보존한다.
- Farming Guide user state와 Game Content lifecycle을 분리한다.

## 8. Program Update / Release

- GitHub latest public stable release를 사용한다.
- 사용자 동의 없이 프로그램을 자동 교체하지 않는다.
- stable ZIP + checksum을 검증한다.
- Release workflow는 exact-main CI artifact를 사용한다.
- 공개 asset과 exact product source/tag target이 일치해야 한다.
- documentation-only main commit이 같은 assembly version의 다른 bytes를 만들 수 있어도 이미 공개된 asset을 교체하거나 historical product source를 변경하지 않는다.

현재 v1.13.3 public identity:

```text
exact product source/tag target:
9a0064d81dca4c2cffcb01c55742d46298d235de
release id: 379676479
513 passed / 0 failed / 0 skipped
```

상세 evidence:

- `docs/RELEASE_1.13.3.md`
- `docs/.release-v1.13.3-status.json`
- `docs/RELEASE_NOTES_V1.13.3.md`

## 9. Product complete / maintenance / lifecycle

관련 결정:

- `docs/DECISION_PRODUCT_COMPLETE_2026-08-26.md`
- `docs/DECISION_LONG_TERM_MAINTENANCE_AUDIT_2026-08-27.md`
- `docs/DECISION_V1.7.12_MAINTENANCE.md`
- `docs/DECISION_V1.7.13_UI_SIMPLIFICATION.md`
- `docs/DECISION_V1.7.14_UI_CONSISTENCY.md`
- `docs/DECISION_V1.8.3_VISIBLE_UI_RUNTIME_ACTIVATION.md`
- `docs/DECISION_V1.10.1_STABILITY_AUDIT.md`
- `docs/DECISION_V1.10.1_POST_RELEASE_STABILITY_SWEEP.md`

현재 공개 stable은 product-complete maintenance mode다. Farming Guide는 사용자가 명시적으로 확정한 기능으로 v1.13.0에 추가됐고, v1.13.1~v1.13.3은 실사용 회귀와 UX/compatibility를 필요한 범위에서 수정했다. 이후 실제 runtime error, Tarkov 변화, reviewed Scanner evidence 또는 사용자가 새로 확정한 제품 요구사항이 있을 때 필요한 범위만 수정한다.

## 10. 현재 결정 확인 순서

1. `docs/PROJECT_STATE.json`
2. `docs/ACTIVE_WORK.md`
3. `docs/PRODUCT.md`
4. `docs/CURRENT_STATE.md`
5. `docs/STATE.md`
6. `docs/DECISIONS.md`
7. 관련 최신 `docs/DECISION_*`
8. `docs/ARCHITECTURE.md` 및 specialist architecture docs
9. `docs/DEVELOPER_REFERENCE.md`
10. `docs/MAINTENANCE_CONTRACTS.md`
11. Scanner / Map / Farming Guide 전문 문서
12. current code / tests / PR / CI / release state

과거 release/decision 문서의 당시 값은 historical evidence다. 현재 제품 의미와 충돌하면 최신 confirmed decision과 current canonical docs가 우선한다.
