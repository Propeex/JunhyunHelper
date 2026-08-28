# 준현 헬퍼 v1.8.0

## Scanner 아이템 정보 DB

Scanner 탭의 아이템 검색 상세를 확장했습니다.

- 아이템 종류, 크기, 무게, 플리마켓 거래 가능 여부, 기본 가격을 추가했습니다.
- 퀘스트와 은신처 업그레이드에서 해당 아이템이 필요한 위치를 전체 Game Content 기준으로 확인할 수 있습니다.
- 어떤 은신처 제작에서 재료로 쓰이는지와 어떤 상인 교환에서 재료로 쓰이는지 확인할 수 있습니다.
- 상인 현금 구매는 충성도 레벨, 가격/화폐, 구매 제한, 데이터에 제공되는 재고 갱신 시각을 표시합니다.
- 상인 교환은 요구 재료/수량과 결과 수량을 표시합니다.
- 은신처 제작은 재료/수량, 비소모 도구, 결과 수량, 제작 시간을 표시합니다.
- 플리마켓과 레이드 획득을 수급처로 구분합니다.
- 제작·교환 관계에 표시된 아이템을 클릭하면 그 아이템의 Scanner 상세로 바로 이동할 수 있습니다.
- 상세 정보가 길어져도 전체 내용을 확인할 수 있도록 아이템 상세 영역을 세로 스크롤할 수 있게 했습니다.

## 데이터 안정성

- 관계 데이터는 Scanner 검색 시 외부 API를 호출하지 않습니다.
- 기존 Game Content 업데이트가 내려받는 Items/Barters/Crafts 데이터에서 관계 그래프를 구축하고 검증한 뒤 로컬 snapshot에 저장합니다.
- snapshot schema를 v8로 올렸으며 v3-v7 데이터는 계속 읽을 수 있습니다.
- 관계 데이터가 없는 구형 snapshot을 실제 관계 없음으로 오해하지 않습니다. 새 게임 데이터 업데이트가 성공하면 관계 정보가 활성화됩니다.
- 관계 데이터의 잘못된 item/trader/station/quest/currency 참조와 잘못된 가격/수량은 활성화 전에 차단합니다.
- 실패한 업데이트 후보는 기존 정상 LKG를 덮지 않습니다.

## 유지되는 계약

- 기존 `필요 개수`와 `필요한 곳`은 ItemsWorkspace의 현재 프로필 계산을 그대로 사용합니다.
- Scanner OCR/인식 임계값, matcher, visual recovery 정책은 변경하지 않았습니다.
- Map/MiniMap donor revision과 기존 프로그램 업데이트/데이터 LKG 계약은 변경하지 않았습니다.

## 릴리즈 검증

공개 릴리즈 전 전체 deterministic tests, Release build, win-x64 single-file publish, 실제 EXE Product UI/Scanner/Map/Factory/MiniMap smoke, 패키지/checksum 및 public release readback을 완료해야 합니다.
