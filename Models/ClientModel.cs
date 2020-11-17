using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GUI_DB.Models
{
    class ClientModel : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string ContactName { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public override string ToString()
        {
            return Name;
        }
    }
}
