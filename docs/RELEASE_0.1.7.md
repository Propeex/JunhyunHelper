# RELEASE 0.1.7 — Quest exact gates + MiniMap exact floor frame

기록일: **2026-08-17**

상태: **PUBLIC RELEASE / VERIFIED**

## 목적

v0.1.7은 v0.1.6 이후 정리한 Quest availability 정확도 개선과 MiniMap 층 전환 화면 고정을 함께 공개합니다.

## Quest prerequisite / availability

- v0.1.6의 BTR Driver `A Helping Hand = Active`, Ref, Lightkeeper recoverable access 의미를 유지합니다.
- 서로 다른 `taskRequirements`는 AND, 한 requirement의 `status[]`는 OR입니다.
- 별도 `수주 가능` 상태는 만들지 않습니다. 게임에서 받을 수 있는 Quest는 Helper에서 즉시 수락한 것으로 간주합니다.
- EFT `globalVariable` availability를 `variableId / operator / value` 구조의 profile-variable requirement로 보존합니다.
- 현재 profile variable의 exact 값이 있으면 그 값으로 정확히 판정합니다.
- exact 값을 알 수 없으면 0, 완료 Quest 수 등으로 추측하지 않고 해당 조건만 `확인 필요`로 유지합니다.
- dialogue / 실제 완료 시각 / 공개 source로 증명할 수 없는 server-side write rule도 임의 복원하지 않습니다.

## MiniMap 층 전환

제품 기준은 **같은 지도에서 층을 바꾸면 화면 구도는 움직이지 않고 층 레이어만 바뀌는 것**입니다.

다층 Map SVG는 층별 별도 크기 이미지가 아니라 같은 canonical canvas 안의 floor layer이므로 층별 임의 zoom coefficient를 만들지 않습니다.

v0.1.7은 floor render 전후에 실제 live transform을 보존합니다.

- live Zoom / Scale 유지
- live Translate X 유지
- live Translate Y 유지
- PlayerTracking의 live transform과 stale persisted offset이 달라도 현재 화면을 우선
- floor-only change 완료 후 map-space center 재계산이나 offset clamp로 다시 프레이밍하지 않음

상세 계약: `docs/MINIMAP_FLOOR_FRAME_2026-08-17.md`

## 저장 호환성

```text
Desktop ProductVersion: 0.1.7
Content schema: v7
Readable Content schemas: v3, v4, v5, v6, v7
user.db SQLite schema: v1 unchanged
v0.1.6 → v0.1.7 mandatory data update: none
```

기존 Profile / Quest 진행 / Inventory / Hideout / Map 설정은 유지됩니다.

## Release gate

다음 검증을 모두 통과한 exact release baseline만 공개했습니다.

1. Desktop Release build
2. 전체 automated tests
3. Windows x64 self-contained single-file publish
4. ProductVersion `0.1.7` 확인
5. `FIRST_RUN_KO.txt` v0.1.7 / Content schema v7 확인
6. release root 검증
   - `준현 헬퍼.exe`
   - `FIRST_RUN_KO.txt`
   - `Assets/`
   - root DLL 없음
   - PDB 없음
   - nested ZIP 없음
   - runtime `Logs/` 없음
   - 금지된 legacy dependency 없음
7. 실제 startup + Main Map + Factory + MiniMap runtime smoke
8. MiniMap stale persisted offset 회귀 재현 + floor viewport 보존
9. 정상 Main Window close / process exit
10. GitHub Release 생성
11. draft/prerelease 아님 확인
12. 공개 ZIP 재다운로드 후 SHA-256 재검증

## 최종 공개 기록

```text
release baseline: 8cf2f76003bf2603b8c0f8c0a7d9297bfc62bd43
candidate PR CI run: 31986395934 — SUCCESS
main CI run: 31986585081 — SUCCESS
release workflow run: 31986801215 — SUCCESS
Desktop ProductVersion: 0.1.7
Content schema: v7
Windows x64 publish: SUCCESS
startup + Main Map + Factory + MiniMap smoke: SUCCESS
graceful shutdown: SUCCESS
public asset: Junhyun-Helper-v0.1.7-win-x64.zip
public asset size: 74,049,135 bytes
public SHA-256: b1f935ba47a48e66a46fc028f2d7f631ffb795dada0f3d50b1c42b57ca7caceb
public ZIP re-download + SHA-256 verification: SUCCESS
draft: false
prerelease: false
release: https://github.com/Propeex/JunhyunHelper/releases/tag/v0.1.7
```

임시 `.github/workflows/release-v0.1.7.yml`은 공개 검증 완료 후 제거했습니다. 상시 workflow는 `ci.yml`만 유지합니다.
