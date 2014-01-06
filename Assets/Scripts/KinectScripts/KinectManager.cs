using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;


public class KinectManager : MonoBehaviour
{
	public enum Smoothing : int { None, Default, Medium, Aggressive }
	
	
	// Public Bool to determine how many players there are. Default of one user.
	public bool TwoUsers = false;
	
	// Public Bool to determine if the sensor is used in near mode.
	public bool NearMode = false;

	// Public Bool to determine whether to receive and compute the user map
	public bool ComputeUserMap = false;
	
	// Public Bool to determine whether to receive and compute the color map
	public bool ComputeColorMap = false;
	
	// Public Bool to determine whether to display user map on the GUI
	public bool DisplayUserMap = false;
	
	// Public Bool to determine whether to display color map on the GUI
	public bool DisplayColorMap = false;
	
	// Public Bool to determine whether to display the skeleton lines on user map.
	public bool DisplaySkeletonLines = false;
	
	/// How high off the ground is the sensor (in meters).
	public float SensorHeight = 1.0f;

	// Kinect elevation angle (in degrees)
	public int SensorAngle = 0;
	
	// Minimum user distance in order to process skeleton data
	public float MinUserDistance = 1.0f;
	
	// Public Bool to determine whether to use only the tracked joints (and ignore the inferred ones)
	public bool IgnoreInferredJoints = true;
	
	// Selection of smoothing parameters
	public Smoothing smoothing = Smoothing.Default;
	
	// Public Bool to determine the use of additional filters
	public bool UseBoneOrientationsFilter = false;
	public bool UseClippedLegsFilter = false;
	public bool UseBoneOrientationsConstraint = true;
	public bool UseSelfIntersectionConstraint = false;
	
	// Lists of GameObjects that will be controlled by which player.
	public List<GameObject> Player1Avatars;
	public List<GameObject> Player2Avatars;
	
	// Calibration poses for each player, if needed
	public KinectWrapper.Gestures Player1CalibrationPose;
	public KinectWrapper.Gestures Player2CalibrationPose;
	
	// List of Gestures to detect for each player
	public List<KinectWrapper.Gestures> Player1Gestures;
	public List<KinectWrapper.Gestures> Player2Gestures;
	
	// Minimum time between gesture detections
	public float MinTimeBetweenGestures = 0f;
	
	// GUI Text to show messages.
	public GameObject CalibrationText;
	
	// GUI Texture to display the hand cursor for Player1
	public GameObject HandCursor1;
	
	// GUI Texture to display the hand cursor for Player1
	public GameObject HandCursor2;
	
	// Bool to keep track of whether Kinect has been initialized
	private bool KinectInitialized = false; 
	
	// Bools to keep track of who is currently calibrated.
	private bool Player1Calibrated = false;
	private bool Player2Calibrated = false;
	
	private bool AllPlayersCalibrated = false;
	
	// Values to track which ID (assigned by the Kinect) is player 1 and player 2.
	private uint Player1ID;
	private uint Player2ID;
	
	// Lists of AvatarControllers that will let the models get updated.
	private List<AvatarController> Player1Controllers;
	private List<AvatarController> Player2Controllers;
	
	// User Map vars.
	private Texture2D usersLblTex;
	private Color32[] usersMapColors;
	private short[] usersPrevState;
	private Rect usersMapRect;
	private int usersMapSize;

	private Texture2D usersClrTex;
	//Color[] usersClrColors;
	private Rect usersClrRect;
	
	//short[] usersLabelMap;
	private short[] usersDepthMap;
	private float[] usersHistogramMap;
	
	// List of all users
	private List<uint> allUsers;
	
	// Image stream handles for the kinect
	private IntPtr colorStreamHandle;
	private IntPtr depthStreamHandle;
	
	// Color image data, if used
	private Color32[] colorImage;
	private byte[] usersColorMap;
	
	// Skeleton related structures
	private KinectWrapper.NuiSkeletonFrame skeletonFrame;
	private KinectWrapper.NuiTransformSmoothParameters smoothParameters;
	
	// Skeleton tracking states, positions and joints' orientations
	private Vector3 player1Pos, player2Pos;
	private Matrix4x4 player1Ori, player2Ori;
	private bool[] player1JointsTracked, player2JointsTracked;
	private bool[] player1PrevTracked, player2PrevTracked;
	private Vector3[] player1JointsPos, player2JointsPos;
	private Matrix4x4[] player1JointsOri, player2JointsOri;
	private KinectWrapper.NuiSkeletonBoneOrientation[] jointOrientations;
	
	// Calibration gesture data for each player
	private KinectWrapper.GestureData player1CalibrationData;
	private KinectWrapper.GestureData player2CalibrationData;
	
	// Lists of gesture data, for each player
	private List<KinectWrapper.GestureData> player1Gestures = new List<KinectWrapper.GestureData>();
	private List<KinectWrapper.GestureData> player2Gestures = new List<KinectWrapper.GestureData>();
	
	// general gesture tracking time start
	private float[] gestureTrackingAtTime;
	
	private Matrix4x4 kinectToWorld, flipMatrix;
	private static KinectManager instance;
	
    // Timer for controlling Filter Lerp blends.
    private float lastNuiTime;

	// Filters
	private TrackingStateFilter[] trackingStateFilter;
	private BoneOrientationsFilter[] boneOrientationFilter;
	private ClippedLegsFilter[] clippedLegsFilter;
	private BoneOrientationsConstraint boneConstraintsFilter;
	private SelfIntersectionConstraint selfIntersectionConstraint;
	
	
	// returns the single KinectManager instance
    public static KinectManager Instance
    {
        get
        {
            return instance;
        }
    }
	
	// checks if Kinect is initialized and ready to use. If not, there was an error during Kinect-sensor initialization
	public static bool IsKinectInitialized()
	{
		return instance != null ? instance.KinectInitialized : false;
	}
	
	// this function is used internally by AvatarController
	public static bool IsCalibrationNeeded()
	{
		return false;
	}
	
	// returns the raw depth/user data,if ComputeUserMap is true
	public short[] GetUsersDepthMap()
	{
		return usersDepthMap;
	}
	
	// returns the depth image/users histogram texture,if ComputeUserMap is true
    public Texture2D GetUsersLblTex()
    { 
		return usersLblTex;
	}
	
	// returns the color image texture,if ComputeColorMap is true
    public Texture2D GetUsersClrTex()
    { 
		return usersClrTex;
	}
	
	// returns true if at least one user is currently detected by the sensor
	public bool IsUserDetected()
	{
		return KinectInitialized && (allUsers.Count > 0);
	}
	
	// returns the UserID of Player1, or 0 if no Player1 is detected
	public uint GetPlayer1ID()
	{
		return Player1ID;
	}
	
	// returns the UserID of Player2, or 0 if no Player2 is detected
	public uint GetPlayer2ID()
	{
		return Player2ID;
	}
	
	// returns true if the User is calibrated and ready to use
	public bool IsPlayerCalibrated(uint UserId)
	{
		if(UserId == Player1ID)
			return Player1Calibrated;
		else if(UserId == Player2ID)
			return Player2Calibrated;
		
		return false;
	}
	
	// returns the User position, relative to the Kinect-sensor, in meters
	public Vector3 GetUserPosition(uint UserId)
	{
		if(UserId == Player1ID)
			return player1Pos;
		else if(UserId == Player2ID)
			return player2Pos;
		
		return Vector3.zero;
	}
	
	// returns the User rotation, relative to the Kinect-sensor
	public Quaternion GetUserOrientation(uint UserId, bool flip)
	{
		if(UserId == Player1ID && player1JointsTracked[(int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter])
			return ConvertMatrixToQuat(player1Ori, (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter, flip);
		else if(UserId == Player2ID && player2JointsTracked[(int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter])
			return ConvertMatrixToQuat(player2Ori, (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter, flip);
		
		return Quaternion.identity;
	}
	
	// returns true if the given joint of the specified user is being tracked
	public bool IsJointTracked(uint UserId, int joint)
	{
		if(UserId == Player1ID)
			return joint >= 0 && joint < player1JointsTracked.Length ? player1JointsTracked[joint] : false;
		else if(UserId == Player2ID)
			return joint >= 0 && joint < player2JointsTracked.Length ? player2JointsTracked[joint] : false;
		
		return false;
	}
	
	// returns the joint position of the specified user, relative to the Kinect-sensor, in meters
	public Vector3 GetJointPosition(uint UserId, int joint)
	{
		if(UserId == Player1ID)
			return joint >= 0 && joint < player1JointsPos.Length ? player1JointsPos[joint] : Vector3.zero;
		else if(UserId == Player2ID)
			return joint >= 0 && joint < player2JointsPos.Length ? player2JointsPos[joint] : Vector3.zero;
		
		return Vector3.zero;
	}
	
	// returns the local joint position of the specified user, relative to the parent joint, in meters
	public Vector3 GetJointLocalPosition(uint UserId, int joint)
	{
        int parent = KinectWrapper.GetSkeletonJointParent(joint);

		if(UserId == Player1ID)
			return joint >= 0 && joint < player1JointsPos.Length ? 
				(player1JointsPos[joint] - player1JointsPos[parent]) : Vector3.zero;
		else if(UserId == Player2ID)
			return joint >= 0 && joint < player2JointsPos.Length ? 
				(player2JointsPos[joint] - player2JointsPos[parent]) : Vector3.zero;
		
		return Vector3.zero;
	}
	
	// returns the joint rotation of the specified user, relative to the Kinect-sensor
	public Quaternion GetJointOrientation(uint UserId, int joint, bool flip)
	{
		if(UserId == Player1ID)
		{
			if(joint >= 0 && joint < player1JointsOri.Length && player1JointsTracked[joint])
				return ConvertMatrixToQuat(player1JointsOri[joint], joint, flip);
		}
		else if(UserId == Player2ID)
		{
			if(joint >= 0 && joint < player2JointsOri.Length && player2JointsTracked[joint])
				return ConvertMatrixToQuat(player2JointsOri[joint], joint, flip);
		}
		
		return Quaternion.identity;
	}
	
	// returns the joint rotation of the specified user, relative to the parent joint
	public Quaternion GetJointLocalOrientation(uint UserId, int joint, bool flip)
	{
        int parent = KinectWrapper.GetSkeletonJointParent(joint);

		if(UserId == Player1ID)
		{
			if(joint >= 0 && joint < player1JointsOri.Length && player1JointsTracked[joint])
			{
				Matrix4x4 localMat = (player1JointsOri[parent].inverse * player1JointsOri[joint]);
				return Quaternion.LookRotation(localMat.GetColumn(2), localMat.GetColumn(1));
			}
		}
		else if(UserId == Player2ID)
		{
			if(joint >= 0 && joint < player2JointsOri.Length && player2JointsTracked[joint])
			{
				Matrix4x4 localMat = (player2JointsOri[parent].inverse * player2JointsOri[joint]);
				return Quaternion.LookRotation(localMat.GetColumn(2), localMat.GetColumn(1));
			}
		}
		
		return Quaternion.identity;
	}
	
	// adds a gesture to the list of detected gestures for the specified user
	public void DetectGesture(uint UserId, KinectWrapper.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);
		if(index >= 0)
			DeleteGesture(UserId, gesture);
		
		KinectWrapper.GestureData gestureData = new KinectWrapper.GestureData();
		
		gestureData.userId = UserId;
		gestureData.gesture = gesture;
		gestureData.state = 0;
		gestureData.joint = 0;
		gestureData.progress = 0f;
		gestureData.complete = false;
		gestureData.cancelled = false;
		
		gestureData.checkForGestures = new List<KinectWrapper.Gestures>();
		switch(gesture)
		{
			case KinectWrapper.Gestures.ZoomIn:
				gestureData.checkForGestures.Add(KinectWrapper.Gestures.ZoomOut);
				gestureData.checkForGestures.Add(KinectWrapper.Gestures.Wheel);			
				break;

			case KinectWrapper.Gestures.ZoomOut:
				gestureData.checkForGestures.Add(KinectWrapper.Gestures.ZoomIn);
				gestureData.checkForGestures.Add(KinectWrapper.Gestures.Wheel);			
				break;

			case KinectWrapper.Gestures.Wheel:
				gestureData.checkForGestures.Add(KinectWrapper.Gestures.ZoomIn);
				gestureData.checkForGestures.Add(KinectWrapper.Gestures.ZoomOut);			
				break;
		}
		
		if(UserId == Player1ID)
			player1Gestures.Add(gestureData);
		else if(UserId == Player2ID)
			player2Gestures.Add(gestureData);
	}
	
	// resets the gesture-data state for the given gesture of the specified user
	public bool ResetGesture(uint UserId, KinectWrapper.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);
		if(index < 0)
			return false;
		
		KinectWrapper.GestureData gestureData = (UserId == Player1ID) ? player1Gestures[index] : player2Gestures[index];
		
		gestureData.state = 0;
		gestureData.joint = 0;
		gestureData.progress = 0f;
		gestureData.complete = false;
		gestureData.cancelled = false;
		gestureData.startTrackingAtTime = Time.realtimeSinceStartup + KinectWrapper.Constants.MinTimeBetweenGestures;

		if(UserId == Player1ID)
			player1Gestures[index] = gestureData;
		else if(UserId == Player2ID)
			player2Gestures[index] = gestureData;
		
		return true;
	}
	
	// deletes the given gesture from the list of detected gestures for the specified user
	public bool DeleteGesture(uint UserId, KinectWrapper.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);
		if(index < 0)
			return false;
		
		if(UserId == Player1ID)
			player1Gestures.RemoveAt(index);
		else if(UserId == Player2ID)
			player2Gestures.RemoveAt(index);
		
		return true;
	}
	
	// clears detected gestures list for the specified user
	public void ClearGestures(uint UserId)
	{
		if(UserId == Player1ID)
		{
			player1Gestures.Clear();
		}
		else if(UserId == Player2ID)
		{
			player2Gestures.Clear();
		}
	}
	
	// returns the count of detected gestures in the list of detected gestures for the specified user
	public int GetGesturesCount(uint UserId)
	{
		if(UserId == Player1ID)
			return player1Gestures.Count;
		else if(UserId == Player2ID)
			return player2Gestures.Count;
		
		return 0;
	}
	
	// returns the list of detected gestures for the specified user
	public List<KinectWrapper.Gestures> GetGesturesList(uint UserId)
	{
		List<KinectWrapper.Gestures> list = new List<KinectWrapper.Gestures>();

		if(UserId == Player1ID)
		{
			foreach(KinectWrapper.GestureData data in player1Gestures)
				list.Add(data.gesture);
		}
		else if(UserId == Player2ID)
		{
			foreach(KinectWrapper.GestureData data in player1Gestures)
				list.Add(data.gesture);
		}
		
		return list;
	}
	
	// returns true, if the given gesture is in the list of detected gestures for the specified user
	public bool IsGestureDetected(uint UserId, KinectWrapper.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);
		return index >= 0;
	}
	
	// returns true, if the given gesture for the specified user is complete
	public bool IsGestureComplete(uint UserId, KinectWrapper.Gestures gesture, bool bResetOnComplete)
	{
		int index = GetGestureIndex(UserId, gesture);

		if(index >= 0)
		{
			if(UserId == Player1ID)
			{
				KinectWrapper.GestureData gestureData = player1Gestures[index];
				
				if(bResetOnComplete && gestureData.complete)
				{
					ResetGesture(UserId, gesture);
					return true;
				}
				
				return gestureData.complete;
			}
			else if(UserId == Player2ID)
			{
				KinectWrapper.GestureData gestureData = player2Gestures[index];

				if(bResetOnComplete && gestureData.complete)
				{
					ResetGesture(UserId, gesture);
					return true;
				}
				
				return gestureData.complete;
			}
		}
		
		return false;
	}
	
	// returns true, if the given gesture for the specified user is cancelled
	public bool IsGestureCancelled(uint UserId, KinectWrapper.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);

		if(index >= 0)
		{
			if(UserId == Player1ID)
			{
				KinectWrapper.GestureData gestureData = player1Gestures[index];
				return gestureData.cancelled;
			}
			else if(UserId == Player2ID)
			{
				KinectWrapper.GestureData gestureData = player2Gestures[index];
				return gestureData.cancelled;
			}
		}
		
		return false;
	}
	
	// returns the progress in range [0, 1] of the given gesture for the specified user
	public float GetGestureProgress(uint UserId, KinectWrapper.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);

		if(index >= 0)
		{
			if(UserId == Player1ID)
			{
				KinectWrapper.GestureData gestureData = player1Gestures[index];
				return gestureData.progress;
			}
			else if(UserId == Player2ID)
			{
				KinectWrapper.GestureData gestureData = player2Gestures[index];
				return gestureData.progress;
			}
		}
		
		return 0f;
	}
	
	// returns the current "screen position" of the given gesture for the specified user
	public Vector3 GetGestureScreenPos(uint UserId, KinectWrapper.Gestures gesture)
	{
		int index = GetGestureIndex(UserId, gesture);

		if(index >= 0)
		{
			if(UserId == Player1ID)
			{
				KinectWrapper.GestureData gestureData = player1Gestures[index];
				return gestureData.screenPos;
			}
			else if(UserId == Player2ID)
			{
				KinectWrapper.GestureData gestureData = player2Gestures[index];
				return gestureData.screenPos;
			}
		}
		
		return Vector3.zero;
	}
	
	// recreates and reinitializes the lists of avatar controllers, after the list of avatars for player 1/2 was changed
	public void SetAvatarControllers()
	{
		if(Player1Avatars.Count == 0 && Player2Avatars.Count == 0)
		{
			AvatarController[] avatars = FindObjectsOfType(typeof(AvatarController)) as AvatarController[];
			
			foreach(AvatarController avatar in avatars)
			{
				Player1Avatars.Add(avatar.gameObject);
			}
		}
		
		if(Player1Controllers != null)
		{
			Player1Controllers.Clear();
	
			foreach(GameObject avatar in Player1Avatars)
			{
				if(avatar != null && avatar.activeInHierarchy)
				{
					AvatarController controller = avatar.GetComponent<AvatarController>();
					controller.RotateToInitialPosition();
					controller.Start();
					
					Player1Controllers.Add(controller);
				}
			}
		}
		
		if(Player2Controllers != null)
		{
			Player2Controllers.Clear();
			
			foreach(GameObject avatar in Player2Avatars)
			{
				if(avatar != null && avatar.activeInHierarchy)
				{
					AvatarController controller = avatar.GetComponent<AvatarController>();
					controller.RotateToInitialPosition();
					controller.Start();
					
					Player2Controllers.Add(controller);
				}
			}
		}
	}
	
	// removes the currently detected kinect users, allowing a new detection/calibration process to start
	public void ClearKinectUsers()
	{
		if(!KinectInitialized)
			return;

		// remove current users
		for(int i = allUsers.Count - 1; i >= 0; i--)
		{
			uint userId = allUsers[i];
			RemoveUser(userId);
		}
		
		ResetFilters();
	}
	
	// clears Kinect buffers and resets the filters
	public void ResetFilters()
	{
		if(!KinectInitialized)
			return;
		
		// clear kinect vars
		player1Pos = Vector3.zero; player2Pos = Vector3.zero;
		player1Ori = Matrix4x4.identity; player2Ori = Matrix4x4.identity;
		
		int skeletonJointsCount = (int)KinectWrapper.NuiSkeletonPositionIndex.Count;
		for(int i = 0; i < skeletonJointsCount; i++)
		{
			player1JointsTracked[i] = false; player2JointsTracked[i] = false;
			player1PrevTracked[i] = false; player2PrevTracked[i] = false;
			player1JointsPos[i] = Vector3.zero; player2JointsPos[i] = Vector3.zero;
			player1JointsOri[i] = Matrix4x4.identity; player2JointsOri[i] = Matrix4x4.identity;
		}
		
		if(trackingStateFilter != null)
		{
			for(int i = 0; i < trackingStateFilter.Length; i++)
				if(trackingStateFilter[i] != null)
					trackingStateFilter[i].Reset();
		}
		
		if(boneOrientationFilter != null)
		{
			for(int i = 0; i < boneOrientationFilter.Length; i++)
				if(boneOrientationFilter[i] != null)
					boneOrientationFilter[i].Reset();
		}
		
		if(clippedLegsFilter != null)
		{
			for(int i = 0; i < clippedLegsFilter.Length; i++)
				if(clippedLegsFilter[i] != null)
					clippedLegsFilter[i].Reset();
		}
	}
	
	
	//----------------------------------- end of public functions --------------------------------------//

	void Start()
	{
		//CalibrationText = GameObject.Find("CalibrationText");
		int hr = 0;
		
		try
		{
			hr = KinectWrapper.NuiInitialize(KinectWrapper.NuiInitializeFlags.UsesDepthAndPlayerIndex | 
				KinectWrapper.NuiInitializeFlags.UsesSkeleton | 
				(ComputeColorMap ? KinectWrapper.NuiInitializeFlags.UsesColor : 0));
            if (hr != 0)
			{
            	throw new Exception("NuiInitialize Failed");
			}
			
			hr = KinectWrapper.NuiSkeletonTrackingEnable(IntPtr.Zero, 8);  // 0, 12,8
			if (hr != 0)
			{
				throw new Exception("Cannot initialize Skeleton Data");
			}
			
			depthStreamHandle = IntPtr.Zero;
			if(ComputeUserMap)
			{
				hr = KinectWrapper.NuiImageStreamOpen(KinectWrapper.NuiImageType.DepthAndPlayerIndex, 
					KinectWrapper.Constants.ImageResolution, 0, 2, IntPtr.Zero, ref depthStreamHandle);
				if (hr != 0)
				{
					throw new Exception("Cannot open depth stream");
				}
			}
			
			colorStreamHandle = IntPtr.Zero;
			if(ComputeColorMap)
			{
				hr = KinectWrapper.NuiImageStreamOpen(KinectWrapper.NuiImageType.Color, 
					KinectWrapper.Constants.ImageResolution, 0, 2, IntPtr.Zero, ref colorStreamHandle);
				if (hr != 0)
				{
					throw new Exception("Cannot open color stream");
				}
			}

			// set kinect elevation angle
			KinectWrapper.NuiCameraElevationSetAngle(SensorAngle);
			
			// init skeleton structures
			skeletonFrame = new KinectWrapper.NuiSkeletonFrame() 
							{ 
								SkeletonData = new KinectWrapper.NuiSkeletonData[KinectWrapper.Constants.NuiSkeletonCount] 
							};
			
			// values used to pass to smoothing function
			smoothParameters = new KinectWrapper.NuiTransformSmoothParameters();
			
			switch(smoothing)
			{
				case Smoothing.Default:
					smoothParameters.fSmoothing = 0.5f;
					smoothParameters.fCorrection = 0.5f;
					smoothParameters.fPrediction = 0.5f;
					smoothParameters.fJitterRadius = 0.05f;
					smoothParameters.fMaxDeviationRadius = 0.04f;
					break;
				case Smoothing.Medium:
					smoothParameters.fSmoothing = 0.5f;
					smoothParameters.fCorrection = 0.1f;
					smoothParameters.fPrediction = 0.5f;
					smoothParameters.fJitterRadius = 0.1f;
					smoothParameters.fMaxDeviationRadius = 0.1f;
					break;
				case Smoothing.Aggressive:
					smoothParameters.fSmoothing = 0.7f;
					smoothParameters.fCorrection = 0.3f;
					smoothParameters.fPrediction = 1.0f;
					smoothParameters.fJitterRadius = 1.0f;
					smoothParameters.fMaxDeviationRadius = 1.0f;
					break;
			}
			
			// init the tracking state filter
			trackingStateFilter = new TrackingStateFilter[KinectWrapper.Constants.NuiSkeletonMaxTracked];
			for(int i = 0; i < trackingStateFilter.Length; i++)
			{
				trackingStateFilter[i] = new TrackingStateFilter();
				trackingStateFilter[i].Init();
			}
			
			// init the bone orientation filter
			boneOrientationFilter = new BoneOrientationsFilter[KinectWrapper.Constants.NuiSkeletonMaxTracked];
			for(int i = 0; i < boneOrientationFilter.Length; i++)
			{
				boneOrientationFilter[i] = new BoneOrientationsFilter();
				boneOrientationFilter[i].Init();
			}
			
			// init the clipped legs filter
			clippedLegsFilter = new ClippedLegsFilter[KinectWrapper.Constants.NuiSkeletonMaxTracked];
			for(int i = 0; i < clippedLegsFilter.Length; i++)
			{
				clippedLegsFilter[i] = new ClippedLegsFilter();
			}

			// init the bone orientation constraints
			boneConstraintsFilter = new BoneOrientationsConstraint();
			boneConstraintsFilter.AddDefaultConstraints();
			// init the self intersection constraints
			selfIntersectionConstraint = new SelfIntersectionConstraint();
			
			// create arrays for joint positions and joint orientations
			int skeletonJointsCount = (int)KinectWrapper.NuiSkeletonPositionIndex.Count;
			
			player1JointsTracked = new bool[skeletonJointsCount];
			player2JointsTracked = new bool[skeletonJointsCount];
			player1PrevTracked = new bool[skeletonJointsCount];
			player2PrevTracked = new bool[skeletonJointsCount];
			
			player1JointsPos = new Vector3[skeletonJointsCount];
			player2JointsPos = new Vector3[skeletonJointsCount];
			
			player1JointsOri = new Matrix4x4[skeletonJointsCount];
			player2JointsOri = new Matrix4x4[skeletonJointsCount];
			
			gestureTrackingAtTime = new float[KinectWrapper.Constants.NuiSkeletonMaxTracked];
			
			//create the transform matrix that converts from kinect-space to world-space
			Quaternion quatTiltAngle = new Quaternion();
			quatTiltAngle.eulerAngles = new Vector3(-SensorAngle, 0.0f, 0.0f);
			
			float heightAboveHips = SensorHeight - 1.0f;
			
			// transform matrix - kinect to world
			kinectToWorld.SetTRS(new Vector3(0.0f, heightAboveHips, 0.0f), quatTiltAngle, Vector3.one);
			flipMatrix = Matrix4x4.identity;
			flipMatrix[2, 2] = -1;
			
			instance = this;
			DontDestroyOnLoad(gameObject);
		}
		catch(DllNotFoundException e)
		{
			string message = "Please check the Kinect SDK installation.";
			Debug.LogError(message);
			Debug.LogError(e.ToString());
			if(CalibrationText != null)
				CalibrationText.guiText.text = message;
				
			return;
		}
		catch (Exception e)
		{
			string message = e.Message + " - " + KinectWrapper.GetNuiErrorString(hr);
			Debug.LogError(message);
			Debug.LogError(e.ToString());
			if(CalibrationText != null)
				CalibrationText.guiText.text = message;
				
			return;
		}

		if(ComputeUserMap)
		{
	        // Initialize depth & label map related stuff
	        usersMapSize = KinectWrapper.GetDepthWidth() * KinectWrapper.GetDepthHeight();
	        usersLblTex = new Texture2D(KinectWrapper.GetDepthWidth(), KinectWrapper.GetDepthHeight());
	        usersMapColors = new Color32[usersMapSize];
			usersPrevState = new short[usersMapSize];
	        usersMapRect = new Rect(Screen.width, Screen.height - usersLblTex.height / 2, -usersLblTex.width / 2, usersLblTex.height / 2);
			
	        usersDepthMap = new short[usersMapSize];
	        usersHistogramMap = new float[5000];
		}
		
		if(ComputeColorMap)
		{
			// Initialize color map related stuff
	        usersClrTex = new Texture2D(KinectWrapper.GetDepthWidth(), KinectWrapper.GetDepthHeight());
	        //usersClrColors = new Color[usersMapSize];
	        usersClrRect = new Rect(Screen.width, Screen.height - usersClrTex.height / 2, -usersClrTex.width / 2, usersClrTex.height / 2);
			
			if(ComputeUserMap)
				usersMapRect.x -= usersClrTex.width / 2;
			
			colorImage = new Color32[KinectWrapper.GetDepthWidth() * KinectWrapper.GetDepthHeight()];
			usersColorMap = new byte[colorImage.Length << 2];
		}
		
		// try to automatically find the available avatar controllers in the scene
		if(Player1Avatars.Count == 0 && Player2Avatars.Count == 0)
		{
			AvatarController[] avatars = FindObjectsOfType(typeof(AvatarController)) as AvatarController[];
			
			foreach(AvatarController avatar in avatars)
			{
				Player1Avatars.Add(avatar.gameObject);
			}
		}
		
        // Initialize user list to contain ALL users.
        allUsers = new List<uint>();
        
		// Pull the AvatarController from each of the players Avatars.
		Player1Controllers = new List<AvatarController>();
		Player2Controllers = new List<AvatarController>();
		
		// Add each of the avatars' controllers into a list for each player.
		foreach(GameObject avatar in Player1Avatars)
		{
			if(avatar != null && avatar.activeInHierarchy)
			{
				Player1Controllers.Add(avatar.GetComponent<AvatarController>());
			}
		}
		
		foreach(GameObject avatar in Player2Avatars)
		{
			if(avatar != null && avatar.activeInHierarchy)
			{
				Player2Controllers.Add(avatar.GetComponent<AvatarController>());
			}
		}
		
		// GUI Text.
		if(CalibrationText != null)
		{
			CalibrationText.guiText.text = "WAITING FOR USERS";
		}
		
		Debug.Log("Waiting for users.");
			
		KinectInitialized = true;
	}
	
	void Update()
	{
		if(KinectInitialized)
		{
//			// for testing purposes only
//			KinectWrapper.UpdateKinectSensor();
			
	        // If the players aren't all calibrated yet, draw the user map.
			if(ComputeUserMap)
			{
				if(depthStreamHandle != IntPtr.Zero &&
					KinectWrapper.PollDepth(depthStreamHandle, NearMode, ref usersDepthMap))
				{
		        	UpdateUserMap();
				}
			}
			
			if(ComputeColorMap)
			{
				if(colorStreamHandle != IntPtr.Zero &&
					KinectWrapper.PollColor(colorStreamHandle, ref usersColorMap, ref colorImage))
				{
					UpdateColorMap();
				}
			}
			
			if(KinectWrapper.PollSkeleton(ref smoothParameters, ref skeletonFrame))
			{
				ProcessSkeleton();
			}
			
			// Update player 1's models if he/she is calibrated and the model is active.
			if(Player1Calibrated)
			{
				foreach (AvatarController controller in Player1Controllers)
				{
					//if(controller.Active)
					{
						controller.UpdateAvatar(Player1ID, NearMode);
					}
					
					// Check for complete gestures
					foreach(KinectWrapper.GestureData gestureData in player1Gestures)
					{
						if(gestureData.complete)
						{
							if(controller.GestureComplete(Player1ID, gestureData.gesture, 
							                              (KinectWrapper.SkeletonJoint)gestureData.joint, gestureData.screenPos))
							{
								ResetGesture(Player1ID, gestureData.gesture);
							}
						}
						else if(gestureData.cancelled)
						{
							if(controller.GestureCancelled(Player1ID, gestureData.gesture, 
								(KinectWrapper.SkeletonJoint)gestureData.joint))
							{
								ResetGesture(Player1ID, gestureData.gesture);
							}
						}
						else if(gestureData.progress >= 0.1f)
						{
							if((gestureData.gesture == KinectWrapper.Gestures.RightHandCursor || 
								gestureData.gesture == KinectWrapper.Gestures.LeftHandCursor) && 
								gestureData.progress >= 0.5f && HandCursor1 != null)
							{
								HandCursor1.transform.position = Vector3.Lerp(HandCursor1.transform.position, gestureData.screenPos, 3 * Time.deltaTime);
							}
				
							controller.GestureInProgress(Player1ID, gestureData.gesture, gestureData.progress, 
								(KinectWrapper.SkeletonJoint)gestureData.joint, gestureData.screenPos);
						}
					}
				}
			}
			
			// Update player 2's models if he/she is calibrated and the model is active.
			if(Player2Calibrated)
			{
				foreach (AvatarController controller in Player2Controllers)
				{
					//if(controller.Active)
					{
						controller.UpdateAvatar(Player2ID, NearMode);
					}

					// Check for complete gestures
					foreach(KinectWrapper.GestureData gestureData in player2Gestures)
					{
						if(gestureData.complete)
						{
							if(controller.GestureComplete(Player2ID, gestureData.gesture, 
								(KinectWrapper.SkeletonJoint)gestureData.joint, gestureData.screenPos))
							{
								ResetGesture(Player2ID, gestureData.gesture);
							}
						}
						else if(gestureData.cancelled)
						{
							if(controller.GestureCancelled(Player2ID, gestureData.gesture, 
								(KinectWrapper.SkeletonJoint)gestureData.joint))
							{
								ResetGesture(Player2ID, gestureData.gesture);
							}
						}
						else if(gestureData.progress >= 0.1f)
						{
							if((gestureData.gesture == KinectWrapper.Gestures.RightHandCursor || 
								gestureData.gesture == KinectWrapper.Gestures.LeftHandCursor) && 
								gestureData.progress >= 0.5f && HandCursor2 != null)
							{
								HandCursor2.transform.position = Vector3.Lerp(HandCursor2.transform.position, gestureData.screenPos, 3 * Time.deltaTime);
							}
							
							controller.GestureInProgress(Player2ID, gestureData.gesture, gestureData.progress, 
								(KinectWrapper.SkeletonJoint)gestureData.joint, gestureData.screenPos);
						}
					}
				}
			}
		}
		
		// Kill the program with ESC.
		if(Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}
	}
	
	// Make sure to kill the Kinect on quitting.
	void OnApplicationQuit()
	{
		if(KinectInitialized)
		{
			// Shutdown OpenNI
			KinectWrapper.NuiShutdown();
			instance = null;
		}
	}
	
	// Draw the Histogram Map on the GUI.
    void OnGUI()
    {
		if(KinectInitialized)
		{
	        if(ComputeUserMap && (/**(allUsers.Count == 0) ||*/ DisplayUserMap))
	        {
	            GUI.DrawTexture(usersMapRect, usersLblTex);
	        }

			if(ComputeColorMap && (/**(allUsers.Count == 0) ||*/ DisplayColorMap))
			{
				GUI.DrawTexture(usersClrRect, usersClrTex);
			}
		}
    }
	
	// Update the User Map
    void UpdateUserMap()
    {
        int numOfPoints = 0;
		Array.Clear(usersHistogramMap, 0, usersHistogramMap.Length);

        // Calculate cumulative histogram for depth
        for (int i = 0; i < usersMapSize; i++)
        {
            // Only calculate for depth that contains users
            if ((usersDepthMap[i] & 7) != 0)
            {
                usersHistogramMap[usersDepthMap[i] >> 3]++;
                numOfPoints++;
            }
        }
		
        if (numOfPoints > 0)
        {
            for (int i = 1; i < usersHistogramMap.Length; i++)
	        {   
		        usersHistogramMap[i] += usersHistogramMap[i-1];
	        }
			
            for (int i = 0; i < usersHistogramMap.Length; i++)
	        {
                usersHistogramMap[i] = 1.0f - (usersHistogramMap[i] / numOfPoints);
	        }
        }
		
		// dummy structure needed by the coordinate mapper
        KinectWrapper.NuiImageViewArea pcViewArea = new KinectWrapper.NuiImageViewArea 
		{
            eDigitalZoom = 0,
            lCenterX = 0,
            lCenterY = 0
        };
		
        // Create the actual users texture based on label map and depth histogram
		Color32 clrClear = Color.clear;
        for (int i = 0; i < usersMapSize; i++)
        {
	        // Flip the texture as we convert label map to color array
            int flipIndex = usersMapSize - i - 1;
			
			short userMap = (short)(usersDepthMap[i] & 7);
			short userDepth = (short)(usersDepthMap[i] >> 3);
			
			short nowUserPixel = userMap != 0 ? (short)(0x4000 | userDepth) : userDepth;
			short wasUserPixel = usersPrevState[flipIndex];
			
			// draw only the changed pixels
			if(nowUserPixel != wasUserPixel)
			{
				usersPrevState[flipIndex] = nowUserPixel;
				
	            if (userMap == 0)
	            {
	                usersMapColors[flipIndex] = clrClear;
	            }
	            else
	            {
					if(colorImage != null)
					{
						int x = i % KinectWrapper.Constants.ImageWidth;
						int y = i / KinectWrapper.Constants.ImageWidth;
	
						int cx, cy;
						int hr = KinectWrapper.NuiImageGetColorPixelCoordinatesFromDepthPixelAtResolution(
							KinectWrapper.Constants.ImageResolution,
							KinectWrapper.Constants.ImageResolution,
							ref pcViewArea,
							x, y, usersDepthMap[i],
							out cx, out cy);
						
						if(hr == 0)
						{
							int colorIndex = cx + cy * KinectWrapper.Constants.ImageWidth;
							colorIndex = usersMapSize - colorIndex - 1;
							if(colorIndex >= 0 && colorIndex < usersMapSize)
							{
								Color32 colorPixel = colorImage[colorIndex];
								usersMapColors[flipIndex] = colorPixel;  // new Color(colorPixel.r / 256f, colorPixel.g / 256f, colorPixel.b / 256f, 0.9f);
								usersMapColors[flipIndex].a = 230; // 0.9f
							}
						}
					}
					else
					{
		                // Create a blending color based on the depth histogram
						float histDepth = usersHistogramMap[userDepth];
		                Color c = new Color(histDepth, histDepth, histDepth, 0.9f);
		                
						switch(userMap % 4)
		                {
		                    case 0:
		                        usersMapColors[flipIndex] = Color.red * c;
		                        break;
		                    case 1:
		                        usersMapColors[flipIndex] = Color.green * c;
		                        break;
		                    case 2:
		                        usersMapColors[flipIndex] = Color.blue * c;
		                        break;
		                    case 3:
		                        usersMapColors[flipIndex] = Color.magenta * c;
		                        break;
		                }
					}
	            }
				
			}
        }
		
		// Draw it!
        usersLblTex.SetPixels32(usersMapColors);
        usersLblTex.Apply();
    }
	
	// Update the Color Map
	void UpdateColorMap()
	{
        usersClrTex.SetPixels32(colorImage);
        usersClrTex.Apply();
	}
	
	// Assign UserId to player 1 or 2.
    void CalibrateUser(uint UserId, ref KinectWrapper.NuiSkeletonData skeletonData)
    {
		// If player 1 hasn't been calibrated, assign that UserID to it.
		if(!Player1Calibrated)
		{
			// Check to make sure we don't accidentally assign player 2 to player 1.
			if (!allUsers.Contains(UserId))
			{
				if(CheckForCalibrationPose(UserId, ref Player1CalibrationPose, ref player1CalibrationData, ref skeletonData))
				{
					Player1Calibrated = true;
					Player1ID = UserId;
					
					allUsers.Add(UserId);
					
					foreach(AvatarController controller in Player1Controllers)
					{
						controller.SuccessfulCalibration(UserId);
					}
	
					// add the gestures to detect, if any
					foreach(KinectWrapper.Gestures gesture in Player1Gestures)
					{
						DetectGesture(UserId, gesture);
					}
					
					// reset skeleton filters
					ResetFilters();
					
					// If we're not using 2 users, we're all calibrated.
					//if(!TwoUsers)
					{
						AllPlayersCalibrated = !TwoUsers ? allUsers.Count >= 1 : allUsers.Count >= 2; // true;
					}
				}
			}
		}
		// Otherwise, assign to player 2.
		else if(TwoUsers && !Player2Calibrated)
		{
			if (!allUsers.Contains(UserId))
			{
				if(CheckForCalibrationPose(UserId, ref Player2CalibrationPose, ref player2CalibrationData, ref skeletonData))
				{
					Player2Calibrated = true;
					Player2ID = UserId;
					
					allUsers.Add(UserId);
					
					foreach(AvatarController controller in Player2Controllers)
					{
						controller.SuccessfulCalibration(UserId);
					}
					
					// add the gestures to detect, if any
					foreach(KinectWrapper.Gestures gesture in Player2Gestures)
					{
						DetectGesture(UserId, gesture);
					}
					
					// reset skeleton filters
					ResetFilters();
					
					// All users are calibrated!
					AllPlayersCalibrated = !TwoUsers ? allUsers.Count >= 1 : allUsers.Count >= 2; // true;
				}
			}
		}
		
		// If all users are calibrated, stop trying to find them.
		if(AllPlayersCalibrated)
		{
			Debug.Log("All players calibrated.");
			
			if(CalibrationText != null)
			{
				CalibrationText.guiText.text = "";
			}
		}
    }
	
	// Remove a lost UserId
	void RemoveUser(uint UserId)
	{
		// If we lose player 1...
		if(UserId == Player1ID)
		{
			// Null out the ID and reset all the models associated with that ID.
			Player1ID = 0;
			Player1Calibrated = false;
			
			foreach(AvatarController controller in Player1Controllers)
			{
				controller.RotateToCalibrationPose(UserId, IsCalibrationNeeded());
			}
			
			player1CalibrationData.userId = 0;
		}
		
		// If we lose player 2...
		if(UserId == Player2ID)
		{
			// Null out the ID and reset all the models associated with that ID.
			Player2ID = 0;
			Player2Calibrated = false;
			
			foreach(AvatarController controller in Player2Controllers)
			{
				controller.RotateToCalibrationPose(UserId, IsCalibrationNeeded());
			}
			
			player2CalibrationData.userId = 0;
		}
		
		// clear gestures list for this user
		ClearGestures(UserId);

        // remove from global users list
        allUsers.Remove(UserId);
		AllPlayersCalibrated = !TwoUsers ? allUsers.Count >= 1 : allUsers.Count >= 2; // false;
		
		// Try to replace that user!
		Debug.Log("Waiting for users.");

		if(CalibrationText != null)
		{
			CalibrationText.guiText.text = "WAITING FOR USERS";
		}
	}
	
	// Some internal constants
	private const int stateTracked = (int)KinectWrapper.NuiSkeletonPositionTrackingState.Tracked;
	private const int stateNotTracked = (int)KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked;
	
	private int [] mustBeTrackedJoints = { 
		(int)KinectWrapper.NuiSkeletonPositionIndex.AnkleLeft,
		(int)KinectWrapper.NuiSkeletonPositionIndex.FootLeft,
		(int)KinectWrapper.NuiSkeletonPositionIndex.AnkleRight,
		(int)KinectWrapper.NuiSkeletonPositionIndex.FootRight,
	};
	
	// Process the skeleton data
	void ProcessSkeleton()
	{
		List<uint> lostUsers = new List<uint>();
		lostUsers.AddRange(allUsers);
		
		// calculate the time since last update
		float currentNuiTime = Time.realtimeSinceStartup;
		float deltaNuiTime = currentNuiTime - lastNuiTime;
		
		for (int i = 0; i < KinectWrapper.Constants.NuiSkeletonCount; i++)
		{
			KinectWrapper.NuiSkeletonData skeletonData = skeletonFrame.SkeletonData[i];
			uint userId = skeletonData.dwTrackingID;
			
			if (skeletonData.eTrackingState == KinectWrapper.NuiSkeletonTrackingState.SkeletonTracked)
			{
				if(!AllPlayersCalibrated)
				{
					CalibrateUser(userId, ref skeletonData);
				}

				//// get joints orientations
				//KinectWrapper.NuiSkeletonBoneOrientation[] jointOrients = new KinectWrapper.NuiSkeletonBoneOrientation[(int)KinectWrapper.NuiSkeletonPositionIndex.Count];
				//KinectWrapper.NuiSkeletonCalculateBoneOrientations(ref skeletonData, jointOrients);
				
				// get the skeleton position
				Vector3 skeletonPos = kinectToWorld.MultiplyPoint3x4(skeletonData.Position);
				
				if(userId == Player1ID && skeletonPos.z >= MinUserDistance)
				{
					// get player position
					player1Pos = skeletonPos;
					
					// apply tracking state filter first
					trackingStateFilter[0].UpdateFilter(ref skeletonData);
					
					// fixup skeleton to improve avatar appearance.
					if(UseClippedLegsFilter && clippedLegsFilter[0] != null)
					{
						clippedLegsFilter[0].FilterSkeleton(ref skeletonData, deltaNuiTime);
					}
	
					if(UseSelfIntersectionConstraint && selfIntersectionConstraint != null)
					{
						selfIntersectionConstraint.Constrain(ref skeletonData);
					}
	
					// get joints' position and rotation
					for (int j = 0; j < (int)KinectWrapper.NuiSkeletonPositionIndex.Count; j++)
					{
						bool playerTracked = IgnoreInferredJoints ? (int)skeletonData.eSkeletonPositionTrackingState[j] == stateTracked :
							(Array.BinarySearch(mustBeTrackedJoints, j) >= 0 ? (int)skeletonData.eSkeletonPositionTrackingState[j] == stateTracked :
							(int)skeletonData.eSkeletonPositionTrackingState[j] != stateNotTracked);
						player1JointsTracked[j] = player1PrevTracked[j] && playerTracked;
						player1PrevTracked[j] = playerTracked;
						
						if(player1JointsTracked[j])
						{
							player1JointsPos[j] = kinectToWorld.MultiplyPoint3x4(skeletonData.SkeletonPositions[j]);
							//player1JointsOri[j] = jointOrients[j].absoluteRotation.rotationMatrix * flipMatrix;
						}
						
//						if(j == (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter)
//						{
//							string debugText = String.Format("{0} {1} {2} {3}", (int)skeletonData.eSkeletonPositionTrackingState[j], 
//								player1JointsTracked[j] ? "T" : "F", player1JointsPos[j], skeletonData.SkeletonPositions[j]);
//							
//							Debug.Log(debugText);
//							if(CalibrationText)
//								CalibrationText.guiText.text = debugText;
//						}
					}
					
					// draw the skeleton on top of texture
					if(DisplaySkeletonLines && ComputeUserMap)
					{
						DrawSkeleton(usersLblTex, ref skeletonData, ref player1JointsTracked);
					}
					
					// calculate joint orientations
					KinectWrapper.GetSkeletonJointOrientation(ref player1JointsPos, ref player1JointsTracked, ref player1JointsOri);
					
					// filter orientation constraints
					if(UseBoneOrientationsConstraint && boneConstraintsFilter != null)
					{
						boneConstraintsFilter.Constrain(ref player1JointsOri, ref player1JointsTracked);
					}
					
                    // filter joint orientations.
                    // it should be performed after all joint position modifications.
	                if(UseBoneOrientationsFilter && boneOrientationFilter[0] != null)
	                {
	                    boneOrientationFilter[0].UpdateFilter(ref skeletonData, ref player1JointsOri);
	                }
	
					// get player rotation
					player1Ori = player1JointsOri[(int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter];
					
					// check for gestures
					if(Time.realtimeSinceStartup >= gestureTrackingAtTime[0])
					{
						int listGestureSize = player1Gestures.Count;
						float timestampNow = Time.realtimeSinceStartup;
						
						for(int g = 0; g < listGestureSize; g++)
						{
							KinectWrapper.GestureData gestureData = player1Gestures[g];
							
							if((timestampNow >= gestureData.startTrackingAtTime) && 
								!IsConflictingGestureInProgress(gestureData))
							{
								KinectWrapper.CheckForGesture(userId, ref gestureData, Time.realtimeSinceStartup, 
									ref player1JointsPos, ref player1JointsTracked);
								player1Gestures[g] = gestureData;

								if(gestureData.complete)
								{
									gestureTrackingAtTime[0] = timestampNow + MinTimeBetweenGestures;
								}
							}
						}
					}
				}
				else if(userId == Player2ID && skeletonPos.z >= MinUserDistance)
				{ 
					// get player position
					player2Pos = skeletonPos;
					
					// apply tracking state filter first
					trackingStateFilter[1].UpdateFilter(ref skeletonData);
					
					// fixup skeleton to improve avatar appearance.
					if(UseClippedLegsFilter && clippedLegsFilter[1] != null)
					{
						clippedLegsFilter[1].FilterSkeleton(ref skeletonData, deltaNuiTime);
					}
	
					if(UseSelfIntersectionConstraint && selfIntersectionConstraint != null)
					{
						selfIntersectionConstraint.Constrain(ref skeletonData);
					}

					// get joints' position and rotation
					for (int j = 0; j < (int)KinectWrapper.NuiSkeletonPositionIndex.Count; j++)
					{
						bool playerTracked = IgnoreInferredJoints ? (int)skeletonData.eSkeletonPositionTrackingState[j] == stateTracked :
							(Array.BinarySearch(mustBeTrackedJoints, j) >= 0 ? (int)skeletonData.eSkeletonPositionTrackingState[j] == stateTracked :
							(int)skeletonData.eSkeletonPositionTrackingState[j] != stateNotTracked);
						player2JointsTracked[j] = player2PrevTracked[j] && playerTracked;
						player2PrevTracked[j] = playerTracked;
						
						if(player2JointsTracked[j])
						{
							player2JointsPos[j] = kinectToWorld.MultiplyPoint3x4(skeletonData.SkeletonPositions[j]);
						}
					}
					
					// draw the skeleton on top of texture
					if(DisplaySkeletonLines && ComputeUserMap)
					{
						DrawSkeleton(usersLblTex, ref skeletonData, ref player2JointsTracked);
					}
					
					// calculate joint orientations
					KinectWrapper.GetSkeletonJointOrientation(ref player2JointsPos, ref player2JointsTracked, ref player2JointsOri);
					
					// filter orientation constraints
					if(UseBoneOrientationsConstraint && boneConstraintsFilter != null)
					{
						boneConstraintsFilter.Constrain(ref player2JointsOri, ref player2JointsTracked);
					}
					
                    // filter joint orientations.
                    // it should be performed after all joint position modifications.
	                if(UseBoneOrientationsFilter && boneOrientationFilter[1] != null)
	                {
	                    boneOrientationFilter[1].UpdateFilter(ref skeletonData, ref player2JointsOri);
	                }
	
					// get player rotation
					player2Ori = player2JointsOri[(int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter];
					
					// check for gestures
					if(Time.realtimeSinceStartup >= gestureTrackingAtTime[1])
					{
						int listGestureSize = player2Gestures.Count;
						float timestampNow = Time.realtimeSinceStartup;
						
						for(int g = 0; g < listGestureSize; g++)
						{
							KinectWrapper.GestureData gestureData = player2Gestures[g];
							
							if((timestampNow >= gestureData.startTrackingAtTime) &&
								!IsConflictingGestureInProgress(gestureData))
							{
								KinectWrapper.CheckForGesture(userId, ref gestureData, Time.realtimeSinceStartup, 
									ref player2JointsPos, ref player2JointsTracked);
								player2Gestures[g] = gestureData;

								if(gestureData.complete)
								{
									gestureTrackingAtTime[1] = timestampNow + MinTimeBetweenGestures;
								}
							}
						}
					}
				}
				
				lostUsers.Remove(userId);
			}
		}
		
		// update the nui-timer
		lastNuiTime = currentNuiTime;
		
		// remove the lost users if any
		if(lostUsers.Count > 0)
		{
			foreach(uint userId in lostUsers)
			{
				RemoveUser(userId);
			}
			
			lostUsers.Clear();
		}
	}
	
	// draws the skeleton in the given texture
	private void DrawSkeleton(Texture2D aTexture, ref KinectWrapper.NuiSkeletonData skeletonData, ref bool[] playerJointsTracked)
	{
		int jointsCount = (int)KinectWrapper.NuiSkeletonPositionIndex.Count;
		
		for(int i = 0; i < jointsCount; i++)
		{
			int parent = KinectWrapper.GetSkeletonJointParent(i);
			
			if(playerJointsTracked[i] && playerJointsTracked[parent])
			{
				Vector3 posParent = KinectWrapper.MapSkeletonPointToDepthPoint(skeletonData.SkeletonPositions[parent]);
				Vector3 posJoint = KinectWrapper.MapSkeletonPointToDepthPoint(skeletonData.SkeletonPositions[i]);
				
				posParent.y = KinectWrapper.Constants.ImageHeight - posParent.y - 1;
				posJoint.y = KinectWrapper.Constants.ImageHeight - posJoint.y - 1;
				posParent.x = KinectWrapper.Constants.ImageWidth - posParent.x - 1;
				posJoint.x = KinectWrapper.Constants.ImageWidth - posJoint.x - 1;
				
				//Color lineColor = playerJointsTracked[i] && playerJointsTracked[parent] ? Color.red : Color.yellow;
				DrawLine(aTexture, (int)posParent.x, (int)posParent.y, (int)posJoint.x, (int)posJoint.y, Color.yellow);
			}
		}
		
		aTexture.Apply();
	}
	
	// draws a line in a texture
	private void DrawLine(Texture2D a_Texture, int x1, int y1, int x2, int y2, Color a_Color)
	{
		int width = KinectWrapper.Constants.ImageWidth;
		int height = KinectWrapper.Constants.ImageHeight;
		
		int dy = y2 - y1;
		int dx = x2 - x1;
	 
		int stepy = 1;
		if (dy < 0) 
		{
			dy = -dy; 
			stepy = -1;
		}
		
		int stepx = 1;
		if (dx < 0) 
		{
			dx = -dx; 
			stepx = -1;
		}
		
		dy <<= 1;
		dx <<= 1;
	 
		if(x1 >= 0 && x1 < width && y1 >= 0 && y1 < height)
			for(int x = -1; x <= 1; x++)
				for(int y = -1; y <= 1; y++)
					a_Texture.SetPixel(x1 + x, y1 + y, a_Color);
		
		if (dx > dy) 
		{
			int fraction = dy - (dx >> 1);
			
			while (x1 != x2) 
			{
				if (fraction >= 0) 
				{
					y1 += stepy;
					fraction -= dx;
				}
				
				x1 += stepx;
				fraction += dy;
				
				if(x1 >= 0 && x1 < width && y1 >= 0 && y1 < height)
					for(int x = -1; x <= 1; x++)
						for(int y = -1; y <= 1; y++)
							a_Texture.SetPixel(x1 + x, y1 + y, a_Color);
			}
		}
		else 
		{
			int fraction = dx - (dy >> 1);
			
			while (y1 != y2) 
			{
				if (fraction >= 0) 
				{
					x1 += stepx;
					fraction -= dy;
				}
				
				y1 += stepy;
				fraction += dx;
				
				if(x1 >= 0 && x1 < width && y1 >= 0 && y1 < height)
					for(int x = -1; x <= 1; x++)
						for(int y = -1; y <= 1; y++)
							a_Texture.SetPixel(x1 + x, y1 + y, a_Color);
			}
		}
		
	}
	
	// convert the matrix to quaternion, taking care of the mirroring
	private Quaternion ConvertMatrixToQuat(Matrix4x4 mOrient, int joint, bool flip)
	{
		Vector4 vZ = mOrient.GetColumn(2);
		Vector4 vY = mOrient.GetColumn(1);

		if(!flip)
		{
			vZ.y = -vZ.y;
			vY.x = -vY.x;
			vY.z = -vY.z;
		}
		else
		{
			vZ.x = -vZ.x;
			vZ.y = -vZ.y;
			vY.z = -vY.z;
		}
		
		if(vZ.x != 0.0f || vZ.y != 0.0f || vZ.z != 0.0f)
			return Quaternion.LookRotation(vZ, vY);
		else
			return Quaternion.identity;
	}
	
	// return the index of gesture in the list, or -1 if not found
	private int GetGestureIndex(uint UserId, KinectWrapper.Gestures gesture)
	{
		if(UserId == Player1ID)
		{
			int listSize = player1Gestures.Count;
			for(int i = 0; i < listSize; i++)
			{
				if(player1Gestures[i].gesture == gesture)
					return i;
			}
		}
		else if(UserId == Player2ID)
		{
			int listSize = player2Gestures.Count;
			for(int i = 0; i < listSize; i++)
			{
				if(player2Gestures[i].gesture == gesture)
					return i;
			}
		}
		
		return -1;
	}
	
	private bool IsConflictingGestureInProgress(KinectWrapper.GestureData gestureData)
	{
		foreach(KinectWrapper.Gestures gesture in gestureData.checkForGestures)
		{
			int index = GetGestureIndex(gestureData.userId, gesture);
			
			if(index >= 0)
			{
				if(gestureData.userId == Player1ID)
				{
					if(player1Gestures[index].progress > 0f)
						return true;
				}
				else if(gestureData.userId == Player2ID)
				{
					if(player2Gestures[index].progress > 0f)
						return true;
				}
			}
		}
		
		return false;
	}
	
	// check if the calibration pose is complete for given user
	private bool CheckForCalibrationPose(uint userId, ref KinectWrapper.Gestures calibrationGesture, 
		ref KinectWrapper.GestureData gestureData, ref KinectWrapper.NuiSkeletonData skeletonData)
	{
		if(calibrationGesture == KinectWrapper.Gestures.None)
			return true;
		
		// init gesture data if needed
		if(gestureData.userId == 0)
		{
			gestureData.userId = userId;
			gestureData.gesture = calibrationGesture;
			gestureData.state = 0;
			gestureData.joint = 0;
			gestureData.progress = 0f;
			gestureData.complete = false;
			gestureData.cancelled = false;
		}
		
		// get temporary joints' position
		int skeletonJointsCount = (int)KinectWrapper.NuiSkeletonPositionIndex.Count;
		bool[] jointsTracked = new bool[skeletonJointsCount];
		Vector3[] jointsPos = new Vector3[skeletonJointsCount];

		int stateTracked = (int)KinectWrapper.NuiSkeletonPositionTrackingState.Tracked;
		int stateNotTracked = (int)KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked;
		
		int [] mustBeTrackedJoints = { 
			(int)KinectWrapper.NuiSkeletonPositionIndex.AnkleLeft,
			(int)KinectWrapper.NuiSkeletonPositionIndex.FootLeft,
			(int)KinectWrapper.NuiSkeletonPositionIndex.AnkleRight,
			(int)KinectWrapper.NuiSkeletonPositionIndex.FootRight,
		};
		
		for (int j = 0; j < skeletonJointsCount; j++)
		{
			jointsTracked[j] = Array.BinarySearch(mustBeTrackedJoints, j) >= 0 ? (int)skeletonData.eSkeletonPositionTrackingState[j] == stateTracked :
				(int)skeletonData.eSkeletonPositionTrackingState[j] != stateNotTracked;
			
			if(jointsTracked[j])
			{
				jointsPos[j] = kinectToWorld.MultiplyPoint3x4(skeletonData.SkeletonPositions[j]);
			}
		}
		
		// estimate the gesture progess
		KinectWrapper.CheckForGesture(userId, ref gestureData, Time.realtimeSinceStartup, 
			ref jointsPos, ref jointsTracked);
		
		// check if gesture is complete
		if(gestureData.complete)
		{
			gestureData.userId = 0;
			return true;
		}
		
		return false;
	}
	
}


