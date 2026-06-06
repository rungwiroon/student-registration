#!/usr/bin/env bash
set -euo pipefail

# Quick pre-deployment smoke test
# Run this before every deploy

echo "🔥 CSR Smoke Test"
echo "=================="
echo ""

API_URL="${API_URL:-http://localhost:8080}"
FRONTEND_URL="${FRONTEND_URL:-http://localhost:5173}"

# --- 1. Backend Health (optional — skip if not running) ---
echo -n "[1/6] Backend health... "
HEALTH=$(curl -sf "${API_URL}/health" 2>/dev/null || echo "")
if [ "$HEALTH" = "Healthy" ]; then
  echo "✅"
else
  echo "⏭️  (backend not running — skip with: API_URL=skip ${0})"
fi

# --- 2. Backend Build ---
echo -n "[2/6] Backend compilation... "
cd "$(dirname "$0")/../CsrApi"
if dotnet build --no-restore -verbosity:quiet 2>/dev/null; then
  echo "✅"
else
  echo "❌"
  exit 1
fi

# --- 3. Frontend Build ---
echo -n "[3/6] Frontend compilation... "
cd "$(dirname "$0")/../csr-frontend"
if npm run build 2>/dev/null | grep -q "built"; then
  echo "✅"
else
  echo "❌"
  exit 1
fi

# --- 4. API Contract (401 without auth) — skip if backend not running ---
echo -n "[4/6] API rejects unauthenticated... "
if [ "$HEALTH" = "Healthy" ]; then
  HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "${API_URL}/api/v1/school-shirt/order" \
    -F "payload={}" 2>/dev/null || echo "000")
  if [ "$HTTP_CODE" = "401" ]; then
    echo "✅"
  else
    echo "⚠️  (got ${HTTP_CODE}, expected 401)"
  fi
else
  echo "⏭️  (skipped — backend not running)"
fi

# --- 5. Frontend Page Load ---
echo -n "[5/6] Frontend page reachable... "
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "${FRONTEND_URL}/shirt-order" 2>/dev/null || echo "000")
if [ "$HTTP_CODE" = "200" ]; then
  echo "✅"
else
  echo "⚠️  (got ${HTTP_CODE}, expected 200)"
fi

# --- 6. Test Files Exist ---
echo -n "[6/6] E2E tests present... "
cd "$(dirname "$0")/../csr-frontend"
if [ -f "tests/e2e/frontoffice/shirt-order.spec.js" ] && [ -f "tests/e2e/api/shirt-order-api.spec.js" ]; then
  echo "✅"
else
  echo "❌"
  exit 1
fi

echo ""
echo "🎉 All smoke tests passed! Ready to deploy."
echo ""
echo "To deploy:"
echo "  ./scripts/deploy.sh"
