using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Web;

using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using Microsoft.Data.SqlClient;
using System.Globalization;

namespace instrumentBE
{


    internal class Program
    {

        static string serialPortName = "";
        static string instrumentID = "";

        static void Main(string[] args)
        {
            string filenameSerialConfig = "serial.conf";
            string filenameInstrumentConfig = "instid.conf";
            string sendComPorts = "comports:";

            bool runInBackrgound = false;
            bool enableLogging = false;

            Thread thread = new Thread(Measurement);

            //ConsoleKeyInfo cki;



            // Iteretate through the command line arguments
            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-b":
                        runInBackrgound |= true;
                        break;
                    case "-l":
                        enableLogging = true;
                        break;
                }

            }


            //Introduksjon
            Console.WriteLine("instrumentBE has stared....");
            Console.WriteLine("please enter TCP port number");
            string serverPort = Console.ReadLine();

            try
            {
                int portNumber = Convert.ToInt32(serverPort);
            }
            catch (FormatException)
            {
                Console.WriteLine("Portnumber is not a number! Exiting....");
                Console.WriteLine("Press a key to exit...");
                Console.ReadKey();
                return;
            }

            //serial configuration. Load form file
            StreamReader serialConfReader = new StreamReader(filenameSerialConfig);
            serialPortName = serialConfReader.ReadLine();
            serialConfReader.Close();

            //InstrumentID
            StreamReader InstrumentConfReader = new StreamReader(filenameInstrumentConfig);
            instrumentID = InstrumentConfReader.ReadLine();
            Console.WriteLine("Instrument ID Configured; " + instrumentID);
            InstrumentConfReader.Close();
            
            //Comports
            ListAvailablePorts();
            Console.WriteLine("Enter the port name:");
            string portName = Console.ReadLine();
            int baudRate = 9600;
            SerialPort serialPort = new SerialPort(portName, baudRate);


            //serialPort.Open();
            //Console.WriteLine("Connected to Ardurino");

            string serverIP = "127.0.0.1";
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(serverIP), 5000);
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);




            try
            {
                server.Bind(endpoint);
                server.Listen(10);
            }
            catch (SocketException ex)
            {

                Console.WriteLine(ex.Message);
                Console.WriteLine("Exiting-...");
                Console.ReadKey();
                return;
            }
            Console.WriteLine("Server started. Waiting for connection...");



            //thread.Start();
            while (true)
            {
                try
                {
                    //Console.WriteLine(SerialCommand(serialPortName, "Readscaled"));

                    //Send to InstrumentDataDB

                    Socket client = server.Accept();


                    Console.WriteLine("Client connected...");
                    //data recived
                    byte[] buffer = new byte[1024];
                    int bytesReceived = client.Receive(buffer);

                    string commandReceived = Encoding.ASCII.GetString(buffer, 0, bytesReceived);
                    Console.WriteLine("Received command: " + commandReceived);

                    if (commandReceived.Substring(0, 8) == "comport:")
                    {
                        serialPortName = commandReceived.Substring(8, commandReceived.Length - 8);
                        Console.WriteLine("Serial port Configured; " + serialPortName);
                        StreamWriter serialConfWrite = new StreamWriter(filenameSerialConfig);
                        serialConfWrite.WriteLine(serialPortName);
                        serialConfWrite.Close();

                        client.Send(Encoding.ASCII.GetBytes("Serial port configurated: " + serialPortName));
                        client.Close();

                    }
                    else if (commandReceived.Substring(0, 8) == "comports")
                    {
                        // Get list of COM ports
                        string[] comPorts = System.IO.Ports.SerialPort.GetPortNames();

                        // Join the COM port names into a single string using a semicolon as the separator
                        string comPortString = string.Join(";", comPorts);

                        // Send the COM port list to the client
                        byte[] sendBuffer = Encoding.ASCII.GetBytes(comPortString);
                        client.Send(sendBuffer);
                        client.Close();

                        // Split the received COM port string at the semicolon delimiter
                        string[] receivedComPorts = commandReceived.Substring(9).Split(';');

                        // Do something with the received COM port names, such as populating a combobox
                        // In this example, we will simply print them to the console
                        foreach (string port in receivedComPorts)
                        {
                            if (!string.IsNullOrWhiteSpace(port))
                            {
                                Console.WriteLine(port);
                            }
                        }

                        // Close the client socket
                        client.Close();
                    }
                    else
                    {
                        string commandResponse = SerialCommand(serialPortName, commandReceived);
                        Console.WriteLine(commandResponse);

                        //send to client
                        client.Send(Encoding.ASCII.GetBytes("Command received was: " + commandResponse));
                        client.Close();
                        Console.WriteLine("Clinet disconnected...");
                    }


                }
                catch (Exception ex)
                {
                    // Log the exception or display an error message to the user
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }



            }
            thread.Join();




        }

        private static void Measurement()
        {
            double measurement = 0.0;
            string serialResponse = "";
            string splitResponse = "";
            string connectionString = "Data Source=127.0.0.1,1434;Initial Catalog=InstrumentData;Persist Security Info=True;User ID=sa;Password=S3cur3P@ssW0rd!;Encrypt=False";
            SqlConnection sqlConnection = new SqlConnection(connectionString);


            string sqlInsertMeasurement = "INSERT INTO InstrumentConfDBSet(InstrumentId, Timestamp, Value) "
                                           + "VALUES (@InstrumentId, @Timestamp, @Value)";




            while (true)
            {


                serialResponse = SerialCommand(serialPortName, "readscaled");
                splitResponse = serialResponse.Split(';')[1];
                splitResponse = splitResponse.Substring(0, splitResponse.Length - 2);
                Console.WriteLine(splitResponse);

                measurement = Convert.ToDouble(splitResponse, CultureInfo.InvariantCulture);


                sqlConnection.Open();
                SqlCommand command = new SqlCommand(sqlInsertMeasurement, sqlConnection);

                command.Parameters.AddWithValue("@InstrumentId", Convert.ToInt32(instrumentID));
                command.Parameters.AddWithValue("@Timestamp", DateTime.Now);
                command.Parameters.AddWithValue("@Value", measurement);

                command.ExecuteNonQuery();
                sqlConnection.Close();

                Thread.Sleep(1000);
            }

        }
        static string SerialCommand(string portName, string command)
        {
            int baudRate = 9600;
            string serialResponse = "";
            SerialPort serialPort = new SerialPort(portName, baudRate);
            try
            {
                serialPort.Open();
                serialPort.WriteLine(command);
                serialResponse = serialPort.ReadLine();
                serialPort.Close();
            }
            catch (System.IO.IOException)
            {
                Console.WriteLine("SerialPort failed....");
                serialResponse = "SerialPort failed";
            }
            return serialResponse;
        }

        private static void ListAvailablePorts()
        {
            string[] ports = SerialPort.GetPortNames();

            Console.WriteLine("Available ports:");
            foreach (string port in ports)
            {
                Console.WriteLine(port);
            }
        }
    }




}
//}
/*static string SerialCommand(string portName, string command) 
{
    int baudRate = 9600;
    string serialResponse =""
    SerialPort serialPort = new SerialPort(portName, baudRate);
    try 
        {
            serialPort.Open();
        serialPort.WriteLine(command);
        serialResponse = serialPort.ReadLine();
        serialPort.Close();
        }
        catch (System.IO.IOExcrption)
        {
            Console.Writeline("SerialPort failed....");
            serialResponse = "failed"
           
        }
        return serialResponse;
   
}*/