# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 현재 공개 버전

**v0.1.10 PUBLIC RELEASE — Windows x64**

**다운로드:** https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.10

v0.1.10은 v0.1.9 실사용에서 확인된 유동 제출 아이콘/정렬, Ammo header/detail 배치, Map Quest sidebar 정렬 연결 누락과 초기 LL1 Quest `확인 필요`를 수정한 패치 릴리즈입니다.

### v0.1.10 핵심 변경

- exact EFT profile variable 값은 계속 최우선
- audited EFT 1.1 LL2~LL4 trader task-pool runtime reconstruction 유지
- **현재 trader가 LL1이고 그 trader의 완료 Quest가 0개인 증명 가능한 초기 상태**는 LL1 pool counter를 0으로 확정
- 완료 Quest가 하나라도 있거나 LL2 이상인 progressed LL1 상태는 exact 값 없이 추측하지 않음
- availability delay는 실제 completion timestamp가 없으면 계속 `확인 필요`
- future Needed Items는 unresolved Quest를 계속 잠재 필요 아이템으로 보호
- flexible hand-in row를 일반 Item list 구조로 재작성
  - 44px icon frame/image로 clipping 제거
  - icon + 이름/분류 좌측 정렬
  - 인레이드/일반 보유량 우측 고정 정렬
- Ammo header의 중복 `구경`, `즐겨찾기` label 제거
- Ammo caliber selector 폭 축소, favorite toggle `☆ / ★` 유지
- Ammo 상세정보 접기/펼치기 버튼을 중앙 정렬
- Map current Quest의 실제 동적 sidebar에도 layout polish 연결
- Map Quest row를 checkbox / A·B·C marker / title lane으로 분리하고 title 좌측 정렬

공개 릴리즈 검증:

```text
release baseline: cc8d968deb6cbb07029fa35186ec3a3881d5c97f
ProductVersion: 0.1.10+cc8d968deb6cbb07029fa35186ec3a3881d5c97f
feature PR #90 CI: 32007776178 — SUCCESS
feature main CI: 32008009801 — SUCCESS
release candidate PR #91 CI: 32011089823 — SUCCESS
release baseline main CI: 32011299363 — SUCCESS
release workflow: 32011564563 — SUCCESS
automated tests: 210 passed / 0 failed / 0 skipped
Windows x64 self-contained single-file publish: SUCCESS
startup + Main Map + Factory + MiniMap runtime smoke: SUCCESS
normal Main Window close / process exit: SUCCESS
asset: Junhyun-Helper-v0.1.10-win-x64.zip
asset size: 74,067,151 bytes
SHA-256: 0d32f2344feb1e9088460830e6cff4bbd527198b1e191a177f7a8652e6efd998
public ZIP re-download + SHA-256 verification: SUCCESS
```

## 주요 기능

- GameMode별 Profile 관리
- Quest 진행/잠김/사용 불가/완료 판정과 선행 Quest 연결
- Quest 제출 Item / 자동 소비·rollback ledger
- Hideout 레벨 / 미래 업그레이드 재료
- 미래 Quest + Hideout 기준 Needed Items
- FIR / 일반 Inventory와 안전한 cleanup 계산
- flexible hand-in 그룹 + `필요 / 전체 / 충분` 상태 필터
- Item 종류/용도/필요 상태 필터, cross-navigation, Item Wiki
- Ammo 성능/수급처/Armor Class 1~6 비교와 caliber favorites
- 온라인 Game Content 안전 업데이트와 image cache
- Map + MiniMap
  - 현재 Quest sidebar / A·B·C marker identity
  - 일반 marker / PMC·Scav·Transit 탈출구
  - floor / zoom / MiniMap 크기 hotkey
  - 타층 marker 유지 + 현재층/위층/아래층 relation 표시
  - MiniMap opacity / temporary transparency / marker scale
  - screenshot 기반 Map 전환 / player tracking
  - MiniMap 층 변경 시 같은 화면 구도에서 floor layer만 교체
- 상단 `스캐너` 탭 — 현재 `준비 중` placeholder 유지

## Quest 정확도 기준

- 서로 다른 `taskRequirements`는 AND
- 한 requirement 내부 `status[]`는 OR
- `complete` / `active` / `failed`의 source 의미 보존
- 별도 `수주 가능` 상태를 만들지 않고 받을 수 있는 Quest는 Helper에서 즉시 수락한 것으로 간주
- source가 직접 제공한 prerequisite를 compatibility overlay가 더 강한 조건으로 바꾸지 않음
- BTR Driver 누락 gate는 `A Helping Hand = Active`로만 보강
- Ref 누락 gate는 현재 GameMode의 검증된 unlock Quest `Complete`로만 보강
- Lightkeeper는 최초 해금 이후 접근 상실/복구가 가능하므로 별도 special trader access로 판정
- 12개 audited dialogue gate는 exact-ID compatibility를 적용하고 새/변경 dialogue는 추측하지 않음
- EFT profile-variable requirement는 exact current value가 있으면 정확 판정
- exact 값이 없으면 current EFT 1.1의 감사된 27-ID 구조가 완전히 일치하는 LL2~LL4 task-pool만 runtime에서 복원
- LL1은 audited 구조 + 현재 LL1 + 해당 trader 완료 Quest 0개의 초기 상태만 counter 0으로 확정
- 그 밖의 LL1 task-pool과 실제 완료 시각 기반 delay는 증명할 수 없으면 `확인 필요`
- compatibility 구조가 upstream과 달라지면 자동으로 fail-closed

## Needed Items 안전성

Quest 화면의 current task-pool compatibility는 future item cleanup을 낙관적으로 바꾸지 않습니다.

- missing future profile-variable fact는 계속 `IndeterminatePotential`로 보호
- unresolved future Quest의 Item도 Needed Items에 포함
- flexible hand-in 후보도 cleanup protection 유지

따라서 `확인 필요`를 줄이기 위해 실제 필요한 Item을 잘못 `정리 가능`으로 판단하도록 완화하지 않습니다.

## Map / MiniMap 안정화 기준

- 서로 다른 floor라는 이유만으로 일반 marker를 숨기지 않음
- current/above/below floor relation을 별도 presentation으로 표현
- 알려진 타층 marker도 유지
- 실제 동일 물리 extract의 semantic duplicate 정규화 유지
- Main Map floor 변경 시 live zoom + map-space viewport center 보존
- MiniMap floor 변경 시 **exact live Scale + Translate X/Y 보존**
- Main Map selector와 MiniMap shared map key 동기화
- `퀘스트 마커 표시`를 포함한 product setting은 `%LocalAppData%/JunhyunHelper/map-product-settings.json`에서 복원
- current Quest sidebar는 checkbox / marker / title lane으로 고정 정렬
- MiniMap hover transparency는 dedicated lightweight 16ms input check 사용

## 실행

1. GitHub Release에서 최신 `Junhyun-Helper-vX.Y.Z-win-x64.zip`을 다운로드합니다.
2. 원하는 폴더에 압축을 풉니다.
3. **`준현 헬퍼.exe`**를 실행합니다.

배포 루트:

```text
준현 헬퍼.exe
FIRST_RUN_KO.txt
Assets/
```

Windows x64 portable / self-contained single-file 빌드이므로 별도 .NET 설치나 관리자 권한은 필요하지 않습니다. 코드 서명은 아직 적용하지 않아 Windows SmartScreen 경고가 표시될 수 있습니다.

사용자 데이터와 로그는 프로그램 폴더가 아니라 `%LocalAppData%/JunhyunHelper`에 저장됩니다.

## 데이터 업데이트 / 호환성

```text
online source
→ download
→ 외부 형식/필수 의미 검증
→ canonical 변환
→ candidate DB
→ 관계/read-back 검증
→ active 교체
→ User Progress와 결합
```

- 실패 candidate가 마지막 정상 Game Content를 덮어쓰지 않습니다.
- Game Content update가 `user.db`를 삭제하거나 덮어쓰지 않습니다.
- Runtime GPT/AI 의존성은 없습니다.
- Content schema는 v7이며 v3~v7 snapshot을 오프라인에서 읽을 수 있습니다.
- `user.db` SQLite schema는 v1 그대로입니다.
- **v0.1.9 → v0.1.10 필수 데이터 업데이트 없음**

## 개발 문서

- [`docs/STATE.md`](docs/STATE.md) — 현재 프로젝트 상태
- [`docs/PRODUCT.md`](docs/PRODUCT.md) — 공식 제품 요구사항
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — 장기 설계 결정
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 기술 구조
- [`docs/QUEST_PREREQUISITE_SEMANTICS.md`](docs/QUEST_PREREQUISITE_SEMANTICS.md) — Quest 선행조건 의미
- [`docs/QUEST_TASK_POOL_AUDIT_2026-08-17.md`](docs/QUEST_TASK_POOL_AUDIT_2026-08-17.md) — EFT 1.1 trader task-pool 감사 및 current-version compatibility 경계
- [`docs/DIALOGUE_GATE_AUDIT_2026-08-17.md`](docs/DIALOGUE_GATE_AUDIT_2026-08-17.md) — dialogue Quest 감사
- [`docs/FEEDBACK_FIXES_2026-08-17.md`](docs/FEEDBACK_FIXES_2026-08-17.md) — 성능/UI/Quest feedback 수정 기록
- [`docs/MINIMAP_FLOOR_FRAME_2026-08-17.md`](docs/MINIMAP_FLOOR_FRAME_2026-08-17.md) — MiniMap exact floor-frame 계약
- [`docs/MAP_PRODUCT_REQUIREMENTS.md`](docs/MAP_PRODUCT_REQUIREMENTS.md) — Map/MiniMap 제품 기준
- [`docs/RELEASE_0.1.10.md`](docs/RELEASE_0.1.10.md) — v0.1.10 공개 검증 기록
