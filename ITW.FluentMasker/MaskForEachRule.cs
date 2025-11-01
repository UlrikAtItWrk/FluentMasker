using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITW.FluentMasker
{
    public class MaskForEachRule<TItem> : IMaskRule
    {
        public AbstractMasker<TItem> Masker { get; set; }

        public MaskForEachRule(AbstractMasker<TItem> masker)
        {
            Masker = masker;
        }

        // This Apply method will need special handling since it's not a string operation
        public string Apply(string value)
        {
            throw new NotImplementedException("MaskForEachRule should not be applied to string values.");
        }
    }
}
