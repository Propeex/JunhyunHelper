# 준현 헬퍼 v1.8.2

상태: **PUBLIC STABLE / VERIFIED**

v1.8.2는 공개 실행 파일의 Ammo 드롭다운 아이콘 초기화 회귀와 현재 json.tarkov.dev 관계 데이터 형식 변화에 대응하는 유지보수 PATCH입니다.

## 공개 실행 파일 Ammo 드롭다운 회귀 수정

v1.7.15에서 추가한 구경/즐겨찾기 드롭다운 아이콘 UI가 published executable에서도 항상 초기화되도록 WPF 타입 초기화 경계를 수정했습니다.

- Ammo 구경 드롭다운의 runtime icon template 등록을 static-field side effect의 실행 시점에 맡기지 않습니다.
- 즐겨찾기 선택은 기존과 동일한 일반 ComboBox 의미를 유지합니다.
- 구경과 즐겨찾기 selector는 동일한 구경별 탄약 아이콘 상태와 순환 타이밍을 공유합니다.
- legacy 즐겨찾기 메뉴는 runtime polish 적용 후 비활성/비표시 상태를 유지합니다.
- 공개 실행 파일 smoke가 실제 렌더링된 `Image`, `Image.Source`, geometry와 shared timer-cycle까지 확인합니다.

## 현재 json.tarkov.dev 관계 데이터 대응

현재 Regular/PvE live source에서 확인된 두 가지 upstream shape를 canonical 관계 의미를 손상시키지 않는 범위에서 정규화했습니다.

### Bitcoin Farm passive production

json.tarkov.dev의 crafts endpoint에는 Bitcoin Farm 생산 항목이 `requiredItems = []`인 형태로 포함됩니다. 이는 재료를 소비하는 일반 제작이 아니라 GPU/station state에 의해 진행되는 passive production입니다.

따라서 audited identity가 모두 일치하는 해당 항목만 일반 craft relationship import에서 제외합니다.

```text
craft:   5d5c205bd582a50d042a3c0e
station: 5d494a445b56502f18c98a10
product: 59faff1d86f7746c51718c9c
```

다른 empty-required craft는 계속 fail closed합니다.

### 중복 trader direct-purchase offer

현재 live items payload에는 동일 item 아래 canonical record 기준으로 완전히 같은 `buyFromTrader` offer가 반복되는 사례가 있습니다.

- 완전히 동일한 direct-purchase record만 하나로 정규화합니다.
- 가격, 화폐, trader, LL, quest unlock, buy limit 등 의미 필드가 다른 offer는 합치지 않습니다.
- 잘못된 중복 수급처 표시와 canonical uniqueness failure를 방지합니다.

## 유지되는 계약

- Game Content candidate / LKG 분리와 fail-closed 정책은 변경하지 않았습니다.
- relationship reference/price/count/limit integrity validation을 유지합니다.
- 관계 및 material-edge 50% completeness floor를 유지합니다.
- critical relationship collection 전면 empty 차단을 유지합니다.
- v3~v7 legacy relationship-null compatibility를 유지합니다.
- Scanner OCR/아이템 인식 임계값, matcher, visual recovery 정책은 변경하지 않았습니다.
- `structural floor 0.34`, `HEADER_FRAME_LOCKED 0.68`, continuous 8 / one-shot 12, 200ms observation target을 유지합니다.
- Map/MiniMap donor revision과 ownership boundary는 변경하지 않았습니다.

## 공개 릴리즈 검증

```text
exact product source/tag:
a0a8390c7c863400a97d174e864c405c2e38f47f

exact-main CI: 33138083383 — SUCCESS
421 passed / 0 failed / 0 skipped
ProductVersion: 1.8.2+a0a8390c7c863400a97d174e864c405c2e38f47f
published Product UI / Main Map / Factory / MiniMap smoke: SUCCESS
Ammo rendered icon + shared timer-cycle smoke: SUCCESS
Regular/PvE live-data fatal validation: 0 / 0
Release workflow: 33138226890 — SUCCESS
public release id: 378240417
latest stable: true
```

공개 ZIP:

```text
Junhyun-Helper.zip
bytes: 80,520,794
SHA-256:
be83ec72d1678b2496e01ce4378708642e0bf0cc00cebeb407fa38756ecf1f0a
```

GitHub의 공개 ZIP digest는 exact-main CI package SHA-256과 일치합니다.

상세 검증 증거는 `docs/RELEASE_1.8.2.md`, 기계 판독 상태는 `docs/.release-v1.8.2-status.json`, 기술적 원인과 설계 결정은 `docs/DECISION_V1.8.2_RUNTIME_LIVE_REGRESSIONS.md`를 기준으로 합니다.

이 문서와 이후 documentation-only commit은 v1.8.2 제품 릴리즈 소스가 아닙니다. 공개 source/tag/assets는 위 exact product source 기준으로 고정합니다.
