#!/bin/bash

# This script is executed on the VPS via SSH.

# Arguments passed from CI/CD:
IMAGE_TAG_AND_REPO="$1" # e.g., ghcr.io/owner/repo:sha
APP_DIR="$2"           # Application base directory (e.g., /root/Mqtt)

# Define variables
APP_ID="rest-api-layer"
DAPR_HTTP_PORT=3500
APP_PORT=8080 
CONTAINER_NAME="dapr-${APP_ID}"

echo "Deploying new image: ${IMAGE_TAG_AND_REPO}"
echo "Application directory (for Dapr components): ${APP_DIR}"

# --- Setup Dapr Components Directory ---
# We place Dapr components inside the application's base directory for easy management.
DAPR_COMPONENTS_PATH="${APP_DIR}/dapr-components"

mkdir -p "$DAPR_COMPONENTS_PATH"
echo "Ensuring Dapr components path exists at: $DAPR_COMPONENTS_PATH"

# 1. Stop and remove the existing Dapr process/container (if running)
# Attempt to kill the dapr sidecar process (if running non-containerized)
pkill -f "dapr run --app-id ${APP_ID}" || true
echo "Existing Dapr sidecar process terminated (if running)."

# Stop and remove the application container (Dapr run with --container-image uses a specific name format)
docker stop $CONTAINER_NAME 2>/dev/null || true
docker rm $CONTAINER_NAME 2>/dev/null || true
echo "Existing application container ($CONTAINER_NAME) stopped and removed."

# 2. Pull the latest Docker image
# This ensures we have the image locally before Dapr tries to run it
docker pull "${IMAGE_TAG_AND_REPO}"

# 3. Run the new service with the Dapr sidecar attached
dapr run \
  --app-id ${APP_ID} \
  --app-port ${APP_PORT} \
  --dapr-http-port ${DAPR_HTTP_PORT} \
  --components-path "$DAPR_COMPONENTS_PATH" \
  --placement-host-address localhost \
  --log-level info \
  --container-image "${IMAGE_TAG_AND_REPO}" \
  --docker-network host \
  --container-name "$CONTAINER_NAME" \
  --detach

echo "Deployment complete. New service and Dapr sidecar running. Container Name: $CONTAINER_NAME"
