# CURRENT STATE

> 최신 개발 상태의 짧은 인덱스입니다. 상세 설계와 이력은 `docs/STATE.md` 및 전문 문서를 참조합니다.

기준일: 2026-08-23

상태: **`v1.3.4 PUBLIC RELEASE / VERIFIED — Scanner live calibration continues`**

## 현재 공개 기준선

```text
public stable: v1.3.4
release source: a78ddbc649747f1320236556f17e6b908304674a
public tag source: a78ddbc649747f1320236556f17e6b908304674a
asset: Junhyun-Helper-v1.3.4-win-x64.zip
bytes: 80,319,654
SHA-256: 8c442fec81a0b993a9a6b080e59b656668a7a73d8fadd8434595545b08c82e8e
ProductVersion: 1.3.4+a78ddbc649747f1320236556f17e6b908304674a
final PR CI: 32636665202 — SUCCESS
automated tests: 267 passed / 0 failed / 0 skipped
release run: 32636927134 — SUCCESS
independent public verifier: 32637159066 — SUCCESS
public/latest: VERIFIED
exact public tag source: VERIFIED
Draft/public re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

상세 공개 검증:

- `docs/RELEASE_1.3.4.md`
- `docs/.release-v1.3.4-status.json`

```text
Desktop Version: 1.3.4
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v4
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.3.3 → v1.3.4 mandatory Game Content update: none
v1.3.3 → v1.3.4 user.db migration: none
```

## Scanner v1.3.4

v1.3.3 공개 후 실제 Tarkov 사용에서 재현된 네 결함을 결합해 수정했습니다.

```text
detail structural candidates
→ red close/X + normalized X template
→ long neutral top frame
→ fixed frame-left search-icon lane
→ normalized magnifier ring/hollow/handle template
→ dark title field + text evidence
→ full HEADER_FRAME_LOCKED only
→ locked-header-based detail bounds refinement
→ magnifier-free title ROI
→ Windows ko-KR OCR
→ current-catalog sanitation
→ optional one-unknown-glyph recovery
→ current official Korean catalog semantic matching
→ optional strict Tarkov-font visual corroboration/recovery
→ Item ID or fail closed
→ local presentation data
→ Mini Scanner
```

핵심 계약:

- title glyph는 fixed search-icon lane 밖에서 magnifier candidate가 될 수 없음
- close/X는 red color뿐 아니라 diagonal-X shape evidence를 결합
- `HEADER_FRAME_LOCKED` + anchor score **0.68 이상** + valid magnifier/X가 아니면 OCR identity path 진입 금지
- initial structural detail bounds는 full lock 후 실제 magnifier/X에서 top/left/right 재정렬
- `Esma「ch` 같은 current-catalog 밖 embedded symbol은 특정 문자로 치환하지 않고 `?` one-unknown-glyph evidence로 별도 보존
- unknown-glyph recovery는 complete current catalog에서 같은 길이/나머지 slot이 정확히 하나이고 global runner-up과 10%p 이상 벌어질 때만 허용
- normal confidence/top1-top2 margin 및 기존 bounded unique one-edit 안전 조건 유지
- false positive보다 miss 선호 유지

공식 근거:

- `docs/SCANNER_V1.3.4_LIVE_HARDENING.md`
- `docs/DECISION_SCANNER_V1.3.4_LIVE_HARDENING_2026-08-23.md`
- `docs/RELEASE_1.3.4.md`

## Scanner 사용자 워크플로

- 실제 Scanner와 DisplayTest는 같은 recognition pipeline 사용
- 1회 인게임 스캔: 기본 `Ctrl+Shift+F10`
- 1회 테스트 스캔: 기본 `Ctrl+Shift+F11`
- Scanner ON/OFF: 기본 `Ctrl+Shift+F12`
- 세 global hotkey는 Scanner 탭 밖에서도 동작
- `인식 이미지`에서 최신 실제 분석 frame을 확인
- 자동 screenshot 저장 없음
- explicit PNG export에는 **초록=상세창 / 파랑=제목 ROI / 노랑=돋보기 / 빨강=닫기 X** 진단 rectangle 포함
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

v1.3.4 공개본에서 확인 완료:

- Release build
- **267 tests / 0 failed / 0 skipped**
- live-derived header geometry / decoy magnifier regression
- unknown-glyph unique/ambiguous/short-title fail-closed regression
- diagnostic PNG four-color overlay smoke
- win-x64 self-contained single-file publish/package audit
- exact ProductVersion / FIRST_RUN identity
- actual packaged EXE Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke
- Draft asset re-download + package identity verification + EXE smoke
- public/latest 전환
- exact public tag source verification
- 독립 public ZIP re-download + SHA256SUMS/hash/size/layout/ProductVersion/FIRST_RUN verification
- public-downloaded EXE smoke + graceful shutdown
- one-shot release/public verifier workflow cleanup

## 기능 상태

| 영역 | 상태 |
|---|---|
| Profile / Quest / Hideout | 구현 완료 |
| Needed Items / Inventory / Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / steady-state smoke 유지 |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / v1.3.4 public package verified |
| Scanner + Mini Scanner | **v1.3.4 public verified / 실제 Tarkov calibration 지속** |

## 현재 개발 방향

새 Scanner 기능을 추가하는 단계가 아니라 실제 Tarkov 사용 결과를 근거로 기존 인식의 정확성·안정성·데이터 연결을 계속 검증하는 단계입니다.

새 문제가 발생하면 v1.3.4의 색상 진단 PNG와 `scanner.log`를 근거로 capture → structural candidate → close/frame/magnifier template → locked bounds/title ROI → raw OCR → catalog sanitation/unknown-glyph/matcher/visual → presentation → overlay 단계로 분리합니다. 실제 evidence 없이 confidence/margin을 전역 완화하지 않습니다.
