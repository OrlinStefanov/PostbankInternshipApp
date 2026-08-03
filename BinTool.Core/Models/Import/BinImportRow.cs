using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinTool.Core.Models.Import
{
    public class BinImportRow
    {
        public string? Prefix { get; set; }
        public string? CardScheme { get; set; }
        public string? ProductType { get; set; }
        public string? FundingType { get; set; }
        public string? CountryCode { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }
}
