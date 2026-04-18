#!/usr/bin/env bash
# scripts/dev-up.sh — one-command local dev stack.
# Boots: LocalDB (SQL), Azurite (blob), BE API, FE dev server.
# Ctrl-C stops BE + Azurite; FE runs in foreground.
#
# Requirements (once):
#   - .NET SDK 10.0.100+
#   - Node 22.17+
#   - SqlLocalDB (ships with SQL Server Express / VS)
#   - Azurite is installed on-demand via npx.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BE_DIR="$ROOT/backend"
FE_DIR="$ROOT/frontend"
API_PROJECT="src/SoftimProject.WebApi"
DEV_APPSETTINGS="$BE_DIR/$API_PROJECT/appsettings.Development.json"
TEMPLATE="$BE_DIR/$API_PROJECT/appsettings.Development.json.template"
LOG_DIR="$ROOT/.dev-logs"
AZURITE_DIR="$ROOT/.azurite"

mkdir -p "$LOG_DIR" "$AZURITE_DIR"

BE_PID=""
AZ_PID=""

cleanup() {
  echo ""
  echo "[dev-up] stopping background processes..."
  if [[ -n "$BE_PID" ]] && kill -0 "$BE_PID" 2>/dev/null; then
    kill "$BE_PID" 2>/dev/null || true
    wait "$BE_PID" 2>/dev/null || true
  fi
  if [[ -n "$AZ_PID" ]] && kill -0 "$AZ_PID" 2>/dev/null; then
    kill "$AZ_PID" 2>/dev/null || true
    wait "$AZ_PID" 2>/dev/null || true
  fi
  echo "[dev-up] done."
}
trap cleanup EXIT INT TERM

# 1. Bootstrap appsettings.Development.json from template if missing.
if [[ ! -f "$DEV_APPSETTINGS" ]]; then
  echo "[dev-up] appsettings.Development.json not found — copying from template."
  cp "$TEMPLATE" "$DEV_APPSETTINGS"
fi

# 2. Start SqlLocalDB. Safe to call when already running.
if command -v SqlLocalDB.exe >/dev/null 2>&1; then
  echo "[dev-up] starting SqlLocalDB MSSQLLocalDB..."
  SqlLocalDB.exe start MSSQLLocalDB >/dev/null
else
  echo "[dev-up] WARN: SqlLocalDB.exe not on PATH. Assuming SQL is running somewhere."
fi

# 3. Start Azurite in background.
echo "[dev-up] starting Azurite (blob on :10000, logs in .dev-logs/azurite.log)..."
(cd "$AZURITE_DIR" && npx --yes azurite --silent --location . --debug ./debug.log) \
  >"$LOG_DIR/azurite.log" 2>&1 &
AZ_PID=$!

# 4. Start BE in background. Migrations + seeder run on startup.
echo "[dev-up] starting API (http://localhost:5249, logs in .dev-logs/api.log)..."
(cd "$BE_DIR" && dotnet run --project "$API_PROJECT") \
  >"$LOG_DIR/api.log" 2>&1 &
BE_PID=$!

# 5. FE in foreground.
echo "[dev-up] starting frontend (http://localhost:3000)..."
echo "[dev-up] Ctrl-C stops everything."
cd "$FE_DIR" && npm run dev
