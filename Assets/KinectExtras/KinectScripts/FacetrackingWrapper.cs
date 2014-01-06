// comment or uncomment the following #define directives
// depending on whether you use KinectExtras together with KinectManager

//#define USE_KINECT_MANAGER

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;


public class FacetrackingWrapper 
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

	public static class Constants
	{
		public const int ImageWidth = 640;
		public const int ImageHeight = 480;
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
		int hr = InitKinectSensor(NuiInitializeFlags.UsesColor|NuiInitializeFlags.UsesDepthAndPlayerIndex|NuiInitializeFlags.UsesSkeleton, true);
		return hr;
	}
#endif
	
	// DLL Imports to pull in the necessary Unity functions to make the Kinect go.
	[DllImport("KinectUnityWrapper")]
	public static extern int InitFaceTracking();
	[DllImport("KinectUnityWrapper")]
	public static extern void FinishFaceTracking();
	[DllImport("KinectUnityWrapper")]
	public static extern int UpdateFaceTracking();
	
	[DllImport("KinectUnityWrapper")]
	public static extern bool IsFaceTracked();
	[DllImport("KinectUnityWrapper")]
	public static extern int GetAnimUnitsCount();
	[DllImport("KinectUnityWrapper")]
	public static extern bool GetAnimUnits([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1, ArraySubType = UnmanagedType.R4)] float[] afAU, ref int iAUCount);
	[DllImport("KinectUnityWrapper")]
	public static extern bool IsShapeConverged();
	[DllImport("KinectUnityWrapper")]
	public static extern int GetShapeUnitsCount();
	[DllImport("KinectUnityWrapper")]
	public static extern bool GetShapeUnits([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1, ArraySubType = UnmanagedType.R4)] float[] afSU, ref int iSUCount);
	[DllImport("KinectUnityWrapper")]
	public static extern int GetShapePointsCount();
	[DllImport("KinectUnityWrapper")]
	public static extern bool GetShapePoints([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1, ArraySubType = UnmanagedType.R4)] float[] avPoints, ref int iPointsCount);
	
	[DllImport(@"KinectUnityWrapper.dll")]
	public static extern bool GetColorFrameData([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1, ArraySubType = UnmanagedType.U1)] byte[] btVideoBuf, ref uint iVideoBufLen, bool bGetNewFrame);
	[DllImport(@"KinectUnityWrapper.dll")]
	public static extern bool GetDepthFrameData([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1, ArraySubType = UnmanagedType.U2)] short[] shDepthBuf, ref uint iDepthBufLen, bool bGetNewFrame);
	
	[DllImport(@"KinectUnityWrapper.dll")]
	public static extern bool GetHeadPosition(ref Vector4 pvHeadPos);
	[DllImport(@"KinectUnityWrapper.dll")]
	public static extern bool GetHeadRotation(ref Vector4 pvHeadRot);
	[DllImport(@"KinectUnityWrapper.dll")]
	public static extern bool GetHeadScale(ref Vector4 pvHeadScale);

	
	public static int GetImageWidth()
	{
		return Constants.ImageWidth;
	}
	
	public static int GetImageHeight()
	{
		return Constants.ImageHeight;
	}
	
	public static bool PollVideo(ref byte[] videoBuffer, ref Color32[] colorImage)
	{
//		float fTimeNow = Time.realtimeSinceStartup;
		uint videoBufLen = (uint)videoBuffer.Length;
		bool newColor = GetColorFrameData(videoBuffer, ref videoBufLen, true);
//		fTimeNow = Time.realtimeSinceStartup - fTimeNow;
//		Debug.Log("    GetColorFrameData() took " + fTimeNow + " s. and returned: " + newColor);
		
		if (newColor)
		{
//			fTimeNow = Time.realtimeSinceStartup;
			int totalPixels = colorImage.Length;
			
			for (int pix = 0; pix < totalPixels; pix++)
			{
				int ind = totalPixels - pix - 1;
				int src = pix << 2;
				
				colorImage[ind].r = videoBuffer[src + 2]; // pixels[pix].r;
				colorImage[ind].g = videoBuffer[src + 1]; // pixels[pix].g;
				colorImage[ind].b = videoBuffer[src]; // pixels[pix].b;
				colorImage[ind].a = 255;
			}
	
//			fTimeNow = Time.realtimeSinceStartup - fTimeNow;
//			Debug.Log("    pixels conversion took " + fTimeNow + " s.");
		}
		
		return newColor;
	}
	
	
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
