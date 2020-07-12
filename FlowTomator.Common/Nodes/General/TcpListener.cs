using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

// TODO : remove
// debug purpose only
using System.Windows.Forms;

namespace FlowTomator.Common
{
    [Node("TcpListener", "Network", "Waits for incoming data and stores it until the socket is closed.")]
    public class TcpListener : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                return new Variable[] { lep, port};
            }
        }

        // Inputs
        private Variable<string> lep = new Variable<string>("Local End Point", "127.0.0.1", "Your listening ip");
        private Variable<int> port = new Variable<int>("Port", 30691, "Your listening port");

        // Outputs
        // private Variable<byte[]> = new Variabl

        public override NodeResult Run()
        {
            IPEndPoint ipEndPoint;
            Socket listener, handler;
            byte[] receivedData = new byte[0];

            #region Initialization
            try
            {
                ipEndPoint = new IPEndPoint(IPAddress.Parse(lep.Value), port.Value);
                Log.Info("IPEndPoint created {0}:{1}", ipEndPoint.Address, ipEndPoint.Port);
            }
            catch (Exception e)
            {
                Log.Error("Failed to create an IPEndPoint : {0}", e.Message);
                return NodeResult.Fail;
            }

            try
            {
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Log.Info("Socket created :");
            }
            catch (Exception e)
            {
                Log.Error("Failed to create a socket : {0}", e.Message);
                return NodeResult.Fail;
            }
            #endregion

            #region Core
            // Listen
            try
            {
                listener.Bind(ipEndPoint);
                Log.Info("Socket binded.");
                listener.Listen(10);
                Log.Info("Socket queue set to 10.");
                Log.Info("Waiting for an incoming connection ...");
                handler = listener.Accept();
                Log.Info("Connection accepted from : {0}",((IPEndPoint)handler.RemoteEndPoint).Address);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return NodeResult.Fail;
            }

            Log.Info("Receiving data ...");
            // Receiving Data
            try
            {
                List<byte> listReceivedData = new List<byte>();
                int bytesRead;
                byte[] buffer;
                // TODO : Find the fastest way to get data
                // - Remove Lists, CF Array.* Buffer.*
                while (true)
                {
                    buffer = new byte[1024];
                    bytesRead = handler.Receive(buffer);
                    if (bytesRead <= 0) break;
                    Array.Resize(ref buffer, bytesRead);
                    listReceivedData.AddRange(buffer.ToList());
                }
                receivedData = listReceivedData.ToArray();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return NodeResult.Fail;
            }
            Log.Info("Received data : {0} bytes.",receivedData.Length);
            #endregion

            // Dispose of stuff
            listener.Dispose();
            handler.Dispose();

            //Setup Outputs
            // TODO

            DialogResult result = MessageBox.Show(ASCIIEncoding.Default.GetString(receivedData), "Received data to string", MessageBoxButtons.OK);

            return NodeResult.Success;
        }
    }
}