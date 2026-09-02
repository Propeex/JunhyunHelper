# ACTIVE WORK

Status: **NONE**

## Current task

None.

## Last completed

v1.16.4 PATCH fixed the user-reported Farming Guide regression where an explicitly locked stored item could be included in an automatic movement/repacking instruction.

```text
public stable: v1.16.4
exact product source/tag target:
5886d8f97abd060d398d4c50d3dd3b720e4ace09
merge PR: #285
validated PR head: d55e138c962e87dc8691f82c81d36a516db52941
PR CI / Shutdown / Docs:
33623459284 / 33623459290 / 33623459267 — SUCCESS
exact-main CI / Shutdown / Docs:
33623824030 / 33623824052 / 33623824027 — SUCCESS
Release workflow: 33624248788 — SUCCESS
release id: 381192920
published UTC: 2026-09-02T11:22:47Z
623 passed / 0 failed / 0 skipped
```

Authoritative lock contract: an explicitly locked stored item is position-locked for automatic Farming Guide advice. Automatic planning cannot discard, replace, relocate, rotate, re-parent or indirectly move it through ancestor/root-carrier movement. Manual editing remains authoritative, and a locked carrier root still exposes legal internal storage.

Draft PR #284 was closed unmerged only because the connected GitHub ready-for-review mutation was broken; non-draft PR #285 is the authoritative validated and merged PR.

Public release assets and exact validation evidence are recorded in `docs/PROJECT_STATE.json`, `docs/.release-v1.16.4-status.json`, `docs/CURRENT_STATE.md`, `docs/STATE.md`, and `docs/RELEASE_NOTES_V1.16.4.md`.

Actual user-PC/Tarkov real-play validation remains separately `PENDING`; it is not unfinished development and does not alter the completed v1.16.4 public release identity.
