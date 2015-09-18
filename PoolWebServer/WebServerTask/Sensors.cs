using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace WebServerTask
{
    //Class the defines all Temperature Sensors and the I2C interface used to read them 
    public static class Sensors
    {
        private static I2cDevice Device;
        private static Timer periodicTimer;
        //How often to read temperature data from the Arduino Mini Pro
        private static int ReadInterval = 4000;  //4000 = 4 seconds

        //Variables to hold temperature data
        private static string poolTemperature = "--.--";
        private static string solarTemperature = "--.--";
        private static string outsideTemperature = "--.--";

        //Property to expose the Temperature Data
        public static string PoolTemperature
        {
            get
            {   //Lock the variable incase the timer is tring to write to it
                lock (poolTemperature)
                {
                    return poolTemperature;
                }
            }

            set
            {   //Lock the variable incase the HTTP Server is tring to read from it
                lock (poolTemperature)
                {
                    poolTemperature = value;
                }
            }
        }

        //Property to expose the Temperature Data
        public static string SolarTemperature
        {
            get
            {   //Lock the variable incase the timer is tring to write to it
                lock (solarTemperature)
                {
                    return solarTemperature;
                }
            }

            set
            {   //Lock the variable incase the HTTP Server is tring to read from it
                lock (solarTemperature)
                {
                    solarTemperature = value;
                }
            }
        }

        //Property to expose the Temperature Data
        public static string OutsideTemperature
        {
            get
            {   //Lock the variable incase the timer is tring to write to it
                lock (outsideTemperature)
                {
                    return outsideTemperature;
                }
            }

            set
            {   //Lock the variable incase the HTTP Server is tring to read from it
                lock (outsideTemperature)
                {
                    outsideTemperature = value;
                }
            }
        }

        //Initilizes the I2C connection and starts the timer to read I2C Data
        async public static void InitSensors()
        {
            //Set up the I2C connection the Arduino
            var settings = new I2cConnectionSettings(0x40); // Arduino address
            settings.BusSpeed = I2cBusSpeed.StandardMode;
            string aqs = I2cDevice.GetDeviceSelector("I2C1");
            var dis = await DeviceInformation.FindAllAsync(aqs);
            Device = await I2cDevice.FromIdAsync(dis[0].Id, settings);

            //Create a timer to periodicly read the temps from the Arduino
            periodicTimer = new Timer(Sensors.TimerCallback, null, 0, ReadInterval); 
        }

        //Handle the time call back
        private static void TimerCallback(object state)
        {
            byte[] RegAddrBuf = new byte[] { 0x40 };
            byte[] ReadBuf = new byte[24];
            //Read the I2C connection
            try
            {
                Device.Read(ReadBuf); // read the data
            }
            catch (Exception) { }

            //Parse the response
            //Data is in the format "88.99|78.12|100.00" where "PoolTemp|SolarTemp|OutsideTemp"
            char[] cArray = System.Text.Encoding.UTF8.GetString(ReadBuf, 0, 23).ToCharArray();  // Converte  Byte to Char
            String c = new String(cArray).Trim();
            string[] data = c.Split('|');

            //Write the data to temperature variables
            try
            {
                if (data[0].Trim() != "")
                    PoolTemperature = data[0];
                if (data[1].Trim() != "")
                    SolarTemperature = data[1];
                if (data[2].Trim() != "")
                    OutsideTemperature = data[2];
            }
            catch (Exception) { }
        }

    }
}
