# Wellcome to ErpNet.Api.Client Project

A Dot Net client for ERP.net APIs

## Introduction
ErpNet.Api.Client allows you to build external applications for ERP.net platform. Currently there are two types of API - Table API and Domain API. Both APIs are build on top od [ODATA](http://odata.org) standard. The project consists of two libraries: ErpNet.Api.Client and ErpNet.Api.Client.DomainApi. 

Documentation: https://docs.erp.net/dev/

## ErpNet.Api.Client
This library provides a generic ODATA service and ODATA command for building API HTTP requests. The JSON result is paresed to IDictionary<string, object> containting the entity properties.

## ErpNet.Api.Client.DomainApi
This library provides typed entity objects for Domain API and type safe methods for building API requests. 
