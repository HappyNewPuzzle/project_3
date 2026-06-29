# Idle Guild

서버 권위형 방치형 게임의 구조를 학습하고, 설계 과정과 테스트 근거를 함께 보여 주기 위한 포트폴리오 프로젝트입니다.

플레이어는 모험가 길드를 운영합니다. 영웅은 시간이 흐르는 동안 자원을 생산하고 스테이지를 진행하며, 플레이어는 획득한 골드로 영웅을 성장시킵니다.

## 목표

- 서버가 시간, 재화, 성장 결과를 검증하는 구조 구현
- 오프라인 보상과 중복 요청을 안전하게 처리
- 작은 기능을 테스트 가능한 단위로 완성
- Unity 클라이언트가 사용할 명확한 API 계약 제공

## 기술 구성

- Client: Unity / C# (서버 MVP 이후 별도 진행)
- Server: ASP.NET Core Web API
- Database: PostgreSQL
- ORM: Entity Framework Core
- Local environment: Docker Compose
- API documentation: OpenAPI (Swagger)
- Tests: xUnit 기반 단위 테스트 및 통합 테스트

## 문서

- [게임 설계](Docs/GAME_DESIGN.md)
- [서버 아키텍처](Docs/ARCHITECTURE.md)
- [개발 로드맵](Docs/ROADMAP.md)

## 현재 상태

Step 1: 게임의 MVP 범위와 서버 설계 원칙을 정의했습니다.
