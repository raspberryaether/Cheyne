using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyncIMLocalHistory.Concept
{
    public class Individual
    {
        public Individual(
            uint identifier,
            string name
            )
        {
            Identifier = identifier;
            Name = name;
        }

        public uint Identifier { get; private set; }
        public string Name { get; private set; }       
    }
}
