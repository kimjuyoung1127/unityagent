#!/usr/bin/env bash
# ralph/run-all.sh — 전체 Phase 순차 실행 + 자동 커밋
#
# 사용법: bash ralph/run-all.sh [max-iterations-per-phase]
# 예시:   bash ralph/run-all.sh 3
#
# 실행 순서: 3b → 3a → 4a → 3c → 4b → 5
# 각 Phase 완료 시 자동 커밋 후 다음 Phase로 진행.

set -euo pipefail

MAX_ITER="${1:-5}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
SCORECARD="$SCRIPT_DIR/workspace/scorecard.log"

# 색상
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

log() { echo -e "${CYAN}[run-all]${NC} $1"; }
ok()  { echo -e "${GREEN}[run-all]${NC} $1"; }
err() { echo -e "${RED}[run-all]${NC} $1"; }

# Phase 실행 순서 (로드맵 기준)
PHASES=("3b" "3a" "4a" "3c" "4b" "5")

PHASE_NAMES=(
    "Flight Recorder"
    "Session Layer"
    "Ghost Mode"
    "Watch Mode"
    "Scene Diff"
    "Agent Layer"
)

TOTAL=${#PHASES[@]}
PASSED=0
FAILED_PHASE=""

start_time=$(date +%s)

# scorecard 초기화
mkdir -p "$SCRIPT_DIR/workspace"
cat > "$SCORECARD" <<HEADER
# Ralph Loop v2 — Scorecard
# 시작: $(date '+%Y-%m-%d %H:%M:%S')
# Phase 순서: ${PHASES[*]}
# Phase당 최대 반복: ${MAX_ITER}
========================================
HEADER

echo ""
log "${BOLD}Ralph Loop v2 — 전체 실행${NC}"
log "Phase 순서: ${PHASES[*]}"
log "Phase당 최대 반복: ${MAX_ITER}"
log "Scorecard: ralph/workspace/scorecard.log"
echo "=========================================="

for i in "${!PHASES[@]}"; do
    phase="${PHASES[$i]}"
    name="${PHASE_NAMES[$i]}"
    num=$((i + 1))
    phase_start=$(date +%s)

    echo ""
    log "${BOLD}[${num}/${TOTAL}] Phase ${phase} — ${name}${NC}"
    echo "=========================================="

    # workspace 초기화 (scorecard는 유지)
    find "$SCRIPT_DIR/workspace" -type f ! -name scorecard.log ! -name .gitkeep -delete 2>/dev/null || true

    echo "" >> "$SCORECARD"
    echo "[$(date '+%H:%M:%S')] Phase ${phase} (${name}) 시작" >> "$SCORECARD"

    # Phase 실행
    if bash "$SCRIPT_DIR/orchestrator.sh" "$phase" "$MAX_ITER"; then
        phase_elapsed=$(( $(date +%s) - phase_start ))
        ok "Phase ${phase} 통과!"
        PASSED=$((PASSED + 1))

        echo "[$(date '+%H:%M:%S')] Phase ${phase} ✅ 완료 (${phase_elapsed}초)" >> "$SCORECARD"

        # 자동 커밋
        cd "$ROOT_DIR"
        git add -A
        if ! git diff --cached --quiet; then
            local_msg="Phase ${phase}: ${name} 완료 (Ralph Loop v2)"
            git commit -m "${local_msg}

Co-Authored-By: Claude Haiku 4.5 <noreply@anthropic.com>
Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
            ok "커밋 완료: Phase ${phase}"
            echo "[$(date '+%H:%M:%S')] Phase ${phase} 커밋 완료" >> "$SCORECARD"
        else
            log "변경 사항 없음, 커밋 스킵"
            echo "[$(date '+%H:%M:%S')] Phase ${phase} 커밋 스킵 (변경 없음)" >> "$SCORECARD"
        fi
    else
        phase_elapsed=$(( $(date +%s) - phase_start ))
        err "Phase ${phase} 실패! 파이프라인 중단."
        FAILED_PHASE="${phase} (${name})"
        echo "[$(date '+%H:%M:%S')] Phase ${phase} ❌ 실패 (${phase_elapsed}초)" >> "$SCORECARD"
        break
    fi
done

elapsed=$(( $(date +%s) - start_time ))
elapsed_min=$((elapsed / 60))
elapsed_sec=$((elapsed % 60))

echo "" >> "$SCORECARD"
echo "========================================" >> "$SCORECARD"
if [[ -z "$FAILED_PHASE" ]]; then
    echo "결과: ${PASSED}/${TOTAL} 통과 (${elapsed_min}분 ${elapsed_sec}초)" >> "$SCORECARD"
else
    echo "결과: ${PASSED}/${TOTAL} 통과, 실패: ${FAILED_PHASE} (${elapsed_min}분 ${elapsed_sec}초)" >> "$SCORECARD"
fi

echo ""
echo "=========================================="
if [[ -z "$FAILED_PHASE" ]]; then
    ok "${BOLD}전체 완료!${NC} ${PASSED}/${TOTAL} Phase 통과 (${elapsed_min}분 ${elapsed_sec}초)"
else
    err "${BOLD}중단됨:${NC} ${PASSED}/${TOTAL} 통과, 실패: ${FAILED_PHASE} (${elapsed_min}분 ${elapsed_sec}초)"
    echo ""
    log "Scorecard 확인: cat ralph/workspace/scorecard.log"
    exit 1
fi
echo "=========================================="
