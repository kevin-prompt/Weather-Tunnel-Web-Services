using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Configuration;
using WSHelpers;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Net;
using System.IO;
using System.Xml;
using System.Collections.Concurrent;
using ThisMethod = System.Reflection.MethodInfo;
using Microsoft.WindowsAzure.StorageClient;
using System.ServiceModel.Channels;

namespace Coolftc.WTClimate
{
    // Using the Namespace helps clean up the WSDL, AddressFilterMode lets you use http on Azure for the 
    // ws binding and ASP Compatibility gives you access to the HTTP headers.     
    [ServiceBehavior(Namespace = "http://coolftc.org/CoolftcWTSupport", AddressFilterMode = AddressFilterMode.Any)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class Support : ISupport
    {
        private static string SUPPORT_KEY = RoleEnvironment.GetConfigurationSettingValue("WTSkey");
        private static string ROLE_NAME = RoleEnvironment.CurrentRoleInstance.Role.Name + "(" + RoleEnvironment.CurrentRoleInstance.Id.ToString() + ")";
        private static string APPL_NAME = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + ".Support";

        public string Version()
        {
            try
            {
                // Get the version out of the AssemblyInfo.cs file.
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " running on " + ROLE_NAME;
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        /********************************************************************************************
         * Prepaq Support
         */
        public void NewDestination(string ticket, string prepaq, string name, string where)
        {
            try
            {
                string holdParms = ServiceLedger.Tag("prepaq", prepaq) + ServiceLedger.Tag("name", name) + ServiceLedger.Tag("where", where);
                CheckLogTicket(ticket);
                if (prepaq.Length == 0 || name.Length == 0) throw new ClassExp(ClassExp.EXP_CODES.EXP_NODATA, ThisMethod.GetCurrentMethod().Name, "prepaq = " + prepaq);
                WPrepaqHelper.Store(prepaq, name, where);
                ServiceLedger.Audit(new ClassAud(ticket, ROLE_NAME, getRemoteIP(), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, 0));
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        public void DelDestination(string ticket, string prepaq, string name)
        {
            try
            {
                string holdParms = ServiceLedger.Tag("prepaq", prepaq) + ServiceLedger.Tag("name", name);
                CheckLogTicket(ticket);
                WPrepaqHelper.Delete(prepaq, name);
                ServiceLedger.Audit(new ClassAud(ticket, ROLE_NAME, getRemoteIP(), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, 0));
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        /********************************************************************************************
         * Registration Support
         */
        public string GetRegInfo(string ticket, string key)
        {
            try
            {
                string holdParms = ServiceLedger.Tag("key", key);
                CheckLogTicket(ticket);
                string allkeys = "No Registration Found";
                WRegisterTbl keyinfo = WRegisterHelper.Read(key);
                if(keyinfo != null)
                {
                    allkeys = keyinfo.PartitionKey + "," + keyinfo.RowKey + "," + keyinfo.Device;
                }
                ServiceLedger.Audit(new ClassAud(ticket, ROLE_NAME, getRemoteIP(), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, 0));
                return allkeys;
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        public void ChgRegActive(string ticket, string key, bool act)
        {
            try
            {
                string holdParms = ServiceLedger.Tag("key", key) + ServiceLedger.Tag("act", act);
                CheckLogTicket(ticket);
                // Since the attribute is in the key, we need to read/delete/add
                WRegisterTbl keyinfo = WRegisterHelper.Read(key);
                if (keyinfo != null && !keyinfo.RowKey.Equals(act.ToString()))
                {
                    WRegisterHelper.Delete(keyinfo.PartitionKey, keyinfo.RowKey);
                    WRegisterHelper.Store(keyinfo.PartitionKey, keyinfo.Device, act);
                    Climate.FlushToken(key); 
                }
                ServiceLedger.Audit(new ClassAud(ticket, ROLE_NAME, getRemoteIP(), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, 0));
                return;
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        public RegistrationID[] GetRegByDate(string ticket, string start, string end)
        {
            try
            {
                string holdParms = ServiceLedger.Tag("start", start) + ServiceLedger.Tag("end", end);
                CheckLogTicket(ticket);
                // Incomming dates expected to be in ISO 8601 format: yyyy-MM-ddTHH:mm:ss.fffffffzzz (where zzz = +00:00)
                DateTime begin = DateTime.Parse(start);
                DateTime fin = DateTime.Parse(end);
                // Get ids
                IList<RegistrationID> regs = new List<RegistrationID>();
                CloudTableQuery<WRegisterTbl> items = WRegisterHelper.Read(begin, fin);
                foreach (WRegisterTbl item in items)
                {
                    RegistrationID holdid = new RegistrationID();
                    holdid.Id = item.PartitionKey;
                    holdid.Active = Convert.ToBoolean(item.RowKey);
                    holdid.Timestamp = item.Timestamp;
                    holdid.Device = item.Device;
                    regs.Add(holdid);
                }
                ServiceLedger.Audit(new ClassAud(ticket, ROLE_NAME, getRemoteIP(), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, 0));
                return regs.ToArray();
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        /********************************************************************************************
         * Logging and Audit Support
         */
        public LedgerLog[] GetLogList(string ticket)
        {
            try
            {
                string holdParms = "";
                CheckLogTicket(ticket);
                IList<LedgerLog> ledgerOut = new List<LedgerLog>();
                CloudTableQuery<LogLedger> items = ServiceLedger.Read();
                foreach (LogLedger item in items)
                {
                    LedgerLog holdLog = new LedgerLog();
                    holdLog.ledgerApplication = item.PartitionKey;
                    holdLog.ledgerID = item.RowKey;
                    holdLog.severity = item.Severity;
                    holdLog.message = item.Message;
                    holdLog.timestamp = item.Timestamp;
                    ledgerOut.Add(holdLog);
                }
                ServiceLedger.Audit(new ClassAud(ticket, ROLE_NAME, getRemoteIP(), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, 0));
                return ledgerOut.ToArray();
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "->" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        public LedgerLog[] GetLogFilter(string ticket, string start, string end)
        {
            try
            {
                string holdParms = ServiceLedger.Tag("start", start) + ServiceLedger.Tag("end", end);
                CheckLogTicket(ticket);
                // Incomming dates expected to be in ISO 8601 format: yyyy-MM-ddTHH:mm:ss.fffffffzzz (where zzz = +00:00)
                DateTime begin = DateTime.Parse(start);
                DateTime fin = DateTime.Parse(end);

                IList<LedgerLog> ledgerOut = new List<LedgerLog>();
                CloudTableQuery<LogLedger> items = ServiceLedger.Read(begin, fin);
                foreach (LogLedger item in items)
                {
                    LedgerLog holdLog = new LedgerLog();
                    holdLog.ledgerApplication = item.PartitionKey;
                    holdLog.ledgerID = item.RowKey;
                    holdLog.severity = item.Severity;
                    holdLog.message = item.Message;
                    holdLog.timestamp = item.Timestamp;
                    ledgerOut.Add(holdLog);
                }
                ServiceLedger.Audit(new ClassAud(ticket, ROLE_NAME, getRemoteIP(), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, 0));
                return ledgerOut.ToArray();
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "->" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        public void DelLogFilter(string ticket, string start, string end)
        {
            try
            {
                string holdParms = ServiceLedger.Tag("start", start) + ServiceLedger.Tag("end", end);
                CheckLogTicket(ticket);
                // Incomming dates expected to be in ISO 8601 format: yyyy-MM-ddTHH:mm:ss.fffffffzzz (where zzz = +00:00)
                DateTime begin = DateTime.Parse(start);
                DateTime fin = DateTime.Parse(end);

                ServiceLedger.Delete(begin, fin);
                ServiceLedger.Audit(new ClassAud(ticket, ROLE_NAME, getRemoteIP(), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, 0));
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "->" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        public void DelLogItem(string ticket, string key)
        {
            try
            {
                string holdParms = ServiceLedger.Tag("key", key);
                CheckLogTicket(ticket);
                ServiceLedger.Delete(key);
                ServiceLedger.Audit(new ClassAud(ticket, ROLE_NAME, getRemoteIP(), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, 0));
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "->" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        public SystemAudit[] GetAuditRange(string ticket, int first, int max, int cSort, bool cASC, string cUser, string cMachine, string cAppl, string cMethod, int cBill, string cStart, string cEnd)
        {
            try
            {
                AuditDisplay criteria = new AuditDisplay();
                criteria.application = cAppl; criteria.billing = cBill; criteria.decrypt = false; criteria.machine = cMachine;
                criteria.userID = cUser; criteria.method = cMethod; criteria.orderASC = cASC; criteria.sortCode = cSort;
                criteria.start = cStart != null ? DateTime.Parse(cStart) : DateTime.MinValue;
                criteria.end = cEnd != null ? DateTime.Parse(cEnd) : DateTime.MaxValue;
                string holdParms = ServiceLedger.Tag("first", first) + ServiceLedger.Tag("max", max) + ServiceLedger.Tag("criteria", criteria.TAG());
                CheckLogTicket(ticket);

                ServiceLedger.Audit(new ClassAud(ticket, ROLE_NAME, getRemoteIP(), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, 0));
                int total = 0;
                return WAuditDataHelper.AuditEntrys(WAuditDataHelper.GetAuditRange(criteria, first, max, out total), criteria.decrypt);
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "->" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        public int GetAuditCount(string ticket, int first, int max, int cSort, bool cASC, string cUser, string cMachine, string cAppl, string cMethod, int cBill, string cStart, string cEnd)
        {
            try
            {
                AuditDisplay criteria = new AuditDisplay();
                criteria.application = cAppl; criteria.billing = cBill; criteria.decrypt = false; criteria.machine = cMachine;
                criteria.userID = cUser; criteria.method = cMethod; criteria.orderASC = cASC; criteria.sortCode = cSort;
                criteria.start = cStart != null ? DateTime.Parse(cStart) : DateTime.MinValue;
                criteria.end = cEnd != null ? DateTime.Parse(cEnd) : DateTime.MaxValue;
                string holdParms = ServiceLedger.Tag("first", first) + ServiceLedger.Tag("max", max) + ServiceLedger.Tag("criteria", criteria.TAG());
                CheckLogTicket(ticket);

                ServiceLedger.Audit(new ClassAud(ticket, ROLE_NAME, getRemoteIP(), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, 0));
                int total = 0;
                return WAuditDataHelper.GetAuditRange(criteria, first, max, out total).Length;
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "->" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        public int DelAuditRange(string ticket, int first, int max, int cSort, bool cASC, string cUser, string cMachine, string cAppl, string cMethod, int cBill, string cStart, string cEnd)
        {
            try
            {
                AuditDisplay criteria = new AuditDisplay();
                criteria.application = cAppl; criteria.billing = cBill; criteria.decrypt = false; criteria.machine = cMachine;
                criteria.userID = cUser; criteria.method = cMethod; criteria.orderASC = cASC; criteria.sortCode = cSort;
                criteria.start = cStart != null ? DateTime.Parse(cStart) : DateTime.MinValue;
                criteria.end = cEnd != null ? DateTime.Parse(cEnd) : DateTime.MaxValue;
                string holdParms = ServiceLedger.Tag("first", first) + ServiceLedger.Tag("max", max) + ServiceLedger.Tag("criteria", criteria.TAG());
                CheckLogTicket(ticket);

                ServiceLedger.Audit(new ClassAud(ticket, ROLE_NAME, getRemoteIP(), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, 0));
                int total = 0;
                return WAuditDataHelper.AuditDeletes(WAuditDataHelper.GetAuditRange(criteria, first, max, out total));
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "->" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }



        /********************************************************************************************
         * Reporting Support
         */
        public XmlElement GetStats(string ticket)
        {
            try
            {
                string holdParms = "";
                CheckLogTicket(ticket);
                Instrumentation stats = new Instrumentation();
                ServiceLedger.Audit(new ClassAud(ticket, ROLE_NAME, getRemoteIP(), APPL_NAME, ThisMethod.GetCurrentMethod().Name, holdParms, 0));
                return stats.Build();
            }

            #region Web Service Exception Catch
            catch (ClassExp kerr)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
                throw new FaultException(kerr.codeNbr.ToString() + ":" + kerr.codeDesc() + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "->" + e.Source;
                ServiceLedger.Entry(17000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                throw new FaultException("17000:" + e.Message + ":(" + holdSrc + ")", FaultCode.CreateReceiverFaultCode(new FaultCode(ThisMethod.GetCurrentMethod().Name)));
            }
            #endregion
        }

        /********************************************************************************************
         * Private Helpers
         */
        private void CheckLogTicket(string ticket)
        {
            if (ticket.Equals(SUPPORT_KEY)) return;

            throw new ClassExp(ClassExp.EXP_CODES.EXP_NOMATCH, this.ToString());
        }

        private string getRemoteIP()
        {
            OperationContext context = OperationContext.Current;
            MessageProperties msgProp = context.IncomingMessageProperties;
            RemoteEndpointMessageProperty epProp = msgProp[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
            return epProp != null ? epProp.Address : "No IP";
        }

    }
}   
