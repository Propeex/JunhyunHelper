# RELEASE 1.1.1 — Scanner 사용성 정리

기준일: 2026-08-21

상태: **`PUBLIC RELEASE / VERIFIED`**

## 목적

v1.1.1은 v1.1.0 Scanner의 제품 의미와 인식 파이프라인을 유지하면서 Scanner 탭과 Mini Scanner 조작을 실제 사용 중심으로 정리한 PATCH release입니다.

버전 근거: DEC-048 — 기존 기능 수정/보완/사용성 개선은 PATCH +1.

## 사용자 변경

### Scanner 탭

- 상단 Scanner 제목과 상시 설명문 제거
- 상단 bar 왼쪽 `스캐너`, `테스트` 버튼
- 상단 bar 오른쪽 `아이템 목록 최신화`
- bar 아래 표시 정보 체크박스
- 하단 `최근 인식 기록`
- Foundation preview 도구와 Mini Scanner 위치 편집/초기화 controls는 사용자 화면에서 제거

### 최근 인식 기록

각 실제 OCR/matcher 시도를 다음 정보로 정리합니다.

- 시각
- 스캐너/테스트 모드
- OCR text
- nearest official Item
- similarity
- top1/top2 margin
- 성공/보류
- 판단 이유

기존 `scanner.log(.1)`에서 최근 기록을 복원하므로 프로그램 재시작 뒤에도 최근 판정을 확인할 수 있습니다.

### Mini Scanner

- 별도 edit mode 없음
- 보이는 동안 직접 left-drag 이동
- drag 완료 후 atomic settings에 위치 저장
- Topmost / no-activate 유지
- always-drag 요구 때문에 Mini Scanner 영역의 click-through(`WS_EX_TRANSPARENT`)는 제거

## 변경하지 않는 것

- Tarkov window capture 전략
- detail geometry detector
- Windows ko-KR OCR
- conservative Item matcher threshold/margin
- full Item catalog 의미
- Item ID 이후 JunhyunHelper data bridge
- current needed = `RequiredTotal`
- scan-time network 금지
- game memory/DLL injection/packet interception 금지

## 호환성

```text
Desktop Version: 1.1.1
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db schema: v1
v1.1.0 → v1.1.1 mandatory Game Content update: none
v1.1.0 → v1.1.1 user.db migration: none
```

기존 Scanner settings/catalog, Profile, Quest, Inventory, Hideout, Map 설정, Ammo favorites는 유지합니다.

## release gate

- [x] final PR Windows Release build
- [x] full automated tests — 243 passed / 0 failed / 0 skipped
- [x] v1.1.1 ProductVersion/FIRST_RUN identity
- [x] rendered Scanner top bar: OFF/OFF + `아이템 목록 최신화`
- [x] recent-recognition empty state + readable decision sentence smoke
- [x] removed Foundation/position controls absent from rendered product UI
- [x] win-x64 self-contained single-file publish
- [x] package/dependency hygiene
- [x] actual published EXE startup
- [x] existing Product UI / Main Map / Factory / MiniMap smoke
- [x] graceful shutdown
- [x] exact release source SHA fixed
- [x] Draft ZIP + SHA256SUMS verification
- [x] Draft-downloaded EXE smoke
- [x] public/latest transition
- [x] public re-download hash/package/ProductVersion validation
- [x] public-downloaded EXE smoke
- [x] temporary release workflow cleanup
- [x] final SHA/hash/run record

## Live Tarkov

최신 Borderless Tarkov in-game E2E는 기존 DEC-051 정책대로 release blocker가 아닙니다. 사용자 실사용 검증에서 문제가 발견되면 `%LocalAppData%/JunhyunHelper/logs/scanner.log`와 Scanner 탭 최근 인식 기록을 함께 사용해 후속 PATCH로 보정합니다.

## 최종 공개 기록

```text
release: v1.1.1 PUBLIC / LATEST
release source SHA: 1316c25d4e90509bb9286064724b778510fa9301
release verification run: 32458154113
automated tests: 243 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.1.1-win-x64.zip
bytes: 80,237,511
SHA-256: db99ec44dc7ba55c6c4b238b62db41fa91fbc766e0428bbd491153a1e7d3a0e6
ProductVersion: 1.1.1+1316c25d4e90509bb9286064724b778510fa9301
Draft downloaded EXE smoke: SUCCESS
public downloaded EXE smoke: SUCCESS
latest stable verification: SUCCESS
```

공개 ZIP은 Draft 검증에 사용한 것과 동일한 SHA-256을 가졌고, public 전환 후 다시 다운로드한 패키지의 checksum/package/ProductVersion/FIRST_RUN 및 실제 실행 smoke를 모두 재검증했습니다.
