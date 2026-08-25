# Decision — 준현 헬퍼 제품 완성 및 유지보수 전환

기준일: 2026-08-26
상태: **ACCEPTED / PRODUCT COMPLETE / MAINTENANCE MODE**

## 결정

제품 사용자는 현재 준현 헬퍼가 의도했던 제품 기능을 갖춘 완성 상태에 도달했다고 최종 판단했다.

특히 마지막까지 개발·교정이 진행되던 Scanner는 v1.7.6에서 실사용 성능 문제가 해결되고 실제 Tarkov 사용에서도 충분한 속도와 동작을 확인했다. 이에 따라 Scanner 역시 기능 개발 단계에서 유지보수 단계로 전환한다.

현재 public stable인 **v1.7.6**을 이 결정 시점의 완성 기준선으로 사용한다.

## 이 결정의 의미

`제품 완성`은 프로그램을 영구 동결한다는 뜻이 아니다. 기본 개발 모드를 다음과 같이 변경한다.

- 기존 기능을 이유 없이 다시 설계하지 않는다.
- 측정 근거 없는 성능 튜닝이나 threshold 완화를 하지 않는다.
- 코드 미관만을 위한 위험한 대규모 refactor를 선제적으로 진행하지 않는다.
- 실사용에서 확인된 defect/regression은 증거를 확보해 영향받은 계층만 수정한다.
- Tarkov 데이터/UI 변경, Windows/.NET 호환성 변화, 외부 데이터 소스 변화에는 필요 시 대응한다.
- 사용자가 새로운 제품 요구사항을 명시적으로 결정하면 그때 다시 기능 개발을 시작한다.

## Scanner 유지보수 계약

Scanner는 다음 상태를 공식 기준으로 한다.

- continuous screen-based Tarkov UI recognition
- conservative identity matching / fail closed
- Mini Scanner presentation
- Display Test / one-shot scan
- correction / Ground Truth / regression support
- performance/support diagnostics
- v1.7.6의 current-cycle visual evidence reuse 및 font-provider hot-path protection 유지

새 Scanner 문제는 다음 순서로 처리한다.

1. exact support bundle 또는 diagnostic Case 확보
2. failure stage 측정
3. 원인이 확인된 stage만 수정
4. reviewed Ground Truth가 있으면 full-pipeline replay에서 `REGRESSION=0` 확인
5. 전체 Windows CI / publish / smoke / package 검증
6. 검증된 결과만 stable release에 반영

## 현재 남아 있는 비기능 항목

v1.7.6 공개 패키지의 `FIRST_RUN_KO.txt` 일부에 개발 중 사용했던 `진단 후보` 표현이 남아 있다.

이는 runtime, Scanner behavior, release identity 또는 asset integrity에 영향을 주지 않는 문구 문제다. 이미 게시된 stable asset은 immutable 원칙을 유지하며 덮어쓰지 않고, 이후 유지보수 patch가 있을 때 함께 수정한다.

## 향후 기본 상태

준현 헬퍼의 기본 프로젝트 상태는 다음과 같다.

```text
PRODUCT COMPLETE
PUBLIC STABLE
MAINTENANCE MODE
```

새 기능 개발은 자동으로 시작하지 않는다. 이후 작업의 기본 우선순위는 **안정성 유지, 회귀 수정, 데이터/플랫폼 호환성 유지**다.
