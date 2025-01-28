
#!/bin/bash

# Variables
SOURCE_IMAGE="docker://mcr.microsoft.com/mssql/rhel/server:2022-latest"
DEST_IMAGE="docker://ghcr.io/dotkube/sql-server:22-rhel"
USERNAME="" # Replace with your GitHub username

CR_PAT="" # GitHub Container Registry Personal Access Token

# Ensure the token was retrieved successfully
if [[ -z "$CR_PAT" ]]; then
  echo "Error: Unable to retrieve GitHub token. Make sure you're logged in to GitHub CLI."
  exit 1
fi

# Authenticate with GitHub Container Registry
echo "Authenticating with GitHub Container Registry..."
echo "$CR_PAT" | skopeo login ghcr.io --username "$USERNAME" --password-stdin
if [[ $? -ne 0 ]]; then
  echo "Error: Failed to authenticate with GitHub Container Registry."
  exit 1
fi

# Copy the image
echo "Copying image from $SOURCE_IMAGE to $DEST_IMAGE..."
skopeo copy "$SOURCE_IMAGE" "$DEST_IMAGE" --dest-creds "$USERNAME:$CR_PAT"
if [[ $? -ne 0 ]]; then
  echo "Error: Failed to copy the image."
  exit 1
fi

echo "Image successfully transferred to $DEST_IMAGE."
