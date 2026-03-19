#!/usr/bin/env bash
# ralph/orchestrator.sh — 단일 Phase 실행 (핵심 루프)
#
# 사용법: bash ralph/orchestrator.sh <phase-id> [max-iterations]
# 예시:   bash ralph/orchestrator.sh 3b 5
#
# 흐름:
#   Haiku(정보수집) → Opus(설계) → Sonnet(구현) → Test
#     ├─ PASS → Opus(문서+플랜) → DONE
#     └─ FAIL → Opus(리뷰) → Sonnet(수정) → Test
#                 ├─ PASS → Opus(문서+플랜) → DONE
#                 └─ FAIL → Opus(리뷰) → ... (max-iterations까지)

set -euo pipefail

PHASE_ID="${1:?사용법: bash ralph/orchestrator.sh <phase-id> [max-iterations]}"
MAX_ITER="${2:-5}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
WORKSPACE="$SCRIPT_DIR/workspace"
PHASE_FILE="$SCRIPT_DIR/phases/phase-${PHASE_ID}.md"
UNITY_PROJECT="C:/Users/ezen601/Desktop/Jason/robotapp2"

# 색상
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

log() { echo -e "${CYAN}[ralph]${NC} $1"; }
ok()  { echo -e "${GREEN}[ralph]${NC} $1"; }
err() { echo -e "${RED}[ralph]${NC} $1"; }
warn(){ echo -e "${YELLOW}[ralph]${NC} $1"; }

# Scorecard 기록
SCORECARD="$WORKSPACE/scorecard.log"
score() {
    echo "[$(date '+%H:%M:%S')] $1" >> "$SCORECARD" 2>/dev/null || true
}

# 검증
if [[ ! -f "$PHASE_FILE" ]]; then
    err "Phase 파일을 찾을 수 없습니다: $PHASE_FILE"
    exit 1
fi

# 시작 전 테스트 수 기록
cd "$ROOT_DIR"
BASELINE_TEST_COUNT=$(dotnet test unityctl.slnx --nologo --no-build 2>&1 \
    | grep -oE "(Passed|통과):[ ]*[0-9]+" | grep -oE "[0-9]+" \
    | awk '{s+=$1} END {print s+0}')
log "기준 테스트 수: ${BASELINE_TEST_COUNT}"

# workspace 초기화
mkdir -p "$WORKSPACE"
rm -f "$WORKSPACE"/{context,design,review,plan,impl-log,docs-log}.md
rm -f "$WORKSPACE"/*.log "$WORKSPACE"/*.txt

# baseline 스냅샷은 각 Sonnet 호출 직전에 찍음 (step_sonnet_implement 내부)

log "${BOLD}Phase ${PHASE_ID} 시작${NC} (최대 ${MAX_ITER}회 반복)"
echo "=========================================="

# ─────────────────────────────────────────────
# claude CLI 래퍼 — 에러 전파 + 빈 출력 감지
# ─────────────────────────────────────────────
run_claude() {
    local model="$1"
    local tools="$2"
    local prompt="$3"
    local output="$4"

    local prompt_file
    prompt_file=$(mktemp "$WORKSPACE/prompt-XXXXXX.txt")
    echo "$prompt" > "$prompt_file"

    local exit_code=0
    # --permission-mode acceptEdits: allow Write/Edit without interactive approval
    # Use -p with file content. For large prompts, reference files instead of inlining.
    if [[ -n "$tools" ]]; then
        claude --model "$model" --print \
            --permission-mode acceptEdits \
            --allowedTools "$tools" \
            -p "$(cat "$prompt_file")" \
            > "$output" 2>"$WORKSPACE/claude-stderr.log" || exit_code=$?
    else
        claude --model "$model" --print \
            --permission-mode acceptEdits \
            -p "$(cat "$prompt_file")" \
            > "$output" 2>"$WORKSPACE/claude-stderr.log" || exit_code=$?
    fi

    rm -f "$prompt_file"

    if [[ $exit_code -ne 0 ]]; then
        err "claude ${model} 실패 (exit code: ${exit_code})"
        cat "$WORKSPACE/claude-stderr.log" 2>/dev/null | tail -5
        return 1
    fi

    if [[ ! -s "$output" ]]; then
        err "claude ${model} 출력이 비어있음: ${output}"
        return 1
    fi

    return 0
}

# ─────────────────────────────────────────────
# Step 1: Haiku — 정보 수집
# ─────────────────────────────────────────────
step_haiku() {
    log "Step 1: ${BOLD}Haiku${NC} — 정보 수집"

    local prompt="당신은 코드베이스 분석가입니다. 아래 Phase 요구사항을 읽고, 현재 코드베이스에서 관련된 모든 파일/패턴/의존성을 수집하세요.

## Phase 요구사항
$(cat "$PHASE_FILE")

## 수집 대상
1. 수정 대상 파일 목록 (전체 경로)
2. 각 파일의 현재 상태 요약 (핵심 클래스/메서드/줄 수)
3. 의존하는 기존 타입/유틸리티 (재사용 가능)
4. docs/ref/code-patterns.md 의 관련 패턴
5. 테스트 프로젝트 구조 및 기존 테스트 패턴
6. Constants.cs, JsonContext.cs 현재 상태

결과를 마크다운으로 구조화해서 출력하세요. 코드를 작성하지 마세요."

    run_claude "haiku" "Read,Glob,Grep,Bash" "$prompt" "$WORKSPACE/context.md"
    local lines
    lines=$(wc -l < "$WORKSPACE/context.md")
    score "Haiku    context.md  ${lines}L  OK"
    ok "context.md (${lines} lines)"
}

# ─────────────────────────────────────────────
# Step 2: Opus — 설계
# ─────────────────────────────────────────────
step_opus_design() {
    log "Step 2: ${BOLD}Opus${NC} — 설계"

    local prompt="당신은 시니어 아키텍트입니다. 수집된 맥락과 Phase 요구사항을 바탕으로 구현 설계를 작성하세요.

## Phase 요구사항
$(cat "$PHASE_FILE")

## 코드베이스 맥락
$(cat "$WORKSPACE/context.md")

## 설계 출력 형식
1. **파일별 변경 계획**: 각 파일에 대해 추가/수정/삭제할 클래스/메서드/프로퍼티
2. **신규 파일**: 생성할 파일의 전체 경로 + 클래스 구조
3. **의존성 그래프**: 변경 순서 (어떤 파일을 먼저 수정해야 하는지)
4. **직렬화**: JsonContext 등록 대상, NDJSON 스키마
5. **테스트 전략**: 각 테스트 클래스의 테스트 케이스 목록
6. **리스크**: 주의할 점, 호환성 이슈

코드를 작성하지 마세요. 설계만 출력하세요."

    run_claude "opus" "Read,Glob,Grep" "$prompt" "$WORKSPACE/design.md"
    local lines
    lines=$(wc -l < "$WORKSPACE/design.md")
    score "Opus     design.md   ${lines}L  OK"
    ok "design.md (${lines} lines)"
}

# ─────────────────────────────────────────────
# Step 3: Sonnet — 구현
# ─────────────────────────────────────────────
step_sonnet_implement() {
    local label="${1:-구현}"
    log "Step: ${BOLD}Sonnet${NC} — ${label}"

    # Sonnet 직전에 baseline 스냅샷 (파일 수 기준)
    cd "$ROOT_DIR"
    local before_count
    before_count=$(( $(git diff --name-only -- src/ tests/ | wc -l) + $(git ls-files --others --exclude-standard -- src/ tests/ | wc -l) ))

    local review_ctx=""
    if [[ -f "$WORKSPACE/review.md" ]]; then
        review_ctx="
## Review feedback (fix these issues)
$(cat "$WORKSPACE/review.md")"
    fi

    local prompt="You are a senior C# developer. Implement code according to the design document.

## FILES TO READ FIRST (use Read tool)
1. Phase requirements: ${PHASE_FILE}
2. Design document: ${WORKSPACE}/design.md
3. Codebase context: ${WORKSPACE}/context.md
4. Code patterns: docs/ref/code-patterns.md
${review_ctx}

## CRITICAL RULES
- Read docs/ref/code-patterns.md FIRST and follow all patterns
- TreatWarningsAsErrors=true — zero warnings required
- Reuse existing code/types/utils, no duplication
- If you modify Shared, sync Plugin Editor/Shared/
- Register new types in JsonContext
- Run \`dotnet build unityctl.slnx\` after implementation and fix any errors

## TEST REQUIREMENT (MANDATORY — validation will fail without this)
You MUST create new test files under tests/ directory. The test count MUST increase.
- Create test files like: tests/Unityctl.Core.Tests/... and tests/Unityctl.Cli.Tests/...
- Each new class/feature needs at least 3-5 test cases
- Follow existing test patterns (xUnit, [Fact], [Theory])
- Run \`dotnet test unityctl.slnx\` to verify all tests pass

Read the files above, then implement the code AND tests."

    run_claude "sonnet" "Read,Write,Edit,Glob,Grep,Bash" "$prompt" "$WORKSPACE/impl-log.md"

    # 코드 변경 검증 — Sonnet 전후 파일 수 비교
    cd "$ROOT_DIR"
    local after_count
    after_count=$(( $(git diff --name-only -- src/ tests/ | wc -l) + $(git ls-files --others --exclude-standard -- src/ tests/ | wc -l) ))
    local src_changes=$((after_count - before_count))

    if [[ "$src_changes" -eq 0 ]]; then
        err "Sonnet did not create/modify any src/ or tests/ files!"
        score "Sonnet   ${label}  0files  FAIL (no code changes)"
        echo "NO_CODE_CHANGES" > "$WORKSPACE/test-result.txt"
        echo "Sonnet did not implement any code. No new/modified files under src/ or tests/. You MUST create new .cs files and test files." > "$WORKSPACE/test-errors.log"
        return 1
    fi

    score "Sonnet   ${label}  ${src_changes}files  OK"
    ok "${label} done (${src_changes} files changed)"
}

# ─────────────────────────────────────────────
# Step 4: Test — 빌드 + 유닛 테스트 + 테스트 수 검증 + 라이브 Unity
# ─────────────────────────────────────────────
step_test() {
    log "Step: ${BOLD}Test${NC} — 빌드 + 테스트 + 검증"

    cd "$ROOT_DIR"

    # ── 1. dotnet 빌드 ──
    log "  [1/4] dotnet build"
    if ! dotnet build unityctl.slnx --nologo -v q 2>"$WORKSPACE/build-err.log"; then
        err "빌드 실패"
        dotnet build unityctl.slnx --nologo 2>&1 | tail -40 > "$WORKSPACE/test-errors.log"
        score "Test     build       FAIL"
        echo "BUILD_FAILED" > "$WORKSPACE/test-result.txt"
        return 1
    fi
    score "Test     build       OK w0"

    # ── 2. dotnet 테스트 ──
    log "  [2/4] dotnet test"
    local test_exit=0
    dotnet test unityctl.slnx --nologo --no-build 2>&1 | tee "$WORKSPACE/test-output.log" || test_exit=$?

    if [[ $test_exit -ne 0 ]]; then
        err "테스트 실패 (exit code: $test_exit)"
        score "Test     dotnet      FAIL exit=$test_exit"
        echo "TEST_FAILED" > "$WORKSPACE/test-result.txt"
        cp "$WORKSPACE/test-output.log" "$WORKSPACE/test-errors.log"
        return 1
    fi

    if grep -qE "(실패:[ ]*[1-9]|Failed:[ ]*[1-9]|FAIL)" "$WORKSPACE/test-output.log" 2>/dev/null; then
        err "테스트 실패 감지"
        score "Test     dotnet      FAIL detected"
        echo "TEST_FAILED" > "$WORKSPACE/test-result.txt"
        cp "$WORKSPACE/test-output.log" "$WORKSPACE/test-errors.log"
        return 1
    fi

    # ── 3. 테스트 수 증가 검증 ──
    log "  [3/4] 테스트 수 검증"
    local current_count
    current_count=$(grep -oE "(Passed|통과):[ ]*[0-9]+" "$WORKSPACE/test-output.log" \
        | grep -oE "[0-9]+" \
        | awk '{s+=$1} END {print s+0}')

    if [[ "$current_count" -le "$BASELINE_TEST_COUNT" ]]; then
        err "테스트 수가 증가하지 않았습니다! (기준: ${BASELINE_TEST_COUNT}, 현재: ${current_count})"
        score "Test     count       FAIL ${BASELINE_TEST_COUNT}->${current_count} (no increase)"
        echo "NO_NEW_TESTS" > "$WORKSPACE/test-result.txt"
        echo "테스트 수가 증가하지 않았습니다. 기준: ${BASELINE_TEST_COUNT}, 현재: ${current_count}. 새 기능에 대한 테스트를 추가하세요." > "$WORKSPACE/test-errors.log"
        return 1
    fi
    score "Test     count       OK ${BASELINE_TEST_COUNT}->${current_count} (+$((current_count - BASELINE_TEST_COUNT)))"
    ok "  테스트 수 증가 확인 (${BASELINE_TEST_COUNT} → ${current_count})"

    # ── 4. 라이브 Unity 검증 (Unity 실행 중일 때만) ──
    log "  [4/4] 라이브 Unity 검증"
    if step_live_unity; then
        score "Test     live-unity  OK"
        ok "빌드 + 테스트 + 검증 모두 통과!"
    else
        score "Test     live-unity  SKIP"
        warn "라이브 검증 스킵 또는 실패 (dotnet 테스트는 통과)"
    fi

    echo "PASSED" > "$WORKSPACE/test-result.txt"
    return 0
}

# Phase별 라이브 Unity 검증
step_live_unity() {
    local cli="dotnet run --project src/Unityctl.Cli --no-build --"

    # Unity 프로젝트 존재 확인
    if [[ ! -d "$UNITY_PROJECT" ]]; then
        warn "    Unity 프로젝트 없음: $UNITY_PROJECT (스킵)"
        return 0
    fi

    # 기본: ping으로 Unity 연결 확인
    log "    ping 테스트..."
    local ping_result
    ping_result=$($cli ping --project "$UNITY_PROJECT" --json 2>&1) || true

    if echo "$ping_result" | grep -qi "pong\|Ready\|statusCode.*0"; then
        ok "    Unity IPC 연결 확인됨"
    else
        warn "    Unity 미연결 (IPC ping 실패, 라이브 검증 스킵)"
        return 0
    fi

    # Phase별 추가 검증
    case "$PHASE_ID" in
        3b)
            log "    log --stats 테스트..."
            $cli log --stats 2>&1 | tee -a "$WORKSPACE/live-test.log" || true
            ;;
        3a)
            log "    session list 테스트..."
            $cli session list --json 2>&1 | tee -a "$WORKSPACE/live-test.log" || true
            ;;
        4a)
            log "    build --dry-run 테스트..."
            $cli build --project "$UNITY_PROJECT" --target StandaloneWindows64 --dry-run --json 2>&1 \
                | tee -a "$WORKSPACE/live-test.log" || true
            ;;
        3c)
            log "    watch console 테스트 (3초)..."
            timeout 3 $cli watch console --project "$UNITY_PROJECT" --format json 2>&1 \
                | tee -a "$WORKSPACE/live-test.log" || true
            ;;
        4b)
            log "    scene snapshot 테스트..."
            $cli scene snapshot --project "$UNITY_PROJECT" --json 2>&1 \
                | tee -a "$WORKSPACE/live-test.log" || true
            ;;
        5)
            log "    schema 테스트..."
            $cli schema --format json 2>&1 | tee -a "$WORKSPACE/live-test.log" || true
            ;;
    esac

    ok "    라이브 검증 완료 (Phase ${PHASE_ID})"
    return 0
}

# ─────────────────────────────────────────────
# Step 5: Opus — 리뷰
# ─────────────────────────────────────────────
step_opus_review() {
    log "Step: ${BOLD}Opus${NC} — 리뷰"

    local prompt="You are a code reviewer. Analyze test failures and provide fix instructions.

## FILES TO READ (use Read tool)
1. Phase requirements: ${PHASE_FILE}
2. Design document: ${WORKSPACE}/design.md
3. Test errors: ${WORKSPACE}/test-errors.log

## OUTPUT FORMAT
1. Root cause analysis for each failed test/build error
2. Fix instructions per file (with line numbers)
3. Missing implementations
4. Prevention tips

Do NOT write code. Only output fix instructions."

    run_claude "opus" "Read,Glob,Grep" "$prompt" "$WORKSPACE/review.md"
    score "Opus     review.md   OK"
    ok "review.md 생성"
}

# ─────────────────────────────────────────────
# Step 6: Opus — 문서 + 플랜 업데이트
# ─────────────────────────────────────────────
step_opus_docs() {
    log "Step: ${BOLD}Opus${NC} — 문서 동기화"

    local prompt="You are a documentation manager. Phase ${PHASE_ID} implementation is complete.
Update these documents to match current code state:

1. docs/status/PROJECT-STATUS.md — set Phase ${PHASE_ID} to Done, update test counts
2. docs/status/PHASE-EXECUTION-BOARD.md — set Phase ${PHASE_ID} status to Done
3. CLAUDE.md — update Phase Status table + test counts
4. docs/DEVELOPMENT.md — add Phase ${PHASE_ID} section (implementation + verification)
5. docs/ref/phase-roadmap.md — update Phase ${PHASE_ID} status

Rules:
- Run dotnet test unityctl.slnx to get actual test counts
- Ensure no inconsistencies between documents
- Do NOT modify code, only update documentation"

    run_claude "opus" "Read,Write,Edit,Glob,Grep,Bash" "$prompt" "$WORKSPACE/docs-log.md"
    score "Opus     docs-sync   OK"
    ok "문서 동기화 완료"
}

# ─────────────────────────────────────────────
# 메인 루프
# ─────────────────────────────────────────────
main() {
    local start_time=$(date +%s)

    # Step 1-2: 정보수집 + 설계 (1회만)
    step_haiku
    step_opus_design

    # Step 3-4: 구현 + 테스트 (첫 시도)
    if step_sonnet_implement "초기 구현" && step_test; then
        ok "첫 시도에 통과! 문서 업데이트 중..."
        step_opus_docs

        local elapsed=$(( $(date +%s) - start_time ))
        echo ""
        ok "=========================================="
        ok "Phase ${PHASE_ID} 완료! (${elapsed}초, 반복 0회)"
        ok "=========================================="
        return 0
    fi

    # 실패 → 리뷰-수정 루프
    for iter in $(seq 1 "$MAX_ITER"); do
        warn "반복 ${iter}/${MAX_ITER}"
        echo "------------------------------------------"

        step_opus_review

        if step_sonnet_implement "수정 (반복 ${iter})" && step_test; then
            ok "반복 ${iter}에서 통과! 문서 업데이트 중..."
            step_opus_docs

            local elapsed=$(( $(date +%s) - start_time ))
            echo ""
            ok "=========================================="
            ok "Phase ${PHASE_ID} 완료! (${elapsed}초, 반복 ${iter}회)"
            ok "=========================================="
            return 0
        fi
    done

    # 최대 반복 초과
    local elapsed=$(( $(date +%s) - start_time ))
    echo ""
    err "=========================================="
    err "Phase ${PHASE_ID} 실패: ${MAX_ITER}회 반복 후에도 미통과 (${elapsed}초)"
    err "마지막 에러: $(head -5 "$WORKSPACE/test-errors.log" 2>/dev/null)"
    err "=========================================="
    return 1
}

main
