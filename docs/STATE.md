# STATE — 현재 프로젝트 상태

> 새 대화/새 개발자는 이 문서를 먼저 읽습니다. 대화 기억이 아니라 저장소의 공식 문서와 코드가 프로젝트의 기준입니다.

기준일: 2026-08-22

## 1. 제품

**준현 헬퍼**는 Escape from Tarkov 플레이를 지원하는 Windows x64 .NET 10 WPF 데스크톱 프로그램입니다.

핵심 기능:

- GameMode별 Profile / User Progress
- Quest availability / Hideout / Needed Items / Inventory
- Items / Ammo
- Map + MiniMap
- Game Content 안전 업데이트
- 사용자 동의형 Program Update
- Scanner + Mini Scanner

Runtime GPT/AI 의존성은 없습니다.

## 2. 현재 공개 릴리즈

현재 public stable은 **v1.1.6**입니다.

```text
version: v1.1.6 PUBLIC RELEASE / VERIFIED
release source: 8efee02e5966adb9b67b47847f95a12dfc357d0a
exact-source release run: 32500707112 — SUCCESS
automated tests: 250 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.6-win-x64.zip
bytes: 80,271,024
SHA-256: 986d0d2855381060267f63d2902317eabedc5d5738448fbd6c2b09e764c3477e
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Desktop Version: 1.1.6
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner cache schema: v1/v2 readable, v2 written
v1.1.5 → v1.1.6 mandatory Game Content update: none
v1.1.5 → v1.1.6 user.db migration: none
```

v1.1.6은 v1.1.5의 Scanner `아이템 목록 최신화` 회귀를 수정한 PATCH입니다. 상세 검증 기록은 `docs/RELEASE_1.1.6.md`에 있습니다.

## 3. Scanner 제품 계약

Scanner는 독립적인 화면 기반 보조 기능입니다.

```text
Tarkov / Display pixels
→ detail-window structural candidates
→ title ROI
→ Windows ko-KR OCR
→ current official Korean full-item catalog matching
→ Item ID
→ local JunhyunHelper presentation data
→ Mini Scanner
```

장기 원칙:

- geometry/structural score는 후보 생성·순위일 뿐 Item identity가 아님
- false positive보다 miss 선호
- matcher confidence/top1-top2 margin을 편의상 완화하지 않음
- historical alias를 production identity source로 무제한 누적하지 않음
- scan-time network 금지
- game memory / DLL injection / packet interception 금지
- icon 하나만으로 Item identity 확정 금지
- current official Korean catalog가 identity 기준

## 4. Scanner / Mini Scanner 현재 구조

v1.1.5 기준으로 다음이 구현되어 있습니다.

### Mini Scanner

- item match 성공 시 아이템 정보만 표시
- runtime/OCR/error/status 문구는 Mini Scanner에 표시하지 않음
- WPF Topmost + native HWND_TOPMOST
- ShowActivated=false / no-activate
- 전체 카드가 drag hit surface
- drag 중 Arrow cursor 유지
- MiniMap과 독립적인 window/service/settings lifecycle
- 실제 Scanner에서는 Tarkov foreground + inventory/stash context를 보수적으로 확인
- 불확실한 inventory context는 fail-closed로 숨김

### OCR concurrency

- item-title OCR과 inventory-context OCR은 하나의 serialized OCR boundary를 공유
- 동시에 WinRT OCR을 실행하지 않음

### Icon cache

- Game Content update에서 canonical item 전체 icon을 prefetch
- 실제 scan 순간에는 새 icon을 다운로드하지 않음
- 개별 icon 실패는 전체 update를 중단하지 않음

## 5. Scanner catalog / market 계약

### Identity health

Scanner identity catalog는 다음 기준으로 건강성을 판단합니다.

```text
accepted item count >= 4000
AND every accepted item has non-empty Item ID
AND every accepted item has non-empty official name
```

시장 데이터는 identity health와 분리합니다.

### Market fields

- raw `traderPrices` 지원
- derived `sellFor` 지원
- best trader price = 유효한 non-flea RUB 환산 최고가
- flea average = positive `avg24hPrice`
- slots = positive width × height
- price/slot = valid price와 slots가 모두 있을 때만 계산
- market/dimension이 없거나 잘못되면 해당 표시 필드만 비움

따라서 4,000개 유효 identity에 trader price가 0개여도 Scanner 식별은 가능하며, 3,999개 identity는 구조적으로 불완전하므로 거부합니다.

### Current needed

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

Inventory 차감 부족량을 Scanner 의미로 사용하지 않습니다. Item이 NeededItems에 없으면 0입니다.

## 6. Scanner diagnostics

개발자 로그:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log
%LocalAppData%/JunhyunHelper/logs/scanner.log.1
```

기록 대상:

- runtime state
- detail geometry/candidate
- OCR result
- matcher result/confidence
- inventory context result
- manual catalog sync outcome/item/trader/flea counts

전체 screenshot/raw pixel은 저장하지 않습니다.

Scanner 탭의 `로그 삭제`는 최근 인식 activity와 두 log 파일을 함께 지웁니다. 진단 I/O 실패는 Scanner runtime을 중단시키지 않습니다.

## 7. Persistence

```text
%LocalAppData%/JunhyunHelper/user.db
%LocalAppData%/JunhyunHelper/content/
%LocalAppData%/JunhyunHelper/image-cache/
%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/catalog/
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

Game Content/User Progress/Scanner preferences/catalog/logs는 program package와 분리되어 있습니다.

## 8. Map / MiniMap

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

Map/MiniMap은 독립 subsystem이며 Quest projection만 JunhyunHelper와 bridge합니다. 안정적인 donor path는 구체적 defect/performance 근거 없이 broad refactor하지 않습니다.

## 9. Program Update / 배포

정식 release는 Draft-first입니다.

```text
exact release source
→ build/tests/publish/smoke
→ ZIP + SHA256SUMS
→ Draft release
→ Draft asset re-download verification
→ Draft-downloaded EXE smoke
→ public/latest
→ exact tag verification
→ public asset re-download verification
→ public-downloaded EXE smoke
```

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

업데이트는 program-owned files만 교체하며 `%LocalAppData%/JunhyunHelper` 사용자 데이터는 건드리지 않습니다.

릴리즈 완료 후 일회성 release/verify workflow와 status marker는 저장소에서 제거합니다. 상시 workflow는 `.github/workflows/ci.yml`만 유지하는 것이 기본입니다.

## 10. v1.1.6 검증 결과

Exact-source release run `32500707112`에서 다음을 모두 통과했습니다.

- Windows Release build
- **250 automated tests / 0 failure / 0 skipped**
- win-x64 self-contained single-file publish
- ProductVersion/FIRST_RUN exact version verification
- actual published EXE Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke
- graceful shutdown / clean portable root
- Draft package re-download checksum/root/ProductVersion/FIRST_RUN verification
- Draft-downloaded EXE smoke
- public/latest transition
- exact tag source verification
- public package re-download verification
- public-downloaded EXE smoke

## 11. 실제 Tarkov 후속 검증

최신 Tarkov live E2E는 public release blocker가 아니며 사용자 환경에서 계속 검증합니다.

실제 사용 중 문제가 발생하면 한꺼번에 추정해 고치지 않고 다음 단계로 분리합니다.

1. capture source/window state
2. detail candidate geometry
3. title ROI
4. OCR
5. catalog/matcher
6. item presentation data
7. inventory/stash visibility gate
8. Mini Scanner window behavior
9. performance/resource behavior

사용자 보고와 `scanner.log`를 근거로 재현 조건을 고정한 뒤 PATCH에서 수정합니다.

## 12. 기능 상태

| 영역 | 상태 |
|---|---|
| Profile | 구현 완료 |
| Quest | 구현 완료 / fail-closed availability |
| Hideout | 구현 완료 |
| Needed Items / Inventory | 구현 완료 |
| Items | 구현 완료 |
| Ammo | 구현 완료 |
| Map + MiniMap | 구현 완료 / 기존 검증 기준선 유지 |
| Game Content Update | 구현 완료 |
| Program Update | 구현 완료 / v1.1.6 public package verified |
| Scanner + Mini Scanner | **v1.1.6 public baseline / live Tarkov validation 및 후속 수정 진행 대상** |

## 13. 새 작업 시작 순서

1. `README.md`
2. `docs/STATE.md`
3. `docs/CURRENT_STATE.md`
4. `docs/PRODUCT.md`
5. `docs/DECISIONS.md`
6. `docs/DEVELOPER_REFERENCE.md`
7. `docs/ARCHITECTURE.md`
8. 관련 전문 문서와 현재 코드/tests

현재 코드가 존재한다는 이유만으로 그 동작을 제품 요구사항으로 추정하지 않습니다. 사용자 확정 요구사항과 공식 문서가 우선합니다.
