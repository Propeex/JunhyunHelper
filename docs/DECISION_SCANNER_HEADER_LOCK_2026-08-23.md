# DEC-058 — Scanner title ROI는 실제 inspect-header frame이 소유한다

- 상태: `CONFIRMED / IMPLEMENTED / PUBLIC VERIFIED v1.3.3`
- 날짜: 2026-08-23
- 근거: 사용자가 제공한 실제 2048×1280 Tarkov 상세창 12개

## 결정

1. first title glyph connected component는 title ROI의 left edge를 결정하지 않는다.
2. red close/X와 long neutral top frame을 먼저 잠근다.
3. magnifier는 bounded frame-left icon lane 안에서만 찾고 실제 13px-class bright core의 위치·크기와 ring/hollow/handle morphology를 함께 평가한다.
4. dark title field와 title text presence가 함께 확인되어 `HEADER_FRAME_LOCKED`가 된 candidate만 OCR identity path로 진행할 수 있다.
5. runtime은 `HEADER_FRAME_LOCKED`와 anchor score **0.68 이상**을 다시 요구한다. partial/failed anchor는 fail closed하고 OCR로 Item ID를 추측하지 않는다.
6. raw OCR과 current-catalog sanitation 이후 matcher input을 diagnostics에서 분리한다.
7. 기존 normal confidence/top1-top2 margin과 bounded unique one-edit 안전 조건을 완화하지 않는다.
8. highest trader / flea `avg24hPrice` / `RequiredTotal` 의미와 content/user/settings schema는 변경하지 않는다.
9. 신규 사용자 기능이 아니라 실제 live regression 수정이므로 DEC-048에 따라 **v1.3.3 PATCH**다.

## 공개 검증

```text
release source/tag: 41bf5b8374ba774866aab4b60a25376d9b5548c2
final PR CI: 32625223009 — SUCCESS
263 passed / 0 failed / 0 skipped
release run: 32625403609 — SUCCESS
asset: Junhyun-Helper-v1.3.3-win-x64.zip
bytes: 80,314,373
SHA-256: 0771d3c7dee5a8f19904d52eeedc7b9abbd6027a7b000255ebd33c296bc2186f
ProductVersion: 1.3.3+41bf5b8374ba774866aab4b60a25376d9b5548c2
public/latest: VERIFIED
public re-download: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

상세 근거:

- `docs/SCANNER_V1.3.3_HEADER_LOCK.md`
- `docs/.scanner-v1.3.3-header-evidence.json`
- `docs/RELEASE_1.3.3.md`
- `docs/.release-v1.3.3-status.json`
