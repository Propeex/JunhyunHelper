# 준현 헬퍼 v1.7.0

v1.7.0은 현재 준현 헬퍼의 기능을 안정적으로 오래 사용할 수 있도록 데이터 갱신, Scanner, 저장·복구, 동시 실행, 배포 검증을 전반적으로 강화한 Product Completion release입니다.

## 주요 변경

- **Scanner 인식 기록에서 바로 교정**
  - 인식 기록과 실제 저장된 진단 Case가 정확히 연결된 경우 해당 결과를 바로 열어 교정할 수 있습니다.
  - 다른 프레임이나 결과를 추정해서 연결하지 않습니다.

- **Scanner 개발 자료 ZIP 내보내기**
  - 기존 Ground Truth와 Scanner 로그를 하나의 개발 분석용 ZIP으로 내보낼 수 있습니다.
  - reviewed Ground Truth는 자동 로그 정리 대상이 아닙니다.

- **게임 데이터 업데이트 신뢰성 강화**
  - 요청별 timeout과 제한된 retry를 적용했습니다.
  - 아이템·퀘스트·상인·지도·은신처·탄약·에디션이 비정상적으로 대량 누락된 데이터는 적용하지 않습니다.
  - 퀘스트 선행조건, 위치, 은신처 필요 아이템, 탄약 획득 조건, 한국어 이름, 아이콘·wiki 같은 내부/표시 데이터의 대량 소실도 기존 정상 데이터와 비교해 차단합니다.
  - 새 데이터는 candidate DB로 저장한 뒤 실제 파일을 다시 읽어 검증하고 나서야 active 데이터가 됩니다.
  - 업데이트가 실패·취소되거나 저장 데이터가 손상되면 기존 정상 데이터를 유지합니다.
  - 동시에 여러 업데이트가 실행되어 서로의 candidate/active 파일을 덮어쓰지 않도록 전체 update transaction을 직렬화했습니다.

- **Scanner 가격/표시 데이터 보호**
  - 최고 상점가, Flea 평균가, slot 정보가 비정상적으로 대량 사라진 Scanner candidate cache는 적용하지 않습니다.
  - Scanner를 아직 켜지 않은 상태에서도 같은 게임 모드의 기존 정상 디스크 cache를 기준으로 보호합니다.
  - Item ID가 확정된 후 이름, 아이콘, wiki, 최고 상점가와 상인, Flea 평균가, slot당 가격, 현재 필요 개수는 모두 동일한 Tarkov Item ID를 기준으로 결합합니다.

- **기존 v1.6.1 안정화 내용 유지**
  - Scanner Advanced 창의 DPI/글꼴 clipping 방지
  - Scanner runtime 로그 7일 자동 정리와 용량 제한
  - 정상 최신 Scanner cache를 실패로 잘못 안내하던 상태 판정 수정
  - Scanner 필수 identity 데이터와 보조 localization/trader 표시 데이터의 failure boundary 분리

## Scanner 인식 안전 기준

실사용 Ground Truth 없이 인식 기준을 임의로 완화하지 않았습니다.

- structural floor: `0.34`
- HEADER_FRAME_LOCKED floor: `0.68`
- continuous candidate cap: `8`
- one-shot candidate cap: `12`
- false positive보다 miss 우선
- scan-time network 없음
- 게임 메모리 읽기, DLL injection, packet interception 없음

Scanner의 실제 detection/OCR/matcher 정확도 튜닝은 이후 reviewed live Ground Truth를 근거로 계속 진행합니다.
