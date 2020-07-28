/*
  Serial Event example

  When new serial data arrives, this sketch adds it to a String.
  When a newline is received, the loop prints the string and clears it.

  A good test for this is to try it with a GPS receiver that sends out
  NMEA 0183 sentences.

  NOTE: The serialEvent() feature is not available on the Leonardo, Micro, or
  other ATmega32U4 based boards.

  created 9 May 2011
  by Tom Igoe

  This example code is in the public domain.

  http://www.arduino.cc/en/Tutorial/SerialEvent
*/

String inputString = "";         // a String to hold incoming data
bool stringComplete = false;  // whether the string is complete
const int analogOutPin = 9; // Analog output pin that the LED is attached to

void setup() {
  // initialize serial:
  Serial.begin(115200);
  // reserve 200 bytes for the inputString:
  inputString.reserve(200);
  inputString = "";
}

void loop() {
  // print the string when a newline arrives:
  if (stringComplete) {
    Serial.print(inputString);
  if(inputString == "255;")
    { // start
        analogWrite(analogOutPin, 255);
        Serial.print(255);
    }
    else if(inputString == "254;")
    { //end
        analogWrite(analogOutPin, 0);
        Serial.print(254);
    }
    // clear the string:
    inputString = "";  
    stringComplete = false;
  }
}
void serialEvent() {
  while (Serial.available()) {
    // get the new byte:
    char inChar = (char)Serial.read();
    // add it to the inputString:
    if(inChar == '\n') continue;
    inputString += inChar;

    // if the incoming character is a newline, set a flag so the main loop can
    // do something about it:
    if (inChar == ';') {
      stringComplete = true;
    }
  }
}
