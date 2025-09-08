#!/bin/bash

# FinApp Docker Deployment Script
# Usage: ./deploy.sh <user@docker-host-ip> or ./deploy.sh <docker-host-ip> [username]

if [ $# -eq 0 ]; then
    echo "Usage: $0 <user@docker-host-ip> or $0 <docker-host-ip> [username]"
    echo "Examples:"
    echo "  $0 misa@192.168.1.100"
    echo "  $0 192.168.1.100 misa"
    echo "  $0 192.168.1.100     # Will prompt for username"
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

echo "� Deploying FinApp to Docker host: $DOCKER_HOST"

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

echo "🐳 Deploying with Docker and PostgreSQL..."
ssh $DOCKER_HOST << EOF
    set -e
    
    # Prepare deployment directory
    sudo mkdir -p $DEPLOY_PATH
    sudo chown \$USER:\$USER $DEPLOY_PATH
    
    # Stop existing application
    cd $DEPLOY_PATH 2>/dev/null && docker compose down || true
    
    # Extract new version
    cd $DEPLOY_PATH
    
    # Clean old files but preserve postgres data
    find . -mindepth 1 -maxdepth 1 ! -name 'postgres_data' -exec rm -rf {} +
    
    # Extract new files
    tar -xzf /tmp/finapp-deploy.tar.gz
    
    echo "🏗️  Building and starting application with PostgreSQL..."
    docker compose up -d --build
    
    echo "⏳ Waiting for PostgreSQL and application to start..."
    sleep 30
    
    # Check if application is running
    if docker compose ps | grep -q "Up"; then
        echo "✅ Deployment successful!"
        echo "🌐 Application should be available at: http://$(echo $DOCKER_HOST | cut -d'@' -f2):5000"
        echo "🐘 PostgreSQL database running on port 5432"
        echo "📋 Container status:"
        docker compose ps
        echo "📝 Recent logs:"
        docker compose logs --tail=10 finapp
    else
        echo "❌ Deployment failed!"
        echo "📋 Container status:"
        docker compose ps
        echo "📝 Error logs:"
        docker compose logs
        exit 1
    fi
    
    # Cleanup
    rm /tmp/finapp-deploy.tar.gz
EOF

# Cleanup local files
rm finapp-deploy.tar.gz

echo "✨ Deployment complete!"
echo "🌐 Your FinApp should be accessible at: http://$(echo $DOCKER_HOST | cut -d'@' -f2):5000"
echo "🐘 PostgreSQL database provides production-grade reliability and performance"
