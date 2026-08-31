# CURRENT STATE

> 최신 제품 상태의 짧은 인덱스입니다. 기계 판독 가능한 사실값은 `docs/PROJECT_STATE.json`, 상세 계약은 `docs/STATE.md`, 진행 중 작업은 `docs/ACTIVE_WORK.md`를 기준으로 합니다.

기준일: **2026-08-31 KST**  
상태: **v1.12.1 PUBLIC STABLE / PRODUCT COMPLETE / MAINTENANCE MODE**

## 공개 stable

```text
public stable/latest: v1.12.1
exact product release source/tag target:
07a808f187e59f1b2b4b62ca6a947ccbed9baeaa
PR: #239 — MERGED
validated feature head: 7e418c7d32c945260b471d19ac43c411f15bef1b
PR exact-head CI: 33350561623 — SUCCESS
PR exact-head Shutdown Race CI: 33350561588 — SUCCESS
PR exact-head Documentation Consistency: 33350561628 — SUCCESS
exact-main CI: 33350742745 — SUCCESS
exact-main Shutdown Race CI: 33350742733 — SUCCESS
exact-main Documentation Consistency: 33350742720 — SUCCESS
release workflow: 33350893047 — SUCCESS
release id: 379473487
published UTC: 2026-08-31T02:31:04Z
483 passed / 0 failed / 0 skipped
```

Public package:

```text
Junhyun-Helper.zip
asset id: 537336876
bytes: 80,572,885
SHA-256:
fbbaa41bbb41843a54ccbdd16721c138d93ddea34092fd7e468bbb3d99ed9212

SHA256SUMS.txt
asset id: 537336877
bytes: 86
asset SHA-256:
aa63dffbea42d2b624b74b96c6acc38dbe34906186c9ea43727abac7fc8c0619
```

Exact-main Actions artifact:

```text
name: JunhyunHelper-win-x64
artifact id: 9743552872
archive bytes: 241,651,204
archive SHA-256:
f65de2b7a1da8f27302cdff815b6978d4ae291fe81964e2d131ec57fbb40050a
```

GitHub `/releases/latest`, release target, `refs/tags/v1.12.1`, exact-main product source가 모두 `07a808f187e59f1b2b4b62ca6a947ccbed9baeaa`에 일치한다. 공개 release는 `draft=false`, `prerelease=false`이다.

## v1.12.1 핵심 변경

### 김태영 PC 진단 UX

- 헤더 진단 아이콘 클릭 확인 문구: `혹시 김태영 본인?`
- `예` 후 진단 ZIP 생성 동안 indeterminate progress bar 표시
- 정상 완료 문구는 `진단 완료.` / `파일을 hyune4784@naver.com 으로 보내주세요.` 두 문장으로 고정
- 완료 안내를 닫으면 기본 브라우저에서 `https://mail.naver.com/v2/new`를 연다.
- ZIP은 Desktop에 로컬 생성하며 자동 업로드, 웹메일 자동 첨부, 자동 발송은 하지 않는다.

### 사용자 노트북 진단 smoke

사용자가 v1.12.0에서 만든 실제 diagnostic ZIP을 검토했다. ZIP CRC와 expected evidence 11개가 모두 정상이고 `probe-errors.txt = none`이었다. display capture/stats, Scanner support bundle, Scanner/catalog snapshot도 정상 생성됐다. 당시 Tarkov가 실행 중이지 않아 Tarkov dual-capture evidence만 없었다. 따라서 exporter 자체는 실제 사용자 노트북에서 정상 동작했다.

## 유지되는 주요 계약

- Scanner는 false positive보다 miss를 선호하며 reviewed actual Tarkov evidence 없이 recognition acceptance를 완화하지 않는다.
- Scanner는 external screen pixels + OCR만 사용한다. game memory read/injection/hook/kernel/input automation/network manipulation/anti-cheat bypass를 사용하지 않는다.
- Quest exact ProfileVariable은 runtime compatibility보다 우선하며 Future Needed Items / cleanup은 current Quest UI compatibility와 분리해 보수적으로 계산한다.
- Hideout FIR은 source `attributes.foundInRaid` 의미를 보존한다.
- Ammo pickup은 same-caliber penetration과 현재 profile에서 증명된 direct purchase 상태를 기준으로 한다.
- Game Content update는 candidate/LKG/completeness/fail-closed 계약을 유지한다.
- Map/MiniMap donor pin은 `d933792b6042a51cea38dc44b686a096fe30de67`이다.
- user-visible WPF 변경은 actual published EXE runtime evidence로 검증한다.

## Schema / compatibility

```text
Desktop target version: 1.12.1
Content schema write: v8
Readable Content schemas: v3~v8
user.db schema: v1
Scanner display settings schema: v9
Scanner catalog cache write: v4
Scanner catalog readable: v1~v4
```

v1.12.0 → v1.12.1 mandatory data/schema migration은 없다.

## 다음 작업

현재 `docs/ACTIVE_WORK.md`는 `NONE`이다. 사용자의 실제 PC/Tarkov 최종 실사용 확인과 김태영 실제 PC diagnostic ZIP 분석은 자동화 release verification과 별개이며 `PENDING`이다.

공개 증거:

- `docs/RELEASE_1.12.1.md`
- `docs/.release-v1.12.1-status.json`
- `docs/RELEASE_NOTES_V1.12.1.md`
- `docs/DECISION_V1.12.0_KIM_TAEYOUNG_PC_DIAGNOSTIC.md`

이 문서와 이후 documentation-only commit은 v1.12.1 product release source가 아니다. historical identity는 `07a808f187e59f1b2b4b62ca6a947ccbed9baeaa`에 고정한다.
