using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Ovidiu.x64.ARDUTIL
{
    public enum Tipuri_Sant : byte
    {
        [Description("01 - Sant-Pereat-2_3-0.5")]SantPereat2_3Ad50cm,
        [Description("01 - Sant Pereat+dren-2_3-0.5")]SantPereatSiDren2_3Ad50cm,
        [Description("02 - Sant-Pereat-1_1-0.5")]SantPereat1_1Ad50cm,
        [Description("02 - Sant Pereat+dren-1_1-0.5")]SantPereatSiDren1_1Ad50cm,
        [Description("03 - Rigola Carosabila")]RigolaCarosabila,
        [Description("04 - Rigola Ranf")]RigolaRanforsata,
        [Description("04 - Rigola Ranf + dren")]RigolaRanforsataSiDren,
        [Description("05- Rigola Triungh - 1_3-0.3")]RigolaTriunghiulara1_3Ad30cm,
        [Description("05 - Rigola Triungh - 1_3-0.3 +dren")]RigolaTriunghiularaSiDren1_3Ad30cm,
        [Description("05 - Rigola Triungh - 2_3-0.3")]RigolaTriunghiulara2_3Ad30cm,
        [Description("05 - Rigola Triungh - 2_3-0.4")]RigolaTriunghiulara2_3Ad40cm,
        [Description("06 - Sant Pereat -2_3-0.4")]SantPereat2_3Ad40cm,
        [Description("06 - Sant Pereat+dren-2_3-0.4")]SantPereatSiDren2_3Ad40cm,
        [Description("07 - Sant Pereat -1_1-0.4")]SantPereat1_1Ad40cm,
        [Description("07 - Sant Pereat +dren -1_1-0.4")]SantPereatSiDren1_1Ad40cm,
        [Description("08 - Sant Pereat -1_1-0.3")]SantPereat1_1Ad30cm,
        [Description("08 - Sant Pereat+dren -1_1-0.3")]SantPereatSiDren1_1Ad30cm,
        [Description("09 - Sant Pereat -2_3-0.3")]SantPereat2_3Ad30cm,
        [Description("09 - Sant Pereat +dren -2_3-0.3")]SantPereatSiDren2_3Ad30cm,
        [Description("10 - Rigola tip scafa")]RigolaTipScafa,
        [Description("12 - Rigola de acostament")]RigolaDeAcostament,
        [Description("13 - Rigola tip U")]RigolaTipU,
        [Description("13 - Rigola tip U+dren")]RigolaTipUSiDren,
        [Description("15 - Sant-Pereat-1_1-1.0")]SantPereat1_1Ad100cm,
        [Description("15 - Sant-Pereat-1_1-1.5")]SantPereat1_1Ad150cm,
        [Description("15 - Sant-Pereat-1_1-2.0")]SantPereat1_1Ad200cm,
    }

    public enum Parte : short
    {
        Stanga = -1,
        Mijloc = 0,
        Dreapta = 1
    }
}
