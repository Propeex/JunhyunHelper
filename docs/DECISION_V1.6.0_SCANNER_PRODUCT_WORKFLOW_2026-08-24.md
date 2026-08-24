# DECISION — v1.6.0 Scanner product workflow

상태: `APPROVED / IMPLEMENTED / RELEASE CANDIDATE`

기준일: 2026-08-24

## 1. 목적

v1.6.0은 Scanner의 인식 알고리즘을 공격적으로 바꾸는 릴리즈가 아니다.

목표는 이미 구축된 Scanner 파이프라인을 사용자가 실제로 테스트·교정·검증하기 쉽게 만들고, Mini Scanner와 교정 데이터 관리의 사용 흐름을 정리하는 것이다.

이번 변경은 새 사용자 능력인 Mini Scanner 정보 순서 설정과 저장된 Ground Truth 재교정을 포함하므로 VERSIONING 규칙상 MINOR 릴리즈 `1.6.0`으로 분류한다.

## 2. Scanner 일반 화면

일반 Scanner 화면의 상단 동작은 다음 세 가지를 기준으로 한다.

1. `스캐너 ON/OFF`
2. `설정`
3. `고급`

일반 화면에 개발·진단 성격의 동작을 늘어놓지 않는다.

하단은 좌우 2분할 구조를 사용한다.

- 왼쪽: `아이템 검색`
- 오른쪽: `Scanner 로그`

기존 전역 단축키 기능은 삭제하지 않는다.

- 1회 인게임 스캔
- 1회 테스트 스캔
- Scanner ON/OFF

일반 화면에서 버튼을 제거하는 것은 기능 삭제가 아니라 단축키/고급 화면으로의 역할 분리다.

## 3. Scanner 아이템 검색

Scanner 탭의 아이템 검색은 현재 내려받은 전체 Tarkov 아이템 catalog를 사용한다.

검색은 scan-time network를 새로 만들지 않는다.

검색 결과에는 아이콘과 공식 아이템명을 표시한다.

아이템 선택 후 사용자에게 보여 주는 정보는 다음을 우선한다.

- 아이콘
- 공식 아이템명
- Tarkov Wiki 링크
- 플리마켓 24시간 평균가
- 최고 상인 판매가 및 가능한 경우 상인 이름
- 현재 필요한 개수

`현재 필요한 개수`는 Inventory 부족량이 아니라 기존 `ItemsWorkspace.Plan.NeededItems[itemId].RequiredTotal` 의미를 유지한다.

## 4. Mini Scanner

Mini Scanner에서 아이콘과 공식 아이템명은 identity header로 항상 표시한다.

사용자가 표시 여부와 순서를 설정할 수 있는 정보는 다음 다섯 가지다.

- 상인 판매가
- 플리마켓 평균가
- 상인 가격/칸
- 플리 가격/칸
- 필요 개수

설정 순서는 영구 저장한다.

상인 판매가는 `최고 상점가` 같은 일반 레이블보다 가능한 경우 실제 최고가 상인 이름과 가격을 함께 보여 주는 형식을 우선한다.

예: `Therapist 42,000₽`

Mini Scanner의 기존 안전 계약은 유지한다.

- Topmost
- ShowActivated=false
- taskbar 비노출
- drag 가능
- matched Item presentation만 표시
- stale result isolation
- inventory OCR single-active/latest coalescing

## 5. 설정 schema v6

Scanner display settings schema는 v6으로 올린다.

v5 이하 설정은 자동 마이그레이션한다.

마이그레이션 시 가능한 한 다음을 보존한다.

- Scanner ON/OFF 상태
- 3종 전역 단축키
- 기존 Mini Scanner 정보 표시 설정
- 위치/글자 크기
- 사용자 OCR substitution 규칙

v6부터 Mini Scanner의 아이콘과 아이템명은 숨김 설정을 허용하지 않는다.

새 정보 순서가 없는 기존 설정은 canonical 기본 순서로 보충한다.

## 6. 고급 화면

Scanner 고급 화면은 실사용 진단 흐름을 중심으로 정리한다.

핵심 동작:

- 테스트 Scanner ON/OFF
- 현재 결과 교정
- 교정 데이터 관리

일반 사용자가 정상 Scanner를 쓰는 데 필요하지 않은 catalog force refresh, 회귀/내보내기/로그 관리 같은 개발용 동작은 일반 화면에서 제거한다.

내부 진단·retention 기능 자체를 삭제한다는 의미는 아니다.

## 7. Ground Truth 교정 UX

교정 창은 큰 원본 이미지도 현재 창/화면 안에 들어오도록 자동 축소해 표시한다.

중요 계약:

- 표시 배율과 Ground Truth 좌표계를 분리한다.
- 저장 좌표는 항상 원본 이미지 좌표계를 기준으로 한다.
- 축소 표시 때문에 ROI 좌표 정밀도가 손실되어서는 안 된다.

후보 선택은 드롭다운보다 이미지 직접 선택을 우선한다.

사용자가 이미지 위 후보 사각형을 클릭해 다음 항목을 선택할 수 있어야 한다.

- 상세보기 창
- 닫기 X
- 돋보기
- 아이템명 ROI

후보가 실제 정답을 포함하지 않으면 직접 영역 지정이 가능해야 한다.

실제로 검출 대상이 없어야 하는 경우 `없음`을 명시적으로 기록할 수 있어야 한다.

## 8. 저장된 Case 재교정

`교정 데이터 관리`에서 기존 Scanner Case를 다시 열어 같은 교정 편집기로 재검토할 수 있어야 한다.

복원 가능한 경우 다음 증거를 재사용한다.

- `case.json`
- `full.png`
- `candidate_selection.json`
- 기존 ground truth item/text
- 기존 candidate 선택

재저장은 같은 Case ID를 유지하며 해당 Case의 reviewed Ground Truth를 갱신한다.

읽기/복원 실패 시 기존 데이터는 fail-closed로 보존한다.

## 9. 인식 알고리즘 비변경 계약

v1.6.0 UX 작업 때문에 Scanner identity safety threshold를 완화하지 않는다.

고정 계약:

- false positive보다 miss 선호
- structural floor `0.34`
- `HEADER_FRAME_LOCKED >= 0.68`
- valid magnifier + red close-X 필수
- continuous candidate cap `8`
- one-shot candidate cap `12`
- current official catalog가 identity authority
- production OCR field는 item-name only
- scan-time network 금지
- game memory read 금지
- DLL injection 금지
- packet interception 금지
- cross-frame OCR cache 금지

## 10. 배포 패키지 계약

정식 사용자 배포 ZIP 이름은 버전 번호와 분리한다.

GitHub Release는 비영문/특수문자 asset filename을 정규화하므로 release asset 자체는 version-independent ASCII 이름을 사용한다. 사용자에게 압축 해제되는 제품 폴더와 실행 파일의 한국어 이름은 유지한다.

- 파일명: `Junhyun-Helper.zip`
- 압축 내부 최상위 폴더: `준현 헬퍼/`
- 실행 파일: `준현 헬퍼/준현 헬퍼.exe`

버전은 다음에 존재한다.

- Desktop project version
- EXE ProductVersion
- Git tag
- GitHub Release metadata / notes

사용자가 새 버전마다 압축 해제 폴더 이름을 바꿔야 하는 구조를 만들지 않는다.

CI는 실제 release ZIP을 생성한 뒤 최상위 폴더 구조와 필수 파일을 검증해야 한다.

### v1.5.0 → v1.6.0 전환 호환성

공개 v1.5.0 updater는 `Junhyun-Helper-vX.Y.Z-win-x64.zip` 이름과 archive-root product layout만 이해한다. 이미 배포된 updater는 수정할 수 없으므로 v1.6.0에 한해 다음 transition bridge asset을 함께 게시한다.

- `Junhyun-Helper-v1.6.0-win-x64.zip`

일반 사용자용 권위 asset은 `Junhyun-Helper.zip`이며 bridge는 v1.5.0 자동 업데이트 호환성만 담당한다.

v1.6.0 updater는 새 Korean stable package를 우선 선택하고 `준현 헬퍼/` wrapper를 staging root로 안전하게 unwrap한다. 전환 기간을 위해 legacy versioned package도 fallback으로 읽을 수 있다.

`SHA256SUMS.txt`는 stable/bridge package를 모두 포함하며, checksum parser는 공백이 포함된 stable filename 전체를 정확히 비교해야 한다.

v1.6.0 이후 release는 old v1.5 updater를 위한 bridge asset을 계속 만들 필요가 없다. 이 예외를 영구 package contract로 확대하지 않는다.

## 11. 검증 gate

v1.6.0 release candidate는 최소 다음을 통과해야 한다.

- Desktop Release build
- 전체 자동 테스트
- Windows x64 self-contained single-file publish
- ProductVersion / FIRST_RUN identity audit
- Product UI smoke
- Scanner normal surface smoke
- Scanner settings schema v6 smoke
- Mini Scanner fixed identity/order smoke
- Map / Factory / MiniMap smoke
- graceful shutdown
- clean portable root
- stable `Junhyun-Helper.zip` 생성 및 내부 `준현 헬퍼/` 경로 검증
- v1.5 updater bridge ZIP 생성 및 legacy root 구조 검증
- stable/bridge package SHA256SUMS 일치 검증
- 공개 release 후 anonymous/public redownload 검증

## 12. 다음 개발 원칙

v1.6.0 이후 Scanner 핵심 과제는 새 UI 기능 추가가 아니라 실제 Tarkov 사용에서 Ground Truth를 축적하는 것이다.

실패를 발견하면 반드시 실패 stage를 분류하고 해당 stage만 수정한다.

threshold 완화나 전역 문자 강제 치환으로 빠르게 맞추는 방식은 사용하지 않는다.
