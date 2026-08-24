# VERSIONING — 준현 헬퍼 버전 규칙

상태: `CONFIRMED`

기준일: 2026-08-24

이 문서는 v1.0.0 정식 릴리즈 이후 준현 헬퍼의 버전 번호를 결정하는 공식 규칙입니다.

## 1. 형식

정식 버전은 `MAJOR.MINOR.PATCH` 세 자리 숫자를 사용합니다.

예: `1.0.0`, `1.0.1`, `1.1.0`.

## 2. 새 기능 추가

사용자가 사용할 수 있는 **새 기능을 추가**하면 두 번째 자리(`MINOR`)를 1 올리고 세 번째 자리(`PATCH`)는 0으로 되돌립니다.

예:

- `1.0.0`에서 Scanner 실제 기능 추가 → `1.1.0`
- `1.0.4`에서 새 기능 추가 → `1.1.0`
- `1.1.3`에서 또 다른 새 기능 추가 → `1.2.0`

여기서 새 기능은 기존 기능의 품질 개선이 아니라 사용자가 새롭게 할 수 있는 일 또는 새 제품 능력을 의미합니다.

## 3. 기존 기능 수정·보완·변경

기존 기능의 동작 수정, 버그 수정, 안정성 보강, 성능 개선, 내부 최적화, UI/UX 보완처럼 **새 기능을 추가하지 않는 변경**은 세 번째 자리(`PATCH`)를 1 올립니다.

예:

- `1.0.0`에서 Quest 기능 수정 → `1.0.1`
- `1.0.1`에서 안정성 개선 → `1.0.2`
- `1.1.0`에서 기존 Map 기능 보완 → `1.1.1`

## 4. 혼합 변경

한 릴리즈에 새 기능 추가와 기존 기능 수정이 함께 들어가면 새 기능 추가 규칙이 우선합니다.

예:

- 현재 `1.0.1`
- Scanner 기능 추가 + Quest 버그 수정
- 결과 버전: `1.1.0`

## 5. v1.0.0의 의미

`1.0.0`은 0.x 개발 버전을 종료하고 현재 확정 기능 집합을 **정식 안정판**으로 선언하는 기준점입니다.

v1.0.0 승격 작업 자체에서는 새 제품 기능을 추가하지 않습니다. v0.1.14의 사용자 동작을 보존하면서 내부 코드 정리, 신뢰성/성능 하드닝, 문서화, 검증 체계 정리만 수행합니다. Scanner는 기존 제품 계약대로 visible `준비 중` placeholder이며 실제 Scanner 기능은 v1.0.0 범위에 포함하지 않습니다.

## 6. MAJOR 자리

사용자가 이번에 확정한 규칙은 v1 계열의 새 기능(`MINOR`)과 수정·보완(`PATCH`)에 대한 규칙입니다. 이후 `MAJOR` 증가 조건이 실제로 필요해질 때 제품 의미와 호환성 영향을 기준으로 별도 확정합니다. 개발자가 임의로 MAJOR를 올리지 않습니다.

## 7. 릴리즈 체크

정식 릴리즈 전에 다음을 확인합니다.

1. 변경 내용이 새 기능인지 기존 기능 수정/보완인지 분류합니다.
2. 이 문서의 규칙으로 목표 버전을 결정합니다.
3. Desktop project version, EXE ProductVersion, 배포 안내문, release tag, release notes가 같은 버전을 가리키는지 검증합니다.
4. 자동 업데이트가 사용하는 GitHub stable release도 같은 버전인지 검증합니다.
5. v1.6.0부터 사용자 배포 ZIP과 압축 해제 폴더 이름은 버전 식별자가 아니라 stable product name을 사용합니다.

```text
준현 헬퍼.zip
└─ 준현 헬퍼/
```

따라서 ZIP 파일명에 버전 문자열이 없다는 이유로 version mismatch로 판단하지 않습니다. 실제 버전 identity는 EXE ProductVersion, tag, release metadata를 기준으로 검증합니다.

## 8. 최근 적용 사례

- v1.3.0 — recognition image export / one-shot test / 3종 global hotkey 추가 → **MINOR**
- v1.3.1 — 기존 Scanner title recognition 보완 → **PATCH**
- v1.3.2 — 추가 live OCR evidence 기반 안정성·정확성 보완 → **PATCH**
- v1.3.3 — 12개 실제 상세창에서 재확인된 header/title-start 회귀 수정 → **PATCH**
- v1.3.4 — 실제 live recognition/diagnostics 안정성 보완 → **PATCH**
- v1.3.5 — 상세창 tracking/diagnostics 회귀 수정 → **PATCH**
- v1.4.0 — 사용자 Scanner 교정, Ground Truth dataset 관리, ZIP export, full-pipeline 회귀 테스트 추가 → **MINOR**
- v1.4.1 — 실제 Tarkov Ground Truth 기반 상세보기 header lock 실패 수정 및 1회 스캔 후보 탐색 보완 → **PATCH**
- v1.4.2 — 실제 v1.4.1 Ground Truth 기반 contained detail-window 복구, 보수적 OCR matcher 복구, Scanner 단축키 창 clipping 수정 → **PATCH**
- v1.4.3 — 상세창 rectangle proposal/semantic validation 경계 재구성 및 current-catalog 기반 OCR 문자·기호 검증/unknown-glyph 복구 개선 → **PATCH**
- v1.4.4 — 실제 v1.4.3 Ground Truth 기반 짧은 title OCR tight-view 재시도 및 r-symbol/o-O matcher evidence 고정 → **PATCH**
- v1.5.0 — mapped market data, unified Scanner catalog update, user OCR substitutions, candidate-first GT, latency telemetry, stabilization/retention/UI finishing 추가 → **MINOR**
- v1.6.0 — Scanner local item search, Mini Scanner 정보 순서 설정, 저장된 GT Case 재교정, 이미지 직접 candidate 선택, stable release package naming 추가 → **MINOR**
