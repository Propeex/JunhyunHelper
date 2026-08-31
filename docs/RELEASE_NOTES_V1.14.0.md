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

## 검증

릴리즈 준비 전 기능 HEAD에서 다음을 확인했습니다.

- Windows Release build 성공
- deterministic tests **527 passed / 0 failed / 0 skipped**
- self-contained Windows x64 publish 성공
- actual published EXE Product UI / Farming Guide / Map smoke 성공
- exact multi-grid render 및 drop-target identity smoke 성공
- graceful shutdown 성공
- release package/checksum 검증 성공
- Shutdown Race 성공
- Documentation Consistency 성공

최종 공개 v1.14.0의 exact source / tag / release asset / checksum은 main 병합 후 공개 release 검증 시 canonical 상태 문서에 기록합니다.
