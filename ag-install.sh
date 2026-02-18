#!/bin/bash

echo "=== AirlogGenerator Deployment Script ==="

# 1. Create config directories
echo "Creating /etc/ag and /etc/ag/stations..."
sudo mkdir -p /etc/ag/stations

# 2. Create log directory
echo "Creating /var/log/ag..."
sudo mkdir -p /var/log/ag

# 3. Set permissions (optional but recommended)
echo "Setting permissions..."
sudo chmod -R 775 /etc/ag
sudo chmod -R 775 /var/log/ag

# 4. Check for system.json
if [ ! -f /etc/ag/system.json ]; then
    echo "WARNING: /etc/ag/system.json does not exist."
    echo "You must create it before the container can run correctly."
    echo "Example: sudo nano /etc/ag/system.json"
    exit 1
fi

# 5. Run Docker Compose
echo "Starting Docker Compose..."
docker compose up -d

echo "=== Deployment Complete ==="
echo "Logs: /var/log/ag/"
echo "Config: /etc/ag/"
echo "Container should now be running."