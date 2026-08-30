# 준현 헬퍼 v1.11.0

## 목적

v1.11.0은 v1.10.1 이후 실사용에서 보고된 Map/MiniMap 회귀를 수정하고, Scanner 교정 데이터 수집과 탄약 판단을 실사용 흐름에 추가하는 MINOR 릴리즈다.

기존 Scanner 인식은 계속 화면 픽셀 캡처와 OCR만 사용한다. 게임 프로세스 메모리 읽기, 주입, hook, kernel 접근, 입력 자동화, 네트워크 조작, anti-cheat 우회는 사용하지 않는다.

## Map / MiniMap 유지보수

### MiniMap 첫 실행 지도 동기화

Main Map을 A에서 B로 바꾼 뒤 MiniMap을 처음 켰을 때 donor의 이전 persisted map A가 먼저 보일 수 있던 수명주기 결함을 수정했다.

- MiniMap window 존재 여부와 무관하게 최신 Main Map key를 product registry에 보존한다.
- 새 MiniMap window가 등록될 때 최신 snapshot을 즉시 replay한다.
- 창 unregister 뒤에도 최신 selection snapshot은 유지한다.

### Extract marker 설정 late-load

Map marker 설정 UI가 donor보다 먼저 구성되면 탈출구 checkbox가 아직 생성되지 않아 목록에서 누락될 수 있었다.

- extract row도 late donor-control retry 대상에 포함한다.
- 이미 product 설정 영역으로 이동된 row는 idempotent하게 유지한다.
- MiniMap extract projection이 비어 있는 경우 현재 extract presentation을 다시 동기화한다.

### Marker 표시 설정 보존

Player Marker Size 변경 과정에서 donor `UpdateMapView()`가 marker visual tree transform을 다시 적용해 MiniMap Marker Size / Name Size 등 현재 표시 설정이 실제 렌더에서 덮어써질 수 있던 경로를 수정했다.

- donor map-view 갱신 뒤 Junhyun marker presentation 전체를 다시 projection한다.
- marker scale, name scale/visibility, hidden category, edge-label 관련 현재 사용자 설정을 유지한다.

### Marker layer 일시 소실 복구

marker refresh가 container를 먼저 clear한 뒤 다른 refresh에 의해 취소될 수 있는 lifecycle race를 확인했다.

- 같은 map/floor에서 직전에 정상 marker가 있었는데 standard marker layer가 0개로 지속되는 경우를 감지한다.
- 명시적인 사용자 숨김 상태가 아니라면 한 번만 refresh를 재요청한다.
- deliberate hide를 되돌리거나 무한 retry하지 않는다.

## Scanner 표시 정리

Mini Scanner의 `플리마켓 최저가` 사용자 표시를 제거했다.

- flea minimum source/model/cache 값은 backward compatibility를 위해 유지한다.
- Scanner 인식 proof나 scan-time network I/O에는 사용하지 않는다.
- 기존 저장 설정에 compatibility field가 남아 있어도 렌더 UI에는 노출하지 않는다.

## Hideout FIR 정확도

Hideout item requirement의 FIR 여부를 source 의미 그대로 읽도록 수정했다.

- requirement `attributes.foundInRaid`를 canonical requirement에 보존한다.
- FIR requirement에는 non-FIR inventory가 충당되지 않는다.
- 동일 아이템의 non-FIR 사용 가능 여부와 cleanup 판정도 현재 canonical requirement를 기준으로 다시 계산한다.
- Quest FIR/Hideout FIR을 임의의 고정 규칙으로 나누지 않는다.

## Scanner `교정 데이터 추가` 전역 단축키

설정 가능한 primary global hotkey를 추가했다.

- 기본 단축키는 `Ctrl+Alt+F9`다.
- 최신 Scanner evidence가 없으면 `저장할 스캔 결과가 없습니다.`만 표시하고 Case를 만들지 않는다.
- 인식 성공/실패/불완전 결과 모두 현재 evidence 그대로 Saved Case로 저장할 수 있다.
- explicit hotkey save는 같은 최신 결과를 연속 저장해도 각각 별도 Case ID를 만든다.
- hotkey는 Ground Truth item을 생성하거나 추측하지 않으며 `UserConfirmed=false`로 저장한다.
- 저장 뒤 기존 Saved Case 관리 화면에서 검토하고, 닫으면 Scanner 화면으로 복귀한다.

## 탄약 `주워야 함` 판단

Scanner/Mini Scanner가 일반 탄약 및 Ammo Pack에 대해 현재 프로필 기준 pickup 판단을 제공한다.

### 구매 가능 기준

같은 caliber 안에서 penetration을 기준으로 비교하며, 현재 사용자가 실제로 직접 구매 가능한 탄약의 범위를 사용한다.

직접 구매 가능으로 인정:

- 현재 Trader Loyalty Level 이하
- 현금 직접 판매
- quest unlock이 필요한 경우 현재 프로필에서 해당 퀘스트 완료가 확인됨

직접 구매 가능으로 인정하지 않음:

- barter
- craft
- flea market
- 현재 LL보다 높은 offer
- 완료 여부를 확인할 수 없는 quest unlock

현재 직접 구매 가능한 탄약보다 penetration이 낮은 중간 탄은 pickup 대상에서 제외하고, 직접 구매 범위보다 더 높은 penetration 탄은 pickup 대상으로 유지한다. 같은 penetration tie는 deterministic하게 처리한다. 해당 caliber에 직접 구매 가능한 탄약이 하나도 없으면 unavailable 탄약은 모두 pickup 후보로 취급한다.

## Ammo Pack → contained ammo

Ammo Pack 자체의 이름만으로 판단하지 않고 실제 contained canonical ammo로 resolve한다.

1. Tarkov item data의 authoritative `containsItems` relation 우선
2. authoritative relation이 비어 있을 때만 제한적인 name fallback
3. non-empty authoritative relation이 모호하거나 혼합 container인 경우 이름으로 덮어쓰지 않음

따라서 같은 탄약의 50발/100발 팩처럼 pack ID가 달라도 실제 내부 탄약의 penetration/구매 가능 상태를 사용한다.

## Schema / compatibility

```text
Desktop target version: 1.11.0
Content schema: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v8
Scanner catalog cache: v1~v4 readable, v4 written
```

v1.10.1 → v1.11.0에서 mandatory Game Content schema migration 또는 user.db migration은 없다. Scanner display settings는 기존 값을 읽어 v8 기본 hotkey/표시 계약을 보완한다.

## 검증 계약

v1.11.0 release candidate는 다음을 모두 통과해야 공개한다.

- deterministic Core/Maintenance tests
- Windows Release desktop build
- Windows x64 self-contained publish
- actual published EXE Product UI / Ammo / Map / Factory / MiniMap / Scanner smoke
- active-async graceful shutdown race
- release package root/dependency/checksum audit
- exact-main CI
- Release workflow의 tag/release/asset readback

사용자의 실제 PC/Tarkov 플레이 환경 실사용은 자동 검증과 별도로 관리한다.
