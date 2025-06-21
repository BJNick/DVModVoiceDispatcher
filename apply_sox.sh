
# Check for required input
if [ "$#" -ne 2 ]; then
  echo "Usage: $0 input.wav output.wav"
  exit 1
fi

INPUT="$1"
OUTPUT="$2"

# Apply radio effect using SoX
# Check if input is in IPA/ directory
if [[ "$INPUT" == *IPA/* ]]; then
  sox "$INPUT" "$OUTPUT" -R \
    highpass 300 \
    lowpass 3400 \
    compand 0.0,0.1 6:-80,-70,-5 \
    overdrive 10 \
    fade t 0.05 \
    rate 8000 \
    norm \
    vol 2dB
elif [[ "$INPUT" == *TrackType/* ]]; then
  sox "$INPUT" "$OUTPUT" -R \
    highpass 300 \
    lowpass 3400 \
    compand 0.0,0.1 6:-80,-70,-5 \
    overdrive 10 \
    fade t 0.05 \
    rate 8000 \
    trim 0 -0.1 \
    norm \
    vol 2dB
else
  sox "$INPUT" "$OUTPUT" -R \
    highpass 300 \
    lowpass 3400 \
    compand 0.0,0.1 6:-80,-70,-5 \
    overdrive 10 \
    fade t 0.05 \
    rate 8000 \
    norm \
    vol 2dB
fi

