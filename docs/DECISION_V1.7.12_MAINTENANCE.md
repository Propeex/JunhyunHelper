# DECISION — v1.7.12 Long-term maintenance patch

기준일: 2026-08-27  
상태: **APPROVED PATCH CANDIDATE / PR #197**

## 결정

장기 완성도 감사의 첫 runtime 개선을 `v1.7.12` PATCH로 관리한다.

이 패치는 사용자 기능을 추가하거나 제품 의미를 변경하지 않는다. 목적은 Desktop composition ownership과 WPF lifecycle 경계를 명확하게 만들어 unrelated UI lifecycle 변경이 다른 화면의 infrastructure를 깨뜨릴 가능성을 줄이는 것이다.

## 포함 범위

```text
MainWindow product initialization
→ Quest/Hideout/Items/Ammo image-cache binding
→ Ammo favorites store binding
→ cross-page navigation wiring
```

Ammo 내부 presentation은 별도 owner다.

```text
AmmoPage.OnInitialized
→ DispatcherPriority.Loaded
→ search/detail presentation initialization
→ grid presentation fixes
```

부모 `MainWindow.xaml`의 `ItemsPage_Loaded`, `HideoutPage_Loaded`, `AmmoPage_Loaded` 연결과 해당 code-behind handlers는 제거한다.

## 실제 회귀에서 확인한 설계 교훈

초기 정리안은 parent `AmmoPage_Loaded`의 body가 중복이라는 이유만으로 제거했지만, published EXE Product UI smoke에서 상세정보 toggle initialization이 누락되는 회귀가 발생했다.

원인은 Ammo presentation이 class-level `Loaded` handler를 사용하고 있었고 parent의 instance `Loaded` subscription이 WPF Loaded delivery의 숨은 전제처럼 작동한 점이었다.

따라서 v1.7.12에서는 class-level Loaded initializer도 제거하고 Ammo가 자신의 `OnInitialized`에서 필요한 presentation 작업을 명시적으로 예약한다.

이 사례는 앞으로 dead-code 삭제에서 다음을 고정한다.

```text
참조/handler body만 확인
≠ lifecycle dead 증명
```

실제 WPF event delivery와 published product smoke까지 포함해 죽은 경로를 증명해야 한다.

## 성능 감사 결과

이번 패치에서 speculative performance refactor는 하지 않는다.

`RefreshActiveWorkspacesAsync`가 동일 profile을 Quest/Hideout/Items에 각각 전달하는 구조는 호출 수만 보면 중복처럼 보이지만 `UserProfileStore.LoadAsync`가 동일 immutable snapshot을 process-local cache에서 반환하므로, 현재 evidence로 SQLite I/O 병목이라고 판정하지 않는다.

실제 trace에서 병목으로 확인될 때만 구조 변경을 검토한다.

## 보존 계약

- Core/Application/Infrastructure 책임과 data ownership 불변
- Game Content candidate validation/LKG activation 불변
- Map/MiniMap donor `d933792b6042a51cea38dc44b686a096fe30de67` 유지
- Scanner recognition 정책/threshold/candidate budget 불변
- Scanner needed quantity authority `RemainingTotal` 유지
- cross-frame identity cache 추가 없음
- public `v1.7.11` tag/source/assets 수정 금지

## 검증 계약

Release candidate는 다음을 모두 통과해야 한다.

- full deterministic tests, 0 failed / 0 skipped
- Windows x64 self-contained single-file publish
- rendered Product UI / Scanner / Map / Factory / MiniMap smoke
- graceful shutdown / clean portable root
- release package verification
- merge 후 exact main CI success
- v1.7.12 public release source/tag/assets readback

최종 수치와 SHA는 release 완료 후 `docs/STATE.md`와 release status record에 기록한다.
