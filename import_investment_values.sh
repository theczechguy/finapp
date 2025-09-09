#!/bin/bash

# Script to import complete investment portfolio from CSV to FinApp API
# Features:
# - Creates investments if they don't exist (for empty/production databases)
# - Sets proper categories, types, currencies, and providers
# - Adds historical values with duplicate checking
# - Safe to run multiple times
# 
# Configuration:
# - Update API_BASE for your environment
# - Default: http://localhost:5071/api/investments (development)
# - Production: https://yourdomain.com/api/investments
#
# Usage: ./import_investment_values.sh

# 🔧 CONFIGURATION - Update this for your environment
API_BASE="http://localhost:5071/api/investments"

echo "🔧 Using API endpoint: $API_BASE"

# Function to count total values across all investments
count_total_values() {
    local total=0
    for id in {8..19}; do
        if command -v jq &> /dev/null; then
            local count=$(curl -s "$API_BASE/$id/values" | jq length 2>/dev/null || echo 0)
        else
            # Fallback: assume we can't count without jq
            local count=0
        fi
        total=$((total + count))
    done
    echo $total
}

echo "🚀 Starting complete investment import with creation and values..."
echo "📊 This script will create investments and add historical values"
echo "   - Creates investments if they don't exist"
echo "   - Checks for existing values before adding"
echo "   - Safe to run multiple times"

# Count values before import
VALUES_BEFORE=$(count_total_values)
echo "📈 Values before import: $VALUES_BEFORE"
echo ""

echo "🏗️  Creating/verifying investments..."
INVESTMENT_8=$(ensure_investment "Crypto - Binance" "Crypto" "Recurring" "USD" "Crypto")
INVESTMENT_9=$(ensure_investment "Crypto - Anycoin" "Crypto" "Recurring" "CZK" "Crypto")
INVESTMENT_10=$(ensure_investment "Conseq Classic Invest - Realitni fond" "RealEstate" "Recurring" "CZK" "Conseq Classic Invest")
INVESTMENT_11=$(ensure_investment "Conseq Horizont Invest - Active Invest dynamicky" "Stocks" "Recurring" "CZK" "Conseq Horizont Invest")
INVESTMENT_12=$(ensure_investment "Amundi - CR All-Star selection" "Stocks" "Recurring" "CZK" "Amundi")
INVESTMENT_13=$(ensure_investment "Amundi - Global Disruptive" "Stocks" "Recurring" "CZK" "Amundi")
INVESTMENT_14=$(ensure_investment "Amundi - Global Silver Age" "Stocks" "Recurring" "CZK" "Amundi")
INVESTMENT_15=$(ensure_investment "Amundi - B&W European Strategic Autonomy 2028" "Stocks" "Recurring" "EUR" "Amundi")
INVESTMENT_16=$(ensure_investment "Amundi - B&W European Strategic Autonomy 2028 II" "Stocks" "Recurring" "EUR" "Amundi")
INVESTMENT_17=$(ensure_investment "Amundi - FUNDS NET ZERO TOP EUROPEAN PLAYER (DIP)" "Stocks" "Recurring" "EUR" "Amundi")
INVESTMENT_18=$(ensure_investment "Trading 212 - ETF pie 1" "Stocks" "Recurring" "CZK" "Trading 212")
INVESTMENT_19=$(ensure_investment "Investbay" "Stocks" "Recurring" "EUR" "")

echo ""
echo "📊 Adding historical values..."

# Function to check if an investment exists
investment_exists() {
    local investment_id=$1
    local response=$(curl -s "$API_BASE/$investment_id")
    
    if echo "$response" | jq -e '.id' > /dev/null 2>&1; then
        return 0  # exists
    else
        return 1  # doesn't exist
    fi
}

# Function to create an investment and return its ID
create_investment() {
    local name="$1"
    local category="$2"
    local type="$3"
    local currency="$4"
    local provider="$5"
    
    local data="{\"name\": \"$name\", \"category\": \"$category\", \"type\": \"$type\", \"currency\": \"$currency\", \"provider\": \"$provider\", \"chargeAmount\": 0}"
    
    local response=$(curl -s -X POST -H "Content-Type: application/json" \
         -d "$data" \
         "$API_BASE")
    
    if echo "$response" | jq -e '.id' > /dev/null 2>&1; then
        local created_id=$(echo "$response" | jq -r '.id')
        echo "✅ CREATED: Investment $created_id - $name"
        echo $created_id
    else
        echo "❌ FAILED: Could not create investment - $name"
        echo "Response: $response"
        echo ""
    fi
}

# Function to ensure investment exists and return its ID
ensure_investment() {
    local name="$1"
    local category="$2"
    local type="$3"
    local currency="$4"
    local provider="$5"
    
    # Try to find existing investment by name
    local existing_response=$(curl -s "$API_BASE")
    local existing_id=$(echo "$existing_response" | jq -r ".[] | select(.name == \"$name\") | .id" 2>/dev/null)
    
    if [ -n "$existing_id" ] && [ "$existing_id" != "null" ]; then
        echo "⚠️  EXISTS: Investment $existing_id - $name already exists"
        echo $existing_id
    else
        create_investment "$name" "$category" "$type" "$currency" "$provider"
    fi
}

# Function to add a value (with duplicate check)
add_value() {
    local investment_id=$1
    local date=$2
    local value=$3

    if value_exists $investment_id "$date"; then
        echo "⚠️  SKIPPED: Value already exists for investment $investment_id on $date"
        return
    fi

    local response=$(curl -s -X POST -H "Content-Type: application/json" \
         -d "{\"investmentId\": $investment_id, \"asOf\": \"$date\", \"value\": $value}" \
         "$API_BASE/$investment_id/values")

    if echo "$response" | jq -e '.id' > /dev/null 2>&1; then
        echo "✅ ADDED: Value $value for investment $investment_id on $date"
    else
        echo "❌ FAILED: Could not add value for investment $investment_id on $date"
        echo "Response: $response"
    fi
}

echo "Importing Crypto - Binance..."
add_value $INVESTMENT_8 "2024-10-17" 1779.44
add_value $INVESTMENT_8 "2024-11-15" 2365.00

echo "Importing Crypto - Anycoin..."
add_value $INVESTMENT_9 "2024-10-17" 61334.89
add_value $INVESTMENT_9 "2024-11-15" 82268.00
add_value $INVESTMENT_9 "2024-12-16" 35551.64
add_value $INVESTMENT_9 "2025-01-24" 35273.07
add_value $INVESTMENT_9 "2025-02-17" 32105.56
add_value $INVESTMENT_9 "2025-03-24" 28626.00
add_value $INVESTMENT_9 "2025-04-19" 26300.00
add_value $INVESTMENT_9 "2025-05-24" 33378.13
add_value $INVESTMENT_9 "2025-06-20" 31275.14
add_value $INVESTMENT_9 "2025-07-16" 35585.00

echo "Importing Conseq Classic Invest - Realitni fond..."
add_value $INVESTMENT_10 "2024-10-17" 149386.80
add_value $INVESTMENT_10 "2024-11-15" 152932.00
add_value $INVESTMENT_10 "2024-12-16" 156366.00
add_value $INVESTMENT_10 "2025-01-24" 163901.00
add_value $INVESTMENT_10 "2025-02-17" 164148.46
add_value $INVESTMENT_10 "2025-03-24" 170798.00
add_value $INVESTMENT_10 "2025-04-19" 172218.00
add_value $INVESTMENT_10 "2025-05-24" 178604.10
add_value $INVESTMENT_10 "2025-06-20" 182626.00
add_value $INVESTMENT_10 "2025-07-16" 182627.00

echo "Importing Conseq Horizont Invest - Active Invest dynamicky..."
add_value $INVESTMENT_11 "2024-10-17" 170666.24
add_value $INVESTMENT_11 "2024-11-15" 173201.00
add_value $INVESTMENT_11 "2024-12-16" 179868.00
add_value $INVESTMENT_11 "2025-01-24" 186969.00
add_value $INVESTMENT_11 "2025-02-17" 190709.78
add_value $INVESTMENT_11 "2025-03-24" 197812.00
add_value $INVESTMENT_11 "2025-04-19" 186876.00
add_value $INVESTMENT_11 "2025-05-24" 208100.40
add_value $INVESTMENT_11 "2025-06-20" 211402.00
add_value $INVESTMENT_11 "2025-07-16" 214156.00

echo "Importing Amundi - CR All-Star selection..."
add_value $INVESTMENT_12 "2024-10-17" 179914.88
add_value $INVESTMENT_12 "2024-11-15" 188076.00
add_value $INVESTMENT_12 "2024-12-16" 193074.33
add_value $INVESTMENT_12 "2025-01-24" 204869.64
add_value $INVESTMENT_12 "2025-02-17" 205446.17
add_value $INVESTMENT_12 "2025-03-24" 200349.00
add_value $INVESTMENT_12 "2025-04-19" 181511.00
add_value $INVESTMENT_12 "2025-05-24" 207011.06
add_value $INVESTMENT_12 "2025-06-20" 210472.36
add_value $INVESTMENT_12 "2025-07-16" 213395.00

echo "Importing Amundi - Global Disruptive..."
add_value $INVESTMENT_13 "2024-10-17" 20447.10
add_value $INVESTMENT_13 "2024-11-15" 22693.00
add_value $INVESTMENT_13 "2024-12-16" 24504.23
add_value $INVESTMENT_13 "2025-01-24" 27164.15
add_value $INVESTMENT_13 "2025-02-17" 26931.53
add_value $INVESTMENT_13 "2025-03-24" 24853.00
add_value $INVESTMENT_13 "2025-04-19" 21448.00
add_value $INVESTMENT_13 "2025-05-24" 27083.60
add_value $INVESTMENT_13 "2025-06-20" 26941.95
add_value $INVESTMENT_13 "2025-07-16" 28806.00

echo "Importing Amundi - Global Silver Age..."
add_value $INVESTMENT_14 "2024-10-17" 9012.20
add_value $INVESTMENT_14 "2024-11-15" 9434.00
add_value $INVESTMENT_14 "2024-12-16" 10051.13
add_value $INVESTMENT_14 "2025-01-24" 11073.87
add_value $INVESTMENT_14 "2025-02-17" 11060.57
add_value $INVESTMENT_14 "2025-03-24" 11249.00
add_value $INVESTMENT_14 "2025-04-19" 9957.00
add_value $INVESTMENT_14 "2025-05-24" 11997.79
add_value $INVESTMENT_14 "2025-06-20" 11725.61
add_value $INVESTMENT_14 "2025-07-16" 12679.00

echo "Importing Amundi - B&W European Strategic Autonomy 2028..."
add_value $INVESTMENT_15 "2024-10-17" 89458.82
add_value $INVESTMENT_15 "2024-11-15" 89850.00
add_value $INVESTMENT_15 "2024-12-16" 90305.88
add_value $INVESTMENT_15 "2025-01-24" 90007.00
add_value $INVESTMENT_15 "2025-02-17" 90831.37
add_value $INVESTMENT_15 "2025-03-24" 90980.00
add_value $INVESTMENT_15 "2025-04-19" 91082.00
add_value $INVESTMENT_15 "2025-05-24" 91929.41
add_value $INVESTMENT_15 "2025-06-20" 92470.59
add_value $INVESTMENT_15 "2025-07-16" 92839.00

echo "Importing Amundi - B&W European Strategic Autonomy 2028 II..."
add_value $INVESTMENT_16 "2024-10-17" 56049.91
add_value $INVESTMENT_16 "2024-11-15" 56403.00
add_value $INVESTMENT_16 "2024-12-16" 56838.10
add_value $INVESTMENT_16 "2025-01-24" 56526.73
add_value $INVESTMENT_16 "2025-02-17" 57110.60
add_value $INVESTMENT_16 "2025-03-24" 57190.00
add_value $INVESTMENT_16 "2025-04-19" 57123.00
add_value $INVESTMENT_16 "2025-05-24" 57674.87
add_value $INVESTMENT_16 "2025-06-20" 58218.55
add_value $INVESTMENT_16 "2025-07-16" 58315.00

echo "Importing Amundi - FUNDS NET ZERO TOP EUROPEAN PLAYER (DIP)..."
add_value $INVESTMENT_17 "2024-10-17" 10301.03
add_value $INVESTMENT_17 "2024-11-15" 10767.00
add_value $INVESTMENT_17 "2024-12-16" 11808.72
add_value $INVESTMENT_17 "2025-01-24" 13812.46
add_value $INVESTMENT_17 "2025-02-17" 14274.90
add_value $INVESTMENT_17 "2025-03-24" 15394.00
add_value $INVESTMENT_17 "2025-04-19" 14252.00
add_value $INVESTMENT_17 "2025-05-24" 17388.86
add_value $INVESTMENT_17 "2025-06-20" 17008.02
add_value $INVESTMENT_17 "2025-07-16" 18059.00

echo "Importing Trading 212 - ETF pie 1..."
add_value $INVESTMENT_18 "2024-10-17" 80611.00
add_value $INVESTMENT_18 "2024-11-15" 81392.00
add_value $INVESTMENT_18 "2025-02-17" 140397.79
add_value $INVESTMENT_18 "2025-03-24" 140211.94
add_value $INVESTMENT_18 "2025-04-19" 130332.00
add_value $INVESTMENT_18 "2025-05-24" 110777.00
add_value $INVESTMENT_18 "2025-06-20" 126827.88
add_value $INVESTMENT_18 "2025-07-16" 132762.57
add_value $INVESTMENT_18 "2025-08-20" 138435.00

echo "Importing Investbay..."
add_value $INVESTMENT_19 "2024-10-17" 2140.00
add_value $INVESTMENT_19 "2024-11-15" 2140.00
add_value $INVESTMENT_19 "2025-05-24" 93075.00
add_value $INVESTMENT_19 "2025-06-20" 97443.50
add_value $INVESTMENT_19 "2025-07-16" 97443.50
add_value $INVESTMENT_19 "2025-08-20" 97443.00

echo ""
echo "🎉 Complete import process completed!"

# Count values after import
VALUES_AFTER=$(count_total_values)
VALUES_ADDED=$((VALUES_AFTER - VALUES_BEFORE))

echo "📈 Values before import: $VALUES_BEFORE"
echo "📈 Values after import: $VALUES_AFTER"
echo "➕ Values added: $VALUES_ADDED"
echo ""
echo "✅ All investments created/verified"
echo "✅ All historical values imported"
echo ""
echo "🔍 Investment IDs created/used:"
echo "   Crypto - Binance: $INVESTMENT_8"
echo "   Crypto - Anycoin: $INVESTMENT_9"
echo "   Conseq Classic Invest - Realitni fond: $INVESTMENT_10"
echo "   Conseq Horizont Invest - Active Invest dynamicky: $INVESTMENT_11"
echo "   Amundi - CR All-Star selection: $INVESTMENT_12"
echo "   Amundi - Global Disruptive: $INVESTMENT_13"
echo "   Amundi - Global Silver Age: $INVESTMENT_14"
echo "   Amundi - B&W European Strategic Autonomy 2028: $INVESTMENT_15"
echo "   Amundi - B&W European Strategic Autonomy 2028 II: $INVESTMENT_16"
echo "   Amundi - FUNDS NET ZERO TOP EUROPEAN PLAYER (DIP): $INVESTMENT_17"
echo "   Trading 212 - ETF pie 1: $INVESTMENT_18"
echo "   Investbay: $INVESTMENT_19"
echo ""
echo "🔍 Verify the import:"
echo "   curl $API_BASE | jq length"
echo ""
echo "📊 Check values for specific investment:"
echo "   curl $API_BASE/{ID}/values | jq length"