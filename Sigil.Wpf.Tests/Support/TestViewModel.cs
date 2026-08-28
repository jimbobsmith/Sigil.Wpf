using System.Collections.Generic;
using System.ComponentModel;
using Sigil.Wpf.Engine;

namespace Sigil.Wpf.Tests.Support
{
    public class TestViewModel : SigilViewModel
    {
        private string? _name;
        private string? _firstName;
        private Address? _address;

        public List<string> Notifications { get; } = new List<string>();

        public string? Name
        {
            get { return _name; }
            set
            {
                _name = value;
                RaisePropertyChanged(nameof(Name));
            }
        }

        public string? FirstName
        {
            get { return _firstName; }
            set
            {
                _firstName = value;
                RaisePropertyChanged(nameof(FirstName));
            }
        }

        [StateChangeExempt]
        public string? ExemptName { get; set; }

        public Address? Address
        {
            get { return _address; }
            set
            {
                _address = value;
                RaisePropertyChanged(nameof(Address));
            }
        }

        public override void RaisePropertyChanged(string propertyName)
        {
            Notifications.Add(propertyName);
            base.RaisePropertyChanged(propertyName);
        }
    }

    public class Address : INotifyPropertyChanged
    {
        private string? _city;

        [StateChangeExempt]
        public string? City
        {
            get { return _city; }
            set
            {
                _city = value;
                var handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs(nameof(City)));
            }
        }

        public string? Street { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
