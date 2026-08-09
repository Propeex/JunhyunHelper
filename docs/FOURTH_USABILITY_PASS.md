# FOURTH USABILITY PASS — 4차 실사용 피드백

기록일: **2026-08-09**

상태: `CONFIRMED / IMPLEMENTED / FINAL VERIFICATION`

3차 Windows 테스트 빌드 실사용 후 확정된 4차 개선 사항입니다.

## 1. Ammo 구경 표시

raw caliber ID를 사용자에게 그대로 노출하거나 기계적으로 숫자화하지 않습니다. 내부 ID는 보존하고 화면에서는 Tarkov에서 사용하는 cartridge 명칭을 표시합니다.

현재 확인된 대표 표시:

- `Caliber784x49` → `.308 Marlin Express`
- `Caliber93x64` → `9.3x64mm`
- `Caliber9x18PM` → `9x18mm Makarov`
- `Caliber127x33` → `.50 Action Express`
- `Caliber127x108` → `12.7x108mm`
- `.45 ACP`, `.300 Blackout`, `.338 Lapua Magnum`, `.366 TKM`, `12/70` 등 기존 cartridge 명칭 유지

Wiki Ballistics의 **표 등록 여부(membership)** 와 Armor Class 1~6 effectiveness는 별개의 사실로 취급합니다. Wiki 표에 등록된 탄약은 effectiveness 6칸을 일시적으로 안전하게 파싱하지 못해도 비교 대상에서 제거하지 않습니다.

## 2. 자동 스크롤 제거

일반적인 workspace refresh, 완료 처리, 보유량 변경 등으로 목록이 임의로 목표 row까지 이동하지 않게 합니다.

- refresh 시 현재 scroll 위치 보존
- 명시적인 화면 간 이동(Quest → Item, Item → Hideout 등)처럼 사용자가 이동을 요청한 경우에만 목표 row로 scroll 가능

## 3. Ammo 해금 Quest 연결

Ammo 상세 수급처의 해금 Quest를 누르면 Quest 탭의 해당 Quest 상세로 이동합니다. 표시 이름이 아니라 stable Quest ID를 사용합니다.

## 4. Ammo 구경 즐겨찾기

- 현재 구경 즐겨찾기 추가/해제
- 즐겨찾기 전용 dropdown
- 즐겨찾기 선택 → 해당 구경으로 이동
- 즐겨찾기는 Game Content가 아니라 로컬 UI preference `ammo-favorites.json`에 저장

## 5. Item 필요 출처

Quest와 Hideout 필요 출처는 같은 block/button 형식을 사용합니다.

- Quest 출처 클릭 → 해당 Quest
- Hideout 출처 클릭 → 해당 Hideout 시설
- stable ID 사용

## 6. Hideout Item 연결

Hideout 다음 업그레이드 재료 card 클릭 → 해당 Item 상세로 이동합니다.

## 7. 진행 완료와 Inventory 자동 차감

Quest 완료 및 Hideout 업그레이드 시 실제로 소모되는 **고정 Item 요구량**을 현재 Inventory에서 자동 차감합니다.

차감 정책:

- `인레이드 필수` 요구 → 인레이드 보유량에서만 차감
- 일반 요구 → 일반 보유량 우선 차감, 부족분만 인레이드에서 차감
- 기록된 보유량이 요구량보다 적으면 음수가 되지 않으며 현재 기록된 수량까지만 차감
- 유동 제출처럼 여러 Item 후보 중 실제 어느 Item을 사용했는지 프로그램이 알 수 없는 요구는 임의 후보를 골라 차감하지 않음

정확한 rollback을 위해 실제 차감한 Item별 인레이드/일반 수량을 User Progress의 소비 기록으로 보존합니다.

Quest 완료 취소 또는 Hideout level rollback 시:

- 자동 차감 기록이 있으면 사용자에게 복원 여부 확인
- `예` → 당시 실제 차감량만 복원하고 해당 소비 기록 제거
- `아니오` → 진행 상태만 rollback하고 보유량과 소비 기록은 유지
- `취소` → rollback 자체를 취소

`아니오`에서 소비 기록을 유지하는 이유는 동일 Quest를 다시 완료하거나 같은 Hideout level을 다시 올렸을 때 이미 사용한 재료를 두 번 차감하지 않기 위해서입니다.

## 8. 유동 제출 정렬

Quest별 유동 제출 group과 후보 row는 일반 Item 목록처럼 부모 폭을 채우고 왼쪽 정렬을 유지합니다. 내용 길이에 따라 카드 폭이 달라지는 형태를 허용하지 않습니다.

## 9. Icon 선다운로드

아이콘을 화면 진입 시 처음 받는 lazy-download에만 의존하지 않습니다.

Game Content update가 성공한 뒤 현재 제품에서 사용하는 icon을 image cache에 미리 내려받습니다.

대상:

- 모든 Quest item requirement 후보
- 모든 Hideout material
- 모든 Ammo item
- Hideout station

동일 Item ID는 화면 종류와 무관하게 같은 `item-{id}` cache key를 사용합니다. 아이콘 하나의 실패는 Game Content update 자체를 실패시키지 않습니다.

## 10. Profile 특별 상인

`특별` 상인 목록은 `고급`과 같은 Expander 형태로 기본 접힘 상태를 사용합니다.

## 11. Prestige 기본값

프레스티지 `미입력` 상태를 제품에서 없애고 **0을 기본 상태**로 사용합니다.

- 새 profile 기본값 0
- 기존 `user.db`의 nullable 값도 읽을 때 0으로 정규화
- Quest 판정에서도 0이라는 실제 알려진 값으로 사용

## 12. 상단 상태 요약

평상시 상단 상태에는 모드/Quest/Hideout/Ammo 개수를 나열하지 않습니다.

예:

```text
정리 필요 2
```

업데이트/저장/오류가 진행 중일 때만 작업 상태 메시지를 일시적으로 표시합니다.

## 13. Factory 야간 통합

Quest Map filter에서 Factory day/night variant를 하나의 `Factory` 항목으로 그룹화합니다.

canonical Map ID와 Quest의 원본 MapId는 변경하지 않습니다.

## Content / 저장 변경

Content snapshot schema는 **v3**입니다.

v3 추가 의미:

- Ammo의 현재 Wiki Ballistics membership을 effectiveness와 별도로 저장

기존 v2 `content.db`는 온라인 source에서 자동 재구축합니다. `user.db`는 별개이므로 Profile, Quest, Hideout, Trader, Inventory는 유지됩니다.

`user.db`에는 자동 차감 reconciliation을 위한 Quest/Hideout 소비 기록이 optional JSON field로 추가됩니다. SQLite table schema는 그대로이며 과거 profile payload와 하위 호환됩니다.

## 검증 기준

- Windows Release Desktop build
- 전체 automated tests
- Windows x64 publish/package
- caliber label regression
- Wiki membership/effectiveness 분리
- `12.7x108mm` membership 유지
- fixed item consumption + exact restore ledger
- rollback 후 복원하지 않은 경우 재완료/재업그레이드 중복 차감 금지
- flexible requirement 무작위 차감 금지
- Prestige null → 0 normalization
- update-time image prefetch
- stable-ID cross navigation
- Factory day/night filter grouping
- 최종 ZIP CRC 검사
