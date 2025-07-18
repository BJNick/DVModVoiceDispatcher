using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Sentis;
using UnityEngine;

namespace Piper
{
    [ExecuteInEditMode]
    public class PiperManager : MonoBehaviour
    {
        public BackendType backend = BackendType.GPUCompute;
        public ModelAsset model;

        public string voice = "en-us";
        public int sampleRate = 22050;

        private Model _runtimeModel;
        private IWorker _worker;

        private void Awake()
        {
            var espeakPath = Path.Combine(Application.streamingAssetsPath, "espeak-ng-data");
            PiperWrapper.InitPiper(espeakPath);

            _runtimeModel = ModelLoader.Load(model);
            _worker = WorkerFactory.CreateWorker(backend, _runtimeModel);
        }

        public async Task<AudioClip> TextToSpeech(string text, string filename)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            Debug.Log("Piper Phonemize processing text...");
            var phonemes = PiperWrapper.ProcessText(text, voice);
            Debug.Log($"Piper Phonemize processed text: {sw.ElapsedMilliseconds} ms");

            Debug.Log("Starting Piper inference...");
            sw.Restart();

            var inputLengthsShape = new TensorShape(1);
            var scalesShape = new TensorShape(3);
            using var scalesTensor = new TensorFloat(scalesShape, new float[] { 0.667f, 1f, 0.8f });

            var audioBuffer = new List<float>();
            for (int i = 0; i < phonemes.Sentences.Length; i++) 
            {
                var sentence = phonemes.Sentences[i];

                var inputPhonemes = sentence.PhonemesIds;
                var inputShape = new TensorShape(1, inputPhonemes.Length);
                using var inputTensor = new TensorInt(inputShape, inputPhonemes);
                using var inputLengthsTensor = new TensorInt(inputLengthsShape, new int[] { inputPhonemes.Length });

                var input = new Dictionary<string, Tensor>();
                input.Add("input", inputTensor);
                input.Add("input_lengths", inputLengthsTensor);
                input.Add("scales", scalesTensor);

                _worker.Execute(input);

                using var outputTensor = _worker.PeekOutput() as TensorFloat;
                await outputTensor.MakeReadableAsync();

                var output = outputTensor.ToReadOnlyArray();
                audioBuffer.AddRange(output);
            }

            Debug.Log($"Finished piper inference: {sw.ElapsedMilliseconds} ms");
            Debug.Log("Saving to audio clip...");
            sw.Restart();

            var audioClip = AudioClip.Create("piper_tts", audioBuffer.Count, 1, sampleRate, false);
            audioClip.SetData(audioBuffer.ToArray(), 0);

            Debug.Log($"Audio clip saved: {sw.ElapsedMilliseconds} ms");
            
            SaveWav(filename, audioBuffer.ToArray(), sampleRate);

            return audioClip;
        }
        
        public static void SaveWav(string filePath, float[] samples, int sampleRate)
        {
            if (string.IsNullOrEmpty(filePath)) {
                Debug.LogWarning("No wav file specified.");
                return;
            }
            
            // if directory doesn't exist, create it
            var directory = Path.GetDirectoryName(filePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            using (var writer = new BinaryWriter(fileStream))
            {
                int byteRate = sampleRate * 2; // 16 bit mono
                int dataSize = samples.Length * 2;

                // RIFF header
                writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
                writer.Write(36 + dataSize);
                writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));

                // fmt subchunk
                writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1); // PCM
                writer.Write((short)1); // Mono
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write((short)2); // Block align
                writer.Write((short)16); // Bits per sample

                // data subchunk
                writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
                writer.Write(dataSize);

                // Write samples
                foreach (var sample in samples)
                {
                    short intSample = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
                    writer.Write(intSample);
                }
            }
        }

        private void OnDestroy()
        {
            PiperWrapper.FreePiper();
            _worker.Dispose();
        }
    }
}
