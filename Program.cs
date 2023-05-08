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
        ///static SerialPort serialPort = null;

        static void Main(string[] args)
        {
            string filenameSerialConfig = "serial.conf";
            string filenameInstrumentConfig = "instid.conf";
            string sendComPorts = "comports:";
            string logFileName = null; // Initialize the log file name variable

            bool runInBackrgound = false;
            bool enableLogging = false;

            Thread thread = new Thread(Measurement);
            // Add variables to keep track of threads
            Thread measurementThread = null;
            Thread serverThread = null;



            //bool enableThread = false; // Add this variable to check if the thread should be started

            // Loop until valid input is entered
            while (true)
            {
                Console.WriteLine("Enter -l to enable logging or -b to run in background mode:");
                string input = Console.ReadLine();

                // Check for valid input
                if (input == "-b")
                {
                    runInBackrgound = true;
                    break;
                }
                else if (input == "-l")
                {
                    enableLogging = true;
                    break;
                }
                else
                {
                    Console.WriteLine($"Unknown argument: {input}");
                    Console.WriteLine("Please enter a valid argument.");
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

            CheckSerialPorts();
            ListAvailablePorts();

            string portName;
            while (true)
            {
                Console.WriteLine("Enter the port name:");
                portName = Console.ReadLine();
                if (SerialPort.GetPortNames().Contains(portName))
                {
                    break;
                }
                else
                {
                    Console.WriteLine($"Invalid port name: {portName}. Please enter a valid port name or check that instrument is connected.");
                }
            }

            int baudRate = 9600;
            SerialPort serialPort = new SerialPort(portName, baudRate);

            Console.WriteLine("Enter ip address:");
            string serverIP = Console.ReadLine();
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
            
            if (enableLogging)
            {
                Console.WriteLine("Server started with logging enabled.  ");
            }
            else if (runInBackrgound)
            {
                Console.WriteLine("Server started in background mode. BE is listening. Waiting for connection...");
            }

            // Start the measurment thred if logging is enabled
            if (enableLogging)

            {
                //InstrumentID
                Console.WriteLine("Connection to instrumentDataDB established");
                StreamReader InstrumentConfReader = new StreamReader(filenameInstrumentConfig);
                instrumentID = InstrumentConfReader.ReadLine();
                Console.WriteLine("Instrument ID Configured; " + instrumentID);
                InstrumentConfReader.Close();

                thread.Start();
            }



            while (true)
            {
                try
                {
                 

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
            string connectionString1 = "Data Source=127.0.0.1,1434;Initial Catalog=InstrumentData;Persist Security Info=True;User ID=sa;Password=S3cur3P@ssW0rd!;Encrypt=False";
            string connectionString2 = "Data Source=127.0.0.1;Initial Catalog=InstrumentSys;User ID=sa;Password=S3cur3P@ssW0rd!;Encrypt=False";
            SqlConnection sqlConnection = new SqlConnection(connectionString1);
            SqlConnection sqlConnection1 = new SqlConnection(connectionString2);

            string sqlSelectInstrumentID = "SELECT Instrument_id FROM InstrumentSet";
            string sqlInsertMeasurement = "INSERT INTO InstrumentConfDBSet(InstrumentId, Timestamp, Value) "
                                           + "VALUES (@InstrumentId, @Timestamp, @Value)";

            while (true)
            {
                // Read measurement from serial port
                serialResponse = SerialCommand(serialPortName, "readscaled");
                splitResponse = serialResponse.Split(';')[1];
                splitResponse = splitResponse.Substring(0, splitResponse.Length - 2);
                Console.WriteLine(splitResponse);

                measurement = Convert.ToDouble(splitResponse, CultureInfo.InvariantCulture);

                // Insert measurment int InstrumentConfDBSet table
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
                if (serialPort.IsOpen) 
                {
                    serialPort.WriteLine(command);
                    serialResponse = serialPort.ReadLine();
                    serialPort.Close();
                }
                else
                {
                    Console.WriteLine("SerialPort failed....");
                    serialResponse = "SerialPort failed";
                }
            }
            catch (IOException)
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

        private static void CheckSerialPorts()
        {
            if (SerialPort.GetPortNames().Length == 0)
            {
                Console.WriteLine("No serial ports found. Please check that the instrument is connected and try again.");
                Console.WriteLine("Press any key to restart the program...");
                Console.ReadKey();
                // Restart the program by calling the main method again
                Main(new string[] { });
            }
        }
    }





}
