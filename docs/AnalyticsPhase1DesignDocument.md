# FinApp Analytics Phase 1 Design Document

## Expense Analytics Dashboard Implementation

**Date:** September 9, 2025  
**Version:** 1.0  
**Author:** GitHub Copilot  
**Status:** Design Phase  

---

## 1. Executive Summary

This design document outlines the implementation of Phase 1 analytics features for FinApp, focusing on category breakdown visualization with supporting charts and date range analysis. The solution leverages Chart.js for visualizations and integrates seamlessly with the existing ASP.NET Core architecture.

**Key Features:**
- Category breakdown with donut/pie charts
- Top 10 categories + "Other" grouping
- Date range picker (default: last 3 months)
- Combination of pie and bar charts
- Mobile-responsive design
- Integration with existing expense data

---

## 2. Requirements

### 2.1 Functional Requirements
- **REQ-1:** Display expense distribution by category (donut chart)
- **REQ-2:** Show top 10 categories + "Other" grouping
- **REQ-3:** Date range picker with 3-month default
- **REQ-4:** Bar chart showing monthly spending trends
- **REQ-5:** Include both regular and irregular expenses
- **REQ-6:** Mobile-responsive chart display
- **REQ-7:** Static reports (no real-time updates)

### 2.2 Non-Functional Requirements
- **PERF-1:** Support 2 concurrent users with 12 months data
- **UI-1:** Match existing Bootstrap Darkly theme
- **MOB-1:** Somewhat mobile-responsive (charts work on mobile)
- **MAINT-1:** Easy to extend with additional analytics

---

## 3. Technical Architecture

### 3.1 System Overview

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Analytics     │───▶│  Analytics       │───▶│  Chart.js       │
│   Page          │    │  Service         │    │  Visualizations  │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌──────────────────┐
                       │  Expense Data    │
                       │  (Regular +      │
                       │   Irregular)     │
                       └──────────────────┘
```

### 3.2 Key Components

#### 3.2.1 Analytics Page
```
Pages/Analytics/Index.cshtml
Pages/Analytics/Index.cshtml.cs
```

#### 3.2.2 Analytics Service
```csharp
public class AnalyticsService
{
    public CategoryBreakdownData GetCategoryBreakdown(DateTime start, DateTime end);
    public MonthlyTrendData GetMonthlyTrends(DateTime start, DateTime end);
}
```

#### 3.2.3 Data Models
```csharp
public class CategoryBreakdownData
{
    public List<CategoryItem> Categories { get; set; }
    public decimal TotalAmount { get; set; }
}

public class CategoryItem
{
    public string CategoryName { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}
```

---

## 4. Implementation Plan

### Phase 1.1: Infrastructure Setup (Days 1-2)

#### 4.1.1 Create Analytics Page Structure
- [ ] Create `Pages/Analytics/Index.cshtml`
- [ ] Create `Pages/Analytics/Index.cshtml.cs`
- [ ] Add navigation links

#### 4.1.2 Set Up Analytics Service
- [ ] Create `Services/AnalyticsService.cs`
- [ ] Add dependency injection
- [ ] Implement data aggregation methods

#### 4.1.3 Add Chart.js Integration
- [ ] Include Chart.js in `_Layout.cshtml`
- [ ] Create chart initialization scripts

### Phase 1.2: Core Analytics Features (Days 3-5)

#### 4.2.1 Category Breakdown Chart
- [ ] Implement donut chart for category distribution
- [ ] Add "Top 10 + Other" logic
- [ ] Style with Bootstrap theme colors

#### 4.2.2 Date Range Picker
- [ ] Add date range controls
- [ ] Set 3-month default
- [ ] Implement AJAX data loading

#### 4.2.3 Monthly Trends Chart
- [ ] Create bar chart for monthly spending
- [ ] Show trend over selected date range
- [ ] Add hover tooltips

### Phase 1.3: UI Polish & Mobile (Days 6-7)

#### 4.3.1 Responsive Design
- [ ] Make charts mobile-friendly
- [ ] Adjust chart sizes for different screens
- [ ] Test on mobile devices

#### 4.3.2 Navigation Integration
- [ ] Add to main navigation menu
- [ ] Add link from expenses dashboard
- [ ] Update breadcrumbs

---

## 5. Database Queries

### 5.1 Category Breakdown Query
```sql
SELECT 
    COALESCE(ec.Name, 'Uncategorized') as CategoryName,
    SUM(CASE 
        WHEN re.Id IS NOT NULL THEN re.Amount * 
            CASE f.Frequency
                WHEN 0 THEN 1  -- Monthly
                WHEN 1 THEN 3  -- Quarterly  
                WHEN 2 THEN 6  -- Semi-Annual
                WHEN 3 THEN 12 -- Annual
            END
        WHEN ie.Id IS NOT NULL THEN ie.Amount
        ELSE 0
    END) as TotalAmount
FROM ExpenseCategories ec
LEFT JOIN RegularExpenses re ON ec.Id = re.CategoryId
LEFT JOIN IrregularExpenses ie ON ec.Id = ie.CategoryId
WHERE (re.StartDate <= @EndDate AND (re.EndDate IS NULL OR re.EndDate >= @StartDate))
   OR (ie.Date BETWEEN @StartDate AND @EndDate)
GROUP BY ec.Id, ec.Name
ORDER BY TotalAmount DESC
```

### 5.2 Monthly Trends Query
```sql
SELECT 
    DATE_TRUNC('month', COALESCE(ie.Date, re.StartDate)) as Month,
    SUM(COALESCE(ie.Amount, re.Amount)) as MonthlyTotal
FROM (
    SELECT Id, CategoryId, Amount, StartDate, NULL as Date, Frequency
    FROM RegularExpenses 
    WHERE StartDate <= @EndDate AND (re.EndDate IS NULL OR re.EndDate >= @StartDate)
    
    UNION ALL
    
    SELECT Id, CategoryId, Amount, NULL as StartDate, Date, NULL as Frequency
    FROM IrregularExpenses
    WHERE Date BETWEEN @StartDate AND @EndDate
) expenses
GROUP BY DATE_TRUNC('month', COALESCE(Date, StartDate))
ORDER BY Month
```

---

## 6. UI/UX Design

### 6.1 Page Layout

```
┌─────────────────────────────────────────────────┐
│ [FinApp]           [Expenses] [Analytics] [Portfolio] │
├─────────────────────────────────────────────────┤
│                                                 │
│ Expense Analytics Dashboard                     │
│                                                 │
│ ┌─────────────┐ ┌─────────────────────────────┐ │
│ │ Date Range  │ │    Category Breakdown       │ │
│ │ [3 months ▼] │ │        [Donut Chart]       │ │
│ └─────────────┘ └─────────────────────────────┘ │
│                                                 │
│ ┌─────────────────────────────────────────────┐ │
│ │        Monthly Spending Trends             │ │
│ │           [Bar Chart]                      │ │
│ └─────────────────────────────────────────────┘ │
│                                                 │
│ Summary Statistics:                             │
│ • Total Expenses: $2,450.00                     │
│ • Top Category: Groceries (28%)                 │
│ • Avg Monthly: $816.67                          │
└─────────────────────────────────────────────────┘
```

### 6.2 Chart Specifications

#### 6.2.1 Donut Chart (Category Breakdown)
- **Colors:** Bootstrap theme colors + custom palette
- **Animation:** Smooth transitions
- **Legend:** External legend with percentages
- **Center Text:** Total amount
- **Hover:** Category details and amounts

#### 6.2.2 Bar Chart (Monthly Trends)
- **X-axis:** Months (MMM YYYY format)
- **Y-axis:** Amount in currency
- **Colors:** Gradient from theme colors
- **Grid:** Light grid lines
- **Tooltips:** Month + exact amount

### 6.3 Mobile Responsiveness
- **Breakpoint:** md (768px)
- **Charts:** Stack vertically on mobile
- **Controls:** Full width date picker
- **Text:** Smaller fonts on mobile

---

## 7. Navigation Integration

### 7.1 Main Navigation
```razor
<!-- _Layout.cshtml -->
<li class="nav-item">
    <a class="nav-link" asp-page="/Analytics/Index">
        <i class="bi bi-bar-chart"></i> Analytics
    </a>
</li>
```

### 7.2 Expenses Dashboard Link
```razor
<!-- Pages/Expenses/Index.cshtml -->
<div class="card">
    <div class="card-body">
        <h5>Quick Actions</h5>
        <a href="/Analytics" class="btn btn-outline-primary">
            <i class="bi bi-bar-chart"></i> View Analytics
        </a>
    </div>
</div>
```

---

## 8. Testing Strategy

### 8.1 Unit Tests
```csharp
[Fact]
public async Task GetCategoryBreakdown_ReturnsCorrectData()
{
    // Test category aggregation
    // Test "Other" grouping
    // Test date filtering
}
```

### 8.2 Integration Tests
- Chart rendering with sample data
- Date range picker functionality
- Mobile responsiveness
- Navigation links

### 8.3 User Acceptance Tests
- Visual verification of charts
- Data accuracy validation
- Performance with 12 months data

---

## 9. Success Metrics

- **Functionality:** All charts load and display correctly
- **Performance:** Page loads in <3 seconds with 12 months data
- **Usability:** Intuitive navigation and date selection
- **Mobile:** Charts work on mobile devices
- **Accuracy:** Data matches expense records

---

## 10. Future Enhancements

### Phase 2 Possibilities
- Export to PDF/CSV
- Real-time updates
- Advanced filtering options
- Trend analysis with predictions
- Comparative analytics (year-over-year)

---

## 11. Implementation Timeline

```
Week 1: Infrastructure & Core Features
├── Day 1: Analytics page setup
├── Day 2: Service layer implementation  
├── Day 3: Category breakdown chart
├── Day 4: Date range picker
├── Day 5: Monthly trends chart
├── Day 6: Mobile responsiveness
└── Day 7: Navigation integration & testing
```

**Total Effort:** 1 week  
**Risk Level:** Low  
**Dependencies:** Chart.js, existing expense data models

---

## 12. Conclusion

This Phase 1 implementation provides immediate value with category breakdown visualization while establishing a foundation for future analytics features. The focused scope and use of existing technologies ensures quick implementation with minimal risk.

**Next Steps:**
1. Review and approve this design
2. Begin implementation of analytics service
3. Create analytics page structure
4. Implement charts and date filtering

---

**Document History:**
- v1.0 (2025-09-09): Initial design document created
- Review Date: [TBD]
- Approval Date: [TBD]