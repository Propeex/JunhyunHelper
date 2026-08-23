# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-23

상태: **`v1.3.2 PUBLIC RELEASE / VERIFIED — Scanner live calibration continues from real evidence`**

## 현재 공개 기준선

```text
public stable: v1.3.2
release source: 922797a99ea221fdc4984dd6ed05df552149d6e4
asset: Junhyun-Helper-v1.3.2-win-x64.zip
bytes: 80,311,752
SHA-256: 6e3a7af2de50dfd14f1c49ccb39753177a0bce5b22993bb8bb94ffde93086767
ProductVersion: 1.3.2+922797a99ea221fdc4984dd6ed05df552149d6e4
final PR CI: 32619142034 — SUCCESS
automated tests: 263 passed / 0 failed / 0 skipped
release run: 32621021058
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

상세 공개 검증:

- `docs/RELEASE_1.3.2.md`
- `docs/.release-v1.3.2-status.json`

```text
Desktop Version: 1.3.2
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.3.1 → v1.3.2 mandatory Game Content update: none
v1.3.1 → v1.3.2 user.db migration: none
```

## Scanner v1.3.2

v1.3.1 공개 후 실제 Tarkov/DisplayTest에서 확인된 추가 title-recognition 실패를 근거로 보강했습니다.

```text
Tarkov / Display pixels
→ detail structural candidates
→ dark title field + red close/X
→ left magnifier morphology + first title glyph evidence
→ magnifier-free title ROI
→ Windows ko-KR OCR
→ current-catalog-derived character / symbol sanitation
→ current official Korean catalog semantic matching
→ bounded unique 1-edit recovery when safe
→ optional strict Tarkov-font visual corroboration/recovery
→ Item ID or fail closed
→ local presentation data
→ Mini Scanner
```

핵심 변경:

- magnifier의 ring / hollow center / lower-right handle / expected left-header position이 핵심 evidence
- 뒤따르는 title glyph component는 magnifier의 필수조건이 아니라 corroboration
- punctuation/symbol whitelist는 current official Korean item catalog에서 자동 파생
- current catalog에 없는 punctuation/symbol은 matcher 입력 전에 제거
- normalized length >= 7의 정확히 1 edit 후보는 full current catalog에서 유일하고 global runner-up과 **10%p 이상** 차이가 있을 때만 복구
- multi-edit low-confidence OCR은 strict visual corroboration 없이 확정하지 않음
- global confidence threshold는 낮추지 않음
- false positive보다 miss 선호 유지

상세 계약:

- `docs/SCANNER_V1.3.2_LIVE_EVIDENCE.md`
- `docs/DECISION_SCANNER_LIVE_EVIDENCE_2026-08-23.md`
- `docs/SCANNER_SYMBOL_POLICY.md`

## Scanner 사용자 워크플로

- 실제 Tarkov Scanner와 DisplayTest는 같은 recognition pipeline 사용
- 1회 인게임 스캔: 기본 `Ctrl+Shift+F10`
- 1회 테스트 스캔: 기본 `Ctrl+Shift+F11`
- Scanner ON/OFF: 기본 `Ctrl+Shift+F12`
- 세 global hotkey는 Scanner 탭 밖에서도 동작
- `인식 이미지`에서 최신 실제 분석 frame을 확인하고 사용자가 원하는 위치에 원본 PNG export 가능
- 자동 screenshot 저장 없음
- `로그 삭제`는 recent activity, scanner.log(.1), latest in-memory diagnostic image를 정리하되 사용자 export PNG는 삭제하지 않음

## Scanner 표시 데이터 계약

- 최고 상점가 = 유효한 non-flea RUB 환산 판매가 최댓값
- 플리마켓 평균가 = positive `avg24hPrice`
- slots = positive `width × height`
- 가격/슬롯 = valid price와 slots가 모두 존재할 때만
- 필요한 개수 = `NeededItems[itemId].RequiredTotal`
- Inventory를 차감한 부족량은 Scanner의 `필요 개수` 의미가 아님
- 일부 가격/크기 데이터가 없어도 확정된 Item ID 자체를 폐기하지 않음

## 검증 상태

v1.3.2 공개본에서 확인 완료:

- Release build
- **263 tests / 0 failed / 0 skipped**
- win-x64 self-contained single-file publish/package audit
- catalog-derived symbol sanitation regression
- bounded one-edit safety regression
- live-scale sparse-glyph magnifier regression
- exact packaged EXE Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke
- Draft ZIP re-download + checksum/layout/ProductVersion/FIRST_RUN verification
- Draft-downloaded EXE smoke
- public/latest 전환
- exact public tag source verification
- public ZIP re-download + SHA256SUMS/hash/size/ProductVersion verification
- public-downloaded EXE smoke + graceful shutdown

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout | 구현 완료 |
| Needed Items / Inventory / Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / steady-state smoke 유지 |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / v1.3.2 public package verified |
| Scanner + Mini Scanner | **v1.3.2 public verified / 실제 Tarkov calibration 지속** |

## 현재 개발 방향

새 Scanner 기능을 추가하는 단계가 아니라 실제 Tarkov 사용 결과를 근거로 기존 인식의 정확성·안정성·데이터 연결을 계속 검증하는 단계입니다.

문제가 발생하면 `scanner.log`와 `인식 이미지`/사용자 export PNG를 근거로 capture → structural candidate → header anchors/title ROI → OCR → catalog matcher/visual → presentation → overlay 단계로 분리합니다. 실제 evidence 없이 confidence/margin을 전역 완화하거나 unrelated Scanner 기능을 추가하지 않습니다.
