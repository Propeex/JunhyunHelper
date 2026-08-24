# 준현 헬퍼 v1.5.0

v1.5.0은 Scanner 연구 기능만 확장하는 버전이 아니라, 현재 준현 헬퍼를 실제 Tarkov 플레이에서 장시간 안정적으로 사용할 수 있도록 제품 완성도·데이터 신뢰성·Scanner 사용성을 함께 마감한 MINOR 릴리즈입니다.

## 주요 변경

- **Scanner mapped data 신뢰성 강화**: Item ID 확정 후 최고 상점가, 최고가 상인, 플리마켓 24시간 평균가, 슬롯 수, 상인/플리 칸당 가격, 현재 필요한 수량을 일관되게 표시합니다.
- **데이터 업데이트 통합**: 상단 `데이터 업데이트` 한 번으로 일반 Tarkov 데이터와 Scanner item/market catalog를 함께 갱신합니다. Scanner 갱신만 실패하면 정상 일반 데이터를 rollback하지 않고 기존 healthy Scanner cache를 유지합니다.
- **Quest `확인 필요` 최신 데이터 감사**: 최신 live data에서 task-pool 구조를 다시 검증하고 GameMode까지 포함한 감사된 조건에서만 안전하게 추론합니다. 구조가 다르면 값을 임의 생성하지 않고 fail closed합니다.
- **사용자 OCR 치환**: 반복 오인식을 사용자가 직접 등록·삭제·ON/OFF·초기화할 수 있습니다. raw OCR은 진단 증거로 보존되고 치환은 catalog matching 전에 한 번만 적용됩니다.
- **후보 선택형 Ground Truth 교정**: 상세창, 빨간 X, 돋보기, item-name ROI를 detector 후보에서 선택하는 흐름을 기본으로 하고, 정답 후보가 없으면 직접 rectangle 지정과 `없음` 기록을 지원합니다.
- **Scanner latency telemetry**: capture, rectangle proposal, semantic header, normal/deep OCR, visual recovery, catalog matching, presentation, end-to-end latency를 계측합니다.
- **정확도 보존 성능 최적화**: 같은 scan-cycle 안에서 픽셀 단위로 완전히 동일한 OCR bitmap만 재사용합니다. 프레임 간 OCR cache는 사용하지 않습니다.
- **연속 Scanner 안정화**: 검증된 상세창의 제목 glyph identity가 유지되면 미세한 배경 픽셀 변화나 일시적 OCR 흔들림으로 Mini Scanner가 깜빡이지 않도록 했습니다. 다른 제목이나 identity evidence 변경 시 기존 결과를 즉시 폐기하는 원칙은 유지합니다.
- **진단 데이터 retention**: 사용자-reviewed Ground Truth는 자동 삭제하지 않습니다. 자동 미검토 Case만 30일·300건·512MiB 상한과 최근 2시간 보호창으로 관리하며 Scanner/startup log도 bounded rotation합니다.
- **Scanner UI 정리**: 일반 화면은 Scanner ON/OFF, 1회 스캔, 현재 결과 교정, 상태, 최근 인식에 집중합니다. 설정과 개발·복구 기능은 각각 `설정`, `고급 / 진단`에 분리했습니다.
- **빠른 현재 결과 교정**: Mini Scanner에서 우클릭 → `현재 결과 교정`으로 방금 본 오인식을 바로 Ground Truth 교정 흐름으로 보낼 수 있습니다.
- **전체 UI consistency audit**: Main / Quest / Hideout / Items / Ammo / Map / Scanner 및 주요 설정·진단 창의 spacing, clipping, scroll, hierarchy, 상태 표현을 점검하고 실제 2-pane 구조와 맞지 않던 Main 최소 폭도 교정했습니다.

## 유지한 Scanner 안전 계약

- false positive보다 miss 선호
- rectangle geometry는 proposal이며 identity proof가 아님
- `HEADER_FRAME_LOCKED >= 0.68`
- magnifier + red close-X 필요
- structural floor `0.34`
- continuous candidate max `8`
- one-shot candidate max `12`
- current official Korean Tarkov item catalog가 identity authority
- production OCR field는 `item_name` 하나
- 가격/슬롯/필요 수량은 Item ID 확정 후 mapped data
- scan-time network 금지
- game memory read / DLL injection / packet interception 금지
- 자동 전역 `r/0/한글` 강제 substitution table 미사용

## 배포

- Windows x64
- .NET 10 self-contained single-file
- 별도 .NET Runtime 설치 불필요
- installer 없음

정식 release asset은 `Junhyun-Helper-v1.5.0-win-x64.zip`과 `SHA256SUMS.txt`로 제공하며, 공개 후 별도 public redownload / SHA-256 / package layout / ProductVersion / startup + Product UI + Map + Scanner smoke 검증을 수행합니다.
