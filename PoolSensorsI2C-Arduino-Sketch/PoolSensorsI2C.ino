#include <OneWire.h>
#include <DallasTemperature.h>
#include <Wire.h>
#define SLAVE_ADDRESS 0x40 

//Define GPIO pin constants
const int POOL_PIN = 3;
const int SOLAR_PIN = 5;
const int OUTSIDE_PIN = 7;

//Define the length of our buffer for the I2C interface
const int I2C_BUFFER_LEN = 24;  //MAX is 32

//Load OneWire - proprietary dallas semiconductor sensor protocol - no license required
OneWire poolTemp(POOL_PIN);
OneWire solarTemp(SOLAR_PIN);
OneWire outsideTemp(OUTSIDE_PIN);

//Load Dallas - proprietary dallas sensor protocol utilizing onewire - no license required
DallasTemperature poolSensor(&poolTemp);
DallasTemperature solarSensor(&solarTemp);
DallasTemperature outsideSensor(&outsideTemp);

//Define I2C buffer
char data[I2C_BUFFER_LEN];
String temperatureData;

//Define variable for timer
long prevMillis = 0;
long interval = 1000;

void setup(void) {
  //Connect to temperature sensor buses
  poolSensor.begin();
  solarSensor.begin();
  outsideSensor.begin();
  //Start the I2C interface
  Wire.begin(SLAVE_ADDRESS);
  Wire.onRequest(requestEvent);
}

void loop(void) {
  //Monitor time to read temperature sensors once every defined interval
  //Don't read them faster than every 1 second. they can't respond that fast 
  unsigned long currMillis = millis();
  if (currMillis - prevMillis > interval) {
    prevMillis = currMillis;
    readTemperatures();
  }
}

void readTemperatures() {
  //Read all three temperature sensors
  poolSensor.requestTemperatures();
  solarSensor.requestTemperatures();
  outsideSensor.requestTemperatures();

  //Store temperature data in a string 
  //We pad right to the full length of the buffer to make sure to overwrite old data
  //Data is in the format "88.99|78.12|100.00" where "PoolTemp|SolarTemp|OutsideTemp"
  temperatureData = padRight(String(poolSensor.getTempFByIndex(0)) + "|" + 
                             String(solarSensor.getTempFByIndex(0)) + "|" + 
                             String(outsideSensor.getTempFByIndex(0)), I2C_BUFFER_LEN);
}

String padRight(String inStr, int inLen) {
  while (inStr.length() < inLen)
    inStr = inStr + " ";
  return inStr;
}

void requestEvent() {
  //sends data over I2C in the format "88.99|78.12|100.00" where "PoolTemp|SolarTemp|OutsideTemp"
  temperatureData.toCharArray(data,I2C_BUFFER_LEN);
  Wire.write(data);
}

