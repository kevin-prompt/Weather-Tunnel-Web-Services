The Weather Tunnel is a hobby project created between jobs a few years ago.  The aim of the project was to get up to speed on Android and provide (me) a simple, data focused weather app.  I actually still run this for myself and friends (it incurred some cost to run it for thousands of people).  

There are two parts.  This repo is the server component.  The other is the Android App.  The server component in this repository is an old style WFC .NET web service that is RESTful, returning xml formatted data.  It provides a smart wrapper for the Weather Underground API, making it usable for a real world client application.  The API is documented in the Weather Tunnel Spec.pdf, also included in this repository.

To minimize support requirements (there were ~13K installs with a 4.08 average rating for the year the App was on Google Play), I ran this server component on Azure Cloud Services.  While not practical for large enterprises, this PaaS model is quite handy for a one man show.  

To get this to run requires:

1) Create an Azure SQL Server database instance and database server.  Once created, use SSMS to run the scripts to generate tables and stored procedures as found in the Database folder.  See the file DbSqlServer.cs and ServiceConfiguration.Cloud.cscfg for places to put connection string information specific to the install.

2) Create an Azure Storage Account classic.  This is used as a non-relational database to keep some logs and other other information.  The connection string for the account created goes into the ServiceConfiguration.Cloud.cscfg.

3) Create a Cloud Services classic instance.  This is where the application is published.  As part of the publishing process, it will also want the storage account.

4) You will need to acquire some Weather Underground keys to access their API and place those in the ServiceConfiguration.Cloud.cscfg.

NOTE: The exact version of memcached server and client (from NuGet) should be used. EnyimMemcached v2.16 and WazMemcachedServer v1.0.  Memcached was popular at the time, although I have used Redis more often since then for other projects.

