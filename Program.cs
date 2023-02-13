using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Web;

using System.Net.Sockets;
using System.Net;


namespace instrumentBE
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string portName = "COM3";
            int baudRate = 9600;
            SerialPort serialPort = new SerialPort(portName, baudRate);

            //serialPort.Open();
            //Console.WriteLine("Connected to Ardurino");
            
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 5000);
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //string serialResponse = serialPort.ReadLine();
            //Console.WriteLine("Arduino response:  " + serialResponse);
            
            //bind to endpoint and start server
            server.Bind(endpoint);
            server.Listen(10);
            //Output info
            Console.WriteLine("Server started. Waiting for connection...");
            //serialPort.Open();
            //if(log)WriteToLogFile(("Server started. Waiting for clients
            
            while (true)
            {
                Socket client = server.Accept();
                Console.WriteLine("Client connected...");
                //data recived
                byte[] buffer = new byte[1023];
                int bytesReceived = client.Receive(buffer);
                string commandReceived = Encoding.ASCII.GetString(buffer, 0, bytesReceived);
                Console.WriteLine("Received command: " + commandReceived);


                string commandResponse = SerialCommand("COM3", commandReceived);


                //send to client
                client.Send(Encoding.ASCII.GetBytes("Command received was: " + commandResponse));
                client.Close();
                Console.WriteLine("Clinet disconnected...");
            }

            /*Console.WriteLine("Waiting for response");

            serialPort.Open();
            Console.WriteLine("Connected to Ardurino");
            serialPort.WriteLine("readscaled");
            
            //string serialResponse = serialPort.ReadLine();
            //Console.WriteLine("Arduino response:  " + serialResponse);



                        string serialResponse = serialPort.ReadLine();

                        Console.WriteLine("Arduino response:  " + serialResponse);
                        Console.ReadKey();

                        serialPort.Close();*/

        }

        static string SerialCommand(string portName, string command)
        {

            int baudRate = 9600;
            SerialPort serialPort = new SerialPort(portName, baudRate);
            //SerialPort serialPort = new SerialPort("COM3",9600);
            serialPort.Open();
            serialPort.WriteLine(command);
            string serialResponse = serialPort.ReadLine();
            serialPort.Close();
            return serialResponse;


        }
    }

                        //serialPort.Close();
                          
        }
    //}
    /*static string SerialCommand(string portName, string command) 
    {
        
        int baudRate = 9600;
        SerialPort serialPort = new SerialPort(portName, baudRate);
        serialPort.Open();
        serialPort.WriteLine(command);
        string serialResponse = serialPort.ReadLine();
        return serialResponse;
    }*/

//}

