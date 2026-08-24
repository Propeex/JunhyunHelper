# Decision — v1.5.0 Product Finishing Pass

기준일: 2026-08-24
상태: APPROVED / IMPLEMENTATION IN PROGRESS

## 목적

v1.5.0은 Scanner 정확도 연구만 계속하는 릴리즈가 아니라, 현재 준현 헬퍼를 실제 Tarkov 플레이에서 장시간 안정적으로 쓰는 제품으로 마감하는 MINOR 릴리즈다.

사용자가 2026-08-24에 아래 범위를 전부 승인했다. 기존 기능 축소가 목적이 아니며, 일상 사용 화면은 단순화하고 개발·진단 기능은 고급 영역으로 이동한다.

## 승인 범위

### 1. 사용자 OCR 문자 치환

- Scanner 설정에 사용자 정의 문자/문자열 치환 규칙을 제공한다.
- 대표 실사용 목적은 WinRT OCR의 `「` → `r` 같은 반복 오인식 보정이다.
- raw OCR 원문은 forensic evidence로 항상 보존한다.
- 사용자 치환은 raw OCR 이후, catalog normalization/matching 이전에 한 번만 적용한다.
- 재귀/순환 치환은 허용하지 않는다.
- 규칙은 Scanner 전역 설정으로 저장하며 개별 ON/OFF, 추가, 삭제, 초기화를 지원한다.
- 결과 진단에는 raw OCR / user-substituted OCR / normalized / matched 결과를 구분해 기록한다.
- 사용자 치환 기능이 자동 알고리즘의 global substitution table을 의미하지는 않는다. 기본값은 안전하게 비어 있거나 명시적으로 검증된 최소 preset만 사용한다.

### 2. Scanner 카탈로그 업데이트 통합

- Scanner 카탈로그/market refresh를 일반 Game Data 업데이트 흐름에 포함한다.
- 사용자가 별도의 Scanner catalog 최신화 절차를 이해하거나 반복할 필요가 없게 한다.
- Scanner 화면의 전용 catalog refresh는 일반 사용 UI에서 제거하고, 필요하면 고급/복구 기능으로만 남긴다.
- 내부 실패 시 기존 healthy cache를 유지하고 부분 실패를 명확한 상태로 보고한다.

### 3. Scanner market presentation 신뢰성

- Item ID가 올바르게 확정됐는데 최고 상점가/칸당 가격 등이 비어 있는 현상은 기능 추가가 아니라 제품 버그로 취급한다.
- 실제 source payload → ScannerCatalogService parse → ScannerCatalogItem → presentation snapshot → Mini Scanner/Scanner UI 전체 경로를 검증한다.
- 최고 상점가, flea avg24hPrice, slot count, trader/flea price-per-slot, RequiredTotal이 가능한 경우 전부 표시돼야 한다.
- market/dimension 일부 누락은 Item identity 실패로 승격하지 않는다.
- 가능하고 데이터가 신뢰 가능하면 최고가 판매 상인명도 표시한다.

### 4. Quest `확인 필요` 재감사

- `확인 필요` 증가를 UI 노이즈로 덮지 않는다.
- 최신 Tarkov data 기준으로 unresolved/unsupported availability를 원인별로 전수 분류한다.
- task-pool, profile-variable, prerequisite, special-trader, dialogue/edition gate, 새 API 조건, 실제 source 누락 등을 구분한다.
- importer/evaluator에서 안전하게 해석 가능한 조건은 구현하고 회귀 테스트한다.
- 실제로 알 수 없는 조건은 fail closed로 유지한다.
- UI에는 가능하면 `확인 필요 · <짧은 이유>` 형태의 원인을 표시한다.

### 5. 후보 선택형 Ground Truth 교정

기본 교정 UX를 수동 rectangle drawing 중심에서 detector evidence 선택 중심으로 개선한다.

권장 단계:

1. 상세보기 rectangle 후보 선택
2. red close-X 후보 선택
3. magnifier 후보 선택
4. item-name ROI 후보 선택
5. 정답 item/text 지정
6. 저장

원칙:

- 후보 선택이 기본 경로다.
- 후보에 정답이 없을 때를 위해 직접 rectangle 지정은 반드시 fallback으로 유지한다.
- `없음` 선택도 보존해 detector가 해당 semantic object를 생성하지 못했음을 Ground Truth로 남긴다.
- 선택된 candidate ID/rank/score/geometry와 정답을 함께 저장한다.
- 이렇게 얻은 데이터로 proposal recall, ranking loss, semantic anchor miss, ROI miss를 분리할 수 있어야 한다.

### 6. Scanner 일반 UI / 고급·진단 UI 분리

일상 Scanner 화면은 핵심 사용 흐름만 남긴다.

- Scanner ON/OFF
- 최근 인식 결과
- 필요한 개수
- 최고 상점가 / 칸당 가격
- flea 평균가 / 칸당 가격
- 1회 스캔
- 설정

아래는 고급/진단 영역으로 이동하거나 묶는다.

- Ground Truth/교정 데이터 관리
- 인식 이미지/Case browser
- regression 실행
- diagnostics export
- 상세 로그
- Scanner data 복구/강제 refresh

기능 자체를 삭제하는 것이 아니라 surface complexity를 낮춘다.

### 7. 연속 Scanner 결과 안정화

- 같은 상세창을 보고 있는 동안 일시적 1-frame OCR miss로 결과가 깜빡이지 않게 한다.
- 동일 대상이라는 증거가 유지되고 명확한 새 Item/창 닫힘 증거가 없으면 직전 trusted result를 짧게 유지할 수 있다.
- 다른 상세창으로 전환되거나 창이 닫히면 stale result를 즉시 해제해야 한다.
- false-positive보다 miss를 선호하는 기존 원칙을 깨지 않는다.

### 8. 성능 계측과 최적화

정확도 threshold를 낮추는 방식이 아니라 stage latency를 먼저 계측한다.

측정 대상:

- capture
- rectangle proposal
- semantic header validation
- OCR normal/deep
- visual recovery
- catalog matching/recovery
- presentation
- end-to-end

계측 후 아래를 우선 검토한다.

- 동일 frame/candidate 중복 처리 감소
- OCR/deep OCR 불필요 호출 감소
- 이전 trusted header/candidate의 안전한 재사용
- bitmap copy/convert 감소
- expensive visual/catalog recovery 조기 종료

정확도와 fail-closed 계약이 성능보다 우선한다.

### 9. 장시간 실행 / 진단 데이터 관리

- 사용자-reviewed Ground Truth는 자동 삭제하지 않는다.
- 자동 diagnostic failure Case와 일반 log/temp artifact는 별도 retention/rotation 정책을 둔다.
- 기본 정책은 충분한 디버깅 표본을 보존하면서 무제한 디스크 증가를 막는 것이다.
- 자동 삭제 대상과 영구 보존 대상은 명확히 분리한다.

### 10. 교정 접근성

- Mini Scanner 또는 가까운 사용자 동선에서 `현재 결과 교정`에 빠르게 접근할 수 있게 한다.
- 잘못 읽힌 직후 몇 초 안에 해당 Case를 reviewed Ground Truth로 남기는 흐름을 목표로 한다.

### 11. 프로그램 전체 UI 일관성 최종 점검

- Main / Quest / Hideout / Items / Ammo / Map / Scanner / settings/dialog 전체에서 spacing, button hierarchy, clipping, scroll, status wording, empty/error state를 다시 점검한다.
- 새 기능을 무분별하게 추가하지 않는다.
- 일상 사용자가 개발/진단 개념을 몰라도 핵심 기능을 사용할 수 있는 방향을 우선한다.

## 구현 우선순위

1. Scanner price/mapped-data 누락 원인 수정
2. Quest `확인 필요` 전수 감사 및 안전한 해석 보완
3. unified Game Data update + Scanner catalog/market refresh
4. 사용자 OCR 치환 설정
5. 후보 선택형 Ground Truth 교정 + 수동 fallback
6. Scanner latency telemetry 및 정확도 보존 최적화
7. continuous result stabilizer
8. diagnostics/log/temp retention
9. Scanner 일반/고급 UI 재구성 + 빠른 현재 결과 교정
10. 전체 프로그램 UI consistency audit
11. 전체 자동 테스트, Windows publish, Product UI/Map/Scanner smoke, graceful shutdown
12. v1.5.0 public release + independent public re-download verification + housekeeping

## 변경하지 않는 Scanner 핵심 계약

- false positive보다 miss 선호
- rectangle geometry는 proposal이며 identity proof가 아님
- `HEADER_FRAME_LOCKED >= 0.68` + magnifier + close-X trusted gate 유지
- structural floor `0.34`
- one-shot max 12 / continuous max 8는 실제 성능/GT evidence 없이 무작정 변경하지 않음
- current official Korean item catalog가 identity authority
- scan-time network 금지
- game memory read / DLL injection / packet interception 금지
- production OCR field는 item_name 하나
- price/flea/slots/needed는 Item ID 이후 mapped_data

## 버전

사용자에게 새로 제공되는 설정/교정 UX 및 제품 동작이 포함되므로 SemVer MINOR로 분류한다.

목표 릴리즈: **v1.5.0**
