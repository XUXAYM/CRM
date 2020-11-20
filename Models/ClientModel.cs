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
        private string _phone;
        public string Phone
        {
            get { return _phone; }
            set
            {
                if (_phone == value) return;
                _phone = value;
                OnPropetyChanged("Phone");
            }
        }
        private string _fax;
        public string Fax
        {
            get { return _fax; }
            set
            {
                if (_fax == value) return;
                _fax = value;
                OnPropetyChanged("Fax");
            }
        }
        private string _email;
        public string Email
        {
            get { return _email; }
            set
            {
                if (_email == value) return;
                _email = value;
                OnPropetyChanged("Email");
            }
        }
        private string _address;
        public string Address
        {
            get { return _address; }
            set
            {
                if (_address == value) return;
                _address = value;
                OnPropetyChanged("Address");
            }
        }
        private string _contactName;
        public string ContactName
        {
            get { return _contactName; }
            set
            {
                if (_contactName == value) return;
                _contactName = value;
                OnPropetyChanged("ContactName");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropetyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
