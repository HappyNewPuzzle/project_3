# Idle Guild 최종 검증 기록

검증일: 2026-07-18

## 자동 검증 결과

- Docker Compose `api`: healthy
- Docker Compose `postgres`: healthy
- Release 서버 테스트: 116개 통과, 실패 0개
  - Domain: 39
  - Application: 22
  - Infrastructure: 14
  - API: 41
- Unity Android Player 스크립트 컴파일: 성공
- Android 최적화 APK: Unity `BuildOptimizedTestApk`로 생성
- 빌드 모드: Release, LZ4HC, Android, 실제 API define 포함
- Unity 빌드 결과: Success (약 228초)
- APK 크기: 79,022,593 bytes (75.36 MB)
- SHA-256: `7A44300648F228F0F46F9A732646E50CF3B8180A530C0A24289994869BDF1D68`

## Android 실행 환경

- 패키지 ID: `com.idleguild.game`
- 버전: `0.1.0` (`versionCode` 1)
- API 모드: `IDLE_GUILD_SERVER_BUILD`
- Android 에뮬레이터 API 주소: `http://10.0.2.2:5219`
- 출력: `Builds/Android/IdleGuild-optimized-test.apk`

## 실제 기기 수동 확인 체크리스트

현재 ADB에 연결된 Android 기기 또는 에뮬레이터가 없어 아래 항목은 APK 설치 후 수동 확인합니다.

- [ ] 세로·가로 화면에서 HUD가 안전 영역 안에 표시됨
- [ ] 영웅·성장·스킬·장비 메뉴가 서로 겹치지 않음
- [ ] 소녀·검은 고양이 영웅 선택이 정상 동작함
- [ ] 일반 몬스터가 달려온 뒤 정지하고 공격함
- [ ] 보스와 주인공의 발선 및 공격 거리가 자연스러움
- [ ] 세 액티브 스킬의 이펙트와 쿨타임이 표시됨
- [ ] 서버 로그인·성장 저장·앱 재시작 후 복원이 동작함

실제 휴대폰에서 PC의 Docker API에 연결하려면 `10.0.2.2` 대신 같은 Wi-Fi의 PC LAN 주소를 사용하는 별도 기기 빌드 설정이 필요합니다.

## 포트폴리오 캡처 체크리스트

아래 화면을 `Docs/Screenshots` 또는 포트폴리오 게시물에 저장합니다.

1. 자동 전투 HUD 전체 화면
2. 소녀와 검은 고양이 영웅 선택 화면
3. 일반 몬스터의 등장 달리기와 공격 장면
4. 보스 체력바·제한시간·보스 공격 장면
5. STAR BURST, SWIFT STRIKE, GUARDIAN LIGHT 이펙트
6. 성장·장비 자동 장착·전투력 상승 화면
7. Docker API/PostgreSQL healthy와 서버 저장·복원 결과

## 2분 영상 권장 구성

- 0:00~0:20 자동 전투와 지역 배경
- 0:20~0:40 영웅 변경과 성장
- 0:40~1:05 액티브 스킬 3종
- 1:05~1:30 보스 등장·공격·처치
- 1:30~1:45 장비 드롭과 자동 장착
- 1:45~2:00 서버 저장 후 재접속 복원
