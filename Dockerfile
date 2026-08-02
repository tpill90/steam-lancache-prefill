FROM ubuntu:20.04
LABEL maintainers="tpilius@gmail.com;kirbo@kirbo-designs.com"

RUN \
        apt update \
        && DEBIAN_FRONTEND=noninteractive apt install -y --no-install-recommends \
                ca-certificates \
                libncursesw5 \
                locales \
                tzdata \
        && sed -i '/en_US.UTF-8/s/^# //' /etc/locale.gen \
        && dpkg-reconfigure --frontend=noninteractive locales \
        && update-locale LANG=en_US.UTF-8 \
        && rm -rf /var/cache/apt/archives /var/lib/apt/lists/*

ENV \
        LANG=en_US.UTF-8 \
        LANGUAGE=en_US:en \
        LC_ALL=en_US.UTF-8 \
        TERM=xterm-256color

COPY  /publish/SteamPrefill /app/SteamPrefill
RUN chmod +x /app/SteamPrefill

# Addresses issue created in https://github.com/tpill90/steam-lancache-prefill/commit/6a94d555fca6bd5a49f950d3b24dba08253b1bb0.
# Moving the application's dir made it so that config dir was no longer located in /Config, which didn't match the instructions in my docs
# on how to run the docker version, causing the app to be "logged out" after every run.
#
# Rather than change the docs and making all users be aware of this issue, I opted to just symlink the original Config dir to the correct new path.
RUN ln -s /Config /app/Config

ENTRYPOINT [ "/app/SteamPrefill" ]