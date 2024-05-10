using System;
using System.Collections.Generic;
using System.Text;

namespace TradingAPI.IG.Streaming.Models
{
    public class AccountModel : PropertyChanged
    {
        private string _accountId;
        public string AccountId
        {
            get { return _accountId; }
            set
            {
                if (_accountId != value)
                {
                    _accountId = value;
                    RaisePropertyChanged("AccountId");
                }
            }
        }

        private string _accountType;
        public string AccountType
        {
            get { return _accountType; }
            set
            {
                if (_accountType != value)
                {
                    _accountType = value;
                    RaisePropertyChanged("AccountType");
                }
            }
        }

        private string _accountName;
        public string AccountName
        {
            get { return _accountName; }
            set
            {
                if (_accountName != value)
                {
                    _accountName = value;
                    RaisePropertyChanged("AccountName");
                }
            }
        }
        private string _clientId;
        public string ClientId
        {
            get { return _clientId; }
            set
            {
                if (_clientId != value)
                {
                    _clientId = value;
                    RaisePropertyChanged("ClientId");
                }
            }
        }

        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    RaisePropertyChanged("UserName");
                }
            }
        }

        private string _lsEndpoint;
        public string LsEndpoint
        {
            get { return _lsEndpoint; }
            set
            {
                if (_lsEndpoint != value)
                {
                    _lsEndpoint = value;
                    RaisePropertyChanged("LsEndpoint");
                }
            }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set
            {
                if (_password != value)
                {
                    _password = value;
                    RaisePropertyChanged("Password");
                }
            }
        }

        private string _apiKey;
        public string ApiKey
        {
            get { return _apiKey; }
            set
            {
                if (_apiKey != value)
                {
                    _apiKey = value;
                    RaisePropertyChanged("ApiKey");
                }
            }
        }


        private decimal? _profitLoss;
        public decimal? ProfitLoss
        {
            get { return _profitLoss; }
            set
            {
                if (_profitLoss != value)
                {
                    _profitLoss = value;
                    RaisePropertyChanged("ProfitLoss");
                }
            }
        }

        private decimal? _deposit;
        public decimal? Deposit
        {
            get { return _deposit; }
            set
            {
                if (_deposit != value)
                {
                    _deposit = value;
                    RaisePropertyChanged("Deposit");
                }
            }
        }

        private decimal? _usedMargin;
        public decimal? UsedMargin
        {
            get { return _usedMargin; }
            set
            {
                if (_usedMargin != value)
                {
                    _usedMargin = value;
                    RaisePropertyChanged("UsedMargin");
                }
            }
        }

        private decimal? _amountDue;
        public decimal? AmountDue
        {
            get { return _amountDue; }
            set
            {
                if (_amountDue != value)
                {
                    _amountDue = value;
                    RaisePropertyChanged("AmountDue");
                }
            }
        }

        private decimal? _availableCash;
        public decimal? AvailableCash
        {
            get { return _availableCash; }
            set
            {
                if (_availableCash != value)
                {
                    _availableCash = value;
                    RaisePropertyChanged("AvailableCash");
                }
            }
        }

        private decimal? _balance;
        public decimal? Balance
        {
            get { return _balance; }
            set
            {
                if (_balance != value)
                {
                    _balance = value;
                    RaisePropertyChanged("Balance");
                }
            }
        }

    }
}
