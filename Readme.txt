The Weather Tunnel is a hobby project created between jobs a few years ago.  The aim of the project was to get up to speed on Android and provide (me) a simple, data focused weather app.  I actually still run this for myself and friends (it incurred some cost to run it for thousands of people).  

There are two parts.  This repo is the server component.  The other is the Android App.  The server component in this repository is an old style WFC .NET web service that is RESTful, returning xml formatted data.  It provides a smart wrapper for the Weather Underground API, making it usable for a real world client application.  The API is documented in the Weather Tunnel Spec.pdf, also included in this repository.

To minimize support requirements (there were ~13K installs with a 4.08 average rating for the year the App was on Google Play), I ran this server component on Azure Cloud Services.  While not practical for large enterprises, this PaaS model is quite handy for a one man show.  

To get this to run requires:

Azure SQL Server database with tables and stored procedures as found in the Database folder.  See the file DbSqlServer.cs and ServiceConfiguration.Cloud.cscfg for some additional connection requirements.

Azure Storage Account classic, for which access keys are placed in the ServiceConfiguration.Cloud.cscfg.

The exact version of memcached from NuGet should be used. EnyimMemcached v2.16 and WazMemcachedServer v1.0.  Memcached was popular at the time, although I have used Redis more often since then for other projects.

Create a Cloud Services classic instance on Azure and publish it there.  You will need to acquire some Weather Underground keys to access their API.
