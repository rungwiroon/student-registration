#!/bin/bash
set -euo pipefail

# ===== ตั้งค่า =====
PROJECT_DIR="${HOME}/csr"
BACKUP_DIR="${PROJECT_DIR}/backups"
API_CONTAINER_NAME="skn50-smte-api-1"
DATA_PATHS=(
  "/app/data/ProtectedUploads"
  "/app/data/ShirtOrderSlips"
)

# ===== ฟังก์ชัน =====
log() {
  echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"
}

backup_from_container() {
  local container="$1"
  local timestamp
  timestamp=$(date +%Y%m%d_%H%M%S)
  local backup_path="${BACKUP_DIR}/${timestamp}"

  mkdir -p "$backup_path"

  for path in "${DATA_PATHS[@]}"; do
    if docker exec "$container" test -d "$path" 2>/dev/null; then
      log "📦 Backing up ${path} ..."
      docker cp "${container}:${path}" "${backup_path}/$(basename $path)"
    else
      log "⚠️  ${path} not found in container, skipping"
    fi
  done

  log "✅ Backup saved to: ${backup_path}"
  echo "$backup_path"
}

cd "$PROJECT_DIR"

# ===== 1. Backup ก่อน deploy =====
log "🔍 Checking running container..."

if docker ps --format '{{.Names}}' | grep -q "^${API_CONTAINER_NAME}$"; then
  BACKUP_PATH=$(backup_from_container "$API_CONTAINER_NAME")
else
  log "⚠️  Container ${API_CONTAINER_NAME} not running, skipping backup"
  BACKUP_PATH=""
fi

# ===== 2. Deploy =====
log "🚀 Pulling latest code..."
git pull

log "🔧 Building API..."
docker compose build --no-cache api

log "🔧 Building Frontend..."
docker compose build --no-cache frontend

log "🟢 Starting services..."
docker compose up -d api frontend

# ===== 3. Cleanup old backups (เก็บแค่ 10 ล่าสุด) =====
if [ -d "$BACKUP_DIR" ]; then
  ls -1dt "${BACKUP_DIR}"/* 2>/dev/null | tail -n +11 | xargs -r rm -rf
  log "🧹 Cleaned up old backups (kept last 10)"
fi

log "🎉 Deploy complete!"

if [ -n "$BACKUP_PATH" ]; then
  log "💾 Latest backup: ${BACKUP_PATH}"
fi
