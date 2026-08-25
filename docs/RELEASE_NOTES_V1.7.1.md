# 준현 헬퍼 v1.7.1

## Data Update 검증 회귀 수정

v1.7.1은 v1.7.0의 게임 데이터 안전 업데이트 기능에서 발견된 검증 회귀를 수정하는 hotfix입니다.

json.tarkov.dev의 퀘스트 objective 필드는 모두 같은 종류의 item reference가 아닙니다. `item/items`는 objective 의미에 따라 실제 인벤토리 아이템이거나 조건 selector일 수 있고, `questItem`은 일반 `/items`와 별개인 QuestItem objective 전용 참조입니다. v1.7.0은 이들을 canonical `/items` 관계로 과도하게 검증해 정상 데이터 업데이트 전체를 거부할 수 있었습니다.

v1.7.1에서는 objective 의미를 기준으로 검증 경계를 분리합니다.

- `Submit`, `FindOrCollect`, `Sell` objective의 `item/items`는 실제 canonical item 참조이므로 계속 엄격하게 검증합니다.
- `Other` objective의 `item/items`는 objective 조건 selector로 취급하며 canonical `/items` 부재만으로 업데이트 전체를 Fatal 처리하지 않습니다.
- `questItem`은 별도의 QuestItem objective 계약이므로 canonical `/items` 존재 여부를 검사하지 않습니다.
- 일반 `QuestItemRequirement`, 은신처 필요 아이템, 탄약 및 탄약 획득 조건의 item 참조 검증은 기존처럼 fail-closed로 유지합니다.

실제 회귀에 사용된 ID는 deterministic regression test에 고정했습니다.

```text
quest: 6524640578137d9edc1628e4
objective: objective-6710469f5474276231657a22
special item selector: 6662e9aca7e0b43baa3d5f74
```

별도 회귀 테스트는 제출·획득·판매 objective의 진짜 dangling canonical item reference와 일반 QuestItemRequirement의 누락 item이 계속 차단되는지 확인합니다.

## 사용자 오류 표시 개선

데이터 업데이트가 무결성 검증에서 거부되면 더 이상 generic 문구만 표시하지 않습니다. 첫 번째 Fatal 검증 사유를 사용자가 이해할 수 있는 짧은 한국어 설명으로 표시하고, 기존 정상 데이터가 유지됐음을 함께 알립니다.

## 유지되는 안전 계약

다음 v1.7.0 보호 장치는 변경하지 않았습니다.

- 전체 endpoint를 사용한 candidate build
- candidate 활성화 전 무결성 검증
- 기존 정상 데이터와 비교하는 partial-payload/completeness guard
- candidate DB 저장 후 disk read-back/revalidation
- 실패 시 Last Known Good 유지
- update transaction 직렬화
- Scanner market/catalog fail-soft 보호

Scanner OCR 판단 기준도 변경하지 않았습니다.

```text
structural floor: 0.34
HEADER_FRAME_LOCKED: 0.68
continuous candidate cap: 8
one-shot candidate cap: 12
```

## 진단 출처 주의

초기 전달된 diagnostic job `97643534791`의 원본 로그를 GitHub Actions API로 다시 검사한 결과, 위 target quest/objective/item ID는 해당 job 로그에 존재하지 않았습니다. 또한 릴리즈 준비 시점의 live `/tasks`에서는 해당 quest가 Regular/PvE/PvpSeason 모두 이미 제거된 상태였습니다.

따라서 v1.7.1은 해당 job에서 target objective의 type/kind가 확인됐다고 기록하지 않습니다. 대신 현재 json.tarkov.dev 계약에서 `TaskObjectiveItem`과 `TaskObjectiveQuestItem`이 서로 다른 objective schema이며 `questItem`이 후자에 속한다는 점, 현재 live payload의 검증 결과, 그리고 준현 헬퍼 importer의 `QuestItemObjectiveKind` 경계를 함께 기준으로 수정 범위를 제한했습니다.
