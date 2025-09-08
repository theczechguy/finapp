#!/bin/bash

# FinApp Pre-Deployment Assessment Script
# Usage: ./check-deployment.sh <user@docker-host-ip> or ./check-deployment.sh <docker-host-ip> [username]

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

echo "🔍 Checking current FinApp deployment status on: $DOCKER_HOST"
echo ""

ssh $DOCKER_HOST << 'EOF'
    echo "📁 Deployment directory status:"
    if [ -d "/opt/finapp" ]; then
        echo "✅ /opt/finapp exists"
        echo "📊 Directory size: $(du -sh /opt/finapp 2>/dev/null || echo 'Cannot calculate')"
        echo "📅 Last modified: $(stat -c %y /opt/finapp 2>/dev/null || echo 'Cannot determine')"
    else
        echo "❌ /opt/finapp does not exist"
    fi
    echo ""
    
    echo "🐳 Docker containers related to FinApp:"
    finapp_containers=$(docker ps -a --filter "name=finapp" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}")
    if [ -n "$finapp_containers" ]; then
        echo "$finapp_containers"
    else
        echo "❌ No FinApp containers found"
    fi
    echo ""
    
    echo "🖼️  Docker images related to FinApp:"
    finapp_images=$(docker images --filter "reference=*finapp*" --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}")
    if [ -n "$finapp_images" ]; then
        echo "$finapp_images"
    else
        echo "❌ No FinApp images found"
    fi
    echo ""
    
    echo "💾 Docker volumes related to FinApp:"
    finapp_volumes=$(docker volume ls --filter "name=finapp" --format "table {{.Name}}\t{{.Driver}}")
    if [ -n "$finapp_volumes" ]; then
        echo "$finapp_volumes"
        echo ""
        echo "📊 Volume details:"
        docker volume ls --filter "name=finapp" --format "{{.Name}}" | while read volume; do
            if [ -n "$volume" ]; then
                echo "  $volume: $(docker volume inspect "$volume" --format '{{.Mountpoint}}' 2>/dev/null || echo 'Cannot inspect')"
            fi
        done
    else
        echo "❌ No FinApp volumes found"
    fi
    echo ""
    
    echo "🌐 Docker networks related to FinApp:"
    finapp_networks=$(docker network ls --filter "name=finapp" --format "table {{.Name}}\t{{.Driver}}")
    if [ -n "$finapp_networks" ]; then
        echo "$finapp_networks"
    else
        echo "❌ No FinApp networks found"
    fi
    echo ""
    
    echo "🐘 PostgreSQL data check:"
    postgres_volume=$(docker volume ls --filter "name=postgres" --format "{{.Name}}")
    if [ -n "$postgres_volume" ]; then
        echo "✅ PostgreSQL volume found: $postgres_volume"
        echo "📊 Estimated data size: $(docker run --rm -v "$postgres_volume":/data alpine du -sh /data 2>/dev/null || echo 'Cannot calculate')"
    else
        echo "❌ No PostgreSQL volumes found"
    fi
    echo ""
    
    echo "⚡ Active services on common ports:"
    echo "Port 5000 (FinApp): $(ss -tuln | grep ':5000' && echo 'In use' || echo 'Available')"
    echo "Port 5432 (PostgreSQL): $(ss -tuln | grep ':5432' && echo 'In use' || echo 'Available')"
    echo ""
    
    echo "💾 Available disk space:"
    df -h /opt 2>/dev/null || df -h /
    echo ""
    
    if [ -d "/opt/finapp" ]; then
        echo "📋 Current docker-compose status:"
        cd /opt/finapp && docker compose ps 2>/dev/null || echo "No docker-compose services found"
        echo ""
        
        echo "📝 Recent application logs (if available):"
        cd /opt/finapp && docker compose logs --tail=5 finapp 2>/dev/null || echo "No application logs available"
    fi
EOF

echo ""
echo "🎯 Assessment complete!"
echo ""
echo "📌 Next steps:"
echo "   • To perform a fresh deployment that destroys all data: ./deploy-fresh.sh $DOCKER_HOST"
echo "   • To perform a regular update deployment: ./deploy.sh $DOCKER_HOST"
echo "   • To check status after deployment: ./check-status.sh $DOCKER_HOST"
