# 준현 헬퍼 v1.7.0 Release Record

상태: `PUBLIC RELEASE VERIFIED`

기준일: 2026-08-25

## 릴리즈 목적

v1.7.0은 새 기능 확장보다 현재 제품의 신뢰 경계를 강화하는 Product Completion release다. Scanner quick-correction capability를 추가하고, Data Update·Scanner catalog·persistence·concurrency·release 검증을 fail-closed 방향으로 보강한다.

## 사용자 체감 변경

- Scanner 인식 기록과 실제 diagnostic Case가 연결되어 있을 때 해당 결과에서 바로 교정 화면으로 진입할 수 있다.
- 기존 Scanner Ground Truth/로그 데이터는 새 subsystem을 만들지 않고 기존 export pipeline을 통해 개발 분석용 ZIP으로 내보낸다.
- 일반 게임 데이터 업데이트는 요청별 timeout과 bounded retry를 사용하고, 부분/손상 payload가 기존 정상 active snapshot을 대체하지 못한다.
- Scanner 가격 cache는 상점가·Flea·slot coverage가 비정상적으로 대량 소실되는 candidate를 거부한다.
- Item ID 확정 이후 이름/아이콘/wiki/상인/가격/slot/필요 개수는 동일 Tarkov item ID를 기준으로 결합된다.
- v1.6.1에서 보완한 Scanner Advanced DPI clipping 방지와 Scanner runtime 로그 7일 자동 정리를 유지한다. reviewed Ground Truth는 자동 삭제하지 않는다.

## Data Update 신뢰 계약

```text
remote sources
→ parse/import
→ integrity + cross-reference validation
→ baseline completeness comparison
→ candidate persistence
→ persisted candidate read-back validation
→ atomic activation
→ final active load verification
```

- transient HTTP/network/timeout은 bounded retry
- permanent HTTP failure는 fail-fast
- malformed/truncated JSON은 제한적으로 재시도 후 fail-closed
- critical domain과 nested relationship의 비정상 대량 축소 차단
- 한국어 localization, icon/wiki/image coverage의 비정상 대량 소실 차단
- 동시 Data Update는 전체 transaction boundary에서 직렬화
- 실패·취소 시 candidate를 active로 승격하지 않음

## Scanner 신뢰 계약

이번 release에서 live recognition threshold/candidate budget은 변경하지 않는다.

```text
structural floor = 0.34
HEADER_FRAME_LOCKED = 0.68
continuous candidate cap = 8
one-shot candidate cap = 12
```

계속 유지되는 원칙:

- false positive보다 miss 선호
- production OCR은 item-name field만 사용
- Item ID 확정 전 mapped price/needed data를 identity 증거로 사용하지 않음
- scan-time network 없음
- game memory read / DLL injection / packet interception 없음
- automatic global OCR substitution 없음
- cross-frame OCR cache 없음

## 자동/공개 검증 — FINAL

- `348 passed / 0 failed / 0 skipped`
- exact release source/tag: `56e12342e3490fd0defa5f327a03d20d4f32b3a6`
- ProductVersion `1.7.0+56e12342e3490fd0defa5f327a03d20d4f32b3a6`
- FIRST_RUN `준현 헬퍼 v1.7.0 — Windows x64`
- rendered Product UI / Scanner / Scanner Advanced / Quest sidebar smoke 통과
- Main Map / Factory / MiniMap smoke 통과
- graceful shutdown + clean portable root 통과
- stable package layout/checksum 검증 통과

```text
tag: v1.7.0
stable asset: Junhyun-Helper.zip
bytes: 80,443,318
SHA-256: 1c640c80bf6113176b885a47e19478666e27dbf584f872d1a8396886334f3418
checksum asset: SHA256SUMS.txt
public proof run: 32745399476
```

공개 asset을 인증 없는 GitHub URL에서 다시 내려받아 byte size, SHA-256 manifest, ZIP root/layout, forbidden debug/legacy payload 부재, exact ProductVersion, FIRST_RUN, 실제 rendered Product UI/Map smoke, normal close/clean portable root까지 재검증했다.

v1.7.0은 **PUBLIC RELEASE VERIFIED**이며 이미 공개된 동일 version asset은 immutable로 유지한다. 이후 `main`의 문서/housekeeping commit은 v1.7.0 제품 source가 아니다.
