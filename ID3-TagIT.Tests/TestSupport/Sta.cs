using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace ID3_TagIT.Tests
{
  // xUnit runs tests on MTA thread-pool threads; WinForms needs an STA thread.
  internal static class Sta
  {
    public static void Run(Action test)
    {
      Exception failure = null;
      Thread thread = new Thread(delegate ()
      {
        try
        {
          test();
        }
        catch (Exception exception)
        {
          failure = exception;
        }
      });

      thread.SetApartmentState(ApartmentState.STA);
      thread.Start();
      thread.Join();

      if (failure != null)
        ExceptionDispatchInfo.Capture(failure).Throw();
    }
  }
}
