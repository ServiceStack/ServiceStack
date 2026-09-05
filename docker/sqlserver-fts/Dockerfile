ARG UBUNTU_VERSION=24.04
FROM ubuntu:${UBUNTU_VERSION}

ARG UBUNTU_VERSION
ARG MSSQL_REPO_FLAVOR=mssql-server-2025
ARG INSTALL_FTS=true

ENV ACCEPT_EULA=Y

RUN set -eux; \
    export DEBIAN_FRONTEND=noninteractive; \
    apt-get update; \
    apt-get install -y --no-install-recommends ca-certificates curl gnupg; \
    mkdir -p /usr/share/keyrings; \
    curl -fsSL https://packages.microsoft.com/keys/microsoft.asc \
      | gpg --dearmor > /usr/share/keyrings/microsoft-prod.gpg; \
    chmod 644 /usr/share/keyrings/microsoft-prod.gpg; \
    curl -fsSL "https://packages.microsoft.com/config/ubuntu/${UBUNTU_VERSION}/${MSSQL_REPO_FLAVOR}.list" \
      -o /etc/apt/sources.list.d/mssql-server.list; \
    curl -fsSL "https://packages.microsoft.com/config/ubuntu/${UBUNTU_VERSION}/prod.list" \
      -o /etc/apt/sources.list.d/microsoft-prod.list; \
    apt-get update; \
    apt-get install -y --no-install-recommends mssql-server; \
    if [ "${INSTALL_FTS}" = "true" ]; then \
      MSSQL_VERSION="$(dpkg-query -W -f='${Version}' mssql-server)"; \
      apt-get install -y --no-install-recommends "mssql-server-fts=${MSSQL_VERSION}"; \
    fi; \
    apt-get install -y --no-install-recommends mssql-tools18; \
    apt-get clean; \
    rm -rf /var/lib/apt/lists/*

EXPOSE 1433

USER mssql

CMD ["/opt/mssql/bin/sqlservr"]
