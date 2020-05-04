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
const int analogOutPin = 9; // Analog output pin that the LED is attached to
void setup() {
  // initialize serial:
  Serial.begin(115200);
}

void loop() {
  
  // print the string when a newline arrives:

  if(Serial.available()) {
    // get the new byte:
    char inChar = (char)Serial.read();
    Serial.println(inChar);
    if(inChar == 1)
    { // start
        analogWrite(analogOutPin, 255);
        Serial.println(255);

    }
    else if(inChar == 2)
    { //end
        analogWrite(analogOutPin, 0);
      Serial.println(254);

    }
  }
}

/*
  SerialEvent occurs whenever a new data comes in the hardware serial RX. This
  routine is run between each time loop() runs, so using delay inside loop can
  delay response. Multiple bytes of data may be available.
*/
