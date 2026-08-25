# RELEASE — v1.7.2

상태: `PUBLIC / VERIFIED`

기준일: 2026-08-25

## Release identity

```text
version: 1.7.2
tag: v1.7.2
exact product source: 8775feba23a2c9ecc6326626527cdfd54f4f0414
public latest: v1.7.2
release id: 376353887
```

## Verification chain

PR #180 final candidate에서 build/test/publish/smoke를 통과한 뒤 `main`에 squash 병합했다.

Exact main source CI:

```text
run: 32842508995
source: 8775feba23a2c9ecc6326626527cdfd54f4f0414
build: SUCCESS
automated tests: 362 passed / 0 failed / 0 skipped
Windows x64 publish: SUCCESS
Product UI / Scanner / Mini Scanner smoke: SUCCESS
Map / Factory / MiniMap smoke: SUCCESS
graceful shutdown: SUCCESS
release package verification: SUCCESS
artifact upload: SUCCESS
```

Release workflow:

```text
run: 32842783940
verified main source checkout: SUCCESS
verified CI artifact download: SUCCESS
stable release verification/publication: SUCCESS
```

## Public package

```text
asset: Junhyun-Helper.zip
bytes: 80,444,391
SHA-256: 81d8e6a82db0f4b33ebbdd2bf7f455c1d92ffc2f8b6015f6ba6190e616be1fc0
checksum asset: SHA256SUMS.txt
release draft: false
release prerelease: false
```

Release runner가 main CI artifact에서 `Junhyun-Helper.zip`의 manifest hash와 실제 파일 hash를 비교해 동일함을 확인한 뒤 공개했다.

공개 GitHub release를 다시 조회해 다음을 확인했다.

- public latest가 `v1.7.2`
- release target commit이 exact product source SHA와 일치
- `Junhyun-Helper.zip` 공개 asset 존재
- 공개 asset size가 `80,444,391` bytes
- 공개 asset digest가 `sha256:81d8e6a82db0f4b33ebbdd2bf7f455c1d92ffc2f8b6015f6ba6190e616be1fc0`
- `SHA256SUMS.txt` 공개 asset 존재

즉 GitHub public asset metadata readback의 size/digest는 업로드 직전 Release runner 검증값과 일치한다.

## v1.7.2 product change

v1.7.2는 Mini Scanner 표시 안정성 PATCH다.

- 마지막 확정 Item은 실제 인식 miss 1~2회 동안 유지
- 3회 연속 실제 miss에서 hide
- 같은 Item 재확정 시 miss budget reset
- 다른 Item 확정 시 즉시 교체
- candidate 안정화/OCR 진행 상태는 miss로 계산하지 않음
- inventory/stash context OCR은 initial entry gate로 유지
- 이미 visible인 정상 결과를 auxiliary context OCR 단발 실패로 숨기지 않음

Recognition safety는 변경하지 않았다.

```text
structural floor: 0.34
HEADER_FRAME_LOCKED floor: 0.68
continuous candidate cap: 8
one-shot candidate cap: 12
continuous scan interval: 350 ms
semantic retry interval: 1200 ms
```

공식 설계 근거는 `docs/DECISION_V1.7.2_MINI_SCANNER_STABILITY_2026-08-25.md`를 따른다.
