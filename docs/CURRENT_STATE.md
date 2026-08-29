# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md`, `docs/PRODUCT.md`, `docs/ARCHITECTURE.md`, `docs/DEVELOPER_REFERENCE.md`, 전문 결정/릴리즈 문서를 참조합니다.

기준일: 2026-08-29 KST

상태: **`v1.10.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE`**

## 공개 stable

```text
public stable/latest: v1.10.1
exact product release source/tag target: c444a1e26793e15c075875159f6605d8a99cf7f9
PR CI run: 33253141127 — SUCCESS
exact-main CI run: 33253293015 — SUCCESS
release workflow run: 33253438908 — SUCCESS
release id: 378982127
published UTC: 2026-08-29T12:49:03Z
stable asset: Junhyun-Helper.zip
stable asset id: 535210900
stable bytes: 80,540,164
stable SHA-256: c37c00a5e5ecdc431d6b26775d73682cabf17e4310533065c88e2d58d8f14922
checksum asset id: 535210901
checksum asset SHA-256: d32a6d50b60b512fa446d708d5d8ba75addad854c1e63c51378b318fbd6116c3
439 passed / 0 failed / 0 skipped
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9715065803
archive bytes: 241,555,171
archive SHA-256: 17fa98916dac423dd304ca59a5769f2fc61851d391ff3c4df89ceaaa25d3b663
```

`/releases/latest`와 `refs/tags/v1.10.1` readback에서 release target/tag ref가 exact product release source와 일치하고 `draft=false`, `prerelease=false`, latest stable임을 확인했다.

공개 증거:

- `docs/RELEASE_1.10.1.md`
- `docs/.release-v1.10.1-status.json`
- `docs/RELEASE_NOTES_V1.10.1.md`
- `docs/DECISION_V1.10.1_STABILITY_AUDIT.md`

## v1.10.1 안정성 감사

### WPF header lifecycle ownership

기존 메인 헤더 보강은 static class-level `Loaded` handler에 의존했다. v1.10.1은 이를 `MainWindow.OnInitialized`가 명시적으로 소유하도록 바꾸고, 실제 visual-tree 변경은 `DispatcherPriority.Loaded`에서 한 번 수행한다.

`DependencyPropertyDescriptor.AddValueChanged`로 등록한 header status watcher도 `MainWindow.OnClosed`에서 명시 해제한다.

사용자-visible 결과는 유지한다.

- 헤더에는 버전 정보만 표시
- 정리 가능한 아이템이 있으면 아이템 탭 우측 상단의 작은 오렌지 점 표시
- update progress는 전용 overlay 사용

### repository/package maintenance

- 현재 CI/Release 실행 경로에서 사용되지 않는 `.github/scripts/finalize-v121.py`를 제거했다.
- v1.2.1 역사적 릴리즈 증거는 기존 공식 docs/Release에 보존한다.
- `FIRST_RUN_KO.txt`는 설치 안내 + 현재/직전 핵심 변경만 유지하며 전체 역사 authority는 GitHub Releases와 `docs/`로 일원화했다.

### 의도적으로 유지한 영역

감사 결과 실제 오류 증거 없이 추가 변경할 근거가 부족한 다음 영역은 유지했다.

- 사용자 진행 저장/busy serialization
- atomic JSON durability/backup promotion
- Program Update checksum/immutable stable/recovery
- Scanner runtime/context monitor lifecycle
- Scanner OCR/matcher/candidate/recovery acceptance
- Game Content LKG/completeness/fail-closed
- Map/Factory/MiniMap semantics

## v1.10.1 post-release 안정성 검증

공개 뒤 제품 동작을 바꾸지 않는 maintenance sweep을 추가로 수행했다.

### deterministic disposal ownership

`DesktopStartupWiringContractTests.ProductLifetime_DisposesOwnedLongLivedServices`가 다음 종료 소유권을 고정한다.

- MainWindow `Closed` → `DesktopServices.Dispose()`
- Scanner coordinator monitor/hotkey/runtime/OCR/overlay/catalog cleanup
- shared `HttpClient.Dispose()`
- `App.OnExit` → Program Update coordinator / Scanner diagnostic retention dispose

확인된 런타임 결함은 없어 speculative lifetime/cancellation 리팩터링은 하지 않았다.

### active-async close published EXE gate

`.github/workflows/shutdown-race-ci.yml`은 실제 Windows x64 Release publish EXE를 실행한 뒤 기존 async Product/Map smoke가 아직 완료되지 않은 상태에서 정상 Main Window close를 요청한다.

합격 조건:

- full async smoke success marker가 close 전에 존재하지 않을 것
- 정상 `CloseMainWindow()` 요청이 수락될 것
- 7초 안에 프로세스가 종료될 것
- exit code = 0
- Map smoke diagnostic / unhandled startup diagnostic 없음

PR #221과 exact-main 모두 이 경계를 통과했다.

```text
post-release test-contract PR #220 CI: 33254932421 — SUCCESS
post-release test-contract exact-main CI: 33255074971 — SUCCESS
post-release test-contract Release workflow: 33255208324 — SUCCESS
shutdown-race PR #221 CI: 33255650930 — SUCCESS
shutdown-race PR #221 dedicated CI: 33255651032 — SUCCESS
shutdown-race exact-main CI: 33258220788 — SUCCESS
shutdown-race exact-main dedicated CI: 33258220786 — SUCCESS
post-merge immutable Release verification: 33258352426 — SUCCESS
current deterministic test suite: 440 passed / 0 failed / 0 skipped
latest non-documentation maintenance head: 22701e5419bca2995d442599fad646abcd484007
```

후속 Release workflow와 공개 readback에서 v1.10.1 release/tag/assets는 변경되지 않았다.

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout / Needed Items | 구현 완료 / maintenance |
| Items / Ammo | 구현 완료 / maintenance |
| Map + MiniMap | 구현 완료 / v1.10.0 reopen rendered sync 유지 |
| Game Content Update | 구현 완료 / relationship LKG + fail-closed 유지 |
| Program Update | 구현 완료 / stable ZIP checksum 계약 |
| Scanner + Mini Scanner | **FEATURE COMPLETE / MAINTENANCE ONLY** |
| Scanner 아이템 정보 DB | **IMPLEMENTED / PUBLIC STABLE** |
| Scanner Favorites / Recents | **IMPLEMENTED / PUBLIC STABLE** |

## Schema / compatibility

```text
Desktop target version: 1.10.1
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v7
Scanner catalog cache: v1~v4 readable, v4 written
Scanner item UI state: scanner-item-ui-state.json / canonical Item ID persistence
```

v1.10.0 → v1.10.1 mandatory Game Content migration: none  
v1.10.0 → v1.10.1 user.db migration: none

## 유지되는 핵심 계약

- Scanner false positive보다 miss 선호.
- OCR/matcher/candidate cap/visual recovery acceptance는 reviewed actual Tarkov evidence 없이 완화하지 않는다.
- Scanner current needed = `ItemsWorkspace.Plan.NeededItems[itemId].RemainingTotal`.
- Scanner source = `ItemsWorkspace.Plan.NeededItems[itemId].Sources`.
- price/needed/source/relationship metadata는 Item ID proof에 사용하지 않는다.
- Game Content candidate/LKG/completeness/fail-closed를 유지한다.
- Map/MiniMap donor pin은 `d933792b6042a51cea38dc44b686a096fe30de67`이다.
- Factory floor/marker/viewport 의미는 변경하지 않는다.
- user-visible WPF lifecycle 변경은 source assertion이 아니라 actual published EXE runtime evidence로 검증한다.
- 장기 async/lifecycle 종료 경계는 가능한 경우 published EXE의 정상 Main Window close로 직접 검증한다.

## 사용자 실사용 상태

v1.10.1은 CI와 published EXE 자동 검증까지 완료됐다. 사용자의 실제 PC/Tarkov 플레이 환경에서의 실사용 검증은 아직 별도 확인 전이다.

## 다음 작업

v1.10.1 릴리즈 배치에 남은 제품 개발 작업은 없다. 기본 운영 모드는 유지보수이며, 새 기능은 사용자가 명시적으로 새 제품 요구사항으로 결정할 때만 시작한다.

이 문서와 이후 documentation-only commit은 v1.10.1 product release source가 아니다. v1.10.1 product source/tag/assets는 `c444a1e26793e15c075875159f6605d8a99cf7f9`에 고정한다.