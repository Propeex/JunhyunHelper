# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.7.10 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

현재 요구사항 범위의 제품과 Scanner는 완성 상태이며, 새로운 실제 회귀·호환성 변화 또는 사용자가 명시적으로 결정한 새 제품 요구사항이 없는 한 선제적 기능 추가나 인식 기준 변경을 시작하지 않습니다.

상세 상태:

- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
- `docs/CURRENT_SCANNER_WORK.md`
- `docs/SCANNER.md`

## 현재 공개 릴리즈

```text
version: v1.7.10
Desktop target version: 1.7.10
exact product release source/tag target: a557daad5b37aca11a189524ecf256564d2b8ea4
main CI: 32983155982 — SUCCESS
Release workflow: 32983498402 — SUCCESS
release id: 377231814
stable asset: Junhyun-Helper.zip
asset id: 530959212
bytes: 80,471,678
SHA-256: 6d4f3f8580318d05361cd4d62bf265c4590532722df22dc8b8d734fe8ec10eb9
389 passed / 0 failed / 0 skipped
```

GitHub `/releases/latest` readback에서 v1.7.10이 draft=false, prerelease=false, latest stable이며 tag target이 위 exact product release source와 일치함을 확인했습니다.

공식 릴리즈 기록:

- `docs/RELEASE_1.7.10.md`
- `docs/RELEASE_NOTES_V1.7.10.md`
- `docs/.release-v1.7.10-status.json`

## 주요 기능

- GameMode별 Profile
- Quest availability / prerequisite / special trader / profile-variable
- Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / consumption ledger
- Items / cross-navigation
- Ammo / favorites
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Scanner + Mini Scanner
- Scanner Ground Truth 교정 / diagnostics / regression dataset
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## Scanner

Scanner는 Tarkov 화면 픽셀을 현재 공식 한국어 Tarkov full-item catalog의 Item ID에 연결하는 closed-domain recognizer입니다.

```text
Tarkov window pixels
→ detail rectangle proposals
→ inspect-header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ serialized Windows ko-KR OCR
→ optional user substitution
→ conditional environment-aware title normalization
→ conservative official-catalog matching / bounded recovery
→ optional current-pixel visual corroboration
→ Item ID or fail closed
→ local mapped presentation
→ Scanner Page / Mini Scanner
```

### Scanner 안전 기준

```text
structural floor = 0.34
HEADER_FRAME_LOCKED floor = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
continuous observation target = 200 ms
```

- false positive보다 miss를 선호합니다.
- geometry와 환경 정규화는 Item identity proof가 아닙니다.
- stale/cross-frame OCR 또는 visual result를 현재 Item identity proof로 사용하지 않습니다.
- Item ID가 확정되기 전 price/needed/slot metadata를 identity evidence로 사용하지 않습니다.
- scan 순간 Item identity를 위해 network 요청을 시작하지 않습니다.
- 새로운 reviewed evidence 없이 threshold/candidate cap/matcher/visual acceptance를 낮추지 않습니다.

## v1.7.10 — 공개 배포 환경 대응

v1.7.10은 특정 사용자 PC에 맞춘 튜닝이 아니라 다양한 정상 Windows/Tarkov 환경에서 Scanner가 더 일관되게 동작하도록 item-title OCR 입력을 hardening했습니다.

```text
normal OCR success
→ 기존 결과 즉시 사용

normal OCR miss 또는 기존 bounded deep pass
→ title ROI luminance profile 분석
→ reference/flat input: 기존 경로 유지
→ lifted/washed/low-contrast input: adaptive normalized auxiliary OCR
→ 기존 conservative catalog matching
→ Item ID or fail closed
```

핵심:

- P60 기반 dark title-field background 추정
- P99.75 기반 sparse bright glyph foreground 추정
- usable contrast가 없는 flat input은 normalization 금지
- 정상 normal OCR 성공 시 histogram/copy/추가 OCR 자체를 생략
- 1080p / 1440p / 4K proportional title raster regression
- SDR-like / lifted / washed / compressed-contrast / low-contrast / flat regression
- 기존 semantic/catalog/matcher/visual acceptance 유지

제품 결정:

- `docs/DECISION_SCANNER_CROSS_ENVIRONMENT_2026-08-26.md`

## v1.7.9 — Mini Scanner 표시

Scanner Item ID가 이미 확정됐는데 Mini Scanner가 별도 inventory-header OCR 실패로 표시되지 않던 presentation 회귀를 수정했습니다.

현재는 confirmed Item identity가 presentation authority이며, 숨겨진 real Scanner의 최초 표시에서만 Tarkov가 foreground인지 확인합니다.

이미 표시 중인 Mini Scanner는 새 confirmed Item으로 즉시 갱신하며, 실제 miss 3회째에 숨깁니다.

## v1.7.8 — raid inspect-header ownership

Raid inventory 수평선이 inspect header와 이어져 header-left ownership이 실제 상세창보다 왼쪽으로 확장되던 문제를 user-reviewed Case로 수정했습니다.

Recovery는 기존 정상 header 경로 뒤에서만 동작하며, 강한 RED-X structural evidence와 기존 close-X/magnifier/header/title evidence, 최종 `HEADER_FRAME_LOCKED >= 0.68`을 모두 요구합니다.

## Scanner 성능 기준선

v1.7.6에서 동일 current-frame visual evidence가 여러 후보에서 반복 계산되며 5~13초까지 지연되던 문제를 수정했습니다.

문제 PC 재검증:

```text
Display Test — 하프 마스크
10,840.877 ms → 70.603 ms

Display Test — USB 보안 플래시 드라이브
12,686.278 ms → 1,354.775 ms
```

실제 Tarkov 성공 12건의 `ReadingTitle → ShowingItem`:

```text
minimum: 38.07 ms
median:  63.92 ms
maximum: 1.05 s
mean:    211.47 ms
```

같은 Scanner cycle의 exact current-pixel evidence만 재사용하며 cross-frame identity cache는 사용하지 않습니다.

## Scanner Ground Truth

정상 Scanner monitoring은 durable automatic correction Case를 만들지 않습니다.

```text
runtime recognition
→ latest exact frame in memory
→ user explicitly opens correction
→ user explicitly saves
→ reviewed durable Ground Truth
```

사용자가 직접 검토/교정한 Case만 Ground Truth입니다. Private user images는 CI 편의를 위해 public repository에 commit하지 않습니다.

## Scanner UI / hotkeys

일반 Scanner 상단:

```text
스캐너 ON/OFF
설정
고급
현재 결과 교정
```

기본 hotkey:

```text
1회 인게임 스캔: Ctrl+Shift+F10
1회 테스트 스캔: Ctrl+Shift+F11
Scanner ON/OFF: Ctrl+Shift+F12
```

Configurable Scanner/Map gesture는 primary key + optional Ctrl/Alt/Shift를 사용합니다. Bare key도 허용하며 Windows modifier는 지원하지 않습니다.

## 업데이트 / 패키지

Release candidate는 Windows Release build, automated tests, self-contained win-x64 single-file publish, rendered Product UI/Scanner/Map smoke, graceful shutdown, package/checksum verification을 모두 통과해야 합니다.

Stable release는 main CI가 성공한 exact main commit의 artifact만 Release workflow가 게시합니다.

Mutable user data는 `%LocalAppData%/JunhyunHelper` 아래에 저장되며 Program Update가 기존 사용자 진행도·설정·reviewed Ground Truth를 덮어쓰지 않습니다.
