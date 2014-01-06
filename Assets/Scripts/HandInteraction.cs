using UnityEngine;
using System.Collections;

public class HandInteraction : MonoBehaviour
{	
		private InteractionManager manager;
		public bool isLeftHandGripped = false;
		public bool isLeftHandReleased = false;
		public bool isRightHandGripped = false;
		public bool isRightHandReleased = false;
	
		void Awake ()
		{
				manager = GameObject.Find ("Player").GetComponent<InteractionManager> ();
		}
	
		void Update ()
		{
				if (manager != null && manager.IsInteractionInited ()) {
						// check for left hand grip
						if (manager.GetLeftHandEvent () == InteractionWrapper.InteractionHandEventType.Grip) {
								isLeftHandGripped = true;
								isLeftHandReleased = false;
								//Debug.Log ("Left Hand Gripped");
								GameObject.Find ("LeftHand").animation.Play ("LeftHandGrip");
						} else if (manager.GetLeftHandEvent () == InteractionWrapper.InteractionHandEventType.Release) {
								isLeftHandGripped = false;
								isLeftHandReleased = true;
								//Debug.Log ("Left Hand Released");
								GameObject.Find ("LeftHand").animation.Play ("LeftHandRelease");
						}
						//check for right hand grip
						if (manager.GetRightHandEvent () == InteractionWrapper.InteractionHandEventType.Grip) {
								isRightHandGripped = true;
								isRightHandReleased = false;
								//Debug.Log ("Right Hand Gripped");
								GameObject.Find ("RightHand").animation.Play ("RightHandGrip");
						} else if (manager.GetRightHandEvent () == InteractionWrapper.InteractionHandEventType.Release) {
								isRightHandGripped = false;
								isRightHandReleased = true;
								//Debug.Log ("Right Hand Released");
								GameObject.Find ("RightHand").animation.Play ("RightHandRelease");
						}
				}
		}

}