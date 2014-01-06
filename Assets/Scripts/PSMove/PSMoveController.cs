using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class PSMoveController : MonoBehaviour 
{
	// We save a list of Move controllers.
	List<UniMoveController> moves = new List<UniMoveController>();
	
	
	private GameObject handPoint;
	private Color orig;
	private bool isGripped = false;
	public AudioSource audioSource;
	public AudioClip audioClip;
	public Vector3 psmoveVec = new Vector3(0, 1.5f, 1.5f);
	private bool childrenAttached = false;
	
	void Start() 
	{
		this.transform.parent = GameObject.Find ("RightHandMiddle4").transform;
		this.transform.localPosition = Vector3.zero;
		Time.maximumDeltaTime = 0.1f;
		audioSource = (AudioSource)gameObject.AddComponent("AudioSource");
		audioSource.clip = audioClip;
		audioSource.loop = false;

		handPoint = GameObject.Find("PSMoveController");
		//handPoint.transform.position = GameObject.Find("23_Hand_Right").transform.position + psmoveVec;
		//handPoint.transform.parent = GameObject.Find("23_Hand_Right").transform;
		//handPoint.rigidbody.isKinematic = true;

		
		int count = UniMoveController.GetNumConnected();
		
		// Iterate through all connections (USB and Bluetooth)
		for (int i = 0; i < count; i++) 
		{
			UniMoveController move = gameObject.AddComponent<UniMoveController>();	// It's a MonoBehaviour, so we can't just call a constructor
			
			// Remember to initialize!
			if (!move.Init(i)) 
			{	
				Destroy(move);	// If it failed to initialize, destroy and continue on
				continue;
			}
					
			// This example program only uses Bluetooth-connected controllers
			PSMoveConnectionType conn = move.ConnectionType;
			if (conn == PSMoveConnectionType.Unknown || conn == PSMoveConnectionType.USB) 
			{
				Destroy(move);
			}
			else 
			{
				moves.Add(move);
				
				move.OnControllerDisconnected += HandleControllerDisconnected;
				
				// Start all controllers with a white LED
				move.SetLED(Color.white);
				Debug.Log(count);
			}
		}
		
		orig = handPoint.renderer.material.color;
		moves[0].SetLED(Color.red);
		
		
	}

	
	void Update() 
	{
		if(moves[0].Trigger > 0.0f){
			Color c = handPoint.renderer.material.color;
			c.b = 0.5f;
			c.g = 0.5f;
			handPoint.renderer.material.color = c;
			handPoint.collider.isTrigger = true;
			
		}
		else{
				moves[0].SetRumble(0.0f);
				handPoint.renderer.material.color = orig;
				handPoint.collider.isTrigger = false;
				this.gameObject.transform.DetachChildren();
				isGripped = false;
				childrenAttached = false;
			}
			
	}
	
	void OnCollisionStay(Collision other){
		if(other.gameObject.transform.tag == "cube" && moves[0].Trigger > 0.0f){//moves[0].Trigger > 0.0f){
			isGripped = true;
			audioSource.Play();
			moves[0].SetRumble(moves[0].Trigger);
		}
		/*
		else{
			isGripped = false;
			this.gameObject.transform.DetachChildren();
			childrenAttached = false;
		}
		*/
		
		if(isGripped && !childrenAttached){
			other.gameObject.transform.parent = this.gameObject.transform;
			childrenAttached = true;
		}
		
	}
	
	
	void HandleControllerDisconnected (object sender, EventArgs e)
	{
		// TODO: Remove this disconnected controller from the list and maybe give an update to the player
	}
	
	void OnGUI() 
	{
        string display = "";
        
		if (moves.Count > 0) 
		{
            for (int i = 0; i < moves.Count; i++) 
			{
                display += string.Format("PS Move {0}: ax:{1:0.000}, ay:{2:0.000}, az:{3:0.000} gx:{4:0.000}, gy:{5:0.000}, gz:{6:0.000}\n", 
					i+1, moves[i].Acceleration.x, moves[i].Acceleration.y, moves[i].Acceleration.z,
					moves[i].Gyro.x, moves[i].Gyro.y, moves[i].Gyro.z);
            }
        }
        else display = "No Bluetooth-connected controllers found. Make sure one or more are both paired and connected to this computer.";

        //GUI.Label(new Rect(10, Screen.height-100, 500, 100), display);
    }
}
