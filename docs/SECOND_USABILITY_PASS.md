# SECOND USABILITY PASS — 2차 실사용 피드백

검증/구현일: **2026-08-09**

상태: `IMPLEMENTED / AUTOMATED VERIFICATION PASSED / USER UI TEST NEXT`

이 문서는 첫 통합 Windows 테스트 빌드를 실제로 사용한 뒤 확정된 2차 제품 피드백과 구현 결정을 기록합니다.

## 1. 이미지가 표시되지 않는 문제

### 확인된 원인

canonical Item/Hideout/Ammo 이미지 URL은 정상적으로 존재했지만, 실제 원본에는 WebP 계열 자산이 포함됩니다.

기존 Desktop cache는 원본 bytes를 그대로 저장한 뒤 WPF `BitmapImage`에 직접 맡겼습니다. 이 방식은 실행 PC의 Windows 이미지 codec 환경에 따라 WebP decode가 실패할 수 있어, 데이터와 URL이 정상이어도 화면에는 이미지가 나타나지 않을 수 있습니다.

### 결정

외부 이미지 형식을 WPF가 직접 해석하게 하지 않습니다.

```text
canonical image URL
→ bytes 다운로드
→ SkiaSharp로 decode
→ 크기/유효성 검증
→ PNG로 normalize
→ local image-cache
→ WPF BitmapImage
```

원칙:

- 원천 URL은 계속 canonical Game Content가 소유한다.
- 이미지 cache는 비권위 데이터다.
- source 형식이 WebP 등으로 달라도 Desktop 렌더링 형식은 PNG로 통일한다.
- decode/download 실패는 Game Content 또는 User Progress 실패가 아니다.
- 최대 다운로드 크기 및 이미지 치수 제한을 유지한다.

## 2. 남아 있는 판정 불가 Quest

사용자 결정:

> 현재 Quest 선행/해금 판정 로직으로도 끝내 확정할 수 없는 Quest는 사용자 화면에서 `진행 중`으로 취급한다.

### 구현 경계

Core의 `Indeterminate` 의미 자체는 삭제하지 않습니다.

Core는 계속 다음과 같은 진단 사실을 보존할 수 있습니다.

- 입력값 누락
- 지원하지 않는 availability requirement
- 참조 누락
- dependency cycle
- 기타 안전하게 확정할 수 없는 조건

그러나 Application의 **제품 표시 정책**에서 residual `Indeterminate`를 `Current`로 승격합니다.

```text
Core evaluation: Indeterminate + reasons
→ Application product policy
→ Current + same diagnostic reasons
→ Desktop: 진행 중
```

따라서:

- Quest가 판정 문제 목록에 따로 빠지지 않는다.
- 진행 중 목록에 나타난다.
- 완료 처리할 수 있다.
- 기존 diagnostic reason은 개발/디버깅에 남는다.

이 결정은 확정 가능한 Locked/Unavailable 판정을 무시한다는 의미가 아닙니다. **Indeterminate에만** 적용합니다.

## 3. ScrollBar 디자인

기존 구현은 색상/Thumb 일부만 변경하고 WPF native ScrollBar의 track/arrow chrome을 완전히 교체하지 않았습니다.

결정:

- vertical/horizontal ScrollBar 전체 ControlTemplate을 dark theme 전용으로 교체
- native arrow button chrome 제거
- 둥근 dark track
- 둥근 thumb
- hover/drag 상태를 현재 accent 체계로 표시

## 4. 유동 제출 후보 분리

유동 제출은 하나의 요구량을 여러 후보 Item ID의 합계로 제출하는 구조입니다.

기존에는 첫 보유량 입력 경로를 보장하기 위해 모든 후보를 일반 Item 목록에 노출했고, 실제 사용에서는 후보가 너무 많이 섞였습니다.

결정:

- 유동 제출 때문에만 목록에 들어온 후보는 일반 Item 목록에서 제외한다.
- `유동 제출 보기`를 별도 view로 둔다.
- 해당 view에서는 모든 후보를 계속 접근할 수 있어 첫 보유량 입력 경로를 잃지 않는다.
- 어떤 Item이 일반 고정 필요량/실제 보유량도 가지고 있다면 일반 목록에서도 유지할 수 있다.
- 유동 제출의 계산 및 cleanup 보호 원칙은 변경하지 않는다.

## 5. Item 종류 분류

수동 이름 규칙이나 과거 Tarkov-Helper의 하드코딩 분류를 새 권위 데이터로 만들지 않습니다.

현재 `json.tarkov.dev` Item payload가 이미 다음 관계를 제공합니다.

```text
item.categories[] = category IDs
itemCategories[categoryId]
  - id
  - name
  - normalizedName
```

준현 헬퍼 importer가 이 category metadata를 canonical `GameItem`에 보존하고 Desktop이 Tarkov식 상위 표시 그룹으로 정규화합니다.

현재 표시 그룹:

- 무기
- 무기 부품
- 장비
- 탄약
- 의약품
- 식량/음료
- 물물교환
- 열쇠
- 정보
- 특수 장비
- 퀘스트 아이템
- 화폐
- 지도
- 기타

분류 원칙:

- 외부 category ID 자체는 변경하지 않는다.
- normalized category metadata를 importer가 매 업데이트 다시 읽는다.
- 새로운/알 수 없는 category는 숨기지 않고 `기타` fallback으로 보존한다.
- Item 화면에서 종류 filter를 제공한다.

### Content snapshot v2

새 category metadata가 canonical Game Content에 추가되었으므로 Content snapshot schema를 v2로 올립니다.

기존 v1 content.db는 새 빌드에서 자동으로 온라인 재구축합니다.

중요:

- `user.db`는 별도 저장소이므로 삭제/초기화하지 않는다.
- Profile, Quest 완료, Hideout level, Inventory 등 User Progress는 그대로 유지된다.

## 6. Quest 제출 Item UI

Quest 상세에서 제출 Item을 긴 문자열로 나열하지 않습니다.

각 요구 Item은 독립된 card/row로 표시합니다.

표시:

- Item icon
- Item 이름
- 요구 수량
- FIR 여부
- 유동 제출 후보 여부

유동 제출 requirement는 후보마다 `유동 제출 후보`임을 표시하며 수량은 해당 그룹의 합계 목표임을 명시합니다.

## 7. 정보 간 상호 이동

연결은 화면의 표시 이름이 아니라 canonical stable ID를 사용합니다.

### Quest → Item

Quest 상세의 Item card 클릭:

```text
Quest item requirement ItemId
→ Item 탭
→ 해당 Item 선택
→ 상세 표시
```

현재 진행 기준 필요 목록에 없는 Item도 canonical Item에 존재하면 reference detail을 열 수 있습니다.

### Item → Quest

Item 상세의 Quest 출처 또는 유동 제출 Quest card 클릭:

```text
QuestId
→ Quest 탭
→ filter를 안전하게 초기화
→ 해당 Quest 선택/스크롤
```

### Quest → 선행 Quest

선행 Quest도 문자열이 아니라 클릭 가능한 card로 표시합니다.

```text
RequiredQuestId
→ 같은 Quest 탭에서 해당 Quest 선택/스크롤
```

이 방식은 게임 패치로 이름/번역이 바뀌어도 stable ID 관계가 유지되는 한 링크가 깨지지 않습니다.

## 8. 검증 상태

자동 검증 완료:

- Desktop Release build 성공
- 전체 Core/Application/Infrastructure test 성공
- residual Indeterminate → Current 회귀 test 성공
- Windows x64 self-contained publish 성공
- ZIP/artifact 생성 성공
- documentation-inclusive CI 성공
- unresolved PR review thread 없음

실제 Windows 사용자 환경에서 확인할 항목:

- 기존 `user.db`를 유지한 채 content v1 → v2 자동 재구축
- Item/Hideout/Ammo/Quest Item 이미지 실제 표시
- ScrollBar vertical/horizontal 실제 모양과 조작성
- 일반 Item 목록에서 flexible-only 후보 분리
- `유동 제출 보기`에서 모든 후보 접근
- Item category filter 분류 결과
- Quest → Item
- Item → Quest
- prerequisite Quest → Quest

## 9. 변경하지 않는 것

이번 개선으로 다음 핵심 원칙은 바뀌지 않습니다.

- 일반 게임 데이터 업데이트에 GPT 불필요
- Game Content와 User Progress 분리
- 안전한 cleanup을 증명하지 못하면 보호
- Map/Scanner 실제 기능은 아직 별도 후속 범위
- 기존 `Propeex/Tarkov-Helper`는 참고 자료일 뿐 제품 사양의 권위 원천이 아님
