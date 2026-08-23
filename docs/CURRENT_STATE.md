# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-23

상태: **`v1.3.3 PUBLIC RELEASE / VERIFIED — Scanner live calibration continues`**

## 현재 공개 기준선

```text
public stable: v1.3.3
release source: 41bf5b8374ba774866aab4b60a25376d9b5548c2
public tag source: 41bf5b8374ba774866aab4b60a25376d9b5548c2
asset: Junhyun-Helper-v1.3.3-win-x64.zip
bytes: 80,314,373
SHA-256: 0771d3c7dee5a8f19904d52eeedc7b9abbd6027a7b000255ebd33c296bc2186f
ProductVersion: 1.3.3+41bf5b8374ba774866aab4b60a25376d9b5548c2
final PR CI: 32625223009 — SUCCESS
automated tests: 263 passed / 0 failed / 0 skipped
release run: 32625403609 — SUCCESS
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

상세 공개 검증:

- `docs/RELEASE_1.3.3.md`
- `docs/.release-v1.3.3-status.json`

```text
Desktop Version: 1.3.3
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.3.2 → v1.3.3 mandatory Game Content update: none
v1.3.2 → v1.3.3 user.db migration: none
```

## Scanner v1.3.3

v1.3.2 공개 후 실제 Tarkov 2048×1280 상세창 12개에서 재확인된 title-start / magnifier-anchor 회귀를 수정했습니다.

```text
detail structural candidates
→ red close/X
→ long neutral top frame
→ bounded frame-left search-icon lane
→ 13px-class magnifier bright core
→ dark title field + text evidence
→ HEADER_FRAME_LOCKED
→ magnifier-free title ROI
→ Windows ko-KR OCR
→ current-catalog sanitation
→ current official Korean catalog semantic matching
→ optional strict Tarkov-font visual corroboration/recovery
→ Item ID or fail closed
→ local presentation data
→ Mini Scanner
```

핵심 계약:

- first Korean/title glyph component가 title ROI left edge를 결정하지 않음
- 12개 실측 표본의 header-relative geometry를 packaged-EXE regression smoke에서 재생
- `HEADER_FRAME_LOCKED` + anchor score **0.68 이상**이 아니면 OCR identity path 진입 금지
- partial/failed header lock은 fail closed
- raw Windows OCR과 current-catalog sanitation 후 matcher input을 별도 진단
- current catalog에 없는 `「` 같은 punctuation/symbol은 matcher evidence에서 제거
- normal confidence/top1-top2 margin 및 bounded unique one-edit 안전 조건 유지
- false positive보다 miss 선호 유지

공식 근거:

- `docs/SCANNER_V1.3.3_HEADER_LOCK.md`
- `docs/.scanner-v1.3.3-header-evidence.json`
- `docs/DECISION_SCANNER_HEADER_LOCK_2026-08-23.md`

## Scanner 사용자 워크플로

- 실제 Scanner와 DisplayTest는 같은 recognition pipeline 사용
- 1회 인게임 스캔: 기본 `Ctrl+Shift+F10`
- 1회 테스트 스캔: 기본 `Ctrl+Shift+F11`
- Scanner ON/OFF: 기본 `Ctrl+Shift+F12`
- 세 global hotkey는 Scanner 탭 밖에서도 동작
- `인식 이미지`에서 최신 실제 분석 frame을 확인하고 원본 PNG export 가능
- 자동 screenshot 저장 없음
- 진단창에서 raw OCR과 실제 matcher input을 구분
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

v1.3.3 공개본에서 확인 완료:

- Release build
- **263 tests / 0 failed / 0 skipped**
- 12개 실측 header geometry regression
- win-x64 self-contained single-file publish/package audit
- exact ProductVersion / FIRST_RUN identity
- actual packaged EXE Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke
- Draft asset re-download + package identity verification
- public/latest 전환
- exact public tag source verification
- 독립 public ZIP re-download + SHA256SUMS/hash/size/layout/ProductVersion/FIRST_RUN verification
- public-downloaded EXE smoke + graceful shutdown

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout | 구현 완료 |
| Needed Items / Inventory / Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / steady-state smoke 유지 |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / v1.3.3 public package verified |
| Scanner + Mini Scanner | **v1.3.3 public verified / 실제 Tarkov calibration 지속** |

## 현재 개발 방향

새 Scanner 기능을 추가하는 단계가 아니라 실제 Tarkov 사용 결과를 근거로 기존 인식의 정확성·안정성·데이터 연결을 계속 검증하는 단계입니다.

새 문제가 발생하면 실제 원본 PNG와 `scanner.log`를 근거로 capture → structural candidate → header frame/anchors/title ROI → OCR → catalog sanitation/matcher/visual → presentation → overlay 단계로 분리합니다. 실제 evidence 없이 confidence/margin을 전역 완화하지 않습니다.
