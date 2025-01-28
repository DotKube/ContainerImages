#!/bin/bash

DOCKER_CONFIG_FILE="$HOME/.docker/config.json"

# Check if the Docker config file exists
if [[ -f "$DOCKER_CONFIG_FILE" ]]; then
  echo "Inspecting $DOCKER_CONFIG_FILE for 'credsStore'..."

  # Check if 'credsStore' exists in the file
  if grep -q '"credsStore"' "$DOCKER_CONFIG_FILE"; then
    echo "'credsStore' found. Removing it..."

    # Remove the 'credsStore' line
    sed -i '/"credsStore"/d' "$DOCKER_CONFIG_FILE"

    # Remove the trailing comma from the previous line, if present
    sed -i '/{},/s/},/}/' "$DOCKER_CONFIG_FILE"

    echo "'credsStore' removed from $DOCKER_CONFIG_FILE."
  else
    echo "'credsStore' not found in $DOCKER_CONFIG_FILE."
  fi
else
  echo "Docker config file $DOCKER_CONFIG_FILE does not exist. Nothing to inspect."
fi


# Local image details
LOCAL_IMAGE="sqlcmd-tools-container:latest"

# Image details for GitHub Container Registry
IMAGE_NAME="mssql-tools"
VERSION="almalinux-9.5"

# GitHub Container Registry details
ORG_NAME="dotkube"
GHCR_URL="ghcr.io"
USERNAME=""

CR_PAT="" # GitHub Container Registry Personal Access Token

if [[ -z "$CR_PAT" ]]; then
  echo "Error: Unable to retrieve GitHub token. Make sure you're logged in to GitHub CLI."
  exit 1
fi

# Log out of GitHub Container Registry
docker logout $GHCR_URL || true

# Log in to GitHub Container Registry
echo "$CR_PAT" | docker login $GHCR_URL -u "$USERNAME" --password-stdin
if [[ $? -ne 0 ]]; then
  echo "Error: Failed to log in to GitHub Container Registry."
  exit 1
fi

# Rename the local image
RENAMED_IMAGE="$IMAGE_NAME-renamed:latest"
docker tag $LOCAL_IMAGE $RENAMED_IMAGE

# Tag the renamed image for the GitHub Container Registry
REMOTE_IMAGE="$GHCR_URL/$ORG_NAME/$IMAGE_NAME:$VERSION"
docker tag $RENAMED_IMAGE $REMOTE_IMAGE

# Push the image to the GitHub Container Registry
docker push $REMOTE_IMAGE
if [[ $? -ne 0 ]]; then
  echo "Error: Failed to push the image to the GitHub Container Registry."
  exit 1
fi

echo "Image pushed successfully: $REMOTE_IMAGE"
