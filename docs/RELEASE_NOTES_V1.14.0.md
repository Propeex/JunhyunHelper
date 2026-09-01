# 준현 헬퍼 v1.14.0

## 파밍 가이드 조립·총기 개조 강화

- 장비 보드에서 현재 사용자가 직접 장착할 수 없는 PMC 인식표 슬롯을 제거했습니다. 기존 저장 데이터는 계속 읽을 수 있으며 current product state에서 안전하게 정리됩니다.
- 총기·헬멧·방어구의 부착물 구조를 root 한 단계에 한정하지 않고 하위 부품 슬롯까지 재귀적으로 편집할 수 있습니다.
- 빈 부착물/방탄판 슬롯을 클릭하면 현재 조립 상태에서 실제로 호환되는 아이템을 아이콘 카드로 같은 화면에 표시합니다. 아이템을 클릭하면 즉시 장착되며 별도 Windows 설정 창을 사용하지 않습니다.
- 검색 결과를 끌어 슬롯에 놓는 기존 방식도 유지하며 클릭 선택과 동일한 current Tarkov filter/conflict 검증을 사용합니다.
- 현재 조립이 imported authoritative default preset 구성과 정확히 일치하면 해당 composed preset 이미지를 사용합니다. 임의 조립은 base image와 설치 부품 표시를 조합한 deterministic fallback으로 상태 변화를 표현합니다.

## 수납 배치 신뢰성

- 리그·가방·컨테이너의 실제 수납 가능 여부는 계속 current Game Content의 grid 크기·filter가 권위입니다.
- UI의 다중 grid 상대 배치는 검증된 visual-layout metadata가 있고 해당 metadata의 grid signature가 current grid count/width/height와 정확히 일치할 때만 exact placement를 사용합니다.
- exact metadata가 없거나 Tarkov 업데이트로 구조가 달라지면 오래된 좌표를 억지로 적용하지 않고 finite compact layout으로 fail-safe fallback합니다.
- importer가 `GridLayoutName` / `RigLayoutName` 계열 layout identity를 보존하도록 확장했습니다.

## 데이터 호환성

- Game Content snapshot write schema를 **v10**으로 올려 assembly source와 storage layout identity를 보존합니다.
- 기존 **v3~v9** Game Content snapshot도 계속 읽을 수 있습니다.
- Farming Guide 사용자 상태 schema는 **v1**을 유지합니다.
- mandatory user-state migration은 없습니다.

## 최종 검증

공개 v1.14.0 exact product source:

```text
9ff23b9f50dd84b84ec93cea31b079d7eff70fe1
```

검증 결과:

- deterministic tests **527 passed / 0 failed / 0 skipped**
- Windows Release build / XAML compile 성공
- self-contained Windows x64 publish 성공
- ProductVersion `1.14.0+9ff23b9f50dd84b84ec93cea31b079d7eff70fe1`
- actual published EXE Product UI / Farming Guide / Map smoke 성공
- recursive assembly / compatible-item picker smoke 성공
- exact multi-grid Canvas render 및 `GridDropTarget.GridIndex` identity smoke 성공
- graceful shutdown / clean portable root 성공
- Shutdown Race 성공
- release package/checksum 검증 성공
- exact-main Documentation Consistency 성공
- exact-main Actions artifact digest 검증 성공
- automatic Release workflow 성공
- public tag / release / assets / latest-stable readback 성공

Public release:

```text
release id: 380133403
published UTC: 2026-09-01T00:15:44Z
Junhyun-Helper.zip
bytes: 80,633,458
SHA-256:
87728ce9e34a30a9b1eb735fe92b1a4a39f172f3b9cf536dfd12d88c8c35667b
```

상세 release evidence:

- `docs/RELEASE_1.14.0.md`
- `docs/.release-v1.14.0-status.json`

후속 documentation-only main commit은 v1.14.0 제품 릴리즈 소스가 아니며 공개 tag/source/assets를 변경하지 않습니다.
