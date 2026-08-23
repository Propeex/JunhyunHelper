# Scanner live-evidence correction — 2026-08-23

## 관측 근거

v1.3.1 공개 후 실제 Tarkov UI가 포함된 두 DisplayTest 캡처에서 다음 패턴이 관측됐다.

- `Thermite 테르밋` → OCR에 `` ` The「mite 테르밋`` 계열 노이즈가 포함됨
- `Gunpowder "Eagle" 화약` → OCR에 `` ` Gunpowde「 ...`` 계열 노이즈가 포함됨
- 두 화면 모두 실제 상세창 좌측 magnifier와 우측 red close/X가 픽셀로 명확히 존재함
- magnifier 자체는 약 21×19~20 px의 bright-neutral ring/handle component로 분리 가능했으나, v1.3.1 association 조건이 nearby text component를 필수로 요구해 실제 anti-aliasing/component 분할에 따라 후보를 버릴 수 있었음
- `「`처럼 current official item-name catalog에 존재하지 않는 punctuation/symbol도 기존 generic 18% invalid-character allowance 때문에 matcher evidence에 남을 수 있었음
- medium-length title은 한 글자 누락만으로 90.9%가 되어 기존 94% floor 아래로 떨어질 수 있었음

## 확정 수정 원칙

1. **기호는 current official catalog 기반 whitelist**
   - 현재 공식 아이템명에 실제 존재하는 punctuation/symbol만 OCR matcher evidence에 유지한다.
   - 그 외 punctuation/symbol은 Item identity evidence가 아니므로 matcher 전에 제거한다.
   - whitelist는 하드코딩하지 않고 현재 catalog 교체 시 다시 파생한다.
   - 문자/숫자는 OCR 자체가 다른 문자로 오인할 수 있으므로 fuzzy correction evidence로 남긴다.
   - CJK Han ideograph는 기존과 같이 Korean item-title contract에서 hard reject한다.

2. **한 글자 OCR 오류는 percentage threshold와 별도 처리**
   - normalized official name 길이 7 이상에서 edit distance가 정확히 1이고 top1-top2 margin이 충분한 경우 `BOUNDED_EDIT_1`로 허용한다.
   - 짧은 이름에는 적용하지 않는다.
   - 여러 글자 오류/저 80%대 OCR을 이 규칙만으로 확정하지 않는다. 그런 사례는 cleaned ROI + current-catalog Tarkov-font visual verification을 사용한다.

3. **magnifier의 nearby text는 corroboration이지 prerequisite가 아님**
   - structural panel top/fallback title/refined title-field의 vertical union에서 magnifier를 찾는다.
   - ring/hollow-center/lower-right-handle morphology와 expected left-header position을 우선 evidence로 사용한다.
   - nearby title glyph가 정상 component로 분리되면 confidence를 보강하지만, glyph 분할이 불안정하다는 이유만으로 강한 magnifier morphology를 버리지 않는다.
   - first Korean glyph를 magnifier로 오인하지 않도록 left-header position과 morphology gate는 유지한다.

4. **오탐보다 미탐 우선 원칙은 유지**
   - global fuzzy threshold를 단순히 80~90%로 낮추지 않는다.
   - current official catalog identity authority, top1/top2 ambiguity fail-closed, visual score/margin gate를 유지한다.

## 영향 없는 계약

- highest trader price 의미
- flea avg24hPrice 의미
- Needed Items `RequiredTotal` 의미
- Game Content schema
- user.db schema
- Scanner display settings schema
- scan-time network 금지
- game memory / DLL injection / packet interception 금지
