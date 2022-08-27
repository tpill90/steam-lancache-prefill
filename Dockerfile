FROM --platform=linux/amd64 ubuntu:20.04
LABEL maintainers="tpilius@gmail.com;kirbo@kirbo-designs.com"

ARG DEBIAN_FRONTEND=noninteractive
RUN \
    apt-get update && \
    apt-get install -y ca-certificates && \
    rm -rf /var/lib/apt/lists/*

COPY  /publish/SteamPrefill /app/SteamPrefill
RUN chmod +x /app/SteamPrefill

WORKDIR /app
ENTRYPOINT [ "/app/SteamPrefill" ]