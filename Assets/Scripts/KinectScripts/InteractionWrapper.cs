// comment or uncomment the following #define directives
// depending on whether you use KinectExtras together with KinectManager

#define USE_KINECT_MANAGER

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System;
using System.IO;

public class InteractionWrapper
{
    public enum InteractionHandType : int
    {
        None = 0,
        Left = 1,
        Right = 2
    }

    [Flags]
    public enum NuiHandpointerState : uint
    {
        None = 0,
        Tracked = 1,
        Active = 2,
        Interactive = 4,
        Pressed = 8,
        PrimaryForUser = 0x10
    }

    public enum InteractionHandEventType : int
    {
        None = 0,
        Grip = 1,
        Release = 2
    }

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
	
	public enum NuiErrorCodes : uint
	{
		FrameNoData = 0x83010001,
		StreamNotEnabled = 0x83010002,
		ImageStreamInUse = 0x83010003,
		FrameLimitExceeded = 0x83010004,
		FeatureNotInitialized = 0x83010005,
		DeviceNotGenuine = 0x83010006,
		InsufficientBandwidth = 0x83010007,
		DeviceNotSupported = 0x83010008,
		DeviceInUse = 0x83010009,
		
		DatabaseNotFound = 0x8301000D,
		DatabaseVersionMismatch = 0x8301000E,
		HardwareFeatureUnavailable = 0x8301000F,
		
		DeviceNotConnected = 0x83010014,
		DeviceNotReady = 0x83010015,
		SkeletalEngineBusy = 0x830100AA,
		DeviceNotPowered = 0x8301027F,
	}

//    public struct NuiHandpointerInfo
//    {
//        public uint State;
//        public InteractionHandType HandType;
//        public float X;
//        public float Y;
//        public float PressExtent;
//        public float RawX;
//        public float RawY;
//        public float RawZ;
//        public InteractionHandEventType HandEventType;
//    }
	
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
		int hr = InitKinectSensor(NuiInitializeFlags.UsesDepthAndPlayerIndex|NuiInitializeFlags.UsesSkeleton, true);
		return hr;
	}
#endif
	
	[DllImport(@"KinectUnityWrapper", EntryPoint = "GetKinectElevationAngle")]
	public static extern int GetKinectElevationAngle();
	
	[DllImport(@"KinectUnityWrapper", EntryPoint = "InitKinectInteraction")]
    public static extern int InitKinectInteraction();

    [DllImport(@"KinectUnityWrapper", EntryPoint = "FinishKinectInteraction")]
    public static extern void FinishKinectInteraction();

    [DllImport(@"KinectUnityWrapper", EntryPoint = "UpdateKinectInteraction")]
    public static extern int UpdateKinectInteraction();

    [DllImport(@"KinectUnityWrapper", EntryPoint = "GetSkeletonTrackingID")]
    public static extern uint GetSkeletonTrackingID();

    [DllImport(@"KinectUnityWrapper", EntryPoint = "GetLeftHandState")]
    public static extern uint GetLeftHandState();

    [DllImport(@"KinectUnityWrapper", EntryPoint = "GetRightHandState")]
    public static extern uint GetRightHandState();

    [DllImport(@"KinectUnityWrapper", EntryPoint = "GetLeftHandEvent")]
    public static extern int GetLeftHandEvent();

    [DllImport(@"KinectUnityWrapper", EntryPoint = "GetRightHandEvent")]
    public static extern int GetRightHandEvent();

//    [DllImport(@"KinectUnityWrapper", EntryPoint = "GetLeftHandPos")]
//    public static extern int GetLeftHandPos(ref Vector4 pHandPos);
//
//    [DllImport(@"KinectUnityWrapper", EntryPoint = "GetRightHandPos")]
//    public static extern int GetRightHandPos(ref Vector4 pHandPos);
//
//    [DllImport(@"KinectUnityWrapper", EntryPoint = "GetLeftShoulderPos")]
//    public static extern int GetLeftShoulderPos(ref Vector4 pShoulderPos);
//
//    [DllImport(@"KinectUnityWrapper", EntryPoint = "GetRightShoulderPos")]
//    public static extern int GetRightShoulderPos(ref Vector4 pShoulderPos);

    [DllImport(@"KinectUnityWrapper", EntryPoint = "GetLeftCursorPos")]
    public static extern int GetLeftCursorPos(ref Vector4 pHandPos);

    [DllImport(@"KinectUnityWrapper", EntryPoint = "GetRightCursorPos")]
    public static extern int GetRightCursorPos(ref Vector4 pHandPos);

    [DllImport(@"KinectUnityWrapper", EntryPoint = "GetLeftHandPressed")]
    public static extern bool GetLeftHandPressed();

    [DllImport(@"KinectUnityWrapper", EntryPoint = "GetRightHandPressed")]
    public static extern uint GetRightHandPressed();

	
	public static string State2String(uint state)
	{
		string s = string.Empty;
		
		if((state & (uint)NuiHandpointerState.Tracked) != 0)
			s += "Tracked ";
		if((state & (uint)NuiHandpointerState.Active) != 0)
			s += "Active ";
		if((state & (uint)NuiHandpointerState.Interactive) != 0)
			s += "Interactive ";
		if((state & (uint)NuiHandpointerState.Pressed) != 0)
			s += "Pressed ";
		if((state & (uint)NuiHandpointerState.PrimaryForUser) != 0)
			s += "Primary ";
		
		return s;
	}
	
	public static string GetNuiErrorString(int hr)
	{
		string message = string.Empty;
		uint uhr = (uint)hr;
		
		switch(uhr)
		{
			case (uint)NuiErrorCodes.FrameNoData:
				message = "Frame contains no data.";
				break;
			case (uint)NuiErrorCodes.StreamNotEnabled:
				message = "Stream is not enabled.";
				break;
			case (uint)NuiErrorCodes.ImageStreamInUse:
				message = "Image stream is already in use.";
				break;
			case (uint)NuiErrorCodes.FrameLimitExceeded:
				message = "Frame limit is exceeded.";
				break;
			case (uint)NuiErrorCodes.FeatureNotInitialized:
				message = "Feature is not initialized.";
				break;
			case (uint)NuiErrorCodes.DeviceNotGenuine:
				message = "Device is not genuine.";
				break;
			case (uint)NuiErrorCodes.InsufficientBandwidth:
				message = "Bandwidth is not sufficient.";
				break;
			case (uint)NuiErrorCodes.DeviceNotSupported:
				message = "Device is not supported (e.g. Kinect for XBox 360).";
				break;
			case (uint)NuiErrorCodes.DeviceInUse:
				message = "Device is already in use.";
				break;
			case (uint)NuiErrorCodes.DatabaseNotFound:
				message = "Database not found.";
				break;
			case (uint)NuiErrorCodes.DatabaseVersionMismatch:
				message = "Database version mismatch.";
				break;
			case (uint)NuiErrorCodes.HardwareFeatureUnavailable:
				message = "Hardware feature is not available.";
				break;
			case (uint)NuiErrorCodes.DeviceNotConnected:
				message = "Device is not connected.";
				break;
			case (uint)NuiErrorCodes.DeviceNotReady:
				message = "Device is not ready.";
				break;
			case (uint)NuiErrorCodes.SkeletalEngineBusy:
				message = "Skeletal engine is busy.";
				break;
			case (uint)NuiErrorCodes.DeviceNotPowered:
				message = "Device is not powered.";
				break;
				
			default:
				message = "hr=0x" + uhr.ToString("X");
				break;
		}
		
		return message;
	}
	
	// copies and configures the needed resources in the project directory
	public static bool CheckKinectInteractionPresence()
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

