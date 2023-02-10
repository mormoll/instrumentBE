using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Web;

namespace instrumentBE
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string portName = "COM3";
            int baudRate = 9600;
            SerialPort serialPort = new SerialPort(portName, baudRate);
            serialPort.Open();
            Console.WriteLine("Connected to Ardurino");
            serialPort.WriteLine("readscaled");
            
            //string serialResponse = serialPort.ReadLine();
            //Console.WriteLine("Arduino response:  " + serialResponse);


                        Console.WriteLine("Waiting for response");
                        string serialResponse = serialPort.ReadLine();

                        Console.WriteLine("Arduino response:  " + serialResponse);
                        Console.ReadKey();
                        serialPort.Close();
                          
        }
    }
    /*static string SerialCommand(string portName, string command) 
    {
        
        int baudRate = 9600;
        SerialPort serialPort = new SerialPort(portName, baudRate);
        serialPort.Open();
        serialPort.WriteLine(command);
        string serialResponse = serialPort.ReadLine();
        return serialResponse;
    }*/
}

