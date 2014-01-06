using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ModelFaceController : MonoBehaviour 
{
	public enum AxisEnum { X, Y, Z };
	
	// Head
	public Transform Head;
	public bool Mirrored = true;
	
	// Upper Lip Left
	public Transform UpperLipLeft;
	public AxisEnum UpperLipLeftAxis;
	public float UpperLipLeftUp;

	// Upper Lip Right
	public Transform UpperLipRight;
	public AxisEnum UpperLipRightAxis;
	public float UpperLipRightUp;

	// Jaw
	public Transform Jaw;
	public AxisEnum JawAxis;
	public float JawDown;
	
	// Lip Left
	public Transform LipLeft;
	public AxisEnum LipLeftAxis;
	public float LipLeftStretched;

	// Lip Right
	public Transform LipRight;
	public AxisEnum LipRightAxis;
	public float LipRightStretched;

	// Eyebrow Left
	public Transform EyebrowLeft;
	public AxisEnum EyebrowLeftAxis;
	public float EyebrowLeftLowered;

	// Eyebrow Right
	public Transform EyebrowRight;
	public AxisEnum EyebrowRightAxis;
	public float EyebrowRightLowered;
	
	// Lip Corner Left
	public Transform LipCornerLeft;
	public AxisEnum LipCornerLeftAxis;
	public float LipCornerLeftDepressed;

	// Lip Corner Right
	public Transform LipCornerRight;
	public AxisEnum LipCornerRightAxis;
	public float LipCornerRightDepressed;

	// Outer Brow Left
	public Transform OuterBrowLeft;
	public AxisEnum OuterBrowLeftAxis;
	public float OuterBrowLeftRaised;

	// Outer Brow Right
	public Transform OuterBrowRight;
	public AxisEnum OuterBrowRightAxis;
	public float OuterBrowRightRaised;
	
	
	private FacetrackingManager manager;
	
	private Vector3 HeadInitialPosition;
	private Quaternion HeadInitialRotation;
	
	private float UpperLipLeftNeutral;
	private float UpperLipRightNeutral;
	private float JawNeutral;
	private float LipLeftNeutral;
	private float LipRightNeutral;
	private float EyebrowLeftNeutral;
	private float EyebrowRightNeutral;
	private float LipCornerLeftNeutral;
	private float LipCornerRightNeutral;
	private float OuterBrowLeftNeutral;
	private float OuterBrowRightNeutral;

	
	void Awake() 
	{
		manager = Camera.main.GetComponent<FacetrackingManager>();
	}
	
	void Start()
	{
		if(Head != null)
		{
			HeadInitialPosition = Head.localPosition;
			HeadInitialPosition.z = 0;
			HeadInitialRotation = Head.localRotation;
		}
		
		UpperLipLeftNeutral = GetJointRotation(UpperLipLeft, UpperLipLeftAxis);
		UpperLipRightNeutral = GetJointRotation(UpperLipRight, UpperLipRightAxis);
		
		JawNeutral = GetJointRotation(Jaw, JawAxis);
		
		LipLeftNeutral = GetJointRotation(LipLeft, LipLeftAxis);
		LipRightNeutral = GetJointRotation(LipRight, LipRightAxis);
		
		EyebrowLeftNeutral = GetJointRotation(EyebrowLeft, EyebrowLeftAxis);
		EyebrowRightNeutral = GetJointRotation(EyebrowRight, EyebrowRightAxis);
		
		LipCornerLeftNeutral = GetJointRotation(LipCornerLeft, LipCornerLeftAxis);
		LipCornerRightNeutral = GetJointRotation(LipCornerRight, LipCornerRightAxis);
		
		OuterBrowLeftNeutral = GetJointRotation(OuterBrowLeft, OuterBrowLeftAxis);
		OuterBrowRightNeutral = GetJointRotation(OuterBrowRight, OuterBrowRightAxis);
	}
	
	void Update() 
	{
		if(manager && manager.IsTracking())
		{
			// set head position & rotation
			if(Head != null)
			{
				Vector3 newPosition = HeadInitialPosition + manager.GetHeadPosition();
				Head.localPosition = Vector3.Lerp(Head.localPosition, newPosition, 3 * Time.deltaTime);
				
				Quaternion newRotation = HeadInitialRotation * manager.GetHeadRotation();
				Head.localRotation = Quaternion.Slerp(Head.localRotation, newRotation, 3 * Time.deltaTime);
			}
			
			// get animation units
			int iAUCount = manager.GetAnimUnitsCount();
			
			if(iAUCount >= 6)
			{
				// AU0 - Upper Lip Raiser
				// 0=neutral, covering teeth; 1=showing teeth fully; -1=maximal possible pushed down lip
				float fAU0 = manager.GetAnimUnit(0);
				SetJointRotation(UpperLipLeft, UpperLipLeftAxis, fAU0, UpperLipLeftNeutral, UpperLipLeftUp);
				SetJointRotation(UpperLipRight, UpperLipRightAxis, fAU0, UpperLipRightNeutral, UpperLipRightUp);
				
				// AU1 - Jaw Lowerer
				// 0=closed; 1=fully open; -1= closed, like 0
				float fAU1 = manager.GetAnimUnit(1);
				SetJointRotation(Jaw, JawAxis, fAU1, JawNeutral, JawDown);
				
				// AU2 – Lip Stretcher
				// 0=neutral; 1=fully stretched (joker’s smile); -1=fully rounded (kissing mouth)
				float fAU2 = manager.GetAnimUnit(2);
				SetJointRotation(LipLeft, LipLeftAxis, fAU2, LipLeftNeutral, LipLeftStretched);
				SetJointRotation(LipRight, LipRightAxis, fAU2, LipRightNeutral, LipRightStretched);
				
				// AU3 – Brow Lowerer
				// 0=neutral; -1=raised almost all the way; +1=fully lowered (to the limit of the eyes)
				float fAU3 = manager.GetAnimUnit(3);
				SetJointRotation(EyebrowLeft, EyebrowLeftAxis, fAU3, EyebrowLeftNeutral, EyebrowLeftLowered);
				SetJointRotation(EyebrowRight, EyebrowRightAxis, fAU3, EyebrowRightNeutral, EyebrowRightLowered);
				
				// AU4 – Lip Corner Depressor
				// 0=neutral; -1=very happy smile; +1=very sad frown
				float fAU4 = manager.GetAnimUnit(4);
				SetJointRotation(LipCornerLeft, LipCornerLeftAxis, fAU4, LipCornerLeftNeutral, LipCornerLeftDepressed);
				SetJointRotation(LipCornerRight, LipCornerRightAxis, fAU4, LipCornerRightNeutral, LipCornerRightDepressed);
				
				// AU5 – Outer Brow Raiser
				// 0=neutral; -1=fully lowered as a very sad face; +1=raised as in an expression of deep surprise
				float fAU5 = manager.GetAnimUnit(5);
				SetJointRotation(OuterBrowLeft, OuterBrowLeftAxis, fAU5, OuterBrowLeftNeutral, OuterBrowLeftRaised);
				SetJointRotation(OuterBrowRight, OuterBrowRightAxis, fAU5, OuterBrowRightNeutral, OuterBrowRightRaised);
			}
		}
	}
	
	private float GetJointRotation(Transform joint, AxisEnum axis)
	{
		float fJointRot = 0.0f;
		
		if(joint == null)
			return fJointRot;
		
		Vector3 jointRot = joint.localRotation.eulerAngles;
		
		switch(axis)
		{
			case AxisEnum.X:
				fJointRot = jointRot.x;
				break;
			
			case AxisEnum.Y:
				fJointRot = jointRot.y;
				break;
			
			case AxisEnum.Z:
				fJointRot = jointRot.z;
				break;
		}
		
		return fJointRot;
	}
	
	private void SetJointRotation(Transform joint, AxisEnum axis, float fAU, float fMin, float fMax)
	{
		if(joint == null)
			return;
		
//		float fSign = 1.0f;
//		if(fMax < fMin)
//			fSign = -1.0f;
		
		// [-1, +1] -> [0, 1]
		//fAUnorm = (fAU + 1f) / 2f;
		float fValue = fMin + (fMax - fMin) * fAU;
		
		Vector3 jointRot = joint.localRotation.eulerAngles;
		
		switch(axis)
		{
			case AxisEnum.X:
				jointRot.x = fValue;
				break;
			
			case AxisEnum.Y:
				jointRot.y = fValue;
				break;
			
			case AxisEnum.Z:
				jointRot.z = fValue;
				break;
		}
		
		joint.localRotation = Quaternion.Euler(jointRot);
	}
	
	
}
