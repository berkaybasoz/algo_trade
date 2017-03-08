using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teb.Infra.Model;

namespace QuantConnect.Brokerages.TEB.Data
{
    public class MatriksMessageEventArgs : EventArgs
    {
        public string Message { get; set; }
    }

    public class SecurityEventArgs : MatriksMessageEventArgs
    {
        public Security Security { get; set; }

        public SecurityEventArgs()
        {

        }

        public SecurityEventArgs(Security security, string message)
        {
            this.Security = security;
            this.Message = message;
        }
    }

    public class SecurityDepthEventArgs : MatriksMessageEventArgs
    {
        public SecurityDepth SecurityDepth { get; set; }

        public SecurityDepthEventArgs()
        {

        }

        public SecurityDepthEventArgs(SecurityDepth securityDepth)
        {
            this.SecurityDepth = securityDepth;
        }

        public SecurityDepthEventArgs(SecurityDepth securityDepth, string message)
            : this(securityDepth)
        {
            this.Message = message;
        }
    }

    public class BistTimeEventArgs : MatriksMessageEventArgs
    {
        public BistTime BistTime { get; set; }

        public BistTimeEventArgs()
        {

        }

        public BistTimeEventArgs(BistTime bistTime)
        {
            this.BistTime = bistTime;
        }

        public BistTimeEventArgs(BistTime bistTime, string message)
            : this(bistTime)
        {
            this.Message = message;
        }
    }

    public class NewsEventArgs : MatriksMessageEventArgs
    {
        public News News { get; set; }

        public NewsEventArgs()
        {

        }

        public NewsEventArgs(News news)
        {
            this.News = news;
        }

        public NewsEventArgs(News news, string message)
            : this(news)
        {
            this.Message = message;
        }
    }

    public class FeedEventArgs : MatriksMessageEventArgs
    {
        public FeedEventArgs()
        {

        }

        public FeedEventArgs(string message)
        {
            this.Message = message;
        }
    }

    public abstract class AbstractSecurityDataProvider
    {

        public event Action<string> OnMaxTimeStampChanged;
        public event Action<Exception> OnException;
        public event Action<string> OnLog;
        public event Action<SecurityEventArgs> OnSecurityChanged;
        public event Action<BistTimeEventArgs> OnBistTimeChanged;
        public event Action<SecurityDepthEventArgs> OnSecurityDepthChanged;
        public event Action<NewsEventArgs> OnHandleNewsChanged;
        public event Action<FeedEventArgs> OnHandleUknownFeed;

        private SecurityCollection securityValues;
        private QuoteCollection quoteValues;


        public SecurityCollection SecurityValues
        {
            get
            {
                if (securityValues == null)
                    securityValues = new SecurityCollection();
                return securityValues;
            }
            set
            {
                securityValues = value;
            }
        }

        public QuoteCollection QuoteValues
        {
            get
            {
                if (quoteValues == null)
                    quoteValues = new QuoteCollection();
                return quoteValues;
            }
            set
            {
                quoteValues = value;
            }
        }

        protected void MaxTimeStampChange(string time)
        {
            if (OnMaxTimeStampChanged != null)
                OnMaxTimeStampChanged(time);
        }

        protected void HandleLog(string log)
        {
            if (OnLog != null)
            {
                OnLog(log);
            }
        }

        protected void HandleException(Exception ex)
        {
            if (OnException != null)
            {
                OnException(ex);
            }
        }

        protected void HandleSecurityChanged(SecurityEventArgs sec)
        {
            if (OnSecurityChanged != null)
            {
                OnSecurityChanged(sec);
            }
        }

        protected void HandleSecurityDepthChanged(SecurityDepthEventArgs secDepth)
        {
            if (OnSecurityDepthChanged != null)
            {
                OnSecurityDepthChanged(secDepth);
            }
        }

        protected void HandleBistTimeChanged(BistTimeEventArgs bistTime)
        {
            if (OnBistTimeChanged != null)
            {
                OnBistTimeChanged(bistTime);
            }
        }
        protected void HandleNewsChanged(NewsEventArgs news)
        {
            if (OnHandleNewsChanged != null)
            {
                OnHandleNewsChanged(news);
            }
        }
        protected void HandleUknownFeed(FeedEventArgs feed)
        {
            if (OnHandleUknownFeed != null)
            {
                OnHandleUknownFeed(feed);
            }
        }
        public abstract void Start();

        public abstract void Stop();



    }
}
