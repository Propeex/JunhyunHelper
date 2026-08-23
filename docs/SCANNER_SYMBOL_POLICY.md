# Scanner OCR symbol policy

Scanner OCR에서 punctuation/symbol은 고정 목록으로 추측하지 않는다.

현재 GameMode의 official Korean full-item catalog를 `ScannerOcrCharacterPolicy.ReplaceCatalog`에 적용할 때 모든 공식 이름을 순회하고, letter/digit/whitespace가 아닌 문자를 current symbol whitelist로 파생한다.

따라서 실제 공식 아이템명에 존재하는 따옴표, 하이픈, 괄호, 슬래시, 마침표 등의 기호는 catalog에 존재하는 동안 그대로 허용된다. 반대로 `「`처럼 현재 official item-name universe에 존재하지 않는 OCR 기호는 matcher 입력에서 제거된다.

이 방식의 목적은 특정 버전의 기호 목록을 하드코딩하는 것이 아니라 Tarkov 공식 데이터가 바뀌어도 자동으로 허용 기호 집합이 따라가게 하는 것이다.

letters/digits는 OCR이 다른 letter/digit로 혼동할 수 있으므로 삭제하지 않고 constrained fuzzy/visual correction evidence로 남긴다. CJK Han ideograph는 Korean item-title contract에서 계속 hard reject한다.
