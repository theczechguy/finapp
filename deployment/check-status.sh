#!/bin/bash

# FinApp Deployment Status Checker
# Usage: ./check-status.sh <user@docker-host-ip> or ./check-status.sh <docker-host-ip> [username]

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

echo "🔍 Checking FinApp status on: $DOCKER_HOST"

ssh $DOCKER_HOST << EOF
    cd $DEPLOY_PATH 2>/dev/null || { echo "❌ FinApp not deployed in $DEPLOY_PATH"; exit 1; }
    
    echo "📋 Current deployment status:"
    
    # Check PostgreSQL deployment
    if docker compose ps | grep -q "finapp"; then
        echo "🐘 PostgreSQL deployment active"
        echo "📦 Containers:"
        docker compose ps
        echo ""
        echo "📊 Resource usage:"
        docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}"
        echo ""
        echo "🔗 Database connection:"
        if docker compose ps postgres | grep -q "Up"; then
            echo "✅ PostgreSQL is running"
        else
            echo "❌ PostgreSQL is not running"
        fi
    else
        echo "❌ No FinApp containers running"
        echo "💡 To deploy: ./deploy.sh user@hostname"
    fi
    
    echo ""
    echo "🌐 Application URL: http://$(hostname -I | awk '{print \$1}'):5000"
EOF
