#!/bin/bash

# This script is executed on the VPS via SSH.

# Arguments passed from CI/CD:
IMAGE_TAG_AND_REPO="$1" # e.g., ghcr.io/owner/repo:sha
APP_DIR="$2"           # Application base directory (e.g., /root/Mqtt)

# Define core variables
APP_ID="rest-api-layer"
DAPR_HTTP_PORT=3500
DAPR_GRPC_PORT=50001
APP_PORT=8080 
CONTAINER_NAME="${APP_ID}-app"
DAPRD_CONTAINER_NAME="dapr-${APP_ID}"

# --- Networking and Dependency Variables ---
NETWORK_NAME="dapr-network"
MOSQUITTO_NAME="mosquitto"
MOSQUITTO_PORT=1883
DAPR_COMPONENTS_PATH="${APP_DIR}/dapr-components" # Assumes components are here
MOSQUITTO_CONFIG_PATH="${APP_DIR}/mosquitto-config" # New path for Mosquitto config

echo "=========================================="
echo "Starting robust Dapr deployment..."
echo "=========================================="
echo "Image: ${IMAGE_TAG_AND_REPO}"
echo "Network: ${NETWORK_NAME}"
echo "=========================================="

# 1. Setup Dapr Components Directory, Mosquitto Config, and Network
echo "Creating Dapr components path and shared network..."
mkdir -p "$DAPR_COMPONENTS_PATH"
echo "✅ Dapr components path created at: $DAPR_COMPONENTS_PATH"

# Setup Mosquitto Config Path
echo "Creating Mosquitto config path and minimal config file..."
mkdir -p "$MOSQUITTO_CONFIG_PATH"

# Create a minimal config file to force Mosquitto to listen on all interfaces (0.0.0.0)
echo -e "listener ${MOSQUITTO_PORT} 0.0.0.0\nallow_anonymous true" > "$MOSQUITTO_CONFIG_PATH/mosquitto.conf"
echo "✅ Mosquitto config created, binding to 0.0.0.0"

# Create a custom bridge network for internal service discovery (mosquitto, app, sidecar)
docker network create $NETWORK_NAME 2>/dev/null || true 
echo "✅ Docker network '$NETWORK_NAME' ready"

# 2. Stop and remove existing containers (including Mosquitto)
echo "Stopping existing containers..."
docker stop $CONTAINER_NAME 2>/dev/null || true
docker rm $CONTAINER_NAME 2>/dev/null || true
docker stop $DAPRD_CONTAINER_NAME 2>/dev/null || true
docker rm $DAPRD_CONTAINER_NAME 2>/dev/null || true
docker stop $MOSQUITTO_NAME 2>/dev/null || true
docker rm $MOSQUITTO_NAME 2>/dev/null || true
echo "✅ Old containers stopped and removed"

# 3. Pull the latest Docker image
echo "Pulling Docker image: ${IMAGE_TAG_AND_REPO}"
if docker pull "${IMAGE_TAG_AND_REPO}"; then
    echo "✅ Docker image pulled successfully"
else
    echo "❌ Failed to pull Docker image"
    exit 1
fi

# 4. Start Mosquitto Broker (CRITICAL FIX APPLIED: Mounting config to force 0.0.0.0 listener)
echo "Starting Mosquitto container with fixed listener..."
docker run -d \
    --name $MOSQUITTO_NAME \
    --network $NETWORK_NAME \
    -p ${MOSQUITTO_PORT}:${MOSQUITTO_PORT} \
    -v ${MOSQUITTO_CONFIG_PATH}/mosquitto.conf:/mosquitto/config/mosquitto.conf \
    --restart unless-stopped \
    eclipse-mosquitto

if [ $? -eq 0 ]; then
    echo "✅ Mosquitto container started (External port: ${MOSQUITTO_PORT})"
else
    echo "❌ Failed to start Mosquitto container"
    exit 1
fi

# 5. Start the application container
echo "Starting application container..."
docker run -d \
  --name ${CONTAINER_NAME} \
  --network $NETWORK_NAME \
  -e ASPNETCORE_URLS="http://+:${APP_PORT}" \
  --restart unless-stopped \
  ${IMAGE_TAG_AND_REPO}

if [ $? -eq 0 ]; then
    echo "✅ Application container started"
else
    echo "❌ Failed to start application container"
    exit 1
fi

# 6. Start Dapr sidecar using app container's network
echo "Starting Dapr sidecar (network mode: container)..."
docker run -d \
  --name ${DAPRD_CONTAINER_NAME} \
  --network container:${CONTAINER_NAME} \
  --restart unless-stopped \
  -v ${DAPR_COMPONENTS_PATH}:/components \
  daprio/daprd:1.16.1 \
  /daprd \
  --app-id ${APP_ID} \
  --app-port ${APP_PORT} \
  --dapr-http-port ${DAPR_HTTP_PORT} \
  --dapr-grpc-port ${DAPR_GRPC_PORT} \
  --resources-path /components \
  --log-level info
  
if [ $? -eq 0 ]; then
    echo "✅ Dapr sidecar started"
else
    echo "❌ Failed to start Dapr sidecar"
    docker logs ${DAPRD_CONTAINER_NAME}
    exit 1
fi

# 7. Wait and verify all containers are running
sleep 5
echo "Verifying containers..."

if docker ps | grep -q "$CONTAINER_NAME" && docker ps | grep -q "$DAPRD_CONTAINER_NAME" && docker ps | grep -q "$MOSQUITTO_NAME"; then
    echo "✅ All containers running successfully"
    echo ""
    echo "Mosquitto Container (Check PORTS for 1883):"
    docker ps | grep "$MOSQUITTO_NAME"
    echo ""
    echo "Dapr Sidecar Container:"
    # Check logs for dapr sidecar to ensure it is stable and connected
    if docker logs "$DAPRD_CONTAINER_NAME" 2>&1 | grep -q "Initialized pubsub mqtt-pubsub"; then
        echo "✅ Dapr Sidecar is STABLE and MQTT component initialized successfully."
    else
        echo "⚠️ Dapr Sidecar is running but connection status is unknown. Please check logs manually for 'dapr-rest-api-layer'."
    fi
else
    echo "❌ One or more containers failed to start"
    echo "Check logs for mosquitto, ${CONTAINER_NAME}, and ${DAPRD_CONTAINER_NAME}"
    exit 1
fi

echo "=========================================="
echo "✅ Deployment complete!"
echo "Internal Dapr connection now uses 'tcp://mosquitto:1883'."
echo "External MQTT Explorer connection uses: YOUR_VPS_IP:${MOSQUITTO_PORT}"
echo "=========================================="
