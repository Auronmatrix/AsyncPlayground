namespace AsyncSocketPlayground.Middleware
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    public class AsynchronousMiddleWareServer
    {
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public AsynchronousMiddleWareServer()
        {
        }

        public static void StartListening()
        {
           
            var bytes = new byte[1024];
            var ipHostInfo = Dns.Resolve(Dns.GetHostName());
            var ipAddress = ipHostInfo.AddressList[0];
            var localEndPoint = new IPEndPoint(ipAddress, 11000);
            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(new AsyncCallback(AcceptCallbackServer), listener);
                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }

        public static void AcceptCallbackServer(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();

            // Get the socket that handles the client request.
            var listener = (Socket)ar.AsyncState;
            var handler = listener.EndAccept(ar);

            // Create the state object.
            var state = new StateServerObject { workSocket = handler };
            handler.BeginReceive(
                state.buffer,
                0,
                StateServerObject.BufferSize,
                0,
                new AsyncCallback(ReadCallbackServer),
                state);
        }

        public static void ReadCallbackServer(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateServerObject state = (StateServerObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read 
                // more data.
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the 
                    // client. Display it on the console.
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}", content.Length, content);
                    
                    // Echo the data back to the client.
                    var forwardClient = new AsynchronousClient();
                    content +=
                        ". This message was received from the client at the middleware and spied on. It has been passed on to the server.";
                    var response = forwardClient.ConnectAndGetResponse(27005, content);
                    SendServer(handler, response);
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(
                        state.buffer,
                        0,
                        StateServerObject.BufferSize,
                        0,
                        new AsyncCallback(ReadCallbackServer),
                        state);
                }
            }
        }

        private static void SendServer(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallbackServer), handler);
        }

        private static void SendCallbackServer(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}