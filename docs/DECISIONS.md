# DECISIONS — 현재 유효한 장기 결정 인덱스

이 문서는 준현 헬퍼의 **현재 유효한 장기 결정과 supersession 관계를 빠르게 복구하기 위한 active index**다. 현재 사실값은 `docs/PROJECT_STATE.json`, 현재 제품 상태와 release evidence는 `docs/CURRENT_STATE.md` / `docs/STATE.md`가 권위다.

기준일: **2026-08-31 KST**  
현재 공개 제품: **v1.12.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**  
개발 중 제품 target: **v1.13.0 Farming Guide**

과거 결정 원문과 당시 release-specific 사실은 historical evidence다. 현재 제품 의미와 충돌하면 최신 confirmed decision, canonical current-state 문서, 실제 코드/테스트가 우선한다.

## 1. 장기 기본 결정

DEC-001~DEC-029 원문은 `docs/DECISIONS_HISTORY_THROUGH_2026-08-09.md`에 보존한다. 이후 numbered/standalone 결정도 각 전문 문서에 보존한다.

현재도 유지되는 핵심 원칙:

- 새로 확정된 사용자 제품 요구사항이 현재 구현보다 우선한다. 기존 `Propeex/Tarkov-Helper` 프로토타입은 제품 사양 권위가 아니다.
- GitHub 저장소의 공식 문서, 현재 코드, 테스트, CI/release 상태가 프로젝트 기억의 기준이다.
- 사용자는 제품 판단에 집중하고 구현/Git/PR/CI/배포는 개발자가 책임진다.
- 새 사용자 기능은 MINOR, 기존 기능의 수정·보완은 PATCH를 기본으로 한다.
- 사용자에게 보이는 WPF 변경은 source assertion만으로 완료 선언하지 않고 actual published EXE runtime smoke까지 검증한다.
- 장기 async/lifecycle 종료 경계는 active async work 중 정상 Main Window close를 포함해 회귀 검증한다.
- 공개 stable release의 tag/source/assets는 immutable historical identity로 취급하며 후속 documentation-only commit을 제품 source로 재정의하지 않는다.

## 2. v1.13.0 Farming Guide

현재 authority:

- `docs/DECISION_V1.13.0_FARMING_GUIDE_LOADOUT_EDITOR.md`

상태: **CONFIRMED / IMPLEMENTING**.

핵심 계약:

- Scanner 오른쪽에 `파밍 가이드` first-class section을 둔다.
- 첫 slice는 raid-start Loadout / Inventory Editor이며 실시간 인게임 inventory mirror가 아니다.
- 실제 Tarkov item width/height, carrier grids, grid filters, attachment slots, armor slots, conflicts를 current validated Game Content에서 사용한다.
- drag/drop은 `R` 회전, grid snap, bounds/overlap/연속 공간 검증과 valid-invalid feedback을 제공한다.
- 내용물이 든 carrier를 다른 carrier로 묵시적으로 교체해 내용을 유실시키지 않는다.
- 과거 preset이 current Tarkov grid/filter와 충돌하면 impossible placement를 복원하지 않고 fail closed한다.
- 근접무기와 PMC 인식표는 per-profile preset과 분리된 user-level fixed setting이다.
- preset은 장비, 부품, 방탄판, carrier, stored item, 위치/회전을 보존한다.
- Farming Guide 사용자 상태는 `%LocalAppData%/JunhyunHelper/farming-guide.json` schema v1에 Game Content와 분리해 저장한다.
- Farming Guide용 optional item structure 추가로 Content write schema는 v9이며 v3~v9를 읽는다.
- v1.13.0에는 가치 판단, 획득/폐기/교체 추천, Scanner 실시간 추천 연동, 지속적인 실제 inventory 좌표 1:1 동기화를 포함하지 않는다.

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

v1.12.0에서 정제된 계약을 유지한다.

- exact ProfileVariable 값이 있으면 항상 최우선 권위값이다.
- audited current-version staged pool과 구조가 완전히 일치할 때만 제한적 compatibility를 허용한다.
- 현재 trader LL이 audited stage보다 낮으면 잠금 의미를 유지한다.
- current stage에서는 기존 보수적 reconstruction/fail-closed를 유지한다.
- 현재 trader LL이 audited stage보다 높으면 과거 stage threshold가 충족됐다는 runtime-only effective floor를 사용할 수 있다.
- 이 floor는 숨은 server counter의 exact fact로 저장하거나 주장하지 않는다.
- structural drift는 `Indeterminate / 확인 필요`로 fail closed한다.
- Future Needed Items / cleanup에는 current Quest UI compatibility를 낙관적으로 전파하지 않는다.

## 5. 김태영 PC 진단

현재 authority:

- `docs/DECISION_V1.12.0_KIM_TAEYOUNG_PC_DIAGNOSTIC.md`

상태: **CONFIRMED / PUBLIC VERIFIED v1.12.1**.

정상 성공 UX:

```text
프로필 이미지 클릭
→ “혹시 김태영 본인?”
→ 예
→ indeterminate progress bar
→ Desktop diagnostic ZIP 생성
→ “진단 완료.\n파일을 hyune4784@naver.com 으로 보내주세요.”
→ 기본 브라우저에서 https://mail.naver.com/v2/new 열기
```

- ZIP은 자동 업로드하지 않는다.
- 웹메일 DOM/UI를 자동 조작하지 않는다.
- 파일을 자동 첨부하거나 이메일을 자동 발송하지 않는다.
- 사용자명, 컴퓨터명, IP/MAC, 네트워크 목록, credential, 전체 환경변수, 임의 전체 process inventory, 설치 경로는 진단 수집에서 제외한다.
- 화면 캡처 자체에는 실행 당시 실제 화면 내용이 포함될 수 있다.
- optional probe는 fail-soft이며 핵심 ZIP 작성 실패만 전체 실패로 처리한다.
- 사용자 노트북의 실제 v1.12.0 diagnostic ZIP에서 exporter 정상 동작을 확인했다. 김태영 PC 원인 판정은 김태영 실제 PC에서 생성된 evidence로 수행한다.

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
- Player Marker Size 변경은 player marker만 변경하며 Name Size / MiniMap Marker Size 등 unrelated presentation을 재초기화하지 않는다.
- Mini Scanner 우클릭 correction context menu는 제거 상태를 유지한다.

관련 결정/역사:

- `docs/DECISION_V1.9.1_FINAL_UI_MINIMAP.md` — historical; first-create/reuse 검증이 이후 강화됨
- `docs/DECISION_V1.10.0_MINIMAP_REOPEN_MINISCANNER_FLEA_MINIMUM.md`
- 최신 실제 계약과 v1.11.x 수정은 `docs/STATE.md` 및 각 release record가 권위다.

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
- current source 의미가 불명확하거나 구조 drift가 있으면 fail closed
- v1.13.0 target부터 optional Farming Guide item structure를 Content v9에 보존한다.

## 8. Program Update / Release

- GitHub latest public stable release를 사용한다.
- 사용자 동의 없이 프로그램을 자동 교체하지 않는다.
- stable ZIP + checksum을 검증한다.
- Release workflow는 exact-main CI artifact를 사용한다.
- 공개 asset과 exact product source/tag target이 일치해야 한다.
- documentation-only main commit이 같은 assembly version의 다른 bytes를 만들 수 있어도 이미 공개된 asset을 교체하거나 historical product source를 변경하지 않는다.

현재 v1.12.1 public identity:

```text
exact product source/tag target:
07a808f187e59f1b2b4b62ca6a947ccbed9baeaa
release id: 379473487
483 passed / 0 failed / 0 skipped
```

상세 evidence:

- `docs/RELEASE_1.12.1.md`
- `docs/.release-v1.12.1-status.json`
- `docs/RELEASE_NOTES_V1.12.1.md`

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

현재 공개 stable은 feature-complete maintenance mode다. 실제 회귀, Tarkov 변화, 또는 사용자가 명시적으로 확정한 새 제품 요구사항이 있을 때 필요한 범위만 수정한다. v1.13.0 Farming Guide는 사용자가 명시적으로 확정한 새 MINOR 기능 작업이다. 정상 동작하는 unrelated lifecycle/disposal 경로를 미관상 이유로 전면 리팩터링하지 않는다.

## 10. 현재 결정 확인 순서

1. `docs/PROJECT_STATE.json`
2. `docs/ACTIVE_WORK.md`
3. `docs/PRODUCT.md`
4. `docs/CURRENT_STATE.md`
5. `docs/STATE.md`
6. `docs/DECISIONS.md`
7. 관련 최신 `docs/DECISION_*`
8. `docs/ARCHITECTURE.md`
9. `docs/DEVELOPER_REFERENCE.md`
10. `docs/MAINTENANCE_CONTRACTS.md`
11. Scanner / Map 전문 문서
12. current code / tests / PR / CI / release state

과거 release/decision 문서의 당시 값은 historical evidence다. 현재 제품 의미와 충돌하면 최신 confirmed decision과 current canonical docs가 우선한다.
