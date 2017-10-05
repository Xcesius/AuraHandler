﻿using System.Collections.Generic;

namespace AuraHandler.FlaskComponents
{
    class FlaskInformation
    {
        public Dictionary<string, FlaskActions> UniqueFlaskNames { get; set; }
        public Dictionary<string, FlaskActions> FlaskTypes { get; set; }
        public Dictionary<string, FlaskActions> FlaskMods { get; set; }
    }
}
