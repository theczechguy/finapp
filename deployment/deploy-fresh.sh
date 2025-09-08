#!/bin/bash

# FinApp Fresh Deployment Script - Complete Clean Install
# Usage: ./deploy-fresh.sh <user@docker-host-ip> or ./deploy-fresh.sh <docker-host-ip> [username]
# WARNING: This will DESTROY all existing data and containers!

if [ $# -eq 0 ]; then
    echo "Usage: $0 <user@docker-host-ip> or $0 <docker-host-ip> [username]"
    echo "Examples:"
    echo "  $0 misa@192.168.1.100"
    echo "  $0 192.168.1.100 misa"
    echo "  $0 192.168.1.100     # Will prompt for username"
    echo ""
    echo "⚠️  WARNING: This will COMPLETELY DESTROY all existing FinApp data!"
    exit 1
fi

# Parse arguments
if [[ "$1" == *"@"* ]]; then
    # Format: user@host
    DOCKER_HOST="$1"
else
    # Format: host [user]
    HOST="$1"
    if [ -n "$2" ]; then
        USER="$2"
    else
        read -p "Enter username for $HOST: " USER
    fi
    DOCKER_HOST="$USER@$HOST"
fi

DEPLOY_PATH="/opt/finapp"

echo "🔥 FRESH DEPLOYMENT: Completely destroying and rebuilding FinApp"
echo "🎯 Target: $DOCKER_HOST"
echo "📍 Path: $DEPLOY_PATH"
echo ""
echo "⚠️  WARNING: This will:"
echo "   - Stop and remove ALL FinApp containers"
echo "   - Delete ALL PostgreSQL data"
echo "   - Remove ALL Docker images"
echo "   - Delete entire deployment directory"
echo "   - Perform a completely fresh installation"
echo ""

read -p "Are you absolutely sure you want to proceed? Type 'YES' to continue: " confirm
if [ "$confirm" != "YES" ]; then
    echo "❌ Deployment cancelled."
    exit 1
fi

echo ""
echo "🚀 Starting fresh deployment..."

# Create deployment package (exclude unnecessary files)
echo "📦 Creating deployment package..."
tar --exclude='.git' \
    --exclude='bin' \
    --exclude='obj' \
    --exclude='*.db*' \
    --exclude='*.log' \
    --exclude='.vs' \
    --exclude='.vscode' \
    --exclude='logs' \
    --exclude='deployment' \
    -czf finapp-deploy.tar.gz -C .. .

echo "📤 Uploading to Docker host..."
scp finapp-deploy.tar.gz $DOCKER_HOST:/tmp/

echo "🔥 Performing complete destruction and fresh deployment..."
ssh $DOCKER_HOST << 'EOF'
    set -e
    
    echo "🛑 Stopping all FinApp containers..."
    cd /opt/finapp 2>/dev/null && docker compose down --volumes --remove-orphans || true
    
    echo "🗑️  Removing all FinApp containers..."
    docker ps -a --filter "name=finapp" --format "{{.ID}}" | xargs -r docker rm -f
    
    echo "🗑️  Removing all FinApp images..."
    docker images --filter "reference=*finapp*" --format "{{.ID}}" | xargs -r docker rmi -f
    docker images --filter "reference=finapp*" --format "{{.ID}}" | xargs -r docker rmi -f
    
    echo "🗑️  Removing all FinApp volumes..."
    docker volume ls --filter "name=finapp" --format "{{.Name}}" | xargs -r docker volume rm
    
    echo "🗑️  Removing all FinApp networks..."
    docker network ls --filter "name=finapp" --format "{{.Name}}" | xargs -r docker network rm
    
    echo "🧹 Cleaning up Docker system..."
    docker system prune -f
    
    echo "🗑️  Completely removing deployment directory..."
    sudo rm -rf /opt/finapp
    
    echo "📁 Creating fresh deployment directory..."
    sudo mkdir -p /opt/finapp
    sudo chown $USER:$USER /opt/finapp
    
    echo "📦 Extracting fresh application..."
    cd /opt/finapp
    tar -xzf /tmp/finapp-deploy.tar.gz
    
    echo "🏗️  Building and starting fresh application..."
    docker compose up -d --build
    
    echo "⏳ Waiting for PostgreSQL and application to initialize..."
    sleep 45
    
    echo "🔍 Checking deployment status..."
    if docker compose ps | grep -q "Up"; then
        echo ""
        echo "✅ FRESH DEPLOYMENT SUCCESSFUL!"
        echo "🌐 Application URL: http://$(hostname -I | awk '{print $1}'):5000"
        echo "🐘 PostgreSQL: Fresh database with clean schema"
        echo "📋 Container status:"
        docker compose ps
        echo ""
        echo "📝 Recent application logs:"
        docker compose logs --tail=15 finapp
        echo ""
        echo "📝 PostgreSQL logs:"
        docker compose logs --tail=10 postgres
        echo ""
        echo "🎉 Your FinApp is ready with a completely fresh installation!"
    else
        echo ""
        echo "❌ DEPLOYMENT FAILED!"
        echo "📋 Container status:"
        docker compose ps
        echo ""
        echo "📝 Full logs:"
        docker compose logs
        exit 1
    fi
    
    echo "🧹 Cleaning up temporary files..."
    rm -f /tmp/finapp-deploy.tar.gz
EOF

# Cleanup local files
rm finapp-deploy.tar.gz

echo ""
echo "✨ FRESH DEPLOYMENT COMPLETE!"
echo "🌐 Your FinApp is accessible at: http://$(echo $DOCKER_HOST | cut -d'@' -f2):5000"
echo "🐘 PostgreSQL database is completely fresh and ready"
echo "🔒 All previous data has been permanently destroyed"
echo "🎯 You can now start using your clean FinApp installation"
