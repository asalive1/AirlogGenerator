#!/bin/bash

set -e

###############################################################################
# 1. Create required directories
###############################################################################

echo "[STEP] Creating /etc/ag and /etc/ag/stations..."
sudo mkdir -p /etc/ag/stations

echo "[STEP] Creating /var/log/ag..."
sudo mkdir -p /var/log/ag

###############################################################################
# 2. Set permissions
###############################################################################

echo "[STEP] Setting permissions..."
sudo chmod -R 775 /etc/ag
sudo chmod -R 775 /var/log/ag

###############################################################################
# 3. Ensure system.json exists
###############################################################################

if [ ! -f /etc/ag/system.json ]; then
    echo "[WARN] /etc/ag/system.json not found."
    echo "[STEP] Copying default system.json from local CONFIG folder..."

    LOCAL_CONFIG="./CONFIG/system.json"

    if [ ! -f "$LOCAL_CONFIG" ]; then
        echo "[ERROR] Default system.json not found at: $LOCAL_CONFIG"
        echo "        Please create /etc/ag/system.json manually."
        exit 1
    fi

    sudo cp "$LOCAL_CONFIG" /etc/ag/system.json
    echo "[OK] system.json copied."
fi

###############################################################################
# 4. Start Docker Compose
###############################################################################

echo "[STEP] Starting Docker Compose..."

# Move to the folder where docker-compose.yml lives
cd "$(dirname "$0")"

echo "[STEP] Pulling latest image from GHCR..."
docker compose pull

echo "[STEP] Starting/updating container..."
docker compose up -d

echo "=== Deployment Complete ==="
echo "Config directory: /etc/ag/"
echo "Station configs:  /etc/ag/stations/"
echo "Logs directory:   /var/log/ag/"
echo "Container should now be running."
