# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-23

상태: **`v1.3.1 PUBLIC RELEASE / VERIFIED — Scanner live-evidence calibration ongoing`**

## 현재 공개 기준선

현재 public stable은 **v1.3.1**입니다.

```text
public stable: v1.3.1
release source: 028bfb600f4662962a0daac1dad04b570e018275
asset: Junhyun-Helper-v1.3.1-win-x64.zip
bytes: 80,310,221
SHA-256: 5c4b79cc5d373b4a28cbeb10be18b8369086b2ee9f0edc172530028dd71b1c3f
ProductVersion: 1.3.1+028bfb600f4662962a0daac1dad04b570e018275
final PR CI: 32615869812 — SUCCESS
automated tests: 256 passed / 0 failed / 0 skipped
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

상세 공개 검증: `docs/RELEASE_1.3.1.md`, `docs/.release-v1.3.1-status.json`.

```text
Desktop Version: 1.3.1
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.3.0 → v1.3.1 mandatory Game Content update: none
v1.3.0 → v1.3.1 user.db migration: none
```

## v1.3.1 Scanner recognition hardening

실제 인게임 실패 사례에서 상세창 좌측 돋보기 대신 아이템 이름 첫 글자가 anchor로 선택되는 패턴이 확인되어 title extraction을 강화했습니다.

```text
Tarkov / Display pixels
→ detail structural candidates
→ dark title-field strip
→ right red X / close evidence
→ left magnifier shape evidence
   - size/aspect
   - hollow center
   - ring perimeter
   - lower-right handle
   - following title-glyph evidence
→ first title-glyph start
→ title ROI between magnifier and close control
→ Windows ko-KR OCR
→ current official Korean catalog semantic match
→ optional local Tarkov-font visual corroboration/recovery
→ conservative confidence + margin gate
→ Item ID
→ local presentation data
→ Mini Scanner
```

핵심 계약:

- 상세창 panel left가 소폭 안쪽으로 drift해도 magnifier 검색 범위를 왼쪽으로 확장해 실제 아이콘을 다시 찾음
- 첫 한글 글자를 magnifier로 잘못 선택해 제목 첫 글자를 잘라내는 회귀를 packaged-EXE smoke에서 재현/차단
- title field의 어두운 배경색, 좌측 magnifier, 우측 red X, 실제 첫 글자군을 독립 evidence로 사용
- OCR semantic success도 필요한 경우 현재 Tarkov 로컬 제목 폰트와 current official catalog 렌더링으로 보수적으로 corroborate
- strict visual evidence가 다른 current official Item ID를 명확히 지목할 때만 OCR identity 교정
- font evidence unavailable/inconclusive이면 기존 healthy OCR result를 임의로 버리지 않음
- false positive보다 miss 선호 및 current official Korean catalog identity authority 유지
- scan-time network 없음
- game memory / DLL injection / packet interception 없음

상세 계약: `docs/SCANNER_V1.3.1_RECOGNITION.md`.

## 상단 버전 표시

- MainWindow 상단 상태 텍스트의 왼쪽에 현재 실행 EXE 버전을 표시
- UI 문자열에 버전을 하드코딩하지 않고 `AssemblyInformationalVersion`을 사용
- build metadata는 UI에서 제외
- 예: `v1.3.1   정리 필요`

## v1.3.0부터 유지되는 Scanner 실사용/분석 워크플로

- 최신 recognition 원본 frame을 `인식 이미지`에서 사용자 지정 PNG로 export
- export PNG는 diagnostic overlay가 합성되지 않은 실제 분석 원본
- 자동 screenshot 저장 없음
- one-shot TarkovWindow scan
- one-shot DisplayTest scan: 모든 연결 디스플레이를 한 번만 동일 pipeline으로 검사
- Scanner 탭 one-shot 버튼 없음
- MainWindow lifetime의 3종 global hotkey
  - 인게임 1회: `Ctrl+Shift+F10`
  - 테스트 1회: `Ctrl+Shift+F11`
  - Scanner ON/OFF: `Ctrl+Shift+F12`
- hotkey 설정/비활성화/중복 차단
- schema v3 사용자 one-shot gesture 보존 → schema v4 자동 승계
- `로그 삭제`는 recent activity/current+rotated scanner log/latest in-memory image를 정리하지만 사용자 export PNG는 삭제하지 않음

## Scanner 표시 데이터 계약

- 최고 상점가 = 유효한 non-flea RUB 판매가 최댓값
- 플리마켓 평균가 = positive `avg24hPrice`
- 가격/슬롯 = 유효한 `width × height` 슬롯 수 기준
- 현재 필요한 수량 = `NeededItems[itemId].RequiredTotal`
- identity가 확정된 뒤 일부 가격/크기 데이터가 없어도 Item ID 자체를 버리지 않음

## 검증 상태

v1.3.1 공개본에서 다음을 검증했습니다.

- Release build / 256 tests
- win-x64 self-contained single-file publish/package audit
- inspect-header synthetic regression
- Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap actual EXE smoke
- exact public tag source
- public/latest
- 공개 ZIP 재다운로드 + SHA256SUMS
- ProductVersion / FIRST_RUN / root layout
- 공개 다운로드본 actual EXE smoke + graceful shutdown

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout | 구현 완료 |
| Needed Items / Inventory / Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / steady-state smoke 유지 |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / v1.3.1 public package verified |
| Scanner + Mini Scanner | **v1.3.1 public verified / 실제 Tarkov calibration 진행 중** |

실제 Tarkov에서 발견되는 recognition 문제는 `scanner.log`와 `인식 이미지`/사용자 export PNG를 근거로 capture → candidate → title-field/anchors/ROI → OCR/visual matcher → catalog → presentation → overlay 단계로 분리합니다. Live evidence 없이 confidence/margin을 임의로 낮추지 않습니다.
