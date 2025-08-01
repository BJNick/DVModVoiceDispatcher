
# Need to copy all wav files from
# ..\..\UnityPiperTts\Assets\Output
# to
# .\Lines
# by using
# apply_sox.sh

src_dir='../../UnityPiperTts/Assets/Output'
dst_dir='./Lines'

find "$src_dir" -type f -name '*.wav' | while read -r wav; do
    rel_path="${wav#$src_dir/}"
    out_path="$dst_dir/$rel_path"
    mkdir -p "$(dirname "$out_path")"
    ./apply_sox.sh "$wav" "$out_path"
    echo "Processed: $wav -> $out_path"
done


