#!/bin/bash

# This script is executed on the VPS via SSH.

# Arguments passed from CI/CD:
IMAGE_TAG_AND_REPO="$1" # e.g., ghcr.io/owner/repo:sha
APP_DIR="$2"           # Application base directory (e.g., /root/Mqtt)

# Define variables
APP_ID="rest-api-layer"
DAPR_HTTP_PORT=3500
DAPR_GRPC_PORT=50001
APP_PORT=8080 
CONTAINER_NAME="${APP_ID}-app"
DAPRD_CONTAINER_NAME="dapr-${APP_ID}"

echo "=========================================="
echo "Starting deployment..."
echo "=========================================="
echo "Image: ${IMAGE_TAG_AND_REPO}"
echo "App Directory: ${APP_DIR}"
echo "Container Name: ${CONTAINER_NAME}"
echo "=========================================="

# --- Setup Dapr Components Directory ---
DAPR_COMPONENTS_PATH="${APP_DIR}/dapr-components"

mkdir -p "$DAPR_COMPONENTS_PATH"
echo "✅ Dapr components path created at: $DAPR_COMPONENTS_PATH"

# 1. Stop and remove existing containers
echo "Stopping existing containers..."
docker stop $CONTAINER_NAME 2>/dev/null || true
docker rm $CONTAINER_NAME 2>/dev/null || true
docker stop $DAPRD_CONTAINER_NAME 2>/dev/null || true
docker rm $DAPRD_CONTAINER_NAME 2>/dev/null || true
echo "✅ Old containers stopped and removed"

# 2. Pull the latest Docker image
echo "Pulling Docker image: ${IMAGE_TAG_AND_REPO}"
if docker pull "${IMAGE_TAG_AND_REPO}"; then
    echo "✅ Docker image pulled successfully"
else
    echo "❌ Failed to pull Docker image"
    exit 1
fi

# 3. Start the application container
echo "Starting application container..."
docker run -d \
  --name ${CONTAINER_NAME} \
  --network host \
  -e ASPNETCORE_URLS="http://+:${APP_PORT}" \
  ${IMAGE_TAG_AND_REPO}

if [ $? -eq 0 ]; then
    echo "✅ Application container started"
else
    echo "❌ Failed to start application container"
    exit 1
fi

# 4. Start Dapr sidecar
echo "Starting Dapr sidecar..."
docker run -d \
  --name ${DAPRD_CONTAINER_NAME} \
  --network host \
  -v ${DAPR_COMPONENTS_PATH}:/components \
  daprio/daprd:1.16.1 \
  ./daprd \
  --app-id ${APP_ID} \
  --app-port ${APP_PORT} \
  --dapr-http-port ${DAPR_HTTP_PORT} \
  --dapr-grpc-port ${DAPR_GRPC_PORT} \
  --components-path /components \
  --log-level info \
  --placement-host-address localhost:50005

if [ $? -eq 0 ]; then
    echo "✅ Dapr sidecar started"
else
    echo "❌ Failed to start Dapr sidecar"
    docker logs ${DAPRD_CONTAINER_NAME}
    exit 1
fi

# 5. Wait and verify both containers are running
sleep 5
echo "Verifying containers..."

if docker ps | grep -q "$CONTAINER_NAME" && docker ps | grep -q "$DAPRD_CONTAINER_NAME"; then
    echo "✅ All containers running successfully"
    echo ""
    echo "Application Container:"
    docker ps | grep "$CONTAINER_NAME"
    echo ""
    echo "Dapr Sidecar Container:"
    docker ps | grep "$DAPRD_CONTAINER_NAME"
else
    echo "❌ One or more containers failed to start"
    echo ""
    echo "Application logs:"
    docker logs "$CONTAINER_NAME" 2>&1 || echo "No logs available"
    echo ""
    echo "Dapr logs:"
    docker logs "$DAPRD_CONTAINER_NAME" 2>&1 || echo "No logs available"
    exit 1
fi

echo "=========================================="
echo "✅ Deployment complete!"
echo "App Container: ${CONTAINER_NAME}"
echo "Dapr Container: ${DAPRD_CONTAINER_NAME}"
echo "App ID: ${APP_ID}"
echo "Dapr HTTP Port: ${DAPR_HTTP_PORT}"
echo "Dapr gRPC Port: ${DAPR_GRPC_PORT}"
echo "App Port: ${APP_PORT}"
echo "=========================================="
echo ""
echo "Test your API:"
echo "curl http://localhost:${DAPR_HTTP_PORT}/v1.0/invoke/${APP_ID}/method/YOUR_ENDPOINT"
echo "=========================================="