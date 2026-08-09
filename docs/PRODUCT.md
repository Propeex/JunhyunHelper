# PRODUCT — 준현 헬퍼 제품 정의

이 문서는 **무엇을 만들고 왜 만드는지**를 정의하는 공식 제품 요구사항입니다.

우선순위는 `AGENTS.md`를 따릅니다. 현재 사용자가 명시한 제품 의도가 과거 구현보다 우선합니다.

## 1. 제품 정의

`CONFIRMED`

**준현 헬퍼**는 Escape from Tarkov의 최신 게임 데이터를 온라인 원천에서 받아 프로그램이 스스로 canonical Game Content와 로컬 DB로 변환·재구축하고, 이를 User Progress와 결합해 플레이에 필요한 정보를 제공하는 Windows 데스크톱 헬퍼입니다.

저장소: `Propeex/JunhyunHelper`

핵심 원칙:

> 게임 데이터의 내용이 바뀌어도 외부 형식이 importer가 이해할 수 있는 범위라면 프로그램이 최신 데이터를 다시 내려받아 같은 변환 규칙으로 DB를 다시 만들 수 있어야 합니다.

일반적인 데이터 업데이트에 GPT가 개입하지 않습니다.

## 2. 데이터 갱신 / 저장

`CONFIRMED / IMPLEMENTED`

```text
온라인 데이터
→ 다운로드
→ 외부 형식/필수 의미 검증
→ canonical model 변환
→ candidate DB
→ 관계/read-back 검증
→ active Game Content 교체
→ 제품 icon 선다운로드
→ User Progress와 결합
→ 파생 결과 계산
→ Desktop 표시
```

원칙:

- 내용 변화는 importer가 이해하는 한 자동 흡수
- 비호환 schema/의미 변화는 update 실패
- 실패한 candidate가 마지막 정상 active content를 덮어쓰지 않음
- Game Content update가 `user.db`를 삭제/덮어쓰지 않음
- 파생 결과를 별도 권위 데이터로 저장하지 않음
- 런타임 AI/GPT 없음

### 2.1 Game Content schema

현재 **v3**.

- v2: Item category metadata
- v3: Ammo의 현재 Wiki Ballistics 표 등록 여부를 Armor effectiveness와 별도 보존

이전 content snapshot은 온라인 source에서 자동 재구축합니다. `user.db`는 별도입니다.

### 2.2 User Progress

저장 사실:

- Profile / GameMode
- player level / faction / edition / prestige
- Trader LL / 필요한 standing
- completed Quest / 필요한 explicit permanent failure
- Hideout level
- 인레이드 / 일반 Inventory
- 자동 inventory reconciliation용 Quest / Hideout 실제 소비 기록

소비 기록은 자동 차감과 rollback을 정확히 맞추기 위한 bookkeeping 사실이며 Game Content update와 독립입니다.

## 3. 데이터 원천

### 3.1 1차 원천

`json.tarkov.dev`

- Quest
- Hideout
- Item + category metadata
- Trader
- Map 최소 메타데이터
- Barter / Craft
- Ammo raw stats

지원 GameMode:

- regular
- pve
- pvp-season

### 3.2 보조 원천

- TarkovTracker overlay: edition rules only
- Escape from Tarkov Wiki Ballistics:
  - 현재 Ammo 비교 표 membership
  - Armor Class 1~6의 명시 0~6 effectiveness

Wiki는 raw Ammo stats의 대체 원천이 아닙니다. Wiki 장애/구조 이상은 기본 Game Content를 손상시키지 않습니다.

## 4. Profile

`CONFIRMED / IMPLEMENTED`

- 한 GameMode당 profile 하나
- 상단 Profile dropdown 안 `새 프로필`
- 삭제는 `프로필 수정` 안
- player level: `- / 값 / +`
- Prestige: **기본 0**, 미입력 상태 없음
- Fence reputation: 주요 진행값, 0.1 단위
- 핵심 Trader: LL 중심, 게임식 순서
- 일반 상인 탭 밖 Trader: `특별` Expander, 기본 접힘
- Quest 판정에 실제로 필요한 비-Fence standing만 `고급` 입력

과거 profile의 Prestige null은 읽을 때 0으로 정규화합니다.

## 5. Quest

`CONFIRMED / IMPLEMENTED`

준현 헬퍼는 실제 게임에서 수주 가능한 Quest를 이미 수락한 것으로 간주합니다. 별도 Accept 버튼은 두지 않습니다.

사용자 상태:

- 진행 중
- 잠김
- 사용 불가
- 완료

Core `Indeterminate`는 diagnostic으로 유지하되 현재 지원 규칙을 모두 적용한 후에도 남는 residual Indeterminate는 Application 제품 경계에서 **진행 중**으로 보여줍니다. 확정 가능한 Locked/Unavailable은 변경하지 않습니다.

사용자 조작:

- 완료
- 완료 취소
- 정말 필요한 비재시작형 영구 실패만 실패 / 실패 취소

상세:

- 목표
- 제출 Item card/list: icon / 이름 / 수량 / 인레이드 여부
- 선행 Quest
- `위키`
- Quest Item click → Item
- prerequisite Quest click → Quest

### 5.1 Map filter grouping

UI filter에서만 variant를 병합합니다.

- Ground Zero + Ground Zero 21+ → `Ground Zero`
- Factory day/night → `Factory`

canonical Map ID와 Quest 원본 MapId는 보존합니다.

### 5.2 자동 scroll 정책

일반 refresh/진행 변경 때문에 목록이 임의로 이동하지 않습니다. 사용자가 cross-navigation을 명시적으로 요청했을 때만 목표 row로 이동할 수 있습니다.

## 6. Hideout

`CONFIRMED / IMPLEMENTED`

- 미입력 = Lv.0
- `- / 현재 level / +`
- 상세는 바로 다음 upgrade
- Needed Items는 현재 level 이후 모든 미래 upgrade material 포함
- station image
- 다음 upgrade material은 icon/name/count/인레이드 card/list
- material click → Item

## 7. Needed Items / Item

`CONFIRMED / IMPLEMENTED`

목적:

> 현재만이 아니라 앞으로 사용할 가능성이 남아 있는 Item을 미리 모으고, 더 이상 필요하지 않은 실제 보유품만 안전하게 정리한다.

포함:

- Current Quest
- 미래에 조건 충족 가능한 Locked Quest
- 아직 닫히지 않은 가능한 Quest branch
- 안전하게 제외할 수 없는 잠재 Quest 요구
- 현재 Hideout level 이후 모든 future upgrade material

제외:

- Completed Quest
- 현재 character에서 영구 불가임이 증명된 Quest / 닫힌 branch
- 이미 지난 Hideout upgrade

### 7.1 목록

한 row에서 다음 네 값을 비교합니다.

- 필요 · 인레이드
- 필요 · 일반
- 보유 · 인레이드
- 보유 · 일반

불필요한 우측 상태 badge는 두지 않습니다.

### 7.2 상세 / 입력

- 인레이드 필요 N
- 일반 필요 N
- 인레이드 `− / 수량 / +`
- 일반 `− / 수량 / +`
- +/- 클릭 즉시 저장
- 직접 숫자 입력 + 명시적 저장 가능
- Quest와 Hideout 필요 출처를 동일한 clickable block 형태로 표시
- Quest 출처 → Quest
- Hideout 출처 → Hideout facility

종류 dropdown은 현재 view/search/filter에서 실제 Item이 있는 category만 표시합니다.

### 7.3 cleanup

- 미래 필요량 충족 후 안전하게 남는 초과분만 `정리 필요`
- 인레이드 최소 요구 보호
- 유동 제출 후보 보호
- 안전성을 증명하지 못하면 판단 보류
- Game Content update가 실제 보유량을 자동 삭제하지 않음

### 7.4 유동 제출

여러 Item ID를 하나의 objective 후보로 받는 요구는 그룹 단위로 계산합니다.

- 후보 보유량 합산
- 후보 하나를 임의 선택하지 않음
- 별도 `유동 제출 보기`
- Quest별 group/card
- 후보 row와 group은 full-width / left aligned
- Quest → Quest, 후보 → Item
- 목표 종료 전 후보별 cleanup은 보수적으로 보호

## 8. 진행 완료와 Inventory 자동 차감

`CONFIRMED / IMPLEMENTED IN FOURTH USABILITY PASS`

고정 제출/재료 요구는 실제 진행 처리와 함께 tracked Inventory에서 자동 차감합니다.

### 8.1 차감 순서

```text
인레이드 필수 요구
→ 인레이드에서만 차감

일반 요구
→ 일반 우선 차감
→ 부족분만 인레이드 차감
```

- tracked 보유량보다 많이 차감하지 않음
- 음수 금지
- 유동 제출은 실제 어느 후보를 사용했는지 알 수 없으므로 자동 차감하지 않음

### 8.2 Quest 완료 취소 / Hideout rollback

실제 자동 차감량을 ledger에 기록합니다.

rollback 시 사용자에게 복원 여부를 묻습니다.

- **예**: 당시 실제 차감량만 복원 + ledger 제거
- **아니오**: 보유량은 그대로 + ledger 유지
- **취소**: rollback 중단

복원하지 않은 ledger는 다시 완료/재업그레이드했을 때 같은 재료를 **중복 차감하지 않기 위해 유지**합니다.

## 9. Ammo

`CONFIRMED / IMPLEMENTED`

선택 GameMode의 read-only 비교 기능입니다.

- 이름 검색 없음
- caliber dropdown
- 표시 열 선택
- penetration power 오름차순 → damage → name
- main table 최소 수급 경로
- 상세 전체 수급 경로
- item image
- 해금 Quest click → Quest 상세

### 9.1 비교 membership

Wiki Ballistics source가 healthy하면 **현재 Wiki 표에 등록된 Ammo만** 비교 화면에 둡니다.

표 등록 여부와 effectiveness는 별개입니다.

- 등록 true + effectiveness 있음 → 정상 0~6 표시
- 등록 true + effectiveness 미매칭 → Ammo는 유지, 해당 효율은 `?`
- 등록 false → Wiki 표 비교 대상에서 제외
- Wiki source unavailable/unhealthy → raw Ammo를 임시 표시; 빈 표/데이터 손상을 만들지 않음

이 구조로 장난/미사용/비교 표 외 Ammo는 정상 source 상태에서 제외하면서 `12.7x108mm`처럼 표에 있는 Ammo가 rating 파싱 문제만으로 사라지지 않게 합니다.

### 9.2 caliber 표시

raw ID는 내부에 유지하고 사용자에게 cartridge 명칭을 표시합니다.

대표:

```text
.308 Marlin Express
9.3x64mm
9x18mm Makarov
.50 Action Express
12.7x108mm
.45 ACP
.300 Blackout
.338 Lapua Magnum
.366 TKM
12/70
```

### 9.3 Armor effectiveness

- Class 1~6 여섯 칸
- 각 칸 0~6
- 색상
- 왼쪽부터 Class 1 → 6
- cell 내부에는 effectiveness 값만 표시, class 숫자 중복 표시 금지
- 자체 heuristic 금지

### 9.4 caliber favorites

- 현재 caliber 즐겨찾기 toggle
- 즐겨찾기 전용 dropdown
- 선택 → 해당 caliber 이동
- `%LocalAppData%/JunhyunHelper/ammo-favorites.json`에 UI preference로 저장

## 10. 이미지

`CONFIRMED / IMPLEMENTED`

권위 데이터는 canonical URL이며 cache는 비권위 presentation asset입니다.

```text
canonical URL
→ download bytes
→ SkiaSharp decode
→ validation
→ PNG normalize
→ image-cache
→ WPF
```

Game Content update 성공 후 제품에서 사용하는 이미지를 **미리 다운로드**합니다.

- Quest Item candidates
- Hideout materials
- Ammo items
- Hideout stations

동일 Item ID는 `item-{id}` cache key를 공유합니다. 개별 이미지 실패는 Game Content update 실패가 아닙니다.

## 11. UI

- dark theme
- dark dropdown popup / light text
- 정렬된 full-width list rows
- native white control chrome 방지
- 일반적인 track + thumb ScrollBar
- refresh-driven automatic list scrolling 금지

평상시 상단 상태는 불필요한 모드/Quest/Hideout/Ammo count를 제거하고 다음 하나만 표시합니다.

```text
정리 필요 N
```

작업 중에는 update/save/error 상태 메시지가 일시적으로 우선할 수 있습니다.

## 12. Map / Scanner

`PLACEHOLDER IMPLEMENTED`

상단에 `지도`, `스캐너` 탭이 존재하고 현재는 `준비 중`을 표시합니다.

실제 Map 공급원과 Scanner 동작은 별도 제품 요구사항 확정 전까지 구현하지 않습니다.

## 13. 현재 실사용 개선 상태

- 첫 실사용 피드백: merged
- 2차: PR #36 merged
- 3차: PR #37 merged
- 4차: PR #39 implemented / final verification

4차 세부 계약: `docs/FOURTH_USABILITY_PASS.md`
