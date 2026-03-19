# docs/

unityctl 프로젝트 문서 루트.

## 폴더 구조
- `ref/` — 정식 참조 문서 (아키텍처, 코드 패턴, 설계 문서, 가이드)
- `status/` — 운영 상태 보드 (프로젝트 상태, Phase 실행 보드)
- `daily/` — 일일 실행 로그 (`MM-DD/module-{slug}.md`)
- `weekly/` — 주간 롤업 (`YYYY-WNN.md`)
- `DEVELOPMENT.md` — 상세 개발 이력 (Phase 체크리스트, 테스트 현황, 기술 결정 로그)

## 규칙
- 새 참조 문서는 `ref/`에 생성.
- 운영 상태 변경은 `status/` 파일에서만 수행.
- 작업 완료 시 `daily/MM-DD/` 로그 작성.
- 마일스톤 완료 시 `weekly/` 롤업 반영.
- 문서 간 상태 불일치 발견 시 코드/테스트 실제 상태를 우선.
