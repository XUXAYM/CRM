using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GUI_DB.Models
{
    class WorkerModel : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public int Salary { get; set; }
        public string Login { get; set; }
        public string Role { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public override string ToString()
        {
            return $"{Name} ({Position})";
        }
    }
}
