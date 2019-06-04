using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RigidFrame_Development
{


	public class RFrame_Strut : MonoBehaviour {
		
		// public enum StrutType {
		// 	/**
		// 	* The basic fixed length strut linking two vertices.
		// 	*/
		// 	normal,
		// 	/**
		// 	* A variable length strut. Anything from a muscle based system, a
		// 	* spring to a piston.
		// 	*/
		// 	pushpull,
		// 	/**
		// 	* Anything where a rotation based system exists, where the strut
		// 	* represent the torque vector. Ableit with direction only.
		// 	* Most likely represent motors, or other rotation thingies.
		// 	*/
		// 	rotary,
		// 	/**
		// 	* The strut acts as a force vector. With the direction of force towards
		// 	* the second vertex.
		// 	* Can represent fans, rockets.
		// 	*/
		// 	thrust
		// }
		//public StrutType strutType = StrutType.normal;
		public float minLength = -1;
		public float maxLength = -1;
		public float length = 1;
		public float intendedLength = -1;

		public float actualLength = 0;
		//int customAffirm = 1;
		public int v1; // Array index into allVertices array in Frame_Object
		public int v2; // Ditto for v1.
		//bool ignoreCollideThisStrut = false;
		//bool locked = true;

		// Use this for initialization
		void Start () {
			
		}
		
		// Update is called once per frame
		void Update () {
			// Update the transform of this object so that it starts at v1 and points
			// towards v2.
			// Only update the struts line renderer if its enabled.
			if(GetComponent<LineRenderer>().enabled) {
				RFrame_Object parentFrame = transform.parent.gameObject.GetComponent<RFrame_Object>();
				GameObject v1Obj = parentFrame.allVertices[this.v1];
				GameObject v2Obj = parentFrame.allVertices[this.v2];
				Vector3[] positions = {v1Obj.transform.localPosition,v2Obj.transform.localPosition};
				this.updateLineRenderPositions(positions);
			}
		}
		
		public float getLength() {
			return length;
		}
    
		// Provides the checks for the strut of whether has any min/max or intended length settings.
		public float distanceOutFromAcceptableRange(float distanceToCheck) {
			// Check if variable length is set
			bool minMaxxed = false;
			float returnDifference = length - distanceToCheck;
			
			if (minLength > 0.0f && maxLength > 0.0f) {
				// Since there's a range that the length can be, reset
				// the return difference back to zero so it can be moved.
				returnDifference = 0.0f;
				if(distanceToCheck < minLength) {
					returnDifference = minLength - distanceToCheck;
					minMaxxed = true;
				}
				if (distanceToCheck > maxLength) {
					returnDifference = maxLength - distanceToCheck;
					minMaxxed = true;
				}
			}
			// Follow up with a check for the intended length. This gets
			// overridden by the min max limits if they're active.
			if (intendedLength > 0.0f && !minMaxxed) {
				returnDifference = intendedLength - distanceToCheck;
			}
			// Default is to return the difference in this struts fixed length versus the one given.
			return returnDifference;
		}

		public void updateLineRenderPositions(Vector3[] positions) {
			LineRenderer lineRenderer = GetComponent<LineRenderer>();
			lineRenderer.positionCount = 2;
			lineRenderer.SetPositions(positions);
		}

	}

}