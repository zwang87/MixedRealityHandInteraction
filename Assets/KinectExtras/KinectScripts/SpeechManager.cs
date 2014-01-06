using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class SpeechManager : MonoBehaviour 
{
	// Grammar XML file name
	public string GrammarFileName;
	
	// Grammar language (English by default)
	public int LanguageCode = 1033;
	
	// Required confidence
	public float RequiredConfidence = 0.0f;
	
	// GUI Text to show messages.
	private GameObject debugText;

	// Is currently listening
	private bool isListening;
	
	// Current phrase recognized
	private bool isPhraseRecognized;
	private string phraseTagRecognized;
	
	// Bool to keep track of whether Kinect and SAPI have been initialized
	private bool sapiInitialized = false;
	
	// The single instance of SpeechManager
	private static SpeechManager instance;
	
	
	// returns the single SpeechManager instance
    public static SpeechManager Instance
    {
        get
        {
            return instance;
        }
    }
	
	// returns true if SAPI is successfully initialized, false otherwise
	public bool IsSapiInitialized()
	{
		return sapiInitialized;
	}
	
	// returns true if the speech recognizer is listening at the moment
	public bool IsListening()
	{
		return isListening;
	}
	
	// returns true if the speech recognizer has recognized a phrase
	public bool IsPhraseRecognized()
	{
		return isPhraseRecognized;
	}
	
	// returns the tag of the recognized phrase
	public string GetPhraseTagRecognized()
	{
		return phraseTagRecognized;
	}
	
	// clears the recognized phrase
	public void ClearPhraseRecognized()
	{
		isPhraseRecognized = false;
		phraseTagRecognized = String.Empty;
	}
	
	//----------------------------------- end of public functions --------------------------------------//
	
	void Awake() 
	{
		debugText = GameObject.Find("DebugText");
		
		// ensure the needed dlls are in place
		if(SpeechWrapper.CheckSpeechWrapperPresence())
		{
			// reload the same level
			Application.LoadLevel(Application.loadedLevel);
		}
	}
	
	void StartRecognizer() 
	{
		try 
		{
			if(debugText != null)
				debugText.guiText.text = "Please, wait...";
			
			// initialize Kinect sensor as needed
			int rc = SpeechWrapper.InitKinectSensor();
			if(rc != 0)
			{
				throw new Exception("Initialization of Kinect sensor failed");
			}
			
			// Initialize the kinect speech wrapper
			string sCriteria = String.Format("Language={0:X};Kinect=True", LanguageCode);
			rc = SpeechWrapper.InitSpeechRecognizer(sCriteria, true, false);
	        if (rc < 0)
	        {
	            throw new Exception(String.Format("Error initializing Kinect/SAPI: hr=0x{0:X}", rc));
	        }
			
			if(RequiredConfidence > 0)
			{
				SpeechWrapper.SetRequiredConfidence(RequiredConfidence);
			}
			
			if(GrammarFileName != string.Empty)
			{
				rc = SpeechWrapper.LoadSpeechGrammar(GrammarFileName, (short)LanguageCode);
		        if (rc < 0)
		        {
		            throw new Exception(String.Format("Error loading grammar file " + GrammarFileName + ": hr=0x{0:X}", rc));
		        }
			}
			
			instance = this;
			sapiInitialized = true;
			
			DontDestroyOnLoad(gameObject);

			if(debugText != null)
				debugText.guiText.text = "Ready.";
		} 
		catch(DllNotFoundException ex)
		{
			Debug.LogError(ex.ToString());
			if(debugText != null)
				debugText.guiText.text = "Please check the Kinect and SAPI installations.";
		}
		catch (Exception ex) 
		{
			Debug.LogError(ex.ToString());
			if(debugText != null)
				debugText.guiText.text = ex.Message;
		}
	}

	// Make sure to kill the Kinect on quitting.
	void OnApplicationQuit()
	{
		// Shutdown Speech Recognizer and Kinect
		SpeechWrapper.FinishSpeechRecognizer();
		SpeechWrapper.ShutdownKinectSensor();
		
		sapiInitialized = false;
		instance = null;
	}
	
	void Update () 
	{
		// start Kinect speech recognizer as needed
		if(!sapiInitialized)
		{
			StartRecognizer();
			
			if(!sapiInitialized)
			{
				Application.Quit();
				return;
			}
		}
		
		if(sapiInitialized)
		{
			// update the speech recognizer
			int rc = SpeechWrapper.UpdateSpeechRecognizer();
			
			if(rc >= 0)
			{
				// estimate the listening state
				if(SpeechWrapper.IsSoundStarted())
				{
					isListening = true;
				}
				else if(SpeechWrapper.IsSoundEnded())
				{
					isListening = false;
				}

				// check if a grammar phrase has been recognized
				if(SpeechWrapper.IsPhraseRecognized())
				{
					isPhraseRecognized = true;
					
					IntPtr pPhraseTag = SpeechWrapper.GetRecognizedTag();
					phraseTagRecognized = Marshal.PtrToStringUni(pPhraseTag);
					
					SpeechWrapper.ClearPhraseRecognized();
					
					//Debug.Log(phraseTagRecognized);
				}
			}
		}
	}
	
	void OnGUI()
	{
		if(sapiInitialized)
		{
			if(debugText != null)
			{
				if(isPhraseRecognized)
					debugText.guiText.text = phraseTagRecognized;
				else if(isListening)
					debugText.guiText.text = "Listening...";
			}
		}
	}
	
	
}
