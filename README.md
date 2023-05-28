# LoggingMiddleware_geh

## Description

In July 2018 i started to write a .NET Core middleware, that should log API-Requests and -Responses.  
Additionally the timing of a Request/Response is measured und can be logged
in a metric-data format suitable for Splunk, Elasticsearch, SigNoz or similar tools.  
Optionally the full Body, all Headers and/or the Querystring can be logged.

The name LoggingMiddleware was already used, so i appended my 3-char Initials meaning Gerhard Herre

The LoggingMiddleware_geh project is based on NLog. For an example nlog.config see the Demo-Project.

I started this project using .NET Core 1.1, right now the master-branch is using .NET 7.0.  

I use this project successfully for several Swingdance-related Websites like  
[RockThatSwing-Festival](https://www.rockthatswing.com/)
or [Munich Balboa and Shag Weekend](https://mbsw.worldofswing.com/)


## Demo-Project

There is a .NET Core Web App project
[LoggingMiddleware_geh_Demo](https://github.com/geh24/LoggingMiddleware_geh_Demo)
that demonstrates the usage of the Logging library. 
