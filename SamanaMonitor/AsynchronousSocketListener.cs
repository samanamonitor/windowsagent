using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;
using System.Security.Cryptography.X509Certificates;

using SamanaMonitor;

// State object for reading client data asynchronously  
public class StateObject
{
    // Client  socket.  
    public Socket workSocket = null;
    // ssl stream
    public SslStream sslstream = null;
    // Size of receive buffer.  
    public const int BufferSize = 1024;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];
    // Received data string.  
    public StringBuilder sb = new StringBuilder();
}

public class AsynchronousSocketListener
{
    // Thread signal.  
    public static ManualResetEvent allDone = new ManualResetEvent(false);
    private Logging log;
    private ServerData sd;
    private HTTPRequest req;
    private X509Certificate2 serverCertificate = null;
    private int tcpport;
    private bool running;
    private int debug;


    public AsynchronousSocketListener(ServerData s, int t, int d)
    {
        debug = d;
        running = false;
        log = new Logging("Samana Service Listener", d);
        tcpport = t;
        sd = s;
        req = null;
        GetCertificateFromStore("SamanaGroup");
        if(serverCertificate == null)
        {
            throw new ArgumentException("Cannot find Certificate", "SamanaCertificate");
        }

        running = true;

        log.debug(1, "AsynchronousSocketListener: " + serverCertificate.Issuer, 100);
    }

    public void Stop()
    {
        running = false;
        allDone.Set();
    }

    private void GetCertificateFromStore(string certName)
    {

        // Get the certificate store for the current user.
        X509Store store = new X509Store(StoreLocation.LocalMachine);
        try
        {
            store.Open(OpenFlags.ReadOnly);

            // Place all certificates in an X509Certificate2Collection object.
            X509Certificate2Collection certCollection = store.Certificates;
            // If using a certificate with a trusted root you do not need to FindByTimeValid, instead:
            // currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certName, true);
            //X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
            //X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindBySubjectName, certName, false);
            X509Certificate2Collection signingCert = certCollection.Find(X509FindType.FindBySubjectName, certName, false);
            if (signingCert.Count > 0)
                serverCertificate = signingCert[0];
            // Return the first certificate in the collection, has the right name and is current.
        }
        catch (Exception e)
        {
            log.error("GetCertificateFromStore: Unable to extract certificate." + e.Message + "\r\n" + e.StackTrace, 15);
        }
        finally
        {
            store.Close();
        }
    }


    public void Start()
    {
        // Create a TCP/IP socket.
        Socket listener = null;

        // Bind the socket to the local endpoint and listen for incoming connections.  
        try
        {
            listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            if (serverCertificate == null) throw new Exception("Unable to find Samana Monitor certificate.");
            listener.Bind(new IPEndPoint(IPAddress.Any, tcpport));
            log.debug(1, "Binding to port " + tcpport.ToString(), 103);
            listener.Listen(100);

            while (running)
            {
                // Set the event to nonsignaled state.  
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.
                log.debug(1, "Waiting for a connection...", 104);
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);
                //log.debug(1, "Connection accepted from " + listener.RemoteEndPoint.ToString(), 106);
                // Wait until a connection is made before continuing.  
                allDone.WaitOne();
            }

        }
        catch (Exception e)
        {
            log.error(e.Message + "\r\n" + e.StackTrace, 2);
        }
        finally
        {
            log.info("Stopping listener gracefully", 5);
        }
    }

    public void AcceptCallback(IAsyncResult ar)
    {
        // Signal the main thread to continue.  
        allDone.Set();

        // Get the socket that handles the client request.
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);
        NetworkStream ns = new NetworkStream(handler);
        SslStream sslStream = new SslStream(ns, false);

        // Create the state object.  
        StateObject state = new StateObject();
        state.workSocket = handler;
        state.sslstream = sslStream;

        try
        {
            log.debug(1, "BeginAccept", 100);
            sslStream.AuthenticateAsServer(serverCertificate, false, System.Security.Authentication.SslProtocols.Tls12, false);

            sslStream.BeginRead(state.buffer, 0, StateObject.BufferSize,
                new AsyncCallback(SSLReadCallback), state);
        }
        catch (Exception e)
        {
            log.error(e.Message + "\r\n" + e.StackTrace, 9);
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }
    }

    public void SSLReadCallback(IAsyncResult ar)
    {
        StateObject state = (StateObject)ar.AsyncState;
        SslStream stream = state.sslstream;
        int byteCount = -1;

        try
        {
            byteCount = stream.EndRead(ar);

            if(byteCount > 0)
            {
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(state.buffer, 0, byteCount)];
                decoder.GetChars(state.buffer, 0, byteCount, chars, 0);
                state.sb.Append(chars);

                string content = state.sb.ToString();
                if (content.Length > 1024)
                {
                    // This server should never receive more than 1024 bytes of data
                    // in the header + body
                    SSLSend(state, new HTTPResponse(400, null).ToString());
                    throw new Exception("Content of request is larger than 1024. Stopping request.");
                }

                if (req != null && req.has_body())
                {
                    // If header already exists and body is expected
                    // keep loading data until content_length
                    if (content.Length < req.content_length)
                    {
                        stream.BeginRead(state.buffer, 0, StateObject.BufferSize, 
                            new AsyncCallback(SSLReadCallback), state);
                    }
                    else
                    {
                        req.body = content.Substring(0, (int)req.content_length);
                        SSLSend(state, sd.get(req));
                    }
                }
                else
                {

                    // Search for end of header 
                    // if \r\n found mark as windows LF
                    bool winlf = true;
                    int end_of_header = content.IndexOf("\r\n\r\n");
                    if (end_of_header == -1)
                    {
                        end_of_header = content.IndexOf("\n\n");
                        if (end_of_header > -1) winlf = false;
                    }

                    if (end_of_header > -1)
                    {
                        // Header found. Trying to identify if body is expected
                        req = new HTTPRequest(content.Substring(0, end_of_header));

                        if (req.has_body())
                        {
                            // Body expected. Extract into b and validate if 
                            // length is the same as content-length header
                            string b = content.Substring(end_of_header + (winlf ? 4 : 2), (int)req.content_length);
                            if (b.Length == req.content_length)
                            {
                                req.body = b;
                                SSLSend(state, sd.get(req));
                            }
                            else
                            {
                                
                                state.sb.Length = 0;
                                state.sb.Capacity = 0;
                                state.sb.Append(b);
                                stream.BeginRead(state.buffer, 0, StateObject.BufferSize, 
                                    new AsyncCallback(SSLReadCallback), state);
                            }
                        }
                        else
                        {
                            SSLSend(state, sd.get(req));
                        }
                    }
                    else
                    {
                        // Not all data received. Get more.  
                        stream.BeginRead(state.buffer, 0, StateObject.BufferSize,
                            new AsyncCallback(SSLReadCallback), state);
                    }
                }
            }
        } 
        catch (Exception e)
        {
            log.error(e.Message + "\r\n" + e.StackTrace, 10);
            return;
        }
    }

    private void SSLSend(StateObject state, string data)
    {
        SslStream stream = state.sslstream;
        byte[] byteData = Encoding.ASCII.GetBytes(data);
        req = null;

        try
        {
            log.debug(1, "Sending data " + byteData.Length.ToString(), 107);
            stream.BeginWrite(byteData, 0, byteData.Length, new AsyncCallback(SSLWriteCallBack), state);
        } 
        catch (Exception e)
        {
            log.error("Unable to send data. " + e.Message + "\n" + e.StackTrace, 10);
        }
    }

    private void SSLWriteCallBack(IAsyncResult ar)
    {
        StateObject state = (StateObject)ar.AsyncState;
        SslStream stream = state.sslstream;
        Socket handler = state.workSocket;

        try
        {
            stream.EndWrite(ar);
            log.debug(1, "Data sent.", 108);
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }
        catch (Exception e)
        {
            log.error(e.Message + "\r\n" + e.StackTrace, 11);
        }
    }

}
