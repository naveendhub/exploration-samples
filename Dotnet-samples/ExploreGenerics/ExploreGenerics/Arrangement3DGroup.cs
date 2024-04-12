using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExploreGenerics {
    internal class Arrangement3DGroup<TArrangement> : Arrangement where TArrangement: Base3DArrangement{

        protected virtual void CreateLayouts()
        {
            Console.WriteLine("layout created");

        }

    }

    internal class MultimodalitySlabFusionArrangement : Arrangement3DGroup<FusionSlabArrangement>
    {


    }

    internal class MultimodalityFullVolumeFusionArrangement : Arrangement3DGroup<FusionFullVolumeArrangement> {


    }



}
