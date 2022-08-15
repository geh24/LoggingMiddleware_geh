# LoggingMiddleware_geh

## Description

July 2018 i started to write a .NET Core middleware, that should log API-requests and -response.
Optionally the full Body, all Headers and/or the Querystring can be logged.

The LoggingMiddleware project is based on NLog. For an example nlog.config see the Demo-project.

I use this project successfuly since three years, starting with .NET Core 1.1, right now the
master-branch is using .NET Core 6.0.


The name LoggingMiddleware was already used, so i appended my 3-char Initials meaning Gerhard Herre

## Demo

There is a, ASP.NET Core Web App Demo project called LoggingMiddleware_geh_Demo that shows the usage
of the Logging library. 
