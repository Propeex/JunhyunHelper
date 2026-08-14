# 2026-08-15 사용성 요구사항 — 층 전환 / 타층 마커 / 유동 제출 / Item Wiki

Status: **CONFIRMED / IMPLEMENTATION IN PROGRESS**

## 1. 데이터 업데이트 원칙 확인

Quest 분류는 런타임 GPT 작업이 아니다. `데이터 업데이트`에서 JunhyunHelper importer가 온라인 원본을 검증하고 canonical Quest / prerequisite / item requirement / map geometry 등으로 변환한 뒤 Game Content DB를 재구축한다.

## 2. 층 전환 시 지도 시점 보존

- Main Map의 층 위/아래 제품 단축키는 층 artwork만 교체한다.
- 층 변경 직전 사용자가 보고 있던 **같은 map-space 지점과 zoom**을 층 변경 후에도 유지한다.
- 층 단축키가 지도 중앙/이동 offset을 초기화해서 사용자가 위치 추적 새로고침을 다시 해야 하는 동작은 금지한다.
- Main Map 렌더가 완료된 뒤 MiniMap 층 이동을 계속 직렬화한다.

## 3. 다른 층 마커 표시

Map artwork는 기존 정책대로 선택한 현재 층만 명확하게 표시한다. 그러나 **marker는 다른 층이라는 이유로 완전히 숨기지 않는다.**

- 현재 층 marker: 기존 정상 강조
- 다른 층 marker: 약 50% opacity
- 위층 marker: 작은 `↑` 방향 badge
- 아래층 marker: 작은 `↓` 방향 badge
- floor 정보가 불명확한 marker는 임의 방향을 추정하지 않는다.
- 위/아래 판정은 문자열 이름이 아니라 Map config의 floor `Order`를 사용한다.
- Main Map과 MiniMap의 의미를 동일하게 유지한다.
- 대상에는 standard map marker, extract, Quest A/B/C marker, Raider처럼 JunhyunHelper가 추가한 marker가 포함된다.

## 4. 유동 제출 상태 필터

유동 제출은 별도 영구 완료 상태를 새로 저장하지 않는다. 이미 계산되는 `FlexibleQuestItemProgress.IsFulfilled`가 현재 Inventory에 대한 권위 상태다.

유동 제출 화면에서도 상태 dropdown을 활성화한다.

```text
필요   = 해당 Quest의 유동 제출 objective 중 아직 충족되지 않은 것이 있음
전체   = 충족 여부와 무관하게 모두 표시
충분   = 해당 Quest의 모든 유동 제출 objective가 현재 Inventory로 충족됨
```

- 유동 제출 화면 기본값은 `필요`.
- 보유량을 충분히 입력해 objective가 충족되면 `필요` 목록에서 자동으로 사라진다.
- `정리 필요`, `판단 보류`는 일반 Item cleanup 의미이므로 유동 제출 상태 dropdown에서는 노출하지 않는다.
- 유동 제출은 본질적으로 Quest 용도이므로 기존 용도 dropdown은 계속 비활성화한다.

## 5. Item Wiki

- Item 상세에도 Quest 상세과 동일한 의미의 `위키` 버튼을 제공한다.
- canonical `GameItem.WikiUrl`을 사용한다.
- URL이 유효한 HTTP/HTTPS 링크일 때만 버튼을 활성화한다.
- 런타임 검색이나 GPT 추론으로 Wiki URL을 만들지 않는다.

## 6. 회귀 / 최적화 기준

이 변경 후 전체 자동 테스트와 Windows x64 publish를 통과한다. 실제 Map + MiniMap smoke에서 층 렌더/마커/정상 종료를 확인하고, 새 marker 표시가 기존 player marker / marker size / MiniMap click-through / current-floor artwork 정책을 깨지 않는지 확인한다.
