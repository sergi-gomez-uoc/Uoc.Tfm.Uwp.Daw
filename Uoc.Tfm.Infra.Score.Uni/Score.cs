﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uoc.Tfm.Infra.Score.Uni
{
    [Serializable]
    public class Score
    {
        public int Row { get; set; }

        public int Column { get; set; }

        public int Length { get; set; }
    }
}
