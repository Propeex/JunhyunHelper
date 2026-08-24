# 준현 헬퍼 v1.6.0 — Public Release Record

기준일: 2026-08-24
상태: **PUBLIC RELEASE / VERIFIED**

## Release identity

```text
version: 1.6.0
tag: v1.6.0
exact release source: e18c108380572913552030aa677bba06ebf49355
ProductVersion: 1.6.0+e18c108380572913552030aa677bba06ebf49355
recovery/final verification run: 32710012954
public verification job: 97379365581
verified_at_utc: 2026-08-24T09:10:44.9630720Z
```

## Public assets

### Stable user package

```text
asset: Junhyun-Helper.zip
bytes: 80,425,013
SHA-256: f9384ff49d522afb5976efe291ff932d66063dcfeee64b0aed7a5daa691a12c5
extracted root: 준현 헬퍼/
```

### v1.5 updater bridge

```text
asset: Junhyun-Helper-v1.6.0-win-x64.zip
bytes: 80,424,089
SHA-256: 3f05b20ccbd7463fb590889042b1b706290a88e0568cd00c3b2fa23cf966dfc8
```

Checksum manifest:

```text
SHA256SUMS.txt
```

`Junhyun-Helper.zip`은 GitHub Release asset filename 정규화 제약 때문에 ASCII stable 이름을 사용한다. 압축 해제 후 제품 폴더 `준현 헬퍼/`와 실행 파일 `준현 헬퍼.exe`는 그대로 유지한다.

## Final verification

최종 exact source와 공개 배포물에 대해 다음을 확인했다.

- Desktop Release build: SUCCESS
- automated tests: **299 passed / 0 failed / 0 skipped**
- Windows x64 self-contained single-file publish: SUCCESS
- release package identity/layout audit: SUCCESS
- rendered Product UI / Scanner / Mini Scanner smoke: SUCCESS
- Main Map / Factory / MiniMap smoke: SUCCESS
- graceful shutdown: SUCCESS
- stable `Junhyun-Helper.zip` generation: SUCCESS
- v1.5 updater bridge package generation: SUCCESS
- stable/bridge payload equivalence: SUCCESS
- `SHA256SUMS.txt` verification: SUCCESS
- exact `v1.6.0` tag source: VERIFIED
- stable/latest publication: VERIFIED
- anonymous public latest metadata: VERIFIED
- anonymous public ZIP + checksum redownload: VERIFIED
- public hash/size verification: VERIFIED
- public ProductVersion verification: VERIFIED
- public-downloaded EXE Product UI/Scanner/Mini Scanner/Map smoke: SUCCESS
- public-downloaded EXE graceful shutdown: SUCCESS
- clean portable root: VERIFIED

## Release path notes

v1.6.0 release 준비 중 GitHub Release가 비영문 asset filename `준현 헬퍼.zip`을 `default.zip`으로 정규화하는 동작을 확인했다. 공개 전에 stable asset contract를 `Junhyun-Helper.zip`으로 교정하고 updater, packaging, checksum, tests, CI, 문서를 함께 수정했다.

교정 PR #175의 HEAD `ad7f4259a44c72902fba452adbbcfd4c540ae577`는 CI run `32708999577`에서 전체 gate를 통과했으며, merge commit `e18c108380572913552030aa677bba06ebf49355`를 v1.6.0 exact release source로 고정했다.

초기 final controller run `32709414932`에서는 build/test/publish/package/product smoke/tag retarget/draft asset 교체까지 성공했지만, draft 재다운로드 manifest 비교 스크립트의 PowerShell `TrimStart` 인자 오류로 공개 직전 fail closed했다. 제품이나 배포 바이트 오류가 아니었으며 stable/latest 공개는 수행되지 않았다.

이후 recovery run `32710012954`에서 현재 draft 자산을 asset ID로 다시 내려받아 checksum, layout, stable/bridge payload 동일성, ProductVersion, 실행 smoke를 검증한 뒤 stable/latest로 공개했다. 별도 public verifier가 인증 없이 public metadata와 세 자산을 다시 내려받아 동일 검증 및 실행 smoke를 완료했다.

## Post-release phase

v1.6.0 이후 Scanner 작업은 **LIVE GROUND TRUTH MAINTENANCE** 단계로 복귀한다.

실사용에서 발견되는 실패는 capture → structural proposal → close-X → magnifier → header lock → title ROI → raw OCR → substitution → sanitation/matcher → visual recovery → Item ID → mapped presentation → overlay/stale timing 순으로 분류한다.

인식 safety threshold는 검토된 Ground Truth evidence 없이 완화하지 않는다. REGRESSION=0을 유지한 변경만 PATCH 후보가 된다.

세부 변경 내용은 `docs/RELEASE_NOTES_V1.6.0.md`, 설계 결정은 `docs/DECISION_V1.6.0_SCANNER_PRODUCT_WORKFLOW_2026-08-24.md`를 참조한다.
