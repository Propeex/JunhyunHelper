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

현재 public stable은 **v1.2.0**입니다.

```text
version: v1.2.0 PUBLIC RELEASE / VERIFIED
release source: a7601f8498e8d75e832962fb9dd60f4112d28dc6
exact-source release run: 32514322439 — SUCCESS
automated tests: 255 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.2.0-win-x64.zip
bytes: 80,298,514
SHA-256: ab5e9ef35b300268d16a1c5eece86cd8c6e57c91c83364caf4b7d02cde1d27d1
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Desktop Version: 1.2.0
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v3
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.1.6 → v1.2.0 mandatory Game Content update: none
v1.1.6 → v1.2.0 user.db migration: none
```

상세 검증 기록은 `docs/RELEASE_1.2.0.md`에 있습니다.

## 3. Scanner 제품 계약

Scanner는 독립적인 화면 기반 보조 기능입니다.

```text
Tarkov / Display pixels
→ detail-window structural candidates
→ red close + magnifier + title-field anchor refinement
→ magnifier-free title ROI
→ Windows ko-KR OCR + current-catalog character validation
→ current official Korean catalog semantic matching
   OR conservative full-catalog Tarkov-font visual recovery
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

## 4. Scanner v1.2.0 recognition 구조

### Detail/title geometry

- Scanner Lab v3.8의 structural candidate 생성과 fail-closed geometry 계약을 유지합니다.
- 빨간 X, 돋보기와 title-field 구조를 title anchor evidence로 사용합니다.
- magnifier anchor가 확실하면 OCR title ROI의 왼쪽 경계를 돋보기 오른쪽으로 이동합니다.
- anchor가 불확실하면 기존 검증된 Scanner Lab title ROI로 fallback합니다.
- geometry만으로 Item ID를 확정하지 않습니다.

### OCR character policy

- Windows `ko-KR` OCR을 primary text path로 유지합니다.
- 현재 공식 한국어 item-name catalog에서 실제 허용 문자를 계산합니다.
- catalog에 없는 비정상 문자는 손상 OCR evidence로 취급합니다.
- Han ideograph는 Korean item-title contract에서 invalid evidence입니다.
- 임의 문자 치환으로 confidence를 인위적으로 높이지 않습니다.

### Tarkov-font visual recovery

- OCR이 비거나 손상되었을 때 공식 전체 item-name 집합을 시각적으로 대조할 수 있습니다.
- visual path는 별도의 일반 OCR replacement가 아니라 catalog-constrained recovery입니다.
- confidence와 top1/top2 margin을 모두 통과해야 합니다.
- ambiguous 결과는 Item ID를 확정하지 않습니다.

## 5. 1회 고정밀 스캔 / 단축키

- 실시간 Scanner가 OFF여도 `1회 고정밀 스캔`을 실행할 수 있습니다.
- 기본 global hotkey: `Ctrl+Shift+F10`.
- Scanner UI에서 단축키 변경 또는 비활성화가 가능합니다.
- 설정 schema는 v3이며 기존 설정은 정규화/마이그레이션합니다.
- one-shot은 현재 local Scanner catalog만 사용하며 scan-time network를 시작하지 않습니다.

실시간 Scanner/Test가 켜진 상태에서 one-shot을 실행하면:

1. 기존 runtime loop cancel 요청
2. 기존 loop Task의 실제 종료 await
3. one-shot detector/OCR/presentation 수행
4. 최신 사용자 설정이 여전히 요청하는 경우 이전 mode 복구

따라서 continuous loop와 one-shot이 같은 capture/OCR/presentation state를 동시에 변경하지 않습니다.

## 6. 인식 이미지 / diagnostics

`인식 이미지`는 process memory에 최신 diagnostic frame 1개만 유지합니다.

표시 가능한 정보:

- capture image
- selected detail bounds
- title ROI
- magnifier / close anchor bounds
- structural/title-anchor evidence
- OCR/visual recognition pass
- OCR text
- candidate official name
- confidence / second score

스크린샷은 디스크에 저장하지 않습니다. 최종 recognition 결과가 선택된 뒤 debug metadata를 갱신하여 버려진 candidate의 score가 최종 결과처럼 보이지 않도록 합니다.

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
- one-shot candidate/selection diagnostics

Scanner 탭의 `로그 삭제`는 recent activity와 두 log 파일을 함께 지웁니다. 진단 I/O 실패는 Scanner runtime을 중단시키지 않습니다.

## 7. Mini Scanner 현재 구조

- item match 성공 시 아이템 정보만 표시
- runtime/OCR/error/status 문구는 Mini Scanner에 표시하지 않음
- WPF Topmost + native HWND_TOPMOST
- ShowActivated=false / no-activate
- 전체 카드가 drag hit surface
- drag 중 Arrow cursor 유지
- MiniMap과 독립적인 window/service/settings lifecycle
- 실제 Scanner에서는 Tarkov foreground + inventory/stash context를 보수적으로 확인
- 불확실한 inventory context는 fail-closed로 숨김

Title OCR과 inventory-context OCR은 serialized WinRT OCR boundary를 공유합니다.

## 8. Scanner catalog / market 계약

Identity catalog health:

```text
accepted item count >= 4000
AND every accepted item has non-empty Item ID
AND every accepted item has non-empty official name
```

시장 데이터는 identity health와 분리합니다.

- raw `traderPrices` 지원
- derived `sellFor` 지원
- best trader price = 유효한 non-flea RUB 환산 최고가
- flea average = positive `avg24hPrice`
- slots = positive width × height
- price/slot = valid price와 slots가 모두 있을 때만 계산
- market/dimension이 없거나 잘못되면 해당 표시 필드만 비움

4,000개 유효 identity에 trader price가 0개여도 Scanner 식별은 가능하며, 3,999개 identity는 구조적으로 불완전하므로 거부합니다.

현재 필요한 수량:

```text
ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal
```

Inventory 차감 부족량을 Scanner 의미로 사용하지 않습니다.

## 9. Icon cache

- Game Content update에서 canonical item 전체 icon을 prefetch
- 실제 scan 순간에는 새 icon을 다운로드하지 않음
- 개별 icon 실패는 전체 update를 중단하지 않음
- 성공적으로 decode된 동일 아이콘은 process-local memory cache로 재사용 가능

## 10. Persistence

```text
%LocalAppData%/JunhyunHelper/user.db
%LocalAppData%/JunhyunHelper/content/
%LocalAppData%/JunhyunHelper/image-cache/
%LocalAppData%/JunhyunHelper/scanner-settings.json(.bak)
%LocalAppData%/JunhyunHelper/scanner/catalog/
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

Game Content/User Progress/Scanner preferences/catalog/logs는 program package와 분리되어 있습니다.

## 11. Map / MiniMap

Pinned donor revision:

```text
d933792b6042a51cea38dc44b686a096fe30de67
```

Map/MiniMap은 독립 subsystem이며 Quest projection만 JunhyunHelper와 bridge합니다. 안정적인 donor path는 구체적 defect/performance 근거 없이 broad refactor하지 않습니다.

v1.2.0 exact-source release의 첫 실행은 기존 Main Map asynchronous smoke의 off-floor marker settle timing assertion에서 중단됐습니다. ZIP/Draft 생성 전이었고 제품 source는 변경하지 않았습니다. 동일 exact source job을 한 번 clean rerun하여 같은 Map smoke와 이후 모든 Draft/Public gate를 통과했습니다. 이 이력은 `docs/RELEASE_1.2.0.md`에 기록합니다.

## 12. Program Update / 배포

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

## 13. v1.2.0 검증 결과

Exact-source release run `32514322439`에서 다음을 모두 통과했습니다.

- exact release source `a7601f8498e8d75e832962fb9dd60f4112d28dc6`
- Windows Release build
- **255 automated tests / 0 failure / 0 skipped**
- win-x64 self-contained single-file publish
- exact ProductVersion/FIRST_RUN verification
- package root / dependency / PDB / nested archive audit
- published EXE Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke
- Scanner schema v3/default hotkey smoke
- synthetic magnifier-exclusion title ROI smoke
- graceful shutdown / clean portable root
- Draft package re-download checksum/root/ProductVersion/FIRST_RUN verification
- Draft-downloaded EXE smoke
- public/latest transition
- exact tag source verification
- public package re-download verification
- public-downloaded EXE smoke

Public asset:

```text
Junhyun-Helper-v1.2.0-win-x64.zip
80,298,514 bytes
SHA-256 ab5e9ef35b300268d16a1c5eece86cd8c6e57c91c83364caf4b7d02cde1d27d1
ProductVersion 1.2.0+a7601f8498e8d75e832962fb9dd60f4112d28dc6
```

## 14. 실제 Tarkov 후속 검증

최신 Tarkov live E2E는 public release blocker가 아니며 사용자 환경에서 계속 검증합니다.

실제 사용 중 문제가 발생하면 다음 단계로 분리합니다.

1. capture source/window state
2. detail candidate geometry
3. close/magnifier/title anchor evidence
4. title ROI
5. OCR character policy
6. semantic/visual matcher
7. catalog Item ID
8. item presentation data
9. inventory/stash visibility gate
10. Mini Scanner window behavior
11. performance/resource behavior

사용자 보고, `scanner.log`, `인식 이미지`를 근거로 재현 조건을 고정한 뒤 PATCH에서 수정합니다.

## 15. 기능 상태

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
| Program Update | 구현 완료 / v1.2.0 public package verified |
| Scanner + Mini Scanner | **v1.2.0 public baseline / live Tarkov validation 및 후속 수정 진행 대상** |

## 16. 새 작업 시작 순서

1. `README.md`
2. `docs/STATE.md`
3. `docs/CURRENT_STATE.md`
4. `docs/PRODUCT.md`
5. `docs/DECISIONS.md`
6. `docs/DEVELOPER_REFERENCE.md`
7. `docs/ARCHITECTURE.md`
8. 관련 전문 문서와 현재 코드/tests

현재 코드가 존재한다는 이유만으로 그 동작을 제품 요구사항으로 추정하지 않습니다. 사용자 확정 요구사항과 공식 문서가 우선합니다.
