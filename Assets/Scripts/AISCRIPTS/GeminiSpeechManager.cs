using System;
using System.Collections;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json.Linq;

public class GeminiSpeechManager : MonoBehaviour
{
    [Header("API Keys")]
    public string geminiApiKey = "AIzaSyC_hf1AVLKzp3mCckdxqqEKFD_96PystUM"; 
    public string ttsApiKey = "AIzaSyC_hf1AVLKzp3mCckdxqqEKFD_96PystUM";
    public string sttApiKey = "AIzaSyC_hf1AVLKzp3mCckdxqqEKFD_96PystUM";

    [Header("UI Elements")]
    public Button sendButton;  // Button to trigger voice input
    public TMP_Text responseText;  
    public AudioSource audioSource;  

    [Header("Speech-to-Text Settings")]
    private string sttUrl;
    private string geminiUrl;
    private string ttsUrl;
    private bool isRecording = false;
    private string audioFilePath;

    void Start()
    {
        geminiUrl = $"https://generativelanguage.googleapis.com/v1/models/gemini-pro:generateContent?key={geminiApiKey}";
        ttsUrl = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={ttsApiKey}";
        sttUrl = $"https://speech.googleapis.com/v1/speech:recognize?key={sttApiKey}";

        sendButton.onClick.AddListener(OnSendButtonClick);
    }

    public void OnSendButtonClick()
    {
        if (!isRecording)
        {
            StartCoroutine(StartRecording());
        }
    }

    IEnumerator StartRecording()
    {
        isRecording = true;
        audioFilePath = Application.persistentDataPath + "/voice.wav";
        responseText.text = "Listening... 🎤";

        // Start recording
        AudioClip clip = Microphone.Start(null, false, 5, 44100);
        yield return new WaitForSeconds(5);
        Microphone.End(null);

        // Save the recorded audio
        SaveAudioClip(clip, audioFilePath);
        responseText.text = "Processing... ⏳";

        // Send to Speech-to-Text API
        StartCoroutine(ConvertSpeechToText(audioFilePath));
    }

    void SaveAudioClip(AudioClip clip, string filePath)
    {
        if (clip == null)
        {
            Debug.LogError("Audio clip is null!");
            return;
        }

        byte[] audioData = WavUtility.FromAudioClip(clip);
        File.WriteAllBytes(filePath, audioData);
    }

    IEnumerator ConvertSpeechToText(string filePath)
    {
        byte[] audioBytes = File.ReadAllBytes(filePath);
        string audioBase64 = Convert.ToBase64String(audioBytes);

        string jsonData = "{ \"config\": { \"encoding\": \"LINEAR16\", \"sampleRateHertz\": 44100, \"languageCode\": \"en-US\" }, \"audio\": { \"content\": \"" + audioBase64 + "\" } }";

        using (UnityWebRequest request = new UnityWebRequest(sttUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log("STT Response: " + response);

                string transcribedText = ExtractSTTResponse(response);
                responseText.text = transcribedText;

                if (!string.IsNullOrEmpty(transcribedText))
                {
                    StartCoroutine(SendRequestToGemini(transcribedText));
                }
            }
            else
            {
                Debug.LogError("STT Error: " + request.error);
                responseText.text = "STT Error: " + request.error;
            }
        }
    }

    string ExtractSTTResponse(string jsonResponse)
    {
        try
        {
            JObject parsedJson = JObject.Parse(jsonResponse);
            return parsedJson["results"]?[0]?["alternatives"]?[0]?["transcript"]?.ToString() ?? "No transcription found";
        }
        catch (Exception ex)
        {
            Debug.LogError("Error parsing STT response: " + ex.Message);
            return "Error parsing STT response";
        }
    }

    IEnumerator SendRequestToGemini(string userMessage)
    {
        string jsonData = "{ \"contents\": [ { \"role\": \"user\", \"parts\": [ { \"text\": \"" + userMessage + "\" } ] } ] }";

        using (UnityWebRequest request = new UnityWebRequest(geminiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log("Gemini Response: " + response);

                string aiResponse = ExtractGeminiResponse(response);
                responseText.text = aiResponse;

                // Convert AI response to speech
                StartCoroutine(ConvertTextToSpeech(aiResponse));
            }
            else
            {
                Debug.LogError("Error sending request: " + request.error);
                responseText.text = "Error: " + request.error;
            }
        }
    }

    string ExtractGeminiResponse(string jsonResponse)
    {
        try
        {
            JObject parsedJson = JObject.Parse(jsonResponse);
            return parsedJson["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "No response found";
        }
        catch (Exception ex)
        {
            Debug.LogError("Error parsing AI response: " + ex.Message);
            return "Error parsing AI response";
        }
    }

    IEnumerator ConvertTextToSpeech(string text)
    {
        string jsonData = "{ \"input\": { \"text\": \"" + text + "\" }, \"voice\": { \"languageCode\": \"en-US\", \"name\": \"en-US-Wavenet-D\" }, \"audioConfig\": { \"audioEncoding\": \"MP3\" } }";

        using (UnityWebRequest request = new UnityWebRequest(ttsUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log("TTS Response: " + response);

                string audioBase64 = ExtractAudio(response);
                if (!string.IsNullOrEmpty(audioBase64))
                {
                    StartCoroutine(PlayAudio(audioBase64));
                }
                else
                {
                    Debug.LogError("TTS API returned an empty response.");
                }
            }
            else
            {
                Debug.LogError("Error converting text to speech: " + request.error);
            }
        }
    }

    string ExtractAudio(string jsonResponse)
    {
        try
        {
            JObject parsedJson = JObject.Parse(jsonResponse);
            return parsedJson["audioContent"]?.ToString();
        }
        catch (Exception ex)
        {
            Debug.LogError("Error parsing TTS response: " + ex.Message);
            return "";
        }
    }

    IEnumerator PlayAudio(string base64Audio)
    {
    if (string.IsNullOrEmpty(base64Audio))
    {
        Debug.LogError("No audio data received.");
        isRecording = false; // Reset state if no audio
        yield break;
    }

    byte[] audioBytes = Convert.FromBase64String(base64Audio);
    string filePath = Application.persistentDataPath + "/tts_audio.mp3";
    File.WriteAllBytes(filePath, audioBytes);

    using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG))
    {
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            audioSource.clip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.Play();
        }
        else
        {
            Debug.LogError("Failed to load audio: " + www.error);
        }
    }

    yield return new WaitForSeconds(audioSource.clip.length); // Wait for speech to finish
    isRecording = false; // 🔄 Reset so the button works again
    }

}
