using ITW.FluentMasker.MaskRules;
using ITW.FluentMasker.TestConsole.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITW.FluentMasker.TestConsole.Maskers
{
    public class PetMasker : AbstractMasker<Pet>
    {
        public void Initialize()
        {
            MaskFor(x => x.Name, (IMaskRule)new MaskFirstRule(1, "="));
        }
    }
}
