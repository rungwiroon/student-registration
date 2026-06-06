#!/usr/bin/env bash
set -euo pipefail

# ── Config ──────────────────────────────────────────────
VPS_HOST="${DEPLOY_HOST:?DEPLOY_HOST env var required}"
VPS_USER="${DEPLOY_USER:?DEPLOY_USER env var required}"
VPS_DIR="~/skn50-smte"
PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
IMAGE_API="csr-api:latest"
IMAGE_FRONTEND="csr-frontend:latest"
TAR_API="csr-api.tar"
TAR_FRONTEND="csr-frontend.tar"

# ── Load .env for frontend build args ──────────────────
if [ -f "$PROJECT_DIR/.env" ]; then
  set -a
  # shellcheck disable=SC1091
  source "$PROJECT_DIR/.env"
  set +a
else
  echo "❌ Missing $PROJECT_DIR/.env (copy from .env.example)"
  exit 1
fi

: "${VITE_LIFF_ID:?VITE_LIFF_ID required in .env}"
: "${VITE_USE_MOCK_LIFF:=false}"

# ── Check deps ──────────────────────────────────────────
for cmd in docker sshpass scp; do
  if ! command -v "$cmd" &>/dev/null; then
    echo "❌ Missing: $cmd. Install: brew install $cmd"
    exit 1
  fi
done

# ── Ensure buildx builder for cross-platform ────────────
PLATFORM="linux/amd64"
# Use default buildx builder (works with OrbStack, Docker Desktop, etc.)
docker buildx use default >/dev/null 2>&1 || true

# ── Read password once ──────────────────────────────────
read -rsp "Enter password for $VPS_USER@$VPS_HOST: " SSHPASS
echo
export SSHPASS

# ── Build images (cross-compile ARM64 → amd64) ──────────
echo "🔨 Building API image for $PLATFORM..."
docker buildx build --platform "$PLATFORM" --load \
  -t "$IMAGE_API" \
  -f "$PROJECT_DIR/CsrApi/Dockerfile" \
  "$PROJECT_DIR/CsrApi"

echo "🔨 Building frontend image for $PLATFORM..."
docker buildx build --platform "$PLATFORM" --load \
  --build-arg "VITE_LIFF_ID=$VITE_LIFF_ID" \
  --build-arg "VITE_USE_MOCK_LIFF=$VITE_USE_MOCK_LIFF" \
  -t "$IMAGE_FRONTEND" \
  -f "$PROJECT_DIR/csr-frontend/Dockerfile" \
  "$PROJECT_DIR/csr-frontend"

# ── Export images to tar ────────────────────────────────
echo "📦 Exporting images..."
docker save -o "/tmp/$TAR_API" "$IMAGE_API"
docker save -o "/tmp/$TAR_FRONTEND" "$IMAGE_FRONTEND"

# ── Copy files to VPS ───────────────────────────────────
echo "📤 Uploading to VPS..."
sshpass -e scp "/tmp/$TAR_API" "/tmp/$TAR_FRONTEND" \
  "$PROJECT_DIR/docker-compose.yml" \
  "$VPS_USER@$VPS_HOST:$VPS_DIR/"

# ── Check/create .env on VPS ────────────────────────────
echo "🔍 Checking .env on VPS..."
sshpass -e ssh "$VPS_USER@$VPS_HOST" <<'REMOTE'
  cd ~/skn50-smte
  if [ ! -f .env ]; then
    echo "⚠️  No .env file found on server."
    echo "Required variables: CONNECTION_STRING, ENCRYPTION_KEY, LINE_LIFF_CHANNEL_ID, VITE_LIFF_ID"
    echo "Run on server: cp .env.example .env && nano .env"
  fi
REMOTE

# ── Deploy on VPS ───────────────────────────────────────
echo "🚀 Deploying on VPS..."
sshpass -e ssh "$VPS_USER@$VPS_HOST" <<'REMOTE'
  set -e
  cd ~/skn50-smte

  echo "📥 Loading images..."
  docker load -i csr-api.tar
  docker load -i csr-frontend.tar

  echo "🔄 Restarting containers..."
  docker compose down
  docker compose up -d

  echo "🧹 Cleaning up tar files..."
  rm -f csr-api.tar csr-frontend.tar

  echo "✅ Deploy complete"
  docker compose ps
REMOTE

# ── Cleanup local tars ──────────────────────────────────
rm -f "/tmp/$TAR_API" "/tmp/$TAR_FRONTEND"
unset SSHPASS

echo "✅ Done. App at: http://$VPS_HOST:5173  API at: http://$VPS_HOST:8080"
