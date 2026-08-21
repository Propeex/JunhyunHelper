# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 릴리즈 상태

현재 public stable은 **v1.1.5**이며 Scanner/Mini Scanner hardening과 Tarkov 상세창 제목 폰트 기반 인식 보강의 공개 패키지 검증까지 완료했습니다.

```text
version: v1.1.5 PUBLIC RELEASE / VERIFIED
release source / tag: 3541bab6536ff91a00f394c4f7b03d5cbf112746
PR final candidate CI: 32493986403 — SUCCESS
Draft/public verification run: 32495042444 — SUCCESS
independent public verification run: 32495225958 — SUCCESS
automated tests: 249 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.5-win-x64.zip
bytes: 80,269,429
SHA-256: dc31177ae1bd4d152453a010dffe6cbb1e6c1d2a4a7e2eb82fb7444fa99c0748
ProductVersion: 1.1.5+3541bab6536ff91a00f394c4f7b03d5cbf112746
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact public tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
independent public-downloaded EXE smoke: SUCCESS
```

```text
Content schema: v7
Readable schemas: v3~v7
user.db schema: v1
v1.1.4 → v1.1.5 mandatory Game Content schema update: none
v1.1.4 → v1.1.5 user.db migration: none
Scanner settings schema: v2
```

Release: https://github.com/Propeex/JunhyunHelper/releases/tag/v1.1.5

상세 릴리즈 기록은 `docs/RELEASE_1.1.5.md`에 있습니다.

## Scanner — v1.1.5

```text
Tarkov / Display pixels
→ RED-X candidates + rectangle/edge fallback
→ IoU deduplication
→ 최대 8 structural candidates
→ adaptive 4x/6x/8x Windows ko-KR OCR
→ current official Korean full-item catalog semantic validation
→ 필요 시 상위 3개 candidate Deep OCR
→ 기존 semantic gate 실패 시에만 conservative Tarkov-font recovery
→ Item ID
→ existing JunhyunHelper data
→ Mini Scanner
```

v1.1.5 보강:

- Mini Scanner는 matched Item 정보만 표시; 대기/OCR/진단 상태 text는 overlay에서 숨김
- WPF Topmost + native `HWND_TOPMOST`, no-activate 유지
- 전체 카드 drag hitbox + Arrow cursor
- foreground Tarkov inventory/stash Korean UI를 확인할 때만 실사용 overlay 표시; 불확실하면 hidden
- title/context WinRT OCR 직렬화
- raw `traderPrices`와 derived `sellFor` market shape 모두 지원
- market-empty catalog가 정상 cache를 덮는 문제 차단
- 기존 설치의 icon/trader/trader-per-slot 표시 default를 1회 정상화
- Game Content update 시 전체 canonical Item icon을 local cache에 prefetch
- Scanner Lab v3.8 multi-candidate/current Korean catalog semantic contract 유지

### 상세창 아이템명 폰트 보강

현재 상세창 상단 이름은 Tarkov `ItemInfoWindowLabels._caption` TextMeshPro text이고, 조사된 UI font stack은 **Bender 계열 primary + Noto Sans CJK KR Korean fallback**입니다.

기존 OCR 성공을 건드리지 않고, Deep OCR까지 기존 semantic 기준을 통과하지 못한 경우에만 공식 이름 후보를 같은 font stack으로 렌더링해 title ROI의 glyph shape와 비교합니다. semantic score, visual score, top1/top2 margin을 모두 통과할 때만 복구합니다.

Bender font 바이너리는 public ZIP에 포함하지 않습니다. Scanner는 실행 중인 사용자의 Tarkov `EscapeFromTarkov_Data/resources.assets`를 **read-only**로 확인해 필요한 Bender/Noto SFNT만 app-local Scanner cache에 복사합니다. asset을 찾거나 검증하지 못하면 기존 OCR-only path로 자동 fallback하고 게임 디렉터리는 수정하지 않습니다.

핵심 안전 원칙:

- current official Korean full-item catalog가 Item identity 권위
- geometry나 font shape만으로 Item 확정 금지
- matcher confidence/top1-top2 margin 완화 금지
- historical alias production 누적 금지
- false positive보다 miss 선호
- scan-time network 없음
- game memory / DLL injection / packet interception / icon identity 없음

상세: `docs/SCANNER.md`, `docs/SCANNER_TEST_PLAN.md`, `docs/SCANNER_LAB_3_8_REFERENCE.md`.

## Scanner 탭

```text
상단 bar
  왼쪽: 스캐너 / 테스트
  오른쪽: 아이템 목록 최신화
↓
표시 정보 checkboxes
↓
최근 인식 기록                         로그 삭제
```

Mini Scanner는 별도 edit/reset mode 없이 visible 상태에서 전체 카드 영역을 직접 drag합니다. Cursor는 일반 Arrow를 유지합니다. Foundation 개발 controls는 일반 UI에 노출하지 않습니다.

개발자 로그:

```text
%LocalAppData%/JunhyunHelper/logs/scanner.log(.1)
```

screenshot/raw pixel은 저장하지 않습니다.

## 주요 기능

- GameMode별 Profile
- Quest / prerequisite / special trader / profile-variable
- Hideout
- Needed Items / FIR·일반 Inventory / cleanup safety / consumption ledger
- Items / cross-navigation
- Ammo / favorites
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Scanner + Mini Scanner
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## Program Update

```text
latest public stable 확인
→ strictly newer면 사용자 동의
→ exact Windows ZIP + SHA256SUMS
→ checksum/package 검증
→ program-owned files transaction 교체
→ 새 버전 재시작
```

사용자 데이터는 `%LocalAppData%/JunhyunHelper`에 분리되어 있으며 프로그램 업데이트가 덮어쓰지 않습니다.

## 배포 형태

Windows x64 portable / self-contained single-file.

ZIP root:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

별도 .NET 설치나 관리자 권한은 필요하지 않으며 현재 code signing은 하지 않습니다.

## 실제 Tarkov Scanner 검증

최신 Tarkov Borderless의 inventory/stash UI anchor와 실제 `resources.assets` font extraction은 환경 의존 live validation입니다. 자동 release gate는 parser/fallback과 제품 동작을 검증하지만 CI runner에는 Tarkov 설치가 없습니다.

문제가 생기면 `scanner.log`의 `inventory-context`, `title-font-*`, candidate/OCR/match metadata를 근거로 보정하며, 확신 기준을 약화하지 않습니다.

## 버전 정책

- 새 사용자 기능 → MINOR +1, PATCH=0
- 기존 기능 수정/보완/버그 수정/성능·안정성 개선 → PATCH +1

v1.1.5는 기존 Scanner/Mini Scanner의 작동성·안정성·정확성·데이터 신뢰성 보강이므로 PATCH입니다.

## 개발 문서

- `docs/STATE.md` — canonical 현재 상태
- `docs/CURRENT_STATE.md` — 짧은 상태 인덱스
- `docs/PRODUCT.md` — 제품 요구사항
- `docs/DECISIONS.md` — 장기 결정
- `docs/SCANNER.md` — Scanner 계약
- `docs/SCANNER_TEST_PLAN.md` — Scanner 검증
- `docs/SCANNER_LAB_3_8_REFERENCE.md` — Scanner Lab v3.8 reference
- `docs/RELEASE_1.1.5.md` — v1.1.5 public release record
- `docs/ARCHITECTURE.md`
- `docs/DEVELOPER_REFERENCE.md`
- `docs/VERSIONING.md`
- `docs/PROGRAM_UPDATE.md`
- `docs/DEPLOYMENT.md`
