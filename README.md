# 준현 헬퍼

`JunhyunHelper`는 Escape from Tarkov 플레이를 지원하는 Windows 데스크톱 헬퍼 **준현 헬퍼**의 공식 저장소입니다.

## 현재 단계

**Phase 2B — 핵심 Desktop 흐름 구현 및 실사용 피드백 반복 개선**

현재 실제 WPF Desktop에서 다음 핵심 흐름이 연결되어 있습니다.

- 게임 모드별 Profile 관리
- Quest 진행/잠김/사용 불가/완료 계산
- residual 판정 불가 Quest의 제품상 `진행 중` fallback
- Quest 제출 Item card/icon과 선행 Quest 링크
- Hideout 현재 레벨 관리
- 미래 Quest + 미래 Hideout 기준 Needed Items 계산
- FIR / Non-FIR 보유량 및 안전한 cleanup 계산
- flexible hand-in 그룹 계산 및 별도 보기
- Tarkov category metadata 기반 Item 종류 필터
- Quest ↔ Item / 선행 Quest stable-ID 상호 이동
- Ammo 성능/수급처/Armor Class 1~6 비교
- 온라인 Game Content 안전 업데이트와 실제 단계 기반 progress 표시
- WebP 등을 PNG로 normalize하는 Item / Hideout / Ammo / Quest Item 이미지 cache
- 전체 dark control + rounded ScrollBar theme

Map과 Scanner의 실제 기능은 후속 범위이며, 현재 상단 내비게이션에 `준비 중` placeholder가 있습니다.

## 가장 중요한 원칙

1. **사용자의 의도가 제품의 최상위 기준입니다.**
2. 기존 `Propeex/Tarkov-Helper`는 참고 자료일 뿐 새 제품의 사양이 아닙니다.
3. 일반적인 Tarkov 패치 데이터 변경 때마다 GPT가 데이터를 다시 수작업으로 변환하지 않습니다.
4. 온라인 데이터는 프로그램이 검증·canonical 변환·DB 재구축을 반복할 수 있어야 합니다.
5. Game Content와 User Progress를 분리합니다.
6. 잘못된 새 데이터로 정상 데이터를 덮어쓰는 것보다 업데이트 실패가 낫습니다.
7. 안전한 cleanup을 증명할 수 없으면 보수적으로 보호합니다.
8. 외부 보조 데이터의 의미를 확신할 수 없으면 값을 추정하지 않습니다.
9. 대화가 바뀌어도 저장소 문서만 읽고 작업을 이어갈 수 있어야 합니다.

## 데이터 구조

기본 사용자 데이터 루트:

```text
%LocalAppData%/JunhyunHelper
```

주요 저장:

```text
user.db
content/<game-mode>/content.db
content/<game-mode>/content.candidate.db
content/<game-mode>/content.previous.db
image-cache/
```

1차 Game Content 원천은 `json.tarkov.dev`입니다.

허용된 보조 원천은 역할을 제한합니다.

- TarkovTracker overlay: edition rule
- Escape from Tarkov Wiki Ballistics: Ammo Armor Class 1~6 effectiveness

## 기술 스택

- .NET 10
- C#
- WPF
- SQLite
- SkiaSharp — source image decode/PNG normalization
- Core / Infrastructure / Application / Desktop 계층 분리

## 새 개발자가 가장 먼저 읽을 문서

1. [`AGENTS.md`](AGENTS.md) — 개발자/AI 작업 규약
2. [`docs/STATE.md`](docs/STATE.md) — 현재 상태와 다음 작업
3. [`docs/PRODUCT.md`](docs/PRODUCT.md) — 확정 제품 요구사항
4. [`docs/DECISIONS.md`](docs/DECISIONS.md) — 장기 결정 이력
5. [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 현재 기술 구조
6. [`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md) — 개발/검증 절차
7. [`docs/REFERENCE_POLICY.md`](docs/REFERENCE_POLICY.md) — 기존 구현 참고 규칙
8. [`docs/SECOND_USABILITY_PASS.md`](docs/SECOND_USABILITY_PASS.md) — 2차 실사용 피드백 설계/검증
9. [`docs/BALLISTICS_EFFECTIVENESS_ANALYSIS.md`](docs/BALLISTICS_EFFECTIVENESS_ANALYSIS.md) — Ammo Class 1~6 source 조사

## 현재 개발 우선순위

첫 실사용 피드백 1~13과 2차 실사용 피드백 1~7은 구현·자동 검증·main 병합 완료되었습니다.

현재 최우선은 **새 Windows x64 빌드를 실제 사용자 환경에서 다시 테스트하는 것**입니다.

확인 대상:

- 기존 진행 데이터 보존 + content v2 자동 재구축
- 아이콘 표시
- ScrollBar 모양/조작
- Item 종류 분류
- 유동 제출 분리
- Quest 제출 Item UI
- Quest ↔ Item / 선행 Quest 이동

새 요구가 없다면 이후 큰 기능은 Map 실제 기능의 데이터 공급원/사용 경험 정의와 Scanner 요구사항 정의입니다.

상세 상태는 [`docs/STATE.md`](docs/STATE.md)를 기준으로 합니다.
