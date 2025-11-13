using System.Collections.Generic;

namespace InvestmentTracker.Models.ImportProfiles
{
    public class BankImportProfile
    {
        public string Id { get; set; } = string.Empty;
        public int Version { get; set; }
        public BankImportProfileMetadata Metadata { get; set; } = new();
        public BankImportProfileParser Parser { get; set; } = new();
        public List<BankImportProfileColumn> Columns { get; set; } = new();
        public BankImportProfileRules Rules { get; set; } = new();
        public List<string> Notes { get; set; } = new();
        public List<ExpenseFieldMapping> ExpenseFieldMappings { get; set; } = new();

        public BankImportProfileSummary ToSummary()
        {
            return new BankImportProfileSummary
            {
                Id = Id,
                DisplayName = Metadata.DisplayName,
                Description = Metadata.Description,
                SampleFile = Metadata.SampleFile,
                DefaultSkipRows = Parser.DefaultSkipRows,
                Encoding = Parser.Encoding,
                Delimiter = Parser.Delimiter,
                Quote = Parser.Quote,
                DecimalSeparator = Parser.DecimalSeparator,
                DateFormat = Parser.DateFormat,
                TrimEmptyPreamble = Parser.TrimEmptyPreamble
            };
        }
    }

    public class BankImportProfileSummary
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? SampleFile { get; set; }
        public int DefaultSkipRows { get; set; }
        public string Encoding { get; set; } = "utf-8";
        public string Delimiter { get; set; } = ";";
        public string Quote { get; set; } = "\"";
        public string DecimalSeparator { get; set; } = ".";
        public string DateFormat { get; set; } = "yyyy-MM-dd";
        public bool TrimEmptyPreamble { get; set; } = true;
    }

    public class BankImportProfileMetadata
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? SampleFile { get; set; }
    }

    public class BankImportProfileParser
    {
        public string Encoding { get; set; } = "utf-8";
        public string Delimiter { get; set; } = ";";
        public string Quote { get; set; } = "\"";
        public string DecimalSeparator { get; set; } = ".";
        public string DateFormat { get; set; } = "yyyy-MM-dd";
        public int DefaultSkipRows { get; set; }
        public bool TrimEmptyPreamble { get; set; } = true;
    }

    public class BankImportProfileColumn
    {
        public string Header { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string? Transform { get; set; }
        public bool Required { get; set; }
        public string? Notes { get; set; }
    }

    public class BankImportProfileRules
    {
        public List<BankImportProfileDropRowRule> DropRows { get; set; } = new();
    }

    public class BankImportProfileDropRowRule
    {
        public string? StartsWith { get; set; }
        public string? Contains { get; set; }
        public new string? Equals { get; set; }
    }

    public class ExpenseFieldMapping
    {
        public string Field { get; set; } = string.Empty;
        public string? SourceHeader { get; set; }
        public List<string> SourceHeaders { get; set; } = new();
        public string? Target { get; set; }
        public string? Fallback { get; set; }
        public string? Notes { get; set; }
    }
}
