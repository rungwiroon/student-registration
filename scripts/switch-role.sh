#!/bin/bash
# Switch backoffice role for development testing
# Usage: ./scripts/switch-role.sh [teacher|pns]

BASE_URL="${API_URL:-http://localhost:8080}"
MOCK_USER_ID="mock-line-user-id"

case "${1:-}" in
  teacher)
    ROLE="Teacher"
    ;;
  pns|parent)
    ROLE="ParentNetworkStaff"
    ;;
  *)
    echo "Usage: $0 [teacher|pns]"
    echo "  teacher  - Switch to Teacher (full access)"
    echo "  pns      - Switch to ParentNetworkStaff (read-only)"
    exit 1
    ;;
esac

echo "Switching to $ROLE..."
curl -s -X POST "$BASE_URL/api/backoffice/dev/switch-role" \
  -H "Content-Type: application/json" \
  -d "{\"lineUserId\":\"$MOCK_USER_ID\",\"role\":\"$ROLE\"}" | python3 -m json.tool 2>/dev/null || echo "Request failed — is the server running?"
