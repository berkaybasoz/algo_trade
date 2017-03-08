using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.TEB.Data
{
    public class SecurityDistributeOption
    {
        private bool distributeSecurityDepth;
        private bool distributeLimitControl;
        private bool distributeSecurity = true;
        private bool distributeBistTime;
        private bool distributeCashStocks = true;

        public bool DistributeSecurityDepth
        {
            get { return distributeSecurityDepth; }
            set { distributeSecurityDepth = value; }
        }
        public bool DistributeLimitControl
        {
            get { return distributeLimitControl; }
            set { distributeLimitControl = value; }
        }
        public bool DistributeSecurity
        {
            get { return distributeSecurity; }
            set { distributeSecurity = value; }
        }
        public bool DistributeBistTime
        {
            get { return distributeBistTime; }
            set { distributeBistTime = value; }
        }
        public bool DistributeCashStocks
        {
            get { return distributeCashStocks; }
            set
            {
                distributeCashStocks = value;
            }
        }
    }
}
