#!/bin/bash
Cyan='\033[0;36m'
RED='\033[0;31m'
Yellow='\033[0;33m'
NC='\033[0m' # No Color

# Checking for required software
if ! [ -x "$(command -v curl)" ]; then
  echo -e "${RED}Required software curl is not installed.${NC}" >&2
  exit 1
fi
if ! [ -x "$(command -v jq)" ]; then
  echo -e "${RED}Required software jq is not installed.${NC}" >&2
  exit 1
fi
if ! [ -x "$(command -v unzip)" ]; then
  echo -e "${RED}Required software unzip is not installed.${NC}" >&2
  exit 1
fi
if ! [ -x "$(command -v wget)" ]; then
  echo -e "${RED}Required software wget is not installed.${NC}" >&2
  exit 1
fi

# Getting latest version tag
echo -e "${Yellow} Checking for latest version ${NC}"
VERSION_API="https://api.github.com/repos/tpill90/steam-lancache-prefill/releases"
LATEST_TAG=$(curl -s $VERSION_API | jq -r '.[0].tag_name' | cut -c 2-)

if [ -z "${LATEST_TAG}" ]; then
    echo -e " ${RED}Something went wrong, unable to get latest version!${NC}"
    exit 1
fi
echo -e " Found latest version : ${Cyan} ${LATEST_TAG} ${NC}"

# Checking to see if SteamPrefill is already up to date
if [ -f ./SteamPrefill ]; then
    CURRENT_VERSION=$(./SteamPrefill --version)

    if [ "${CURRENT_VERSION}" == "v${LATEST_TAG}" ]; then
        echo -e "${Yellow} Already up to date !${NC}"
        exit
    fi
fi

# Downloading latest version
echo -e "${Yellow} Downloading... ${NC}"
DOWNLOAD_URL="https://github.com/tpill90/steam-lancache-prefill/releases/download/v${LATEST_TAG}/SteamPrefill-${LATEST_TAG}-linux-x64.zip"
wget -q -nc --show-progress --progress=bar:force:noscroll $DOWNLOAD_URL

# Unzip
echo -e "${Yellow} Unzipping... ${NC}"
unzip -q -j -o SteamPrefill-${LATEST_TAG}-linux-x64.zip

# Required so executable permissions don't get overwritten by unzip
chmod +x SteamPrefill update.sh

# Cleanup
rm SteamPrefill-${LATEST_TAG}-linux-x64.zip

echo -e " ${Cyan} Complete! ${NC}"
