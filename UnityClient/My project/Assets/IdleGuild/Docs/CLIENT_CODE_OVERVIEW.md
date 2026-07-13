# Idle Guild Unity 클라이언트 학습 가이드

이 문서는 Unity를 처음 사용하는 개발자가 Idle Guild 클라이언트를 직접 실행하고, 코드를 읽고, 이후 기능을 함께 확장할 수 있도록 만든 누적 학습 문서입니다.

앞으로 기능 Step이 완료될 때마다 이 문서에 다음 내용을 추가합니다.

- 이번 Step에서 만든 결과
- 새로 배울 Unity/C# 개념
- 변경된 파일과 각 파일의 책임
- Unity Editor에서 직접 확인하는 방법
- 실습 과제와 정상 동작 기준
- 발생할 수 있는 오류와 해결 방법

## 1. 현재 프로젝트 한눈에 보기

| 항목 | 현재 값 |
| --- | --- |
| Unity 버전 | Unity 6 `6000.3.19f1` |
| 프로젝트 위치 | `UnityClient/My project` |
| 메인 씬 | `Assets/IdleGuild/Scenes/MainScene.unity` |
| 시작 스크립트 | `Assets/IdleGuild/Scripts/UnityClientBootstrap.cs` |
| 기본 실행 모드 | 서버가 필요 없는 Mock API 모드 |
| 실제 서버 주소 | `http://localhost:5219` |
| 입력 시스템 | Unity Input System |
| UI 시스템 | uGUI (`Canvas`, `Button`, `Text`, `InputField`) |

현재 구현된 기능은 다음과 같습니다.

1. 게스트 로그인
2. 게임 상태 조회
3. 방치 보상 수령
4. 메인 영웅 강화
5. 스테이지 도전
6. 청동 검 장착
7. 테스트용 골드 상품 구매
8. 임시 도트 영웅과 슬라임의 전투 연출
9. 서버 없이 전체 흐름을 시험하는 Mock API

현재 화면은 완성된 게임 UI가 아니라 서버 기능과 게임 흐름을 검증하는 학습용 프로토타입입니다. 스테이지 도전 결과에 따라 실제 PNG Sprite Sheet의 도트 캐릭터가 이동하고 공격합니다. 프레임 재생은 학습을 위해 Coroutine으로 구현했으며, Animator 기반 상태 전환은 다음 단계에서 발전시킵니다.

## 2. Unity Editor에서 알아야 할 화면

Unity 프로젝트를 열면 우선 아래 다섯 영역을 구분하면 됩니다.

| 창 | 역할 | 이 프로젝트에서 하는 일 |
| --- | --- | --- |
| Hierarchy | 현재 씬에 들어 있는 GameObject 목록 | `Main Camera`, `Unity Client Bootstrap` 확인 |
| Scene | 게임 공간을 편집하는 화면 | 카메라와 오브젝트 위치 확인 |
| Game | 플레이어가 실제로 보게 될 화면 | Play 후 UI와 전투 연출 확인 |
| Inspector | 선택한 오브젝트의 컴포넌트와 설정 | Mock API 사용 여부와 서버 주소 변경 |
| Project | 프로젝트의 폴더와 파일 | `Assets/IdleGuild` 아래 씬과 스크립트 열기 |
| Console | 로그와 오류 표시 | API 결과와 예외 확인 |

Unity에서 가장 중요한 기본 구조는 다음과 같습니다.

- **Scene**은 하나의 게임 화면 또는 공간입니다. 현재는 `MainScene` 하나를 사용합니다.
- **GameObject**는 씬에 존재하는 기본 단위입니다. 이름만 있는 빈 오브젝트에도 여러 기능을 붙일 수 있습니다.
- **Component**는 GameObject에 붙는 기능입니다. `Camera`, `Transform`, `MonoBehaviour` 스크립트가 모두 컴포넌트입니다.
- **Transform**은 위치, 회전, 크기와 부모-자식 관계를 보관하며 모든 GameObject가 가집니다.
- **Inspector**의 `[SerializeField]` 항목은 코드를 수정하지 않고 값을 바꾸는 통로입니다.

## 3. 서버 없이 처음 실행하기

Mock API가 기본값이므로 서버 프로그램을 켜지 않아도 됩니다.

1. Unity Hub에서 `UnityClient/My project`를 엽니다.
2. Project 창에서 `Assets/IdleGuild/Scenes/MainScene`을 더블 클릭합니다.
3. Hierarchy에서 `Unity Client Bootstrap`을 선택합니다.
4. Inspector에서 `Use Mock Api`가 체크되어 있는지 확인합니다.
5. Editor 위쪽의 Play 삼각형 버튼을 누릅니다.
6. Game 탭으로 이동합니다.
7. `Run Demo Flow`를 누르거나 번호가 붙은 버튼을 순서대로 누릅니다.

수동 실습 권장 순서는 다음과 같습니다.

1. `Check Server`: Mock 시스템 상태가 `mock-ok`인지 확인합니다.
2. `1. Guest Login`: 임시 플레이어 ID와 토큰을 만듭니다.
3. `2. State`: 골드 20, 영웅 레벨 1, 최고 스테이지 1을 확인합니다.
4. `3. Claim`: 골드가 100 이상 증가하는지 확인합니다.
5. `4. Upgrade`: 골드를 사용해 영웅 레벨과 전투력이 증가하는지 확인합니다.
6. `6. Equip Bronze`: 장비 전투력 보너스가 5 증가하는지 확인합니다.
7. Stage 입력에 `2`를 넣고 `5. Challenge Stage`를 누릅니다.
8. 영웅이 슬라임 앞으로 이동해 공격하고 결과에 따라 승리 또는 후퇴하는지 확인합니다.

Mock 데이터는 Play를 시작할 때 메모리에 새로 만들어집니다. Play를 중지하면 골드, 레벨, 장비 상태는 초기화됩니다. 로그인 정보는 `PlayerPrefs`에 저장되므로 `Clear Saved Session`으로 지울 수 있습니다.

## 4. 실제 서버에 연결하기

실제 서버 API를 시험할 때만 다음과 같이 전환합니다.

1. 서버를 `http://localhost:5219`에서 실행합니다.
2. Hierarchy에서 `Unity Client Bootstrap`을 선택합니다.
3. Inspector에서 `Use Mock Api` 체크를 해제합니다.
4. 서버 포트가 다르면 `Api Base Url`을 실제 주소로 바꿉니다.
5. Play 후 `Check Server`를 먼저 눌러 연결을 확인합니다.
6. `Guest Login`부터 기능을 순서대로 실행합니다.

`Use Mock Api` 값은 `UnityClientBootstrap`의 `[SerializeField] private bool useMockApi`와 연결됩니다. 체크 상태에 따라 같은 기능 인터페이스를 구현한 두 객체 중 하나를 선택합니다.

```text
UnityClientBootstrap
        |
        v
IIdleGuildApiClient
   |             |
   v             v
Mock API      실제 HTTP API
(메모리)      (UnityWebRequest)
```

이 구조 덕분에 버튼과 게임 흐름 코드는 그대로 두고 데이터 공급자만 교체할 수 있습니다.

## 5. 코드 구조와 파일별 책임

| 파일 | 책임 | 먼저 읽을 부분 |
| --- | --- | --- |
| `UnityClientBootstrap.cs` | 씬 진입, 버튼 동작, 기능 실행 순서, 상태와 로그 관리 | `Awake()`, `RunDemoFlow()` |
| `IdleGuildApiClient.cs` | 실제 서버 HTTP 요청, 인증/멱등 헤더, JSON 변환, 오류 처리 | `IIdleGuildApiClient`, `Send()` |
| `IdleGuildMockApiClient.cs` | 서버 없이 동작하는 메모리 기반 가짜 서버 | `CreateGameState()` |
| `IdleGuildApiModels.cs` | 서버 JSON과 C# 객체 사이의 데이터 모양(DTO) 정의 | `GameStateResponse` |
| `IdleGuildSession.cs` | 토큰과 플레이어 ID를 `PlayerPrefs`에 저장/복원 | `Load()`, `Save()`, `Clear()` |
| `IdleGuildRuntimeUi.cs` | 실행 중 Canvas, 텍스트, 버튼, 입력 필드를 코드로 생성 | `Build()`, `Refresh()` |
| `IdleGuildGameWorld.cs` | 배경, 땅, 도트 캐릭터 생성과 전투 코루틴 재생 | `Build()`, `PlayCombat()` |
| `Resources/Sprites/*.png` | 영웅과 슬라임의 4x4 애니메이션 Sprite Sheet | 각 행의 Idle/Run/Attack/Hit 프레임 |

### 전체 실행 흐름

```text
MainScene 로드
  -> UnityClientBootstrap.Awake()
  -> 저장된 Session 불러오기
  -> Mock 또는 실제 ApiClient 선택
  -> Runtime UI 생성
  -> 도트 Game World 생성
  -> 버튼 클릭
  -> Bootstrap의 Coroutine 실행
  -> API 결과를 DTO로 받기
  -> 상태와 로그 갱신
  -> 필요하면 전투 연출 실행
```

## 6. 이번 프로젝트에서 배우는 C#과 Unity 개념

### MonoBehaviour와 Awake

`UnityClientBootstrap`은 `MonoBehaviour`를 상속합니다. 그래서 씬의 GameObject에 컴포넌트로 붙을 수 있고 Unity 생명주기 함수를 사용할 수 있습니다.

`Awake()`는 오브젝트가 준비될 때 Unity가 자동 호출합니다. 현재 프로젝트에서는 세션, API, UI, 게임 월드를 초기화합니다. 일반 C# 프로그램의 `Main()`과 비슷한 진입 역할이지만 Unity가 호출 시점을 관리한다는 차이가 있습니다.

### Coroutine과 IEnumerator

서버 통신과 전투 애니메이션은 즉시 끝나지 않습니다. Unity의 Coroutine은 여러 프레임에 걸쳐 작업을 이어 가게 해 줍니다.

```csharp
yield return MoveTo(hero, attackPoint, 0.55f);
yield return new WaitForSeconds(0.08f);
```

첫 줄은 영웅 이동이 끝날 때까지 기다리고, 두 번째 줄은 0.08초 기다린 뒤 다음 코드를 실행합니다. 이 동안 게임 화면 전체가 멈추지는 않습니다.

### Interface와 구현 교체

`IIdleGuildApiClient`는 API 클라이언트가 반드시 제공해야 하는 기능 목록입니다. 실제 서버용 `IdleGuildApiClient`와 오프라인용 `IdleGuildMockApiClient`가 같은 인터페이스를 구현합니다.

이 패턴은 테스트할 때 매우 유용합니다. 게임 로직은 “어느 서버인지” 알 필요 없이 `api.GetGameState()`처럼 같은 방식으로 호출할 수 있습니다.

### DTO와 JSON

DTO(Data Transfer Object)는 서버와 주고받는 데이터 전용 클래스입니다. 예를 들어 서버의 게임 상태 JSON은 `GameStateResponse`로 변환됩니다.

```text
서버 JSON -> GameStateResponse -> Bootstrap 상태 -> UI Text
```

DTO에는 게임 행동을 넣지 않고 데이터 필드만 두는 것이 현재 구조의 원칙입니다.

### PlayerPrefs

`PlayerPrefs`는 작은 값을 로컬에 저장하는 Unity 기능입니다. 이 프로젝트에서는 게스트 토큰과 플레이어 ID만 저장합니다. 대규모 게임 상태나 중요한 서버 데이터를 저장하는 데이터베이스는 아닙니다.

### Runtime UI

현재 Canvas와 버튼은 Scene에서 미리 편집한 Prefab이 아니라 `IdleGuildRuntimeUi.Build()`가 Play 중 생성합니다. 코드 흐름을 한 파일에서 확인하기 쉽지만, 실제 게임 UI가 커지면 Unity Editor에서 Prefab과 Canvas를 구성하는 방식으로 옮길 예정입니다.

### Sprite와 픽셀 텍스처

`IdleGuildGameWorld`는 `Resources/Sprites`의 1024x1024 PNG를 불러옵니다. 각 이미지는 4열x4행이고, 코드는 각 셀을 하나의 Sprite로 잘라 16개 프레임 배열을 만듭니다.

| 행 | 애니메이션 | 프레임 |
| --- | --- | --- |
| 1 | Idle | 4 |
| 2 | Run | 4 |
| 3 | Attack | 4 |
| 4 | Hit | 4 |

Texture 좌표는 왼쪽 아래가 원점이지만, 사람이 보는 이미지의 첫 번째 행은 위쪽입니다. 따라서 `LoadSpriteSheet()`는 Y 좌표를 뒤집어 위쪽 Idle 행부터 배열에 저장합니다.

`Resources.Load<Texture2D>("Sprites/hero-spritesheet")`처럼 확장자를 제외한 경로로 이미지를 읽습니다. `Resources`는 작은 학습 프로젝트에서 편리하지만 모든 포함 애셋을 빌드에 넣기 때문에, 프로젝트가 커지면 Inspector 참조나 Addressables로 교체하는 것이 좋습니다.

`FilterMode.Point`는 확대해도 픽셀이 흐려지지 않게 합니다. PNG를 찾지 못하면 기존의 16x16 코드 생성 Sprite를 fallback으로 사용하므로 게임 흐름 자체는 계속 시험할 수 있습니다.

이 방식은 프로토타입에는 적합하지만 최종 도트 캐릭터는 PNG/Aseprite 애셋, Sprite Editor, Animation Clip, Animator Controller를 사용하는 편이 좋습니다.

## 7. 서버 API 기능 흐름

| 버튼 | 클라이언트 호출 | 실제 서버 API |
| --- | --- | --- |
| `Check Server` | `GetSystemStatus` | `GET /api/v1/system/status` |
| `1. Guest Login` | `GuestLogin` | `POST /api/v1/accounts/guest` |
| `2. State` | `GetGameState` | `GET /api/v1/game-state` |
| `3. Claim` | `ClaimIdleReward` | `POST /api/v1/rewards/idle/claim` |
| `4. Upgrade` | `UpgradeMainHero` | `POST /api/v1/heroes/main/upgrade` |
| `5. Challenge Stage` | `ChallengeStage` | `POST /api/v1/stages/{stage}/challenge` |
| `6. Equip Bronze` | 목록 조회 후 장착 | `GET /equipment`, `PUT /equipment/{id}/equipped` |
| `7. Buy 100 Gold (Mock)` | 상품 조회 후 구매 | `GET /shop/products`, `POST /shop/products/{id}/purchase` |

상태 변경 API에는 `Idempotency-Key`를 보냅니다. 네트워크 문제로 같은 요청이 재전송되어도 보상이나 구매가 두 번 처리되지 않도록 서버가 요청을 구분하는 값입니다.

보호 API에는 로그인 후 받은 토큰을 다음 HTTP 헤더에 넣습니다.

```text
Authorization: Bearer <accessToken>
```

## 8. 자주 만나는 오류와 해결

### `Arial.ttf is no longer a valid built in font`

Unity 6에서는 예전 기본 폰트 이름인 `Arial.ttf`를 사용할 수 없습니다. 현재 UI 코드는 `LegacyRuntime.ttf`를 사용해야 합니다.

### `trying to read Input using UnityEngine.Input`

프로젝트가 새 Input System을 사용 중인데 구형 `StandaloneInputModule`을 사용하면 발생합니다. 현재 코드는 EventSystem에 `InputSystemUIInputModule`을 붙입니다.

### 버튼이 비활성화됨

`State`, `Claim`, `Upgrade`, `Challenge` 같은 보호 기능은 게스트 로그인 토큰이 있어야 활성화됩니다. 먼저 `Guest Login`을 누릅니다. 요청 중에는 중복 실행을 막기 위해 버튼이 잠시 비활성화됩니다.

### 실제 서버 연결 실패

`Use Mock Api`가 꺼져 있는지, 서버가 실행 중인지, `Api Base Url` 포트가 서버 주소와 같은지 확인합니다. Console과 화면 로그에 표시되는 HTTP 상태 및 Trace ID도 함께 확인합니다.

## 9. Step별 학습 기록

### Step 1: Unity 클라이언트 기반 구조와 서버 연결

- `MainScene`과 `UnityClientBootstrap`을 클라이언트 진입점으로 구성했습니다.
- `UnityWebRequest`로 서버 API를 호출하는 클라이언트를 만들었습니다.
- 게스트 로그인부터 스테이지 도전까지 MVP 흐름을 연결했습니다.
- 학습 개념: Scene, GameObject, Component, MonoBehaviour, Coroutine, HTTP/JSON.

### Step 2: 세션과 상태 변경 요청 안정화

- 로그인 토큰과 플레이어 ID를 `PlayerPrefs`에 저장합니다.
- 인증 헤더, 멱등 키, 오류 응답, Trace ID를 처리합니다.
- 장비와 테스트 상점 기능을 추가했습니다.
- 학습 개념: 클라이언트 세션, 서버 권위 상태, 멱등성, 오류 처리.

### Step 3: 런타임 UI와 Unity 6 호환성

- 실행 중 Canvas와 조작 버튼을 생성합니다.
- Unity 6 기본 폰트를 `LegacyRuntime.ttf`로 사용합니다.
- 새 Input System용 `InputSystemUIInputModule`을 사용합니다.
- 학습 개념: Canvas, uGUI, EventSystem, Input System, Layout Group.

### Step 4: 기본 2D 전투 미리보기

- 코드로 임시 픽셀 영웅과 슬라임을 생성합니다.
- 스테이지 도전 시 이동, 공격, 승리/실패 연출을 재생합니다.
- 학습 개념: SpriteRenderer, Texture2D, 좌표, 프레임, Coroutine 애니메이션.

### Step 5: 서버 없는 Mock API 모드

- `IIdleGuildApiClient` 인터페이스로 API 계약을 분리했습니다.
- 메모리에서 골드, 레벨, 스테이지, 장비를 계산하는 Mock API를 추가했습니다.
- Inspector의 `Use Mock Api` 체크로 실제 서버와 Mock을 전환합니다.
- 학습 개념: Interface, 의존성 교체, 테스트 더블, 인메모리 상태.

### Step 6: 누적 학습 문서 도입

- 현재 구현과 일치하도록 프로젝트 구조와 실행 방법을 다시 정리했습니다.
- Unity Editor를 처음 사용하는 사람을 위한 기본 용어와 실습 순서를 추가했습니다.
- 앞으로 모든 기능 Step은 이 문서의 학습 기록에 계속 추가합니다.
- 학습 개념: 프로젝트 구조 읽기, 실행 흐름 추적, 검증 기준 세우기.

### Step 7: 실제 PNG Sprite Sheet와 프레임 애니메이션

- 영웅과 슬라임의 4열x4행 도트 Sprite Sheet를 추가했습니다.
- 마젠타 생성 배경을 투명 알파로 변환하고 1024x1024 PNG로 정리했습니다.
- `Resources.Load`로 Texture를 불러와 런타임에 16개 Sprite 프레임으로 나눕니다.
- 평상시에는 Idle, 이동 중에는 Run, 공격 시 Attack, 피격 시 Hit 행을 재생합니다.
- 이미지가 없을 때는 기존 코드 생성 Sprite를 사용하는 fallback을 유지합니다.
- 학습 개념: PNG alpha, Resources, Texture2D, Sprite Sheet, UV 좌표, Point Filter, 프레임 애니메이션.

직접 확인 방법:

1. Unity가 열려 있다면 이미지 Import가 끝날 때까지 기다립니다.
2. `MainScene`을 열고 Play를 누릅니다.
3. 영웅과 슬라임이 가만히 있을 때도 조금씩 움직이는지 확인합니다.
4. Mock 모드에서 `Guest Login`, `Claim`, `Upgrade`를 순서대로 실행합니다.
5. Stage `2`에 도전합니다.
6. 영웅이 Run 동작으로 이동하고 Attack 동작으로 공격하는지 확인합니다.
7. 승리하면 슬라임의 Hit 동작 후 사라지고, 실패하면 영웅의 Hit 동작이 나오는지 확인합니다.

정상 동작 기준:

- 캐릭터 주위에 마젠타 사각형 배경이 보이지 않아야 합니다.
- 확대된 도트 캐릭터가 흐릿하지 않아야 합니다.
- 한 캐릭터의 프레임이 다른 캐릭터 프레임과 섞이지 않아야 합니다.
- 전투를 여러 번 실행해도 이전 애니메이션 Coroutine이 겹치지 않아야 합니다.

## 10. 다음 학습 Step

다음 단계에서는 현재 Coroutine 프레임 재생을 Unity의 정식 Animator 구조로 발전시킵니다.

1. Sprite Editor와 현재 런타임 분할 방식의 차이를 배웁니다.
2. Idle, Run, Attack, Hit Animation Clip을 만듭니다.
3. Animator Controller와 상태 전환 파라미터를 구성합니다.
4. 현재 Coroutine 이동과 서버의 스테이지 결과를 Animator에 연결합니다.
5. 전투 상태를 문자열 대신 enum으로 정리합니다.

이 Step부터는 Unity Editor에서 직접 만드는 작업과 C# 코드 작업을 함께 진행합니다. 각 작업 후에는 이 문서의 Step 기록, 직접 실행 절차, 정상 동작 기준을 함께 갱신합니다.

## 11. 매 Step 완료 체크리스트

앞으로 Step 완료 시 아래 항목을 기준으로 마무리합니다.

- [ ] 기능이 코드에 구현됨
- [ ] Unity Editor에서 확인하는 절차가 문서화됨
- [ ] 새 Unity/C# 개념이 초보자 관점으로 설명됨
- [ ] 변경 파일과 데이터 흐름이 문서화됨
- [ ] 가능한 범위의 컴파일/동작 검증이 완료됨
- [ ] `CLIENT_CODE_OVERVIEW.md`의 Step 기록이 갱신됨
- [ ] 해당 Step을 별도 git commit으로 기록함
- [ ] 원격 `main` 브랜치에 push함
