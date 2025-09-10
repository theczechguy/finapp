# FinApp Deployment Tools

This directory contains all deployment-related scripts and configuration files for FinApp.

## 📁 Files Overview

### 🔧 **Core Deployment Scripts**
- **`deploy.sh`** - Regular deployment (preserves existing data)
- **`deploy-fresh.sh`** - Fresh deployment (destroys ALL data)
- **`check-status.sh`** - Check current deployment status
- **`check-deployment.sh`** - Assess what's currently deployed

### 🐳 **Docker Configuration**
- **`Dockerfile`** - Application container definition
- **`docker-compose.yml`** - PostgreSQL + FinApp service orchestration

## 🚀 Quick Usage

### From Project Root (Recommended)
```bash
# Check deployment status
./finapp check user@your-server

# Assess current deployment  
./finapp assess user@your-server

# Regular deployment
./finapp deploy user@your-server

# Fresh deployment (destroys data)
./finapp fresh user@your-server
```

### Direct Script Usage
```bash
cd deployment

# Check what's deployed
./check-deployment.sh user@your-server

# Regular deployment
./deploy.sh user@your-server

# Fresh deployment (with data destruction)
./deploy-fresh.sh user@your-server

# Check status after deployment
./check-status.sh user@your-server
```

## 📋 Deployment Process

### 1. **Assessment**
```bash
./finapp assess user@your-server
```
- Shows current containers, images, volumes
- Displays PostgreSQL data size
- Checks port usage and disk space

### 2. **Choose Deployment Type**

#### Regular Deployment (Data Preserved)
```bash
./finapp deploy user@your-server
```
- Updates application code
- Preserves PostgreSQL data
- Performs rolling update

#### Fresh Deployment (Data Destroyed)
```bash
./finapp fresh user@your-server
```
- ⚠️ **Destroys ALL data permanently**
- Removes all containers, images, volumes
- Creates completely fresh installation
- Requires typing 'YES' to confirm

### 3. **Verification**
```bash
./finapp check user@your-server
```
- Verifies containers are running
- Shows resource usage
- Displays recent logs

## 🛡️ Safety Features

### **deploy-fresh.sh**
- Multiple warnings about data destruction
- Confirmation prompt (must type 'YES')
- Comprehensive cleanup of Docker resources
- Detailed logging throughout process

### **All Scripts**
- Error handling with `set -e`
- Clear status messages
- Proper cleanup of temporary files
- Host connection validation

## 🔧 Configuration

### **PostgreSQL Settings**
- Database: `finapp`
- Username: `finapp` 
- Password: `<YOUR_PASSWORD>`
- Port: `5432`

### **Application Settings**
- Port: `5000`
- Environment: `Production`
- Health checks enabled
- Automatic restart policy

## 📊 Docker Compose Services

### **PostgreSQL**
- Image: `postgres:16-alpine`
- Persistent volume: `postgres_data`
- Health checks with `pg_isready`

### **FinApp**
- Built from source using Dockerfile
- Depends on PostgreSQL health
- Automated migration on startup
- Health checks via HTTP endpoint

## 🚨 Important Notes

1. **Data Destruction**: `deploy-fresh.sh` permanently destroys all data
2. **Network Access**: Scripts require SSH access to target server
3. **Docker Requirements**: Target server must have Docker and Docker Compose
4. **Permissions**: Scripts may need sudo access for directory creation
5. **Port Conflicts**: Ensure ports 5000 and 5432 are available

## 🔍 Troubleshooting

### Check Logs
```bash
ssh user@server "cd /opt/finapp && docker compose logs finapp"
ssh user@server "cd /opt/finapp && docker compose logs postgres"
```

### Manual Container Management
```bash
ssh user@server "cd /opt/finapp && docker compose down"
ssh user@server "cd /opt/finapp && docker compose up -d"
```

### Database Connection Test
```bash
ssh user@server "docker exec finapp-postgres pg_isready -U finapp"
```

## 📁 Directory Structure After Deployment

```
/opt/finapp/
├── docker-compose.yml
├── Dockerfile
├── InvestmentTracker/
│   ├── appsettings.Production.json
│   ├── Program.cs
│   └── ... (application files)
└── postgres_data/ (Docker volume mount)
```
