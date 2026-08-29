# 준현 헬퍼

Escape from Tarkov 플레이를 지원하는 Windows x64 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 제품 상태

현재 제품 상태는 **v1.10.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**입니다.

현재 확정 요구사항 범위의 제품과 Scanner는 완성 상태입니다. 새로운 실제 회귀·Tarkov 호환성 변화·사용자가 명시적으로 확정한 새 제품 요구사항이 없는 한 선제적 기능 추가나 Scanner 인식 기준 조정을 시작하지 않습니다.

공식 현재 상태 문서:

- `docs/CURRENT_STATE.md`
- `docs/STATE.md`
- `docs/PRODUCT.md`
- `docs/ARCHITECTURE.md`
- `docs/DEVELOPER_REFERENCE.md`
- `docs/MAINTENANCE_CONTRACTS.md`
- `docs/DECISIONS.md`

## 현재 공개 릴리즈

```text
version: v1.10.1
Desktop target version: 1.10.1
exact product release source/tag target: c444a1e26793e15c075875159f6605d8a99cf7f9
PR CI: 33253141127 — SUCCESS
exact-main CI: 33253293015 — SUCCESS
Release workflow: 33253438908 — SUCCESS
release id: 378982127
stable asset: Junhyun-Helper.zip
asset id: 535210900
bytes: 80,540,164
SHA-256: c37c00a5e5ecdc431d6b26775d73682cabf17e4310533065c88e2d58d8f14922
439 passed / 0 failed / 0 skipped
```

GitHub `/releases/latest`와 `refs/tags/v1.10.1` readback에서 v1.10.1이 `draft=false`, `prerelease=false`, latest stable이며 release target과 tag ref가 exact product release source와 일치함을 확인했습니다.

공식 릴리즈 기록:

- `docs/RELEASE_1.10.1.md`
- `docs/RELEASE_NOTES_V1.10.1.md`
- `docs/.release-v1.10.1-status.json`
- `docs/DECISION_V1.10.1_STABILITY_AUDIT.md`

이 README와 이후 documentation-only commit은 v1.10.1 제품 릴리즈 소스가 아닙니다. v1.10.1 product source/tag/assets는 위 `c444a1e2...` 기준의 immutable historical release입니다.

## 설치 / 실행

배포 형태는 Windows x64 portable ZIP입니다.

```text
Junhyun-Helper.zip
└─ 준현 헬퍼/
   ├─ 준현 헬퍼.exe
   ├─ FIRST_RUN_KO.txt
   └─ Assets/
```

- Windows x64
- .NET 10 WPF
- self-contained single-file executable
- 별도 .NET Runtime 설치 불필요
- installer 없음
- 일반 사용에 관리자 권한 불필요

사용자 데이터는 프로그램 폴더가 아니라 `%LocalAppData%/JunhyunHelper` 아래에 저장됩니다.

## 주요 기능

- GameMode별 Profile / User Progress
- Quest / Hideout 진행 관리
- Needed Items / FIR·일반 Inventory / consumption ledger
- Items / cross-navigation
- Ammo / favorites
- Game Content 안전 업데이트 / image cache
- Map + MiniMap
- Scanner + Mini Scanner
- Scanner Ground Truth / diagnostics / regression dataset
- Scanner 아이템 정보 DB
- Scanner Favorites / Recents
- 사용자 동의형 Program Update

Runtime GPT/AI 의존성은 없습니다.

## v1.10.1 — 안정성 및 유지보수 감사

- 새 사용자 기능은 추가하지 않았습니다.
- 메인 헤더의 `버전만 표시 + 아이템 정리 필요 오렌지 점` 보강을 static WPF class-level `Loaded` handler에서 `MainWindow`의 명시적 제품 수명주기 소유로 이동했습니다.
- header 상태 감시의 `DependencyPropertyDescriptor` 구독을 창 종료 시 명시 해제해 초기화/종료 ownership을 대칭화했습니다.
- 기존 헤더 표시 결과, Quest/Hideout/Items/Ammo/Map/MiniMap/Scanner의 사용자 의미는 변경하지 않았습니다.
- 현재 CI/Release 경로에서 사용되지 않는 v1.2.1 일회성 finalization helper를 제거했습니다. 역사적 릴리즈 증거는 공식 docs/Release에 유지됩니다.
- `FIRST_RUN_KO.txt`의 장기 누적 changelog를 정리하고 전체 변경 이력의 권위를 GitHub Releases와 `docs/`로 일원화했습니다.
- Scanner OCR threshold/matcher/candidate cap/visual recovery acceptance, Game Content LKG/completeness/fail-closed, Map/Factory/MiniMap semantics는 변경하지 않았습니다.

PR CI와 exact-main CI는 각각 439개 테스트와 실제 published EXE Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke, graceful shutdown, clean portable root, package 검증을 통과했습니다.

## v1.10.0 — MiniMap 재표시 동기화 / Mini Scanner 플리마켓 최저가

- Main Map을 A에서 B로 바꾼 직후 MiniMap을 처음 열거나, 이미 로드되어 숨겨져 있던 같은 MiniMap 창을 다시 표시해도 첫 visible frame부터 B를 사용하도록 수정했습니다.
- donor `Hide()` → same loaded Window `Show()` 재사용 경로를 별도 동기화 경계로 보강했습니다.
- Mini Scanner에 `플리마켓 최저가` 표시 항목을 추가했고 설정에서 표시/숨김, 순서 변경, persistence를 지원합니다.
- 플리 최저가는 Scanner catalog의 `lastLowPrice`를 Item ID 확정 뒤 presentation-only 데이터로 사용하며 Scanner 인식 기준과 scan-time network I/O는 변경하지 않았습니다.

MiniMap runtime evidence:

```text
main-map-selection-boundary=ok
active-minimap-map-sync=ok
reused-minimap-show-boundary=ok
rendered-minimap-map-sync=ok
```

## 유지보수

실사용 오류나 Tarkov 변화가 발생하면 실제 source/log/runtime state를 확인해 최소 수정하고 deterministic regression → published EXE smoke → exact-main release gate 순으로 검증합니다. 사용자-visible WPF lifecycle 변경은 source assertion만으로 성공을 선언하지 않습니다.

CI 및 published EXE 자동 검증과 사용자의 실제 PC/Tarkov 실사용 검증은 별도로 관리합니다. 현재 v1.10.1의 사용자 실사용은 아직 별도 확인 전입니다.
