const int leds[] = {2, 3, 4, 5, 6};
const int buttons[] = {8, 9, 10, 11, 12};
const int ledCount = 5;

bool ledState[5] = {false};
unsigned long lastPressTime[5] = {0};
const unsigned long debounceDelay = 200;

void setup() {
  Serial.begin(9600);

  for (int i = 0; i < ledCount; i++) {
    pinMode(leds[i], OUTPUT);
    pinMode(buttons[i], INPUT_PULLUP);
    digitalWrite(leds[i], LOW);
  }
}

void loop() {
  if (Serial.available()) {
    String data = Serial.readStringUntil('\n');
    data.trim();
    int index = data.toInt();

    if (index == -1) {
      for (int i = 0; i < ledCount; i++) {
        ledState[i] = false;
        digitalWrite(leds[i], LOW);
      }
    }
    else if (index >= 0 && index < ledCount) {
      ledState[index] = !ledState[index];
      digitalWrite(leds[index], ledState[index] ? HIGH : LOW);
    }
  }

  for (int i = 0; i < ledCount; i++) {
    if (digitalRead(buttons[i]) == LOW) {
      unsigned long now = millis();
      if (now - lastPressTime[i] > debounceDelay) {
        lastPressTime[i] = now;
        Serial.print("HIT:");
        Serial.println(i);
      }
    }
  }
}
