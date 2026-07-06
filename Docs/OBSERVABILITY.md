# 로깅과 예외 처리

이 문서는 서버가 운영 중 문제를 추적할 수 있도록 어떤 로그와 오류 응답을 남기는지 설명합니다.

## 1. 목적

방치형 게임 서버는 재화, 성장, 진행 상태를 서버가 권위 있게 판단합니다. 그래서 실패 상황이 생겼을 때 클라이언트에 내부 정보를 노출하지 않으면서도 서버 로그에서는 원인을 추적할 수 있어야 합니다.

## 2. 전역 예외 처리

예상 가능한 실패는 각 Endpoint가 직접 처리합니다.

- 멱등 키 누락: `400 ProblemDetails`
- 플레이어 상태 없음: `404 ProblemDetails`
- 멱등 키 충돌: `409 ProblemDetails`
- 저장 충돌 재시도 초과: `503 ProblemDetails`

예상하지 못한 예외는 `GlobalExceptionHandler`가 마지막 방어선으로 처리합니다.

```json
{
  "title": "An unexpected server error occurred.",
  "status": 500,
  "detail": "Contact support with the traceId if the problem persists.",
  "traceId": "..."
}
```

Development 환경에서는 학습과 디버깅 편의를 위해 `detail`에 예외 메시지를 포함합니다. 운영 환경에서는 내부 구현이 새지 않도록 추적 ID 안내만 반환합니다.

## 3. traceId

`traceId`는 클라이언트 오류 화면, 서버 로그, 테스트 실패를 연결하는 실마리입니다.

클라이언트는 500 오류를 받으면 사용자에게 재시도 안내를 보여 주고, 개발자용 로그에는 `traceId`를 함께 남기면 됩니다.

## 4. 요청 로깅

서버는 ASP.NET Core `HttpLogging`으로 다음 값만 기록합니다.

- HTTP Method
- Request Path
- Response Status Code
- Duration

요청 Body, 응답 Body, Authorization 헤더는 기록하지 않습니다. JWT와 플레이어 데이터가 로그에 남는 것을 피하기 위해서입니다.

## 5. 구현 위치

- `src/IdleGuild.Api/ErrorHandling/GlobalExceptionHandler.cs`: 처리되지 않은 예외를 500 `ProblemDetails`로 변환합니다.
- `src/IdleGuild.Api/Program.cs`: `AddExceptionHandler`, `AddProblemDetails`, `AddHttpLogging`, `UseExceptionHandler`, `UseHttpLogging`을 연결합니다.
- `tests/IdleGuild.Api.Tests/ErrorHandlingTests.cs`: 강제로 예외를 발생시켜 500 `ProblemDetails`와 `traceId`를 검증합니다.
