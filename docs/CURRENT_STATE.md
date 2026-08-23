# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-23

상태: **`v1.3.3 RELEASE CANDIDATE — v1.3.2 PUBLIC VERIFIED`**

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
release run: 32621021058 — SUCCESS
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

## v1.3.3 Scanner candidate

사용자가 제공한 실제 2048×1280 Tarkov 상세창 12개를 다시 측정해 title ROI ownership을 수정했다.

```text
detail structural candidate
→ red close/X
→ long neutral top frame
→ bounded frame-left search-icon lane
→ 13px-class magnifier bright core
→ dark title field + text-presence corroboration
→ HEADER_FRAME_LOCKED
→ magnifier-free title ROI
→ Windows ko-KR OCR
→ current-catalog sanitation
→ semantic/visual identity or fail closed
```

핵심 변경:

- first Korean/title glyph component가 ROI left edge를 결정하지 않음
- 12개 실제 표본의 header width 822~862px, close 25~27×16~17px, magnifier core 13×13px, X offset 11~13px, Y offset 7px, title gap 5~6px를 회귀 근거로 사용
- partial/failed header lock은 anchor score 0.47 이하로 제한되어 OCR identity path에 진입하지 못함
- raw OCR과 current-catalog sanitation 이후 matcher input을 별도로 기록/표시
- `「` 같은 current catalog 밖 punctuation은 matcher input에서 제거
- normal confidence/top1-top2 margin과 bounded unique one-edit 안전 조건은 완화하지 않음
- highest trader / flea avg24hPrice / RequiredTotal 의미와 schema는 변경하지 않음

검증 상태:

- v1.3.3 candidate CI `32624123821`: SUCCESS
- follow-up CI `32624855995`: SUCCESS
- 263 tests / 0 failed / 0 skipped
- Windows build/publish + actual packaged EXE Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke: SUCCESS
- 12개 실제 geometry를 모두 재생하는 cleaned final-head CI: release blocker
- Draft/public exact-source package verification: release blocker

공식 근거:

- `docs/SCANNER_V1.3.3_HEADER_LOCK.md`
- `docs/.scanner-v1.3.3-header-evidence.json`
- `docs/DECISION_SCANNER_HEADER_LOCK_2026-08-23.md`
- `docs/RELEASE_1.3.3.md`

## 고정 데이터/호환성

```text
Desktop Version: 1.3.3 candidate
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.3.2 → v1.3.3 mandatory Game Content update: none
v1.3.2 → v1.3.3 user.db migration: none
```

## Scanner 사용자 워크플로

- real Scanner와 DisplayTest는 같은 recognition pipeline 사용
- 1회 인게임 스캔: 기본 `Ctrl+Shift+F10`
- 1회 테스트 스캔: 기본 `Ctrl+Shift+F11`
- Scanner ON/OFF: 기본 `Ctrl+Shift+F12`
- 세 global hotkey는 Scanner 탭 밖에서도 동작
- `인식 이미지`에서 최신 원본 frame을 보고 PNG export 가능
- 자동 screenshot 저장 없음
- 진단창에서 raw OCR과 실제 matcher input을 구분

## Scanner 표시 데이터 계약

- 최고 상점가 = 유효한 non-flea RUB 환산 판매가 최댓값
- 플리마켓 평균가 = positive `avg24hPrice`
- slots = positive `width × height`
- 가격/슬롯 = valid price와 slots가 모두 존재할 때만
- 필요한 개수 = `NeededItems[itemId].RequiredTotal`
- Inventory를 차감한 부족량은 Scanner의 `필요 개수` 의미가 아님
- 일부 가격/크기 데이터가 없어도 확정된 Item ID 자체를 폐기하지 않음
