# xNetStandard

[![NuGet version](https://badge.fury.io/nu/xNetStandard.svg)](https://badge.fury.io/nu/xNetStandard)

This is version of https://github.com/X-rus/xNet for .NET Standard 2.0. Unlike xNet it can be used in .net Core.

There was some bugs with GZipStream before .net Core 2.1. 
If you having issues with content decoding you can: 
1) Disable it using "EnableEncodingContent" in HttpRequest (set it to false)
2) Find Init() method where EnableEncodingContent is set to "true" by default 
