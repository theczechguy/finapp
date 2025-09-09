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

ssh -T $DOCKER_HOST << EOF
    cd $DEPLOY_PATH 2>/dev/null || { echo "❌ FinApp not deployed in $DEPLOY_PATH"; exit 1; }
    
    echo "📋 Current deployment status:"
    echo "📁 Current directory: \$(pwd)"
    echo "📂 Directory contents:"
    ls -la
    
    # Check if FinApp directory exists
    if [ -d "FinApp" ]; then
        echo "✅ FinApp directory found"
        echo "📂 FinApp contents:"
        ls -la FinApp/
        
        if [ -d "FinApp/deployment" ]; then
            echo "✅ Deployment directory found"
        else
            echo "❌ Deployment directory NOT found in FinApp/"
            echo "💡 Expected: FinApp/deployment/"
            echo "🔍 Available directories:"
            find . -maxdepth 2 -type d | head -10
        fi
    else
        echo "❌ FinApp directory NOT found"
        echo "💡 Expected: FinApp/"
        echo "🔍 Available directories:"
        find . -maxdepth 1 -type d
    fi
    
    # Check PostgreSQL deployment
    if [ -d "FinApp/deployment" ]; then
        echo "🐘 PostgreSQL deployment active"
        echo "📦 Containers:"
        if cd "FinApp/deployment" 2>/dev/null; then
            docker compose ps 2>/dev/null || echo "   Unable to list containers"
            echo ""
            echo "📊 Resource usage:"
            docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}" 2>/dev/null || echo "   Unable to get resource usage"
            echo ""
            echo "🔗 Database connection:"
            echo "🔍 Checking PostgreSQL container status:"
            if docker compose ps postgres 2>/dev/null | grep -q "Up\|running\|healthy"; then
                echo "✅ PostgreSQL is running"
                docker compose ps postgres 2>/dev/null | tail -n 1 | sed 's/^/   Status: /'
            else
                echo "❌ PostgreSQL is not running"
                echo "   Full container list:"
                docker compose ps 2>/dev/null || echo "   Unable to get container list"
            fi
        else
            echo "❌ Cannot access deployment directory: FinApp/deployment"
            echo "   This might be a permissions issue"
        fi
    else
        echo "❌ No FinApp containers running"
        echo "💡 To deploy: ./deploy.sh user@hostname"
        echo "   Deployment directory not found: FinApp/deployment"
    fi
    
    echo ""
    echo "🌐 Application URL: http://\$(hostname -i 2>/dev/null || ip route get 1 2>/dev/null | awk '{print \$7}' 2>/dev/null || hostname -I 2>/dev/null | awk '{print \$1}' 2>/dev/null || echo 'localhost'):5000"
EOF
