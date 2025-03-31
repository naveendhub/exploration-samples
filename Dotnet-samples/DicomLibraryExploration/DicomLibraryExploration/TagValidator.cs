using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Philips.Platform.Dicom;

namespace DicomLibraryExploration {
    public class TagValidator: IDicomTagValidator {
        public bool IsPrivate(uint tag)
        {
            if ((tag & 0x0001ffff) == 0) {
                return false;
            }
            // 1. All group lenght tags are not considered as private tags
            // 2. Tag's Group should be an odd number(Odd tags are Private tags)
            // 3. Tag's Group should be greater than 8 (groups 0001,0005,0007 are reserved)
            // 4. Tag's Group 0x0003 is considered as valid.
            var maskedTag = tag & 0xffff0000;
            return ((tag & 0x00010000) != 0) && (maskedTag > 0x00080000 || maskedTag == 0x00030000);
        }
    }
}
