# Use the official SQL Server 2022 image as the base
FROM mcr.microsoft.com/mssql/server:2022-latest

# Switch to root user to configure the environment
USER root

# Ensure the 'mssql' user and necessary permissions are set
RUN id mssql || useradd -M -s /bin/bash -u 10001 -g 0 mssql \
    && mkdir -p -m 770 /var/opt/mssql \
    && chgrp -R 0 /var/opt/mssql

# Grant permissions to allow the non-root user to bind to privileged ports (<1024)
RUN setcap 'cap_net_bind_service+ep' /opt/mssql/bin/sqlservr

# Allow dumps from the non-root process
RUN setcap 'cap_sys_ptrace+ep' /opt/mssql/bin/paldumper \
    && setcap 'cap_sys_ptrace+ep' /usr/bin/gdb

# Add an ldconfig file because setcap causes the OS to remove LD_LIBRARY_PATH
RUN mkdir -p /etc/ld.so.conf.d && touch /etc/ld.so.conf.d/mssql.conf \
    && echo -e "# mssql libs\n/opt/mssql/lib" >> /etc/ld.so.conf.d/mssql.conf \
    && ldconfig

# Update the container's package list
RUN apt-get update

# Install the curl package
RUN apt-get install -y curl

# Download the public key for Microsoft SQL Server
RUN curl https://packages.microsoft.com/keys/microsoft.asc | tee /etc/apt/trusted.gpg.d/microsoft.asc

# Add the SQL Server 2022 repository
RUN curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list | tee /etc/apt/sources.list.d/mssql-server-2022.list

# Install Full-Text Search (FTS) and clean up
RUN apt-get update && apt-get install -y mssql-server-fts && apt-get clean


# Set required environment variables for SQL Server
ENV ACCEPT_EULA=Y
ENV MSSQL_SA_PASSWORD="ReallyStrongPassword123!#"
ENV MSSQL_AGENT_ENABLED=True

# Switch to the non-root user
USER mssql

# Expose the default SQL Server port
EXPOSE 1433

# Run SQL Server as the non-root user
CMD ["/opt/mssql/bin/sqlservr"]
