# 준현 헬퍼 v1.6.0 릴리즈 노트

## Scanner 사용 화면 정리

Scanner 탭의 일반 화면을 실제 사용 흐름에 맞게 단순화했습니다.

상단은 다음 세 동작만 중심에 둡니다.

- 스캐너 ON/OFF
- 설정
- 고급

하단은 아이템 검색과 Scanner 인식 로그를 동시에 볼 수 있는 좌우 2분할 구조입니다.

기존 1회 인게임 스캔, 1회 테스트 스캔, Scanner ON/OFF 전역 단축키는 그대로 유지됩니다.

## Scanner 아이템 검색

Scanner 탭에서 Tarkov 아이템을 바로 검색할 수 있습니다.

검색은 이미 내려받은 전체 아이템 catalog를 사용하므로 검색할 때 별도 network request를 만들지 않습니다.

검색 결과에는 아이콘과 아이템명이 표시되며, 아이템을 선택하면 다음 정보를 확인할 수 있습니다.

- 아이콘
- 공식 아이템명
- Tarkov Wiki 링크
- 플리마켓 24시간 평균가
- 최고 상인 판매가와 가능한 경우 해당 상인 이름
- 현재 필요한 개수

현재 필요 개수의 의미는 기존과 동일하게 퀘스트/은신처 계획에서 앞으로 필요한 총량입니다.

## Mini Scanner 설정 개선

Mini Scanner의 아이콘과 아이템명은 항상 상단에 표시됩니다.

다음 다섯 정보는 사용자가 표시 여부와 순서를 정할 수 있습니다.

- 상인 판매가
- 플리마켓 평균가
- 상인 가격/칸
- 플리 가격/칸
- 필요 개수

순서는 설정 파일에 저장됩니다.

최고 상인 판매가는 가능한 경우 `Therapist 42,000₽`처럼 실제 최고가 상인 이름과 가격을 함께 표시합니다.

## Scanner 설정 schema v6

Scanner display settings를 schema v6으로 올렸습니다.

기존 설정은 자동으로 마이그레이션되며 다음 값은 가능한 한 그대로 유지합니다.

- Scanner ON/OFF 상태
- 3종 전역 단축키
- Mini Scanner 정보 표시 설정
- Mini Scanner 위치와 글자 크기
- 사용자 OCR substitution 규칙

v6부터 아이템 아이콘과 이름은 Mini Scanner identity header이므로 숨길 수 없습니다.

## 교정 화면 개선

큰 Tarkov 스크린샷도 교정 창 안에서 전체를 볼 수 있도록 자동 축소합니다.

화면에 보이는 배율과 저장되는 좌표는 분리되어 있어 Ground Truth는 항상 원본 이미지 좌표계로 기록됩니다.

상세보기 창, 닫기 X, 돋보기, 아이템명 ROI는 기존 드롭다운 대신 이미지 위 후보 사각형을 직접 클릭해 선택할 수 있습니다.

후보가 정답을 포함하지 않으면 직접 영역을 지정할 수 있고, 실제 대상이 없어야 하면 `없음`을 명시적으로 기록할 수 있습니다.

## 기존 교정 데이터 재편집

교정 데이터 관리에서 저장된 Case를 다시 열 수 있습니다.

기존 `case.json`, `full.png`, `candidate_selection.json`과 Ground Truth를 복원해 같은 편집기에서 다시 검토하고 수정할 수 있습니다.

재저장은 동일한 Case ID를 유지합니다.

읽기 실패나 불완전한 Case는 기존 데이터를 지우지 않고 보존합니다.

## 배포 ZIP 구조 변경

사용자 배포 파일 이름을 버전 번호와 분리했습니다.

- ZIP: `준현 헬퍼.zip`
- 압축 내부 최상위 폴더: `준현 헬퍼/`
- 실행 파일: `준현 헬퍼/준현 헬퍼.exe`

새 버전이 나올 때마다 압축 해제 폴더 이름이 달라지지 않습니다.

CI도 실제 ZIP을 생성해 이 구조를 검증합니다.

## 인식 안전성

이번 릴리즈는 Scanner UI/교정 workflow 개선이 목적이며 identity threshold를 완화하지 않았습니다.

다음 안전 계약은 그대로 유지됩니다.

- false positive보다 miss 선호
- structural floor 0.34
- HEADER_FRAME_LOCKED >= 0.68
- valid magnifier + red close-X 필수
- continuous candidate cap 8
- one-shot candidate cap 12
- current official Tarkov item catalog가 identity authority
- scan-time network 금지
- game memory read 금지
- DLL injection 금지
- packet interception 금지
- cross-frame OCR cache 금지
