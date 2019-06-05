using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModManager
{
    public class ModIdentifier
    {
        public ModIdentifier() {}
        public ModIdentifier( string id, string name )
        {
            Id = id;
            Name = name;
        }

        public string Id { get; set; }
        public string Name { get; set; }
    }
}
