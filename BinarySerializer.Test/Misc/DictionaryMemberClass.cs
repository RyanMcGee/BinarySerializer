﻿using System.Collections.Generic;
using System.IO.Ports;

namespace BinarySerializer.Test.Misc
{
    public class DictionaryMemberClass
    {
        public DictionaryMemberClass()
        {
            Field = new Dictionary<string, string>();
        }

        public Dictionary<string, string> Field { get; set; }
    }
}
