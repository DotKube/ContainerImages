FROM almalinux:9.5-minimal
ENV TARGETARCH="linux-x64"

# Update and install necessary packages, including Podman, .NET SDK, and tools
RUN microdnf install -y epel-release \
 && microdnf install -y \
    ca-certificates \
    git \
    iputils \
    jq \
    wget \
    podman \
    podman-docker \
    fuse-overlayfs \
    libicu \
 && microdnf clean all


# Install Azure CLI
RUN curl -o /tmp/packages-microsoft-prod.rpm https://packages.microsoft.com/config/rhel/9.0/packages-microsoft-prod.rpm && \
    rpm -Uvh /tmp/packages-microsoft-prod.rpm && \
    microdnf -y install azure-cli && \
    rm -f /tmp/packages-microsoft-prod.rpm && \
    microdnf clean all

# Create work directory for Azure Pipelines Agent
WORKDIR /azp

# Set up volumes
VOLUME /var/lib/containers

# Copy startup script
COPY ./start.sh ./
RUN chmod +x ./start.sh

# Run as root user
ENV AGENT_ALLOW_RUNASROOT="true"

# Entry point
ENTRYPOINT ["./start.sh"]
