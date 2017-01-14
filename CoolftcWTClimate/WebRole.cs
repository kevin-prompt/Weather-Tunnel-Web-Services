using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Collections.Concurrent;
using System.Configuration;
using System.Threading;
using System.Diagnostics;
using Enyim.Caching;
using Microsoft.WindowsAzure.StorageClient;
using WSHelpers;
using ThisMethod = System.Reflection.MethodInfo;
using System.Net;

namespace Coolftc.WTClimate
{
    public class WebRole : RoleEntryPoint
    {
        // Global data items.
        string datefmtISO8601;
        private string ROLE_NAME;
        private string APPL_NAME;
        private double MAX_A_MINUTE;
        private double MAX_A_DAY;
        private int CACHE_SIZE;
        private string CACHE_ALIAS;
        private double TILL_NEW;
        private double TILL_STALE; 
        private double TILL_DEATH;
        string ZERO = "0";
        string ONE = "1";
        private enum ST_CACHE { ST_NOTFOUND, ST_READY, ST_STALE, ST_DEAD }

        /**
         * A separate process is created to run the cache, currently implemented using Memcached.  Each role instance
         * will spin up a command window to run a copy of Memcached and they will act as one large memory cache. To 
         * install Memcached for any solution, use NuGet Package Manager in VS.  The three packages required are:
         * 
         * PM> Install-Package WazMemcachedServer
         * PM> Install-Package EnyimMemcached
         * PM> Install-Package WazMemcachedClient
         * 
         * At some point in the future Azure will offer a similar service and we can switch to it.*/
        private Process cacheServer;        
        private MemcachedClient cacheClient;    

        public override void Run()
        {
            /*
             * In this loop any stale data found in the cache, and placed in the queue, is updated withing the limits
             * specified from the configuration.  The limits are n updates per minute and x updates per 24 hours.
             */
            bool rtn = false;
            bool skipqueue = false;
            string CACHE_FirstIn = "memFirstIn";

            cacheClient = WindowsAzureMemcachedHelpers.CreateDefaultClient(RoleEnvironment.CurrentRoleInstance.Role.Name, CACHE_ALIAS);

            // Check if there is a stored Cache and load it. Lets only call this once per instance, so check if someone has been doing this lately.
            if (GetCacheData(CACHE_FirstIn).Equals(ZERO))
            {
                cacheClient.Store(Enyim.Caching.Memcached.StoreMode.Set, CACHE_FirstIn, ONE);
                LoadPersistentCache();
                cacheClient.Store(Enyim.Caching.Memcached.StoreMode.Set, CACHE_FirstIn, ZERO);
            }

            // Record Instance Loop Start time
            cacheClient.Store(Enyim.Caching.Memcached.StoreMode.Set, RoleEnvironment.CurrentRoleInstance.Role.Name, DateTime.Now.ToString(datefmtISO8601));
            ServiceLedger.Entry(18000, ServiceLedger.SEV_CODES.SEV_INFO, "Starting Role at " + DateTime.Now.ToString(datefmtISO8601), RoleEnvironment.CurrentRoleInstance.Role.Name);

            while (true)
            {
                // If our per minute count outside of this queue is high, wait for another minute.  
                skipqueue = (WCountDataHelper.Total(ServiceLedger.CNT_CODES.CNT_WUAPI_CALLS, DateTimeOffset.Now.AddSeconds(-60), DateTimeOffset.Now) > (MAX_A_MINUTE / 2));

                // Process as much of the queue as possible.
                if (!skipqueue)
                {
                    for (ulong i = 0; i < MAX_A_MINUTE; ++i)
                    {
                        CloudQueueMessage stationName = StaleQueueHelper.StaleQueue.GetMessage();
                        if (stationName != null)
                        {
                            string holdStation = stationName.AsString;
                            StaleQueueHelper.StaleQueue.DeleteMessage(stationName);
                            ST_CACHE status = GetCacheStationStatus(holdStation);
                            WSHelpers.ServiceLedger.Count(ServiceLedger.CNT_CODES.CNT_WT_QUEUE); 

                            if (status != ST_CACHE.ST_READY)    // avoid duplicates by checking if cache already updated
                            {
                                WStation upd = new WStation(TILL_STALE, TILL_DEATH, TILL_NEW);
                                if (upd.Load(holdStation, MAX_A_DAY))
                                {
                                    rtn = cacheClient.Store(Enyim.Caching.Memcached.StoreMode.Set, holdStation, upd.RawStation());
                                    // This PUT will only work on the real platform, not on the Emulator. 
                                    try {
                                        WStationHelper.Put(holdStation, upd.stationData.OuterXml, upd.timestamp);
                                        ServiceLedger.Audit(new ClassAud("AsyncUpdater", ROLE_NAME, "", APPL_NAME, ThisMethod.GetCurrentMethod().Name, ServiceLedger.Tag("station", holdStation), 4));
                                    }
                                    catch (ClassExp kerr)
                                    { // Not the end of the world if it fails, so lets NOT exit loop and reboot.
                                        string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                                        //if (!kerr.code == ClassExp.EXP_CODES.EXP_TS_SIZE)
                                        ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
                                    }
                                    catch (Exception ex)
                                    { // Not the end of the world if it fails, so lets NOT exit loop and reboot.
                                        string holdSrc = "WTClimate.Run.WStationHelper.Store(Station=" + holdStation + ") --" + ex.Source;
                                        ServiceLedger.Entry(22010, ServiceLedger.SEV_CODES.SEV_EXCEPTION, ex.Message, holdSrc);
                                    }
                                }
                            }
                        } else { break; } // Nothing to process, go back to waiting
                    }
                }
                Thread.Sleep(80000); // Wait over a minute (actual time less because 2 boxes). If you change this, review this whole loop.
                if (cacheServer.HasExited)
                {
                    ServiceLedger.Entry(22000, ServiceLedger.SEV_CODES.SEV_ALERT, "Cache Server as Exited", "WebRole.Run()");
                    break; // If the cache dies, restart the instance.
                }
            }
        }

        public override bool OnStart()
        {
            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
            RoleEnvironment.Changing += RoleEnvironmentChanging;

            // Load initialization info. Note that this code exists outside of IIS, so the web.config is not
            // accessible.  Instead an all settings are saved in the ServiceConfiguration.
            ROLE_NAME = RoleEnvironment.CurrentRoleInstance.Role.Name + "(" + RoleEnvironment.CurrentRoleInstance.Id.ToString() + ")";
            APPL_NAME = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + ".RoleProcessor";
            try { MAX_A_MINUTE = Convert.ToUInt64(RoleEnvironment.GetConfigurationSettingValue("MaxMinute")); }
            catch { MAX_A_MINUTE = 10; }
            try { MAX_A_DAY = Convert.ToUInt64(RoleEnvironment.GetConfigurationSettingValue("MaxDay")); }
            catch { MAX_A_DAY = 500; }
            try { TILL_NEW = Convert.ToDouble(RoleEnvironment.GetConfigurationSettingValue("Forcetime")); }
            catch { TILL_NEW = 300.0; }
            try { TILL_STALE = Convert.ToDouble(RoleEnvironment.GetConfigurationSettingValue("Lifetime")); }
            catch { TILL_STALE = 30.0; }
            try { TILL_DEATH = Convert.ToDouble(RoleEnvironment.GetConfigurationSettingValue("Deathtime")); }
            catch { TILL_DEATH = 360.0; }
            try { CACHE_SIZE = Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("CacheSize")); }
            catch { CACHE_SIZE = 768; }
            try { CACHE_ALIAS = RoleEnvironment.GetConfigurationSettingValue("CacheEndPoint"); }
            catch { CACHE_ALIAS = "Memcached"; }
            datefmtISO8601 = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";

            // Configure socket management for calling other Web Services.
            // The ServicePointManager configures globally for all web service calls. If some web services
            // have throughput limits this might saturate the system.  In that case each address should be
            // given a connection limit.  See <connectionManagement> for how to do configure by address.
            // http://msdn.microsoft.com/en-us/library/fb6y0fyc(v=vs.100).aspx
            ServicePointManager.DefaultConnectionLimit = 512;
            ServicePointManager.MaxServicePointIdleTime = 3000;

            // Shared Cache
            cacheServer = WindowsAzureMemcachedHelpers.StartMemcached(CACHE_ALIAS, CACHE_SIZE);

            return base.OnStart();
        }

        public override void OnStop()
        {
            base.OnStop();
        }

        private void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            // If a configuration setting is changing
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
            {
                // Set e.Cancel to true to restart this role instance
                e.Cancel = true;
            }
        }

        /// <summary>
        /// As the queue is processed and the cached updated, the database is updated too.  If the cache must be 
        /// repopulated due to an instance reboot, the database can be directly loaded into the cache if that data 
        /// was lost.  The "Add" will only insert new items, so it will safely populate the cache. The ReadByDate() 
        /// will only return the unDead.
        /// </summary>
        private void LoadPersistentCache()
        {
            try
            {
                /* The Add 1 Day is helpful for testing locally, as the datetime of of the code will be different
                 * than the datetime used to store database items.  In data center, they all use the same date. */
                int cnt = 0;
                DateTime endRange = DateTime.Now.AddDays(1);
                DateTime beginRange = DateTime.Now.AddMinutes(TILL_DEATH * -1);
                CloudTableQuery<WStationTbl> cacheBacking = WStationHelper.Read(beginRange, endRange);

                foreach (WStationTbl local in cacheBacking)
                {
                    WStation holdStation = new WStation(TILL_STALE, TILL_DEATH, TILL_NEW, local.Report, local.Update);
                    bool rtn = cacheClient.Store(Enyim.Caching.Memcached.StoreMode.Add, local.RowKey, holdStation.RawStation());
                    WSHelpers.ServiceLedger.Count(ServiceLedger.CNT_CODES.CNT_CACHE_LOAD);
                    cnt++;
                }
                ServiceLedger.Audit(new ClassAud("AsyncUpdater", ROLE_NAME, "", APPL_NAME, ThisMethod.GetCurrentMethod().Name, ServiceLedger.Tag("stationsloaded", cnt), 0));
                ServiceLedger.Entry(18000, ServiceLedger.SEV_CODES.SEV_INFO, "Persistent Station Cache Loaded.  Total records: " + cnt.ToString(), ThisMethod.GetCurrentMethod().Name);

                // Clear out any records older than the beginning time.
                DateTime earliest = new DateTime(2012, 6, 1);
                WStationHelper.Delete(earliest, beginRange);
            }
            // If this throws, let skip it and hope for the best.
            catch (ClassExp kerr)
            { 
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + kerr.codeSource;
                ServiceLedger.Entry(kerr.codeNbr, ServiceLedger.SEV_CODES.SEV_EXCEPTION, kerr.codeDesc(), holdSrc);
            }
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + e.Source;
                ServiceLedger.Entry(22000, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
            }
        }

        /*
         * Generic method for getting data from cache.
         */
        private string GetCacheData(string key)
        {
            try
            {
                object holdCnt;
                if (cacheClient.TryGet(key, out holdCnt))
                    return holdCnt.ToString();
                else
                    return ZERO;
            }
            catch { return ZERO; }
        }

        /**
         * This will check the Cache for the current status of the station. The Azure Storage Queue does
         * not support a "no-duplicates" feature, so this is a way of checking if maybe a prior message
         * already took care of this one.
         */
        private ST_CACHE GetCacheStationStatus(string station)
        {
            try
            {
                object holdStationCACHE;
                if (cacheClient.TryGet(station, out holdStationCACHE))
                {
                    WStation holdStation = new WStation(TILL_STALE, TILL_DEATH, TILL_NEW, (string)holdStationCACHE);
                    if (holdStation.isDead()) { return ST_CACHE.ST_DEAD; }

                    if (holdStation.isStale()) { return ST_CACHE.ST_STALE; }

                    return ST_CACHE.ST_READY;
                }
                else
                {
                    return ST_CACHE.ST_NOTFOUND;
                }
            }
            // If there is a problem, lets just get another record
            catch (Exception e)
            {
                string holdSrc = RoleEnvironment.GetConfigurationSettingValue("ApplicationName") + "." + ThisMethod.GetCurrentMethod().Name + "--" + e.Source;
                ServiceLedger.Entry(22030, ServiceLedger.SEV_CODES.SEV_EXCEPTION, e.Message, holdSrc);
                return ST_CACHE.ST_NOTFOUND;
            }
 
        }

    }
}
