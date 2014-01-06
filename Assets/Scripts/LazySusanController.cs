using UnityEngine;
using System;
using System.Collections;

public class LazySusanController : MonoBehaviour {
	private float mouseX = 0;
	private int screenWidth = 0;
	private Vector3 angle = Vector3.zero;
	// Use this for initialization
	void Start () {
		screenWidth = Screen.width;
		//e = Event.current;
		//Screen.lockCursor = true;
	}
	
	// Update is called once per frame
	void Update () {
		//Debug.Log(Event.current.mousePosition.x);
		//EventType.MouseMove e = EventType.MouseMove.current;
		 
		/*
		if(Event.current != null && Event.current.type == EventType.MouseMove){
			Debug.Log(Event.current.delta);
			Debug.Log("ok");
		}
		*/
		
		mouseX = Input.mousePosition.x;
		
		angle.y = 360.0f * mouseX/ screenWidth;
		
		this.gameObject.transform.localEulerAngles = angle;
		
		//Debug.Log(mouseX + " " + angle);
	}

	/*
	void OnGUI() {
        Event e = Event.current;
        if (e.isMouse)
            //Debug.Log("Detected a mouse event!");
		
		GUI.Label(new Rect(10, Screen.height-100, 500, 100), "Detected a mouse event!");
        
    }*/
	
}