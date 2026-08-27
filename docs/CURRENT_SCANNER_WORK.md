# Current Scanner Work

기준일: 2026-08-27
상태: **FEATURE COMPLETE / MAINTENANCE ONLY / v1.7.14 PUBLIC STABLE**

## 최종 결론

Scanner 기능 개발 단계는 종료됐다. 현재 기본 운영 모드는 **유지보수 전용**이다.

새 실제 회귀 증거가 없는 한 structural/header threshold, candidate cap, OCR/matcher/visual acceptance, 200 ms observation target을 선제 조정하지 않는다.

## 현재 Public stable

```text
version: v1.7.14
exact product release source/tag target: 0a51375de36cd13047216006c2c0311728b1bd89
main CI run: 33060827905 — SUCCESS
release workflow run: 33061059154 — SUCCESS
release id: 377720327
asset: Junhyun-Helper.zip
asset id: 532104142
bytes: 80,488,363
SHA-256: 341ac502d2ace563ab2e7c8d7091a8e796cf87e7d1f5961edf869feab106e2fd
407 passed / 0 failed / 0 skipped
Product UI / Scanner / Map / Factory / MiniMap smoke: SUCCESS
```

GitHub `/releases/latest`와 `refs/tags/v1.7.14`는 모두 exact product source를 가리키며 공개 ZIP digest는 main-CI package SHA-256과 일치한다.

공개 증거:

- `docs/RELEASE_1.7.14.md`
- `docs/.release-v1.7.14-status.json`
- `docs/RELEASE_NOTES_V1.7.14.md`
- `docs/DECISION_V1.7.14_UI_CONSISTENCY.md`

이 문서와 이후 documentation-only commit은 v1.7.14 제품 릴리즈 소스가 아니다.

## 현재 Scanner pipeline

```text
Tarkov window pixels
→ capture
→ detail rectangle proposals
→ close-X / magnifier / inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ serialized Windows ko-KR OCR
→ optional user OCR substitution
→ conditional environment-aware title normalization
→ current official Korean full-item catalog sanitation / normalization
→ conservative catalog matching / bounded recovery
→ optional current-pixel visual corroboration
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
→ optional user correction / reviewed Ground Truth
```

Scanner는 closed-domain recognizer이며 current official Korean Tarkov full-item catalog가 Item identity authority다.

## 현재 안전 불변식

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

- false positive는 miss보다 나쁘다.
- geometry/environment normalization은 Item identity proof가 아니다.
- stale/cross-frame OCR 또는 visual result를 current identity proof로 사용하지 않는다.
- Item ID 확정 전 price/needed/slot/previous-frame metadata를 identity evidence로 사용하지 않는다.
- scan 순간 identity 결정을 위해 network 요청을 시작하지 않는다.
- reviewed Ground Truth evidence 없이 matcher/visual acceptance를 완화하지 않는다.

## v1.7.14 — Scanner 영향

v1.7.14는 **Scanner identity recognition 변경 릴리즈가 아니다**. 설정·고급 화면과 공통 UI interaction만 정리했다.

현재 UI/presentation 계약:

- Scanner Settings가 Mini Scanner display configuration과 Scanner hotkey configuration을 함께 소유한다.
- hotkey persistence는 기존 `ScannerCoordinator` authority를 사용한다.
- old dedicated `ScannerHotkeySettingsWindow`는 제거됐다.
- Scanner Advanced는 standalone Window로 표시하지 않고 MainWindow shared in-app overlay에 host한다.
- Advanced 화면 내부의 별도 `닫기` 버튼은 사용하지 않는다.
- Scanner Settings / Advanced는 같은 launcher 재클릭, backdrop click, common overlay X로 dismiss된다.
- child surface의 기존 저장/검증 의미는 MainWindow overlay가 재구현하지 않는다.
- Scanner 주요 검색창은 입력창 우측 내부 `×` clear affordance를 사용한다.
- searched item이 current needed item이면 기존 `ItemsWorkspace.Plan.NeededItems[itemId].Sources`를 presentation에 join해 관련 Quest/Hideout source를 표시한다.
- `현재 결과 교정`은 기본 Scanner 화면 우측 command lane에 둔다.

보존된 recognition 계약은 위 안전 불변식 전체와 동일하다.

Regression protection:

- `V1714UiConsistencyContractTests`
- old `ScannerHotkeySettingsWindow` 재도입 금지
- Scanner Advanced를 실제 MainWindow shared overlay에 host한 published EXE Product UI smoke
- full Windows release gate 407/407 tests

## 현재 presentation authority

### 필요 개수

Item ID 확정 뒤 Scanner / Mini Scanner의 `필요 개수`:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal
```

`RequiredTotal`은 전체 요구량이며 Scanner 사용자 표시 authority가 아니다.

### Quest / Hideout source

Item ID 확정 뒤 searched needed-item source:

```text
ItemsWorkspace.Plan.NeededItems[itemId].Sources
```

Scanner가 Quest/Hideout 필요량이나 source를 별도로 재계산하지 않는다.

### Mini Scanner

Confirmed Scanner Item ID가 presentation authority다. 보조 inventory-header OCR은 이미 확정된 Item 표시를 veto할 권한이 없다.

Sticky presentation:

```text
success → show/update + miss budget reset
miss #1 → last good 유지
miss #2 → last good 유지
miss #3 → hide
```

## Ground Truth / diagnostics

- 정상 monitoring은 durable automatic correction Case를 만들지 않는다.
- latest exact current frame은 current correction용 in-memory evidence로만 유지한다.
- user-explicit correction save만 reviewed durable Ground Truth다.
- reviewed/manual/corrupt/unknown/state-changed Case는 자동 정리하지 않는다.
- runtime log와 Ground Truth lifetime은 분리한다.
- support bundle은 reviewed Ground Truth/source pixels, `user.db`, profile/account-identifying progress data를 포함하지 않는다.

## 최근 안정화 이력

- v1.7.6: 일부 실제 데스크톱의 5~13초 인식 지연 해결
- v1.7.7: durable automatic Case 폭증, 반복 로그, Scanner/Map hotkey 계약 정리
- v1.7.8: raid inventory inspect-header ownership 회귀 수정
- v1.7.9: recognition success 뒤 Mini Scanner 표시 veto 회귀 수정
- v1.7.10: 공개 배포 범용성을 위한 title OCR 입력 환경 정규화
- v1.7.11: `필요 개수` presentation 및 configurable hotkey modifier UX 수정
- v1.7.12: Desktop lifecycle/ownership 유지보수; Scanner recognition 불변
- v1.7.13: display settings 즉시 저장, searched needed-source 표시, UI 단순화
- v1.7.14: settings/hotkey/advanced shared overlay 및 search interaction 통일; Scanner recognition 불변

역사적 상세는 각 버전 `DECISION_*`, `RELEASE_*`, `SCANNER_*` 기록을 사용한다.

## 다음 Scanner 작업의 진입 조건

다음 중 하나가 있을 때만 새 Scanner maintenance 작업을 시작한다.

1. 실제 Tarkov 화면에서 재현 가능한 인식 회귀
2. user-reviewed Ground Truth가 특정 failure stage를 입증
3. Tarkov UI/locale/rendering 변화가 기존 semantic/OCR 계약을 깨뜨림
4. 성능 telemetry가 실제 병목을 입증

작업 순서:

```text
runtime evidence 확보
→ failure stage 분류
→ root cause 확인
→ affected layer 최소 수정
→ reviewed deterministic regression
→ full Windows release gate
→ PATCH release
```

현재 v1.7.14 릴리즈 배치에 남은 Scanner 개발 작업은 없다.
