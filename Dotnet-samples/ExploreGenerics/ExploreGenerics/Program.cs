using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExploreGenerics {
    internal class Program {
        internal static void Main(string[] args)
        {

            var a = new MultimodalityFullVolumeFusionArrangement();
            
            var helper = new ArrangementTestHelper<MultimodalityFullVolumeFusionArrangement>();
        }

        internal static void TestFullDeposit(ArrangementTestHelper<MultimodalityFullVolumeFusionArrangement> testHelper)  {
            testHelper.TestMethod();
        }
    }
}
