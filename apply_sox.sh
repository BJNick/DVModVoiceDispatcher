
INPUT="$1"
OUTPUT="$2"

RADIO_FILTERS="highpass 300 \
lowpass 3400 \
compand 0.0,0.1 6:-80,-70,-5 \
overdrive 10 \
fade t 0.05 \
rate 8000 \
norm \
vol 2dB"

# Apply radio effect using SoX
if [[ "$INPUT" =~ IPA/|TrackType/|StationGreetings/ ]]; then
  sox "$INPUT" "$OUTPUT" -V0 -R $RADIO_FILTERS trim 0 -0.1
elif [[ "$INPUT" == */NoiseClick.wav ]]; then
  sox -n "$INPUT" -V0 -R trim 0 0.1 synth 0.1 noise pitch -2000 pad 0 0.1 $RADIO_FILTERS vol -6dB
else
  sox "$INPUT" "$OUTPUT" -V0 -R $RADIO_FILTERS
fi
