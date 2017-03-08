using QuickFix;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Teb.DAL;
using Teb.FIX.Infra.Session;
using Teb.Infra.Collection;

namespace QuantConnect.Brokerages.TEB.FIX
{
    public class SessionDBSetting : SessionSetting
    {
        string applicationName;

        string senderCompId;

        string targetCompId;

        private QuickFix.Dictionary defaults_;

        private System.Collections.Generic.Dictionary<SessionID, QuickFix.Dictionary> settings_;
        private DbManager dbManager;
        public SessionDBSetting(StoreType appStoreType, DbManager dbManager, Teb.FIX.ServiceLib.IAppDataService service, IQueue<Teb.FIX.Infra.Session.DbLog> queue, string appName, string senderCompId, string targetCompId)
            : base(appStoreType, dbManager, service, queue)
        {
            this.applicationName = appName;
            this.senderCompId = senderCompId;
            this.targetCompId = targetCompId;
            this.dbManager = dbManager;
        }

        SessionSettings sessionsettings;

        public override QuickFix.SessionSettings GetSessionSettings()
        {
            defaults_ = new QuickFix.Dictionary();

            settings_ = new Dictionary<SessionID, Dictionary>();

            sessionsettings = new SessionSettings();

            SesionDataSetSettings settings = new SesionDataSetSettings(GetConfigs());

            //---- load the DEFAULT section
            LinkedList<QuickFix.Dictionary> section = settings.Get("DEFAULT");
            QuickFix.Dictionary def = new QuickFix.Dictionary();
            if (section.Count > 0)
                def = section.First.Value;
            Set(def);

            //---- load each SESSION section
            section = settings.Get("SESSION");
            foreach (QuickFix.Dictionary dict in section)
            {
                dict.Merge(def);

                string sessionQualifier = SessionID.NOT_SET;
                string senderSubID = SessionID.NOT_SET;
                string senderLocID = SessionID.NOT_SET;
                string targetSubID = SessionID.NOT_SET;
                string targetLocID = SessionID.NOT_SET;

                if (dict.Has(SessionSettings.SESSION_QUALIFIER))
                    sessionQualifier = dict.GetString(SessionSettings.SESSION_QUALIFIER);
                if (dict.Has(SessionSettings.SENDERSUBID))
                    senderSubID = dict.GetString(SessionSettings.SENDERSUBID);
                if (dict.Has(SessionSettings.SENDERLOCID))
                    senderLocID = dict.GetString(SessionSettings.SENDERLOCID);
                if (dict.Has(SessionSettings.TARGETSUBID))
                    targetSubID = dict.GetString(SessionSettings.TARGETSUBID);
                if (dict.Has(SessionSettings.TARGETLOCID))
                    targetLocID = dict.GetString(SessionSettings.TARGETLOCID);

                SessionID sessionID = new SessionID(dict.GetString(SessionSettings.BEGINSTRING), dict.GetString(SessionSettings.SENDERCOMPID), senderSubID, senderLocID, dict.GetString(SessionSettings.TARGETCOMPID), targetSubID, targetLocID, sessionQualifier);

                Set(sessionID, dict);

                sessionsettings.Set(sessionID, dict);


            }


            sessionsettings.Set(defaults_);
            return sessionsettings;
        }

        private DataSet GetConfigs()
        {
            try
            {

                Dictionary<string, object> pList = new Dictionary<string, object>();

                pList.Add("ApplicationName", applicationName);
                pList.Add("Kriter", 2);
                if (string.IsNullOrEmpty(senderCompId))
                {
                    pList.Add("SenderCompID", DBNull.Value);
                }
                else
                {
                    pList.Add("SenderCompID", senderCompId);
                }

                if (string.IsNullOrEmpty(targetCompId))
                {
                    pList.Add("TargetCompID", DBNull.Value);
                }
                else
                {
                    pList.Add("TargetCompID", targetCompId);
                }

                DataSet configs = dbManager.ToDataSet("sel_FIXAppConfigs", true, pList);

                return configs;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        private void Set(QuickFix.Dictionary defaults)
        {
            defaults_ = defaults;
            foreach (KeyValuePair<SessionID, QuickFix.Dictionary> entry in settings_)
                entry.Value.Merge(defaults_);
        }

        private void Set(SessionID sessionID, QuickFix.Dictionary settings)
        {
            if (Has(sessionID))
                throw new ConfigError("Duplicate Session " + sessionID.ToString());
            settings.SetString(SessionSettings.BEGINSTRING, sessionID.BeginString);
            settings.SetString(SessionSettings.SENDERCOMPID, sessionID.SenderCompID);
            if (SessionID.IsSet(sessionID.SenderSubID))
                settings.SetString(SessionSettings.SENDERSUBID, sessionID.SenderSubID);
            if (SessionID.IsSet(sessionID.SenderLocationID))
                settings.SetString(SessionSettings.SENDERLOCID, sessionID.SenderLocationID);
            settings.SetString(SessionSettings.TARGETCOMPID, sessionID.TargetCompID);
            if (SessionID.IsSet(sessionID.TargetSubID))
                settings.SetString(SessionSettings.TARGETSUBID, sessionID.TargetSubID);
            if (SessionID.IsSet(sessionID.TargetLocationID))
                settings.SetString(SessionSettings.TARGETLOCID, sessionID.TargetLocationID);
            settings.Merge(defaults_);
            Validate(settings);
            settings_[sessionID] = settings;


        }

        private void Validate(QuickFix.Dictionary dictionary)
        {
            string beginString = dictionary.GetString(SessionSettings.BEGINSTRING);
            if (beginString != Values.BeginString_FIX40 &&
                beginString != Values.BeginString_FIX41 &&
                beginString != Values.BeginString_FIX42 &&
                beginString != Values.BeginString_FIX43 &&
                beginString != Values.BeginString_FIX44 &&
                beginString != Values.BeginString_FIXT11)
            {
                throw new ConfigError(SessionSettings.BEGINSTRING + " (" + beginString + ") must be FIX.4.0 to FIX.4.4 or FIXT.1.1");
            }

            string connectionType = dictionary.GetString(SessionSettings.CONNECTION_TYPE);
            if (!"initiator".Equals(connectionType) && !"acceptor".Equals(connectionType))
            {
                throw new ConfigError(SessionSettings.CONNECTION_TYPE + " must be 'initiator' or 'acceptor'");
            }
        }

        private bool Has(SessionID sessionID)
        {
            return settings_.ContainsKey(sessionID);
        }

    }
}
