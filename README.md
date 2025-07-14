# Voice Dispatcher

Adds a Voice Dispatcher using a TTS model. They will tell you about your jobs, cars, speed limit violations, and welcome you to stations.

You can switch to a new comms radio mode which has the voiced dispatcher features.

Available features:
Job Helper (current order read, completion)
Sign Helper (speed limit signs read, speeding warnings, derailment checks)
Station Helper (station arrival, highest paying jobs read)
Car Helper (point radio to car to describe it, its job)

To edit lines, you can make a copy of lines.json and set the new name in mod settings.

To change TTS model, download it and set the new path in mod settings.

## Credits

This mod is using Piper TTS engine executable https://github.com/rhasspy/piper or https://github.com/OHF-Voice/piper1-gpl

Note they are undergoing restructuring and compatibility may change in the future.

It also uses SoX - Sound eXchange executable https://github.com/rbouqueau/SoX

By default it comes pre-packaged with Joe (en_US-joe-medium) voice. Find more voices at https://huggingface.co/rhasspy/piper-voices/tree/main/en/en_US

## Adding your own trigger

**Note this version is SUBJECT TO CHANGE, and NOT READY for submods and translations.** Hopefully it will get there in the future.

Call from your own mod:

```csharp
string line = JsonLinesLoader.GetRandomAndReplace("speed_limit_change", new() {
    { "speed_limit", "20" },
    { "distance", "243" },
    { "distance_rounded", "240" }
});
CommsRadioNarrator.PlayWithClick(LineChain.SplitIntoChain(line));
```

For testing lines you can use `ConsoleTestRunner` solution.

