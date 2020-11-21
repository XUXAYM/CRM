using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GUI_DB.Models
{
    class TaskModel:INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public ClientModel Client { get; set; }
        public WorkerModel Master { get; set; }
        public WorkerModel Slave { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.Now;
        public DateTime DoUntilDate { get; set; }
        public int Serial_Number { get; set; }
        private bool _isCompleted;
        public bool isCompleted { 
            get { return _isCompleted; }
            set
            {
                if (_isCompleted == value || _isCompleted == true) return;
                _isCompleted = value;
                OnPropetyChanged("IsDone");
            } 
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropetyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
