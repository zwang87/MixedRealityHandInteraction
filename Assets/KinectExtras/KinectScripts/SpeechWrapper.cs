// comment or uncomment the following #define directives
// depending on whether you use KinectExtras together with KinectManager

//#define USE_KINECT_MANAGER

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;


public class SpeechWrapper 
{
    [Flags]
    public enum NuiInitializeFlags : uint
    {
		UsesAudio = 0x10000000,
        UsesDepthAndPlayerIndex = 0x00000001,
        UsesColor = 0x00000002,
        UsesSkeleton = 0x00000008,
        UsesDepth = 0x00000020,
		UsesHighQualityColor = 0x00000040
    }
	
#if USE_KINECT_MANAGER
	public static int InitKinectSensor()
	{
		return 0;
	}
	
	public static void ShutdownKinectSensor()
	{
	}
#else
	[DllImport(@"KinectUnityWrapper", EntryPoint = "InitKinectSensor")]
    public static extern int InitKinectSensor(NuiInitializeFlags dwFlags, bool bEnableEvents);

	[DllImport(@"KinectUnityWrapper", EntryPoint = "ShutdownKinectSensor")]
    public static extern void ShutdownKinectSensor();
	
	public static int InitKinectSensor()
	{
		int hr = InitKinectSensor(NuiInitializeFlags.UsesAudio, false);
		return hr;
	}
#endif
	
	// DLL Imports to pull in the necessary Unity functions to make the Kinect go.
	[DllImport("KinectUnityWrapper")]
	public static extern int InitSpeechRecognizer([MarshalAs(UnmanagedType.LPWStr)]string sRecoCriteria, bool bUseKinect, bool bAdaptationOff);
	[DllImport("KinectUnityWrapper")]
	public static extern void FinishSpeechRecognizer();
	[DllImport("KinectUnityWrapper")]
	public static extern int UpdateSpeechRecognizer();
	
	[DllImport("KinectUnityWrapper")]
	public static extern int LoadSpeechGrammar([MarshalAs(UnmanagedType.LPWStr)]string sFileName, short iNewLangCode);
	[DllImport("KinectUnityWrapper")]
	public static extern void SetRequiredConfidence(float fConfidence);

	[DllImport("KinectUnityWrapper")]
	public static extern bool IsSoundStarted();
	[DllImport("KinectUnityWrapper")]
	public static extern bool IsSoundEnded();
	[DllImport("KinectUnityWrapper")]
	public static extern bool IsPhraseRecognized();
	[DllImport("KinectUnityWrapper")]
	public static extern IntPtr GetRecognizedTag();
	[DllImport("KinectUnityWrapper")]
	public static extern void ClearPhraseRecognized();
	
//	public delegate void SpeechStatusDelegate();
//	public delegate void SpeechRecoDelegate([MarshalAs(UnmanagedType.LPWStr)]string sRecognizedTag);
//	
//	[DllImport("KinectUnityWrapper")]
//	public static extern void SetSoundStartCallback(SpeechStatusDelegate SoundStartDelegate);
//	[DllImport("KinectUnityWrapper")]
//	public static extern void SetSoundEndCallback(SpeechStatusDelegate SoundEndDelegate);
//	[DllImport("KinectUnityWrapper")]
//	public static extern void SetSpeechRecoCallback(SpeechRecoDelegate SpeechRecognizedDelegate);
	
	
	// copies and configures the needed resources in the project directory
	public static bool CheckSpeechWrapperPresence()
	{
		bool bOneCopied = false, bAllCopied = true;
		
		if(!File.Exists("KinectUnityWrapper.dll"))
		{
			Debug.Log("Copying KinectUnityWrapper library...");
			TextAsset textRes = Resources.Load("KinectUnityWrapper.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				//File.WriteAllBytes("InteractionLibrary.dll", textRes.bytes);
				using (FileStream fileStream = new FileStream ("KinectUnityWrapper.dll", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("KinectUnityWrapper.dll");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied KinectUnityWrapper library.");
			}
		}

		if(!File.Exists("KinectInteraction170_32.dll"))
		{
			Debug.Log("Copying KinectInteraction library...");
			TextAsset textRes = Resources.Load("KinectInteraction170_32.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				//File.WriteAllBytes("KinectInteraction170_32.dll", textRes.bytes);
				using (FileStream fileStream = new FileStream ("KinectInteraction170_32.dll", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("KinectInteraction170_32.dll");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied KinectInteraction library.");
			}
		}
		
		if(!File.Exists("FaceTrackData.dll"))
		{
			Debug.Log("Copying FaceTracking data...");
			TextAsset textRes = Resources.Load("FaceTrackData.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				//File.WriteAllBytes("FaceTrackData.dll", textRes.bytes);
				using (FileStream fileStream = new FileStream ("FaceTrackData.dll", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("FaceTrackData.dll");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied FaceTracking data.");
			}
		}
		
		if(!File.Exists("FaceTrackLib.dll"))
		{
			Debug.Log("Copying FaceTracking library...");
			TextAsset textRes = Resources.Load("FaceTrackLib.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				//File.WriteAllBytes("FaceTrackLib.dll", textRes.bytes);
				using (FileStream fileStream = new FileStream ("FaceTrackLib.dll", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("FaceTrackLib.dll");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied FaceTracking library.");
			}
		}
		
		if(!File.Exists("SpeechGrammar.grxml"))
		{
			Debug.Log("Copying SpeechGrammar grammar...");
			TextAsset textRes = Resources.Load("SpeechGrammar.grxml", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				string sResText = textRes.text;
				File.WriteAllText("SpeechGrammar.grxml", sResText);
				
				bOneCopied = File.Exists("SpeechGrammar.grxml");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied SpeechGrammar grammar.");
			}
		}

		if(!File.Exists("msvcp100d.dll"))
		{
			Debug.Log("Copying msvcp100d library...");
			TextAsset textRes = Resources.Load("msvcp100d.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream ("msvcp100d.dll", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("msvcp100d.dll");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied msvcp100d library.");
			}
		}
		
		if(!File.Exists("msvcr100d.dll"))
		{
			Debug.Log("Copying msvcr100d library...");
			TextAsset textRes = Resources.Load("msvcr100d.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream ("msvcr100d.dll", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("msvcr100d.dll");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied msvcr100d library.");
			}
		}
		
		return bOneCopied && bAllCopied;
	}
	
}
