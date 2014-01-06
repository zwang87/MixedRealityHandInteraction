using UnityEngine;
using System.Collections;

public class CubeCloudGenerator : MonoBehaviour {
	public int cubeEdgePopulation;
	public float cubeEdgeLength;
	public float spacing;
	
	public Vector3 cubeCloudPos;
	public Material cubeMaterial;

	public bool turnLeft = false;
	void Start () {
		var halfEdge =  cubeEdgePopulation/2;
		//this.gameObject.transform.parent = GameObject.Find("Turntable").transform;
		for (var i = -halfEdge; i < halfEdge ; i+=1){
			for (var j = -halfEdge; j < halfEdge ; j+=1){
				for (var k = -halfEdge; k < halfEdge ; k+=1){
					var newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);			
					float scale = cubeEdgeLength/(cubeEdgePopulation*(1+(cubeEdgePopulation-1)*spacing));
					newCube.transform.parent = this.gameObject.transform;
					newCube.transform.localScale = new Vector3(scale,scale,scale);
					newCube.transform.localPosition = new Vector3(i*(scale + spacing), j*(scale + spacing),k*(scale + spacing));	
					//The parent object is the one that executes the code, therefore it hasn't changed arbitrarily yet.
					//That's why it's safe to assign relative positions to it.
					newCube.AddComponent<Rigidbody>();
					newCube.rigidbody.useGravity = false;
					newCube.rigidbody.drag = 100;
					newCube.rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
					newCube.collider.isTrigger = false;
					newCube.tag = "cube";
					newCube.renderer.material = cubeMaterial;
				}
			}
		}
		this.gameObject.transform.localPosition += cubeCloudPos;
	}
	
	
	//Vector3 vec3(float f){ return new Vector3(f,f,f); }
	
	// Update is called once per frame
	void OnLevelWasLoaded () {
		
	}
	// Use this for initialization
	//void Start () {
	
	//}
	
	// Update is called once per frame
	void Update () {
		if(turnLeft){
			this.gameObject.transform.Rotate(new Vector3(0, 20*Time.deltaTime, 0));
			//turnLeft = false;
		}
	}
}