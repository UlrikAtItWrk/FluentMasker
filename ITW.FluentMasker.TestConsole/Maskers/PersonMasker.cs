using ITW.FluentMasker.MaskRules;
using ITW.FluentMasker.TestConsole.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITW.FluentMasker.TestConsole.Maskers
{
    public class PersonMasker : AbstractMasker<Person>
    {
        public void Initialize()
        {
            MaskFor(x => x.FirstName, (IMaskRule)new MaskFirstRule(2, "*"));
            MaskFor(x => x.FirstName, (IMaskRule)new MaskLastRule(1, "x"));
            MaskFor(x => x.LastName, (IMaskRule)new MaskLastRule(2, "*"));
        }
    }
}
