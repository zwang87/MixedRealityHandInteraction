using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.IO;

public class InteractionManager : MonoBehaviour 
{
//	/// How high off the ground is the sensor (in meters).
//	public float SensorHeight = 1.0f;

	// GUI-Texture object to be used as screen cursor
	public GameObject handCursor;
	
	// GUI-Text object to be used for displaying debug information
	public GameObject debugText;
	
	private bool interactionInited = false;
	private int skeletonTrackingID = 0;
	
	private uint leftHandState = 0;
	private uint rightHandState = 0;
	
	private float leftHandScreenMag = 0f;
	private float rightHandScreenMag = 0f;
	private Vector3 cursorScreenPos = Vector3.zero;
	
	// last event parameters
	private InteractionWrapper.InteractionHandEventType leftHandEvent = InteractionWrapper.InteractionHandEventType.None;
	private InteractionWrapper.InteractionHandEventType lastLeftHandEvent = InteractionWrapper.InteractionHandEventType.None;
	private Vector3 leftHandScreenPos = Vector3.zero;
	
	private InteractionWrapper.InteractionHandEventType rightHandEvent = InteractionWrapper.InteractionHandEventType.None;
	private InteractionWrapper.InteractionHandEventType lastRightHandEvent = InteractionWrapper.InteractionHandEventType.None;
	private Vector3 rightHandScreenPos = Vector3.zero;
	
	private Matrix4x4 kinectToWorld;
	
	private Texture gripHandTexture;
	private Texture releaseHandTexture;
	private Texture normalHandTexture;
	
	
	// returns true if the InteractionLibrary is initialized, otherwise returns false
	public bool IsInteractionInited()
	{
		return interactionInited;
	}
	
	// returns the user ID (skeleton tracking ID), or 0 if no user is currently tracked
	public int GetUserID()
	{
		return skeletonTrackingID;
	}
	
	// returns the current left hand event (none, grip or release)
	public InteractionWrapper.InteractionHandEventType GetLeftHandEvent()
	{
		return leftHandEvent;
	}
	
	// returns the current screen position of the left hand
	public Vector3 GetLeftHandScreenPos()
	{
		return leftHandScreenPos;
	}
	
	// returns true if the left hand is primary for the user
	public bool IsLeftHandPrimary()
	{
		return ((leftHandState & (uint)InteractionWrapper.NuiHandpointerState.PrimaryForUser) != 0);
	}
	
//	// resets the last valid left hand event
//	public void ResetLeftHandEvent()
//	{
//		lastLeftHandEvent = InteractionWrapper.InteractionHandEventType.None;
//	}
	
	// returns the last valid right hand event (none, grip or release)
	public InteractionWrapper.InteractionHandEventType GetRightHandEvent()
	{
		return rightHandEvent;
	}
	
	// returns the current screen position of the right hand
	public Vector3 GetRightHandScreenPos()
	{
		return rightHandScreenPos;
	}
	
	// returns true if the right hand is primary for the user
	public bool IsRightHandPrimary()
	{
		return ((rightHandState & (uint)InteractionWrapper.NuiHandpointerState.PrimaryForUser) != 0);
	}
	
//	// resets the last valid right hand event
//	public void ResetRightHandEvent()
//	{
//		lastRightHandEvent = InteractionWrapper.InteractionHandEventType.None;
//	}
	
	//----------------------------------- end of public functions --------------------------------------//
	
	void Awake() 
	{
//		// get reference to gui texts for left/right hand
//		handGuiText = GameObject.Find("HandGuiText");
//		handCursor = GameObject.Find("HandCursor");
		
		// ensure the needed dlls are in place
		if(InteractionWrapper.CheckKinectInteractionPresence())
		{
			// reload the same level
			Application.LoadLevel(Application.loadedLevel);
		}
	}
	

	void StartInteraction() 
	{
		int hr = 0;
		
		try 
		{
			// initialize Kinect sensor as needed
			hr = InteractionWrapper.InitKinectSensor();
			if(hr != 0)
			{
				throw new Exception("Initialization of Kinect sensor failed");
			}
			
			// initialize Kinect interaction
			hr = InteractionWrapper.InitKinectInteraction();
			if(hr != 0)
			{
				throw new Exception("Initialization of KinectInteraction failed");
			}
			
			// kinect interaction was successfully initialized
			interactionInited = true;
		} 
		catch(DllNotFoundException ex)
		{
			Debug.LogError(ex.ToString());
			if(debugText != null)
				debugText.guiText.text = "Please check the Kinect SDK installation.";
		}
		catch (Exception ex) 
		{
			string message = ex.Message + " - " + InteractionWrapper.GetNuiErrorString(hr);
			Debug.LogError(ex.ToString());
			
			if(debugText != null)
			{
				debugText.guiText.text = message;
			}
				
			return;
		}
		
//		// transform matrix - kinect to world
//		Quaternion quatTiltAngle = new Quaternion();
//		int sensorAngle = InteractionWrapper.GetKinectElevationAngle();
//		quatTiltAngle.eulerAngles = new Vector3(-sensorAngle, 0.0f, 0.0f);
//			
//		float heightAboveHips = SensorHeight - 1.0f;
//		kinectToWorld.SetTRS(new Vector3(0.0f, heightAboveHips, 0.0f), quatTiltAngle, Vector3.one);
		
		// load cursor textures once
		gripHandTexture = (Texture)Resources.Load("GripCursor");
		releaseHandTexture = (Texture)Resources.Load("ReleaseCursor");
		normalHandTexture = (Texture)Resources.Load("HandCursor");
		
		// don't destroy the object on loading levels
		DontDestroyOnLoad(gameObject);
	}
	
	void OnApplicationQuit()
	{
		// uninitialize Kinect interaction
		if(interactionInited)
		{
			InteractionWrapper.FinishKinectInteraction();
			InteractionWrapper.ShutdownKinectSensor();
			interactionInited = false;
		}
	}
	
	void Update () 
	{
		// start Kinect interaction as needed
		if(!interactionInited)
		{
			StartInteraction();
			
			if(!interactionInited)
			{
				Application.Quit();
				return;
			}
		}
		
		// update Kinect interaction
		if(InteractionWrapper.UpdateKinectInteraction() == 0)
		{
			int lastSkeletonTrackingID = skeletonTrackingID;
			skeletonTrackingID = (int)InteractionWrapper.GetSkeletonTrackingID();
			
			if(skeletonTrackingID != 0)
			{
				InteractionWrapper.InteractionHandEventType handEvent = InteractionWrapper.InteractionHandEventType.None;
				Vector4 handPos = Vector4.zero;
				Vector4 shoulderPos = Vector4.zero;
				Vector3 screenPos = Vector3.zero;
				
				// process left hand
				leftHandState = InteractionWrapper.GetLeftHandState();
				handEvent = (InteractionWrapper.InteractionHandEventType)InteractionWrapper.GetLeftHandEvent();
				
//				InteractionWrapper.GetLeftHandPos(ref handPos);
//				Vector3 handWorldPos = kinectToWorld.MultiplyPoint3x4(handPos);
//				
//				InteractionWrapper.GetLeftShoulderPos(ref shoulderPos);
//				Vector3 shoulderWorldPos = kinectToWorld.MultiplyPoint3x4(shoulderPos);
//				
//				Vector3 shoulderToHand =  handWorldPos - shoulderWorldPos; 
//				if(leftHandScreenMag == 0f || skeletonTrackingID != lastSkeletonTrackingID)
//				{
//					leftHandScreenMag = shoulderToHand.magnitude;
//				}
//				
//				if(leftHandScreenMag > 0f)
//				{
//					screenPos.x = Mathf.Clamp01((leftHandScreenMag / 2 + shoulderToHand.x) / leftHandScreenMag);
//					screenPos.y = Mathf.Clamp01((leftHandScreenMag / 2 + shoulderToHand.y) / leftHandScreenMag);
//					leftHandScreenPos = screenPos;
//				}
				
				InteractionWrapper.GetLeftCursorPos(ref handPos);
				leftHandScreenPos.x = Mathf.Clamp01(handPos.x);
				leftHandScreenPos.y = 1.0f - Mathf.Clamp01(handPos.y);
				leftHandScreenPos.z = Mathf.Clamp01(handPos.z);
				
				leftHandEvent = handEvent;
				if(handEvent != InteractionWrapper.InteractionHandEventType.None)
				{
					lastLeftHandEvent = handEvent;
				}
				
				if((leftHandState & (uint)InteractionWrapper.NuiHandpointerState.PrimaryForUser) != 0)
				{
					cursorScreenPos = leftHandScreenPos;
				}
				
				// process right hand
				rightHandState = InteractionWrapper.GetRightHandState();
				handEvent = (InteractionWrapper.InteractionHandEventType)InteractionWrapper.GetRightHandEvent();
				
//				InteractionWrapper.GetRightHandPos(ref handPos);
//				handWorldPos = kinectToWorld.MultiplyPoint3x4(handPos);
//
//				InteractionWrapper.GetRightShoulderPos(ref shoulderPos);
//				shoulderWorldPos = kinectToWorld.MultiplyPoint3x4(shoulderPos);
//				
//				shoulderToHand =  handWorldPos - shoulderWorldPos; 
//				if(rightHandScreenMag == 0f || skeletonTrackingID != lastSkeletonTrackingID)
//				{
//					rightHandScreenMag = shoulderToHand.magnitude;
//				}
//				
//				if(rightHandScreenMag > 0f)
//				{
//					screenPos.x = Mathf.Clamp01((rightHandScreenMag / 2 + shoulderToHand.x) / rightHandScreenMag);
//					screenPos.y = Mathf.Clamp01((rightHandScreenMag / 2 + shoulderToHand.y) / rightHandScreenMag);
//					rightHandScreenPos = screenPos;
//				}
				
				InteractionWrapper.GetRightCursorPos(ref handPos);
				rightHandScreenPos.x = Mathf.Clamp01(handPos.x);
				rightHandScreenPos.y = 1.0f - Mathf.Clamp01(handPos.y);
				rightHandScreenPos.z = Mathf.Clamp01(handPos.z);
				
				rightHandEvent = handEvent;
				if(handEvent != InteractionWrapper.InteractionHandEventType.None)
				{
					lastRightHandEvent = handEvent;
				}	
				
				if((rightHandState & (uint)InteractionWrapper.NuiHandpointerState.PrimaryForUser) != 0)
				{
					cursorScreenPos = rightHandScreenPos;
				}
				
			}
			else
			{
				leftHandState = 0;
				rightHandState = 0;
				
				leftHandEvent = lastLeftHandEvent = InteractionWrapper.InteractionHandEventType.None;
				rightHandEvent = lastRightHandEvent = InteractionWrapper.InteractionHandEventType.None;
			}
		}
		
	}
	
	void OnGUI()
	{
		if(!interactionInited)
			return;
		
//		// display debug information
//		if(debugText)
//		{
//			string sGuiText = "Cursor: " + cursorScreenPos.ToString();
//			debugText.guiText.text = sGuiText;
//		}
		
		// display the cursor status and position
		if(handCursor != null)
		{
			Texture texture = null;
			
			if(IsLeftHandPrimary())
			{
				if(lastLeftHandEvent == InteractionWrapper.InteractionHandEventType.Grip)
					texture = gripHandTexture;
				else if(lastLeftHandEvent == InteractionWrapper.InteractionHandEventType.Release)
					texture = releaseHandTexture;
			}
			else if(IsRightHandPrimary())
			{
				if(lastRightHandEvent == InteractionWrapper.InteractionHandEventType.Grip)
					texture = gripHandTexture;
				else if(lastRightHandEvent == InteractionWrapper.InteractionHandEventType.Release)
					texture = releaseHandTexture;
			}
			
			if(texture == null)
			{
				texture = normalHandTexture;
			}
			
			handCursor.guiTexture.texture = texture;
			handCursor.transform.position = Vector3.Lerp(handCursor.transform.position, cursorScreenPos, 3 * Time.deltaTime);
		}
	}

}
