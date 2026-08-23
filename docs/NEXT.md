# NEXT

기준일: 2026-08-23

v1.3.2는 정식 공개 및 public re-download 검증까지 완료되었습니다. 현재 우선순위는 **실제 Tarkov 사용 결과를 기반으로 Scanner를 계속 검증·보정하는 것**입니다.

1. 실제 Tarkov Borderless 환경에서 다양한 아이템, 상세창 위치, 해상도·DPI 조합으로 스캔 사용
2. 결과를 성공 / 미인식 / 오인식 / detail detection / title ROI / OCR / catalog matcher / visual corroboration 실패로 분류
3. Item ID가 정확히 인식된 실제 표본에서 최고 상점가, flea `avg24hPrice`, `RequiredTotal`가 해당 Item ID와 끝까지 정확히 연결되는지 확인
4. 빠른 연속 스캔에서 이전 Item/result/image가 남거나 서로 섞이지 않는지 확인
5. 장시간 Scanner 실행에서 CPU / memory / UI responsiveness / OCR serialization 안정성 확인
6. 새 실제 실패 사례가 확보되면 해당 사례를 최소 재현 regression으로 먼저 고정한 뒤 실패 단계만 수정
7. live evidence 없이 global confidence/margin 완화, broad recognition heuristic 추가, unrelated Scanner 기능 추가 금지

개선 우선순위는 계속 **정확성 → 안정성 → 데이터 무결성 → 성능** 순서입니다. 오인식보다 미인식을 허용하는 fail-closed 원칙을 유지합니다.
