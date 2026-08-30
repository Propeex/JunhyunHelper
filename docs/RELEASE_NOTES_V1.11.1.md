# 준현 헬퍼 v1.11.1

## 목적

v1.11.1은 v1.11.0 실사용 직후 확인된 Scanner 설정/검색/교정 저장 피드백 회귀를 수정하는 PATCH 릴리즈다.

새 Scanner 인식 방식이나 새로운 게임 데이터 의미를 추가하지 않는다. v1.11.0에서 확정한 탄약 판단, Ammo Pack, Hideout FIR, Map/MiniMap 계약을 보존하면서 사용자에게 실제 조작 가능성과 즉시 피드백을 완성한다.

## Scanner 탄약 판단 설정

v1.11.0의 Mini Scanner는 탄약 pickup 판단을 렌더했지만 `AmmoPickupText`를 정보 순서 밖의 고정 마지막 줄로 추가해 Scanner 설정의 표시/숨김·순서 목록에서 누락됐다.

v1.11.1은 이를 정상적인 Scanner display field로 승격한다.

- settings field: `ammo_pickup`
- 표시 이름: `탄약 줍기 판단`
- 표시/숨김 지원
- Mini Scanner 정보 순서 변경 및 저장 지원
- 기존 v1.11.0 사용자는 migration 뒤에도 기본적으로 표시 상태 유지
- Scanner display settings schema: v8 → v9

플리마켓 최저가 compatibility field/data는 계속 보존하지만 사용자 표시와 설정 목록에는 다시 노출하지 않는다.

## Items / Hideout 검색 지우기

아이템과 은신처 탭 검색창에 다른 검색 UI와 동일한 `×` clear 동작을 추가한다.

- 현재 검색어 즉시 삭제
- 기존 TextChanged 검색/필터 계약 그대로 재사용
- 삭제 뒤 검색창으로 keyboard focus 복귀
- 기존 TextBox를 교체하지 않고 product-owned 보조 버튼만 추가

## `교정 데이터 추가` 저장 완료 피드백

전역 단축키로 Saved Case 저장에 성공하면 Mini Scanner에 정확히 `저장 완료`를 약 2초 표시한다.

- 이미 Mini Scanner 아이템이 보이는 경우 현재 item snapshot을 교체하거나 지우지 않고 상단 상태 badge만 표시
- Mini Scanner가 닫혀 있고 현재 item snapshot이 없는 경우 status-only Mini Scanner 카드로 잠시 표시한 뒤 자동 숨김
- 저장 실패 또는 저장할 evidence가 없는 경우 성공 피드백을 표시하지 않음
- `저장할 스캔 결과가 없습니다.` 계약 유지
- hotkey는 Ground Truth를 생성/추측하지 않음
- `UserConfirmed=false` 및 deferred Saved Case review 계약 유지

## v1.11.0 포함 유지보수 감사

이번 PATCH와 함께 직전 v1.11.0 변경 및 주요 제품 회귀 계약을 재점검했다.

### 확인한 영역

- ammo pickup evaluator의 caliber/penetration band 판정
- 현재 Trader LL 및 completed quest 기준 direct purchase projection
- barter/craft/flea/higher-LL/unproven quest unlock 제외
- 동일 penetration tie 및 direct-purchase 없음 경계
- Ammo Pack authoritative `containsItems` 우선, empty relation의 좁은 fallback, ambiguous/mixed relation fail-closed
- Hideout requirement `attributes.foundInRaid` 우선 및 FIR inventory/cleanup 의미
- MiniMap first-open latest map replay
- Extract marker late-load
- Player Marker Size 변경 뒤 marker/name presentation 복구
- marker empty-layer one-shot recovery
- correction hotkey no-Ground-Truth Saved Case 계약
- Scanner OCR/matcher/candidate/recovery acceptance 및 screen-pixels+OCR anti-cheat boundary
- 기존 Quest/Hideout/Items/Ammo/Map/MiniMap/Scanner published EXE smoke와 shutdown lifecycle

감사 결과 위 세 사용자 보고사항과 Scanner 설정 runtime-smoke 공백 외에 추가로 수정할 근거가 있는 제품 결함은 확인되지 않았다. 따라서 추측성 리팩터링이나 인식 기준 변경은 하지 않는다.

## 검증 보강

v1.11.0에서는 Scanner 본 화면은 actual published EXE smoke로 검증했지만 Scanner 설정 정보 목록 자체는 검증하지 않아 탄약 설정 항목 누락을 잡지 못했다.

v1.11.1은 `JUNHYUNHELPER_MAP_SMOKE=1` published EXE에서 다음을 실제 WPF control 기준으로 직접 검증한다.

- Scanner 설정 목록에 `탄약 줍기 판단` 존재
- flea minimum 표시 항목이 다시 노출되지 않음
- Items 검색 `×` 버튼이 렌더되고 실제 query를 삭제함
- Hideout 검색 `×` 버튼이 렌더되고 실제 query를 삭제함
- Mini Scanner가 `저장 완료` status-only 피드백을 렌더함

source-level regression contracts도 별도로 유지한다.

## Schema / compatibility

```text
Desktop target version: 1.11.1
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog cache: v1~v4 readable, v4 written
```

v1.11.0 → v1.11.1에서 mandatory Game Content migration 또는 user.db migration은 없다.

## 릴리즈 게이트

공개 전 다음을 모두 통과해야 한다.

- deterministic Core/Maintenance tests
- Windows Release desktop build
- Windows x64 self-contained publish
- actual published EXE Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- v1.11.1 Scanner settings / Items·Hideout clear / Mini Scanner save-feedback smoke
- active-async graceful shutdown race
- release package root/dependency/checksum audit
- exact-main CI
- Release workflow의 tag/release/asset readback

사용자의 실제 PC/Tarkov 플레이 환경 실사용은 자동 검증과 별도로 관리한다.
