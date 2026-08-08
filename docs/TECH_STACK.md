# TECH STACK — 준현 헬퍼 기술 스택

결정일: **2026-08-08**

상태: `CONFIRMED — 초기 핵심 구현`

기술 선택 기준은 최신 유행보다 **준현 헬퍼의 기능을 가장 단순하고 직접적으로 구현할 수 있는가**입니다.

---

# 1. 애플리케이션 플랫폼

## C# + .NET 10 LTS

선택 이유:

- Windows 데스크톱 앱과 파일/프로세스/전역 단축키/오버레이 등 향후 기능에 자연스럽게 접근 가능
- 강한 정적 타입으로 외부 API → 내부 모델 변환 계약을 명확히 표현 가능
- `System.Text.Json`, `HttpClient`, 파일/해시/비동기 처리 등 필요한 기능을 기본 플랫폼에서 제공
- .NET 10은 현재 LTS이며 장기 유지보수에 적합
- 기존 Tarkov-Helper가 C#이었다는 이유가 아니라 새 제품의 Windows 중심 요구와 유지보수성을 기준으로 선택

---

# 2. UI

## WPF

선택 이유:

- 준현 헬퍼는 Windows용 데스크톱 프로그램이며 웹/크로스플랫폼 UI가 필요하지 않음
- 표/목록/필터/설정/별도 오버레이 창 등 필요한 UI를 직접 구현 가능
- 향후 미니맵/투명 창/클릭 투과/전역 단축키 같은 Windows 기능과 연결하기 용이
- 안정적이고 오랫동안 검증된 기술이며 .NET 10에서도 계속 지원/개선됨
- Electron/WebView 기반 UI처럼 별도 웹 런타임과 프론트엔드 생태계를 추가하지 않아도 됨

주의:

WPF를 사용한다는 것이 기존 Tarkov-Helper의 거대한 code-behind 구조를 승계한다는 뜻은 아닙니다.

화면은 기능별 View/ViewModel로 분리하고 게임 규칙과 DB/API 처리는 UI 바깥에 둡니다.

---

# 3. 로컬 데이터베이스

## SQLite + Microsoft.Data.Sqlite

두 개의 논리적으로 독립된 DB를 기본으로 합니다.

### `content.db`

- 온라인 데이터에서 재생성 가능한 Game Content
- Quest / Item / Trader / MapReference / Hideout / Ammo / Acquisition
- 데이터 업데이트 실패 시 교체되지 않음
- 필요하면 삭제 후 API에서 재구축 가능

### `user.db`

- 사용자의 실제 진행 상태
- GameProfile
- CompletedQuest
- HideoutLevel
- InventoryQuantity
- 필요한 TraderProgress

`content.db` 업데이트가 `user.db`를 수정하지 않습니다.

## ORM을 초기에는 사용하지 않음

초기 DB 규모와 쿼리는 단순하므로 `Microsoft.Data.Sqlite`를 직접 사용합니다.

이유:

- 별도 ORM 상태 추적/마이그레이션 계층 불필요
- content DB는 부분 migration보다 재구축이 기본
- 실제 SQL과 저장 책임을 명확히 유지 가능
- 의존성 감소

쿼리가 실제로 복잡해져 반복 코드가 문제가 될 때만 작은 데이터 접근 라이브러리 도입을 재검토합니다.

---

# 4. JSON / HTTP

추가 라이브러리를 기본으로 도입하지 않습니다.

- HTTP: `HttpClient`
- JSON: `System.Text.Json`
- Hash: .NET 기본 cryptography API
- 파일: `System.IO`

외부 API DTO는 Infrastructure 영역에만 존재합니다.

Domain/Core 프로젝트는 `json.tarkov.dev`의 필드 이름을 몰라야 합니다.

---

# 5. 테스트

## xUnit

테스트를 세 종류로 나눕니다.

### Unit / Domain

인터넷/DB 없이 순수 계산 검증.

예:

- Quest availability
- Needed item 집계
- FIR 계산

### Contract Fixture

저장된 작은 JSON fixture → Importer → canonical model 검증.

외부 API의 현재 shape를 우리가 어떻게 해석하는지 고정합니다.

### Live Contract

실제 `json.tarkov.dev`를 호출하여 현재 shape가 지원 범위 안인지 검사합니다.

일반 Unit Test와 분리하여 외부 네트워크 장애가 순수 로직 테스트를 실패시키지 않게 합니다.

---

# 6. 프로젝트 구조

초기 솔루션은 **3개의 제품 프로젝트 + 1개의 테스트 프로젝트**를 넘기지 않는 것을 기본으로 합니다.

```text
JunhyunHelper.sln

src/
  JunhyunHelper.Core/
  JunhyunHelper.Infrastructure/
  JunhyunHelper.Desktop/

tests/
  JunhyunHelper.Tests/
```

## `JunhyunHelper.Core`

순수한 제품 의미와 계산만 둡니다.

- canonical models
- Quest availability
- Hideout requirement calculation
- Needed item calculation
- Ammo domain representation

금지:

- WPF
- SQLite
- HTTP
- 파일 경로
- json.tarkov.dev DTO

## `JunhyunHelper.Infrastructure`

외부 세계와의 연결입니다.

- json.tarkov.dev client
- endpoint DTO/parser/importer
- content validation
- SQLite repositories
- content candidate build/activation
- 파일/manifest 처리

Core를 참조할 수 있지만 Core는 Infrastructure를 참조하지 않습니다.

## `JunhyunHelper.Desktop`

WPF 앱입니다.

- View
- ViewModel
- 사용자 명령을 Core/Infrastructure에 전달
- composition root

게임 규칙을 화면에 다시 구현하지 않습니다.

## `JunhyunHelper.Tests`

Core/Infrastructure를 검증합니다.

별도의 smoke/utility 프로젝트를 기능마다 만들지 않습니다. 실제로 독립 실행 도구가 필요한 이유가 생기기 전까지 테스트 프로젝트 하나에서 관리합니다.

---

# 7. 의존 방향

```text
Desktop ───────► Core
   │              ▲
   └────► Infrastructure
               │
               └────► Core

Core ─────X────► Desktop/Infrastructure
```

Core가 가장 안쪽이며 어떤 UI/DB/API 기술에도 종속되지 않습니다.

---

# 8. 의존성 예산

초기 핵심 구현에서 외부 NuGet 의존성은 필요한 최소만 허용합니다.

기본 후보:

- `Microsoft.Data.Sqlite`
- xUnit 관련 테스트 패키지

MVVM toolkit, DI container, logging framework, retry library, ORM 등은 **필요가 실제로 발생했을 때** 도입합니다.

특히 다음을 금지합니다.

- 단순 constructor 연결을 위해 DI container부터 추가
- HTTP 재시도 몇 줄을 위해 대형 resilience stack 추가
- 단순 CRUD를 위해 ORM + repository abstraction 여러 겹 생성
- 모든 클래스에 interface를 미리 생성
- 하나의 구현만 존재하는데 factory/provider/manager를 연속으로 추가

---

# 9. 객체 생성/연결

초기에는 `Desktop`의 한 composition root에서 필요한 객체를 명시적으로 생성합니다.

예시 개념:

```text
HttpClient
  → TarkovJsonClient
  → ContentImporter
  → ContentUpdater

content.db
  → ContentRepository

user.db
  → UserProgressRepository

Repositories + Core calculators
  → ViewModels
```

객체 관계를 한 곳에서 보면 프로그램 전체 구성을 쉽게 파악할 수 있어야 합니다.

규모가 실제로 커져 수동 연결이 문제가 될 때만 DI container를 검토합니다.

---

# 10. 로깅

초기에는 단순한 애플리케이션 로그만 둡니다.

필요한 핵심 이벤트:

- 앱 시작/종료
- content update 시작/성공/실패
- source endpoint 실패
- validation Fatal/Warning
- candidate activation/rollback
- 예상하지 못한 예외

정상적인 화면 클릭이나 계산마다 로그를 남기지 않습니다.

---

# 11. 현재 선택하지 않는 기술

- Electron
- Tauri/web frontend
- WinUI 3
- Avalonia
- Entity Framework Core
- 별도 backend/web server
- 클라우드 DB
- microservice
- event bus
- CQRS/MediatR
- 범용 rule engine

이 기술들이 나쁘기 때문이 아니라 **현재 준현 헬퍼에 필요하지 않기 때문**입니다.

---

# 12. 재검토 조건

기술 선택은 목적이 아니라 도구입니다.

다음 상황이 실제로 발생하면 재검토할 수 있습니다.

- WPF가 확정 UX를 구현하는 데 실질적 제약이 있음
- SQLite 단순 접근으로 유지보수가 오히려 어려워짐
- 수동 composition이 실제 규모에서 반복 오류를 만듦
- 별도 프로세스/도구가 필요한 명확한 기능이 생김

그 전에는 구조를 미리 키우지 않습니다.
