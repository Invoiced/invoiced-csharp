
namespace Invoiced
{
    using System;
    using System.Net;

    public class ApiException : InvoicedException {       
        
         private const long serialVersionUID = 1L;

         public ApiException(string message) : base(message) {
    
         }

    }

}