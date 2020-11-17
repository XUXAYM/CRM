using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GUI_DB.Models
{
    class AbstaractModel
    {
        int Id { get; set; }
        string Name { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
}
