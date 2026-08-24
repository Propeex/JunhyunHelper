# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 릴리즈 상태

현재 public stable은 **v1.4.4**입니다.

```text
version: v1.4.4 PUBLIC RELEASE / VERIFIED
release source/tag: 0c7f31e118122ffef6e5999f7a20a77d823a450d
asset: Junhyun-Helper-v1.4.4-win-x64.zip
SHA-256: 64320e36ba94b6f206ef997e3d42a809c7beef2c859f4bc7f53f704f74866f40
ProductVersion: 1.4.4+0c7f31e118122ffef6e5999f7a20a77d823a450d
public/latest: VERIFIED
exact public tag source: VERIFIED
public re-download / checksum / package layout: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

공개 검증 기록은 `docs/.release-v1.4.4-status.json`에 있습니다.

**v1.5.0 Product Finishing Pass**는 구현 완료 후 final release gate를 진행 중입니다. 공식 범위와 현재 상태는 다음 문서가 기준입니다.

- `docs/DECISION_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md`
- `docs/STATUS_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md`
- `docs/RELEASE_NOTES_V1.5.0.md`

```text
Content schema: v7
Readable content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v5
Scanner catalog cache: v1/v2 readable, v2 written
```

## 주요 기능

- GameMode별 Profile
- Quest / prerequisite / special trader / profile-variable 판정
- Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / cleanup safety / consumption ledger
- Items / cross-navigation
- Ammo / favorites
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Scanner + Mini Scanner
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## Scanner

Production Scanner는 게임 화면 픽셀만 사용합니다.

```text
Tarkov window pixels
→ detail-window rectangle proposals
→ red close-X + magnifier + neutral header semantic validation
→ HEADER_FRAME_LOCKED
→ item-name ROI
→ Windows ko-KR OCR
→ optional user OCR substitution
→ current-catalog sanitation / normalization
→ conservative catalog matching / bounded recovery
→ Item ID or fail closed
→ local mapped presentation data
→ Mini Scanner
```

핵심 안전 원칙:

- false positive보다 miss 선호
- geometry만으로 Item identity 확정 금지
- `HEADER_FRAME_LOCKED >= 0.68`
- magnifier + red close-X 필수
- structural floor `0.34`
- continuous candidate max `8`
- one-shot candidate max `12`
- current official Korean Tarkov item catalog가 identity authority
- matcher/visual ambiguity는 fail closed
- production OCR field는 item-name 하나
- 가격·슬롯·필요 수량은 Item ID 확정 후 local mapped data
- scan-time network 없음
- game memory read / DLL injection / packet interception 없음
- 자동 global `r/0/한글` 강제 substitution table 없음

### v1.5.0 Scanner 제품 흐름

일반 Scanner 화면은 실제 플레이 동선에 필요한 기능을 우선합니다.

- `스캐너 ON/OFF`
- `1회 스캔`
- `현재 결과 교정`
- runtime status
- 최근 인식 기록

`설정`에는 전역 단축키, OCR 문자 치환, Mini Scanner 표시 항목이 있습니다.

`고급 / 진단`에는 display test, 인식 이미지, Ground Truth 회귀/관리/내보내기, Scanner catalog 강제 최신화, 로그 삭제를 둡니다. 진단 기능은 삭제하지 않고 일반 사용 surface에서 분리합니다.

Mini Scanner에서는 우클릭 → `현재 결과 교정`으로 방금 본 결과를 바로 교정할 수 있습니다.

## OCR 문자 치환

반복해서 확인한 OCR 오인식은 사용자가 Scanner 설정에 exact 문자열 치환 규칙으로 등록할 수 있습니다.

```text
raw OCR
→ user substitution (single pass)
→ catalog sanitation / normalization
→ matching
```

- 규칙 추가 / 삭제 / ON·OFF / 초기화
- 기본 규칙은 비어 있음
- recursive replacement / substitution chain 없음
- raw OCR 원문은 forensic diagnostic evidence로 별도 보존

## Scanner 표시 데이터

Item ID 확정 후 다음 값은 OCR이 아니라 local data에서 조회합니다.

- 최고 상점가 = 유효한 non-flea RUB 환산 판매가 최댓값
- 최고가 판매 상인
- 플리마켓 평균가 = positive `avg24hPrice`
- 슬롯 = positive `width × height`
- 상인/플리 가격·슬롯
- 필요한 개수 = `NeededItems[itemId].RequiredTotal`

Inventory를 차감한 부족량은 Scanner의 `필요 개수` 의미가 아닙니다. 가격/크기 일부가 없으면 해당 표시 필드만 비우고 Item identity를 폐기하지 않습니다.

## 교정 / Ground Truth

교정은 detector가 실제 생성한 후보를 선택하는 방식이 기본입니다.

1. detail rectangle 후보
2. red close-X 후보
3. magnifier 후보
4. item-name ROI 후보
5. 정답 item/text
6. 저장

정답 후보가 없으면 manual rectangle을 직접 지정할 수 있고, 실제로 없어야 하는 semantic object는 `없음`으로 기록할 수 있습니다. Candidate ID / rank / score / geometry가 Ground Truth와 함께 저장됩니다.

기본 진단 저장 위치:

```text
%LocalAppData%\JunhyunHelper\scanner\diagnostics
```

사용자가 확인한 Ground Truth는 자동 retention 대상이 아닙니다. 자동 미검토 diagnostic Case만 30일 / 300건 / 512MiB 상한과 최근 2시간 보호창으로 관리합니다.

## Scanner 성능 / 안정화

v1.5.0은 threshold를 낮추지 않고 stage latency를 계측합니다.

- capture
- rectangle proposal
- semantic header validation
- normal/deep OCR
- visual recovery
- catalog matching/recovery
- presentation
- end-to-end

같은 active scan-cycle 안에서 픽셀 단위로 완전히 동일한 OCR bitmap만 재사용하며 frame/cycle 사이에는 OCR 결과를 캐시하지 않습니다.

이미 검증된 item의 title glyph identity가 유지되는 동안에는 미세한 dark-background/trailing-ROI 변화 때문에 OCR을 매 frame 반복하거나 Mini Scanner 결과를 불필요하게 흔들지 않습니다. 다른 title/identity evidence가 확인되면 기존 trusted result를 폐기하고 다시 검증합니다.

## Mini Scanner

- matched item 정보만 overlay 표시
- Topmost + no-activate
- 전체 카드 drag
- 실제 Scanner mode에서는 Tarkov foreground/inventory context를 보수적으로 확인
- stale item/context epoch를 적용하지 않음
- canonical icon은 update/cache 경로에서 준비하고 scan 순간 HTTP 없음

## Scanner font 정책

게임 폰트 파일을 준현 헬퍼 배포물에 재배포하지 않습니다.

```text
Tarkov resources.assets (read-only)
→ bounded SFNT discovery/extraction
→ %LocalAppData%/JunhyunHelper/scanner/fonts
→ source/font generation 검증
→ local visual corroboration/recovery
```

Tarkov/font generation이 바뀌면 stale rendered template generation을 그대로 신뢰하지 않습니다.

## 로그 / 진단

```text
%LocalAppData%\JunhyunHelper\logs\scanner.log
%LocalAppData%\JunhyunHelper\logs\scanner.log.1
%LocalAppData%\JunhyunHelper\logs\startup.log
```

일반 로그는 bounded rotation을 사용합니다. `로그 삭제`는 recent Scanner activity, scanner.log(.1), 최신 in-memory recognition image를 정리하지만 Ground Truth dataset은 삭제하지 않습니다.

## Program Update

```text
latest public stable 확인
→ strictly newer면 사용자 동의
→ exact Windows ZIP + SHA256SUMS
→ checksum/package 검증
→ program-owned files transaction 교체
→ 새 버전 재시작
```

사용자 데이터는 `%LocalAppData%\JunhyunHelper`에 분리되어 있으며 프로그램 업데이트가 덮어쓰지 않습니다.

## 배포 형태

Windows x64 portable / .NET 10 self-contained single-file.

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

별도 .NET Runtime 설치나 관리자 권한은 필요하지 않으며 현재 code signing은 하지 않습니다.

## 버전 정책

- 새 사용자 기능 또는 명확한 제품 UX 확장 → MINOR +1, PATCH=0
- 기존 기능의 수정/보완/버그 수정/성능·안정성·정확성 개선 → PATCH +1

v1.5.0은 OCR 사용자 설정, candidate-based correction UX, Scanner surface 재구성과 제품 동작 변경이 포함되어 MINOR 릴리즈로 분류합니다.

## 개발 문서

- `docs/STATE.md` — canonical 현재 상태
- `docs/CURRENT_STATE.md` — 짧은 상태 인덱스
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` — 장기 결정 인덱스
- `docs/ARCHITECTURE.md` — 전체 아키텍처
- `docs/DEVELOPER_REFERENCE.md` — 구현/참조 지도
- `docs/SCANNER.md` — Scanner 제품/기술 기준선
- `docs/SCANNER_TEST_PLAN.md` — Scanner 검증 gate
- `docs/SCANNER_SYMBOL_POLICY.md` — current-catalog symbol 정책
- `docs/DECISION_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md` — v1.5.0 승인 범위
- `docs/QUEST_TASK_POOL_AUDIT_2026-08-24.md` — v1.5.0 Quest live-data audit
- `docs/STATUS_V1.5.0_PRODUCT_FINISHING_PASS_2026-08-24.md` — v1.5.0 구현/릴리즈 상태
- `docs/RELEASE_NOTES_V1.5.0.md` — v1.5.0 release notes
