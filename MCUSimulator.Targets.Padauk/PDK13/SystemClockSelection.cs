using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCUSimulator.Targets.Padauk.PDK13
{
    public enum SystemClockSelection
    {
        T_0_IHRC_4 = 0,
        T_1_IHRC_16 = 0,

        T_0_IHRC_2 = 1,
        T_1_IHRC_8 = 1,

        T_1_ILRC_16 = 2,
        T_1_IHRC_32 = 3,
        T_1_IHRC_64 = 4,

        T_0_ILRC_4 = 6,
        T_0_ILRC = 7,
    }
}
