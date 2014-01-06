using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class FacetrackingManager : MonoBehaviour 
{
	// Public Bool to determine whether to receive and compute the color map
	public bool ComputeColorMap = false;
	
	// Public Bool to determine whether to display color map on the GUI
	public bool DisplayColorMap = false;
	
	// Public Bool to determine whether to visualize facetracker lines on the GUI
	public bool VisualizeFacetracker = false;
	
	// GUI Text to show messages.
	public GameObject debugText;

	// Is currently tracking
	private bool isTracking = false;
	
	// Are shape units converged
	private bool isConverged = false;
	
	// Animation units
	private float[] afAU = null;
	private bool bGotAU = false;
	private string sAuDebug = string.Empty;

	// Shape units
	private float[] afSU = null;
	private bool bGotSU = false;
	
	// Points to visualize (each point consists of 2 elements - for its X and Y)
	private float[] avPointsXY = null;
	private Vector2[] avPoints = null;
	private bool bGotPoints = false;
	
	// Head position and rotation
	private Vector3 headPos = Vector3.zero;
	private Quaternion headRot = Quaternion.identity;
	
	// Bool to keep track of whether Kinect and FT-library have been initialized
	private bool facetrackingInitialized = false;
	
	// The single instance of FacetrackingManager
	private static FacetrackingManager instance;
	
	// Color image data, if used
	private Texture2D usersClrTex;
	private Rect usersClrRect;
	private Color32[] colorImage;
	private byte[] videoBuffer;
	
	
	// returns the single FacetrackingManager instance
    public static FacetrackingManager Instance
    {
        get
        {
            return instance;
        }
    }
	
	// returns true if SAPI is successfully initialized, false otherwise
	public bool IsFacetrackingInitialized()
	{
		return facetrackingInitialized;
	}
	
	// returns true if the facetracking library is tracking a face at the moment
	public bool IsTracking()
	{
		return isTracking;
	}
	
	// returns the color image texture,if ComputeColorMap is true
    public Texture2D GetUsersClrTex()
    { 
		return usersClrTex;
	}
	
	// returns the tracked head position
	public Vector3 GetHeadPosition()
	{
		return headPos;
	}
	
	// returns the tracked head rotation
	public Quaternion GetHeadRotation()
	{
		return headRot;
	}
	
	// returns animation units count, or 0 if no face has been tracked
	public int GetAnimUnitsCount()
	{
		if(afAU != null)
		{
			return afAU.Length;
		}
		
		return 0;
	}
	
	// returns the animation unit at given index, or 0 if the index is invalid
	public float GetAnimUnit(int index)
	{
		if(afAU != null && index >= 0 && index < afAU.Length)
		{
			return afAU[index];
		}
		
		return 0.0f;
	}
	
	// returns shape units count, or 0 if no face has been tracked
	public int GetShapeUnitsCount()
	{
		if(afSU != null)
		{
			return afSU.Length;
		}
		
		return 0;
	}
	
	// returns the shape unit at given index, or 0 if the index is invalid
	public float GetShapeUnit(int index)
	{
		if(afSU != null && index >= 0 && index < afSU.Length)
		{
			return afSU[index];
		}
		
		return 0.0f;
	}
	
	// returns true if shape is converged, false otherwise
	public bool IsShapeConverged()
	{
		return isConverged;
	}
	
	
	//----------------------------------- end of public functions --------------------------------------//
	
	
	void Awake() 
	{
		//debugText = GameObject.Find("DebugText");
		
		// ensure the needed dlls are in place
		if(FacetrackingWrapper.CheckSpeechWrapperPresence())
		{
			// reload the same level
			Application.LoadLevel(Application.loadedLevel);
		}
	}
	
	void StartFacetracker() 
	{
		try 
		{
			if(debugText != null)
				debugText.guiText.text = "Please, wait...";
			
			// initialize Kinect sensor as needed
			int rc = FacetrackingWrapper.InitKinectSensor();
			if(rc != 0)
			{
				throw new Exception("Initialization of Kinect sensor failed");
			}
			
			// Initialize the kinect speech wrapper
			rc = FacetrackingWrapper.InitFaceTracking();
	        if (rc < 0)
	        {
	            throw new Exception(String.Format("Error initializing Kinect/FT: hr=0x{0:X}", rc));
	        }
			
			if(ComputeColorMap)
			{
				// Initialize color map related stuff
		        usersClrTex = new Texture2D(FacetrackingWrapper.GetImageWidth(), FacetrackingWrapper.GetImageHeight());
		        usersClrRect = new Rect(Screen.width, Screen.height - usersClrTex.height, -usersClrTex.width, usersClrTex.height);
				
				colorImage = new Color32[FacetrackingWrapper.GetImageWidth() * FacetrackingWrapper.GetImageHeight()];
				videoBuffer = new byte[FacetrackingWrapper.GetImageWidth() * FacetrackingWrapper.GetImageHeight() * 4];
			}
			
			instance = this;
			facetrackingInitialized = true;
			
			DontDestroyOnLoad(gameObject);

			if(debugText != null)
				debugText.guiText.text = "Ready.";
		} 
		catch(DllNotFoundException ex)
		{
			Debug.LogError(ex.ToString());
			if(debugText != null)
				debugText.guiText.text = "Please check the Kinect and FT-Library installations.";
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
		FacetrackingWrapper.FinishFaceTracking();
		FacetrackingWrapper.ShutdownKinectSensor();
		
		facetrackingInitialized = false;
		instance = null;
	}
	
	void Update() 
	{
		// start Kinect face tracker as needed
		if(!facetrackingInitialized)
		{
			StartFacetracker();
			
			if(!facetrackingInitialized)
			{
				Application.Quit();
				return;
			}
		}
		
		if(facetrackingInitialized)
		{
			// update the face tracker
			int rc = FacetrackingWrapper.UpdateFaceTracking();
			
			if(rc >= 0)
			{
				// poll the video frame as needed
				if(ComputeColorMap)
				{
					//float fTimeNow2 = Time.realtimeSinceStartup;
					
					if(FacetrackingWrapper.PollVideo(ref videoBuffer, ref colorImage))
					{
				        usersClrTex.SetPixels32(colorImage);
				        usersClrTex.Apply();
					}
			
					//fTimeNow2 = Time.realtimeSinceStartup - fTimeNow2;
					//Debug.Log("PollVideo() took " + fTimeNow2 + " s.");
				}
				
				// estimate the tracking state
				isTracking = FacetrackingWrapper.IsFaceTracked();

				// get the facetracking parameters
				if(isTracking)
				{
					//float fTimeNow = Time.realtimeSinceStartup;
					
					// get head position and rotation
					Vector4 vHeadPos = Vector4.zero, vHeadRot = Vector4.zero;
					if(FacetrackingWrapper.GetHeadPosition(ref vHeadPos))
					{
						headPos = (Vector3)vHeadPos;
					}
					
					if(FacetrackingWrapper.GetHeadRotation(ref vHeadRot))
					{
						vHeadRot.x = -vHeadRot.x;
						vHeadRot.z = -vHeadRot.z;
						
						headRot = Quaternion.Euler((Vector3)vHeadRot);
					}
					
					// get the animation units
					int iNumAU = FacetrackingWrapper.GetAnimUnitsCount();
					bGotAU = false;
					
					if(iNumAU > 0)
					{
						if(afAU == null)
						{
							afAU = new float[iNumAU];
						}
						
						bGotAU = FacetrackingWrapper.GetAnimUnits(afAU, ref iNumAU);
					}
					
//					// debug anim. units
//					sAuDebug = "AU: ";
//					for(int i = 0; i < iNumAU; i++)
//					{
//						sAuDebug += String.Format("{0}:{1:F2} ", i, afAU[i]);
//					}
					
					// get the shape units
					isConverged = FacetrackingWrapper.IsShapeConverged();
					int iNumSU = FacetrackingWrapper.GetShapeUnitsCount();
					bGotSU = false;
					
					if(iNumSU > 0)
					{
						if(afSU == null)
						{
							afSU = new float[iNumSU];
						}
						
						bGotSU = FacetrackingWrapper.GetShapeUnits(afSU, ref iNumSU);
					}
					
					// get the shape points
					int iNumPoints = FacetrackingWrapper.GetShapePointsCount();
					bGotPoints = false;
					
					if(iNumPoints > 0)
					{
						int iNumPointsXY = iNumPoints << 1;

						if(avPointsXY == null)
						{
							avPointsXY = new float[iNumPointsXY];
							avPoints = new Vector2[iNumPoints];
						}
						
						bGotPoints = FacetrackingWrapper.GetShapePoints(avPointsXY, ref iNumPointsXY);

						if(bGotPoints)
						{
							for(int i = 0; i < iNumPoints; i++)
							{
								int iXY = i << 1;
								
								avPoints[i].x = avPointsXY[iXY];
								avPoints[i].y = avPointsXY[iXY + 1];
							}
						}
					}
						
					//fTimeNow = Time.realtimeSinceStartup - fTimeNow;
					//Debug.Log("GetAU/SU/Points() took " + fTimeNow + " s.");
					
					if(ComputeColorMap && VisualizeFacetracker)
					{
						//fTimeNow = Time.realtimeSinceStartup;
						
						DrawFacetrackerLines(usersClrTex, avPoints);
						
						//fTimeNow = Time.realtimeSinceStartup - fTimeNow;
						//Debug.Log("DrawFacetrackerLines() took " + fTimeNow + " s.");
					}
				}
			}
		}
	}
	
	void OnGUI()
	{
		if(facetrackingInitialized)
		{
			if(ComputeColorMap && DisplayColorMap)
			{
//				float fTimeNow = Time.realtimeSinceStartup;
				
				GUI.DrawTexture(usersClrRect, usersClrTex);
				
//				fTimeNow = Time.realtimeSinceStartup - fTimeNow;
//				Debug.Log("DrawTexture() took " + fTimeNow + " s.");
			}
			
			if(debugText != null)
			{
				if(isTracking)
					debugText.guiText.text = "Tracking... " + sAuDebug;
				else
					debugText.guiText.text = "Not tracking...";
			}
		}
	}
	
	// visualizes the tracked face lines
	private void DrawFacetrackerLines(Texture2D aTexture, Vector2[] avPoints)
	{
		if(avPoints == null || avPoints.Length < 87)
			return;
		
		Color color = Color.yellow;
		
	    for (int ipt = 0; ipt < 8; ++ipt)
	    {
	        Vector2 ptStart = avPoints[ipt];
	        Vector2 ptEnd = avPoints[(ipt+1)%8];
			
	        DrawLine(aTexture, ptStart, ptEnd, color);
	    }
	
	    for (int ipt = 8; ipt < 16; ++ipt)
	    {
	        Vector2 ptStart = avPoints[ipt];
	        Vector2 ptEnd = avPoints[(ipt - 8 + 1) % 8 + 8];
			
	        DrawLine(aTexture, ptStart, ptEnd, color);
	    }
	
	    for (int ipt = 16; ipt < 26; ++ipt)
	    {
	        Vector2 ptStart = avPoints[ipt];
	        Vector2 ptEnd = avPoints[(ipt - 16 + 1) % 10 + 16];
			
	        DrawLine(aTexture, ptStart, ptEnd, color);
	    }
	
	    for (int ipt = 26; ipt < 36; ++ipt)
	    {
	        Vector2 ptStart = avPoints[ipt];
	        Vector2 ptEnd = avPoints[(ipt - 26 + 1) % 10 + 26];
			
	        DrawLine(aTexture, ptStart, ptEnd, color);
	    }
	
	    for (int ipt = 36; ipt < 47; ++ipt)
	    {
	        Vector2 ptStart = avPoints[ipt];
	        Vector2 ptEnd = avPoints[ipt + 1];
			
	        DrawLine(aTexture, ptStart, ptEnd, color);
	    }
	
	    for (int ipt = 48; ipt < 60; ++ipt)
	    {
	        Vector2 ptStart = avPoints[ipt];
	        Vector2 ptEnd = avPoints[(ipt - 48 + 1) % 12 + 48];
			
	        DrawLine(aTexture, ptStart, ptEnd, color);
	    }
	
	    for (int ipt = 60; ipt < 68; ++ipt)
	    {
	        Vector2 ptStart = avPoints[ipt];
	        Vector2 ptEnd = avPoints[(ipt - 60 + 1) % 8 + 60];
			
	        DrawLine(aTexture, ptStart, ptEnd, color);
	    }
	
	    for (int ipt = 68; ipt < 86; ++ipt)
	    {
	        Vector2 ptStart = avPoints[ipt];
	        Vector2 ptEnd = avPoints[ipt + 1];
			
	        DrawLine(aTexture, ptStart, ptEnd, color);
	    }
		
		aTexture.Apply();
	}
	
	// draws a line in a texture
	private void DrawLine(Texture2D a_Texture, Vector2 ptStart, Vector2 ptEnd, Color a_Color)
	{
		int width = FacetrackingWrapper.Constants.ImageWidth;
		int height = FacetrackingWrapper.Constants.ImageHeight;
		
		DrawLine(a_Texture, width - (int)ptStart.x, height - (int)ptStart.y, 
					width - (int)ptEnd.x, height - (int)ptEnd.y, a_Color, width, height);
	}
	
	// draws a line in a texture
	private void DrawLine(Texture2D a_Texture, int x1, int y1, int x2, int y2, Color a_Color, int width, int height)
	{
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
			a_Texture.SetPixel(x1, y1, a_Color);
//			for(int x = -1; x <= 1; x++)
//				for(int y = -1; y <= 1; y++)
//					a_Texture.SetPixel(x1 + x, y1 + y, a_Color);
		
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
					a_Texture.SetPixel(x1, y1, a_Color);
//					for(int x = -1; x <= 1; x++)
//						for(int y = -1; y <= 1; y++)
//							a_Texture.SetPixel(x1 + x, y1 + y, a_Color);
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
					a_Texture.SetPixel(x1, y1, a_Color);
//					for(int x = -1; x <= 1; x++)
//						for(int y = -1; y <= 1; y++)
//							a_Texture.SetPixel(x1 + x, y1 + y, a_Color);
			}
		}
		
	}
	
	
}
