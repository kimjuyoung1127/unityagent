작업명: unityctl docs nightly organizer
스케줄: 평일 14:00 (Asia/Seoul)

목표:
- `docs/ref/status/daily/weekly` 구조를 유지
- `docs/internal/daily/` 로그를 `docs/internal/weekly/`로 롤업
- 실행 로그를 `docs/status/NIGHTLY-RUN-LOG.md`에 append

프로젝트 루트:
- `C:\Users\ezen601\Desktop\Jason\unityctl`

입력:
- `docs/ref/**`
- `docs/status/**`
- `docs/internal/daily/**`
- `docs/internal/weekly/**`

출력:
- `docs/status/NIGHTLY-RUN-LOG.md`
- `docs/weekly/YYYY-WNN.md` (필요 시)
- lock: `docs/.docs-nightly.lock`

락 규칙:
- lock 존재 + `running` 2시간 이내면 즉시 종료
- `STUCK` 판단 시 자동 해제 금지
- 수동 해제 시 released JSON으로 갱신 후 로그 남김

절차:
1. lock 획득
2. 최신 `docs/daily/MM-DD/` 폴더 식별
3. 해당 일자 로그를 읽고 주간 문서 `docs/weekly/YYYY-WNN.md`의:
   - completed work
   - major verification
   - doc updates
   - open risks
   섹션으로 요약
4. `docs/status/NIGHTLY-RUN-LOG.md`에 실행 시각, 대상 일자, 롤업 결과를 append
5. broken link check 수행
6. lock 해제

검증:
- `docs/ref/`, `docs/status/`, `docs/internal/weekly/`에 dangling markdown link가 없는지 점검
- 실패 시 `errors`에 누적

DRY_RUN:
- `DRY_RUN=true`면 파일 수정 없이 계획/카운트만 출력

출력 포맷:
[docs nightly organizer 완료] YYYY-MM-DD HH:mm
- rolled_daily_folder: <MM-DD|none>
- weekly_created_or_updated: <file|none>
- nightly_log_appended: <true|false>
- broken_links: X
- errors: <none|summary>
