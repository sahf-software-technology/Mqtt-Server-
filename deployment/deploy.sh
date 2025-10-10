#!/bin/bash

# Define variables
APP_ID="rest-api-layer"
DAPR_HTTP_PORT=3500
APP_PORT=8080 # The port your .NET app listens on
IMAGE_NAME="ghcr.io/$GITHUB_REPOSITORY_OWNER/rest-api-layer" # Default GHCR path
IMAGE_TAG="$1" # Tag passed as the first argument from CI/CD

echo "Deploying new image: ${IMAGE_NAME}:${IMAGE_TAG}"

# 1. Stop and remove the existing Dapr process (if running)
pkill -f "dapr run --app-id ${APP_ID}" || true
echo "Existing Dapr process for ${APP_ID} terminated."

# 2. Pull the latest Docker image
docker pull "${IMAGE_NAME}:${IMAGE_TAG}"

# 3. Run the new service with the Dapr sidecar attached
dapr run   --app-id ${APP_ID}   --app-port ${APP_PORT}   --dapr-http-port ${DAPR_HTTP_PORT}   --components-path /opt/stylesearch/deployment/dapr-components   --placement-host-address localhost   --log-level info   --container-image "${IMAGE_NAME}:${IMAGE_TAG}"   --docker-network host   --detach

echo "Deployment complete. New service and Dapr sidecar running."
