#!/usr/bin/env bash
# Create the first Teacher account in production via bootstrap endpoint.
# Usage: ./scripts/bootstrap-teacher.sh [--url <api-url>]
#
# Reads BOOTSTRAP_SECRET_TOKEN from .env (or env var).
# Self-disabling: returns 409 once a Teacher already exists.

set -euo pipefail

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
BASE_URL="${API_URL:-}"

# Parse args
while [[ $# -gt 0 ]]; do
  case "$1" in
    --url) BASE_URL="$2"; shift 2 ;;
    *) echo "Usage: $0 [--url <api-url>]"; exit 1 ;;
  esac
done

# Load .env if present
if [ -f "$PROJECT_DIR/.env" ]; then
  set -a
  # shellcheck disable=SC1091
  source "$PROJECT_DIR/.env"
  set +a
fi

if [ -z "$BASE_URL" ]; then
  read -rp "API URL (e.g. https://your-domain or http://localhost:8080): " BASE_URL
fi

: "${BOOTSTRAP_SECRET_TOKEN:?BOOTSTRAP_SECRET_TOKEN not set — add to .env or export}"

read -rp "LINE User ID of first Teacher: " LINE_USER_ID
if [ -z "$LINE_USER_ID" ]; then
  echo "❌ LineUserId required"
  exit 1
fi

read -rp "Teacher name (leave blank for default ครูผู้ดูแลระบบ): " TEACHER_NAME

PAYLOAD="{\"secretToken\":\"$BOOTSTRAP_SECRET_TOKEN\",\"lineUserId\":\"$LINE_USER_ID\""
if [ -n "$TEACHER_NAME" ]; then
  PAYLOAD="$PAYLOAD,\"name\":\"$TEACHER_NAME\""
fi
PAYLOAD="$PAYLOAD}"

echo ""
echo "Calling $BASE_URL/api/bootstrap ..."
HTTP_CODE=$(curl -s -o /tmp/bootstrap_response.json -w "%{http_code}" \
  -X POST "$BASE_URL/api/bootstrap" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD")

BODY=$(cat /tmp/bootstrap_response.json)
rm -f /tmp/bootstrap_response.json

case "$HTTP_CODE" in
  200) echo "✅ Teacher created: $BODY" ;;
  401) echo "❌ Wrong secret token (401)" ;;
  404) echo "❌ Bootstrap endpoint disabled — set BOOTSTRAP_SECRET_TOKEN on server (404)" ;;
  409) echo "⚠️  Teacher already exists — bootstrap already done (409)" ;;
  *)   echo "❌ Unexpected response $HTTP_CODE: $BODY" ;;
esac
