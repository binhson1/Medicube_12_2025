const int leds[] = {2, 3, 4, 5, 6};
const int buttons[] = {8, 9, 10, 11, 12};
const int ledCount = 5;

unsigned long lastPressTime[5] = {0};
const unsigned long debounceDelay = 150;

void setup() {
  Serial.begin(9600);

  for (int i = 0; i < ledCount; i++) {
    pinMode(leds[i], OUTPUT);
    pinMode(buttons[i], INPUT_PULLUP);
    digitalWrite(leds[i], LOW);
  }
}

void loop() {

  // ===== SERIAL COMMAND =====
  if (Serial.available()) {
    String data = Serial.readStringUntil('\n');
    data.trim();
    int cmd = data.toInt();

    // TẮT TẤT CẢ
    if (cmd == -1) {
      for (int i = 0; i < ledCount; i++) {
        digitalWrite(leds[i], LOW);
      }
    }

    // BẬT LED
    else if (cmd >= 0 && cmd < ledCount) {
      digitalWrite(leds[cmd], HIGH);
    }

    // TẮT LED
    else if (cmd >= 100 && cmd < 100 + ledCount) {
      int index = cmd - 100;
      digitalWrite(leds[index], LOW);
    }
  }

  // ===== BUTTON CHECK =====
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
