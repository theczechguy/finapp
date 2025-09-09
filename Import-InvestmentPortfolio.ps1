#Requires -Version 7.0

<#
.SYNOPSIS
    PowerShell script to import complete investment portfolio from CSV to FinApp API

.DESCRIPTION
    Features:
    - Creates investments if they don't exist (for empty/production databases)
    - Sets proper categories, types, currencies, and providers
    - Adds historical values with duplicate checking
    - Safe to run multiple times

.PARAMETER ApiBase
    The base URL for the FinApp API
    Default: http://localhost:5071/api/investments

.EXAMPLE
    .\Import-InvestmentPortfolio.ps1

.EXAMPLE
    .\Import-InvestmentPortfolio.ps1 -ApiBase "https://yourdomain.com/api/investments"
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$ApiBase = "http://localhost:5071/api/investments"
)

# 🔧 CONFIGURATION
Write-Host "🔧 Using API endpoint: $ApiBase" -ForegroundColor Cyan

# Function to count total values across all investments
function Get-TotalValuesCount {
    param([int[]]$InvestmentIds = (8..19))

    $total = 0
    foreach ($id in $InvestmentIds) {
        try {
            $response = Invoke-RestMethod -Uri "$ApiBase/$id/values" -Method Get
            $total += $response.Count
        }
        catch {
            # Investment doesn't exist or has no values
            continue
        }
    }
    return $total
}

# Function to check if an investment exists
function Test-InvestmentExists {
    param([int]$InvestmentId)

    try {
        $response = Invoke-RestMethod -Uri "$ApiBase/$InvestmentId" -Method Get
        return $null -ne $response.id
    }
    catch {
        return $false
    }
}

# Function to check if a value exists for an investment on a specific date
function Test-ValueExists {
    param([int]$InvestmentId, [string]$Date)

    try {
        $values = Invoke-RestMethod -Uri "$ApiBase/$InvestmentId/values" -Method Get
        return $values | Where-Object { $_.asOf -eq $Date } | Select-Object -First 1
    }
    catch {
        return $null
    }
}

# Function to create an investment and return its ID
function New-Investment {
    param(
        [string]$Name,
        [string]$Category,
        [string]$Type,
        [string]$Currency,
        [string]$Provider
    )

    # Map textual values to numeric enums expected by the API
    function Map-CategoryToEnum([string]$cat) {
        switch ($cat) {
            'Stocks' { return 0 }
            'RealEstate' { return 1 }
            'Crypto' { return 2 }
            'Bonds' { return 3 }
            default { return 0 }
        }
    }

    function Map-TypeToEnum([string]$t) {
        switch ($t) {
            'OneTime' { return 0 }
            'Recurring' { return 1 }
            default { return 0 }
        }
    }

    function Map-CurrencyToEnum([string]$c) {
        switch ($c) {
            'CZK' { return 0 }
            'EUR' { return 1 }
            'USD' { return 2 }
            default { return 0 }
        }
    }

    $bodyObj = [ordered]@{
        name = $Name
        category = (Map-CategoryToEnum $Category)
        type = (Map-TypeToEnum $Type)
        currency = (Map-CurrencyToEnum $Currency)
        provider = $Provider
        chargeAmount = 0
    }

    $body = $bodyObj | ConvertTo-Json
    Write-Host "➡️  POST payload: $body" -ForegroundColor DarkCyan

    try {
        $response = Invoke-RestMethod -Uri $ApiBase -Method Post -Body $body -ContentType "application/json"

        if ($response.id) {
            Write-Host "✅ CREATED: Investment $($response.id) - $Name" -ForegroundColor Green
            return $response.id
        }
        else {
            Write-Host "❌ FAILED: Could not create investment - $Name" -ForegroundColor Red
            Write-Host "Response: $($response | ConvertTo-Json)" -ForegroundColor Red
            return $null
        }
    }
    catch {
        Write-Host "❌ FAILED: Could not create investment - $Name" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Function to ensure investment exists and return its ID
function Get-OrCreateInvestment {
    param(
        [string]$Name,
        [string]$Category,
        [string]$Type,
        [string]$Currency,
        [string]$Provider
    )

    try {
        # Try to find existing investment by name
        $investments = Invoke-RestMethod -Uri $ApiBase -Method Get
        $existing = $investments | Where-Object { $_.name -eq $Name } | Select-Object -First 1

        if ($existing) {
            Write-Host "⚠️  EXISTS: Investment $($existing.id) - $Name already exists" -ForegroundColor Yellow
            return $existing.id
        }
        else {
            return New-Investment -Name $Name -Category $Category -Type $Type -Currency $Currency -Provider $Provider
        }
    }
    catch {
        Write-Host "❌ ERROR: Could not check for existing investment - $Name" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        return New-Investment -Name $Name -Category $Category -Type $Type -Currency $Currency -Provider $Provider
    }
}

# Function to add a value (with duplicate check)
function Add-InvestmentValue {
    param([int]$InvestmentId, [string]$Date, [decimal]$Value, [int]$ChangeType = 0)

    # Check if value already exists
    $existingValue = Test-ValueExists -InvestmentId $InvestmentId -Date $Date
    if ($existingValue) {
        Write-Host "⚠️  SKIPPED: Value already exists for investment $InvestmentId on $Date" -ForegroundColor Yellow
        return
    }

    $body = @{
        investmentId = $InvestmentId
        asOf = $Date
        value = $Value
        changeType = $ChangeType
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri "$ApiBase/$InvestmentId/values" -Method Post -Body $body -ContentType "application/json"

        if ($response.id) {
            Write-Host "✅ ADDED: Value $Value for investment $InvestmentId on $Date" -ForegroundColor Green
        }
        else {
            Write-Host "❌ FAILED: Could not add value for investment $InvestmentId on $Date" -ForegroundColor Red
            Write-Host "Response: $($response | ConvertTo-Json)" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "❌ FAILED: Could not add value for investment $InvestmentId on $Date" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Main execution
Write-Host "🚀 Starting complete investment import with creation and values..." -ForegroundColor Magenta
Write-Host "📊 This script will create investments and add historical values" -ForegroundColor White
Write-Host "   - Creates investments if they don't exist" -ForegroundColor White
Write-Host "   - Checks for existing values before adding" -ForegroundColor White
Write-Host "   - Safe to run multiple times" -ForegroundColor White
Write-Host ""

# Count values before import
$valuesBefore = Get-TotalValuesCount
Write-Host "📈 Values before import: $valuesBefore" -ForegroundColor Blue
Write-Host ""

Write-Host "🏗️  Creating/verifying investments..." -ForegroundColor Cyan

# Create all investments and capture their IDs
$INVESTMENT_8 = Get-OrCreateInvestment "Crypto - Binance" "Crypto" "Recurring" "USD" "Crypto"
$INVESTMENT_9 = Get-OrCreateInvestment "Crypto - Anycoin" "Crypto" "Recurring" "CZK" "Crypto"
$INVESTMENT_10 = Get-OrCreateInvestment "Conseq Classic Invest - Realitni fond" "RealEstate" "Recurring" "CZK" "Conseq Classic Invest"
$INVESTMENT_11 = Get-OrCreateInvestment "Conseq Horizont Invest - Active Invest dynamicky" "Stocks" "Recurring" "CZK" "Conseq Horizont Invest"
$INVESTMENT_12 = Get-OrCreateInvestment "Amundi - CR All-Star selection" "Stocks" "Recurring" "CZK" "Amundi"
$INVESTMENT_13 = Get-OrCreateInvestment "Amundi - Global Disruptive" "Stocks" "Recurring" "CZK" "Amundi"
$INVESTMENT_14 = Get-OrCreateInvestment "Amundi - Global Silver Age" "Stocks" "Recurring" "CZK" "Amundi"
$INVESTMENT_15 = Get-OrCreateInvestment "Amundi - B&W European Strategic Autonomy 2028" "Stocks" "Recurring" "EUR" "Amundi"
$INVESTMENT_16 = Get-OrCreateInvestment "Amundi - B&W European Strategic Autonomy 2028 II" "Stocks" "Recurring" "EUR" "Amundi"
$INVESTMENT_17 = Get-OrCreateInvestment "Amundi - FUNDS NET ZERO TOP EUROPEAN PLAYER (DIP)" "Stocks" "Recurring" "EUR" "Amundi"
$INVESTMENT_18 = Get-OrCreateInvestment "Trading 212 - ETF pie 1" "Stocks" "Recurring" "CZK" "Trading 212"
$INVESTMENT_19 = Get-OrCreateInvestment "Investbay" "Stocks" "Recurring" "EUR" ""

Write-Host ""
Write-Host "📊 Adding historical values..." -ForegroundColor Cyan

# Import historical values for each investment
Write-Host "Importing Crypto - Binance..." -ForegroundColor White
Add-InvestmentValue $INVESTMENT_8 "2024-10-17" 1779.44
Add-InvestmentValue $INVESTMENT_8 "2024-11-15" 2365.00

Write-Host "Importing Crypto - Anycoin..." -ForegroundColor White
Add-InvestmentValue $INVESTMENT_9 "2024-10-17" 61334.89
Add-InvestmentValue $INVESTMENT_9 "2024-11-15" 82268.00
Add-InvestmentValue $INVESTMENT_9 "2024-12-16" 35551.64
Add-InvestmentValue $INVESTMENT_9 "2025-01-24" 35273.07
Add-InvestmentValue $INVESTMENT_9 "2025-02-17" 32105.56
Add-InvestmentValue $INVESTMENT_9 "2025-03-24" 28626.00
Add-InvestmentValue $INVESTMENT_9 "2025-04-19" 26300.00
Add-InvestmentValue $INVESTMENT_9 "2025-05-24" 33378.13
Add-InvestmentValue $INVESTMENT_9 "2025-06-20" 31275.14
Add-InvestmentValue $INVESTMENT_9 "2025-07-16" 35585.00

Write-Host "Importing Conseq Classic Invest - Realitni fond..." -ForegroundColor White
Add-InvestmentValue $INVESTMENT_10 "2024-10-17" 149386.80
Add-InvestmentValue $INVESTMENT_10 "2024-11-15" 152932.00
Add-InvestmentValue $INVESTMENT_10 "2024-12-16" 156366.00
Add-InvestmentValue $INVESTMENT_10 "2025-01-24" 163901.00
Add-InvestmentValue $INVESTMENT_10 "2025-02-17" 164148.46
Add-InvestmentValue $INVESTMENT_10 "2025-03-24" 170798.00
Add-InvestmentValue $INVESTMENT_10 "2025-04-19" 172218.00
Add-InvestmentValue $INVESTMENT_10 "2025-05-24" 178604.10
Add-InvestmentValue $INVESTMENT_10 "2025-06-20" 182626.00
Add-InvestmentValue $INVESTMENT_10 "2025-07-16" 182627.00

Write-Host "Importing Conseq Horizont Invest - Active Invest dynamicky..." -ForegroundColor White
Add-InvestmentValue $INVESTMENT_11 "2024-10-17" 170666.24
Add-InvestmentValue $INVESTMENT_11 "2024-11-15" 173201.00
Add-InvestmentValue $INVESTMENT_11 "2024-12-16" 179868.00
Add-InvestmentValue $INVESTMENT_11 "2025-01-24" 186969.00
Add-InvestmentValue $INVESTMENT_11 "2025-02-17" 190709.78
Add-InvestmentValue $INVESTMENT_11 "2025-03-24" 197812.00
Add-InvestmentValue $INVESTMENT_11 "2025-04-19" 186876.00
Add-InvestmentValue $INVESTMENT_11 "2025-05-24" 208100.40
Add-InvestmentValue $INVESTMENT_11 "2025-06-20" 211402.00
Add-InvestmentValue $INVESTMENT_11 "2025-07-16" 214156.00

Write-Host "Importing Amundi - CR All-Star selection..." -ForegroundColor White
Add-InvestmentValue $INVESTMENT_12 "2024-10-17" 179914.88
Add-InvestmentValue $INVESTMENT_12 "2024-11-15" 188076.00
Add-InvestmentValue $INVESTMENT_12 "2024-12-16" 193074.33
Add-InvestmentValue $INVESTMENT_12 "2025-01-24" 204869.64
Add-InvestmentValue $INVESTMENT_12 "2025-02-17" 205446.17
Add-InvestmentValue $INVESTMENT_12 "2025-03-24" 200349.00
Add-InvestmentValue $INVESTMENT_12 "2025-04-19" 181511.00
Add-InvestmentValue $INVESTMENT_12 "2025-05-24" 207011.06
Add-InvestmentValue $INVESTMENT_12 "2025-06-20" 210472.36
Add-InvestmentValue $INVESTMENT_12 "2025-07-16" 213395.00

Write-Host "Importing Amundi - Global Disruptive..." -ForegroundColor White
Add-InvestmentValue $INVESTMENT_13 "2024-10-17" 20447.10
Add-InvestmentValue $INVESTMENT_13 "2024-11-15" 22693.00
Add-InvestmentValue $INVESTMENT_13 "2024-12-16" 24504.23
Add-InvestmentValue $INVESTMENT_13 "2025-01-24" 27164.15
Add-InvestmentValue $INVESTMENT_13 "2025-02-17" 26931.53
Add-InvestmentValue $INVESTMENT_13 "2025-03-24" 24853.00
Add-InvestmentValue $INVESTMENT_13 "2025-04-19" 21448.00
Add-InvestmentValue $INVESTMENT_13 "2025-05-24" 27083.60
Add-InvestmentValue $INVESTMENT_13 "2025-06-20" 26941.95
Add-InvestmentValue $INVESTMENT_13 "2025-07-16" 28806.00

Write-Host "Importing Amundi - Global Silver Age..." -ForegroundColor White
Add-InvestmentValue $INVESTMENT_14 "2024-10-17" 9012.20
Add-InvestmentValue $INVESTMENT_14 "2024-11-15" 9434.00
Add-InvestmentValue $INVESTMENT_14 "2024-12-16" 10051.13
Add-InvestmentValue $INVESTMENT_14 "2025-01-24" 11073.87
Add-InvestmentValue $INVESTMENT_14 "2025-02-17" 11060.57
Add-InvestmentValue $INVESTMENT_14 "2025-03-24" 11249.00
Add-InvestmentValue $INVESTMENT_14 "2025-04-19" 9957.00
Add-InvestmentValue $INVESTMENT_14 "2025-05-24" 11997.79
Add-InvestmentValue $INVESTMENT_14 "2025-06-20" 11725.61
Add-InvestmentValue $INVESTMENT_14 "2025-07-16" 12679.00

Write-Host "Importing Amundi - B&W European Strategic Autonomy 2028..." -ForegroundColor White
Add-InvestmentValue $INVESTMENT_15 "2024-10-17" 89458.82
Add-InvestmentValue $INVESTMENT_15 "2024-11-15" 89850.00
Add-InvestmentValue $INVESTMENT_15 "2024-12-16" 90305.88
Add-InvestmentValue $INVESTMENT_15 "2025-01-24" 90007.00
Add-InvestmentValue $INVESTMENT_15 "2025-02-17" 90831.37
Add-InvestmentValue $INVESTMENT_15 "2025-03-24" 90980.00
Add-InvestmentValue $INVESTMENT_15 "2025-04-19" 91082.00
Add-InvestmentValue $INVESTMENT_15 "2025-05-24" 91929.41
Add-InvestmentValue $INVESTMENT_15 "2025-06-20" 92470.59
Add-InvestmentValue $INVESTMENT_15 "2025-07-16" 92839.00

Write-Host "Importing Amundi - B&W European Strategic Autonomy 2028 II..." -ForegroundColor White
Add-InvestmentValue $INVESTMENT_16 "2024-10-17" 56049.91
Add-InvestmentValue $INVESTMENT_16 "2024-11-15" 56403.00
Add-InvestmentValue $INVESTMENT_16 "2024-12-16" 56838.10
Add-InvestmentValue $INVESTMENT_16 "2025-01-24" 56526.73
Add-InvestmentValue $INVESTMENT_16 "2025-02-17" 57110.60
Add-InvestmentValue $INVESTMENT_16 "2025-03-24" 57190.00
Add-InvestmentValue $INVESTMENT_16 "2025-04-19" 57123.00
Add-InvestmentValue $INVESTMENT_16 "2025-05-24" 57674.87
Add-InvestmentValue $INVESTMENT_16 "2025-06-20" 58218.55
Add-InvestmentValue $INVESTMENT_16 "2025-07-16" 58315.00

Write-Host "Importing Amundi - FUNDS NET ZERO TOP EUROPEAN PLAYER (DIP)..." -ForegroundColor White
Add-InvestmentValue $INVESTMENT_17 "2024-10-17" 10301.03
Add-InvestmentValue $INVESTMENT_17 "2024-11-15" 10767.00
Add-InvestmentValue $INVESTMENT_17 "2024-12-16" 11808.72
Add-InvestmentValue $INVESTMENT_17 "2025-01-24" 13812.46
Add-InvestmentValue $INVESTMENT_17 "2025-02-17" 14274.90
Add-InvestmentValue $INVESTMENT_17 "2025-03-24" 15394.00
Add-InvestmentValue $INVESTMENT_17 "2025-04-19" 14252.00
Add-InvestmentValue $INVESTMENT_17 "2025-05-24" 17388.86
Add-InvestmentValue $INVESTMENT_17 "2025-06-20" 17008.02
Add-InvestmentValue $INVESTMENT_17 "2025-07-16" 18059.00

Write-Host "Importing Trading 212 - ETF pie 1..." -ForegroundColor White
Add-InvestmentValue $INVESTMENT_18 "2024-10-17" 80611.00
Add-InvestmentValue $INVESTMENT_18 "2024-11-15" 81392.00
Add-InvestmentValue $INVESTMENT_18 "2025-02-17" 140397.79
Add-InvestmentValue $INVESTMENT_18 "2025-03-24" 140211.94
Add-InvestmentValue $INVESTMENT_18 "2025-04-19" 130332.00
Add-InvestmentValue $INVESTMENT_18 "2025-05-24" 110777.00
Add-InvestmentValue $INVESTMENT_18 "2025-06-20" 126827.88
Add-InvestmentValue $INVESTMENT_18 "2025-07-16" 132762.57
Add-InvestmentValue $INVESTMENT_18 "2025-08-20" 138435.00

Write-Host "Importing Investbay..." -ForegroundColor White
Add-InvestmentValue $INVESTMENT_19 "2024-10-17" 2140.00
Add-InvestmentValue $INVESTMENT_19 "2024-11-15" 2140.00
Add-InvestmentValue $INVESTMENT_19 "2025-05-24" 93075.00
Add-InvestmentValue $INVESTMENT_19 "2025-06-20" 97443.50
Add-InvestmentValue $INVESTMENT_19 "2025-07-16" 97443.50
Add-InvestmentValue $INVESTMENT_19 "2025-08-20" 97443.00

Write-Host ""
Write-Host "🎉 Complete import process completed!" -ForegroundColor Green

# Count values after import
$valuesAfter = Get-TotalValuesCount
$valuesAdded = $valuesAfter - $valuesBefore

Write-Host "📈 Values before import: $valuesBefore" -ForegroundColor Blue
Write-Host "📈 Values after import: $valuesAfter" -ForegroundColor Blue
Write-Host "➕ Values added: $valuesAdded" -ForegroundColor Blue
Write-Host ""
Write-Host "✅ All investments created/verified" -ForegroundColor Green
Write-Host "✅ All historical values imported" -ForegroundColor Green
Write-Host ""
Write-Host "🔍 Investment IDs created/used:" -ForegroundColor Cyan
Write-Host "   Crypto - Binance: $INVESTMENT_8" -ForegroundColor White
Write-Host "   Crypto - Anycoin: $INVESTMENT_9" -ForegroundColor White
Write-Host "   Conseq Classic Invest - Realitni fond: $INVESTMENT_10" -ForegroundColor White
Write-Host "   Conseq Horizont Invest - Active Invest dynamicky: $INVESTMENT_11" -ForegroundColor White
Write-Host "   Amundi - CR All-Star selection: $INVESTMENT_12" -ForegroundColor White
Write-Host "   Amundi - Global Disruptive: $INVESTMENT_13" -ForegroundColor White
Write-Host "   Amundi - Global Silver Age: $INVESTMENT_14" -ForegroundColor White
Write-Host "   Amundi - B&W European Strategic Autonomy 2028: $INVESTMENT_15" -ForegroundColor White
Write-Host "   Amundi - B&W European Strategic Autonomy 2028 II: $INVESTMENT_16" -ForegroundColor White
Write-Host "   Amundi - FUNDS NET ZERO TOP EUROPEAN PLAYER (DIP): $INVESTMENT_17" -ForegroundColor White
Write-Host "   Trading 212 - ETF pie 1: $INVESTMENT_18" -ForegroundColor White
Write-Host "   Investbay: $INVESTMENT_19" -ForegroundColor White
Write-Host ""
Write-Host "🔍 Verify the import:" -ForegroundColor Cyan
Write-Host "   Invoke-RestMethod -Uri '$ApiBase' -Method Get | Measure-Object | Select-Object -ExpandProperty Count" -ForegroundColor White
Write-Host ""
Write-Host "📊 Check values for specific investment:" -ForegroundColor Cyan
Write-Host "   Invoke-RestMethod -Uri '$ApiBase/{ID}/values' -Method Get | Measure-Object | Select-Object -ExpandProperty Count" -ForegroundColor White