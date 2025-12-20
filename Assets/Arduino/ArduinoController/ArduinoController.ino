const int leds[] = {2, 3, 4, 5, 6};
const int buttons[] = {8, 9, 10, 11, 12};
const int ledCount = 5;
const int ledL = 13;

int currentIndex = -1;

void setup() {
  Serial.begin(9600);

  for (int i = 0; i < ledCount; i++) {
    pinMode(leds[i], OUTPUT);
    pinMode(buttons[i], INPUT_PULLUP);
  }
  pinMode(ledL, OUTPUT);
}

void loop() {
  if (Serial.available()) {
    String data = Serial.readStringUntil('\n');
    currentIndex = data.toInt();

    for (int i = 0; i < ledCount; i++) {
      digitalWrite(leds[i], LOW);
    }
    digitalWrite(ledL, LOW);

    if (currentIndex >= 0 && currentIndex < ledCount) {
      digitalWrite(leds[currentIndex], HIGH);
    }
  }

  // if (currentIndex != -1) {
  //   if (digitalRead(buttons[currentIndex]) == LOW) {
  //     digitalWrite(ledL, HIGH);
  //     Serial.print("HIT:");
  //     Serial.println(currentIndex);
  //     delay(300); 
  //   }
  // }

  if (digitalRead(buttons[0]) == LOW) {
    Serial.println("HIT:0");
    delay(200);
  }
  if (digitalRead(buttons[1]) == LOW) {
    Serial.println("HIT:1");
    delay(200);
  }

  if (digitalRead(buttons[2]) == LOW) {
    Serial.println("HIT:2");
    delay(200);
  }
  if (digitalRead(buttons[3]) == LOW) {
    Serial.println("HIT:3");
    delay(200);
  }if (digitalRead(buttons[4]) == LOW) {
    Serial.println("HIT:4");
    delay(200);
  }
}
