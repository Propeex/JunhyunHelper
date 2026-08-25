# 준현 헬퍼 v1.7.1

## Data Update 검증 회귀 수정

v1.7.1은 v1.7.0의 게임 데이터 안전 업데이트 기능에서 발견된 검증 회귀를 수정하는 hotfix입니다.

json.tarkov.dev의 퀘스트 objective에서 `item/items`와 `questItem`은 같은 관계가 아닙니다. `item/items`는 준현 헬퍼가 canonical `/items`에 연결해 사용하는 일반 item reference이고, `questItem`은 별도의 quest-only entity reference입니다. v1.7.0은 `QuestItemId`도 canonical `/items`에 존재해야 한다고 검증해 정상 데이터 업데이트를 거부할 수 있었습니다.

v1.7.1에서는 검증 경계를 다음처럼 제한합니다.

- 모든 objective의 `item/items`는 기존처럼 canonical `/items`에 반드시 존재해야 하며, objective kind와 관계없이 dangling reference를 Fatal로 차단합니다.
- `questItem`/`QuestItemId`만 quest-only reference로 취급하여 canonical `/items` 부재만으로 Fatal 처리하지 않습니다.
- 일반 `QuestItemRequirement`, 은신처 필요 아이템, 탄약 및 탄약 획득 조건의 item/currency/requirement 참조 검증은 기존처럼 fail-closed로 유지합니다.
- partial-payload/completeness guard, candidate 재검증, Last Known Good 보존 정책도 변경하지 않습니다.

실제 회귀에 전달된 ID는 deterministic regression test에 고정했습니다.

```text
quest: 6524640578137d9edc1628e4
objective: objective-6710469f5474276231657a22
quest-only item/entity reference: 6662e9aca7e0b43baa3d5f74
```

회귀 테스트는 위 ID가 `QuestItemId`로 존재하면서 canonical `/items`에 없을 때는 허용되는지 확인합니다. 동시에 같은 item ID가 일반 `ItemIds` canonical reference로 들어오면 `quest-objective.item.missing` Fatal이 발생하는지 확인합니다. Submit/Find/Sell objective, 일반 `QuestItemRequirement`, hideout, ammunition의 실제 dangling reference도 계속 차단됩니다.

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

Scanner OCR 판단 기준과 candidate cap도 변경하지 않았습니다.

```text
structural floor: 0.34
HEADER_FRAME_LOCKED: 0.68
continuous candidate cap: 8
one-shot candidate cap: 12
```

## 진단 출처 주의

초기 전달된 diagnostic job `97643534791`의 원본 로그를 GitHub Actions API로 다시 내려받아 target quest/objective/item ID를 직접 검색한 결과, 세 target ID는 해당 job 로그에 존재하지 않았습니다(`TARGET_OBJECTIVE_REF_COUNT=0`, `TARGET_ANY_LINE_COUNT=0`). 따라서 해당 job에서 target objective의 정확한 type/kind가 확인됐다고 기록하지 않습니다.

다만 동일 원본 broad probe에서 누락으로 출력된 objective reference 284건은 전부 `missingQuestItem`이었고 `missingItems`는 0건이었습니다. 이 증거와 importer의 분리된 `QuestItemId` 모델을 기준으로 수정 범위를 `QuestItemId`의 canonical `/items` 존재 요구 제거에만 제한했습니다.

릴리즈 준비 시점의 현재 live `/tasks`에서는 target quest가 이미 제거되어 있으므로, 실제 전달 ID를 사용하는 deterministic fixture가 이 회귀를 지속적으로 보존합니다.
