using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RigidFrame_Development
{
	public class RFrame_Component : MonoBehaviour {

		// This class positions and rotates a representation 3D model of a secion of the frame
		public float xRotationOffset = 0.0f;
		public float yRotationOffset = 0.0f;
		public float zRotationOffset = 0.0f;

		public float xOffset = 0.0f;
		public float yOffset = 0.0f;
		public float zOffset = 0.0f;


		public int centerVertex = 0;
		public int upVertex = 0;
		public int forwardVertex = 0;

		public string meshName = "";
		// Use this for initialization
		void Start () {
			
		}
		
		// Update is called once per frame
		void Update () {
			// Position the attached mesh to the centre vertex
			RFrame_Object parentFrame = transform.parent.gameObject.GetComponent<RFrame_Object>();
			GameObject centerObj = parentFrame.allVertices[this.centerVertex];
			GameObject upObj = parentFrame.allVertices[this.upVertex];
			GameObject forwardObj = parentFrame.allVertices[this.forwardVertex];
			Vector3 upVertex = upObj.transform.localPosition - centerObj.transform.localPosition;
			Vector3 forwardVertex = forwardObj.transform.localPosition - centerObj.transform.localPosition;
			Quaternion orientation = Quaternion.LookRotation(forwardVertex, upVertex);

			transform.localPosition = centerObj.transform.localPosition;
			transform.rotation = orientation;
			
		}
	}
}