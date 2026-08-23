# Scanner v1.3.2 — live-evidence correction

기준일: 2026-08-23

상태: **PATCH CANDIDATE**

v1.3.1 공개 후 실제 Tarkov 상세창 두 사례에서 확인된 magnifier association, impossible OCR punctuation, medium-title one-glyph miss를 수정한다.

## Recognition pipeline delta

```text
inspect header pixels
→ broad header band
→ red close/X
→ magnifier ring/hollow/handle + left-header position
   + nearby title glyph corroboration (optional)
→ magnifier-free title ROI
→ Windows ko-KR OCR
→ current-catalog symbol sanitation
→ semantic matcher
   ├─ normal exact/fuzzy gate
   └─ unique single-edit bounded recovery for normalized length >= 7
→ strict current-catalog Tarkov-font visual corroboration/recovery when needed
→ Item ID or fail closed
```

## Symbol policy

- punctuation/symbol whitelist는 current official Korean item-name catalog에서 매번 파생한다.
- catalog에 없는 punctuation/symbol은 OCR matcher evidence에서 제거한다.
- `「` 등 일본식 bracket이 current catalog에 없으면 자동 제거된다.
- letters/digits는 OCR confusion correction을 위해 유지한다.
- Han ideograph는 hard reject한다.
- symbol 제거 후 identity text가 3 alphanumeric characters 미만이면 fail closed한다.

## Similarity policy

기존 percentage threshold는 유지한다.

- normalized length <= 6: 최소 98%
- 7~12: 최소 94%
- 13 이상: 기본 90%
- top1-top2 margin gate 유지

단, 길이 7 이상에서 **정확히 한 번의 edit**만 존재하고 runner-up과 최소 8%p 이상 분리되면 `BOUNDED_EDIT_1`로 허용한다. 이는 `Thermite 테르밋`처럼 한 글자 누락 때문에 90.9%가 되는 중간 길이 제목을 위한 제한적 복구이며, 81%대 다중 오류 텍스트를 OCR만으로 허용하는 규칙이 아니다.

## Compatibility

- Desktop PATCH target: v1.3.2
- Content schema v7 유지
- user.db schema v1 유지
- Scanner display settings schema v4 유지
- Scanner catalog cache schema 변경 없음
- v1.3.1 사용자 데이터 migration 없음
