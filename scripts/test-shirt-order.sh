#!/usr/bin/env bash
set -euo pipefail

# Integration test script for School Shirt Order feature
# Requires: .NET SDK, Node.js, npm

echo "=== CSR School Shirt Order — Integration Test ==="
echo ""

# --- Step 1: Backend build ---
echo "[1/4] Building backend..."
cd "$(dirname "$0")/../CsrApi"
dotnet build --no-restore -verbosity:quiet 2>/dev/null || dotnet build -verbosity:quiet

echo "✅ Backend compiles successfully"
echo ""

# --- Step 2: Frontend build ---
echo "[2/4] Building frontend..."
cd "$(dirname "$0")/../csr-frontend"
npm run build 2>/dev/null | tail -n 3

echo "✅ Frontend builds successfully"
echo ""

# --- Step 3: Check test files exist ---
echo "[3/4] Checking test files..."
TEST_FILES=(
  "tests/e2e/frontoffice/shirt-order.spec.js"
  "tests/e2e/api/shirt-order-api.spec.js"
)
for f in "${TEST_FILES[@]}"; do
  if [ -f "$f" ]; then
    echo "  ✅ $f"
  else
    echo "  ❌ $f missing"
    exit 1
  fi
done
echo ""

# --- Step 4: API contract validation ---
echo "[4/4] Validating API contract..."

# Check that ShirtOrderRequest model has expected fields
REQUEST_MODEL="$(dirname "$0")/../CsrApi/Models/ShirtOrderRequest.cs"
if grep -q "StudentName" "$REQUEST_MODEL" && \
   grep -q "StudentNumber" "$REQUEST_MODEL" && \
   grep -q "Items" "$REQUEST_MODEL"; then
  echo "  ✅ ShirtOrderRequest model has correct fields"
else
  echo "  ❌ ShirtOrderRequest model missing expected fields"
  exit 1
fi

# Check that endpoint file exists and maps correct route
ENDPOINT_FILE="$(dirname "$0")/../CsrApi/ShirtOrderEndpoints.cs"
if grep -q "/api/v1/school-shirt/order" "$ENDPOINT_FILE" && \
   grep -q "MapPost" "$ENDPOINT_FILE" && \
   grep -q "MapGet.*slips" "$ENDPOINT_FILE"; then
  echo "  ✅ ShirtOrderEndpoints has POST order + GET slips"
else
  echo "  ❌ ShirtOrderEndpoints missing expected routes"
  exit 1
fi

# Check frontend composable exports expected methods
COMPOSABLE="$(dirname "$0")/../csr-frontend/src/composables/useShirtOrder.js"
if grep -q "submitShirtOrder" "$(dirname "$0")/../csr-frontend/src/services/shirtOrderApi.js" && \
   grep -q "validateForm" "$COMPOSABLE" && \
   grep -q "totalAmount" "$COMPOSABLE"; then
  echo "  ✅ Frontend composable + API service exports correct methods"
else
  echo "  ❌ Frontend missing expected exports"
  exit 1
fi

echo ""
echo "=== All integration checks passed ✅ ==="
echo ""
echo "Next steps:"
echo "  1. Start backend:    cd CsrApi && dotnet run"
echo "  2. Start frontend:   cd csr-frontend && npm run dev"
echo "  3. Run E2E tests:    cd csr-frontend && npx playwright test"
echo ""
echo "Manual test checklist:"
echo "  [ ] Navigate to /shirt-order from dashboard"
echo "  [ ] Select shirt quantities, verify total updates"
echo "  [ ] Fill student name + number"
echo "  [ ] Upload slip (JPG/PNG)"
echo "  [ ] Submit and see success state"
echo "  [ ] Verify Google Sheet row added (if configured)"
echo "  [ ] Verify slip file saved in App_Data/ShirtOrderSlips/"
