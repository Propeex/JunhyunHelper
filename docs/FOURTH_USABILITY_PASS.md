# FOURTH USABILITY PASS — 4차 실사용 피드백

기록일: **2026-08-09**

상태: `CONFIRMED / IMPLEMENTATION IN PROGRESS`

3차 Windows 테스트 빌드 실사용 후 확정된 개선 사항입니다.

## 1. Ammo 구경 표시

raw caliber ID를 사용자에게 그대로 노출하거나 기계적으로 숫자화하지 않습니다.

현재 확인된 표시:

- `Caliber784x49` → `.308 Marlin Express`
- `Caliber93x64` → `9.3×64mm`
- `Caliber9x18PM` → `9×18mm Makarov`
- `Caliber127x33` → `.50 Action Express`
- `Caliber127x108` → `12.7×108mm`

`.45 ACP`, `.300 Blackout`, `.338 Lapua Magnum`, `.366 TKM`, `12/70` 등 기존 전통 표기도 유지합니다.

Wiki Ballistics **membership**과 Armor Class effectiveness를 같은 의미로 취급하지 않습니다. Wiki 표에 등록된 탄약이면 effectiveness가 일시적으로 매칭되지 않아도 비교 대상 membership은 유지할 수 있어야 합니다.

## 2. 자동 스크롤 제거

일반적인 데이터 refresh/선택 유지 때문에 목록이 자동으로 `ScrollIntoView` 되지 않게 합니다.

명시적인 화면 간 이동(Quest → Item 등)처럼 사용자가 이동을 요청한 경우에만 목표 row로 스크롤할 수 있습니다.

## 3. Ammo 해금 Quest 연결

Ammo 상세 수급처의 해금 Quest를 누르면 Quest 탭의 해당 Quest 상세로 이동합니다. stable Quest ID를 사용합니다.

## 4. Ammo 구경 즐겨찾기

- 현재 구경을 즐겨찾기 추가/해제할 수 있음
- 즐겨찾기 전용 dropdown 제공
- 선택하면 해당 구경으로 즉시 이동
- 즐겨찾기는 Game Content가 아니라 로컬 UI preference로 저장

## 5. Item 필요 출처

Quest와 Hideout 필요 출처는 동일한 block/button 형식을 사용합니다.

- Quest 출처 클릭 → 해당 Quest
- Hideout 출처 클릭 → 해당 Hideout 시설
- stable ID 사용

## 6. Hideout Item 연결

Hideout 다음 업그레이드 재료 card 클릭 → 해당 Item 상세로 이동합니다.

## 7. 진행 완료와 Inventory 자동 차감

Quest 완료 및 Hideout 업그레이드 시 실제로 소모되는 고정 Item 요구량을 현재 Inventory에서 자동 차감합니다.

차감 정책:

- `인레이드 필수` 요구 → 인레이드 보유량에서만 차감
- 일반 요구 → 일반 보유량 우선 차감, 부족분만 인레이드에서 차감
- 기록된 보유량이 요구량보다 적으면 음수가 되지 않으며 현재 기록된 수량까지만 차감
- 유동 제출처럼 여러 Item 후보 중 실제 어느 Item을 사용했는지 프로그램이 알 수 없는 요구는 임의 후보를 골라 차감하지 않음

정확한 rollback을 위해 실제 차감한 Item별 인레이드/일반 수량을 User Progress에 소비 기록으로 보존합니다.

Quest 완료 취소 또는 Hideout 레벨 rollback 시:

- 자동 차감 기록이 있으면 사용자에게 보유량 복원 여부를 확인
- `예` → 당시 실제 차감량만 복원
- `아니오` → 진행 상태만 rollback하고 수량은 유지
- `취소` → rollback 자체를 취소
- 처리한 소비 기록은 제거하여 중복 복원을 방지

## 8. 유동 제출 정렬

Quest별 유동 제출 group과 후보 row는 일반 Item 목록처럼 부모 폭을 채우고 왼쪽 정렬을 유지합니다. 내용 길이에 따라 카드 폭이 달라지는 책장 형태를 허용하지 않습니다.

## 9. Icon 선다운로드

아이콘을 화면을 열 때 처음 받는 lazy-download에만 의존하지 않습니다.

Game Content update 시 현재 제품에서 사용하는 아이콘을 image cache에 미리 내려받습니다.

대상:

- 모든 Quest item requirement 후보
- 모든 Hideout material
- 모든 Ammo item
- Hideout station

동일 Item ID는 화면 종류와 무관하게 같은 `item-{id}` cache key를 사용합니다.

## 10. Profile 특별 상인

`특별` 상인 목록은 `고급`과 마찬가지로 Expander 형태로 기본 접힘 상태를 사용합니다.

## 11. Prestige 기본값

프레스티지 `미입력` 상태를 제품에서 없애고 **0을 기본 상태**로 사용합니다.

기존 user.db의 nullable 값은 읽을 때 제품상 0으로 정규화합니다.

## 12. 상단 상태 요약

평상시 상단 상태에는 모드/Quest/Hideout/Ammo 개수를 나열하지 않습니다.

예:

```text
정리 필요 2
```

업데이트/저장/오류가 진행 중일 때만 작업 상태 메시지를 일시적으로 사용할 수 있습니다.

## 13. Factory 야간 통합

Quest Map filter에서 Factory day/night variant를 하나의 `Factory` 항목으로 그룹화합니다.

canonical Map ID와 Quest의 원본 MapId는 변경하지 않습니다.

## 검증

- Windows Release build
- 전체 automated tests
- Windows x64 publish/package
- caliber label regression
- Wiki membership/effectiveness 분리
- 12.7×108mm 표 노출
- favorite caliber persistence
- Quest/Hideout/Ammo stable-ID navigation
- fixed item consumption + exact restore ledger
- flexible requirement 무작위 차감 금지
- Prestige null → 0 normalization
- update-time image prefetch
- Factory day/night filter grouping
