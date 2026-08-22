from pathlib import Path
import re

SOURCE = "8c0de649f18d7caa4f5669a06511c15e784dfd29"
PR_RUN = "32540688111"
RELEASE_RUN = "32542259521"
DUPLICATE_RUN = "32542441274"
ASSET = "Junhyun-Helper-v1.2.1-win-x64.zip"
BYTES = "80,306,749"
SHA256 = "48a8b54fcdc3346a092ef3da2744f2d4ca7e27d99da5b52e3ebee7b55fa0affa"
PRODUCT_VERSION = f"1.2.1+{SOURCE}"


def load(path: str) -> str:
    return Path(path).read_text(encoding="utf-8")


def save(path: str, text: str) -> None:
    Path(path).write_text(text, encoding="utf-8")


def replace_once(text: str, old: str, new: str, label: str) -> str:
    if new in text:
        return text
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{label}: expected one replacement target, found {count}")
    return text.replace(old, new, 1)


def replace_between(text: str, start_marker: str, end_marker: str, replacement: str, label: str) -> str:
    start = text.find(start_marker)
    end = text.find(end_marker, start + len(start_marker)) if start >= 0 else -1
    if start < 0 or end < 0 or end <= start:
        raise RuntimeError(f"{label}: section markers not found")
    return text[:start] + replacement + text[end:]


# RELEASE_1.2.1.md
path = "docs/RELEASE_1.2.1.md"
text = load(path)
text = replace_once(
    text,
    "Status: **RELEASE CANDIDATE — public release not yet published**",
    "Status: **PUBLIC / VERIFIED**",
    path,
)
text = replace_once(text, "Prepared: 2026-08-22 KST", "Released: 2026-08-22 KST", path)
text = text.replace(
    "The version/documentation commit must pass the same gate before merge.",
    f"The final PR candidate CI run `{PR_RUN}` passed the same Windows Release build, exactly 255/255 tests, win-x64 publish, actual published EXE product smoke, graceful shutdown and clean portable-root gate before merge.",
    1,
)
public = f'''## Public verification

```text
version: v1.2.1 PUBLIC RELEASE / VERIFIED
release source: {SOURCE}
final PR CI: {PR_RUN} — SUCCESS
exact-source release run: {RELEASE_RUN} — SUCCESS
automated tests: 255 passed / 0 failed / 0 skipped
asset: {ASSET}
bytes: {BYTES}
SHA-256: {SHA256}
ProductVersion: {PRODUCT_VERSION}
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact public tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

Exact-source release run `{RELEASE_RUN}` checked out `{SOURCE}` directly and completed the full release gate: build, exactly 255 tests, win-x64 package audit, exact published EXE smoke, Draft creation, Draft asset re-download/checksum/root/ProductVersion/FIRST_RUN verification, Draft-downloaded EXE smoke, public/latest transition, exact tag-source verification, public asset re-download verification and public-downloaded EXE smoke.

A second release-controller run `{DUPLICATE_RUN}` was started by the controller-file trigger while the successful run was already in progress. It independently rebuilt the same exact source, passed 255/255 tests and the exact published EXE smoke, then created a second Draft. Its Draft re-download resolved the already-published canonical v1.2.1 asset from run `{RELEASE_RUN}`, so its independently-created ZIP hash did not match and that duplicate run stopped before any public transition. It did **not** replace the public release, tag, source, assets or canonical checksum. The duplicate Draft is removed during post-release cleanup.

Public release: `https://github.com/Propeex/JunhyunHelper/releases/tag/v1.2.1`.
'''
text = replace_between(text, "## Public verification", "", public, path) if False else text
marker = "## Public verification\n\nThis section is completed only after the exact merged release source is built, a Draft asset is re-downloaded and verified, the release becomes public/latest, and the public asset is independently re-downloaded and smoke-tested."
text = replace_once(text, marker, public.rstrip(), path)
save(path, text)


release_block_v120 = '''현재 public stable은 **v1.2.0**입니다.

```text
version: v1.2.0 PUBLIC RELEASE / VERIFIED
release source: a7601f8498e8d75e832962fb9dd60f4112d28dc6
exact-source release run: 32514322439 — SUCCESS
automated tests: 255 passed / 0 failed / 0 skipped
asset: Junhyun-Helper-v1.2.0-win-x64.zip
bytes: 80,298,514
SHA-256: ab5e9ef35b300268d16a1c5eece86cd8c6e57c91c83364caf4b7d02cde1d27d1
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```'''
release_block_v121 = f'''현재 public stable은 **v1.2.1**입니다.

```text
version: v1.2.1 PUBLIC RELEASE / VERIFIED
release source: {SOURCE}
final PR CI: {PR_RUN} — SUCCESS
exact-source release run: {RELEASE_RUN} — SUCCESS
automated tests: 255 passed / 0 failed / 0 skipped
asset: {ASSET}
bytes: {BYTES}
SHA-256: {SHA256}
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```'''

compat_state_v120 = '''```text
Desktop Version: 1.2.0
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v3
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.1.6 → v1.2.0 mandatory Game Content update: none
v1.1.6 → v1.2.0 user.db migration: none
```'''
compat_state_v121 = '''```text
Desktop Version: 1.2.1
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v3
Scanner catalog cache schema: v1/v2 readable, v2 written
v1.2.0 → v1.2.1 mandatory Game Content update: none
v1.2.0 → v1.2.1 user.db migration: none
```'''

# STATE.md
path = "docs/STATE.md"
text = load(path)
text = replace_once(text, release_block_v120, release_block_v121, path)
text = replace_once(text, compat_state_v120, compat_state_v121, path)
text = text.replace("상세 검증 기록은 `docs/RELEASE_1.2.0.md`에 있습니다.", "상세 검증 기록은 `docs/RELEASE_1.2.1.md`에 있습니다.", 1)
text = text.replace("## 4. Scanner v1.2.0 recognition 구조", "## 4. Scanner recognition 구조 / v1.2.1 hardening", 1)
hardening = '''### v1.2.1 deterministic hardening

- Tarkov `resources.assets` font discovery는 bounded streaming scan을 사용합니다.
- Bender/Noto cache는 source manifest와 실제 font-binary generation hash로 세대를 구분합니다.
- OCR-guided/full-catalog visual template cache는 generation-aware bounded cache입니다.
- Mini Scanner inventory/stash OCR은 동시에 최대 1개이며 latest snapshot으로 coalesce하고 stale epoch 결과를 폐기합니다.
- one-shot과 profile/GameMode monitor는 shared runtime state를 직렬화하며 최신 mode/context만 복구합니다.
- font-aware OCR은 active-operation lifetime으로 종료 중 resource-disposal race를 막습니다.
- PrintWindow sparse validation은 전체 frame의 두 번째 managed copy 없이 locked bitmap에서 직접 sample합니다.
- title-anchor diagnostics는 실제 detector evidence score를 보존합니다.
- confidence/top1-top2 margin과 fail-closed identity 기준은 v1.2.0에서 완화하지 않았습니다.

'''
if "### v1.2.1 deterministic hardening" not in text:
    text = replace_once(text, "## 5. 1회 고정밀 스캔 / 단축키", hardening + "## 5. 1회 고정밀 스캔 / 단축키", path)
verification = f'''## 13. v1.2.1 검증 결과

Final PR CI `{PR_RUN}`와 exact-source release run `{RELEASE_RUN}`에서 다음을 모두 통과했습니다.

- exact release source `{SOURCE}`
- Windows Release build
- **255 automated tests / 0 failure / 0 skipped**
- win-x64 self-contained single-file publish
- exact ProductVersion/FIRST_RUN verification
- package root / dependency / PDB / nested archive audit
- exact published EXE Product UI / Scanner / Mini Scanner / Main Map / Factory / MiniMap smoke
- one-shot mode restoration + title-anchor/magnifier product smoke
- graceful shutdown / clean portable root
- Draft package re-download checksum/root/ProductVersion/FIRST_RUN verification
- Draft-downloaded EXE smoke
- public/latest transition
- exact tag source verification
- public package re-download verification
- public-downloaded EXE smoke

Public asset:

```text
{ASSET}
{BYTES} bytes
SHA-256 {SHA256}
ProductVersion {PRODUCT_VERSION}
```

'''
match = re.search(r"## 13\.[\s\S]*?(?=## 14\.)", text)
if not match:
    raise RuntimeError(f"{path}: verification section 13 not found")
text = text[:match.start()] + verification + text[match.end():]
text = text.replace("v1.2.0 public package verified", "v1.2.1 public package verified")
text = text.replace("v1.2.0 public baseline / live Tarkov validation 및 후속 수정 진행 대상", "v1.2.1 public verified / live Tarkov calibration 및 후속 evidence-based 수정 진행 대상")
save(path, text)


# CURRENT_STATE.md
path = "docs/CURRENT_STATE.md"
text = load(path)
text = text.replace(
    "상태: **`v1.2.0 PUBLIC RELEASE / VERIFIED — Scanner title recognition overhaul`**",
    "상태: **`v1.2.1 PUBLIC RELEASE / VERIFIED — Scanner stability and accuracy hardening`**",
    1,
)
baseline = f'''## 현재 공개 기준선

```text
version: v1.2.1
release source: {SOURCE}
final PR CI: {PR_RUN} — SUCCESS
exact-source release run: {RELEASE_RUN} — SUCCESS
automated tests: 255 passed / 0 failed / 0 skipped
asset: {ASSET}
bytes: {BYTES}
SHA-256: {SHA256}
Draft-downloaded EXE smoke: SUCCESS
public/latest: VERIFIED
exact tag source: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

```text
Desktop Version: 1.2.1
Content schema: v7
Readable Content schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v3
Scanner cache schema: v1/v2 readable, v2 written
v1.2.0 → v1.2.1 mandatory Game Content update: none
v1.2.0 → v1.2.1 user.db migration: none
```

'''
text = replace_between(text, "## 현재 공개 기준선", "## Scanner 현재 계약", baseline, path)
changes = '''## v1.2.1 핵심 변경

- `resources.assets` title-font discovery bounded streaming scan
- Tarkov source manifest + actual font-binary generation hash
- generation-aware bounded visual template caches
- Mini Scanner inventory/stash OCR single-probe coalescing + stale-result rejection
- one-shot/profile/GameMode lifecycle serialization and latest-mode restore rule
- shutdown-safe font-aware OCR active-operation lifetime
- PrintWindow sparse validation의 redundant full-frame managed copy 제거
- title-anchor diagnostic score에 실제 detector evidence 보존
- recognition confidence/top1-top2 margin 완화 없음
- v1.2.0의 `인식 이미지`, `1회 고정밀 스캔`, title-anchor/Tarkov-font recovery 기능 유지

상세: `docs/RELEASE_1.2.1.md`, `docs/SCANNER.md`, `docs/SCANNER_TEST_PLAN.md`.

'''
text = replace_between(text, "## v1.2.0 핵심 변경", "## Mini Scanner / data baseline", changes, path)
text = text.replace("v1.2.0 public package verified", "v1.2.1 public package verified")
text = text.replace("v1.2.0 public baseline / live Tarkov validation and follow-up fixes ongoing", "v1.2.1 public verified / live Tarkov calibration and evidence-based follow-up ongoing")
save(path, text)


# README.md
path = "README.md"
text = load(path)
text = replace_once(text, release_block_v120, release_block_v121, path)
compat_readme_v120 = '''```text
Content schema: v7
Readable schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v3
Scanner cache schema: v1/v2 readable, v2 written
v1.1.6 → v1.2.0 mandatory Game Content update: none
v1.1.6 → v1.2.0 user.db migration: none
```'''
compat_readme_v121 = '''```text
Content schema: v7
Readable schemas: v3~v7
user.db schema: v1
Scanner display settings schema: v3
Scanner cache schema: v1/v2 readable, v2 written
v1.2.0 → v1.2.1 mandatory Game Content update: none
v1.2.0 → v1.2.1 user.db migration: none
```'''
text = replace_once(text, compat_readme_v120, compat_readme_v121, path)
text = text.replace("상세 릴리즈 기록은 `docs/RELEASE_1.2.0.md`에 있습니다.", "상세 릴리즈 기록은 `docs/RELEASE_1.2.1.md`에 있습니다.", 1)
readme_hardening = '''v1.2.1 Scanner 하드닝:

- Tarkov title-font discovery bounded streaming scan
- source manifest + font generation hash로 stale visual template 방지
- visual template cache bounded/generation-aware
- Mini Scanner inventory OCR 단일 probe + latest-request coalescing + stale result 폐기
- one-shot/profile/GameMode lifecycle 직렬화
- shutdown 중 font-aware OCR resource disposal race 방지
- PrintWindow validation의 redundant full-frame managed copy 제거
- 실제 anchor detector score를 진단에 보존
- 인식 confidence/margin 및 fail-closed 정책은 완화하지 않음

'''
if "v1.2.1 Scanner 하드닝:" not in text:
    text = replace_once(text, "v1.2.0 Scanner 보강:\n", readme_hardening + "v1.2.0 Scanner 보강:\n", path)
text = text.replace("상세: `docs/SCANNER.md`, `docs/SCANNER_TEST_PLAN.md`, `docs/RELEASE_1.2.0.md`.", "상세: `docs/SCANNER.md`, `docs/SCANNER_TEST_PLAN.md`, `docs/RELEASE_1.2.1.md`.", 1)
text = text.replace(
    "v1.2.0은 Scanner 진단 이미지와 1회 고정밀 스캔이라는 사용자 기능을 추가하고 제목 인식 구조를 보강한 MINOR 릴리즈입니다.",
    "v1.2.0은 Scanner 진단 이미지와 1회 고정밀 스캔이라는 사용자 기능을 추가한 MINOR 릴리즈이며, v1.2.1은 그 기능의 lifecycle/cache/capture/resource 안정성을 보강한 PATCH 릴리즈입니다.",
    1,
)
text = text.replace("- `docs/RELEASE_1.2.0.md` — v1.2.0 public release record", "- `docs/RELEASE_1.2.1.md` — current public release record\n- `docs/RELEASE_1.2.0.md` — v1.2.0 release history", 1)
save(path, text)


# SCANNER.md
path = "docs/SCANNER.md"
text = load(path)
text = text.replace(
    "상태: **`v1.2.0 PUBLIC BASELINE / v1.2.1 RELEASE CANDIDATE / SCANNER LAB v3.8 CONTRACT PRESERVED / LIVE TARKOV E2E ONGOING`**",
    "상태: **`v1.2.1 PUBLIC RELEASE / VERIFIED / SCANNER LAB v3.8 CONTRACT PRESERVED / LIVE TARKOV E2E ONGOING`**",
    1,
)
if "## v1.2.1 Public verification" not in text:
    text += f'''\n\n## v1.2.1 Public verification

```text
release source: {SOURCE}
final PR CI: {PR_RUN} — SUCCESS
exact-source release run: {RELEASE_RUN} — SUCCESS
automated tests: 255 passed / 0 failed / 0 skipped
asset: {ASSET}
bytes: {BYTES}
SHA-256: {SHA256}
ProductVersion: {PRODUCT_VERSION}
Draft-downloaded EXE smoke: SUCCESS
public/latest + exact tag: VERIFIED
public-downloaded EXE smoke: SUCCESS
```

실제 최신 Tarkov Borderless E2E calibration은 별도 evidence 기반 후속 검증으로 계속 진행합니다. live 근거 없이 recognition threshold를 낮추지 않습니다.
'''
save(path, text)


# SCANNER_TEST_PLAN.md
path = "docs/SCANNER_TEST_PLAN.md"
text = load(path)
text = text.replace(
    "상태: **`v1.2.0 PUBLIC VERIFIED / v1.2.1 RELEASE CANDIDATE / LIVE TARKOV CALIBRATION DEFERRED`**",
    "상태: **`v1.2.1 PUBLIC RELEASE GATE PASSED / PUBLIC VERIFIED / LIVE TARKOV CALIBRATION DEFERRED`**",
    1,
)
text = text.replace("## 1. v1.2.1 Release blocking gate", "## 1. v1.2.1 Release blocking gate — 완료", 1)
if "## v1.2.1 Public release verification — 완료" not in text:
    text += f'''\n\n## v1.2.1 Public release verification — 완료

```text
release source: {SOURCE}
final PR CI: {PR_RUN} — SUCCESS
exact-source release run: {RELEASE_RUN} — SUCCESS
255 passed / 0 failed / 0 skipped
asset: {ASSET}
bytes: {BYTES}
SHA-256: {SHA256}
ProductVersion: {PRODUCT_VERSION}
Draft asset verification + EXE smoke: SUCCESS
public/latest + exact tag verification: SUCCESS
Public asset verification + EXE smoke: SUCCESS
```
'''
save(path, text)


# ARCHITECTURE.md
path = "docs/ARCHITECTURE.md"
text = load(path)
old = "현재 공개 기준선은 **`v1.2.0 PUBLIC RELEASE / VERIFIED`**이며, **v1.2.1은 Scanner deterministic 안정성·정확성 하드닝 release candidate**입니다. v1.2.1은 live Tarkov 데이터가 필요한 recognition threshold를 추측해서 변경하지 않습니다."
new = f"현재 공개 기준선은 **`v1.2.1 PUBLIC RELEASE / VERIFIED`**입니다. release source는 `{SOURCE}`이며 exact-source release run `{RELEASE_RUN}`에서 Draft/Public 재다운로드와 실제 EXE smoke까지 검증했습니다. v1.2.1은 live Tarkov 데이터가 필요한 recognition threshold를 추측해서 변경하지 않은 deterministic Scanner 하드닝 PATCH입니다."
text = replace_once(text, old, new, path)
save(path, text)

print("v1.2.1 public documentation finalized")
