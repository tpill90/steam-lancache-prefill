FROM ubuntu:20.04
LABEL maintainers="tpilius@gmail.com;kirbo@kirbo-designs.com"

RUN \
        apt update \
        && apt install -y --no-install-recommends \
                ca-certificates \
                libncursesw5 \
                locales \
        && sed -i '/en_US.UTF-8/s/^# //' /etc/locale.gen \
        && dpkg-reconfigure --frontend=noninteractive locales \
        && update-locale LANG=en_US.UTF-8 \
        && rm -rf /var/cache/apt/archives /var/lib/apt/lists/*

ENV \
        LANG=en_US.UTF-8 \
        LANGUAGE=en_US:en \
        LC_ALL=en_US.UTF-8 \
        TERM=xterm-256color

COPY  /publish/SteamPrefill /usr/bin
RUN chmod +x /usr/bin/SteamPrefill

ENTRYPOINT [ "/usr/bin/SteamPrefill" ]