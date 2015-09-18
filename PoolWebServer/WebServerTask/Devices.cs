using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;


namespace WebServerTask
{
    //Class the defines all devices and what GPIO pins they are connected to.
    public static class Devices
    {
        //Define the GPIO pins numbers
        private const int POOL_PUMP_PIN = 12;
        private const int POOL_WATERFALL_PIN = 13;
        private const int POOL_LIGHTS_PIN = 16;
        private const int YARD_LIGHTS_PIN = 18;
        private const int POOL_SOLAR_VALVE_PIN = 22;

        //Define the GPIO pins 
        private static GpioPin poolPumpPin;
        private static GpioPin poolWaterfallPin;
        private static GpioPin poolLightsPin;
        private static GpioPin yardLightsPin;
        private static GpioPin poolSolarValvePin;

        //Property for GPIO Pin assigned to the Pool Pump
        public static GpioPinValue PoolPumpPinValue
        {
            get
            {
                return poolPumpPin.Read();  //Read the Pin returns High or Low
            }

            set
            {
                if (poolPumpPin.Read() != value) //Only set the pin if is changing
                    poolPumpPin.Write(value);
            }
        }

        //Property to read status of the Pool Pump ON or OFF
        public static string PoolPumpState
        {
            get
            {
                return GetState(PoolPumpPinValue, GpioPinValue.High);  //Get the state
            }
        }

        //Property for GPIO Pin assigned to the Waterfall Pump
        public static GpioPinValue PoolWaterfallPinValue
        {
            get
            {
                return poolWaterfallPin.Read();
            }

            set
            {
                if (poolWaterfallPin.Read() != value)
                    poolWaterfallPin.Write(value);
            }
        }

        //Property to read status of the Waterfall Pump ON or OFF
        public static string PoolWaterfallState
        {
            get
            {
                return GetState(PoolWaterfallPinValue, GpioPinValue.High);
            }
        }

        //Property for GPIO Pin assigned to the Pool Lights
        public static GpioPinValue PoolLightsPinValue
        {
            get
            {
                return poolLightsPin.Read();
            }

            set
            {
                if (poolLightsPin.Read() != value)
                    poolLightsPin.Write(value);
            }
        }

        //Property to read status of the Pool Lights ON or OFF
        public static string PoolLightsState
        {
            get
            {
                return GetState(PoolLightsPinValue, GpioPinValue.High);
            }
        }

        //Property for GPIO Pin assigned to the valve to turn Solar on and off
        public static GpioPinValue PoolSolarValvePinValue
        {
            get
            {
                return poolSolarValvePin.Read();
            }

            set
            {
                if (poolSolarValvePin.Read() != value)
                    poolSolarValvePin.Write(value);
            }
        }

        //Property to read status of the Solar valve ON or OFF
        public static string PoolSolarValveState
        {
            get
            {
                return GetState(PoolSolarValvePinValue, GpioPinValue.High);
            }
        }

        //Property for GPIO Pin assigned to the Yard Lights
        public static GpioPinValue YardLightsPinValue
        {
            get
            {
                return yardLightsPin.Read();
            }

            set
            {
                if (yardLightsPin.Read() != value)
                    yardLightsPin.Write(value);
            }
        }

        //Property to read status of the Yard Lights ON or OFF
        public static string YardLightsState
        {
            get
            {
                return GetState(YardLightsPinValue, GpioPinValue.High);
            }
        }



        //Intialize all GPIO pin used
        public static void InitDevices()
        {
            var gpio = GpioController.GetDefault();
            if (gpio != null)
            {
                //These pins are on an active high relay.  We set everything to OFF when we start
                poolPumpPin = gpio.OpenPin(POOL_PUMP_PIN);
                poolPumpPin.Write(GpioPinValue.Low);
                poolPumpPin.SetDriveMode(GpioPinDriveMode.Output);

                poolWaterfallPin = gpio.OpenPin(POOL_WATERFALL_PIN);
                poolWaterfallPin.Write(GpioPinValue.Low);
                poolWaterfallPin.SetDriveMode(GpioPinDriveMode.Output);

                poolLightsPin = gpio.OpenPin(POOL_LIGHTS_PIN);
                poolLightsPin.Write(GpioPinValue.Low);
                poolLightsPin.SetDriveMode(GpioPinDriveMode.Output);

                yardLightsPin = gpio.OpenPin(YARD_LIGHTS_PIN);
                yardLightsPin.Write(GpioPinValue.Low);
                yardLightsPin.SetDriveMode(GpioPinDriveMode.Output);

                poolSolarValvePin = gpio.OpenPin(POOL_SOLAR_VALVE_PIN);
                poolSolarValvePin.Write(GpioPinValue.Low);
                poolSolarValvePin.SetDriveMode(GpioPinDriveMode.Output);
            }
        }

        //Gets the state of a device based upon it ActiveState
        //ActiveState means what required to turn the device on High or Low on the GPIO pin 
        private static string GetState(GpioPinValue value, GpioPinValue ActiveState)
        {
            string state = "OFF";
            if (value == ActiveState)
                state = "ON";
            return state;
        }

    }
}
