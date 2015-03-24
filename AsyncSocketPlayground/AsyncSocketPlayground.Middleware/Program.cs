namespace AsyncSocketPlayground.Middleware
{
    using System;

    public class Program
    {
        public static int Main(string[] args)
        {
            AsynchronousMiddleWareServer.StartListening();
            return 0;
        }
    }


}