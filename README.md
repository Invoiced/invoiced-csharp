# invoiced-csharp
Official Invoiced C# Library

[![Build Status](https://travis-ci.org/Invoiced/invoiced-csharp.svg?branch=master)](https://travis-ci.org/Invoiced/invoiced-csharp) [![NuGet](https://img.shields.io/nuget/v/Invoiced.svg)](https://www.nuget.org/packages/Invoiced/)

## Usage

First, you must instantiate a new client

```c#
using Invoiced;
 ...
var connection = new Connection("{YOUR_API_KEY}",Invoiced.Environment.sandbox);
```

Then, API calls can be made like this:
```c#
# retrieve invoice
var invoiceID = 1;
var invoice = connection.NewInvoice().retrieve(invoiceID);

# mark as paid
var transaction = connection.NewTransaction();
transaction.invoice = invoice.id;
transaction.amount = invoice.balance;
transaction.method = "check";
transaction.Create();
```

## Developing


The test suite can be ran with `dotnet test`
