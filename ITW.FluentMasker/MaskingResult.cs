using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITW.FluentMasker
{
    public class MaskingResult
    {
        public bool IsSuccess { get; set; }
        public List<string> Errors { get; set; }
        public string MaskedData { get; set; }
    }
}
