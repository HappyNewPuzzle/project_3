# Unity 클라이언트 서버 연동

## 1. 현재 단계

Unity 6000.3.19f1 Universal 2D 프로젝트가 ASP.NET Core 서버의 핵심 API를 호출합니다. 현재 화면은 게임 아트 이전의 개발용 런타임 UI이며, 서버 계약과 전체 게임 루프를 먼저 검증하는 목적입니다.

구현된 흐름:

- 서버 상태 확인
- 게스트 로그인과 JWT 세션 저장
- 게임 상태와 서버 전투력 표시
- 방치 보상 수령
- 영웅 강화
- 스테이지 도전
- 보유 장비 조회 후 Bronze Sword 장착
- 모의 상점 상품 조회 후 Small Gold Pack 구매
- HTTP 실패 시 Trace ID 표시

## 2. 실행 순서

### 서버

프로젝트 루트 PowerShell에서 PostgreSQL, Migration, API 순서로 실행합니다. 자세한 명령은 [README](../README.md)의 서버 실행 순서를 따릅니다.

서버가 실행된 상태에서 브라우저 또는 PowerShell로 `http://localhost:5219/health`가 `Healthy`인지 확인합니다.

### Unity

1. Unity Hub에서 `UnityClient/My project`를 엽니다.
2. `Assets/IdleGuild/Scenes/MainScene.unity`를 엽니다.
3. Hierarchy의 `Unity Client Bootstrap`을 선택합니다.
4. Inspector의 `Api Base Url`이 `http://localhost:5219`인지 확인합니다.
5. 상단 Play 버튼을 누릅니다.
6. `Check Server`를 눌러 연결을 확인합니다.
7. `1. Guest Login`으로 토큰을 만든 뒤 나머지 버튼을 사용합니다.

서버를 재시작해도 JWT 서명 키가 같으면 저장된 세션을 다시 사용할 수 있습니다. 401이 반복되면 `Clear Saved Session`을 누르고 새 게스트로 로그인합니다.

## 3. 버튼 의미

| 버튼 | 동작 |
| --- | --- |
| Check Server | 서버 UTC 상태 API 호출 |
| Run Demo Flow | 로그인부터 보상·강화·스테이지까지 자동 실행 |
| State | 골드, 레벨, 총 전투력, 장비 보너스, 최고 스테이지 조회 |
| Claim | 새 멱등 키로 방치 보상 수령 |
| Upgrade | 새 멱등 키로 주 영웅 강화 |
| Challenge Stage | 입력한 스테이지를 서버 전투력으로 판정 |
| Equip Bronze | 장비 목록에서 Bronze Sword를 찾아 장착 |
| Buy 100 Gold (Mock) | 서버 카탈로그 확인 후 테스트 골드 팩 구매 |

## 4. 코드 흐름

```text
IdleGuildRuntimeUi
    ↓ 버튼 callback
UnityClientBootstrap
    ↓ coroutine
IdleGuildApiClient
    ↓ UnityWebRequest / JSON / JWT / Idempotency-Key
ASP.NET Core API
    ↓ response DTO + X-Trace-Id
Bootstrap 상태·로그
    ↓ Refresh
Runtime UI
```

- `IdleGuildApiModels.cs`: 서버 JSON DTO와 성공·실패·Trace ID 결과
- `IdleGuildApiClient.cs`: HTTP 전송, JWT, JSON Body, 멱등 키와 오류 파싱
- `IdleGuildSession.cs`: PlayerPrefs 토큰과 플레이어 ID 저장
- `UnityClientBootstrap.cs`: 화면 행동과 API 코루틴 연결
- `IdleGuildRuntimeUi.cs`: 실행 시 생성되는 학습용 Canvas

## 5. Trace ID

서버는 모든 응답에 `X-Trace-Id`를 보냅니다. Unity API 결과가 이를 보존하며 실패 로그에 `(trace ...)`로 표시합니다. 서버 문제를 조사할 때 이 값을 [관측성 문서](OBSERVABILITY.md)의 구조화 로그에서 검색합니다. 토큰은 로그에 남기지 않습니다.

## 6. 현재 경계와 다음 Step

현재 클라이언트는 서버 기능을 조작하고 결과를 확인하는 개발용 화면까지 완성됐습니다. 다음 Step은 API를 더 추가하는 작업보다 실제 방치형 게임 화면을 만드는 작업입니다.

- 영웅과 몬스터 임시 2D 오브젝트 배치
- 전투 진행 애니메이션과 서버 결과 연출
- 골드 획득·강화 이펙트
- 런타임 생성 UI를 Prefab 또는 UI Toolkit 화면으로 분리
- 로딩, 재시도, 401 재로그인 UX

전투 승패와 보상량은 계속 서버 결과를 사용하고 Unity는 그 결과를 시각적으로 표현합니다.
