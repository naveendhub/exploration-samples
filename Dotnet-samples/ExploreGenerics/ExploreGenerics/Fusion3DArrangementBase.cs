using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExploreGenerics {

    internal abstract class Arrangement {

    }

    internal class Base3DArrangement :Arrangement
    {
        protected virtual void CreateArrangement() {
            Console.WriteLine("Arrangement created");
        }
    }
    internal class Fusion3DArrangementBase: Base3DArrangement {
        
    }

    internal class FusionSlabArrangement : Fusion3DArrangementBase
    {
        private int a;
        public FusionSlabArrangement(int s)
        {
            a = s;
        }
        protected virtual void InitializeLinks() {
            Console.WriteLine("Links initialized");
        }

        protected virtual void CreateImages()
        {
            Console.WriteLine("Create images");
        }
    }

    internal class FusionFullVolumeArrangement : Fusion3DArrangementBase
    {

    }



    internal class ArrangementTestHelper<TArrangement> where TArrangement : Arrangement {

        internal void TestMethod()
        {

        }
    }


}
