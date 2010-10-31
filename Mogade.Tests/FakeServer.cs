using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Mogade.Tests
{
   /// <summary>
   /// A fake server for testing
   /// </summary>
   /// <remarks>
   /// Kinda hate this...bound to setup a real/fake testing server somewhere at some point
   /// </remarks>
   public class FakeServer : IDisposable
   {
      private bool _disposed;
      private readonly HttpListener _listener;
      private readonly Thread _thread;
      public const int Port = 9948;
      private readonly IList<ApiExpectation> _expectations;

      public FakeServer()
      {
         _expectations = new List<ApiExpectation>(5);
         _listener = new HttpListener();
         _listener.Prefixes.Add("http://*:" + 9948 + "/");
         _listener.Start();
         _thread = new Thread(Listen);
         _thread.Start();
      }

      public void Stub(ApiExpectation expectation)
      {
         _expectations.Add(expectation);         
      }

      private void Listen()
      {
         while (true)
         {
            var context = _listener.GetContext();
            var body = ExtractBody(context.Request);
            var expectation = FindExpectation(context, body);
            var response = context.Response;
            response.StatusCode = expectation.Status ?? 200;
            response.ContentLength64 = (expectation.Response ?? body).Length;
            using (var sw = new StreamWriter(response.OutputStream))
            {
               sw.Write(expectation.Response ?? body);               
            }            
            response.Close();
         }
      }

      private ApiExpectation FindExpectation(HttpListenerContext context, string body)
      {
         var request = context.Request;         
         foreach (var expectation in _expectations)
         {
            if (expectation.Method != null && string.Compare(request.HttpMethod, expectation.Method, true) != 0) {  continue;  }
            if (expectation.Url != null && string.Compare(request.Url.ToString(), expectation.Url, true) != 0) { continue; }
            if (expectation.Request != null && string.Compare(body, expectation.Request, true) != 0) { continue; }            
            return expectation; //we found a match!
         }
         throw new Exception(string.Format("Unexpected call: {0} {1}{2}{3}", request.HttpMethod, request.Url, Environment.NewLine, body));
      }
      private static string ExtractBody(HttpListenerRequest request)
      {
         var buffer = new byte[request.ContentLength64];
         request.InputStream.Read(buffer, 0, buffer.Length);
         return Encoding.Default.GetString(buffer);
      }


      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }
      private void Dispose(bool disposing)
      {
         if (!_disposed && disposing)
         {
            _thread.Abort();
            _listener.Stop();
         }
         _disposed = true;       
      }
   }

   /// <remarks
   /// All of these default to safe values, the most interesting of which is a null response will act as an echo server (return the request)
   /// </remarks>
   public class ApiExpectation
   {
      public string Method { get; set; }
      public string Url { get; set; }
      public string Request { get; set; }
      public int? Status { get; private set; }
      public string Response { get; private set; }
   }
   public enum HttpMethod
   {
      POST = 1,
      PUT = 2,
   }
}